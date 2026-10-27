using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WholeHomeFinishBuilder
{
    const string Root="Assets/NightHome/WholeFinish";
    const string Living="Assets/NightHome/LivingFinish";
    static void Check(){if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/NightHome/NightHome_Blockout.unity")throw new Exception("Open home in edit mode");}
    static Material Resolve(string name)
    {
        foreach(var path in new[]{Root,Living,"Assets/NightHome/PlantsWalls","Assets/NightHome"}){var m=AssetDatabase.LoadAssetAtPath<Material>(path+"/Materials/"+name+".mat");if(m)return m;}
        throw new Exception("Missing material "+name);
    }
    [MenuItem("Night Home/Whole Home/Prepare model library")]
    public static void Prepare()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
        string[] names={"BedLinen","BookBlue","BookClay","BookPages","TeaBrown"};
        Color[] colors={new Color(.76f,.74f,.68f),new Color(.28f,.36f,.40f),new Color(.48f,.32f,.24f),new Color(.76f,.73f,.65f),new Color(.10f,.055f,.026f)};
        for(int i=0;i<names.Length;i++)
        {
            string path=Root+"/Materials/"+names[i]+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=i==0?new Material(Resolve("GrayFabric")):new Material(Shader.Find("Standard"));m.name=names[i];AssetDatabase.CreateAsset(m,path);}
            m.color=colors[i];m.SetFloat("_Glossiness",i==4?.35f:.06f);EditorUtility.SetDirty(m);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:Model",new[]{Root+"/Models"}))
        {var importer=(ModelImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.generateSecondaryUV=true;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.importNormals=ModelImporterNormals.Import;importer.SaveAndReimport();}
        AssetDatabase.SaveAssets();
    }
    static Transform RootTransform(){var g=GameObject.Find("HOME - Whole interior finish");return g?g.transform:new GameObject("HOME - Whole interior finish").transform;}
    static Transform Archive()
    {foreach(var g in Resources.FindObjectsOfTypeAll<GameObject>())if(g.scene.IsValid()&&g.name=="HOME - Archived remaining blockout (disabled)")return g.transform;var a=new GameObject("HOME - Archived remaining blockout (disabled)");a.SetActive(false);return a.transform;}
    static void Retire(GameObject g){var archive=Archive();if(!g.transform.IsChildOf(archive))g.transform.SetParent(archive,true);}
    static GameObject Model(string model,Vector3 position,float yaw=0,Transform parent=null)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+model+".fbx");if(!source)throw new Exception("Missing FBX "+model);
        var f=source.GetComponentInChildren<MeshFilter>();if(f.transform.localScale!=Vector3.one||Quaternion.Angle(f.transform.localRotation,Quaternion.identity)>.1f)throw new Exception("FBX has unapplied transform");
        var go=new GameObject(model,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent?parent:RootTransform());go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));go.GetComponent<MeshFilter>().sharedMesh=f.sharedMesh;
        var mats=new List<Material>();foreach(var m in f.GetComponent<Renderer>().sharedMaterials)mats.Add(Resolve(m.name));go.GetComponent<MeshRenderer>().sharedMaterials=mats.ToArray();
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);return go;
    }
    static void Box(GameObject go,Vector3 center,Vector3 size){var c=go.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static void Prefab(GameObject go,string name=null)
    {
        var p=go.transform.position;var q=go.transform.rotation;go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        PrefabUtility.SaveAsPrefabAssetAndConnect(go,Root+"/Prefabs/"+(name??go.name)+".prefab",InteractionMode.AutomatedAction);go.transform.SetPositionAndRotation(p,q);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
    }
    static void Sofa(string name,Vector3 position,float yaw,float width,int seats)
    {
        var go=Model(name,position,yaw);Box(go,new Vector3(0,.29f,0),new Vector3(width-.14f,.36f,1.02f));Box(go,new Vector3(0,.64f,-.37f),new Vector3(width-.15f,.44f,.25f));
        foreach(float x in new[]{-width/2+.075f,width/2-.075f})Box(go,new Vector3(x,.45f,0),new Vector3(.15f,.43f,1.04f));
        for(int i=0;i<seats;i++){var marker=new GameObject("Seat marker "+(i+1));marker.transform.SetParent(go.transform,false);marker.transform.localPosition=new Vector3((i-(seats-1)*.5f)*(width-.35f)/seats,.47f,.095f);}
        Prefab(go);
    }
    [MenuItem("Night Home/Whole Home/1 Finish bedroom and quiet room")]
    public static void BedroomAndQuiet()
    {
        Check();if(GameObject.Find("BedroomBed"))throw new Exception("Bedroom already applied");Prepare();
        foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {
            string n=g.name;
            if(n=="Bed base"||n=="Mattress"||n=="Bed cover"||n=="Pillow"||n=="Headboard"||n=="Bedroom rug"||n.StartsWith("Bedside table")||n=="Quiet sofa south"||n=="Quiet sofa east"||n.StartsWith("Quiet table")||n=="Quiet rug")Retire(g);
        }
        var bed=Model("BedroomBed",new Vector3(6,.029f,5.5f));Box(bed,new Vector3(0,.33f,0),new Vector3(1.96f,.66f,2.26f));Box(bed,new Vector3(0,.68f,1.15f),new Vector3(2.04f,1.16f,.10f));Prefab(bed);
        var table=Model("BedsideTable",new Vector3(7.55f,.029f,6.1f));Box(table,new Vector3(0,.30f,0),new Vector3(.60f,.60f,.50f));Prefab(table);
        Prefab(Model("BedsideLamp",new Vector3(7.55f,.629f,6.1f)));
        Prefab(Model("BedroomCurtains",new Vector3(5.5f,0,7.52f)));
        Prefab(Model("BedroomRug",new Vector3(6,.002f,4.8f)));
        Sofa("QuietSofa300",new Vector3(7,.029f,-5),0,3,3);
        Sofa("QuietSofa240",new Vector3(9,.029f,-2.2f),270,2.4f,2);
        var qt=Model("QuietTable150",new Vector3(7,.029f,-3));Box(qt,new Vector3(0,.455f,0),new Vector3(1.5f,.05f,.9f));foreach(float x in new[]{-.57f,.57f})foreach(float z in new[]{-.29f,.29f})Box(qt,new Vector3(x,.22f,z),new Vector3(.075f,.44f,.075f));Prefab(qt);
        Prefab(Model("QuietRug",new Vector3(7,.002f,-3)));
        Prefab(Model("FloorCushion",new Vector3(7.7f,.029f,-1.1f),15));
        Save();Debug.Log("[WholeFinish] Bedroom and quiet-room meshes/prefabs placed; room audio boundary and all Udon objects retained.");
    }
    static void Label(string name,string text,Vector3 position,Vector2 metres,float yaw,int size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Canvas));go.transform.SetParent(RootTransform());go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;go.GetComponent<RectTransform>().sizeDelta=metres*400;go.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*.0025f;
        var child=new GameObject("Text",typeof(RectTransform),typeof(Text));child.transform.SetParent(go.transform,false);child.GetComponent<RectTransform>().sizeDelta=metres*400;var t=child.GetComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/NightHome/Fonts/ZenKakuGothicNew-Regular.ttf");t.text=text;t.color=new Color(.9f,.87f,.80f);t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
    }
    [MenuItem("Night Home/Whole Home/2 Finish entry and details")]
    public static void EntryAndDetails()
    {
        Check();if(GameObject.Find("EntryStorage"))throw new Exception("Details already applied");
        foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())
        {string n=g.name;if(n=="Entry mat"||n=="Bookcase side"||n=="Book"||n=="Mug"||n.StartsWith("QvPen return stand"))Retire(g);}
        var storage=Model("EntryStorage",new Vector3(-8.7f,0,-7.45f));Box(storage,new Vector3(0,.44f,0),new Vector3(1.73f,.88f,.45f));Prefab(storage);
        Prefab(Model("EntryMat",new Vector3(-1,.002f,-6.9f)));
        GameObject.Find("Entry door").GetComponent<Renderer>().enabled=false;GameObject.Find("Door handle").GetComponent<Renderer>().enabled=false;Prefab(Model("EntryDoor",new Vector3(-1,0,-7.88f)));
        var shelf=Model("Bookcase",new Vector3(-9.55f,0,3.2f),90);Box(shelf,new Vector3(0,.94f,0),new Vector3(1.80f,1.88f,.38f));Prefab(shelf);
        Prefab(Model("Mug",new Vector3(-4.5f,.489f,4),15));Prefab(Model("Mug",new Vector3(-3.4f,.489f,4.2f),-25),"Mug");
        var desk=Model("PenReturnDesk",new Vector3(-7,0,-1.8f));Box(desk,new Vector3(0,.835f,0),new Vector3(2.2f,.07f,.60f));foreach(float x in new[]{-.91f,.91f})foreach(float z in new[]{-.21f,.21f})Box(desk,new Vector3(x,.40f,z),new Vector3(.07f,.80f,.07f));Prefab(desk);
        var drawing=GameObject.Find("Drawing board");drawing.GetComponent<Renderer>().sharedMaterial=Resolve("Ceramic");Prefab(Model("DrawingFrame",new Vector3(-9.72f,1.6f,-3.7f),90));
        var switchGo=GameObject.Find("Mirror Use switch");switchGo.GetComponent<Renderer>().enabled=false;var bezel=Model("MirrorBezel",switchGo.transform.position);Prefab(bezel);bezel.transform.SetParent(switchGo.transform,true);PrefabUtility.RecordPrefabInstancePropertyModifications(bezel.transform);
        Prefab(Model("LogFrame",new Vector3(-3.4f,1.8f,-7.79f)));
        Label("Entry log header","入退室ログ  /  JST 日本時間",new Vector3(-3.4f,3.13f,-7.72f),new Vector2(3.3f,.25f),180,27);
        Label("Quiet room title","個室",new Vector3(3.84f,2.80f,-3),new Vector2(1.50f,.30f),90,34);
        Label("Pen return desk label","QvPen 返却台",new Vector3(-7,.75f,-2.13f),new Vector2(1.50f,.20f),0,26);
        // Replace remaining floating blockout ceiling cubes with flush FBX fixtures.
        int index=0;foreach(var g in UnityEngine.Object.FindObjectsOfType<GameObject>())if(g.name=="Ceiling lamp")
        {var p=g.transform.position;Retire(g);Prefab(Model("CeilingDisc",new Vector3(p.x,3.4f,p.z)),"CeilingDisc");index++;}
        // Reuse the existing bedroom light at the bedside. No new lights or shadows.
        foreach(var light in UnityEngine.Object.FindObjectsOfType<Light>())if(light.type==LightType.Point&&Vector3.Distance(light.transform.position,new Vector3(6,3,5))<.1f)
        {light.transform.position=new Vector3(7.55f,1.03f,6.1f);light.intensity=1.10f;light.range=6;light.color=new Color(1,.82f,.64f);}
        Save();Debug.Log("[WholeFinish] Entry, bookshelf, mugs, desk, mirror bezel and flush ceiling fixtures complete; new realtime lights=0.");
    }
    public static void Save(){EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
    [MenuItem("Night Home/Whole Home/3 Review corrections and material unification")]
    public static void ReviewAdjustments()
    {
        Check();GameObject.Find("BedroomCurtains").transform.position=new Vector3(5.5f,0,7.52f);PrefabUtility.RecordPrefabInstancePropertyModifications(GameObject.Find("BedroomCurtains").transform);
        if(!GameObject.Find("DrawingRug")){Retire(GameObject.Find("Drawing area rug"));Prefab(Model("DrawingRug",new Vector3(-7,.002f,-3.8f)));}
        var penSign=GameObject.Find("Pen area sign");penSign.transform.SetPositionAndRotation(new Vector3(-9.70f,2.78f,-3.7f),Quaternion.Euler(0,-90,0));penSign.GetComponent<RectTransform>().sizeDelta=new Vector2(880,120);penSign.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(880,120);
        var mirrorSwitch=GameObject.Find("Mirror Use switch");mirrorSwitch.transform.position=new Vector3(-9.82f,1.1f,1.1f);
        var mirrorLabel=GameObject.Find("Mirror state");mirrorLabel.transform.position=new Vector3(-9.855f,1.58f,1.25f);mirrorLabel.GetComponent<RectTransform>().sizeDelta=new Vector2(360,120);mirrorLabel.GetComponentInChildren<Text>().rectTransform.sizeDelta=new Vector2(360,120);
        var cushion=GameObject.Find("FloorCushion");cushion.transform.position=new Vector3(7.7f,.029f,-1.1f);PrefabUtility.RecordPrefabInstancePropertyModifications(cushion.transform);
        var bedside=GameObject.Find("BedsideTable").GetComponent<BoxCollider>();bedside.center=new Vector3(0,.30f,0);Prefab(bedside.gameObject);
        foreach(string n in new[]{"QuietSofa300","QuietSofa240"}){var body=GameObject.Find(n).GetComponents<BoxCollider>()[0];body.center=new Vector3(0,.29f,0);body.size=new Vector3(n=="QuietSofa300"?2.86f:2.26f,.36f,1.02f);Prefab(body.gameObject);}
        if(!GameObject.Find("QuietOccupancyPanel"))Prefab(Model("QuietOccupancyPanel",new Vector3(3.90f,1.9f,-1.2f)));
        // Preserve the actual referenced Text and Canvas; move only their sign.
        GameObject.Find("Quiet room occupancy").transform.position=new Vector3(3.84f,1.9f,-1.2f);
        foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())if(l.type==LightType.Point)
        {
            var p=l.transform.position;
            if(p.x>2||p.z<-5){l.color=new Color(1,.82f,.64f);if(p.y>2)l.intensity=p.z<-5?1.25f:1.15f;}
        }
        Directory.CreateDirectory(Root+"/Textures");AssetDatabase.Refresh();
        var tex=new Texture2D(512,512,TextureFormat.RGB24,false);
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            int row=Mathf.Min(2,y*3/512);int end=(x+row*171)%512;
            float value=.82f+row*.025f+.065f*Mathf.PerlinNoise(x*.022f,y*.31f)+.020f*Mathf.Sin(y*.95f+Mathf.Sin(x*.018f));
            if(y%171<2)value*=.62f;if(end<2)value*=.75f;tex.SetPixel(x,y,new Color(value,value,value));
        }
        tex.Apply();var texturePath=Root+"/Textures/WalnutFloorBoards.png";File.WriteAllBytes(texturePath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(texturePath);
        var ti=(TextureImporter)AssetImporter.GetAtPath(texturePath);ti.wrapMode=TextureWrapMode.Repeat;ti.mipmapEnabled=true;ti.anisoLevel=4;ti.maxTextureSize=512;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        var floorMatPath=Root+"/Materials/FloorWalnut.mat";var floorMat=AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);if(!floorMat){floorMat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(floorMat,floorMatPath);}
        floorMat.color=new Color(.36f,.245f,.16f);floorMat.SetFloat("_Glossiness",.18f);floorMat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);floorMat.mainTextureScale=new Vector2(1/1.2f,1/.6f);EditorUtility.SetDirty(floorMat);
        var floor=GameObject.Find("House floor 20 x 16m");string meshPath=Root+"/WholeFloorMetreUV.asset";var floorMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!floorMesh){floorMesh=UnityEngine.Object.Instantiate(floor.GetComponent<MeshFilter>().sharedMesh);floorMesh.name="WholeFloorMetreUV";var uv=new Vector2[floorMesh.vertexCount];var vertices=floorMesh.vertices;var normals=floorMesh.normals;for(int i=0;i<uv.Length;i++){var p=Vector3.Scale(vertices[i],floor.transform.lossyScale);uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(p.x,p.z):new Vector2(p.x,p.y);}floorMesh.uv=uv;AssetDatabase.CreateAsset(floorMesh,meshPath);}
        floor.GetComponent<MeshFilter>().sharedMesh=floorMesh;floor.GetComponent<Renderer>().sharedMaterial=floorMat;
        Save();Debug.Log("[WholeFinish] Curtains clear sills; occupancy sign readable; same light count; metre-scale floor grain.");
    }
}
