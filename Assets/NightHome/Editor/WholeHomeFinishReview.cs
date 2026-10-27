using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WholeHomeFinishReview
{
    [MenuItem("Night Home/Whole Home/Validate preserved functions and paths")]
    public static void ValidateFromMenu(){Debug.Log(Validate());}
    public static string Validate()
    {
        var report=new List<string>();string backup=SessionState.GetString("WholeFinish.Backup","Backups/WholeFinish_20261027_140030");
        var before=File.ReadAllLines(backup+"/udon_before.txt");var current=new List<string>();
        foreach(var u in UnityEngine.Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>(true))
        {var row=GlobalObjectId.GetGlobalObjectIdSlow(u)+"|"+u.programSource+"|";foreach(var symbol in u.publicVariables.VariableSymbols){object value;if(u.publicVariables.TryGetVariableValue(symbol,out value)&&value is UnityEngine.Object)row+=symbol+"="+GlobalObjectId.GetGlobalObjectIdSlow((UnityEngine.Object)value)+";";}current.Add(row);}
        Array.Sort(before);var after=current.ToArray();Array.Sort(after);bool equal=string.Join("\n",before)==string.Join("\n",after);report.Add("Udon="+current.Count+"; reference fingerprint identical="+equal);if(!equal)throw new Exception("Udon references changed");
        int missing=0;foreach(var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);report.Add("Missing scripts="+missing);if(missing>0)throw new Exception("Missing scripts");
        var descriptor=UnityEngine.Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();report.Add("Capacity="+descriptor.capacity+"; spawn="+descriptor.spawns[0].position+" yaw="+descriptor.spawns[0].eulerAngles.y);
        if(descriptor.capacity!=16||Vector3.Distance(descriptor.spawns[0].position,new Vector3(-1,.08f,-6.1f))>.001f)throw new Exception("Spawn changed");
        var zone=GameObject.Find("HOME - Quiet room audio boundary").GetComponent<BoxCollider>();report.Add("Room audio boundary="+zone.transform.position+" size="+zone.size+" trigger="+zone.isTrigger);
        if(zone.transform.position!=new Vector3(7,1.6f,-3)||zone.size!=new Vector3(6,3.8f,6)||!zone.isTrigger)throw new Exception("Audio boundary changed");
        var archived=new HashSet<int>();foreach(var t in Resources.FindObjectsOfTypeAll<Transform>())if(t.gameObject.scene.IsValid()&&t.name=="HOME - Archived remaining blockout (disabled)")foreach(var c in t.GetComponentsInChildren<Collider>(true))archived.Add(c.GetInstanceID());
        report.Add("Old furniture colliders disabled by archive="+archived.Count);
        int newColliders=0,renderers=0,triangles=0,slots=0;foreach(var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
        {var f=r.GetComponent<MeshFilter>();if(!f||!r.enabled)continue;string path=AssetDatabase.GetAssetPath(f.sharedMesh);if(!path.StartsWith("Assets/NightHome/WholeFinish/Models/"))continue;renderers++;triangles+=f.sharedMesh.triangles.Length/3;slots+=r.sharedMaterials.Length;newColliders+=r.GetComponents<Collider>().Length;report.Add(r.name+": "+f.sharedMesh.triangles.Length/3+" tris, "+r.sharedMaterials.Length+" material slots, "+r.bounds);}
        report.Add("New FBX renderer instances="+renderers+"; tris="+triangles+"; material slots="+slots+"; new box colliders="+newColliders);
        Physics.SyncTransforms();
        var routes=new[]{new[]{new Vector2(-1,-6.1f),new Vector2(-1,-3.8f),new Vector2(1,-2),new Vector2(3,-2),new Vector2(3,1),new Vector2(3.1f,2.8f),new Vector2(4.5f,3.3f),new Vector2(7.6f,3.3f)},new[]{new Vector2(3,-2),new Vector2(3,-3),new Vector2(5.7f,-3),new Vector2(5.7f,-1),new Vector2(7,-1)},new[]{new Vector2(-1,-3.8f),new Vector2(-7,-3.8f),new Vector2(-8.6f,-2.5f),new Vector2(-8.6f,0),new Vector2(-8.5f,1.1f),new Vector2(-8.5f,4.9f)}};
        int hits=0;for(int route=0;route<routes.Length;route++)for(int i=1;i<routes[route].Length;i++)
        {var from=routes[route][i-1];var to=routes[route][i];int steps=Mathf.CeilToInt(Vector2.Distance(from,to)/.10f);for(int j=0;j<=steps;j++){var p=Vector2.Lerp(from,to,j/(float)steps);var collisions=Physics.OverlapCapsule(new Vector3(p.x,.30f,p.y),new Vector3(p.x,1.50f,p.y),.22f,~0,QueryTriggerInteraction.Ignore);if(collisions.Length>0){hits++;if(hits<12)report.Add("Route "+route+" obstacle="+collisions[0].name+" at "+p);}}}
        report.Add("Sampled routes: radius .22m, height range .08..1.72m; blocked samples="+hits);
        var switchCollider=GameObject.Find("Mirror Use switch").GetComponent<Collider>();RaycastHit hit;bool target=switchCollider.Raycast(new Ray(new Vector3(-8,1.1f,1.1f),Vector3.left),out hit,3);report.Add("Existing mirror Use collider reachable="+target);
        int realtime=0,shadow=0;foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())if(l.enabled&&l.lightmapBakeType!=LightmapBakeType.Baked){realtime++;if(l.shadows!=LightShadows.None)shadow++;}report.Add("Active realtime lights="+realtime+"; shadow lights="+shadow+"; baked GI="+Lightmapping.lightingSettings.bakedGI);
        foreach(var g in Resources.FindObjectsOfTypeAll<GameObject>())if(g.scene.IsValid()&&g.name=="HOME - Local mirror (default OFF)"){report.Add("Mirror initial active="+g.activeSelf);if(g.activeSelf)throw new Exception("Mirror should be off");}
        Directory.CreateDirectory("Documentation/WholeFinish");File.WriteAllLines("Documentation/WholeFinish/Validation.txt",report);File.WriteAllLines("Documentation/WholeFinish/UdonRefsAfter.txt",after);
        if(hits>0)throw new Exception("Sampled route has obstacles; see Validation.txt");return string.Join("\n",report);
    }
}
