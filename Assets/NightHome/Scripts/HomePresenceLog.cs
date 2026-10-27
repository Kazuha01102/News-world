using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HomePresenceLog : UdonSharpBehaviour
{
    public Text display;
    public Text soundLabel;
    public AudioSource sound;
    public AudioClip joined;
    public AudioClip left;
    [HideInInspector] public bool roomMuted;
    [UdonSynced] private bool initialized;
    [UdonSynced] private int sequence;
    [UdonSynced] private string[] lines = new string[32];
    [UdonSynced] private int[] roster = new int[80];
    [UdonSynced] private string[] names = new string[80];
    [UdonSynced] private int lastKind;
    private VRCPlayerApi[] players = new VRCPlayerApi[80];
    private int[] currentIds = new int[80];
    private string[] currentNames = new string[80];
    private float nextPoll;
    private float warmUntil;
    private int seen=-1;
    private bool soundEnabled=true;
    private void Start()
    {
        warmUntil=Time.time+5f;
        for(int i=0;i<32;i++) if(lines[i]==null) lines[i]="";
        for(int i=0;i<80;i++) if(names[i]==null) names[i]="";
        Draw(); ShowSound();
    }
    private void Update()
    {
        if(!Networking.IsNetworkSettled) return;
        if(Networking.IsOwner(gameObject) && Time.time>=nextPoll) {nextPoll=Time.time+1f; Reconcile();}
        if(seen<0) seen=sequence;
        if(seen!=sequence)
        {
            if(sequence>seen && Time.time>warmUntil && soundEnabled && !roomMuted) sound.PlayOneShot(lastKind==1 ? joined : left);
            seen=sequence;
        }
        sound.mute=roomMuted || !soundEnabled;
    }
    private bool Contains(int[] values,int id) { for(int i=0;i<80;i++) if(values[i]==id) return true; return false; }
    private void Reconcile()
    {
        int count=VRCPlayerApi.GetPlayerCount(); VRCPlayerApi.GetPlayers(players);
        for(int i=0;i<80;i++) { currentIds[i]=0; currentNames[i]=""; }
        for(int i=0;i<count && i<80;i++) if(Utilities.IsValid(players[i])) { currentIds[i]=players[i].playerId; currentNames[i]=players[i].displayName; }
        bool changed=false;
        if(!initialized)
        {
            initialized=true;
            // Existing players are a baseline, not 16 simultaneous join events.
            Add(Networking.LocalPlayer.displayName,1);
            changed=true;
        }
        else
        {
            for(int i=0;i<80;i++) if(roster[i]!=0 && !Contains(currentIds,roster[i])) {Add(names[i],2); changed=true;}
            for(int i=0;i<80;i++) if(currentIds[i]!=0 && !Contains(roster,currentIds[i])) {Add(currentNames[i],1); changed=true;}
        }
        for(int i=0;i<80;i++) { roster[i]=currentIds[i]; names[i]=currentNames[i]; }
        if(changed) {RequestSerialization(); Draw();}
    }
    private void Add(string name,int kind)
    {
        for(int i=31;i>0;i--) lines[i]=lines[i-1];
        string safe=name.Replace("\n"," ").Replace("\r"," ");
        if(safe.Length>32) safe=safe.Substring(0,32);
        lines[0]=System.DateTime.UtcNow.AddHours(9).ToString("HH:mm:ss")+"  "+(kind==1?"入室  ":"退室  ")+safe;
        lastKind=kind; sequence++;
    }
    private void Draw()
    {
        if(display==null) return;
        string text="入退室ログ  JST（日本時間 UTC+09:00）\n";
        for(int i=0;i<32;i++) if(!string.IsNullOrEmpty(lines[i])) text+=lines[i]+"\n";
        display.text=text;
    }
    public override void OnDeserialization() { if(seen<0) seen=sequence; Draw(); }
    public override void OnOwnershipTransferred(VRCPlayerApi player) { if(player.isLocal) nextPoll=Time.time+1f; }
    public void ToggleSound() {soundEnabled=!soundEnabled; ShowSound();}
    public void Louder() {sound.volume=Mathf.Clamp(sound.volume+.05f,0,.5f); ShowSound();}
    public void Quieter() {sound.volume=Mathf.Clamp(sound.volume-.05f,0,.5f); ShowSound();}
    private void ShowSound() {if(soundLabel!=null) soundLabel.text="入退室音："+(soundEnabled?"ON":"OFF")+"  "+Mathf.RoundToInt(sound.volume*100)+"%（自分だけ）";}
}
