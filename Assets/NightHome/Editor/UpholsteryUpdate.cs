using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class UpholsteryUpdate
{
    static bool Target(string path){return path.EndsWith("/Sofa320.fbx")||path.EndsWith("/Sofa300.fbx")||path.EndsWith("/QuietSofa300.fbx")||path.EndsWith("/QuietSofa240.fbx")||path.EndsWith("/BedroomBed.fbx")||path.EndsWith("/FloorCushion.fbx");}
    public static void Rebind()
    {
        foreach(string folder in new[]{"Assets/NightHome/LivingFinish/Prefabs","Assets/NightHome/WholeFinish/Prefabs"})
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{folder}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var f=prefab.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
            string model=AssetDatabase.GetAssetPath(f.sharedMesh);if(!Target(model))continue;
            var imported=AssetDatabase.LoadAssetAtPath<GameObject>(model).GetComponentInChildren<MeshFilter>();f.sharedMesh=imported.sharedMesh;EditorUtility.SetDirty(prefab);PrefabUtility.SavePrefabAsset(prefab);
        }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
    }
    public static void RefineMaterials()
    {
        foreach(var path in new[]{"Assets/NightHome/LivingFinish/Materials/GrayFabric.mat","Assets/NightHome/WholeFinish/Materials/BedLinen.mat","Assets/NightHome/LivingFinish/Materials/SeamGray.mat"})
        {var m=AssetDatabase.LoadAssetAtPath<Material>(path);m.SetFloat("_Glossiness",path.Contains("BedLinen")?.03f:.04f);if(m.HasProperty("_BumpScale"))m.SetFloat("_BumpScale",path.Contains("BedLinen")?.08f:.10f);EditorUtility.SetDirty(m);}AssetDatabase.SaveAssets();
    }
    public static string Validate()
    {
        var records=new List<string>();var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))records.Add("T|"+GlobalObjectId.GetGlobalObjectIdSlow(t)+"|"+t.localPosition.ToString("F6")+"|"+t.localRotation.ToString("F6")+"|"+t.localScale.ToString("F6")+"|"+t.gameObject.activeSelf);
        foreach(var c in UnityEngine.Object.FindObjectsOfType<Collider>(true))records.Add("C|"+GlobalObjectId.GetGlobalObjectIdSlow(c)+"|"+EditorJsonUtility.ToJson(c));
        int udon=0;foreach(var u in UnityEngine.Object.FindObjectsOfType<VRC.Udon.UdonBehaviour>(true)){udon++;var row="U|"+GlobalObjectId.GetGlobalObjectIdSlow(u)+"|"+u.programSource+"|";foreach(var key in u.publicVariables.VariableSymbols){object value;if(u.publicVariables.TryGetVariableValue(key,out value)&&value is UnityEngine.Object)row+=key+"="+GlobalObjectId.GetGlobalObjectIdSlow((UnityEngine.Object)value)+";";}records.Add(row);}
        records.Sort();var before=File.ReadAllLines("Documentation/Upholstery/ProtectedStateBefore.txt");bool equal=string.Join("\n",before)==string.Join("\n",records);
        Directory.CreateDirectory("Documentation/Upholstery");File.WriteAllLines("Documentation/Upholstery/ProtectedStateAfter.txt",records);
        var report=new List<string>{"Original Transform/Collider/Udon state identical="+equal,"Records="+records.Count+" Udon="+udon,"Scene="+scene.path};int missing=0;
        foreach(var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>())if(f.gameObject.scene==scene&&f.GetComponent<Renderer>()&&f.GetComponent<Renderer>().enabled){if(!f.sharedMesh){missing++;continue;}string path=AssetDatabase.GetAssetPath(f.sharedMesh);if(Target(path))report.Add(f.name+" | "+f.sharedMesh.triangles.Length/3+" tris | "+f.sharedMesh.bounds+" | "+f.GetComponent<Renderer>().sharedMaterials.Length+" materials");}
        report.Add("Missing active meshes="+missing);File.WriteAllLines("Documentation/Upholstery/Validation.txt",report);if(!equal||missing>0)throw new Exception("Protected state or mesh validation failed; inspect saved report");return string.Join("\n",report);
    }
}
