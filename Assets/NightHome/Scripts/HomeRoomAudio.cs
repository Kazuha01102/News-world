using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class HomeRoomAudio : UdonSharpBehaviour
{
    public BoxCollider room;
    public NewsPlaybackProbe television;
    public HomePresenceLog presence;
    public Text occupancy;
    public float normalGain = 15f;
    public float normalFar = 25f;
    private VRCPlayerApi[] players = new VRCPlayerApi[80];
    private int[] ids = new int[80];
    private bool[] inside = new bool[80];
    private float nextPoll;
    private bool localInside;
    private void Update() { if(Time.time >= nextPoll) { nextPoll=Time.time+.25f; Poll(); } }
    private bool IsInside(Vector3 point, bool previous)
    {
        Vector3 p=room.transform.InverseTransformPoint(point)-room.center;
        Vector3 half=room.size*.5f;
        float margin=previous ? .08f : -.08f;
        return Mathf.Abs(p.x)<half.x+margin && Mathf.Abs(p.z)<half.z+margin && Mathf.Abs(p.y)<half.y+.2f;
    }
    private void Poll()
    {
        if(!Utilities.IsValid(Networking.LocalPlayer)) return;
        localInside=IsInside(Networking.LocalPlayer.GetPosition(),localInside);
        int count=VRCPlayerApi.GetPlayerCount();
        VRCPlayerApi.GetPlayers(players);
        int occupants=0;
        for(int i=0;i<count && i<80;i++)
        {
            var p=players[i]; if(!Utilities.IsValid(p)) continue;
            if(ids[i]!=p.playerId) { ids[i]=p.playerId; inside[i]=false; }
            inside[i]=p.isLocal ? localInside : IsInside(p.GetPosition(),inside[i]);
            if(inside[i]) occupants++;
            if(p.isLocal) continue;
            bool separated=localInside!=inside[i];
            p.SetVoiceDistanceNear(0f);
            p.SetVoiceGain(normalGain);
            p.SetVoiceDistanceFar(separated ? 0f : normalFar);
        }
        if(television!=null) television.roomMuted=localInside;
        if(presence!=null) presence.roomMuted=localInside;
        if(occupancy!=null) occupancy.text="静かな個室  "+occupants+" / 6人目安\n"+(localInside ? "室外の声・テレビ・入退室音を抑制中" : "室内外の声を分離します");
    }
    public override void OnPlayerRespawn(VRCPlayerApi player) { if(player.isLocal) Poll(); }
    public override void OnPlayerLeft(VRCPlayerApi player) { nextPoll=0; }
    private void OnDisable()
    {
        for(int i=0;i<80;i++) if(Utilities.IsValid(players[i]) && !players[i].isLocal) { players[i].SetVoiceGain(normalGain); players[i].SetVoiceDistanceNear(0); players[i].SetVoiceDistanceFar(normalFar); }
        if(television!=null) television.roomMuted=false;
        if(presence!=null) presence.roomMuted=false;
    }
}
