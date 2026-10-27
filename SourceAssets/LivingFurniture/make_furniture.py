"""Original NightHome furniture. Run with Blender --background --factory-startup.
Exports metre-scale FBX with applied export copies and editable source parts.
No downloaded models or textures are used.
"""
import bpy
import bmesh
import json
import math
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
OUT = HERE.parent.parent / 'Assets/NightHome/LivingFinish/Models'
OUT.mkdir(parents=True, exist_ok=True)
stats = {}
for obj in list(bpy.context.scene.objects):bpy.data.objects.remove(obj,do_unlink=True)
for col in list(bpy.data.collections):
    if col.name.startswith(('Source_','Export_')):bpy.data.collections.remove(col)
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1
MATERIALS = ['GrayFabric', 'SeamGray', 'Walnut', 'Metal', 'RugWeave', 'RugEdge', 'FrostedShade']
COLORS = [(.43,.44,.43,1),(.28,.29,.28,1),(.33,.20,.10,1),(.08,.08,.075,1),(.61,.57,.49,1),(.38,.35,.30,1),(1,.85,.61,1)]
materials = []
for name, color in zip(MATERIALS, COLORS):
    mat=bpy.data.materials.get(name) or bpy.data.materials.new(name);mat.use_nodes=True
    node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value=color
    node.inputs['Roughness'].default_value=.9 if name in ('GrayFabric','RugWeave') else .4
    mat.diffuse_color=color; materials.append(mat)
parts=[]

def uv_metres(obj, dimensions):
    mesh=obj.data;layer=mesh.uv_layers.active or mesh.uv_layers.new(name='UV_Metres')
    grain=max(range(3),key=lambda i:dimensions[i])
    for face in mesh.polygons:
        normal_axis=max(range(3),key=lambda i:abs(face.normal[i]))
        u_axis=grain if grain!=normal_axis else (normal_axis+1)%3
        v_axis=next(i for i in range(3) if i not in (normal_axis,u_axis))
        for loop in face.loop_indices:
            v=mesh.vertices[mesh.loops[loop].vertex_index].co
            layer.data[loop].uv=(v[u_axis],v[v_axis])

def cushion(name,position,dimensions,tilt=(0,0,0),sag=0):
    bpy.ops.mesh.primitive_cube_add(size=2,location=position)
    obj=bpy.context.object;obj.name=name
    bm=bmesh.new();bm.from_mesh(obj.data)
    bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=7,use_grid_fill=True)
    bm.to_mesh(obj.data);bm.free()
    for vertex in obj.data.vertices:
        v=vertex.co.copy();v/=sum(abs(c)**4.4 for c in v)**(1/4.4)
        z=v.z*dimensions[2]/2
        if v.z>0:z-=sag*max(0,1-v.x*v.x)*max(0,1-v.y*v.y)*v.z
        vertex.co=(v.x*dimensions[0]/2,v.y*dimensions[1]/2,z)
    obj.data.update();uv_metres(obj,dimensions)
    for face in obj.data.polygons:face.use_smooth=True
    obj.rotation_euler=tilt;obj.data.materials.append(materials[0]);parts.append(obj)
    return obj
def box(name, position, dimensions, radius, material, segments=3, tilt=(0,0,0)):
    bpy.ops.mesh.primitive_cube_add(size=1,location=position)
    obj=bpy.context.object;obj.name=name;obj.dimensions=dimensions
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    uv_metres(obj,dimensions)
    if radius:
        bevel=obj.modifiers.new('Authored edge radius','BEVEL');bevel.width=radius;bevel.segments=segments
    for polygon in obj.data.polygons: polygon.use_smooth=True
    weighted=obj.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');weighted.keep_sharp=True
    obj.rotation_euler=tilt;obj.data.materials.append(materials[MATERIALS.index(material)])
    parts.append(obj);return obj

def seam(name, width, depth, height, cx, cy, material='SeamGray'):
    points=[]
    for i in range(96):
        angle=2*math.pi*i/96;c=math.cos(angle);s=math.sin(angle)
        x=math.copysign(abs(c)**(2/4.4),c)*(width/2+.0008)
        y=math.copysign(abs(s)**(2/4.4),s)*(depth/2+.0008)
        points.append((cx+x,cy+y,height))
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=.0016;curve.bevel_resolution=1
    spline=curve.splines.new('POLY');spline.points.add(len(points)-1)
    for p,co in zip(spline.points,points):p.co=(*co,1)
    spline.use_cyclic_u=True
    obj=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials[MATERIALS.index(material)])
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.convert(target='MESH');parts.append(obj)

def export(name):
    global parts
    # Preserve editable separate parts/modifiers; export an evaluated joined copy.
    source=bpy.data.collections.new('Source_'+name);bpy.context.scene.collection.children.link(source)
    copies=[];deps=bpy.context.evaluated_depsgraph_get()
    for part in parts:
        for col in list(part.users_collection):col.objects.unlink(part)
        source.objects.link(part)
        mesh=bpy.data.meshes.new_from_object(part.evaluated_get(deps),depsgraph=deps)
        copy=bpy.data.objects.new(part.name+'_Applied',mesh);bpy.context.scene.collection.objects.link(copy)
        copy.matrix_world=part.matrix_world.copy();copies.append(copy)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in copies:obj.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    bpy.ops.object.join();obj=bpy.context.object;obj.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},global_scale=1,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False)
    mesh=obj.data;mesh.calc_loop_triangles()
    stats[name]={'triangles':len(mesh.loop_triangles),'materials':len(mesh.materials),'dimensions_m':list(obj.dimensions)}
    col=bpy.data.collections.new('Export_'+name);bpy.context.scene.collection.children.link(col)
    for existing in list(obj.users_collection):existing.objects.unlink(obj)
    col.objects.link(obj);col.hide_viewport=True;col.hide_render=True
    offset=(len(stats)-1)*9
    for part in parts:part.location.x+=offset
    parts=[];return obj

def sofa(name,width):
    box('Upholstered plinth',(0,0,.22),(width-.08,.94,.17),.035,'GrayFabric')
    for x in (-width/2+.20,width/2-.20):
        for y in (-.34,.34):box('Recessed oak foot',(x,y,.08),(.085,.085,.16),.012,'Walnut')
    for x in (-width/2+.075,width/2-.075):box('Soft arm',(x,0,.45),(.15,1.04,.43),.065,'GrayFabric',5)
    seat_width=(width-.35)/3
    for i in range(3):
        x=(i-1)*seat_width
        cushion('Seat cushion with 11mm sag',(x,-.095,.37),(seat_width-.018,.77,.20),sag=.011)
        seam('Seat piping',seat_width-.018,.77,.37,x,-.095)
        cushion('Inclined back cushion',(x,.36,.65),(seat_width-.023,.22,.46),(math.radians(-8),0,0))
    for side in (-1,1):
        cushion('Loose cushion',(side*(width/2-.37),.10,.61),(.38,.18,.35),(math.radians(-12),side*.11,side*.10))
    return export(name)

sofa('Sofa320',3.2);sofa('Sofa300',3.0)
box('Solid oak top',(0,0,.435),(2.10,1.05,.05),.023,'Walnut',5)
for x in (-.79,.79):
    for y in (-.32,.32):
        foot=box('Tapered mortise leg',(x,y,.2075),(.08,.08,.415),.008,'Walnut')
        for v in foot.data.vertices:
            t=(v.co.z+.2075)/.415;v.co.x*=.62+.38*t;v.co.y*=.62+.38*t
        uv_metres(foot,(.08,.08,.415))
        box('Leg joint',(x,y,.389),(.11,.11,.08),.01,'Walnut')
box('Long apron',(0,.32,.375),(1.6,.045,.08),.008,'Walnut')
box('Long apron',(0,-.32,.375),(1.6,.045,.08),.008,'Walnut')
export('CoffeeTable210')

# Stage two meshes share the same material palette and metre UV convention.
box('Rug base',(0,0,.012),(7.4,5.5,.024),.011,'RugWeave',4)
for y in (-2.69,2.69):box('Bound rug edge',(0,y,.024),(7.25,.045,.006),.002,'RugEdge',2)
for x in (-3.64,3.64):box('Bound rug edge',(x,0,.024),(.045,5.35,.006),.002,'RugEdge',2)
export('Rug740')
width=4.1;depth=.60
for x in (-1.80,1.80):
    for y in (-.21,.21):box('Console foot',(x,y,.065),(.055,.055,.13),.007,'Metal')
box('Console bottom',(0,0,.145),(width,depth,.045),.008,'Walnut')
box('Console top',(0,0,.62),(width+.025,depth+.025,.045),.009,'Walnut',4)
box('Console back',(0,.28,.38),(width,.025,.45),.004,'Walnut')
for x in (-width/2+.02,width/2-.02,-.63,.63):box('Carcass divider',(x,0,.38),(.032,depth,.45),.005,'Walnut')
box('Open shelf',(0,0,.37),(1.22,.56,.027),.004,'Walnut')
for x in (-1.34,1.34):
    box('Recessed door',(x,-.30,.38),(1.29,.035,.43),.006,'Walnut')
    box('Pull',(x,-.326,.48),(.22,.022,.015),.005,'Metal')
export('TVConsole410')
box('Canopy',(0,0,0),(.22,.22,.045),.02,'Metal')
box('Pendant stem',(0,0,-.14),(.016,.016,.25),.005,'Metal')
box('Linen diffuser',(0,0,-.34),(1.20,.52,.20),.08,'FrostedShade',5)
box('Shade upper rim',(0,0,-.25),(1.21,.53,.025),.01,'Walnut')
export('Pendant120')

# Editable source collections are laid out separately; export copies retain the origin.
(HERE/'mesh_stats.json').write_text(json.dumps(stats,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'LivingFurniture.blend'))
