using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class HomeLocalMirror : UdonSharpBehaviour
{
    public GameObject mirror;
    public Text label;
    private void Start() { mirror.SetActive(false); Show(); }
    public override void Interact() { mirror.SetActive(!mirror.activeSelf); Show(); }
    public void Toggle() { Interact(); }
    private void Show() { if(label != null) label.text = mirror.activeSelf ? "ミラー ON（自分だけ）" : "ミラー OFF ／ Useで切替"; }
}
