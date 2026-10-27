"""Original metre-scale plants and architectural finish. Run in a dedicated Blender file.
Rebuilds the open Blender scene; the existing furniture .blend on disk is untouched.
Opaque, closed leaf geometry avoids alpha sorting and shows the leaf undersides.
"""
import bpy, bmesh, math, json, random
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
OUT=HERE.parent.parent/'Assets/NightHome/PlantsWalls/Models'
OUT.mkdir(parents=True,exist_ok=True)
for obj in list(bpy.data.objects):bpy.data.objects.remove(obj,do_unlink=True)
for col in list(bpy.data.collections):bpy.data.collections.remove(col)
bpy.context.scene.unit_settings.system='METRIC'
bpy.context.scene.unit_settings.scale_length=1
MATS={}
for name,color in [('Leaf',(.20,.33,.18,1)),('Bark',(.22,.16,.10,1)),('Ceramic',(.72,.69,.61,1)),('Soil',(.075,.055,.035,1)),('Wallpaper',(.83,.80,.74,1)),('Trim',(.77,.73,.65,1))]:
    m=bpy.data.materials.get(name) or bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=color
    node=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value=color;node.inputs['Roughness'].default_value=.88
    MATS[name]=m
parts=[];stats={}

def mesh_object(name,verts,faces,mat,uv=None,colors=None,smooth=False):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.scene.collection.objects.link(obj);mesh.materials.append(MATS[mat])
    layer=mesh.uv_layers.new(name='UV_Metres')
    for f in mesh.polygons:
        f.use_smooth=smooth
        axis=max(range(3),key=lambda a:abs(f.normal[a]))
        for loop in f.loop_indices:
            index=mesh.loops[loop].vertex_index;v=mesh.vertices[index].co
            layer.data[loop].uv=uv[index] if uv else ((v.x,v.y) if axis==2 else ((v.y,v.z) if axis==0 else (v.x,v.z)))
    attr=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
    for i,c in enumerate(attr.data):c.color=colors[i] if colors else (1,1,1,1)
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    parts.append(obj);return obj

def box(name,pos,size,mat,bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);obj=bpy.context.object;obj.name=name;obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);obj.data.materials.append(MATS[mat])
    layer=obj.data.uv_layers.active
    for f in obj.data.polygons:
        a=max(range(3),key=lambda i:abs(f.normal[i]))
        for loop in f.loop_indices:
            v=obj.data.vertices[obj.data.loops[loop].vertex_index].co
            layer.data[loop].uv=(v.x,v.y) if a==2 else ((v.y,v.z) if a==0 else (v.x,v.z))
    if bevel:
        mod=obj.modifiers.new('Small physical edge','BEVEL');mod.width=bevel;mod.segments=2
        normal=obj.modifiers.new('Weighted normals','WEIGHTED_NORMAL');normal.keep_sharp=True
    parts.append(obj);return obj

def tube(name,points,radii,mat='Bark',sides=8):
    vs=[];fs=[]
    for i,p in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])
        tangent.normalize();side=tangent.cross(Vector((0,1,0))).normalized();up=tangent.cross(side).normalized()
        for j in range(sides):
            a=j*2*math.pi/sides;vs.append(Vector(p)+radii[i]*(math.cos(a)*side+math.sin(a)*up))
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
    return mesh_object(name,vs,fs,mat,smooth=True)

def pot():
    # Continuous outside, rim, inside and base: real wall thickness, not a solid cube.
    rings=[(.165,0),(.174,.025),(.226,.444),(.231,.468),(.229,.482),(.211,.482),(.207,.463),(.197,.10)]
    vs=[];fs=[];n=48
    for r,z in rings:
        for j in range(n):a=j*2*math.pi/n;vs.append((r*math.cos(a),r*math.sin(a),z))
    for i in range(len(rings)-1):
        for j in range(n):a=i*n+j;b=i*n+(j+1)%n;fs.append((a,b,b+n,a+n))
    fs.append(tuple(reversed(range(n))))
    center=len(vs);vs.append((0,0,.10));start=(len(rings)-1)*n
    for j in range(n):fs.append((start+j,start+(j+1)%n,center))
    mesh_object('Ceramic planter with thick rim',vs,fs,'Ceramic',smooth=True)
    vs=[(0,0,.433)]+[(.203*math.cos(j*2*math.pi/n),.203*math.sin(j*2*math.pi/n),.431) for j in range(n)]
    mesh_object('Recessed soil',vs,[(0,j+1,(j+1)%n+1) for j in range(n)],'Soil')

def leaf(base,angle,length,width,droop,color,twist):
    pitch=.22+.24*abs(twist);roll=twist*.55
    forward=Vector((math.cos(angle)*math.cos(pitch),math.sin(angle)*math.cos(pitch),math.sin(pitch)))
    side=Vector((-math.sin(angle),math.cos(angle),0))*math.cos(roll)+Vector((0,0,1))*math.sin(roll)
    normal=forward.cross(side).normalized();side=normal.cross(forward).normalized()
    vs=[];uv=[];colors=[];rows=11;cols=5
    for surface in (-1,1):
        for i in range(rows):
            t=i/(rows-1);w=max(.001,math.sin(math.pi*t)**.72*width/2)
            mid=Vector(base)+forward*(length*t)+Vector((0,0,length*(.22*math.sin(math.pi*t)-droop*t*t)))
            for j in range(cols):
                s=(j-2)/2
                z=-.022*abs(s)*math.sin(math.pi*t)+twist*s*t*.025+surface*.00075
                vs.append(mid+side*(s*w)+normal*z);uv.append((t,(s+1)/2));colors.append((*[c*(.92+.08*math.sin(math.pi*t)) for c in color],1))
    fs=[];count=rows*cols
    for k in range(2):
        for i in range(rows-1):
            for j in range(cols-1):
                a=k*count+i*cols+j;face=(a,a+1,a+cols+1,a+cols);fs.append(face if k==1 else tuple(reversed(face)))
    boundary=list(range(cols))+[i*cols+cols-1 for i in range(1,rows)]+list(range(count-2,count-cols-1,-1))+[i*cols for i in range(rows-2,0,-1)]
    for a,b in zip(boundary,boundary[1:]+boundary[:1]):fs.append((a,b,b+count,a+count))
    mesh_object('Folded curved leaf',vs,fs,'Leaf',uv,colors,True)

def plant(name,compact=False):
    rng=random.Random(503 if compact else 127);pot();height=1.18 if compact else 1.60
    def trunk(z):return Vector((.045*math.sin(z*3)+(.085 if compact else .035)*z,.026*math.sin(z*5),z))
    points=[trunk(.42+(height-.42)*i/12) for i in range(13)]
    tube('Tapered living trunk',points,[.013-.007*i/12 for i in range(13)])
    total=13 if compact else 18
    for i in range(total):
        z=.59+(height-.64)*(i/(total-1))**.82+rng.uniform(-.023,.023)
        angle=(rng.uniform(-1.35,1.35) if compact else i*2.31+rng.uniform(-.27,.27))
        start=trunk(z);direction=Vector((math.cos(angle),math.sin(angle),0))
        reach=rng.uniform(.045,.09) if compact else rng.uniform(.06,.13)
        base=start+direction*reach+Vector((0,0,.035+rng.random()*.025))
        middle=start.lerp(base,.52)+Vector((0,0,.012))
        tube('Branch and leaf petiole',[start,middle,base],[.0045,.0032,.0020],sides=7)
        length=rng.uniform(.21,.30) if compact else rng.uniform(.28,.42)
        if i>total-4:length*=.77
        color=rng.choice([(.78,.94,.73),(.94,1,.87),(.65,.83,.63)])
        leaf(base,angle+rng.uniform(-.12,.12),length,length*rng.uniform(.57,.74),rng.uniform(.04,.24),color,rng.uniform(-1,1))
    export(name)

def export(name):
    global parts
    source=bpy.data.collections.new('Source_'+name);bpy.context.scene.collection.children.link(source)
    copies=[];deps=bpy.context.evaluated_depsgraph_get()
    for obj in parts:
        for col in list(obj.users_collection):col.objects.unlink(obj)
        source.objects.link(obj)
        mesh=bpy.data.meshes.new_from_object(obj.evaluated_get(deps),depsgraph=deps)
        copy=bpy.data.objects.new(obj.name+'_Applied',mesh);bpy.context.scene.collection.objects.link(copy);copy.matrix_world=obj.matrix_world.copy();copies.append(copy)
    bpy.ops.object.select_all(action='DESELECT')
    for o in copies:o.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    if len(copies)>1:bpy.ops.object.join()
    obj=bpy.context.object;obj.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # FBX axis conversion to Unity reflects X. Compensate in the applied export
    # mesh (not a negative-scale Unity transform), then restore outward normals.
    obj.scale.x=-1;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,colors_type='LINEAR')
    obj.data.calc_loop_triangles();stats[name]={'triangles':len(obj.data.loop_triangles),'materials':len(obj.data.materials),'dimensions_blender_m':list(obj.dimensions)}
    col=bpy.data.collections.new('Export_'+name);bpy.context.scene.collection.children.link(col)
    for old in list(obj.users_collection):old.objects.unlink(obj)
    col.objects.link(obj);col.hide_viewport=True;col.hide_render=True
    source.hide_viewport=True;source.hide_render=True;parts=[]

plant('BroadleafTall');plant('BroadleafCompact',True)

# Build a surface union of the axis-aligned structural boxes. There are no internal
# coplanar faces at touching slabs. Keep existing Unity colliders as separate objects.
boxes=json.loads((HERE/'wall_layout.json').read_text())
boxes.append({'name':'Former control window infill','min':[-1,1,7.9],'max':[1,2.9,8.1]})
coords=[sorted(set(round(b[k][a],4) for b in boxes for k in ('min','max'))) for a in range(3)]
occupied=set()
for i in range(len(coords[0])-1):
 for j in range(len(coords[1])-1):
  for k in range(len(coords[2])-1):
   p=[(coords[a][v]+coords[a][v+1])/2 for a,v in enumerate((i,j,k))]
   if any(all(b['min'][a]-.0001<p[a]<b['max'][a]+.0001 for a in range(3)) for b in boxes):occupied.add((i,j,k))
vs=[];fs=[];foot_edges=[]
for cell in sorted(occupied):
 for axis in range(3):
  for sign in (-1,1):
   neighbor=list(cell);neighbor[axis]+=sign
   if tuple(neighbor) in occupied:continue
   other=[a for a in range(3) if a!=axis]
   face=[]
   for u,v in [(0,0),(1,0),(1,1),(0,1)]:
    p=[0,0,0];p[axis]=coords[axis][cell[axis]+(sign==1)]
    p[other[0]]=coords[other[0]][cell[other[0]]+u];p[other[1]]=coords[other[1]][cell[other[1]]+v]
    # Unity xyz -> Blender x,-z,y.
    face.append((p[0],-p[2],p[1]))
   normal=(Vector(face[1])-Vector(face[0])).cross(Vector(face[2])-Vector(face[0]));expected=Vector((sign if axis==0 else 0,-sign if axis==2 else 0,sign if axis==1 else 0))
   if normal.dot(expected)<0:face.reverse()
   fs.append(tuple(range(len(vs),len(vs)+4)));vs.extend(face)
   if axis!=1 and abs(coords[1][cell[1]])<.001:
    center=[sum(c[a] for c in face)/4 for a in range(3)]
    if abs(center[0])<9.99 and abs(center[1])<7.99:foot_edges.append((axis,sign,face))
obj=mesh_object('Continuous structural wall shell',vs,fs,'Wallpaper')
bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00005);bmesh.ops.dissolve_limit(bm,angle_limit=.001,verts=list(bm.verts),edges=list(bm.edges));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
# Reproject after welding/dissolving, with continuous real-metre coordinates.
layer=obj.data.uv_layers.active
for f in obj.data.polygons:
 axis=max(range(3),key=lambda a:abs(f.normal[a]))
 for loop in f.loop_indices:
  v=obj.data.vertices[obj.data.loops[loop].vertex_index].co;layer.data[loop].uv=(v.x,v.y) if axis==2 else ((v.y,v.z) if axis==0 else (v.x,v.z))
export('WallShell')

# Baseboards follow room-side floor boundaries, not window outlines.
for axis,sign,face in foot_edges:
    lo=[min(p[a] for p in face) for a in range(3)];hi=[max(p[a] for p in face) for a in range(3)]
    if axis==0:
        center=(lo[0]+sign*.008,(lo[1]+hi[1])/2,.06);size=(.016,hi[1]-lo[1],.10)
        box('Skirting',center,size,'Trim',.0015)
    else:
        ranges=[(lo[0],hi[0])]
        if lo[1]>7.8: # inside south wall: interrupt at the entry door
            ranges=[(a,min(b,-1.70)) for a,b in ranges if a<-1.70]+[(max(a,-.30),b) for a,b in ranges if b>-.30]
        for a,b in ranges:
            if b-a>.001:box('Skirting',((a+b)/2,lo[1]-sign*.008,.06),(b-a,.016,.10),'Trim',.0015)

def window(cx,width):
    for x in (cx-width/2+.025,cx+width/2-.025):box('Window jamb',(x,-7.97,1.95),(.07,.24,1.90),'Trim')
    for z in (1.03,2.87):box('Window rail',(cx,-7.97,z),(width,.24,.07),'Trim')
    box('Projecting window sill',(cx,-7.82,.989),(width+.10,.34,.055),'Trim',.006)
    if width>2:
        for x in (cx-width/6,cx+width/6):box('Window mullion',(x,-8.005,1.95),(.045,.07,1.80),'Trim',.002)
window(-7.5,1.0);window(5.5,5.0)

def door_frame(cx,cy,width,depth,rotate=False):
    # cx,cy in Blender XY; opening top remains above 2.4m.
    for side in (-1,1):
        p=(cx+side*(width/2+.018),cy,1.21);d=(.055,depth,2.42)
        if rotate:p=(cx,cy+side*(width/2+.018),1.21);d=(depth,.055,2.42)
        box('Door jamb',p,d,'Trim')
    box('Door lintel',(cx,cy,2.43),(depth,width+.09,.07) if rotate else (width+.09,depth,.07),'Trim')
door_frame(-1,7.80,1.30,.12)
door_frame(3.1,-2,1.40,.25)
door_frame(4,3,1.60,.25,True)
door_frame(2,2,2.0,.25,True)
export('ArchitecturalTrim')
(HERE/'mesh_stats.json').write_text(json.dumps(stats,indent=2))
bpy.data.collections['Source_BroadleafTall'].hide_viewport=False
bpy.data.collections['Source_BroadleafTall'].hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'PlantsWalls.blend'))
print(json.dumps(stats))
