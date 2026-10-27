using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class LivingFinishBuilder
{
    const string Root="Assets/NightHome/LivingFinish";
    static Material[] palette;
    [MenuItem("Night Home/Living Finish/1 Sofa and table")]
    public static void PhaseOne()
    {
        CheckScene();if(GameObject.Find("LIVING - Finished furniture"))throw new Exception("Already applied; edit existing furniture instead.");
        Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Textures");AssetDatabase.Refresh();
        MakeMaterials();
        var finished=new GameObject("LIVING - Finished furniture").transform;
        var archive=new GameObject("LIVING - Original blockout (disabled)");archive.SetActive(false);
        foreach(string n in new[]{"Main sofa","Window sofa","Side sofa","Coffee table top"}){var g=GameObject.Find(n);if(g)g.transform.SetParent(archive.transform,true);}
        foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())if(g.name=="Coffee table leg")g.transform.SetParent(archive.transform,true);
        Sofa(finished,"Main sofa - 3.20m","Sofa320",new Vector3(-4,0,1.8f),0,3.2f);
        Sofa(finished,"Window sofa - 3.00m","Sofa300",new Vector3(-7.1f,0,4),90,3f);
        Sofa(finished,"Side sofa - 3.00m","Sofa300",new Vector3(-.9f,0,4),270,3f);
        var table=Model(finished,"Coffee table - 2.10m","CoffeeTable210",new Vector3(-4,0,4),0);
        BoxCollider(table,new Vector3(0,.435f,0),new Vector3(2.1f,.05f,1.05f));
        foreach(float x in new[]{-.79f,.79f})foreach(float z in new[]{-.32f,.32f})BoxCollider(table,new Vector3(x,.2075f,z),new Vector3(.09f,.415f,.09f));
        foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())if(g.name=="Mug"&&g.transform.position.z>3&&g.transform.position.z<5)g.transform.position+=Vector3.down*.02f;
        Save();Debug.Log("[LivingFinish] Sofa/table meshes replaced; lighting unchanged for comparison.");
    }
    [MenuItem("Night Home/Living Finish/2 Rug console and lighting")]
    public static void PhaseTwo()
    {
        CheckScene();LoadPalette();var root=GameObject.Find("LIVING - Finished furniture").transform;
        if(GameObject.Find("TV console - 4.10m"))throw new Exception("Second stage already applied");
        var archive=FindInactive("LIVING - Original blockout (disabled)").transform;
        foreach(string n in new[]{"Living rug","TV console"}){var g=GameObject.Find(n);if(g)g.transform.SetParent(archive,true);}
        Model(root,"Living rug - bound edge","Rug740",new Vector3(-4,.002f,3.35f),0);
        var cabinet=Model(root,"TV console - 4.10m","TVConsole410",new Vector3(-5,0,7.25f),0);
        BoxCollider(cabinet,new Vector3(0,.36f,0),new Vector3(4.13f,.64f,.63f));
        foreach(var go in UnityEngine.Object.FindObjectsOfType<GameObject>())if(go.name=="Ceiling lamp" && go.transform.position.x<1 && go.transform.position.z>1)go.transform.SetParent(archive,true);
        var pendant=Model(root,"Living pendant - linen shade","Pendant120",new Vector3(-4,3.38f,3.7f),0);
        var warmWhite=Mat("Living warm plaster",new Color(.78f,.745f,.68f),.1f,0);
        foreach(var r in GameObject.Find("HOME - Architecture and furniture").GetComponentsInChildren<Renderer>())
        {if(r.name=="West wall"||r.name=="TV wall"||r.name=="North base"||r.name=="North lintel"||(r.name=="North pier"&&r.transform.position.x<3))r.sharedMaterial=warmWhite;}
        // Living lights are baked, with zero real-time shadow casting.
        foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())if(l.type==LightType.Point && l.transform.position.x<0 && l.transform.position.z>-5)
        {l.lightmapBakeType=LightmapBakeType.Baked;l.shadows=LightShadows.Soft;l.intensity=1.7f;l.color=new Color(1,.83f,.65f);}
        var area=new GameObject("Living softbox - baked only");area.transform.SetParent(root);area.transform.position=new Vector3(-4,3.02f,3.7f);area.transform.rotation=Quaternion.Euler(90,0,0);var a=area.AddComponent<Light>();a.type=LightType.Area;a.areaSize=new Vector2(1.3f,.6f);a.lightmapBakeType=LightmapBakeType.Baked;a.color=new Color(1,.84f,.68f);a.intensity=4;
        var settings=new LightingSettings();settings.name="NightHome Living Lighting";settings.bakedGI=true;settings.realtimeGI=false;
        settings.lightmapper=LightingSettings.Lightmapper.ProgressiveCPU;settings.lightmapResolution=12;settings.lightmapMaxSize=1024;settings.lightmapPadding=3;settings.directSampleCount=16;settings.indirectSampleCount=32;settings.environmentSampleCount=16;settings.lightmapCompression=LightmapCompression.NormalQuality;
        AssetDatabase.CreateAsset(settings,Root+"/LivingLighting.lighting");Lightmapping.lightingSettings=settings;
        var probes=new GameObject("Living light probes");probes.transform.SetParent(root);var pg=probes.AddComponent<LightProbeGroup>();var pos=new List<Vector3>();foreach(float x in new[]{-8f,-6f,-4f,-2f,0})foreach(float z in new[]{0f,2f,4f,6f})foreach(float y in new[]{.7f,1.6f})pos.Add(new Vector3(x,y,z));pg.probePositions=pos.ToArray();
        Save();Debug.Log("[LivingFinish] Rug, console, plaster and baked-only living lights ready. Bake required.");
    }
    static void Sofa(Transform root,string name,string mesh,Vector3 position,float yaw,float width)
    {
        var g=Model(root,name,mesh,position,yaw);
        BoxCollider(g,new Vector3(0,.31f,0),new Vector3(width-.14f,.40f,1.02f));
        BoxCollider(g,new Vector3(0,.64f,-.37f),new Vector3(width-.15f,.44f,.25f));
        foreach(float x in new[]{-width/2+.075f,width/2-.075f})BoxCollider(g,new Vector3(x,.45f,0),new Vector3(.15f,.43f,1.04f));
        var seats=new GameObject("Seat plane 0.47m - layout markers");seats.transform.SetParent(g.transform,false);
        for(int i=0;i<3;i++){var marker=new GameObject("Seat "+(i+1));marker.transform.SetParent(seats.transform,false);marker.transform.localPosition=new Vector3((i-1)*(width-.35f)/3,.47f,.095f);}
    }
    static GameObject Model(Transform root,string name,string model,Vector3 pos,float yaw)
    {
        if(palette==null)LoadPalette();
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+model+".fbx");
        if(!asset)throw new Exception("Missing FBX: "+model);
        var source=asset.GetComponentInChildren<MeshFilter>();var mesh=source.sharedMesh;
        var used=new List<Material>();
        foreach(var material in source.GetComponent<Renderer>().sharedMaterials)
        {var replacement=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+material.name+".mat");if(!replacement)throw new Exception("Missing material "+material.name);used.Add(replacement);}
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(root);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterials=used.ToArray();GameObjectUtility.SetStaticEditorFlags(g,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);return g;
    }
    [MenuItem("Night Home/Living Finish/3 Import FBX and update prefabs")]
    public static void UpgradeFbx()
    {
        CheckScene();Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();MakeMaterials();
        foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{Root+"/Models"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.generateSecondaryUV=true;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;
            importer.importNormals=ModelImporterNormals.Import;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }
        string[] names={"Main sofa - 3.20m","Window sofa - 3.00m","Side sofa - 3.00m","Coffee table - 2.10m","Living rug - bound edge","TV console - 4.10m","Living pendant - linen shade"};
        string[] models={"Sofa320","Sofa300","Sofa300","CoffeeTable210","Rug740","TVConsole410","Pendant120"};
        for(int i=0;i<names.Length;i++)
        {
            var go=GameObject.Find(names[i]);if(!go)throw new Exception(names[i]+" missing");
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+models[i]+".fbx");var source=asset.GetComponentInChildren<MeshFilter>();
            if(source.transform.localScale!=Vector3.one||Quaternion.Angle(source.transform.localRotation,Quaternion.identity)>.1f)throw new Exception("Non-applied FBX transform");
            go.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;var mats=new List<Material>();
            foreach(var m in source.GetComponent<Renderer>().sharedMaterials)mats.Add(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+m.name+".mat"));
            go.GetComponent<Renderer>().sharedMaterials=mats.ToArray();
            var pos=go.transform.position;var rotation=go.transform.rotation;
            go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;
            // Separate scene instances can share a model while retaining their placement.
            PrefabUtility.SaveAsPrefabAssetAndConnect(go,Root+"/Prefabs/"+names[i].Replace(" - ","_")+".prefab",InteractionMode.AutomatedAction);
            go.transform.SetPositionAndRotation(pos,rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        UseRealtimeFallback();Save();Debug.Log("[LivingFinish] All furniture uses FBX meshes and prefabs; existing colliders and seat markers retained.");
    }
    [MenuItem("Night Home/Living Finish/Use realtime lighting fallback")]
    public static void UseRealtimeFallback()
    {
        CheckScene();if(Lightmapping.isRunning)Lightmapping.Cancel();
        foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())
        {
            if(l.type==LightType.Point && l.lightmapBakeType==LightmapBakeType.Baked){l.lightmapBakeType=LightmapBakeType.Realtime;l.shadows=LightShadows.None;l.intensity=1.6f;}
            if(l.name=="Living softbox - baked only")l.enabled=false;
        }
        if(Lightmapping.lightingSettings)Lightmapping.lightingSettings.bakedGI=false;
        var root=GameObject.Find("LIVING - Finished furniture");var table=GameObject.Find("Coffee table - 2.10m");float delta=.029f-table.transform.position.y;
        foreach(Transform t in root.transform)if(t.name.Contains("sofa")||t.name.StartsWith("Coffee table")){var p=t.position;p.y=.029f;t.position=p;PrefabUtility.RecordPrefabInstancePropertyModifications(t);}
        foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())if(g.name=="Mug"&&g.transform.position.z>3&&g.transform.position.z<5)g.transform.position+=Vector3.up*delta;
        var cabinet=GameObject.Find("TV console - 4.10m");cabinet.transform.rotation=Quaternion.Euler(0,180,0);PrefabUtility.RecordPrefabInstancePropertyModifications(cabinet.transform);
        Save();
    }
    static void BoxCollider(GameObject go,Vector3 center,Vector3 size){var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static GameObject FindInactive(string name){foreach(var g in Resources.FindObjectsOfTypeAll<GameObject>())if(g.scene.IsValid()&&g.name==name)return g;throw new Exception(name+" missing");}
    static void CheckScene(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/NightHome/NightHome_Blockout.unity"||EditorApplication.isPlaying)throw new Exception("Open home scene in edit mode");}
    static void Save(){EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
    static Material Mat(string name,Color color,float smooth,float metal)
    {var path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Standard"));m.name=name;m.color=color;m.SetFloat("_Glossiness",smooth);m.SetFloat("_Metallic",metal);AssetDatabase.CreateAsset(m,path);return m;}
    static void LoadPalette(){palette=new Material[7];string[] names={"GrayFabric","SeamGray","Walnut","Metal","RugWeave","RugEdge","FrostedShade"};for(int i=0;i<7;i++)palette[i]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+names[i]+".mat");}
    static void MakeMaterials()
    {
        palette=new[]{Mat("GrayFabric",new Color(.54f,.55f,.54f),.08f,0),Mat("SeamGray",new Color(.47f,.48f,.47f),.06f,0),Mat("Walnut",new Color(.51f,.35f,.21f),.3f,0),Mat("Metal",new Color(.11f,.115f,.11f),.58f,.8f),Mat("RugWeave",new Color(.72f,.68f,.59f),.04f,0),Mat("RugEdge",new Color(.48f,.45f,.39f),.05f,0),Mat("FrostedShade",new Color(.93f,.85f,.7f),.12f,0)};
        Texture2D fabric=new Texture2D(512,512),normal=new Texture2D(512,512),wood=new Texture2D(512,512);
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float a=Mathf.Sin(x*Mathf.PI*.5f),b=Mathf.Sin(y*Mathf.PI*.5f),noise=Mathf.PerlinNoise(x*.27f,y*.27f);
            float value=.83f+.06f*a+.05f*b+.04f*noise;fabric.SetPixel(x,y,new Color(value,value,value,1));
            normal.SetPixel(x,y,new Color(.5f+.10f*Mathf.Cos(x*Mathf.PI*.5f),.5f+.10f*Mathf.Cos(y*Mathf.PI*.5f),1,1));
            float w=.86f+.018f*Mathf.Sin(y*.64f+Mathf.Sin(x*.012f)*1.1f)+.012f*Mathf.Sin(y*1.9f)+.025f*Mathf.PerlinNoise(x*.012f,y*.32f);wood.SetPixel(x,y,new Color(w,w*.98f,w*.93f,1));
        }
        Texture2D f=WriteTexture(fabric,"WovenFabric",false),n=WriteTexture(normal,"WovenNormal",true),wtex=WriteTexture(wood,"OakGrain",false);
        foreach(int i in new[]{0,4,5}){palette[i].mainTexture=f;palette[i].mainTextureScale=new Vector2(4,4);palette[i].SetTexture("_BumpMap",n);palette[i].SetTextureScale("_BumpMap",new Vector2(4,4));palette[i].SetFloat("_BumpScale",.24f);palette[i].EnableKeyword("_NORMALMAP");}
        palette[2].mainTexture=wtex;palette[2].mainTextureScale=new Vector2(1,1);
        palette[6].EnableKeyword("_EMISSION");palette[6].SetColor("_EmissionColor",new Color(1,.84f,.62f)*1.1f);palette[6].globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
        foreach(var m in palette)EditorUtility.SetDirty(m);AssetDatabase.SaveAssets();
    }
    static Texture2D WriteTexture(Texture2D t,string name,bool normal)
    {t.Apply();string path=Root+"/Textures/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());AssetDatabase.ImportAsset(path);var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;i.wrapMode=TextureWrapMode.Repeat;i.maxTextureSize=512;i.mipmapEnabled=true;i.SaveAndReimport();UnityEngine.Object.DestroyImmediate(t);return AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
}
