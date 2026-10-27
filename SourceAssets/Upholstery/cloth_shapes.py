"""Sewn rectangular upholstery, metres. No global scene mutation or simulation.
Foam cores use broad planar decks and narrow corner radii. Filled cases use a
thin rectangular flange, broad fill and a separate compressed contact region.
"""
import bpy,bmesh,math
from mathutils import Vector

def rectangle(width,depth,radius,steps=6):
    r=min(radius,width*.24,depth*.24);points=[]
    corners=[(width/2-r,depth/2-r,0),(-width/2+r,depth/2-r,90),(-width/2+r,-depth/2+r,180),(width/2-r,-depth/2+r,270)]
    for index,(x,y,start) in enumerate(corners):
        for i in range(steps+1):
            a=math.radians(start+i*90/steps);points.append((x+r*math.cos(a),y+r*math.sin(a)))
        nx,ny,angle=corners[(index+1)%4];angle=math.radians(angle)
        end=(nx+r*math.cos(angle),ny+r*math.sin(angle));begin=points[-1]
        for j in range(1,8):points.append((begin[0]+(end[0]-begin[0])*j/8,begin[1]+(end[1]-begin[1])*j/8))
    return points

def create(name,position,dimensions,tilt=(0,0,0),kind='seat'):
    vertical=kind in ('back','throw')
    width=dimensions[0];depth=dimensions[2] if vertical else dimensions[1]
    thick=dimensions[1] if vertical else dimensions[2]
    if kind=='back':depth*=.965;thick*=.84
    vertices=[];faces=[]
    def map_point(x,y,t):
        if kind=='back':
            v=y/(depth/2);u=x/(width/2)
            x*=1-.012*max(0,-v)**2
            y-=.004*(1-u*u)*max(0,1-v*v)
            if t>0:t-=.008*math.exp(-((v+.70)/.14)**2)*max(0,1-u*u)
        return (x,-t,y) if vertical else (x,y,t)
    def ring(w,d,r,t):
        indices=[]
        for x,y in rectangle(w,d,r):
            indices.append(len(vertices));vertices.append(map_point(x,y,t(x,y) if callable(t) else t))
        return indices
    rings=[]
    if kind in ('pillow','throw','floor'):
        # A regular fabric grid avoids diagonal fan-shaped shading across the fill.
        us=sorted(set([i/8 for i in range(-8,9)]+[-.985,.985]))
        vs=sorted(set([i/6 for i in range(-6,7)]+[-.985,.985]))
        nx=len(us);ny=len(vs)
        for front in (False,True):
            for u in us:
                for v in vs:
                    x=u*width/2;y=v*depth/2
                    r=.008;ax=abs(x);ay=abs(y)
                    if ax>width/2-r and ay>depth/2-r:
                        dx=ax-(width/2-r);dy=ay-(depth/2-r);length=math.hypot(dx,dy)
                        if length>r:x=math.copysign(width/2-r+dx*r/length,x);y=math.copysign(depth/2-r+dy*r/length,y)
                    fill=max(0,(1-abs(u)**4)*(1-abs(v)**4))
                    seam=-thick*(.16+.06*abs(u)*abs(v))
                    t=seam+((thick/2 if front else -thick/2)-seam)*fill**(.65 if front else .42)
                    if abs(u)==1 or abs(v)==1:t=seam+(.001 if front else -.001)
                    vertices.append(map_point(x,y,t))
        count=nx*ny
        for k in range(2):
            for i in range(nx-1):
                for j in range(ny-1):
                    a=k*count+i*ny+j;face=(a,a+ny,a+ny+1,a+1)
                    faces.append(face if k else tuple(reversed(face)))
        edge=list(range(ny))+[i*ny+ny-1 for i in range(1,nx)]+list(range(count-2,count-ny-1,-1))+[i*ny for i in range(nx-2,0,-1)]
        for a,b in zip(edge,edge[1:]+edge[:1]):faces.append((a,b,b+count,a+count))
    else:
        bevel=.012 if kind=='mattress' else min(.018,thick*.16)
        radius=.014 if kind=='mattress' else .028
        profiles=[(width-.014,depth-.014,-thick/2),(width,depth,-thick/2+bevel),
                  (width,depth,-.0015),(width-.0015,depth-.0015,0),
                  (width,depth,.0015),(width,depth,thick/2-bevel),
                  (width-.014,depth-.014,thick/2-.003),(width-.034,depth-.034,thick/2)]
        if kind=='mattress':profiles=[profiles[0],profiles[1],(width,depth,thick/2-bevel),profiles[-2],profiles[-1]]
        for w,d,t in profiles:rings.append(ring(w,d,radius,t))
        for scale in (.82,.48):
            dish=0 if kind in ('mattress','arm') else (.001 if scale>.5 else .003)
            rings.append(ring((width-.034)*scale,(depth-.034)*scale,radius,thick/2-dish))
        for a,b in zip(rings,rings[1:]):
            for j in range(len(a)):faces.append((a[j],a[(j+1)%len(a)],b[(j+1)%len(a)],b[j]))
        faces.append(tuple(reversed(rings[0])))
        center=len(vertices);vertices.append(map_point(0,0,thick/2-(0 if kind in ('mattress','arm') else .003)))
        for j in range(len(rings[-1])):faces.append((center,rings[-1][j],rings[-1][(j+1)%len(rings[-1])]))
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(obj);obj.location=position;obj.rotation_euler=tilt
    bpy.context.view_layer.update()
    # Compress against an actual support plane, without shifting object origins.
    contact=.662 if kind=='pillow' else (.468 if kind=='throw' else (0 if kind=='floor' else None))
    if contact is not None:
        transform=obj.matrix_world.copy();inverse=transform.inverted()
        for v in data.vertices:
            world=transform@v.co
            if world.z<contact:world.z=contact;v.co=inverse@world
    bm=bmesh.new();bm.from_mesh(data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    for f in data.polygons:f.use_smooth=True
    if kind not in ('pillow','throw','floor'):
        weighted=obj.modifiers.new('Flat panel weighted normals','WEIGHTED_NORMAL');weighted.keep_sharp=True
    return obj

def welt(name,width,depth,height,cx,cy):
    # A 1mm flat stitch tape sits over a submillimetre channel. No tubular curve.
    points=rectangle(width-.0006,depth-.0006,.028);vertices=[];faces=[]
    for x,y in points:vertices.extend([(x+cx,y+cy,height-.0005),(x+cx,y+cy,height+.0005)])
    for i in range(len(points)):a=2*i;b=2*((i+1)%len(points));faces.append((a,b,b+1,a+1))
    data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);data.update()
    obj=bpy.data.objects.new(name,data);bpy.context.scene.collection.objects.link(obj)
    # Face outward; these are flush opaque ribbons, not overlapping alpha cards.
    for f in data.polygons:f.use_smooth=True
    return obj
