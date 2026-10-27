using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Temporary review stage: never saved into the production scene.
public static class LivingFinishReview
{
    [MenuItem("Night Home/Living Finish/Capture furniture views")]
    public static void CaptureSingles()
    {
        var stage=new GameObject("TEMP - furniture review");
        var mode=RenderSettings.ambientMode;var ambient=RenderSettings.ambientLight;
        RenderTexture rt=null;Texture2D pixels=null;
        try
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.35f,.35f,.35f);
            var cg=new GameObject("Review camera");cg.transform.SetParent(stage.transform);var cam=cg.AddComponent<Camera>();
            cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.19f,.18f,.16f);cam.fieldOfView=35;cam.nearClipPlane=.05f;cam.farClipPlane=60;
            foreach(var euler in new[]{new Vector3(35,-30,0),new Vector3(25,150,0)})
            {var lg=new GameObject("Review light");lg.transform.SetParent(stage.transform);lg.transform.rotation=Quaternion.Euler(euler);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=.85f;l.cullingMask=1<<31;l.shadows=LightShadows.None;}
            rt=new RenderTexture(1000,800,24);cam.targetTexture=rt;pixels=new Texture2D(1000,800,TextureFormat.RGB24,false);
            string folder="Documentation/LivingFinish";Directory.CreateDirectory(folder);
            string[] names={"Main sofa - 3.20m","Coffee table - 2.10m","TV console - 4.10m","Living rug - bound edge"};
            string[] labels={"Sofa","Table","Console","Rug"};
            var views=new[]{new Vector3(0,.15f,1),new Vector3(1,.15f,0),new Vector3(1,.65f,1)};
            for(int i=0;i<names.Length;i++)
            {
                var source=GameObject.Find(names[i]);var copy=UnityEngine.Object.Instantiate(source,stage.transform);
                copy.transform.SetPositionAndRotation(new Vector3(100,0,100),Quaternion.identity);
                foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
                var renderer=copy.GetComponent<Renderer>();renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
                renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
                var bounds=renderer.bounds;float size=Mathf.Max(bounds.size.x,bounds.size.z,bounds.size.y);
                for(int j=0;j<views.Length;j++)
                {
                    cam.transform.position=bounds.center+views[j].normalized*size*1.8f;cam.transform.LookAt(bounds.center);
                    cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1000,800),0,0);pixels.Apply();RenderTexture.active=previous;
                    File.WriteAllBytes(folder+"/"+labels[i]+"_"+new[]{"Front","Side","Angle"}[j]+".png",pixels.EncodeToPNG());
                }
                UnityEngine.Object.DestroyImmediate(copy);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(stage);if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);
            RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;
        }
        Debug.Log("[LivingFinish] Isolated front/side/angle views saved; temporary stage removed.");
    }
}
