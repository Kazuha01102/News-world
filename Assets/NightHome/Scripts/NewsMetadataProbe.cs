using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.StringLoading;

// Downloads are diagnostic only; arbitrary downloaded strings cannot become VRCUrl.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class NewsMetadataProbe : UdonSharpBehaviour
{
    public VRCUrl endpoint;
    public Text output;
    private int retries;
    private bool busy;
    private float nextAllowed;
    public void Fetch()
    {
        if (busy || Time.time < nextAllowed) return;
        if (endpoint == null || string.IsNullOrEmpty(endpoint.Get())) { Show("Metadata endpoint is not configured"); return; }
        busy = true;
        nextAllowed = Time.time + 6f;
        VRCStringDownloader.LoadUrl(endpoint, this);
    }
    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        busy = false; retries = 0;
        string value = result.Result;
        Show("Received metadata (does not interrupt playback):\n" + value.Substring(0, Mathf.Min(value.Length, 800)));
    }
    public override void OnStringLoadError(IVRCStringDownload result)
    {
        busy = false;
        Show("Metadata error " + result.ErrorCode + ": " + result.Error);
        if (retries >= 3) return;
        retries++;
        SendCustomEventDelayedSeconds(nameof(Fetch), 6f * retries);
    }
    private void Show(string message) { Debug.Log("[NightHome Metadata] " + message); if (output != null) output.text = message; }
}
