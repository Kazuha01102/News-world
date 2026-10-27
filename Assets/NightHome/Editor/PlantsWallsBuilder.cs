using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlantsWallsBuilder
{
    public const string Root="Assets/NightHome/PlantsWalls";
    static readonly HashSet<string> WallNames=new HashSet<string>(new[]{"West wall","East wall","South wall","North base","North lintel","North pier","TV wall","Living hall partition south","Living hall partition north","Bedroom west","Bedroom south right","Bedroom south left","Bedroom door lintel","Quiet room north","Quiet room south","Quiet room west N","Quiet room west S","Quiet door lintel"});
    static Material Material(string name,Color color,float smooth=.05f,string shader="Standard")
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static Texture2D Texture(string name,Func<int,int,Color> pixel)
    {
        var texture=new Texture2D(512,512,TextureFormat.RGB24,false);
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)texture.SetPixel(x,y,pixel(x,y));texture.Apply();
        string path=Root+"/Textures/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    public static void MakeMaterials()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Textures");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
        var leaf=Material("Leaf",new Color(.30f,.45f,.25f),.09f,"NightHome/Opaque Vertex Surface");
        leaf.mainTexture=Texture("LeafVeins",(x,y)=>{float center=Mathf.Exp(-Mathf.Pow((y-255.5f)/4,2));float vein=Mathf.Pow(Mathf.Max(0,Mathf.Cos((x+Mathf.Abs(y-255.5f)*1.3f)*.16f)),16)*.035f;float value=.89f+.045f*center+vein+.012f*Mathf.PerlinNoise(x*.08f,y*.08f);return new Color(value,value,value);});
        Material("Bark",new Color(.27f,.205f,.13f),.035f);Material("Ceramic",new Color(.76f,.725f,.66f),.13f);var soil=Material("Soil",new Color(.12f,.085f,.05f),.015f);
        soil.mainTexture=Texture("Soil",(x,y)=>{float v=.72f+.24f*Mathf.PerlinNoise(x*.4f,y*.4f);return new Color(v,v,v);});
        var wall=Material("Wallpaper",new Color(.84f,.815f,.77f),.015f);
        wall.mainTexture=Texture("QuietWallpaper",(x,y)=>{float value=.965f+.012f*Mathf.PerlinNoise(x*.31f,y*.31f)+.004f*Mathf.Sin(y*2.7f)*Mathf.Sin(x*2.4f);return new Color(value,value,value);});wall.mainTextureScale=new Vector2(4,4);
        Material("Trim",new Color(.76f,.725f,.66f),.08f);Material("Ceiling",new Color(.80f,.785f,.75f),.015f);
        var glass=Material("NightGlass",new Color(.018f,.025f,.035f,.13f),.15f);glass.SetFloat("_Mode",3);glass.SetOverrideTag("RenderType","Transparent");glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;
        AssetDatabase.SaveAssets();
    }
    public static GameObject Model(string model,Vector3 pos,Transform parent=null)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+model+".fbx");if(!source)throw new Exception("FBX missing "+model);
        var f=source.GetComponentInChildren<MeshFilter>();if(f.transform.localScale!=Vector3.one||Quaternion.Angle(f.transform.localRotation,Quaternion.identity)>.1f)throw new Exception("FBX transform not applied");
        var g=new GameObject(model,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent);g.transform.position=pos;g.GetComponent<MeshFilter>().sharedMesh=f.sharedMesh;
        var mats=new List<Material>();foreach(var m in f.GetComponent<Renderer>().sharedMaterials){var replacement=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+m.name+".mat");if(!replacement)throw new Exception("Material missing "+m.name);mats.Add(replacement);}g.GetComponent<Renderer>().sharedMaterials=mats.ToArray();
        GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);return g;
    }
    static void Prefab(GameObject g,string name)
    {
        var p=g.transform.position;var q=g.transform.rotation;g.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        PrefabUtility.SaveAsPrefabAssetAndConnect(g,Root+"/Prefabs/"+name+".prefab",InteractionMode.AutomatedAction);g.transform.SetPositionAndRotation(p,q);PrefabUtility.RecordPrefabInstancePropertyModifications(g.transform);
    }
    [MenuItem("Night Home/Plants and Walls/Apply geometry and materials")]
    public static void Apply()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/NightHome/NightHome_Blockout.unity"||EditorApplication.isPlaying)throw new Exception("Open saved home in edit mode");
        if(GameObject.Find("HOME - Plant and wall finish"))throw new Exception("Already applied; edit existing instances");
        MakeMaterials();
        foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{Root+"/Models"}))
        {var i=(ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));i.generateSecondaryUV=true;i.importCameras=false;i.importLights=false;i.importAnimation=false;i.importNormals=ModelImporterNormals.Import;i.SaveAndReimport();}
        var root=new GameObject("HOME - Plant and wall finish").transform;
        var originals=new List<Renderer>();
        foreach(var r in GameObject.Find("HOME - Architecture and furniture").GetComponentsInChildren<Renderer>())
        {
            if(WallNames.Contains(r.name)||r.name=="Window frame"||r.name=="Plant pot"||r.name=="Plant leaf cluster"){r.enabled=false;originals.Add(r);}
            if(r.name=="Ceiling")r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Ceiling.mat");
            if(r.name=="Night window")
            {
                float x=r.transform.position.x;
                if(x>-1&&x<1){r.enabled=false;continue;}
                r.transform.position=new Vector3(x<0?-7.5f:5.5f,1.95f,8.02f);r.transform.localScale=new Vector3(x<0?.94f:4.94f,1.80f,.018f);r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/NightGlass.mat");
            }
        }
        var wall=Model("WallShell",Vector3.zero,root);Prefab(wall,"WallShell");
        var trim=Model("ArchitecturalTrim",Vector3.zero,root);Prefab(trim,"ArchitecturalTrim");
        var infill=new GameObject("Closed former window behind TV controls");infill.transform.SetParent(root);infill.transform.position=new Vector3(0,1.95f,8);var c=infill.AddComponent<BoxCollider>();c.size=new Vector3(2,1.9f,.2f);
        var a=Model("BroadleafTall",new Vector3(-9,0,7),root);Prefab(a,"BroadleafTall");
        var b=Model("BroadleafCompact",new Vector3(.8f,0,7),root);Prefab(b,"BroadleafCompact");
        var d=Model("BroadleafTall",new Vector3(9,0,7),root);d.transform.rotation=Quaternion.Euler(0,180,0);Prefab(d,"BroadleafTallBedroom");
        // Entry door keeps its existing mesh shape, transform and collider. Give the
        // stretched primitive a real-metre UV projection with vertical wood grain.
        var door=GameObject.Find("Entry door");var mesh=UnityEngine.Object.Instantiate(door.GetComponent<MeshFilter>().sharedMesh);mesh.name="EntryDoorMetreUV";var uv=new Vector2[mesh.vertexCount];var vertices=mesh.vertices;var normals=mesh.normals;
        for(int i=0;i<uv.Length;i++){var p=Vector3.Scale(vertices[i],door.transform.lossyScale);uv[i]=Mathf.Abs(normals[i].z)>.5f?new Vector2(p.y,p.x):new Vector2(p.y,p.z);}mesh.uv=uv;AssetDatabase.CreateAsset(mesh,Root+"/EntryDoorMetreUV.asset");door.GetComponent<MeshFilter>().sharedMesh=mesh;door.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/NightHome/LivingFinish/Materials/Walnut.mat");
        Save();Debug.Log("[PlantsWalls] Geometry/material changes saved; light settings unchanged.");
    }
    public static void Save(){EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
}
