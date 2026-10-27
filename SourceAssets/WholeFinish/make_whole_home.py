"""NightHome remaining interiors. Run in a dedicated Blender file via MCP.
Reuses the authored bevel/cushion/UV methods from LivingFurniture, not its exports.
Source collections remain editable. Applied furniture-only copies are exported to FBX.
"""
import bpy,bmesh,math,json,random
from pathlib import Path
from mathutils import Vector
AUTHOR=Path(__file__).resolve().parent
living=AUTHOR.parent/'LivingFurniture/make_furniture.py'
exec(compile(living.read_text().split('def export(name):')[0],str(living),'exec'))
HERE=AUTHOR;OUT=HERE.parent.parent/'Assets/NightHome/WholeFinish/Models';OUT.mkdir(parents=True,exist_ok=True)
for name,color in [('BedLinen',(.76,.74,.68,1)),('BookBlue',(.28,.36,.40,1)),('BookClay',(.48,.32,.24,1)),('BookPages',(.76,.73,.65,1)),('Ceramic',(.76,.725,.66,1)),('TeaBrown',(.10,.055,.026,1)),('Charcoal',(.065,.075,.08,1))]:
    MATERIALS.append(name);mat=bpy.data.materials.get(name) or bpy.data.materials.new(name);mat.use_nodes=True
    next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=color
    mat.diffuse_color=color;materials.append(mat)

def mat_on(obj,name):obj.data.materials.clear();obj.data.materials.append(materials[MATERIALS.index(name)])

def mesh(name,verts,faces,material,smooth=True):
    data=bpy.data.meshes.new(name);data.from_pydata(verts,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(obj)
    uv_metres(obj,obj.dimensions)
    for f in data.polygons:f.use_smooth=smooth
    mat_on(obj,material);parts.append(obj);return obj

def export(name):
    global parts
    col=bpy.data.collections.new('Source_'+name);bpy.context.scene.collection.children.link(col)
    copies=[];deps=bpy.context.evaluated_depsgraph_get()
    for part in parts:
        for old in list(part.users_collection):old.objects.unlink(part)
        col.objects.link(part)
        data=bpy.data.meshes.new_from_object(part.evaluated_get(deps),depsgraph=deps)
        obj=bpy.data.objects.new(part.name+'_Applied',data);bpy.context.scene.collection.objects.link(obj);obj.matrix_world=part.matrix_world.copy();copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in copies:obj.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    if len(copies)>1:bpy.ops.object.join()
    obj=bpy.context.object;obj.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    obj.scale.x=-1;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_scale_options='FBX_SCALE_ALL',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    obj.data.calc_loop_triangles();stats[name]={'triangles':len(obj.data.loop_triangles),'materials':len(obj.data.materials),'dimensions_blender_m':list(obj.dimensions)}
    out=bpy.data.collections.new('Export_'+name);bpy.context.scene.collection.children.link(out)
    for old in list(obj.users_collection):old.objects.unlink(obj)
    out.objects.link(obj);out.hide_viewport=True;out.hide_render=True
    col.hide_viewport=True;col.hide_render=True;parts=[]

def lathe(name,profile,material,n=40):
    verts=[];faces=[];rings=[]
    for radius,z in profile:
        ring=[]
        for j in range(n if radius else 1):
            a=2*math.pi*j/n;ring.append(len(verts));verts.append((radius*math.cos(a),radius*math.sin(a),z))
        rings.append(ring)
    for first,second in zip(rings,rings[1:]):
        for j in range(n):
            if len(first)==1:faces.append((first[0],second[(j+1)%n],second[j]))
            elif len(second)==1:faces.append((first[j],first[(j+1)%n],second[0]))
            else:faces.append((first[j],first[(j+1)%n],second[(j+1)%n],second[j]))
    obj=mesh(name,verts,faces,material)
    bm=bmesh.new();bm.from_mesh(obj.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(obj.data);bm.free()
    return obj

def rug(name,w,d):
    box('Soft textile base',(0,0,.012),(w,d,.024),.011,'RugWeave',4)
    for y in (-d/2+.05,d/2-.05):box('Bound edge',(0,y,.024),(w-.10,.032,.006),.002,'RugEdge',2)
    for x in (-w/2+.05,w/2-.05):box('Bound edge',(x,0,.024),(.032,d-.10,.006),.002,'RugEdge',2)
    export(name)

# Bedroom: 180 x 210cm mattress, recessed frame and a lightly draped duvet.
for x in (-.79,.79):
    for y in (-.94,.94):box('Bed leg',(x,y,.125),(.085,.085,.25),.012,'Walnut')
box('Bed platform',(0,0,.34),(1.96,2.26,.18),.025,'Walnut',4)
box('Recessed underframe',(0,0,.235),(1.80,2.10,.09),.012,'Metal')
box('Rounded headboard',(0,-1.15,.68),(2.04,.10,1.16),.035,'Walnut',4)
box('Headboard upholstered inset',(0,-1.086,.84),(1.85,.045,.55),.018,'GrayFabric')
mat_on(cushion('Rounded mattress',(0,0,.54),(1.80,2.10,.24),sag=.005),'BedLinen')
for x in (-.44,.44):mat_on(cushion('Soft pillow',(x,-.72,.748),(.75,.46,.18),(math.radians(5),0,x*.03),sag=.008),'BedLinen')
vs=[];fs=[];nx=19;ny=19
for surface in (0,1):
 for i in range(nx):
  u=i/(nx-1)*2-1
  for j in range(ny):
   v=j/(ny-1)*2-1;x=.945*u;y=.25+.775*v
   loft=.035*(1-u*u)*(1-v*v)
   remaining=max(0,1-(abs(x)/.90)**4.4-(abs(y)/1.05)**4.4)
   mattress_surface=.54+.12*remaining**(1/4.4)
   fold=.015*math.exp(-((v+.77)/.18)**2)*(.7+.3*math.cos(u*5))
   z=mattress_surface+(.045+loft+fold if surface else .006)+.003*math.sin(v*7+u*2)*(1-u*u)
   vs.append((x,y,z))
count=nx*ny
for k in range(2):
 for i in range(nx-1):
  for j in range(ny-1):a=k*count+i*ny+j;fs.append((a,a+ny,a+ny+1,a+1) if k else (a+1,a+ny+1,a+ny,a))
edge=list(range(ny))+[i*ny+ny-1 for i in range(1,nx)]+list(range(count-2,count-ny-1,-1))+[i*ny for i in range(nx-2,0,-1)]
for a,b in zip(edge,edge[1:]+edge[:1]):fs.append((a,b,b+count,a+count))
mesh('Lofted draped duvet',vs,fs,'GrayFabric');export('BedroomBed')

for x in (-.23,.23):
    for y in (-.19,.19):box('Nightstand tapered leg',(x,y,.255),(.045,.045,.51),.006,'Walnut')
box('Nightstand carcass',(0,0,.515),(.60,.50,.12),.01,'Walnut')
box('Nightstand top',(0,0,.575),(.62,.52,.05),.015,'Walnut',4)
box('Inset drawer',(0,-.255,.518),(.535,.025,.072),.005,'Walnut')
box('Drawer pull',(0,-.274,.52),(.12,.012,.014),.004,'Metal');export('BedsideTable')
lathe('Lamp foot',[(0,0),(.11,0),(.115,.012),(.10,.027),(0,.027)],'Metal')
box('Lamp stem',(0,0,.14),(.014,.014,.24),.004,'Metal')
lathe('Linen lampshade',[(.15,.25),(.105,.45),(.096,.45),(.14,.25),(.15,.25)],'FrostedShade')
export('BedsideLamp')

# Full-length opaque curtains with actual folds, not alpha cards or thin cubes.
for cx in (-2.05,2.05):
    vs=[];fs=[];nx=33;ny=9
    for layer in (-1,1):
     for i in range(nx):
      t=i/(nx-1);x=cx+(t-.5)*.80
      for j in range(ny):
       h=j/(ny-1);z=.035+2.93*h
       y=.042*math.cos(t*math.pi*12)*(1-.20*h)+.012*math.sin(h*math.pi)+layer*.001
       vs.append((x,y,z))
    count=nx*ny
    for k in range(2):
     for i in range(nx-1):
      for j in range(ny-1):a=k*count+i*ny+j;fs.append((a,a+1,a+ny+1,a+ny) if k else (a+ny,a+ny+1,a+1,a))
    edge=list(range(ny))+[i*ny+ny-1 for i in range(1,nx)]+list(range(count-2,count-ny-1,-1))+[i*ny for i in range(nx-2,0,-1)]
    for a,b in zip(edge,edge[1:]+edge[:1]):fs.append((a,b,b+count,a+count))
    mesh('Natural curtain folds',vs,fs,'BedLinen')
box('Curtain rail',(0,0,3.035),(5.30,.035,.035),.012,'Metal')
for cx in (-2.05,2.05):
    for i in range(6):box('Fabric hanging tab',(cx+(i-2.5)*.128,0,3.005),(.035,.030,.09),.005,'BedLinen',2)
for x in (-2.45,2.45):
    box('Curtain rod wall bracket',(x,-.19,3.035),(.025,.38,.025),.006,'Metal')
    box('Curtain bracket wall plate',(x,-.38,3.035),(.065,.018,.14),.006,'Metal')
export('BedroomCurtains');rug('BedroomRug',4.8,4.2)

def quiet_sofa(name,width,seats):
    box('Upholstered base',(0,0,.22),(width-.08,.94,.17),.035,'GrayFabric')
    for x in (-width/2+.20,width/2-.20):
        for y in (-.34,.34):box('Recessed foot',(x,y,.08),(.085,.085,.16),.012,'Walnut')
    for x in (-width/2+.075,width/2-.075):box('Soft arm',(x,0,.45),(.15,1.04,.43),.065,'GrayFabric',5)
    w=(width-.35)/seats
    for i in range(seats):
        x=(i-(seats-1)/2)*w
        cushion('Sagged seat',(x,-.095,.37),(w-.02,.77,.20),sag=.011)
        cushion('Inclined back',(x,.36,.65),(w-.025,.22,.46),(math.radians(-8),0,0))
        seam('Seat piping',w-.02,.77,.37,x,-.095)
    for side in (-1,1):mat_on(cushion('Loose accent pillow',(side*(width/2-.37),.11,.61),(.38,.18,.35),(math.radians(-12),side*.10,side*.12)),'BedLinen')
    export(name)
quiet_sofa('QuietSofa300',3,3);quiet_sofa('QuietSofa240',2.4,2)
box('Small rounded tabletop',(0,0,.455),(1.5,.9,.05),.024,'Walnut',5)
for x in (-.57,.57):
 for y in (-.29,.29):
    leg=box('Tapered table leg',(x,y,.22),(.065,.065,.44),.007,'Walnut')
    for v in leg.data.vertices:t=(v.co.z+.22)/.44;v.co.x*=.67+.33*t;v.co.y*=.67+.33*t
export('QuietTable150');rug('QuietRug',4.6,4.5)
mat_on(cushion('Floor cushion',(0,0,.09),(.68,.64,.18),sag=.012),'BedLinen');export('FloorCushion')

# Entry storage and a framed wood door keep the existing spawn and colliders clear.
for x in (-.68,.68):
 for y in (-.14,.14):box('Storage foot',(x,y,.06),(.05,.05,.12),.006,'Metal')
for x in (-.825,.825):box('Storage side',(x,0,.45),(.05,.42,.72),.007,'Walnut')
box('Storage back',(0,.20,.45),(1.65,.025,.72),.004,'Walnut')
for z in (.12,.43,.83):box('Storage shelf',(0,0,z),(1.7,.42,.035),.007,'Walnut')
for x in (-.40,.40):
 box('Storage door',(x,-.215,.63),(.78,.028,.34),.006,'Walnut')
 box('Recessed pull',(x,-.235,.745),(.15,.012,.012),.003,'Metal')
box('Storage top',(0,0,.855),(1.73,.45,.045),.010,'Walnut');export('EntryStorage')
box('Door leaf',(0,0,1.2),(1.30,.075,2.40),.012,'Walnut')
for z,h in ((.57,.88),(1.65,1.04)):box('Recessed door panel',(0,-.042,z),(1.10,.018,h),.012,'Walnut')
box('Handle escutcheon',(.47,-.060,1.05),(.055,.018,.18),.006,'Metal')
box('Door lever',(.405,-.088,1.05),(.19,.035,.025),.009,'Metal');export('EntryDoor');rug('EntryMat',1.9,1.1)

for x in (-.875,.875):box('Bookcase side',(x,0,.94),(.05,.38,1.82),.008,'Walnut')
box('Bookcase back',(0,.177,.95),(1.75,.025,1.80),.004,'Walnut')
for z in (.12,.55,.98,1.41,1.86):box('Bookcase shelf',(0,0,z),(1.80,.38,.03),.005,'Walnut')
for x in (-.75,.75):box('Bookcase foot',(x,0,.055),(.07,.28,.11),.008,'Walnut')
rng=random.Random(314)
for level,z in enumerate((.135,.565,.995,1.425)):
    x=-.76
    for j in range(5 if level%2==0 else 3):
        width=rng.uniform(.035,.065);h=rng.uniform(.23,.34);d=rng.uniform(.16,.22);y=-.04+rng.uniform(-.018,.015)
        cover=['BookBlue','BookClay','BedLinen'][(j+level)%3]
        box('Book pages',(x,y,z+h/2),(width-.008,d-.008,h-.01),.002,'BookPages',2)
        for xx in (-width/2,width/2):box('Book cover',(x+xx,y,z+h/2),(.004,d,h),.001,cover,2)
        box('Book spine',(x,y-d/2,z+h/2),(width,.005,h),.002,cover,2)
        x+=width+.01
    if level in (0,2):
        for j in range(2):box('Stacked book',(.42+j*.018,-.03,z+.017+j*.034),(.27,.19,.03),.003,'BookBlue' if j else 'BookClay')
export('Bookcase')

lathe('Mug with lip and internal thickness',[(.0,0),(.041,0),(.045,.006),(.046,.102),(.044,.106),(.040,.104),(.039,.018),(.0,.018)],'Ceramic',40)
vs=[];fs=[];n=20;sides=8
for i in range(n):
 a=-math.pi/2+math.pi*i/(n-1);p=Vector((.043+.035*math.cos(a),0,.055+.032*math.sin(a)))
 tangent=Vector((-.035*math.sin(a),0,.032*math.cos(a))).normalized();side=Vector((0,1,0));up=tangent.cross(side)
 for j in range(sides):b=j*2*math.pi/sides;vs.append(p+.0065*(side*math.cos(b)+up*math.sin(b)))
for i in range(n-1):
 for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
fs.append(tuple(reversed(range(sides))));fs.append(tuple((n-1)*sides+j for j in range(sides)))
mesh('Connected mug handle',vs,fs,'Ceramic')
lathe('Tea surface',[(0,.082),(.038,.082),(.038,.083),(0,.083)],'TeaBrown',40);export('Mug')

box('Pen return desk top',(0,0,.835),(2.2,.60,.07),.020,'Walnut',4)
for x in (-.91,.91):
 for y in (-.21,.21):box('Desk leg',(x,y,.40),(.065,.065,.80),.008,'Metal')
for y in (-.21,.21):box('Desk apron',(0,y,.76),(1.86,.035,.07),.007,'Walnut')
export('PenReturnDesk')
box('Switch housing',(0,0,0),(.13,.36,.36),.025,'Walnut',4)
box('Switch face',(.067,0,0),(.012,.28,.28),.018,'Metal',4)
box('Use indicator',(.078,0,0),(.008,.065,.12),.010,'FrostedShade');export('MirrorBezel')
lathe('Flush ceiling mount',[(0,0),(.18,0),(.18,-.025),(0,-.025)],'Metal')
lathe('Soft ceiling diffuser',[(0,-.025),(.22,-.025),(.235,-.045),(.22,-.080),(0,-.080)],'FrostedShade')
export('CeilingDisc')

for x in (-1.60,1.60):box('Log side frame',(x,0,0),(.05,.065,2.50),.008,'Walnut')
for z in (-1.225,1.225):box('Log horizontal frame',(0,0,z),(3.25,.065,.05),.008,'Walnut')
export('LogFrame')
for x in (-1.53,1.53):box('Drawing board side',(x,0,0),(.045,.08,1.99),.007,'Walnut')
for z in (-.9725,.9725):box('Drawing board horizontal',(0,0,z),(3.10,.08,.045),.007,'Walnut')
export('DrawingFrame')
box('Occupancy sign frame',(0,0,0),(.06,1.90,.86),.016,'Walnut')
box('Occupancy sign face',(-.034,0,0),(.008,1.78,.74),.008,'Charcoal')
export('QuietOccupancyPanel')
rug('DrawingRug',3.4,3.0)
(HERE/'mesh_stats.json').write_text(json.dumps(stats,indent=2))
bpy.data.collections['Source_BedroomBed'].hide_viewport=False
bpy.data.collections['Source_BedroomBed'].hide_render=False
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'WholeHomeFurniture.blend'))
print(json.dumps(stats))
