using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Fixed cameras and lights for before/after comparisons. Never persists a stage.
public static class UpholsteryReview
{
    public static void Capture(string phase,bool prototypes=false)
    {
        var stage=new GameObject("TEMP - upholstery review");var mode=RenderSettings.ambientMode;var ambient=RenderSettings.ambientLight;
        RenderTexture rt=null;Texture2D pixels=null;
        try
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.35f,.35f);
            var cg=new GameObject("Comparison camera",typeof(Camera));cg.transform.SetParent(stage.transform);var cam=cg.GetComponent<Camera>();cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.18f,.16f);cam.fieldOfView=35;cam.nearClipPlane=.02f;cam.farClipPlane=30;
            foreach(var e in new[]{new Vector3(35,-30,0),new Vector3(25,150,0)}){var lg=new GameObject("Comparison light",typeof(Light));lg.transform.SetParent(stage.transform);lg.transform.rotation=Quaternion.Euler(e);var l=lg.GetComponent<Light>();l.type=LightType.Directional;l.intensity=.85f;l.cullingMask=1<<31;l.shadows=LightShadows.None;}
            rt=new RenderTexture(1000,800,24);cam.targetTexture=rt;pixels=new Texture2D(1000,800,TextureFormat.RGB24,false);Directory.CreateDirectory("Documentation/Upholstery");
            string[] names=prototypes?new[]{"SeatBefore","SeatAfter","PillowBefore","PillowAfter"}:new[]{"Main sofa - 3.20m","BedroomBed"};
            foreach(var name in names)
            {
                GameObject copy;
                if(prototypes){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NightHome/UpholsteryStudy/Models/"+name+".fbx");copy=UnityEngine.Object.Instantiate(asset,stage.transform);var renderer=copy.GetComponentInChildren<MeshRenderer>();renderer.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>(name.StartsWith("Seat")?"Assets/NightHome/LivingFinish/Materials/GrayFabric.mat":"Assets/NightHome/WholeFinish/Materials/BedLinen.mat")};}
                else copy=UnityEngine.Object.Instantiate(GameObject.Find(name),stage.transform);
                copy.transform.SetPositionAndRotation(new Vector3(100,0,100),Quaternion.identity);
                foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                foreach(var r in copy.GetComponentsInChildren<Renderer>()){r.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;r.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;}
                var origin=new Vector3(100,0,100);Vector3[] positions,targets;
                if(prototypes){float d=name.StartsWith("Seat")?2.0f:1.65f;positions=new[]{new Vector3(0,.22f,d),new Vector3(d*.65f,d*.55f,d*.65f),new Vector3(.35f,.50f,.95f)};targets=new[]{Vector3.zero,Vector3.zero,Vector3.zero};}
                else if(name.StartsWith("Main")){positions=new[]{new Vector3(0,1.05f,5.7f),new Vector3(3.8f,2.6f,3.8f),new Vector3(-.45f,1,1.50f)};targets=new[]{new Vector3(0,.45f,0),new Vector3(0,.45f,0),new Vector3(0,.38f,.05f)};}
                else{positions=new[]{new Vector3(0,1.4f,-4.7f),new Vector3(-3.1f,2.2f,-3.1f),new Vector3(-.85f,1.15f,-.10f)};targets=new[]{new Vector3(0,.65f,0),new Vector3(0,.65f,0),new Vector3(-.44f,.75f,.72f)};}
                for(int j=0;j<3;j++){cam.transform.position=origin+positions[j];cam.transform.LookAt(origin+targets[j]);cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1000,800),0,0);pixels.Apply();RenderTexture.active=previous;string label=prototypes?name:(name.StartsWith("Main")?"Sofa":"Bed");File.WriteAllBytes("Documentation/Upholstery/"+phase+"_"+label+"_"+new[]{"Front","Angle","Near"}[j]+".png",pixels.EncodeToPNG());}
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(stage);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;}
    }
}
