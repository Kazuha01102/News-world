"""Compare the previous panel algorithm with the sewn rectangular design first."""
import bpy,bmesh,sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE));import cloth_shapes
project=HERE.parent.parent
legacy=project/'SourceAssets/LivingFurniture/make_furniture.py'
namespace={'__file__':str(legacy)}
exec(compile(legacy.read_text().split('def export(name):')[0],str(legacy),'exec'),namespace)
out=project/'Assets/NightHome/UpholsteryStudy/Models';out.mkdir(parents=True,exist_ok=True)
for name,kind,dimensions in [('SeatBefore','seat',(.932,.77,.20)),('SeatAfter','seat',(.932,.77,.20)),('PillowBefore','pillow',(.75,.46,.18)),('PillowAfter','pillow',(.75,.46,.18))]:
    z=.748 if kind=='pillow' else 0
    if name.endswith('Before'):
        bpy.ops.mesh.primitive_cube_add(size=2,location=(0,0,z));obj=bpy.context.object;obj.name=name
        bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.subdivide_edges(bm,edges=list(bm.edges),cuts=7,use_grid_fill=True);bm.to_mesh(obj.data);bm.free()
        for vertex in obj.data.vertices:
            v=vertex.co.copy();v/=sum(abs(c)**4.4 for c in v)**(1/4.4)
            height=v.z*dimensions[2]/2
            if v.z>0:height-=(.008 if kind=='pillow' else .011)*max(0,1-v.x*v.x)*max(0,1-v.y*v.y)*v.z
            vertex.co=(v.x*dimensions[0]/2,v.y*dimensions[1]/2,height)
        obj.data.update()
        for f in obj.data.polygons:f.use_smooth=True
    else:obj=cloth_shapes.create(name,(0,0,z),dimensions,kind=kind)
    if not obj.data.materials:obj.data.materials.append(namespace['materials'][0])
    namespace['uv_metres'](obj,dimensions)
    bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    for v in obj.data.vertices:v.co.z-=z
    obj.scale.x=-1;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    obj.location.x=len(bpy.context.scene.objects)*1.3
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'ClothPrototype.blend'))
print('Old/new seat and pillow prototypes exported; original furniture files untouched')
