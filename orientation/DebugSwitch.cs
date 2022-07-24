
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class DebugSwitch : UdonSharpBehaviour
{
    private bool _enable = false;
    [SerializeField] private Orientation _orientation;
    [SerializeField] private Material _onMaterial;
    [SerializeField] private Material _offMaterial;
    void Start()
    {
        SetEnable();
    }
    public override void Interact()
    {
        _enable = !_enable;
        SetEnable();
    }
    private void SetEnable()
    {
        if (_enable) {
            this.gameObject.GetComponent<Renderer>().material = _onMaterial;
        } else {
            this.gameObject.GetComponent<Renderer>().material = _offMaterial;
        }
        _orientation.InteractDebugSwitch(_enable);
    }
}
