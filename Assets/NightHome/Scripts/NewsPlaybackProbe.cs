using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Components.Video;

// A live-stream probe: shared intent, independent client decoding and volume.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class NewsPlaybackProbe : UdonSharpBehaviour
{
    public VRCAVProVideoPlayer player;
    public VRCUrlInputField urlInput;
    public AudioSource speaker;
    public Text status;
    public VRCUrl primary;
    public VRCUrl fallback;
    [HideInInspector] public bool roomMuted;
    private bool userMuted;
    [UdonSynced] private VRCUrl sharedUrl;
    [UdonSynced] private bool sharedPlaying;
    [UdonSynced] private int revision;
    private int appliedRevision = -1;
    private VRCUrl loadedUrl;
    private bool queued;
    private bool ready;
    private int attempts;
    private float nextLoad;
    private float retryAt;

    private void LateUpdate() { if (speaker != null) speaker.mute = userMuted || roomMuted; }

    private void Update()
    {
        if (!Networking.IsNetworkSettled) return;
        // Only the first owner initializes. A join event never changes playback.
        if (revision == 0 && Networking.IsOwner(gameObject)) Change(primary, true);
        if (revision > 0 && revision != appliedRevision) ApplyState();
        if (queued && sharedPlaying && Time.time >= nextLoad && Time.time >= retryAt)
        {
            queued = false;
            ready = false;
            loadedUrl = sharedUrl;
            nextLoad = Time.time + 6f;
            player.PlayURL(sharedUrl);
            Report("Loading live stream (client test required)");
        }
    }

    private void Change(VRCUrl url, bool play)
    {
        if (!Networking.IsNetworkSettled || !Utilities.IsValid(Networking.LocalPlayer)) return;
        if (play && (url == null || string.IsNullOrEmpty(url.Get()))) { Report("URL is empty"); return; }
        if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
        if (!Networking.IsOwner(gameObject)) { Report("Ownership pending; try again"); return; }
        sharedUrl = url;
        sharedPlaying = play;
        revision++;
        RequestSerialization();
        ApplyState();
    }

    private void ApplyState()
    {
        appliedRevision = revision;
        attempts = 0;
        queued = false;
        if (!sharedPlaying) { player.Stop(); ready = false; Report("Stopped (shared)"); return; }
        // Metadata or ownership changes do not force existing viewers to reload.
        if (ready && loadedUrl != null && sharedUrl != null && loadedUrl.Get() == sharedUrl.Get()) return;
        queued = true;
        retryAt = Time.time;
    }

    public override void OnDeserialization() { if (revision > 0) ApplyState(); }
    public override void OnVideoReady() { if (!sharedPlaying) { player.Stop(); return; } ready = true; attempts = 0; Report("Ready (local client)"); }
    public override void OnVideoStart() { Report("Playing (local client)"); }
    public override void OnVideoEnd() { ready = false; Report("Stream ended; use Reconnect or Fallback"); }
    public override void OnVideoError(VideoError error)
    {
        ready = false;
        if (!sharedPlaying) return;
        if (attempts >= 3) { queued = false; Report("Failed: " + error + "; use Fallback or Manual URL"); return; }
        attempts++;
        retryAt = Time.time + 6f * attempts;
        queued = true;
        Report("Error: " + error + "; retry " + attempts + "/3");
    }
    public void Primary() { Change(primary, true); }
    public void Fallback() { Change(fallback, true); }
    public void Play() { Change(sharedUrl, true); }
    public void Stop() { Change(sharedUrl, false); }
    public void Manual() { if (urlInput != null) Change(urlInput.GetUrl(), true); }
    public void Reconnect() { if (!sharedPlaying) return; attempts = 0; ready = false; retryAt = Time.time; queued = true; Report("Reconnect requested (local)"); }
    public void Mute() { userMuted = !userMuted; speaker.mute = userMuted || roomMuted; ReportAudio(); }
    public void Louder() { speaker.volume = Mathf.Clamp01(speaker.volume + .1f); ReportAudio(); }
    public void Quieter() { speaker.volume = Mathf.Clamp01(speaker.volume - .1f); ReportAudio(); }
    private void ReportAudio() { Report("Local volume=" + Mathf.RoundToInt(speaker.volume * 100f) + "% mute=" + userMuted + " quietRoom=" + roomMuted); }
    private void Report(string message) { Debug.Log("[NightHome News] " + message); if (status != null) status.text = message; }
}
