using UnityEditor;
using UnityEngine;
using System.IO;
[InitializeOnLoad]
public static class HomeSdkDiagnostics
{
    private static bool reporting;
    static HomeSdkDiagnostics() { Application.logMessageReceived += Record; }
    private static void Record(string message,string stack,LogType type)
    {
        if(reporting || !(message+stack).Contains("CreateContentInfoGUI")) return;
        reporting=true;
        try
        {
            bool resource=Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("VRCSdkWorldBuilderContentInfo")!=null;
            bool asset=AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>("Packages/com.vrchat.worlds/Editor/VRCSDK/SDK3/Resources/VRCSdkWorldBuilderContentInfo.uxml")!=null;
            string text=System.DateTime.UtcNow.AddHours(9).ToString("yyyy-MM-dd HH:mm:ss")+" JST; isBuilding="+BuildPipeline.isBuildingPlayer+"; resourceLoad="+resource+"; assetLoad="+asset+"\n"+message+"\n"+stack+"\n";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Documentation"));
            Directory.CreateDirectory(folder); File.AppendAllText(Path.Combine(folder,"SDK_UI_Exception.txt"),text);
        }
        finally { reporting=false; }
    }
}
