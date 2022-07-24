
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StaffSetting : UdonSharpBehaviour
{
    [SerializeField] private string[] _staffNames;
    [SerializeField] private GameObject[] _staffActiveObjects;
    private const int PLAYER_MAX = 100;

    //================================
    //  外部関数
    //================================
    public bool IsStaff(string displayName)
    {
        bool ret = false;
        for (int i=0; i<_staffNames.Length; i++) {
            if (_staffNames[i] == displayName) {
                ret = true;
                break;
            }
        }
        return ret;
    }

    //================================
    //  内部関数
    //================================
    void Start()
    {
        if (IsStaff(Networking.LocalPlayer.displayName)) {
            for (int i=0; i<_staffActiveObjects.Length; i++) {
                _staffActiveObjects[i].SetActive(true);
            }
        } else {
            for (int i=0; i<_staffActiveObjects.Length; i++) {
                _staffActiveObjects[i].SetActive(false);
            }
        }
    }
}
