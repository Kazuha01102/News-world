using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;
using VRC.SDK3.Components;

public static class HomeBlockoutBuilder
{
    private const string Root="Assets/NightHome";
    private static Transform architecture;
    private static Font font;
    private static Material wood,wall,cream,fabric,green,black,glow,night;

    [MenuItem("Night Home/Create Home Blockout")]
    public static void Build()
    {
        string path=Root+"/NightHome_Blockout.unity";
        if(File.Exists(path)) throw new Exception("Home already exists. Open it; do not overwrite manual changes.");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.path!=Root+"/NewsPlaybackTest.unity" || active.isDirty) throw new Exception("Open and save NewsPlaybackTest before creating the home.");
        EditorSceneManager.SaveScene(active,path,true);
        EditorSceneManager.OpenScene(path);
        Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Audio");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/ZenKakuGothicNew-Regular.ttf");
        if(!font) throw new Exception("Japanese font missing");
        foreach(string name in new[]{"HomeLocalMirror","HomeRoomAudio","HomePresenceLog"}) EnsureProgram(name);
        typeof(UdonSharpProgramAsset).GetMethod("ClearProgramAssetCache",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,null);
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        architecture=new GameObject("HOME - Architecture and furniture").transform;
        wood=Mat("Walnut",new Color(.30f,.16f,.085f));
        Texture2D grain=new Texture2D(128,128,TextureFormat.RGB24,false);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++) {float n=Mathf.PerlinNoise(x*.055f,y*.42f)*.24f+.78f; if(y%32==0)n*=.55f; grain.SetPixel(x,y,new Color(n,n,n));} grain.Apply();
        AssetDatabase.CreateAsset(grain,Root+"/Materials/WoodGrain.asset"); wood.mainTexture=grain; wood.mainTextureScale=new Vector2(8,8);
        wall=Mat("Warm greige",new Color(.49f,.46f,.41f)); cream=Mat("Warm linen",new Color(.76f,.69f,.55f));
        fabric=Mat("Sofa olive",new Color(.26f,.32f,.27f)); green=Mat("Leaves",new Color(.12f,.27f,.17f));
        black=Mat("Charcoal",new Color(.065f,.075f,.08f)); glow=Mat("Warm lamp",new Color(1,.71f,.35f),true);
        night=Mat("Night glass",new Color(.018f,.035f,.075f),true);
        var oldFloor=GameObject.Find("Test Floor"); oldFloor.name="House floor 20 x 16m"; oldFloor.transform.position=new Vector3(0,-.12f,0); oldFloor.transform.localScale=new Vector3(20,.24f,16); oldFloor.GetComponent<Renderer>().sharedMaterial=wood;
        Static(oldFloor);
        Box("Ceiling",new Vector3(0,3.5f,0),new Vector3(20,.2f,16),wall);
        Box("West wall",new Vector3(-10,1.7f,0),new Vector3(.2f,3.4f,16),wall);
        Box("East wall",new Vector3(10,1.7f,0),new Vector3(.2f,3.4f,16),wall);
        Box("South wall",new Vector3(0,1.7f,-8),new Vector3(20,3.4f,.2f),wall);
        // North wall with real window openings, not a daytime skybox.
        Box("North base",new Vector3(0,.5f,8),new Vector3(20,1,.2f),wall);
        Box("North lintel",new Vector3(0,3.15f,8),new Vector3(20,.5f,.2f),wall);
        foreach(float x in new[]{-9f,-2f,2f,9f})Box("North pier",new Vector3(x,1.95f,8),new Vector3(2,1.9f,.2f),wall);
        Box("TV wall",new Vector3(-5,1.95f,8),new Vector3(4,1.9f,.2f),wall);
        Window(-.1f,8); Window(6,8); Window(-7.8f,8);
        // Hall: two metres wide, bedroom door 1.6m, quiet-room door 1.6m.
        Box("Living hall partition south",new Vector3(2,1.7f,-5.5f),new Vector3(.18f,3.4f,5),wall);
        Box("Living hall partition north",new Vector3(2,1.7f,.5f),new Vector3(.18f,3.4f,3),wall);
        Box("Bedroom west",new Vector3(2,1.7f,5),new Vector3(.18f,3.4f,6),wall);
        Box("Bedroom south right",new Vector3(6.9f,1.7f,2),new Vector3(6.2f,3.4f,.18f),wall);
        Box("Bedroom south left",new Vector3(2.2f,1.7f,2),new Vector3(.4f,3.4f,.18f),wall);
        Box("Bedroom door lintel",new Vector3(3.1f,2.9f,2),new Vector3(1.4f,1,.18f),wall);
        Box("Quiet room north",new Vector3(7,1.7f,0),new Vector3(6,3.4f,.18f),wall);
        Box("Quiet room south",new Vector3(7,1.7f,-6),new Vector3(6,3.4f,.18f),wall);
        Box("Quiet room west N",new Vector3(4,1.7f,-1.1f),new Vector3(.18f,3.4f,2.2f),wall);
        Box("Quiet room west S",new Vector3(4,1.7f,-4.9f),new Vector3(.18f,3.4f,2.2f),wall);
        Box("Quiet door lintel",new Vector3(4,2.9f,-3),new Vector3(.18f,1,1.6f),wall);
        Box("Entry door",new Vector3(-1,1.2f,-7.88f),new Vector3(1.3f,2.4f,.08f),wood);
        Box("Door handle",new Vector3(-.5f,1.05f,-7.8f),new Vector3(.18f,.05f,.08f),glow,false);
        Box("Entry mat",new Vector3(-1,.018f,-6.9f),new Vector3(2,.03f,1.1f),fabric,false);
        Box("Living rug",new Vector3(-4,.025f,3.35f),new Vector3(8,.04f,6),cream,false);
        Sofa("Main sofa",new Vector3(-4,0,1),0,3.7f);
        Sofa("Window sofa",new Vector3(-8,0,4),90,3.3f);
        Sofa("Side sofa",new Vector3(0,0,4),-90,3.3f);
        Table("Coffee table",new Vector3(-4,0,4),new Vector2(2.3f,1.2f),.43f);
        Cup(new Vector3(-4.5f,.5f,4)); Cup(new Vector3(-3.4f,.5f,4.2f));
        Box("TV console",new Vector3(-5,.4f,7.25f),new Vector3(4.5f,.8f,.65f),wood);
        Box("Bedroom rug",new Vector3(6,.02f,4.7f),new Vector3(5,.03f,4.6f),fabric,false);
        Box("Bed base",new Vector3(6,.28f,5.3f),new Vector3(2.5f,.56f,3),wood);
        Box("Mattress",new Vector3(6,.68f,5.3f),new Vector3(2.45f,.26f,2.9f),cream);
        Box("Bed cover",new Vector3(6,.84f,4.8f),new Vector3(2.46f,.08f,1.9f),fabric,false);
        foreach(float x in new[]{5.35f,6.65f}) Box("Pillow",new Vector3(x,.89f,6.3f),new Vector3(.9f,.2f,.55f),cream,false);
        Box("Headboard",new Vector3(6,.9f,6.9f),new Vector3(2.7f,1.6f,.14f),wood);
        Table("Bedside table",new Vector3(8,0,6.2f),new Vector2(.7f,.7f),.58f);
        Sofa("Quiet sofa south",new Vector3(7,0,-5),0,3.5f);
        Sofa("Quiet sofa east",new Vector3(9,0,-2.2f),-90,2.5f);
        Table("Quiet table",new Vector3(7,0,-3),new Vector2(1.6f,1),.45f);
        Box("Quiet rug",new Vector3(7,.025f,-3),new Vector3(4.6f,.04f,4.5f),cream,false);
        Shelf(new Vector3(-9.5f,0,-.2f)); Plant(new Vector3(-9,0,7)); Plant(new Vector3(.8f,0,7)); Plant(new Vector3(9,0,7));
        Table("QvPen return stand",new Vector3(-7,0,-1.8f),new Vector2(2.2f,.6f),.82f);
        Box("Drawing area rug",new Vector3(-7,.02f,-3.8f),new Vector3(3.4f,.025f,3),fabric,false);
        Box("Drawing board",new Vector3(-9.8f,1.6f,-3.7f),new Vector3(.1f,1.9f,3),cream);
        Label("Pen area sign","お絵かきスペース\n使い終わったペンはこちらへ",new Vector3(-7,1.7f,-.9f),new Vector2(2.2f,.65f),0,30);
        var qv=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/net.ureishi.qvpen/QvPen.prefab");
        if(qv) {var obj=(GameObject)PrefabUtility.InstantiatePrefab(qv); obj.name="QvPen 3.3.15 - Official prefab"; obj.transform.position=new Vector3(-7.8f,.88f,-1.8f);}
        CreateNight();
        RenderSettings.skybox=null; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.4f,.36f,.3f); RenderSettings.ambientIntensity=.65f;
        RenderSettings.fog=true; RenderSettings.fogColor=new Color(.015f,.022f,.05f); RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogStartDistance=25; RenderSettings.fogEndDistance=65;
        var sun=GameObject.Find("Directional Light").GetComponent<Light>(); sun.intensity=.14f; sun.color=new Color(.4f,.53f,.8f); sun.shadows=LightShadows.None;
        foreach(var pos in new[]{new Vector3(-5,3,4),new Vector3(-4,3,-3),new Vector3(6,3,5),new Vector3(7,3,-3),new Vector3(3,3,-1),new Vector3(-1,3,-6)}) Lamp(pos);
        var camera=Camera.main; camera.transform.position=new Vector3(-1,1.65f,-5.8f); camera.transform.rotation=Quaternion.Euler(0,-18,0); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RenderSettings.fogColor;
        var descriptor=UnityEngine.Object.FindObjectOfType<VRCSceneDescriptor>(); descriptor.spawns[0].position=new Vector3(-1,.08f,-6.1f); descriptor.spawns[0].rotation=Quaternion.Euler(0,-18,0); descriptor.capacity=16; descriptor.ReferenceCamera=camera.gameObject;
        ConfigureTelevision(); CreateSystems();
        var eventSystem=GameObject.Find("EventSystem"); if(eventSystem) eventSystem.SetActive(false);
        UpdateLayers.SetupEditorLayers(); UpdateLayers.SetupCollisionLayerMatrix();
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)}; AssetDatabase.SaveAssets();
        Debug.Log("[NightHome] Home blockout saved; original NewsPlaybackTest retained.");
    }
    private static void ConfigureTelevision()
    {
        var root=GameObject.Find("News TV - Live Probe");
        var u=root.GetComponent<UdonBehaviour>(); var tv=(NewsPlaybackProbe)UdonSharpEditorUtility.GetProxyBehaviour(u); UdonSharpEditorUtility.CopyUdonToProxy(tv);
        root.transform.position=Vector3.zero;
        var screen=root.transform.Find("Screen"); screen.position=new Vector3(-5,2.35f,7.7f); screen.localScale=new Vector3(3.8f,2.14f,1);
        tv.speaker.transform.position=new Vector3(-5,1.8f,7.4f); tv.speaker.volume=.35f; tv.speaker.dopplerLevel=0; tv.speaker.spatialBlend=1; tv.speaker.rolloffMode=AudioRolloffMode.Linear; tv.speaker.minDistance=2; tv.speaker.maxDistance=16;
        var spat=tv.speaker.GetComponent<VRCSpatialAudioSource>(); spat.Gain=0; spat.Near=2; spat.Far=16; spat.UseAudioSourceVolumeCurve=true;
        var so=new SerializedObject(tv.speaker.GetComponent<VRC.SDK3.Video.Components.AVPro.VRCAVProVideoSpeaker>()); so.FindProperty("videoPlayer").objectReferenceValue=tv.player; so.ApplyModifiedProperties();
        var ui=GameObject.Find("News Controls"); ui.transform.position=new Vector3(-.6f,1.7f,7.4f); ui.transform.localScale=Vector3.one*.002f;
        foreach(var t in ui.GetComponentsInChildren<Text>(true)) {t.font=font; t.fontSize=23;}
        tv.status.text="ニューステレビ";
        string[] ids={"Primary","Fallback","Play","Stop","Reconnect","Manual","Mute","Quieter","Louder"};
        string[] labels={"ニュース","予備URL","再生","停止","再接続","URLを適用","消音切替","音量 −","音量 ＋"};
        for(int i=0;i<ids.Length;i++) {var b=ui.transform.Find(ids[i]); if(b)b.GetComponentInChildren<Text>().text=labels[i];}
        var placeholder=ui.transform.Find("Manual URL/Placeholder"); if(placeholder)placeholder.GetComponent<Text>().text="公式配信のURLを入力";
        ui.transform.Find("Fetch Metadata").gameObject.SetActive(false); ui.transform.Find("Metadata status").gameObject.SetActive(false);
        Box("TV control backing",new Vector3(-.6f,1.7f,7.5f),new Vector3(2.7f,1.4f,.08f),black,false);
        UdonSharpEditorUtility.CopyProxyToUdon(tv);
    }
    private static void CreateSystems()
    {
        var logGo=new GameObject("HOME - Shared presence log"); var log=logGo.AddUdonSharpComponent<HomePresenceLog>();
        log.display=Label("Entry log panel","入退室ログ JST",new Vector3(-3.4f,1.8f,-7.75f),new Vector2(3,2.3f),180,23); log.display.supportRichText=false; log.display.alignment=TextAnchor.UpperLeft;
        Box("Log backing",new Vector3(-3.4f,1.8f,-7.85f),new Vector3(3.15f,2.4f,.07f),black,false);
        log.sound=logGo.AddComponent<AudioSource>(); log.sound.playOnAwake=false; log.sound.spatialBlend=0; log.sound.volume=.15f;
        log.joined=Chime("Join",660,880); log.left=Chime("Leave",660,440);
        var soundCanvas=Canvas("Entry sound controls",new Vector3(-6.1f,1.4f,-7.65f),new Vector2(750,420),180,.002f);
        log.soundLabel=TextAt(soundCanvas.transform,"State","入退室音",new Vector2(0,135),new Vector2(730,80),28);
        UdonSharpEditorUtility.CopyProxyToUdon(log);
        ButtonAt(soundCanvas.transform,"入退室音 ON/OFF","ToggleSound",log,new Vector2(0,25),new Vector2(700,70));
        ButtonAt(soundCanvas.transform,"音量 −","Quieter",log,new Vector2(-180,-80),new Vector2(330,70)); ButtonAt(soundCanvas.transform,"音量 ＋","Louder",log,new Vector2(180,-80),new Vector2(330,70));
        var zone=new GameObject("HOME - Quiet room audio boundary"); zone.transform.position=new Vector3(7,1.6f,-3); var box=zone.AddComponent<BoxCollider>(); box.isTrigger=true; box.size=new Vector3(6,3.8f,6);
        var room=zone.AddUdonSharpComponent<HomeRoomAudio>(); room.room=box; room.presence=log;
        var tvu=GameObject.Find("News TV - Live Probe").GetComponent<UdonBehaviour>(); room.television=(NewsPlaybackProbe)UdonSharpEditorUtility.GetProxyBehaviour(tvu); UdonSharpEditorUtility.CopyUdonToProxy(room.television);
        room.occupancy=Label("Quiet room occupancy","静かな個室 0 / 6人目安",new Vector3(3.84f,2.35f,-3),new Vector2(1.7f,.7f),90,27);
        Label("Quiet room notice","室内と室外の声を分離します\n完全な秘匿を保証するものではありません",new Vector3(7,2.6f,-.15f),new Vector2(3.4f,.55f),0,24);
        Label("Bedroom sign","寝室",new Vector3(3.1f,2.65f,1.85f),new Vector2(1.3f,.35f),0,30);
        UdonSharpEditorUtility.CopyProxyToUdon(room);
        var mirrorPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCMirror.prefab");
        var mirror=(GameObject)PrefabUtility.InstantiatePrefab(mirrorPrefab); mirror.name="HOME - Local mirror (default OFF)"; mirror.transform.position=new Vector3(-9.85f,1.65f,-.6f); mirror.transform.rotation=Quaternion.Euler(0,-90,0); mirror.transform.localScale=new Vector3(2.6f,2.3f,1); mirror.SetActive(false);
        var mirrorComp=mirror.GetComponent<VRC.SDK3.Components.VRCMirrorReflection>();
        if(mirrorComp) {var serialized=new SerializedObject(mirrorComp); serialized.FindProperty("m_ReflectLayers").intValue=(1<<0)|(1<<9)|(1<<10)|(1<<18); serialized.ApplyModifiedProperties();}
        var switchGo=Box("Mirror Use switch",new Vector3(-9.6f,1.1f,1.1f),new Vector3(.14f,.32f,.32f),glow);
        var toggle=switchGo.AddUdonSharpComponent<HomeLocalMirror>(); toggle.mirror=mirror; toggle.label=Label("Mirror state","ミラー OFF ／ Useで切替",new Vector3(-9.5f,1.55f,1.1f),new Vector2(1.25f,.5f),-90,25);
        UdonSharpEditorUtility.CopyProxyToUdon(toggle); var backing=UdonSharpEditorUtility.GetBackingUdonBehaviour(toggle); backing.interactText="自分だけミラー ON / OFF"; backing.proximity=2;
    }
    private static void EnsureProgram(string name)
    {
        string path=Root+"/Scripts/"+name+".asset"; if(AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path))return;
        var p=ScriptableObject.CreateInstance<UdonSharpProgramAsset>(); p.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"/Scripts/"+name+".cs"); AssetDatabase.CreateAsset(p,path); AssetDatabase.SaveAssets();
    }
    private static Material Mat(string name,Color color,bool emission=false)
    {
        var m=new Material(Shader.Find("Standard")); m.name=name; m.color=color; m.SetFloat("_Glossiness",.15f);
        if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.6f);} AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat"); return m;
    }
    private static void Static(GameObject go) {GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);}
    private static GameObject Box(string name,Vector3 pos,Vector3 size,Material material,bool collider=true)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(architecture);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider) UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); Static(g); return g;
    }
    private static void Sofa(string name,Vector3 pos,float yaw,float width)
    {
        var root=new GameObject(name).transform;root.position=pos;root.rotation=Quaternion.Euler(0,yaw,0);root.SetParent(architecture);
        foreach(var data in new[]{new Vector4(0,.28f,0,0),new Vector4(0,.82f,-.5f,1),new Vector4(-width*.5f,.6f,0,2),new Vector4(width*.5f,.6f,0,2)})
        {var s=data.w==0?new Vector3(width,.5f,1.15f):data.w==1?new Vector3(width,1,.2f):new Vector3(.2f,.8f,1.15f);var g=Box(name+" part",root.TransformPoint(new Vector3(data.x,data.y,data.z)),s,fabric);g.transform.rotation=root.rotation;g.transform.SetParent(root);}
        for(int i=0;i<3;i++){var g=Box("Cushion",root.TransformPoint(new Vector3((i-1)*width*.27f,.61f,.05f)),new Vector3(width*.28f,.15f,.88f),cream,false);g.transform.rotation=root.rotation;g.transform.SetParent(root);}
    }
    private static void Table(string name,Vector3 pos,Vector2 size,float height)
    {
        Box(name+" top",pos+Vector3.up*height,new Vector3(size.x,.1f,size.y),wood);
        foreach(float x in new[]{-.4f,.4f})foreach(float z in new[]{-.35f,.35f})Box(name+" leg",pos+new Vector3(size.x*x,height*.5f,size.y*z),new Vector3(.08f,height,.08f),black);
    }
    private static void Cup(Vector3 pos) {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Mug";g.transform.SetParent(architecture);g.transform.position=pos+Vector3.up*.07f;g.transform.localScale=new Vector3(.13f,.09f,.13f);g.GetComponent<Renderer>().sharedMaterial=cream;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Static(g);}
    private static void Plant(Vector3 pos)
    {Box("Plant pot",pos+Vector3.up*.22f,new Vector3(.4f,.44f,.4f),cream);for(int i=0;i<5;i++){var g=GameObject.CreatePrimitive(PrimitiveType.Sphere);g.name="Plant leaf cluster";g.transform.SetParent(architecture);g.transform.position=pos+new Vector3(Mathf.Sin(i*2)*.18f,.65f+i*.08f,Mathf.Cos(i*2)*.18f);g.transform.localScale=new Vector3(.45f,.4f,.3f);g.GetComponent<Renderer>().sharedMaterial=green;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Static(g);}}
    private static void Shelf(Vector3 pos)
    {Box("Bookcase side",pos+new Vector3(0,1,0),new Vector3(.6f,2,1.8f),wood);for(int i=0;i<4;i++)for(int j=0;j<4;j++)Box("Book",pos+new Vector3(.34f,.25f+i*.45f,-.6f+j*.3f),new Vector3(.26f,.31f,.12f),i%2==0?cream:fabric,false);}
    private static void Window(float x,float z)
    {Box("Night window",new Vector3(x,1.95f,z+.02f),new Vector3(1.6f,1.8f,.05f),night,false);foreach(float xx in new[]{x-.84f,x+.84f})Box("Window frame",new Vector3(xx,1.95f,z-.08f),new Vector3(.08f,1.9f,.1f),wood,false);}
    private static void Lamp(Vector3 pos)
    {Box("Ceiling lamp",pos+Vector3.up*.25f,new Vector3(.65f,.15f,.65f),glow,false);var g=new GameObject("Warm light");g.transform.SetParent(architecture);g.transform.position=pos;var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.75f,.48f);l.intensity=1.5f;l.range=7;l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;}
    private static void CreateNight()
    {for(int i=0;i<8;i++){float x=-16+i*4.5f;Box("Neighbour house",new Vector3(x,2,16+(i%2)*4),new Vector3(3.7f,4,4),black);for(int j=0;j<2;j++)Box("Neighbour window",new Vector3(x-.8f+j*1.4f,2.3f,13.95f+(i%2)*4),new Vector3(.55f,.7f,.04f),glow,false);}}
    private static GameObject Canvas(string name,Vector3 pos,Vector2 size,float yaw,float scale)
    {var g=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(VRCUiShape));g.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;g.GetComponent<RectTransform>().sizeDelta=size;g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=Vector3.one*scale;return g;}
    private static Text Label(string name,string value,Vector3 pos,Vector2 metres,float yaw,int fontSize)
    {var g=Canvas(name,pos,metres*400,yaw,.0025f);return TextAt(g.transform,name+" text",value,Vector2.zero,metres*400,fontSize);}
    private static Text TextAt(Transform parent,string name,string value,Vector2 pos,Vector2 size,int fontSize)
    {var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=pos;var t=g.GetComponent<Text>();t.font=font;t.fontSize=fontSize;t.color=new Color(.92f,.88f,.76f);t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;return t;}
    private static void ButtonAt(Transform parent,string label,string method,UdonSharpBehaviour target,Vector2 pos,Vector2 size)
    {var g=new GameObject(label,typeof(RectTransform),typeof(Image),typeof(Button));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.sizeDelta=size;r.anchoredPosition=pos;g.GetComponent<Image>().color=new Color(.16f,.21f,.19f);TextAt(g.transform,"Label",label,Vector2.zero,size,28);UnityEventTools.AddStringPersistentListener(g.GetComponent<Button>().onClick,UdonSharpEditorUtility.GetBackingUdonBehaviour(target).SendCustomEvent,method);}
    private static AudioClip Chime(string name,float a,float b)
    {
        string path=Root+"/Audio/"+name+".wav";int rate=22050,count=(int)(rate*.22f);using(var w=new BinaryWriter(File.Create(path))){w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);for(int i=0;i<count;i++){float t=i/(float)rate;float f=t<.1f?a:b;float envelope=Mathf.Sin(Mathf.PI*i/count)*Mathf.Exp(-t*8);w.Write((short)(Mathf.Sin(2*Mathf.PI*f*t)*envelope*5000));}}AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
}
