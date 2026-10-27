using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UdonSharpEditor;
using UdonSharp;
using VRC.Udon;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components.AVPro;

public static class NewsProbeBuilder
{
    [MenuItem("Night Home/Create News Test Scene")]
    public static void Build()
    {
        const string scenePath = "Assets/NightHome/NewsPlaybackTest.unity";
        if (System.IO.File.Exists(scenePath)) { ValidateSaved(); return; }
        EnsureProgram("NewsPlaybackProbe");
        EnsureProgram("NewsMetadataProbe");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Test Floor"; floor.transform.position = new Vector3(0,-.1f,0); floor.transform.localScale = new Vector3(14,.2f,14);
        var spawn = new GameObject("Spawn - clear test area"); spawn.transform.position = new Vector3(0,.1f,-3);
        var descriptor = new GameObject("World Descriptor").AddComponent<VRCSceneDescriptor>();
        var pipeline = descriptor.gameObject.AddComponent<VRC.Core.PipelineManager>();
        pipeline.contentType = VRC.Core.PipelineManager.ContentType.world;
        descriptor.spawns = new[] { spawn.transform };
        descriptor.RespawnHeightY = -10;
        UpdateLayers.SetupEditorLayers();
        UpdateLayers.SetupCollisionLayerMatrix();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VideoPlayers/UdonSyncPlayer (AVPro).prefab");
        if (!prefab) throw new System.Exception("SDK AVPro sample prefab missing");
        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = "News TV - Live Probe"; root.transform.position = Vector3.zero;
        foreach (var old in root.GetComponentsInChildren<UdonBehaviour>(true)) Object.DestroyImmediate(old);
        var oldCanvas = root.GetComponentInChildren<Canvas>(true); if (oldCanvas) Object.DestroyImmediate(oldCanvas.gameObject);
        var screen = root.transform.Find("Screen");
        screen.localPosition = new Vector3(0,2.5f,4); screen.localScale = new Vector3(4.8f,2.7f,1);
        var probe = root.AddUdonSharpComponent<NewsPlaybackProbe>();
        probe.player = root.GetComponent<VRCAVProVideoPlayer>();
        probe.primary = new VRCUrl("https://www.youtube.com/watch?v=Anr15FA9OCI");
        // A separate resolver test, not a guaranteed second broadcaster.
        probe.fallback = new VRCUrl("https://www.youtube.com/@tbsnewsdig/live");
        probe.speaker = root.GetComponentInChildren<AudioSource>(); probe.speaker.volume = .35f;
        probe.speaker.transform.position = new Vector3(0,2,4);
        var ui = new GameObject("News Controls", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(VRCUiShape));
        var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        ui.GetComponent<RectTransform>().sizeDelta = new Vector2(1300,650);
        ui.transform.position = new Vector3(0,1.05f,3.5f); ui.transform.localScale = Vector3.one * .0028f;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        probe.status = Label(ui.transform, "Playback status", "AVPro live probe: Build & Test in VRChat", new Vector2(0,270), new Vector2(1250,55), font);
        var inputGo = new GameObject("Manual URL", typeof(RectTransform), typeof(Image), typeof(VRCUrlInputField));
        inputGo.transform.SetParent(ui.transform,false);
        var rect = inputGo.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1200,65); rect.anchoredPosition = new Vector2(0,190);
        var input = inputGo.GetComponent<VRCUrlInputField>();
        input.textComponent = Label(inputGo.transform,"URL Text","",Vector2.zero,new Vector2(1170,60),font);
        input.textComponent.color = Color.black;
        input.placeholder = Label(inputGo.transform,"Placeholder","Paste an official YouTube watch URL",Vector2.zero,new Vector2(1170,60),font);
        input.placeholder.color = Color.gray;
        probe.urlInput = input;
        UdonSharpEditorUtility.CopyProxyToUdon(probe);
        var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(probe);
        string[] events = {"Primary","Fallback","Play","Stop","Reconnect","Manual","Mute","Quieter","Louder"};
        for (int i=0; i<events.Length; i++)
        {
            var b = new GameObject(events[i],typeof(RectTransform),typeof(Image),typeof(Button)); b.transform.SetParent(ui.transform,false);
            var r=b.GetComponent<RectTransform>(); r.sizeDelta=new Vector2(230,55); r.anchoredPosition=new Vector2(-500+(i%5)*250,95-(i/5)*75);
            b.GetComponent<Image>().color=new Color(.22f,.28f,.32f);
            Label(b.transform,"Label",events[i],Vector2.zero,new Vector2(230,55),font);
            UnityEventTools.AddStringPersistentListener(b.GetComponent<Button>().onClick,backing.SendCustomEvent,events[i]);
        }
        var metadataGo = new GameObject("Metadata diagnostic");
        var metadata = metadataGo.AddUdonSharpComponent<NewsMetadataProbe>();
        metadata.endpoint = VRCUrl.Empty;
        metadata.output=Label(ui.transform,"Metadata status","Metadata endpoint not configured. Downloaded URL text cannot become VRCUrl at runtime.",new Vector2(0,-150),new Vector2(1230,140),font);
        UdonSharpEditorUtility.CopyProxyToUdon(metadata);
        var fetch = new GameObject("Fetch Metadata",typeof(RectTransform),typeof(Image),typeof(Button)); fetch.transform.SetParent(ui.transform,false);
        fetch.GetComponent<RectTransform>().sizeDelta=new Vector2(300,55); fetch.GetComponent<RectTransform>().anchoredPosition=new Vector2(0,-270);
        Label(fetch.transform,"Label","Fetch Metadata",Vector2.zero,new Vector2(300,55),font).color=Color.black;
        UnityEventTools.AddStringPersistentListener(fetch.GetComponent<Button>().onClick,UdonSharpEditorUtility.GetBackingUdonBehaviour(metadata).SendCustomEvent,"Fetch");
        if (!Object.FindObjectOfType<EventSystem>()) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),scenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scenePath,true)};
        AssetDatabase.SaveAssets();
        Debug.Log("[NightHome] News probe scene created. Playback and multiplayer remain unverified until VRChat client testing.");
    }
    [MenuItem("Night Home/Validate Saved News Test")]
    public static void ValidateSaved()
    {
        EditorSceneManager.OpenScene("Assets/NightHome/NewsPlaybackTest.unity");
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        int missing=0;
        foreach(var go in Object.FindObjectsOfType<GameObject>(true)) missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        var probe=Object.FindObjectOfType<NewsPlaybackProbe>();
        if(missing!=0 || !probe || !probe.player || !probe.urlInput || !probe.speaker || !probe.status) throw new System.Exception("News probe reference validation failed");
        var metadata=Object.FindObjectOfType<NewsMetadataProbe>();
        if(!metadata || !metadata.output) throw new System.Exception("Metadata probe reference validation failed");
        foreach(var backing in Object.FindObjectsOfType<UdonBehaviour>())
            if(!backing.programSource || !new SerializedObject(backing).FindProperty("serializedProgramAsset").objectReferenceValue) throw new System.Exception("Missing Udon program");
        foreach(var button in Object.FindObjectsOfType<Button>())
            if(button.onClick.GetPersistentEventCount()!=1 || !button.onClick.GetPersistentTarget(0)) throw new System.Exception("Missing button listener");
        if(!Object.FindObjectOfType<VRCSceneDescriptor>() || !Object.FindObjectOfType<Camera>() || !Object.FindObjectOfType<Light>()) throw new System.Exception("Missing scene essentials");
        System.IO.File.WriteAllText("Documentation/unity-validation.txt", "Saved scene reopened; C# and Udon compile completed; no missing scripts; TV/URL/audio/status/metadata references checked; 10 UI button targets checked; descriptor/camera/light present. Runtime playback and network behavior NOT tested.");
        Debug.Log("[NightHome] Saved scene validation passed. Runtime playback and multiplayer NOT tested.");
    }
    private static void EnsureProgram(string name)
    {
        string path="Assets/NightHome/Scripts/"+name+".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path)) return;
        var program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
        program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/NightHome/Scripts/"+name+".cs");
        AssetDatabase.CreateAsset(program,path);
        AssetDatabase.SaveAssets();
    }
    private static Text Label(Transform parent,string name,string value,Vector2 position,Vector2 size,Font font)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var rect=go.GetComponent<RectTransform>(); rect.sizeDelta=size; rect.anchoredPosition=position;
        var text=go.GetComponent<Text>(); text.font=font; text.fontSize=22; text.alignment=TextAnchor.MiddleCenter; text.color=Color.white; text.text=value; text.raycastTarget=false; return text;
    }
}
