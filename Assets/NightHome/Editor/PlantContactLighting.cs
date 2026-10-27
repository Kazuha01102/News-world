using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class PlantContactLighting
{
    public const string Home="Assets/NightHome/NightHome_Blockout.unity";
    public const string Trial="Assets/NightHome/PlantsWalls/LightingTrial/ContactProbe.unity";
    [MenuItem("Night Home/Plants and Walls/Start isolated small bake trial")]
    public static void StartTrial()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=Home||EditorApplication.isPlaying)throw new Exception("Open home in edit mode");
        if(!File.Exists(Trial))throw new Exception("Restore the committed ContactProbe trial scene before rebaking");
        foreach(var panel in Resources.FindObjectsOfTypeAll<VRCSdkControlPanel>())panel.Close();
        PlantsWallsBuilder.Save();EditorSceneManager.OpenScene(Trial);
        // Saved trial: only 3 renderers, 16 texels/m, 256 max atlas, two single-island
        // surface UVs, foliage casts baked shadows but receives eight Light Probes.
        SessionState.SetString("PlantsWalls.BakeTrial",DateTime.UtcNow.ToString("O"));
        if(!Lightmapping.BakeAsync())throw new Exception("Small bake did not start");
    }
    [MenuItem("Night Home/Plants and Walls/Apply one contact shadow light")]
    public static void ContactLight()
    {
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=Home)throw new Exception("Open home");
        if(GameObject.Find("Living contact shadow - one spot"))throw new Exception("Already added");
        var root=GameObject.Find("HOME - Plant and wall finish");var go=new GameObject("Living contact shadow - one spot");go.transform.SetParent(root.transform);go.transform.position=new Vector3(-4,3.18f,4);go.transform.rotation=Quaternion.Euler(90,0,0);
        var light=go.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=150;light.range=11;light.intensity=.75f;light.color=new Color(1,.84f,.68f);light.shadows=LightShadows.Soft;light.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Low;light.shadowCustomResolution=512;light.shadowStrength=.60f;light.shadowBias=.025f;light.shadowNormalBias=.12f;light.cullingMask=1;light.renderMode=LightRenderMode.ForcePixel;
        // Only the world Default layer is lit/cast by this spot; avatar layers are
        // excluded. Existing seven lights retain their shadow settings.
        foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>())if(l.type==LightType.Point&&Vector3.Distance(l.transform.position,new Vector3(-5,3,4))<.1f)l.intensity=1.25f;
        var shader=Shader.Find("NightHome/Quiet Night Sky");if(!shader)throw new Exception("Night sky shader missing");
        string path=PlantsWallsBuilder.Root+"/Materials/QuietNightSky.mat";var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!sky){sky=new Material(shader);AssetDatabase.CreateAsset(sky,path);}RenderSettings.skybox=sky;
        PlantsWallsBuilder.Save();
    }
}
