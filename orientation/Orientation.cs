
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Orientation : UdonSharpBehaviour
{
    [SerializeField] private float _measureInterval = 10.0f;
    [SerializeField] private float _syncInterval = 60.0f;
    [SerializeField] private GameObject _teleportTo;
    [SerializeField] private GameObject _logPanel;
    [SerializeField] private StaffSetting _staffSetting;
    private float _elapseMeasureTime = 0.0f;
    private float _elapseSyncTime = 0.0f;
    private const int PLAYER_MAX = 100;
    private bool _debug = false;
    [UdonSynced(UdonSyncMode.None)] private int[] _playerIds = new int[PLAYER_MAX];
    private bool _isAgreeed = false;
    private bool _found = false;
    private bool _isStaff = false;

    // のべプレイヤー数の最大人数（＝PlayerIdの最大値）
    // これを超えるPlayerIdの場合は測定は行われない
    private const int PLAYER_ID_MAX = 500;
    private const string VERSION = "1.0.0";

    void Start()
    {
        _isStaff = _staffSetting.IsStaff(Networking.LocalPlayer.displayName);
        SetDebugObject();
        if (_isStaff) {
            Debug.Log("[Player Position Version]" + VERSION);
        }
    }
    private void Update()
    {
        _elapseSyncTime += Time.deltaTime;
        if (_elapseSyncTime > _syncInterval) {
            _found = false;
            if (_isAgreeed) {
                for (int i=0; i<PLAYER_MAX; i++) {
                    if (_playerIds[i] == Networking.LocalPlayer.playerId) {
                        _found = true;
                        break;
                    }
                }
                if (_found) {
                    // 既存。何もしない。
                } else {
                    // 該当無し。新規登録する。
                    SetMeasureOn();
                }
            } else {
                for (int i=0; i<PLAYER_MAX; i++) {
                    if (_playerIds[i] == Networking.LocalPlayer.playerId) {
                        _found = true;
                        break;
                    }
                }
                if (_found) {
                    // 既存。登録削除する。
                    SetMeasureOff();
                } else {
                    // 該当無し。何もしない。
                }
            }
            _elapseSyncTime = 0.0f;
        }

        if (_isStaff) {
            _elapseMeasureTime += Time.deltaTime;
            if (_elapseMeasureTime > _measureInterval) {
                for (int i=0; i<PLAYER_MAX; i++) {
                    if (_playerIds[i] != 0) {
                        var player = VRCPlayerApi.GetPlayerById(_playerIds[i]);
                        if (player != null) {
                            string EscapedDisplayName = player.displayName.Replace("\"","\"\"");
                            Debug.Log("[Player Position]" + player.playerId + ",\"" + EscapedDisplayName + "\"," + player.GetPosition().x + "," + player.GetPosition().y + "," + player.GetPosition().z + "," + player.GetRotation().eulerAngles.x + "," + player.GetRotation().eulerAngles.y + "," + player.GetRotation().eulerAngles.z + "," + player.GetVelocity().x + "," + player.GetVelocity().y + "," + player.GetVelocity().z + "," + player.IsUserInVR());
                        } else {
                            // 念のため、ここでクリーニング
                            if (Networking.LocalPlayer.IsOwner(this.gameObject)) {
                                SetMeasureOffOwner(_playerIds[i]);
                            }
                        }
                    }
                }
                _elapseMeasureTime = 0.0f;
            }

            if (_debug) {
                UnityEngine.UI.Text log1 = _logPanel.transform.Find("Canvas/Panel/Text1").GetComponent<UnityEngine.UI.Text>();
                log1.text = "";
                for (int i=PLAYER_MAX/2-1; i>=0; i--) { // 49 - 0
                    if (_playerIds[i] != 0) {
                        var player = VRCPlayerApi.GetPlayerById(_playerIds[i]);
                        if (player != null) {
                            log1.text += "[" + i + "]" + player.playerId + ",\"" + player.displayName + "\"," + player.GetPosition().x + "," + player.GetPosition().y + "," + player.GetPosition().z + "," + player.GetRotation().eulerAngles.x + "," + player.GetRotation().eulerAngles.y + "," + player.GetRotation().eulerAngles.z + "," + player.GetVelocity().x + "," + player.GetVelocity().y + "," + player.GetVelocity().z + "," + player.IsUserInVR() + "\n";
                        } else {
                            log1.text += "[" + i + "]" + "\n";
                        }
                    } else {
                        log1.text += "[" + i + "]" + "\n";
                    }
                }
                UnityEngine.UI.Text log2 = _logPanel.transform.Find("Canvas/Panel/Text2").GetComponent<UnityEngine.UI.Text>();
                log2.text = "";
                for (int i=PLAYER_MAX-1; i>=PLAYER_MAX/2; i--) { // 99 - 50
                    if (_playerIds[i] != 0) {
                        var player = VRCPlayerApi.GetPlayerById(_playerIds[i]);
                        if (player != null) {
                            log2.text += "[" + i + "]" + player.playerId + ",\"" + player.displayName + "\"," + player.GetPosition().x + "," + player.GetPosition().y + "," + player.GetPosition().z + "," + player.GetRotation().eulerAngles.x + "," + player.GetRotation().eulerAngles.y + "," + player.GetRotation().eulerAngles.z + "," + player.GetVelocity().x + "," + player.GetVelocity().y + "," + player.GetVelocity().z + "," + player.IsUserInVR() + "\n";
                        } else {
                            log2.text += "[" + i + "]" + "\n";
                        }
                    } else {
                        log2.text += "[" + i + "]" + "\n";
                    }
                }
            }
        }

    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Networking.LocalPlayer.IsOwner(this.gameObject)) {
            SetMeasureOffOwner(player.playerId);
        }
    }

    // =================================
    // デバッグ
    // =================================
    public void InteractDebugSwitch(bool enable) {
        _debug = enable;
        SetDebugObject();
    }
    public void SetDebugObject() {
        _logPanel.transform.Find("Canvas").gameObject.SetActive(_debug);
    }

    // =================================
    // ボタン操作
    // =================================
    public void PressedOK()
    {
        _isAgreeed = true;
        // 初回同期を行う
        _elapseSyncTime = _syncInterval;
        if (_teleportTo != null) {
            Networking.LocalPlayer.TeleportTo(_teleportTo.transform.position, _teleportTo.transform.rotation);
        }
    }
    public void PressedNG()
    {
        _isAgreeed = false;
        // 初回同期を行う
        _elapseSyncTime = _syncInterval;
        if (_teleportTo != null) {
            Networking.LocalPlayer.TeleportTo(_teleportTo.transform.position, _teleportTo.transform.rotation);
        }
    }

    // =================================
    // 測定フラグ更新 Owner処理
    // =================================
    private void SetMeasureOn()
    {
        if (Networking.LocalPlayer.IsOwner(this.gameObject)) {
            SetMeasureOnOwner(Networking.LocalPlayer.playerId);
        } else {
            SetMeasureOnEventToOwner(Networking.LocalPlayer.playerId);
        }
    }
    private void SetMeasureOff()
    {
        if (Networking.LocalPlayer.IsOwner(this.gameObject)) {
            SetMeasureOffOwner(Networking.LocalPlayer.playerId);
        } else {
            SetMeasureOffEventToOwner(Networking.LocalPlayer.playerId);
        }
    }

    private void SetMeasureOnOwner(int PlayerId)
    {
        int found = -1;
        int empty = -1;
        for (int i=0; i<PLAYER_MAX; i++) {
            if (found == -1) {
                if (_playerIds[i] == PlayerId) {
                    found = i;
                }
            }
            if (empty == -1) {
                if (_playerIds[i] == 0) {
                    empty = i;
                }
            }
            if (found != -1) {
                break;
            }
        }

        if (found != -1) {
            // 既存なので何もしない
        } else {
            // 新規なのでセットが必要
            if (empty != -1) {
                // 空き有り。自PlayerIdをセットする
                _playerIds[empty] = PlayerId;
                RequestSerialization();
            } else {
                // 空き無し。人数的に想定外なので何もしない
            }
        }
    }
    private void SetMeasureOffOwner(int PlayerId)
    {
        for (int i=0; i<PLAYER_MAX; i++) {
            if (_playerIds[i] == PlayerId) {
                _playerIds[i] = 0;
                RequestSerialization();
            }
        }
    }

    // =================================
    // 測定フラグON更新 非Owner処理
    // =================================
    private void SetMeasureOnEventToOwner(int PlayerId)
    {
        switch(PlayerId) {
            case 1: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId1)); break;
            case 2: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId2)); break;
            case 3: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId3)); break;
            case 4: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId4)); break;
            case 5: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId5)); break;
            case 6: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId6)); break;
            case 7: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId7)); break;
            case 8: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId8)); break;
            case 9: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId9)); break;
            case 10: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId10)); break;
            case 11: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId11)); break;
            case 12: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId12)); break;
            case 13: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId13)); break;
            case 14: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId14)); break;
            case 15: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId15)); break;
            case 16: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId16)); break;
            case 17: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId17)); break;
            case 18: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId18)); break;
            case 19: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId19)); break;
            case 20: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId20)); break;
            case 21: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId21)); break;
            case 22: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId22)); break;
            case 23: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId23)); break;
            case 24: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId24)); break;
            case 25: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId25)); break;
            case 26: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId26)); break;
            case 27: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId27)); break;
            case 28: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId28)); break;
            case 29: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId29)); break;
            case 30: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId30)); break;
            case 31: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId31)); break;
            case 32: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId32)); break;
            case 33: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId33)); break;
            case 34: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId34)); break;
            case 35: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId35)); break;
            case 36: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId36)); break;
            case 37: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId37)); break;
            case 38: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId38)); break;
            case 39: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId39)); break;
            case 40: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId40)); break;
            case 41: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId41)); break;
            case 42: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId42)); break;
            case 43: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId43)); break;
            case 44: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId44)); break;
            case 45: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId45)); break;
            case 46: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId46)); break;
            case 47: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId47)); break;
            case 48: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId48)); break;
            case 49: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId49)); break;
            case 50: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId50)); break;
            case 51: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId51)); break;
            case 52: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId52)); break;
            case 53: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId53)); break;
            case 54: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId54)); break;
            case 55: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId55)); break;
            case 56: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId56)); break;
            case 57: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId57)); break;
            case 58: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId58)); break;
            case 59: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId59)); break;
            case 60: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId60)); break;
            case 61: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId61)); break;
            case 62: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId62)); break;
            case 63: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId63)); break;
            case 64: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId64)); break;
            case 65: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId65)); break;
            case 66: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId66)); break;
            case 67: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId67)); break;
            case 68: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId68)); break;
            case 69: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId69)); break;
            case 70: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId70)); break;
            case 71: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId71)); break;
            case 72: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId72)); break;
            case 73: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId73)); break;
            case 74: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId74)); break;
            case 75: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId75)); break;
            case 76: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId76)); break;
            case 77: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId77)); break;
            case 78: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId78)); break;
            case 79: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId79)); break;
            case 80: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId80)); break;
            case 81: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId81)); break;
            case 82: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId82)); break;
            case 83: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId83)); break;
            case 84: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId84)); break;
            case 85: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId85)); break;
            case 86: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId86)); break;
            case 87: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId87)); break;
            case 88: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId88)); break;
            case 89: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId89)); break;
            case 90: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId90)); break;
            case 91: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId91)); break;
            case 92: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId92)); break;
            case 93: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId93)); break;
            case 94: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId94)); break;
            case 95: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId95)); break;
            case 96: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId96)); break;
            case 97: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId97)); break;
            case 98: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId98)); break;
            case 99: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId99)); break;
            case 100: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId100)); break;
            case 101: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId101)); break;
            case 102: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId102)); break;
            case 103: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId103)); break;
            case 104: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId104)); break;
            case 105: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId105)); break;
            case 106: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId106)); break;
            case 107: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId107)); break;
            case 108: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId108)); break;
            case 109: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId109)); break;
            case 110: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId110)); break;
            case 111: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId111)); break;
            case 112: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId112)); break;
            case 113: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId113)); break;
            case 114: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId114)); break;
            case 115: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId115)); break;
            case 116: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId116)); break;
            case 117: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId117)); break;
            case 118: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId118)); break;
            case 119: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId119)); break;
            case 120: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId120)); break;
            case 121: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId121)); break;
            case 122: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId122)); break;
            case 123: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId123)); break;
            case 124: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId124)); break;
            case 125: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId125)); break;
            case 126: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId126)); break;
            case 127: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId127)); break;
            case 128: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId128)); break;
            case 129: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId129)); break;
            case 130: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId130)); break;
            case 131: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId131)); break;
            case 132: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId132)); break;
            case 133: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId133)); break;
            case 134: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId134)); break;
            case 135: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId135)); break;
            case 136: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId136)); break;
            case 137: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId137)); break;
            case 138: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId138)); break;
            case 139: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId139)); break;
            case 140: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId140)); break;
            case 141: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId141)); break;
            case 142: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId142)); break;
            case 143: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId143)); break;
            case 144: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId144)); break;
            case 145: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId145)); break;
            case 146: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId146)); break;
            case 147: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId147)); break;
            case 148: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId148)); break;
            case 149: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId149)); break;
            case 150: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId150)); break;
            case 151: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId151)); break;
            case 152: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId152)); break;
            case 153: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId153)); break;
            case 154: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId154)); break;
            case 155: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId155)); break;
            case 156: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId156)); break;
            case 157: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId157)); break;
            case 158: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId158)); break;
            case 159: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId159)); break;
            case 160: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId160)); break;
            case 161: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId161)); break;
            case 162: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId162)); break;
            case 163: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId163)); break;
            case 164: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId164)); break;
            case 165: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId165)); break;
            case 166: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId166)); break;
            case 167: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId167)); break;
            case 168: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId168)); break;
            case 169: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId169)); break;
            case 170: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId170)); break;
            case 171: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId171)); break;
            case 172: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId172)); break;
            case 173: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId173)); break;
            case 174: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId174)); break;
            case 175: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId175)); break;
            case 176: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId176)); break;
            case 177: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId177)); break;
            case 178: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId178)); break;
            case 179: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId179)); break;
            case 180: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId180)); break;
            case 181: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId181)); break;
            case 182: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId182)); break;
            case 183: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId183)); break;
            case 184: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId184)); break;
            case 185: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId185)); break;
            case 186: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId186)); break;
            case 187: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId187)); break;
            case 188: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId188)); break;
            case 189: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId189)); break;
            case 190: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId190)); break;
            case 191: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId191)); break;
            case 192: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId192)); break;
            case 193: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId193)); break;
            case 194: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId194)); break;
            case 195: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId195)); break;
            case 196: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId196)); break;
            case 197: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId197)); break;
            case 198: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId198)); break;
            case 199: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId199)); break;
            case 200: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId200)); break;
            case 201: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId201)); break;
            case 202: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId202)); break;
            case 203: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId203)); break;
            case 204: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId204)); break;
            case 205: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId205)); break;
            case 206: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId206)); break;
            case 207: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId207)); break;
            case 208: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId208)); break;
            case 209: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId209)); break;
            case 210: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId210)); break;
            case 211: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId211)); break;
            case 212: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId212)); break;
            case 213: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId213)); break;
            case 214: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId214)); break;
            case 215: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId215)); break;
            case 216: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId216)); break;
            case 217: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId217)); break;
            case 218: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId218)); break;
            case 219: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId219)); break;
            case 220: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId220)); break;
            case 221: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId221)); break;
            case 222: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId222)); break;
            case 223: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId223)); break;
            case 224: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId224)); break;
            case 225: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId225)); break;
            case 226: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId226)); break;
            case 227: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId227)); break;
            case 228: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId228)); break;
            case 229: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId229)); break;
            case 230: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId230)); break;
            case 231: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId231)); break;
            case 232: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId232)); break;
            case 233: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId233)); break;
            case 234: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId234)); break;
            case 235: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId235)); break;
            case 236: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId236)); break;
            case 237: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId237)); break;
            case 238: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId238)); break;
            case 239: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId239)); break;
            case 240: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId240)); break;
            case 241: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId241)); break;
            case 242: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId242)); break;
            case 243: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId243)); break;
            case 244: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId244)); break;
            case 245: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId245)); break;
            case 246: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId246)); break;
            case 247: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId247)); break;
            case 248: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId248)); break;
            case 249: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId249)); break;
            case 250: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId250)); break;
            case 251: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId251)); break;
            case 252: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId252)); break;
            case 253: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId253)); break;
            case 254: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId254)); break;
            case 255: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId255)); break;
            case 256: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId256)); break;
            case 257: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId257)); break;
            case 258: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId258)); break;
            case 259: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId259)); break;
            case 260: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId260)); break;
            case 261: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId261)); break;
            case 262: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId262)); break;
            case 263: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId263)); break;
            case 264: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId264)); break;
            case 265: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId265)); break;
            case 266: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId266)); break;
            case 267: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId267)); break;
            case 268: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId268)); break;
            case 269: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId269)); break;
            case 270: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId270)); break;
            case 271: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId271)); break;
            case 272: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId272)); break;
            case 273: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId273)); break;
            case 274: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId274)); break;
            case 275: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId275)); break;
            case 276: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId276)); break;
            case 277: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId277)); break;
            case 278: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId278)); break;
            case 279: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId279)); break;
            case 280: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId280)); break;
            case 281: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId281)); break;
            case 282: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId282)); break;
            case 283: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId283)); break;
            case 284: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId284)); break;
            case 285: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId285)); break;
            case 286: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId286)); break;
            case 287: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId287)); break;
            case 288: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId288)); break;
            case 289: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId289)); break;
            case 290: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId290)); break;
            case 291: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId291)); break;
            case 292: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId292)); break;
            case 293: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId293)); break;
            case 294: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId294)); break;
            case 295: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId295)); break;
            case 296: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId296)); break;
            case 297: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId297)); break;
            case 298: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId298)); break;
            case 299: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId299)); break;
            case 300: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId300)); break;
            case 301: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId301)); break;
            case 302: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId302)); break;
            case 303: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId303)); break;
            case 304: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId304)); break;
            case 305: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId305)); break;
            case 306: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId306)); break;
            case 307: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId307)); break;
            case 308: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId308)); break;
            case 309: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId309)); break;
            case 310: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId310)); break;
            case 311: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId311)); break;
            case 312: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId312)); break;
            case 313: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId313)); break;
            case 314: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId314)); break;
            case 315: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId315)); break;
            case 316: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId316)); break;
            case 317: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId317)); break;
            case 318: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId318)); break;
            case 319: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId319)); break;
            case 320: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId320)); break;
            case 321: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId321)); break;
            case 322: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId322)); break;
            case 323: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId323)); break;
            case 324: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId324)); break;
            case 325: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId325)); break;
            case 326: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId326)); break;
            case 327: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId327)); break;
            case 328: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId328)); break;
            case 329: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId329)); break;
            case 330: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId330)); break;
            case 331: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId331)); break;
            case 332: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId332)); break;
            case 333: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId333)); break;
            case 334: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId334)); break;
            case 335: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId335)); break;
            case 336: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId336)); break;
            case 337: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId337)); break;
            case 338: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId338)); break;
            case 339: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId339)); break;
            case 340: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId340)); break;
            case 341: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId341)); break;
            case 342: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId342)); break;
            case 343: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId343)); break;
            case 344: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId344)); break;
            case 345: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId345)); break;
            case 346: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId346)); break;
            case 347: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId347)); break;
            case 348: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId348)); break;
            case 349: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId349)); break;
            case 350: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId350)); break;
            case 351: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId351)); break;
            case 352: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId352)); break;
            case 353: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId353)); break;
            case 354: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId354)); break;
            case 355: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId355)); break;
            case 356: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId356)); break;
            case 357: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId357)); break;
            case 358: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId358)); break;
            case 359: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId359)); break;
            case 360: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId360)); break;
            case 361: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId361)); break;
            case 362: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId362)); break;
            case 363: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId363)); break;
            case 364: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId364)); break;
            case 365: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId365)); break;
            case 366: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId366)); break;
            case 367: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId367)); break;
            case 368: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId368)); break;
            case 369: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId369)); break;
            case 370: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId370)); break;
            case 371: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId371)); break;
            case 372: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId372)); break;
            case 373: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId373)); break;
            case 374: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId374)); break;
            case 375: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId375)); break;
            case 376: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId376)); break;
            case 377: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId377)); break;
            case 378: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId378)); break;
            case 379: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId379)); break;
            case 380: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId380)); break;
            case 381: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId381)); break;
            case 382: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId382)); break;
            case 383: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId383)); break;
            case 384: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId384)); break;
            case 385: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId385)); break;
            case 386: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId386)); break;
            case 387: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId387)); break;
            case 388: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId388)); break;
            case 389: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId389)); break;
            case 390: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId390)); break;
            case 391: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId391)); break;
            case 392: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId392)); break;
            case 393: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId393)); break;
            case 394: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId394)); break;
            case 395: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId395)); break;
            case 396: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId396)); break;
            case 397: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId397)); break;
            case 398: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId398)); break;
            case 399: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId399)); break;
            case 400: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId400)); break;
            case 401: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId401)); break;
            case 402: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId402)); break;
            case 403: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId403)); break;
            case 404: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId404)); break;
            case 405: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId405)); break;
            case 406: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId406)); break;
            case 407: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId407)); break;
            case 408: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId408)); break;
            case 409: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId409)); break;
            case 410: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId410)); break;
            case 411: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId411)); break;
            case 412: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId412)); break;
            case 413: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId413)); break;
            case 414: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId414)); break;
            case 415: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId415)); break;
            case 416: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId416)); break;
            case 417: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId417)); break;
            case 418: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId418)); break;
            case 419: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId419)); break;
            case 420: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId420)); break;
            case 421: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId421)); break;
            case 422: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId422)); break;
            case 423: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId423)); break;
            case 424: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId424)); break;
            case 425: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId425)); break;
            case 426: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId426)); break;
            case 427: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId427)); break;
            case 428: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId428)); break;
            case 429: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId429)); break;
            case 430: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId430)); break;
            case 431: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId431)); break;
            case 432: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId432)); break;
            case 433: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId433)); break;
            case 434: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId434)); break;
            case 435: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId435)); break;
            case 436: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId436)); break;
            case 437: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId437)); break;
            case 438: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId438)); break;
            case 439: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId439)); break;
            case 440: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId440)); break;
            case 441: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId441)); break;
            case 442: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId442)); break;
            case 443: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId443)); break;
            case 444: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId444)); break;
            case 445: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId445)); break;
            case 446: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId446)); break;
            case 447: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId447)); break;
            case 448: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId448)); break;
            case 449: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId449)); break;
            case 450: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId450)); break;
            case 451: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId451)); break;
            case 452: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId452)); break;
            case 453: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId453)); break;
            case 454: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId454)); break;
            case 455: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId455)); break;
            case 456: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId456)); break;
            case 457: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId457)); break;
            case 458: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId458)); break;
            case 459: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId459)); break;
            case 460: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId460)); break;
            case 461: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId461)); break;
            case 462: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId462)); break;
            case 463: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId463)); break;
            case 464: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId464)); break;
            case 465: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId465)); break;
            case 466: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId466)); break;
            case 467: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId467)); break;
            case 468: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId468)); break;
            case 469: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId469)); break;
            case 470: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId470)); break;
            case 471: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId471)); break;
            case 472: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId472)); break;
            case 473: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId473)); break;
            case 474: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId474)); break;
            case 475: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId475)); break;
            case 476: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId476)); break;
            case 477: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId477)); break;
            case 478: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId478)); break;
            case 479: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId479)); break;
            case 480: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId480)); break;
            case 481: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId481)); break;
            case 482: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId482)); break;
            case 483: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId483)); break;
            case 484: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId484)); break;
            case 485: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId485)); break;
            case 486: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId486)); break;
            case 487: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId487)); break;
            case 488: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId488)); break;
            case 489: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId489)); break;
            case 490: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId490)); break;
            case 491: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId491)); break;
            case 492: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId492)); break;
            case 493: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId493)); break;
            case 494: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId494)); break;
            case 495: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId495)); break;
            case 496: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId496)); break;
            case 497: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId497)); break;
            case 498: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId498)); break;
            case 499: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId499)); break;
            case 500: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOnByPlayerId500)); break;
            default: break;
        }

    }
    public void SetMeasureOnByPlayerId1() { SetMeasureOnOwner(1); }
    public void SetMeasureOnByPlayerId2() { SetMeasureOnOwner(2); }
    public void SetMeasureOnByPlayerId3() { SetMeasureOnOwner(3); }
    public void SetMeasureOnByPlayerId4() { SetMeasureOnOwner(4); }
    public void SetMeasureOnByPlayerId5() { SetMeasureOnOwner(5); }
    public void SetMeasureOnByPlayerId6() { SetMeasureOnOwner(6); }
    public void SetMeasureOnByPlayerId7() { SetMeasureOnOwner(7); }
    public void SetMeasureOnByPlayerId8() { SetMeasureOnOwner(8); }
    public void SetMeasureOnByPlayerId9() { SetMeasureOnOwner(9); }
    public void SetMeasureOnByPlayerId10() { SetMeasureOnOwner(10); }
    public void SetMeasureOnByPlayerId11() { SetMeasureOnOwner(11); }
    public void SetMeasureOnByPlayerId12() { SetMeasureOnOwner(12); }
    public void SetMeasureOnByPlayerId13() { SetMeasureOnOwner(13); }
    public void SetMeasureOnByPlayerId14() { SetMeasureOnOwner(14); }
    public void SetMeasureOnByPlayerId15() { SetMeasureOnOwner(15); }
    public void SetMeasureOnByPlayerId16() { SetMeasureOnOwner(16); }
    public void SetMeasureOnByPlayerId17() { SetMeasureOnOwner(17); }
    public void SetMeasureOnByPlayerId18() { SetMeasureOnOwner(18); }
    public void SetMeasureOnByPlayerId19() { SetMeasureOnOwner(19); }
    public void SetMeasureOnByPlayerId20() { SetMeasureOnOwner(20); }
    public void SetMeasureOnByPlayerId21() { SetMeasureOnOwner(21); }
    public void SetMeasureOnByPlayerId22() { SetMeasureOnOwner(22); }
    public void SetMeasureOnByPlayerId23() { SetMeasureOnOwner(23); }
    public void SetMeasureOnByPlayerId24() { SetMeasureOnOwner(24); }
    public void SetMeasureOnByPlayerId25() { SetMeasureOnOwner(25); }
    public void SetMeasureOnByPlayerId26() { SetMeasureOnOwner(26); }
    public void SetMeasureOnByPlayerId27() { SetMeasureOnOwner(27); }
    public void SetMeasureOnByPlayerId28() { SetMeasureOnOwner(28); }
    public void SetMeasureOnByPlayerId29() { SetMeasureOnOwner(29); }
    public void SetMeasureOnByPlayerId30() { SetMeasureOnOwner(30); }
    public void SetMeasureOnByPlayerId31() { SetMeasureOnOwner(31); }
    public void SetMeasureOnByPlayerId32() { SetMeasureOnOwner(32); }
    public void SetMeasureOnByPlayerId33() { SetMeasureOnOwner(33); }
    public void SetMeasureOnByPlayerId34() { SetMeasureOnOwner(34); }
    public void SetMeasureOnByPlayerId35() { SetMeasureOnOwner(35); }
    public void SetMeasureOnByPlayerId36() { SetMeasureOnOwner(36); }
    public void SetMeasureOnByPlayerId37() { SetMeasureOnOwner(37); }
    public void SetMeasureOnByPlayerId38() { SetMeasureOnOwner(38); }
    public void SetMeasureOnByPlayerId39() { SetMeasureOnOwner(39); }
    public void SetMeasureOnByPlayerId40() { SetMeasureOnOwner(40); }
    public void SetMeasureOnByPlayerId41() { SetMeasureOnOwner(41); }
    public void SetMeasureOnByPlayerId42() { SetMeasureOnOwner(42); }
    public void SetMeasureOnByPlayerId43() { SetMeasureOnOwner(43); }
    public void SetMeasureOnByPlayerId44() { SetMeasureOnOwner(44); }
    public void SetMeasureOnByPlayerId45() { SetMeasureOnOwner(45); }
    public void SetMeasureOnByPlayerId46() { SetMeasureOnOwner(46); }
    public void SetMeasureOnByPlayerId47() { SetMeasureOnOwner(47); }
    public void SetMeasureOnByPlayerId48() { SetMeasureOnOwner(48); }
    public void SetMeasureOnByPlayerId49() { SetMeasureOnOwner(49); }
    public void SetMeasureOnByPlayerId50() { SetMeasureOnOwner(50); }
    public void SetMeasureOnByPlayerId51() { SetMeasureOnOwner(51); }
    public void SetMeasureOnByPlayerId52() { SetMeasureOnOwner(52); }
    public void SetMeasureOnByPlayerId53() { SetMeasureOnOwner(53); }
    public void SetMeasureOnByPlayerId54() { SetMeasureOnOwner(54); }
    public void SetMeasureOnByPlayerId55() { SetMeasureOnOwner(55); }
    public void SetMeasureOnByPlayerId56() { SetMeasureOnOwner(56); }
    public void SetMeasureOnByPlayerId57() { SetMeasureOnOwner(57); }
    public void SetMeasureOnByPlayerId58() { SetMeasureOnOwner(58); }
    public void SetMeasureOnByPlayerId59() { SetMeasureOnOwner(59); }
    public void SetMeasureOnByPlayerId60() { SetMeasureOnOwner(60); }
    public void SetMeasureOnByPlayerId61() { SetMeasureOnOwner(61); }
    public void SetMeasureOnByPlayerId62() { SetMeasureOnOwner(62); }
    public void SetMeasureOnByPlayerId63() { SetMeasureOnOwner(63); }
    public void SetMeasureOnByPlayerId64() { SetMeasureOnOwner(64); }
    public void SetMeasureOnByPlayerId65() { SetMeasureOnOwner(65); }
    public void SetMeasureOnByPlayerId66() { SetMeasureOnOwner(66); }
    public void SetMeasureOnByPlayerId67() { SetMeasureOnOwner(67); }
    public void SetMeasureOnByPlayerId68() { SetMeasureOnOwner(68); }
    public void SetMeasureOnByPlayerId69() { SetMeasureOnOwner(69); }
    public void SetMeasureOnByPlayerId70() { SetMeasureOnOwner(70); }
    public void SetMeasureOnByPlayerId71() { SetMeasureOnOwner(71); }
    public void SetMeasureOnByPlayerId72() { SetMeasureOnOwner(72); }
    public void SetMeasureOnByPlayerId73() { SetMeasureOnOwner(73); }
    public void SetMeasureOnByPlayerId74() { SetMeasureOnOwner(74); }
    public void SetMeasureOnByPlayerId75() { SetMeasureOnOwner(75); }
    public void SetMeasureOnByPlayerId76() { SetMeasureOnOwner(76); }
    public void SetMeasureOnByPlayerId77() { SetMeasureOnOwner(77); }
    public void SetMeasureOnByPlayerId78() { SetMeasureOnOwner(78); }
    public void SetMeasureOnByPlayerId79() { SetMeasureOnOwner(79); }
    public void SetMeasureOnByPlayerId80() { SetMeasureOnOwner(80); }
    public void SetMeasureOnByPlayerId81() { SetMeasureOnOwner(81); }
    public void SetMeasureOnByPlayerId82() { SetMeasureOnOwner(82); }
    public void SetMeasureOnByPlayerId83() { SetMeasureOnOwner(83); }
    public void SetMeasureOnByPlayerId84() { SetMeasureOnOwner(84); }
    public void SetMeasureOnByPlayerId85() { SetMeasureOnOwner(85); }
    public void SetMeasureOnByPlayerId86() { SetMeasureOnOwner(86); }
    public void SetMeasureOnByPlayerId87() { SetMeasureOnOwner(87); }
    public void SetMeasureOnByPlayerId88() { SetMeasureOnOwner(88); }
    public void SetMeasureOnByPlayerId89() { SetMeasureOnOwner(89); }
    public void SetMeasureOnByPlayerId90() { SetMeasureOnOwner(90); }
    public void SetMeasureOnByPlayerId91() { SetMeasureOnOwner(91); }
    public void SetMeasureOnByPlayerId92() { SetMeasureOnOwner(92); }
    public void SetMeasureOnByPlayerId93() { SetMeasureOnOwner(93); }
    public void SetMeasureOnByPlayerId94() { SetMeasureOnOwner(94); }
    public void SetMeasureOnByPlayerId95() { SetMeasureOnOwner(95); }
    public void SetMeasureOnByPlayerId96() { SetMeasureOnOwner(96); }
    public void SetMeasureOnByPlayerId97() { SetMeasureOnOwner(97); }
    public void SetMeasureOnByPlayerId98() { SetMeasureOnOwner(98); }
    public void SetMeasureOnByPlayerId99() { SetMeasureOnOwner(99); }
    public void SetMeasureOnByPlayerId100() { SetMeasureOnOwner(100); }
    public void SetMeasureOnByPlayerId101() { SetMeasureOnOwner(101); }
    public void SetMeasureOnByPlayerId102() { SetMeasureOnOwner(102); }
    public void SetMeasureOnByPlayerId103() { SetMeasureOnOwner(103); }
    public void SetMeasureOnByPlayerId104() { SetMeasureOnOwner(104); }
    public void SetMeasureOnByPlayerId105() { SetMeasureOnOwner(105); }
    public void SetMeasureOnByPlayerId106() { SetMeasureOnOwner(106); }
    public void SetMeasureOnByPlayerId107() { SetMeasureOnOwner(107); }
    public void SetMeasureOnByPlayerId108() { SetMeasureOnOwner(108); }
    public void SetMeasureOnByPlayerId109() { SetMeasureOnOwner(109); }
    public void SetMeasureOnByPlayerId110() { SetMeasureOnOwner(110); }
    public void SetMeasureOnByPlayerId111() { SetMeasureOnOwner(111); }
    public void SetMeasureOnByPlayerId112() { SetMeasureOnOwner(112); }
    public void SetMeasureOnByPlayerId113() { SetMeasureOnOwner(113); }
    public void SetMeasureOnByPlayerId114() { SetMeasureOnOwner(114); }
    public void SetMeasureOnByPlayerId115() { SetMeasureOnOwner(115); }
    public void SetMeasureOnByPlayerId116() { SetMeasureOnOwner(116); }
    public void SetMeasureOnByPlayerId117() { SetMeasureOnOwner(117); }
    public void SetMeasureOnByPlayerId118() { SetMeasureOnOwner(118); }
    public void SetMeasureOnByPlayerId119() { SetMeasureOnOwner(119); }
    public void SetMeasureOnByPlayerId120() { SetMeasureOnOwner(120); }
    public void SetMeasureOnByPlayerId121() { SetMeasureOnOwner(121); }
    public void SetMeasureOnByPlayerId122() { SetMeasureOnOwner(122); }
    public void SetMeasureOnByPlayerId123() { SetMeasureOnOwner(123); }
    public void SetMeasureOnByPlayerId124() { SetMeasureOnOwner(124); }
    public void SetMeasureOnByPlayerId125() { SetMeasureOnOwner(125); }
    public void SetMeasureOnByPlayerId126() { SetMeasureOnOwner(126); }
    public void SetMeasureOnByPlayerId127() { SetMeasureOnOwner(127); }
    public void SetMeasureOnByPlayerId128() { SetMeasureOnOwner(128); }
    public void SetMeasureOnByPlayerId129() { SetMeasureOnOwner(129); }
    public void SetMeasureOnByPlayerId130() { SetMeasureOnOwner(130); }
    public void SetMeasureOnByPlayerId131() { SetMeasureOnOwner(131); }
    public void SetMeasureOnByPlayerId132() { SetMeasureOnOwner(132); }
    public void SetMeasureOnByPlayerId133() { SetMeasureOnOwner(133); }
    public void SetMeasureOnByPlayerId134() { SetMeasureOnOwner(134); }
    public void SetMeasureOnByPlayerId135() { SetMeasureOnOwner(135); }
    public void SetMeasureOnByPlayerId136() { SetMeasureOnOwner(136); }
    public void SetMeasureOnByPlayerId137() { SetMeasureOnOwner(137); }
    public void SetMeasureOnByPlayerId138() { SetMeasureOnOwner(138); }
    public void SetMeasureOnByPlayerId139() { SetMeasureOnOwner(139); }
    public void SetMeasureOnByPlayerId140() { SetMeasureOnOwner(140); }
    public void SetMeasureOnByPlayerId141() { SetMeasureOnOwner(141); }
    public void SetMeasureOnByPlayerId142() { SetMeasureOnOwner(142); }
    public void SetMeasureOnByPlayerId143() { SetMeasureOnOwner(143); }
    public void SetMeasureOnByPlayerId144() { SetMeasureOnOwner(144); }
    public void SetMeasureOnByPlayerId145() { SetMeasureOnOwner(145); }
    public void SetMeasureOnByPlayerId146() { SetMeasureOnOwner(146); }
    public void SetMeasureOnByPlayerId147() { SetMeasureOnOwner(147); }
    public void SetMeasureOnByPlayerId148() { SetMeasureOnOwner(148); }
    public void SetMeasureOnByPlayerId149() { SetMeasureOnOwner(149); }
    public void SetMeasureOnByPlayerId150() { SetMeasureOnOwner(150); }
    public void SetMeasureOnByPlayerId151() { SetMeasureOnOwner(151); }
    public void SetMeasureOnByPlayerId152() { SetMeasureOnOwner(152); }
    public void SetMeasureOnByPlayerId153() { SetMeasureOnOwner(153); }
    public void SetMeasureOnByPlayerId154() { SetMeasureOnOwner(154); }
    public void SetMeasureOnByPlayerId155() { SetMeasureOnOwner(155); }
    public void SetMeasureOnByPlayerId156() { SetMeasureOnOwner(156); }
    public void SetMeasureOnByPlayerId157() { SetMeasureOnOwner(157); }
    public void SetMeasureOnByPlayerId158() { SetMeasureOnOwner(158); }
    public void SetMeasureOnByPlayerId159() { SetMeasureOnOwner(159); }
    public void SetMeasureOnByPlayerId160() { SetMeasureOnOwner(160); }
    public void SetMeasureOnByPlayerId161() { SetMeasureOnOwner(161); }
    public void SetMeasureOnByPlayerId162() { SetMeasureOnOwner(162); }
    public void SetMeasureOnByPlayerId163() { SetMeasureOnOwner(163); }
    public void SetMeasureOnByPlayerId164() { SetMeasureOnOwner(164); }
    public void SetMeasureOnByPlayerId165() { SetMeasureOnOwner(165); }
    public void SetMeasureOnByPlayerId166() { SetMeasureOnOwner(166); }
    public void SetMeasureOnByPlayerId167() { SetMeasureOnOwner(167); }
    public void SetMeasureOnByPlayerId168() { SetMeasureOnOwner(168); }
    public void SetMeasureOnByPlayerId169() { SetMeasureOnOwner(169); }
    public void SetMeasureOnByPlayerId170() { SetMeasureOnOwner(170); }
    public void SetMeasureOnByPlayerId171() { SetMeasureOnOwner(171); }
    public void SetMeasureOnByPlayerId172() { SetMeasureOnOwner(172); }
    public void SetMeasureOnByPlayerId173() { SetMeasureOnOwner(173); }
    public void SetMeasureOnByPlayerId174() { SetMeasureOnOwner(174); }
    public void SetMeasureOnByPlayerId175() { SetMeasureOnOwner(175); }
    public void SetMeasureOnByPlayerId176() { SetMeasureOnOwner(176); }
    public void SetMeasureOnByPlayerId177() { SetMeasureOnOwner(177); }
    public void SetMeasureOnByPlayerId178() { SetMeasureOnOwner(178); }
    public void SetMeasureOnByPlayerId179() { SetMeasureOnOwner(179); }
    public void SetMeasureOnByPlayerId180() { SetMeasureOnOwner(180); }
    public void SetMeasureOnByPlayerId181() { SetMeasureOnOwner(181); }
    public void SetMeasureOnByPlayerId182() { SetMeasureOnOwner(182); }
    public void SetMeasureOnByPlayerId183() { SetMeasureOnOwner(183); }
    public void SetMeasureOnByPlayerId184() { SetMeasureOnOwner(184); }
    public void SetMeasureOnByPlayerId185() { SetMeasureOnOwner(185); }
    public void SetMeasureOnByPlayerId186() { SetMeasureOnOwner(186); }
    public void SetMeasureOnByPlayerId187() { SetMeasureOnOwner(187); }
    public void SetMeasureOnByPlayerId188() { SetMeasureOnOwner(188); }
    public void SetMeasureOnByPlayerId189() { SetMeasureOnOwner(189); }
    public void SetMeasureOnByPlayerId190() { SetMeasureOnOwner(190); }
    public void SetMeasureOnByPlayerId191() { SetMeasureOnOwner(191); }
    public void SetMeasureOnByPlayerId192() { SetMeasureOnOwner(192); }
    public void SetMeasureOnByPlayerId193() { SetMeasureOnOwner(193); }
    public void SetMeasureOnByPlayerId194() { SetMeasureOnOwner(194); }
    public void SetMeasureOnByPlayerId195() { SetMeasureOnOwner(195); }
    public void SetMeasureOnByPlayerId196() { SetMeasureOnOwner(196); }
    public void SetMeasureOnByPlayerId197() { SetMeasureOnOwner(197); }
    public void SetMeasureOnByPlayerId198() { SetMeasureOnOwner(198); }
    public void SetMeasureOnByPlayerId199() { SetMeasureOnOwner(199); }
    public void SetMeasureOnByPlayerId200() { SetMeasureOnOwner(200); }
    public void SetMeasureOnByPlayerId201() { SetMeasureOnOwner(201); }
    public void SetMeasureOnByPlayerId202() { SetMeasureOnOwner(202); }
    public void SetMeasureOnByPlayerId203() { SetMeasureOnOwner(203); }
    public void SetMeasureOnByPlayerId204() { SetMeasureOnOwner(204); }
    public void SetMeasureOnByPlayerId205() { SetMeasureOnOwner(205); }
    public void SetMeasureOnByPlayerId206() { SetMeasureOnOwner(206); }
    public void SetMeasureOnByPlayerId207() { SetMeasureOnOwner(207); }
    public void SetMeasureOnByPlayerId208() { SetMeasureOnOwner(208); }
    public void SetMeasureOnByPlayerId209() { SetMeasureOnOwner(209); }
    public void SetMeasureOnByPlayerId210() { SetMeasureOnOwner(210); }
    public void SetMeasureOnByPlayerId211() { SetMeasureOnOwner(211); }
    public void SetMeasureOnByPlayerId212() { SetMeasureOnOwner(212); }
    public void SetMeasureOnByPlayerId213() { SetMeasureOnOwner(213); }
    public void SetMeasureOnByPlayerId214() { SetMeasureOnOwner(214); }
    public void SetMeasureOnByPlayerId215() { SetMeasureOnOwner(215); }
    public void SetMeasureOnByPlayerId216() { SetMeasureOnOwner(216); }
    public void SetMeasureOnByPlayerId217() { SetMeasureOnOwner(217); }
    public void SetMeasureOnByPlayerId218() { SetMeasureOnOwner(218); }
    public void SetMeasureOnByPlayerId219() { SetMeasureOnOwner(219); }
    public void SetMeasureOnByPlayerId220() { SetMeasureOnOwner(220); }
    public void SetMeasureOnByPlayerId221() { SetMeasureOnOwner(221); }
    public void SetMeasureOnByPlayerId222() { SetMeasureOnOwner(222); }
    public void SetMeasureOnByPlayerId223() { SetMeasureOnOwner(223); }
    public void SetMeasureOnByPlayerId224() { SetMeasureOnOwner(224); }
    public void SetMeasureOnByPlayerId225() { SetMeasureOnOwner(225); }
    public void SetMeasureOnByPlayerId226() { SetMeasureOnOwner(226); }
    public void SetMeasureOnByPlayerId227() { SetMeasureOnOwner(227); }
    public void SetMeasureOnByPlayerId228() { SetMeasureOnOwner(228); }
    public void SetMeasureOnByPlayerId229() { SetMeasureOnOwner(229); }
    public void SetMeasureOnByPlayerId230() { SetMeasureOnOwner(230); }
    public void SetMeasureOnByPlayerId231() { SetMeasureOnOwner(231); }
    public void SetMeasureOnByPlayerId232() { SetMeasureOnOwner(232); }
    public void SetMeasureOnByPlayerId233() { SetMeasureOnOwner(233); }
    public void SetMeasureOnByPlayerId234() { SetMeasureOnOwner(234); }
    public void SetMeasureOnByPlayerId235() { SetMeasureOnOwner(235); }
    public void SetMeasureOnByPlayerId236() { SetMeasureOnOwner(236); }
    public void SetMeasureOnByPlayerId237() { SetMeasureOnOwner(237); }
    public void SetMeasureOnByPlayerId238() { SetMeasureOnOwner(238); }
    public void SetMeasureOnByPlayerId239() { SetMeasureOnOwner(239); }
    public void SetMeasureOnByPlayerId240() { SetMeasureOnOwner(240); }
    public void SetMeasureOnByPlayerId241() { SetMeasureOnOwner(241); }
    public void SetMeasureOnByPlayerId242() { SetMeasureOnOwner(242); }
    public void SetMeasureOnByPlayerId243() { SetMeasureOnOwner(243); }
    public void SetMeasureOnByPlayerId244() { SetMeasureOnOwner(244); }
    public void SetMeasureOnByPlayerId245() { SetMeasureOnOwner(245); }
    public void SetMeasureOnByPlayerId246() { SetMeasureOnOwner(246); }
    public void SetMeasureOnByPlayerId247() { SetMeasureOnOwner(247); }
    public void SetMeasureOnByPlayerId248() { SetMeasureOnOwner(248); }
    public void SetMeasureOnByPlayerId249() { SetMeasureOnOwner(249); }
    public void SetMeasureOnByPlayerId250() { SetMeasureOnOwner(250); }
    public void SetMeasureOnByPlayerId251() { SetMeasureOnOwner(251); }
    public void SetMeasureOnByPlayerId252() { SetMeasureOnOwner(252); }
    public void SetMeasureOnByPlayerId253() { SetMeasureOnOwner(253); }
    public void SetMeasureOnByPlayerId254() { SetMeasureOnOwner(254); }
    public void SetMeasureOnByPlayerId255() { SetMeasureOnOwner(255); }
    public void SetMeasureOnByPlayerId256() { SetMeasureOnOwner(256); }
    public void SetMeasureOnByPlayerId257() { SetMeasureOnOwner(257); }
    public void SetMeasureOnByPlayerId258() { SetMeasureOnOwner(258); }
    public void SetMeasureOnByPlayerId259() { SetMeasureOnOwner(259); }
    public void SetMeasureOnByPlayerId260() { SetMeasureOnOwner(260); }
    public void SetMeasureOnByPlayerId261() { SetMeasureOnOwner(261); }
    public void SetMeasureOnByPlayerId262() { SetMeasureOnOwner(262); }
    public void SetMeasureOnByPlayerId263() { SetMeasureOnOwner(263); }
    public void SetMeasureOnByPlayerId264() { SetMeasureOnOwner(264); }
    public void SetMeasureOnByPlayerId265() { SetMeasureOnOwner(265); }
    public void SetMeasureOnByPlayerId266() { SetMeasureOnOwner(266); }
    public void SetMeasureOnByPlayerId267() { SetMeasureOnOwner(267); }
    public void SetMeasureOnByPlayerId268() { SetMeasureOnOwner(268); }
    public void SetMeasureOnByPlayerId269() { SetMeasureOnOwner(269); }
    public void SetMeasureOnByPlayerId270() { SetMeasureOnOwner(270); }
    public void SetMeasureOnByPlayerId271() { SetMeasureOnOwner(271); }
    public void SetMeasureOnByPlayerId272() { SetMeasureOnOwner(272); }
    public void SetMeasureOnByPlayerId273() { SetMeasureOnOwner(273); }
    public void SetMeasureOnByPlayerId274() { SetMeasureOnOwner(274); }
    public void SetMeasureOnByPlayerId275() { SetMeasureOnOwner(275); }
    public void SetMeasureOnByPlayerId276() { SetMeasureOnOwner(276); }
    public void SetMeasureOnByPlayerId277() { SetMeasureOnOwner(277); }
    public void SetMeasureOnByPlayerId278() { SetMeasureOnOwner(278); }
    public void SetMeasureOnByPlayerId279() { SetMeasureOnOwner(279); }
    public void SetMeasureOnByPlayerId280() { SetMeasureOnOwner(280); }
    public void SetMeasureOnByPlayerId281() { SetMeasureOnOwner(281); }
    public void SetMeasureOnByPlayerId282() { SetMeasureOnOwner(282); }
    public void SetMeasureOnByPlayerId283() { SetMeasureOnOwner(283); }
    public void SetMeasureOnByPlayerId284() { SetMeasureOnOwner(284); }
    public void SetMeasureOnByPlayerId285() { SetMeasureOnOwner(285); }
    public void SetMeasureOnByPlayerId286() { SetMeasureOnOwner(286); }
    public void SetMeasureOnByPlayerId287() { SetMeasureOnOwner(287); }
    public void SetMeasureOnByPlayerId288() { SetMeasureOnOwner(288); }
    public void SetMeasureOnByPlayerId289() { SetMeasureOnOwner(289); }
    public void SetMeasureOnByPlayerId290() { SetMeasureOnOwner(290); }
    public void SetMeasureOnByPlayerId291() { SetMeasureOnOwner(291); }
    public void SetMeasureOnByPlayerId292() { SetMeasureOnOwner(292); }
    public void SetMeasureOnByPlayerId293() { SetMeasureOnOwner(293); }
    public void SetMeasureOnByPlayerId294() { SetMeasureOnOwner(294); }
    public void SetMeasureOnByPlayerId295() { SetMeasureOnOwner(295); }
    public void SetMeasureOnByPlayerId296() { SetMeasureOnOwner(296); }
    public void SetMeasureOnByPlayerId297() { SetMeasureOnOwner(297); }
    public void SetMeasureOnByPlayerId298() { SetMeasureOnOwner(298); }
    public void SetMeasureOnByPlayerId299() { SetMeasureOnOwner(299); }
    public void SetMeasureOnByPlayerId300() { SetMeasureOnOwner(300); }
    public void SetMeasureOnByPlayerId301() { SetMeasureOnOwner(301); }
    public void SetMeasureOnByPlayerId302() { SetMeasureOnOwner(302); }
    public void SetMeasureOnByPlayerId303() { SetMeasureOnOwner(303); }
    public void SetMeasureOnByPlayerId304() { SetMeasureOnOwner(304); }
    public void SetMeasureOnByPlayerId305() { SetMeasureOnOwner(305); }
    public void SetMeasureOnByPlayerId306() { SetMeasureOnOwner(306); }
    public void SetMeasureOnByPlayerId307() { SetMeasureOnOwner(307); }
    public void SetMeasureOnByPlayerId308() { SetMeasureOnOwner(308); }
    public void SetMeasureOnByPlayerId309() { SetMeasureOnOwner(309); }
    public void SetMeasureOnByPlayerId310() { SetMeasureOnOwner(310); }
    public void SetMeasureOnByPlayerId311() { SetMeasureOnOwner(311); }
    public void SetMeasureOnByPlayerId312() { SetMeasureOnOwner(312); }
    public void SetMeasureOnByPlayerId313() { SetMeasureOnOwner(313); }
    public void SetMeasureOnByPlayerId314() { SetMeasureOnOwner(314); }
    public void SetMeasureOnByPlayerId315() { SetMeasureOnOwner(315); }
    public void SetMeasureOnByPlayerId316() { SetMeasureOnOwner(316); }
    public void SetMeasureOnByPlayerId317() { SetMeasureOnOwner(317); }
    public void SetMeasureOnByPlayerId318() { SetMeasureOnOwner(318); }
    public void SetMeasureOnByPlayerId319() { SetMeasureOnOwner(319); }
    public void SetMeasureOnByPlayerId320() { SetMeasureOnOwner(320); }
    public void SetMeasureOnByPlayerId321() { SetMeasureOnOwner(321); }
    public void SetMeasureOnByPlayerId322() { SetMeasureOnOwner(322); }
    public void SetMeasureOnByPlayerId323() { SetMeasureOnOwner(323); }
    public void SetMeasureOnByPlayerId324() { SetMeasureOnOwner(324); }
    public void SetMeasureOnByPlayerId325() { SetMeasureOnOwner(325); }
    public void SetMeasureOnByPlayerId326() { SetMeasureOnOwner(326); }
    public void SetMeasureOnByPlayerId327() { SetMeasureOnOwner(327); }
    public void SetMeasureOnByPlayerId328() { SetMeasureOnOwner(328); }
    public void SetMeasureOnByPlayerId329() { SetMeasureOnOwner(329); }
    public void SetMeasureOnByPlayerId330() { SetMeasureOnOwner(330); }
    public void SetMeasureOnByPlayerId331() { SetMeasureOnOwner(331); }
    public void SetMeasureOnByPlayerId332() { SetMeasureOnOwner(332); }
    public void SetMeasureOnByPlayerId333() { SetMeasureOnOwner(333); }
    public void SetMeasureOnByPlayerId334() { SetMeasureOnOwner(334); }
    public void SetMeasureOnByPlayerId335() { SetMeasureOnOwner(335); }
    public void SetMeasureOnByPlayerId336() { SetMeasureOnOwner(336); }
    public void SetMeasureOnByPlayerId337() { SetMeasureOnOwner(337); }
    public void SetMeasureOnByPlayerId338() { SetMeasureOnOwner(338); }
    public void SetMeasureOnByPlayerId339() { SetMeasureOnOwner(339); }
    public void SetMeasureOnByPlayerId340() { SetMeasureOnOwner(340); }
    public void SetMeasureOnByPlayerId341() { SetMeasureOnOwner(341); }
    public void SetMeasureOnByPlayerId342() { SetMeasureOnOwner(342); }
    public void SetMeasureOnByPlayerId343() { SetMeasureOnOwner(343); }
    public void SetMeasureOnByPlayerId344() { SetMeasureOnOwner(344); }
    public void SetMeasureOnByPlayerId345() { SetMeasureOnOwner(345); }
    public void SetMeasureOnByPlayerId346() { SetMeasureOnOwner(346); }
    public void SetMeasureOnByPlayerId347() { SetMeasureOnOwner(347); }
    public void SetMeasureOnByPlayerId348() { SetMeasureOnOwner(348); }
    public void SetMeasureOnByPlayerId349() { SetMeasureOnOwner(349); }
    public void SetMeasureOnByPlayerId350() { SetMeasureOnOwner(350); }
    public void SetMeasureOnByPlayerId351() { SetMeasureOnOwner(351); }
    public void SetMeasureOnByPlayerId352() { SetMeasureOnOwner(352); }
    public void SetMeasureOnByPlayerId353() { SetMeasureOnOwner(353); }
    public void SetMeasureOnByPlayerId354() { SetMeasureOnOwner(354); }
    public void SetMeasureOnByPlayerId355() { SetMeasureOnOwner(355); }
    public void SetMeasureOnByPlayerId356() { SetMeasureOnOwner(356); }
    public void SetMeasureOnByPlayerId357() { SetMeasureOnOwner(357); }
    public void SetMeasureOnByPlayerId358() { SetMeasureOnOwner(358); }
    public void SetMeasureOnByPlayerId359() { SetMeasureOnOwner(359); }
    public void SetMeasureOnByPlayerId360() { SetMeasureOnOwner(360); }
    public void SetMeasureOnByPlayerId361() { SetMeasureOnOwner(361); }
    public void SetMeasureOnByPlayerId362() { SetMeasureOnOwner(362); }
    public void SetMeasureOnByPlayerId363() { SetMeasureOnOwner(363); }
    public void SetMeasureOnByPlayerId364() { SetMeasureOnOwner(364); }
    public void SetMeasureOnByPlayerId365() { SetMeasureOnOwner(365); }
    public void SetMeasureOnByPlayerId366() { SetMeasureOnOwner(366); }
    public void SetMeasureOnByPlayerId367() { SetMeasureOnOwner(367); }
    public void SetMeasureOnByPlayerId368() { SetMeasureOnOwner(368); }
    public void SetMeasureOnByPlayerId369() { SetMeasureOnOwner(369); }
    public void SetMeasureOnByPlayerId370() { SetMeasureOnOwner(370); }
    public void SetMeasureOnByPlayerId371() { SetMeasureOnOwner(371); }
    public void SetMeasureOnByPlayerId372() { SetMeasureOnOwner(372); }
    public void SetMeasureOnByPlayerId373() { SetMeasureOnOwner(373); }
    public void SetMeasureOnByPlayerId374() { SetMeasureOnOwner(374); }
    public void SetMeasureOnByPlayerId375() { SetMeasureOnOwner(375); }
    public void SetMeasureOnByPlayerId376() { SetMeasureOnOwner(376); }
    public void SetMeasureOnByPlayerId377() { SetMeasureOnOwner(377); }
    public void SetMeasureOnByPlayerId378() { SetMeasureOnOwner(378); }
    public void SetMeasureOnByPlayerId379() { SetMeasureOnOwner(379); }
    public void SetMeasureOnByPlayerId380() { SetMeasureOnOwner(380); }
    public void SetMeasureOnByPlayerId381() { SetMeasureOnOwner(381); }
    public void SetMeasureOnByPlayerId382() { SetMeasureOnOwner(382); }
    public void SetMeasureOnByPlayerId383() { SetMeasureOnOwner(383); }
    public void SetMeasureOnByPlayerId384() { SetMeasureOnOwner(384); }
    public void SetMeasureOnByPlayerId385() { SetMeasureOnOwner(385); }
    public void SetMeasureOnByPlayerId386() { SetMeasureOnOwner(386); }
    public void SetMeasureOnByPlayerId387() { SetMeasureOnOwner(387); }
    public void SetMeasureOnByPlayerId388() { SetMeasureOnOwner(388); }
    public void SetMeasureOnByPlayerId389() { SetMeasureOnOwner(389); }
    public void SetMeasureOnByPlayerId390() { SetMeasureOnOwner(390); }
    public void SetMeasureOnByPlayerId391() { SetMeasureOnOwner(391); }
    public void SetMeasureOnByPlayerId392() { SetMeasureOnOwner(392); }
    public void SetMeasureOnByPlayerId393() { SetMeasureOnOwner(393); }
    public void SetMeasureOnByPlayerId394() { SetMeasureOnOwner(394); }
    public void SetMeasureOnByPlayerId395() { SetMeasureOnOwner(395); }
    public void SetMeasureOnByPlayerId396() { SetMeasureOnOwner(396); }
    public void SetMeasureOnByPlayerId397() { SetMeasureOnOwner(397); }
    public void SetMeasureOnByPlayerId398() { SetMeasureOnOwner(398); }
    public void SetMeasureOnByPlayerId399() { SetMeasureOnOwner(399); }
    public void SetMeasureOnByPlayerId400() { SetMeasureOnOwner(400); }
    public void SetMeasureOnByPlayerId401() { SetMeasureOnOwner(401); }
    public void SetMeasureOnByPlayerId402() { SetMeasureOnOwner(402); }
    public void SetMeasureOnByPlayerId403() { SetMeasureOnOwner(403); }
    public void SetMeasureOnByPlayerId404() { SetMeasureOnOwner(404); }
    public void SetMeasureOnByPlayerId405() { SetMeasureOnOwner(405); }
    public void SetMeasureOnByPlayerId406() { SetMeasureOnOwner(406); }
    public void SetMeasureOnByPlayerId407() { SetMeasureOnOwner(407); }
    public void SetMeasureOnByPlayerId408() { SetMeasureOnOwner(408); }
    public void SetMeasureOnByPlayerId409() { SetMeasureOnOwner(409); }
    public void SetMeasureOnByPlayerId410() { SetMeasureOnOwner(410); }
    public void SetMeasureOnByPlayerId411() { SetMeasureOnOwner(411); }
    public void SetMeasureOnByPlayerId412() { SetMeasureOnOwner(412); }
    public void SetMeasureOnByPlayerId413() { SetMeasureOnOwner(413); }
    public void SetMeasureOnByPlayerId414() { SetMeasureOnOwner(414); }
    public void SetMeasureOnByPlayerId415() { SetMeasureOnOwner(415); }
    public void SetMeasureOnByPlayerId416() { SetMeasureOnOwner(416); }
    public void SetMeasureOnByPlayerId417() { SetMeasureOnOwner(417); }
    public void SetMeasureOnByPlayerId418() { SetMeasureOnOwner(418); }
    public void SetMeasureOnByPlayerId419() { SetMeasureOnOwner(419); }
    public void SetMeasureOnByPlayerId420() { SetMeasureOnOwner(420); }
    public void SetMeasureOnByPlayerId421() { SetMeasureOnOwner(421); }
    public void SetMeasureOnByPlayerId422() { SetMeasureOnOwner(422); }
    public void SetMeasureOnByPlayerId423() { SetMeasureOnOwner(423); }
    public void SetMeasureOnByPlayerId424() { SetMeasureOnOwner(424); }
    public void SetMeasureOnByPlayerId425() { SetMeasureOnOwner(425); }
    public void SetMeasureOnByPlayerId426() { SetMeasureOnOwner(426); }
    public void SetMeasureOnByPlayerId427() { SetMeasureOnOwner(427); }
    public void SetMeasureOnByPlayerId428() { SetMeasureOnOwner(428); }
    public void SetMeasureOnByPlayerId429() { SetMeasureOnOwner(429); }
    public void SetMeasureOnByPlayerId430() { SetMeasureOnOwner(430); }
    public void SetMeasureOnByPlayerId431() { SetMeasureOnOwner(431); }
    public void SetMeasureOnByPlayerId432() { SetMeasureOnOwner(432); }
    public void SetMeasureOnByPlayerId433() { SetMeasureOnOwner(433); }
    public void SetMeasureOnByPlayerId434() { SetMeasureOnOwner(434); }
    public void SetMeasureOnByPlayerId435() { SetMeasureOnOwner(435); }
    public void SetMeasureOnByPlayerId436() { SetMeasureOnOwner(436); }
    public void SetMeasureOnByPlayerId437() { SetMeasureOnOwner(437); }
    public void SetMeasureOnByPlayerId438() { SetMeasureOnOwner(438); }
    public void SetMeasureOnByPlayerId439() { SetMeasureOnOwner(439); }
    public void SetMeasureOnByPlayerId440() { SetMeasureOnOwner(440); }
    public void SetMeasureOnByPlayerId441() { SetMeasureOnOwner(441); }
    public void SetMeasureOnByPlayerId442() { SetMeasureOnOwner(442); }
    public void SetMeasureOnByPlayerId443() { SetMeasureOnOwner(443); }
    public void SetMeasureOnByPlayerId444() { SetMeasureOnOwner(444); }
    public void SetMeasureOnByPlayerId445() { SetMeasureOnOwner(445); }
    public void SetMeasureOnByPlayerId446() { SetMeasureOnOwner(446); }
    public void SetMeasureOnByPlayerId447() { SetMeasureOnOwner(447); }
    public void SetMeasureOnByPlayerId448() { SetMeasureOnOwner(448); }
    public void SetMeasureOnByPlayerId449() { SetMeasureOnOwner(449); }
    public void SetMeasureOnByPlayerId450() { SetMeasureOnOwner(450); }
    public void SetMeasureOnByPlayerId451() { SetMeasureOnOwner(451); }
    public void SetMeasureOnByPlayerId452() { SetMeasureOnOwner(452); }
    public void SetMeasureOnByPlayerId453() { SetMeasureOnOwner(453); }
    public void SetMeasureOnByPlayerId454() { SetMeasureOnOwner(454); }
    public void SetMeasureOnByPlayerId455() { SetMeasureOnOwner(455); }
    public void SetMeasureOnByPlayerId456() { SetMeasureOnOwner(456); }
    public void SetMeasureOnByPlayerId457() { SetMeasureOnOwner(457); }
    public void SetMeasureOnByPlayerId458() { SetMeasureOnOwner(458); }
    public void SetMeasureOnByPlayerId459() { SetMeasureOnOwner(459); }
    public void SetMeasureOnByPlayerId460() { SetMeasureOnOwner(460); }
    public void SetMeasureOnByPlayerId461() { SetMeasureOnOwner(461); }
    public void SetMeasureOnByPlayerId462() { SetMeasureOnOwner(462); }
    public void SetMeasureOnByPlayerId463() { SetMeasureOnOwner(463); }
    public void SetMeasureOnByPlayerId464() { SetMeasureOnOwner(464); }
    public void SetMeasureOnByPlayerId465() { SetMeasureOnOwner(465); }
    public void SetMeasureOnByPlayerId466() { SetMeasureOnOwner(466); }
    public void SetMeasureOnByPlayerId467() { SetMeasureOnOwner(467); }
    public void SetMeasureOnByPlayerId468() { SetMeasureOnOwner(468); }
    public void SetMeasureOnByPlayerId469() { SetMeasureOnOwner(469); }
    public void SetMeasureOnByPlayerId470() { SetMeasureOnOwner(470); }
    public void SetMeasureOnByPlayerId471() { SetMeasureOnOwner(471); }
    public void SetMeasureOnByPlayerId472() { SetMeasureOnOwner(472); }
    public void SetMeasureOnByPlayerId473() { SetMeasureOnOwner(473); }
    public void SetMeasureOnByPlayerId474() { SetMeasureOnOwner(474); }
    public void SetMeasureOnByPlayerId475() { SetMeasureOnOwner(475); }
    public void SetMeasureOnByPlayerId476() { SetMeasureOnOwner(476); }
    public void SetMeasureOnByPlayerId477() { SetMeasureOnOwner(477); }
    public void SetMeasureOnByPlayerId478() { SetMeasureOnOwner(478); }
    public void SetMeasureOnByPlayerId479() { SetMeasureOnOwner(479); }
    public void SetMeasureOnByPlayerId480() { SetMeasureOnOwner(480); }
    public void SetMeasureOnByPlayerId481() { SetMeasureOnOwner(481); }
    public void SetMeasureOnByPlayerId482() { SetMeasureOnOwner(482); }
    public void SetMeasureOnByPlayerId483() { SetMeasureOnOwner(483); }
    public void SetMeasureOnByPlayerId484() { SetMeasureOnOwner(484); }
    public void SetMeasureOnByPlayerId485() { SetMeasureOnOwner(485); }
    public void SetMeasureOnByPlayerId486() { SetMeasureOnOwner(486); }
    public void SetMeasureOnByPlayerId487() { SetMeasureOnOwner(487); }
    public void SetMeasureOnByPlayerId488() { SetMeasureOnOwner(488); }
    public void SetMeasureOnByPlayerId489() { SetMeasureOnOwner(489); }
    public void SetMeasureOnByPlayerId490() { SetMeasureOnOwner(490); }
    public void SetMeasureOnByPlayerId491() { SetMeasureOnOwner(491); }
    public void SetMeasureOnByPlayerId492() { SetMeasureOnOwner(492); }
    public void SetMeasureOnByPlayerId493() { SetMeasureOnOwner(493); }
    public void SetMeasureOnByPlayerId494() { SetMeasureOnOwner(494); }
    public void SetMeasureOnByPlayerId495() { SetMeasureOnOwner(495); }
    public void SetMeasureOnByPlayerId496() { SetMeasureOnOwner(496); }
    public void SetMeasureOnByPlayerId497() { SetMeasureOnOwner(497); }
    public void SetMeasureOnByPlayerId498() { SetMeasureOnOwner(498); }
    public void SetMeasureOnByPlayerId499() { SetMeasureOnOwner(499); }
    public void SetMeasureOnByPlayerId500() { SetMeasureOnOwner(500); }

    // =================================
    // 測定フラグOFF更新 非Owner処理
    // =================================
    private void SetMeasureOffEventToOwner(int PlayerId)
    {
        switch(PlayerId) {
            case 1: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId1)); break;
            case 2: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId2)); break;
            case 3: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId3)); break;
            case 4: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId4)); break;
            case 5: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId5)); break;
            case 6: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId6)); break;
            case 7: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId7)); break;
            case 8: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId8)); break;
            case 9: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId9)); break;
            case 10: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId10)); break;
            case 11: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId11)); break;
            case 12: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId12)); break;
            case 13: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId13)); break;
            case 14: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId14)); break;
            case 15: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId15)); break;
            case 16: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId16)); break;
            case 17: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId17)); break;
            case 18: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId18)); break;
            case 19: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId19)); break;
            case 20: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId20)); break;
            case 21: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId21)); break;
            case 22: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId22)); break;
            case 23: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId23)); break;
            case 24: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId24)); break;
            case 25: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId25)); break;
            case 26: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId26)); break;
            case 27: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId27)); break;
            case 28: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId28)); break;
            case 29: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId29)); break;
            case 30: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId30)); break;
            case 31: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId31)); break;
            case 32: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId32)); break;
            case 33: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId33)); break;
            case 34: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId34)); break;
            case 35: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId35)); break;
            case 36: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId36)); break;
            case 37: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId37)); break;
            case 38: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId38)); break;
            case 39: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId39)); break;
            case 40: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId40)); break;
            case 41: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId41)); break;
            case 42: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId42)); break;
            case 43: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId43)); break;
            case 44: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId44)); break;
            case 45: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId45)); break;
            case 46: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId46)); break;
            case 47: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId47)); break;
            case 48: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId48)); break;
            case 49: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId49)); break;
            case 50: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId50)); break;
            case 51: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId51)); break;
            case 52: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId52)); break;
            case 53: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId53)); break;
            case 54: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId54)); break;
            case 55: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId55)); break;
            case 56: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId56)); break;
            case 57: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId57)); break;
            case 58: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId58)); break;
            case 59: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId59)); break;
            case 60: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId60)); break;
            case 61: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId61)); break;
            case 62: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId62)); break;
            case 63: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId63)); break;
            case 64: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId64)); break;
            case 65: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId65)); break;
            case 66: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId66)); break;
            case 67: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId67)); break;
            case 68: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId68)); break;
            case 69: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId69)); break;
            case 70: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId70)); break;
            case 71: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId71)); break;
            case 72: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId72)); break;
            case 73: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId73)); break;
            case 74: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId74)); break;
            case 75: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId75)); break;
            case 76: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId76)); break;
            case 77: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId77)); break;
            case 78: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId78)); break;
            case 79: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId79)); break;
            case 80: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId80)); break;
            case 81: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId81)); break;
            case 82: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId82)); break;
            case 83: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId83)); break;
            case 84: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId84)); break;
            case 85: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId85)); break;
            case 86: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId86)); break;
            case 87: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId87)); break;
            case 88: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId88)); break;
            case 89: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId89)); break;
            case 90: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId90)); break;
            case 91: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId91)); break;
            case 92: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId92)); break;
            case 93: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId93)); break;
            case 94: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId94)); break;
            case 95: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId95)); break;
            case 96: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId96)); break;
            case 97: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId97)); break;
            case 98: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId98)); break;
            case 99: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId99)); break;
            case 100: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId100)); break;
            case 101: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId101)); break;
            case 102: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId102)); break;
            case 103: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId103)); break;
            case 104: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId104)); break;
            case 105: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId105)); break;
            case 106: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId106)); break;
            case 107: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId107)); break;
            case 108: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId108)); break;
            case 109: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId109)); break;
            case 110: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId110)); break;
            case 111: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId111)); break;
            case 112: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId112)); break;
            case 113: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId113)); break;
            case 114: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId114)); break;
            case 115: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId115)); break;
            case 116: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId116)); break;
            case 117: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId117)); break;
            case 118: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId118)); break;
            case 119: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId119)); break;
            case 120: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId120)); break;
            case 121: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId121)); break;
            case 122: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId122)); break;
            case 123: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId123)); break;
            case 124: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId124)); break;
            case 125: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId125)); break;
            case 126: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId126)); break;
            case 127: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId127)); break;
            case 128: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId128)); break;
            case 129: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId129)); break;
            case 130: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId130)); break;
            case 131: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId131)); break;
            case 132: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId132)); break;
            case 133: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId133)); break;
            case 134: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId134)); break;
            case 135: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId135)); break;
            case 136: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId136)); break;
            case 137: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId137)); break;
            case 138: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId138)); break;
            case 139: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId139)); break;
            case 140: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId140)); break;
            case 141: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId141)); break;
            case 142: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId142)); break;
            case 143: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId143)); break;
            case 144: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId144)); break;
            case 145: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId145)); break;
            case 146: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId146)); break;
            case 147: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId147)); break;
            case 148: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId148)); break;
            case 149: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId149)); break;
            case 150: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId150)); break;
            case 151: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId151)); break;
            case 152: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId152)); break;
            case 153: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId153)); break;
            case 154: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId154)); break;
            case 155: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId155)); break;
            case 156: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId156)); break;
            case 157: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId157)); break;
            case 158: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId158)); break;
            case 159: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId159)); break;
            case 160: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId160)); break;
            case 161: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId161)); break;
            case 162: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId162)); break;
            case 163: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId163)); break;
            case 164: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId164)); break;
            case 165: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId165)); break;
            case 166: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId166)); break;
            case 167: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId167)); break;
            case 168: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId168)); break;
            case 169: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId169)); break;
            case 170: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId170)); break;
            case 171: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId171)); break;
            case 172: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId172)); break;
            case 173: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId173)); break;
            case 174: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId174)); break;
            case 175: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId175)); break;
            case 176: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId176)); break;
            case 177: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId177)); break;
            case 178: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId178)); break;
            case 179: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId179)); break;
            case 180: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId180)); break;
            case 181: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId181)); break;
            case 182: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId182)); break;
            case 183: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId183)); break;
            case 184: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId184)); break;
            case 185: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId185)); break;
            case 186: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId186)); break;
            case 187: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId187)); break;
            case 188: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId188)); break;
            case 189: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId189)); break;
            case 190: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId190)); break;
            case 191: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId191)); break;
            case 192: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId192)); break;
            case 193: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId193)); break;
            case 194: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId194)); break;
            case 195: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId195)); break;
            case 196: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId196)); break;
            case 197: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId197)); break;
            case 198: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId198)); break;
            case 199: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId199)); break;
            case 200: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId200)); break;
            case 201: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId201)); break;
            case 202: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId202)); break;
            case 203: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId203)); break;
            case 204: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId204)); break;
            case 205: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId205)); break;
            case 206: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId206)); break;
            case 207: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId207)); break;
            case 208: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId208)); break;
            case 209: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId209)); break;
            case 210: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId210)); break;
            case 211: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId211)); break;
            case 212: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId212)); break;
            case 213: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId213)); break;
            case 214: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId214)); break;
            case 215: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId215)); break;
            case 216: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId216)); break;
            case 217: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId217)); break;
            case 218: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId218)); break;
            case 219: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId219)); break;
            case 220: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId220)); break;
            case 221: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId221)); break;
            case 222: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId222)); break;
            case 223: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId223)); break;
            case 224: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId224)); break;
            case 225: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId225)); break;
            case 226: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId226)); break;
            case 227: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId227)); break;
            case 228: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId228)); break;
            case 229: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId229)); break;
            case 230: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId230)); break;
            case 231: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId231)); break;
            case 232: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId232)); break;
            case 233: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId233)); break;
            case 234: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId234)); break;
            case 235: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId235)); break;
            case 236: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId236)); break;
            case 237: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId237)); break;
            case 238: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId238)); break;
            case 239: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId239)); break;
            case 240: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId240)); break;
            case 241: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId241)); break;
            case 242: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId242)); break;
            case 243: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId243)); break;
            case 244: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId244)); break;
            case 245: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId245)); break;
            case 246: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId246)); break;
            case 247: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId247)); break;
            case 248: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId248)); break;
            case 249: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId249)); break;
            case 250: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId250)); break;
            case 251: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId251)); break;
            case 252: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId252)); break;
            case 253: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId253)); break;
            case 254: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId254)); break;
            case 255: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId255)); break;
            case 256: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId256)); break;
            case 257: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId257)); break;
            case 258: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId258)); break;
            case 259: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId259)); break;
            case 260: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId260)); break;
            case 261: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId261)); break;
            case 262: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId262)); break;
            case 263: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId263)); break;
            case 264: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId264)); break;
            case 265: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId265)); break;
            case 266: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId266)); break;
            case 267: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId267)); break;
            case 268: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId268)); break;
            case 269: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId269)); break;
            case 270: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId270)); break;
            case 271: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId271)); break;
            case 272: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId272)); break;
            case 273: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId273)); break;
            case 274: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId274)); break;
            case 275: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId275)); break;
            case 276: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId276)); break;
            case 277: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId277)); break;
            case 278: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId278)); break;
            case 279: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId279)); break;
            case 280: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId280)); break;
            case 281: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId281)); break;
            case 282: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId282)); break;
            case 283: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId283)); break;
            case 284: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId284)); break;
            case 285: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId285)); break;
            case 286: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId286)); break;
            case 287: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId287)); break;
            case 288: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId288)); break;
            case 289: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId289)); break;
            case 290: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId290)); break;
            case 291: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId291)); break;
            case 292: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId292)); break;
            case 293: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId293)); break;
            case 294: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId294)); break;
            case 295: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId295)); break;
            case 296: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId296)); break;
            case 297: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId297)); break;
            case 298: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId298)); break;
            case 299: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId299)); break;
            case 300: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId300)); break;
            case 301: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId301)); break;
            case 302: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId302)); break;
            case 303: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId303)); break;
            case 304: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId304)); break;
            case 305: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId305)); break;
            case 306: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId306)); break;
            case 307: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId307)); break;
            case 308: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId308)); break;
            case 309: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId309)); break;
            case 310: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId310)); break;
            case 311: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId311)); break;
            case 312: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId312)); break;
            case 313: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId313)); break;
            case 314: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId314)); break;
            case 315: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId315)); break;
            case 316: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId316)); break;
            case 317: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId317)); break;
            case 318: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId318)); break;
            case 319: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId319)); break;
            case 320: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId320)); break;
            case 321: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId321)); break;
            case 322: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId322)); break;
            case 323: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId323)); break;
            case 324: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId324)); break;
            case 325: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId325)); break;
            case 326: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId326)); break;
            case 327: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId327)); break;
            case 328: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId328)); break;
            case 329: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId329)); break;
            case 330: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId330)); break;
            case 331: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId331)); break;
            case 332: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId332)); break;
            case 333: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId333)); break;
            case 334: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId334)); break;
            case 335: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId335)); break;
            case 336: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId336)); break;
            case 337: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId337)); break;
            case 338: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId338)); break;
            case 339: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId339)); break;
            case 340: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId340)); break;
            case 341: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId341)); break;
            case 342: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId342)); break;
            case 343: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId343)); break;
            case 344: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId344)); break;
            case 345: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId345)); break;
            case 346: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId346)); break;
            case 347: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId347)); break;
            case 348: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId348)); break;
            case 349: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId349)); break;
            case 350: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId350)); break;
            case 351: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId351)); break;
            case 352: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId352)); break;
            case 353: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId353)); break;
            case 354: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId354)); break;
            case 355: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId355)); break;
            case 356: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId356)); break;
            case 357: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId357)); break;
            case 358: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId358)); break;
            case 359: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId359)); break;
            case 360: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId360)); break;
            case 361: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId361)); break;
            case 362: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId362)); break;
            case 363: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId363)); break;
            case 364: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId364)); break;
            case 365: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId365)); break;
            case 366: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId366)); break;
            case 367: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId367)); break;
            case 368: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId368)); break;
            case 369: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId369)); break;
            case 370: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId370)); break;
            case 371: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId371)); break;
            case 372: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId372)); break;
            case 373: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId373)); break;
            case 374: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId374)); break;
            case 375: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId375)); break;
            case 376: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId376)); break;
            case 377: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId377)); break;
            case 378: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId378)); break;
            case 379: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId379)); break;
            case 380: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId380)); break;
            case 381: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId381)); break;
            case 382: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId382)); break;
            case 383: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId383)); break;
            case 384: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId384)); break;
            case 385: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId385)); break;
            case 386: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId386)); break;
            case 387: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId387)); break;
            case 388: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId388)); break;
            case 389: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId389)); break;
            case 390: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId390)); break;
            case 391: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId391)); break;
            case 392: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId392)); break;
            case 393: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId393)); break;
            case 394: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId394)); break;
            case 395: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId395)); break;
            case 396: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId396)); break;
            case 397: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId397)); break;
            case 398: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId398)); break;
            case 399: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId399)); break;
            case 400: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId400)); break;
            case 401: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId401)); break;
            case 402: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId402)); break;
            case 403: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId403)); break;
            case 404: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId404)); break;
            case 405: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId405)); break;
            case 406: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId406)); break;
            case 407: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId407)); break;
            case 408: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId408)); break;
            case 409: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId409)); break;
            case 410: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId410)); break;
            case 411: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId411)); break;
            case 412: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId412)); break;
            case 413: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId413)); break;
            case 414: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId414)); break;
            case 415: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId415)); break;
            case 416: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId416)); break;
            case 417: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId417)); break;
            case 418: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId418)); break;
            case 419: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId419)); break;
            case 420: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId420)); break;
            case 421: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId421)); break;
            case 422: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId422)); break;
            case 423: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId423)); break;
            case 424: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId424)); break;
            case 425: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId425)); break;
            case 426: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId426)); break;
            case 427: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId427)); break;
            case 428: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId428)); break;
            case 429: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId429)); break;
            case 430: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId430)); break;
            case 431: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId431)); break;
            case 432: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId432)); break;
            case 433: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId433)); break;
            case 434: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId434)); break;
            case 435: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId435)); break;
            case 436: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId436)); break;
            case 437: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId437)); break;
            case 438: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId438)); break;
            case 439: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId439)); break;
            case 440: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId440)); break;
            case 441: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId441)); break;
            case 442: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId442)); break;
            case 443: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId443)); break;
            case 444: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId444)); break;
            case 445: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId445)); break;
            case 446: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId446)); break;
            case 447: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId447)); break;
            case 448: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId448)); break;
            case 449: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId449)); break;
            case 450: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId450)); break;
            case 451: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId451)); break;
            case 452: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId452)); break;
            case 453: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId453)); break;
            case 454: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId454)); break;
            case 455: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId455)); break;
            case 456: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId456)); break;
            case 457: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId457)); break;
            case 458: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId458)); break;
            case 459: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId459)); break;
            case 460: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId460)); break;
            case 461: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId461)); break;
            case 462: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId462)); break;
            case 463: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId463)); break;
            case 464: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId464)); break;
            case 465: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId465)); break;
            case 466: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId466)); break;
            case 467: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId467)); break;
            case 468: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId468)); break;
            case 469: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId469)); break;
            case 470: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId470)); break;
            case 471: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId471)); break;
            case 472: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId472)); break;
            case 473: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId473)); break;
            case 474: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId474)); break;
            case 475: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId475)); break;
            case 476: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId476)); break;
            case 477: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId477)); break;
            case 478: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId478)); break;
            case 479: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId479)); break;
            case 480: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId480)); break;
            case 481: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId481)); break;
            case 482: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId482)); break;
            case 483: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId483)); break;
            case 484: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId484)); break;
            case 485: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId485)); break;
            case 486: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId486)); break;
            case 487: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId487)); break;
            case 488: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId488)); break;
            case 489: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId489)); break;
            case 490: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId490)); break;
            case 491: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId491)); break;
            case 492: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId492)); break;
            case 493: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId493)); break;
            case 494: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId494)); break;
            case 495: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId495)); break;
            case 496: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId496)); break;
            case 497: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId497)); break;
            case 498: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId498)); break;
            case 499: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId499)); break;
            case 500: SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(SetMeasureOffByPlayerId500)); break;
            default: break;
        }
    }
    public void SetMeasureOffByPlayerId1() { SetMeasureOffOwner(1); }
    public void SetMeasureOffByPlayerId2() { SetMeasureOffOwner(2); }
    public void SetMeasureOffByPlayerId3() { SetMeasureOffOwner(3); }
    public void SetMeasureOffByPlayerId4() { SetMeasureOffOwner(4); }
    public void SetMeasureOffByPlayerId5() { SetMeasureOffOwner(5); }
    public void SetMeasureOffByPlayerId6() { SetMeasureOffOwner(6); }
    public void SetMeasureOffByPlayerId7() { SetMeasureOffOwner(7); }
    public void SetMeasureOffByPlayerId8() { SetMeasureOffOwner(8); }
    public void SetMeasureOffByPlayerId9() { SetMeasureOffOwner(9); }
    public void SetMeasureOffByPlayerId10() { SetMeasureOffOwner(10); }
    public void SetMeasureOffByPlayerId11() { SetMeasureOffOwner(11); }
    public void SetMeasureOffByPlayerId12() { SetMeasureOffOwner(12); }
    public void SetMeasureOffByPlayerId13() { SetMeasureOffOwner(13); }
    public void SetMeasureOffByPlayerId14() { SetMeasureOffOwner(14); }
    public void SetMeasureOffByPlayerId15() { SetMeasureOffOwner(15); }
    public void SetMeasureOffByPlayerId16() { SetMeasureOffOwner(16); }
    public void SetMeasureOffByPlayerId17() { SetMeasureOffOwner(17); }
    public void SetMeasureOffByPlayerId18() { SetMeasureOffOwner(18); }
    public void SetMeasureOffByPlayerId19() { SetMeasureOffOwner(19); }
    public void SetMeasureOffByPlayerId20() { SetMeasureOffOwner(20); }
    public void SetMeasureOffByPlayerId21() { SetMeasureOffOwner(21); }
    public void SetMeasureOffByPlayerId22() { SetMeasureOffOwner(22); }
    public void SetMeasureOffByPlayerId23() { SetMeasureOffOwner(23); }
    public void SetMeasureOffByPlayerId24() { SetMeasureOffOwner(24); }
    public void SetMeasureOffByPlayerId25() { SetMeasureOffOwner(25); }
    public void SetMeasureOffByPlayerId26() { SetMeasureOffOwner(26); }
    public void SetMeasureOffByPlayerId27() { SetMeasureOffOwner(27); }
    public void SetMeasureOffByPlayerId28() { SetMeasureOffOwner(28); }
    public void SetMeasureOffByPlayerId29() { SetMeasureOffOwner(29); }
    public void SetMeasureOffByPlayerId30() { SetMeasureOffOwner(30); }
    public void SetMeasureOffByPlayerId31() { SetMeasureOffOwner(31); }
    public void SetMeasureOffByPlayerId32() { SetMeasureOffOwner(32); }
    public void SetMeasureOffByPlayerId33() { SetMeasureOffOwner(33); }
    public void SetMeasureOffByPlayerId34() { SetMeasureOffOwner(34); }
    public void SetMeasureOffByPlayerId35() { SetMeasureOffOwner(35); }
    public void SetMeasureOffByPlayerId36() { SetMeasureOffOwner(36); }
    public void SetMeasureOffByPlayerId37() { SetMeasureOffOwner(37); }
    public void SetMeasureOffByPlayerId38() { SetMeasureOffOwner(38); }
    public void SetMeasureOffByPlayerId39() { SetMeasureOffOwner(39); }
    public void SetMeasureOffByPlayerId40() { SetMeasureOffOwner(40); }
    public void SetMeasureOffByPlayerId41() { SetMeasureOffOwner(41); }
    public void SetMeasureOffByPlayerId42() { SetMeasureOffOwner(42); }
    public void SetMeasureOffByPlayerId43() { SetMeasureOffOwner(43); }
    public void SetMeasureOffByPlayerId44() { SetMeasureOffOwner(44); }
    public void SetMeasureOffByPlayerId45() { SetMeasureOffOwner(45); }
    public void SetMeasureOffByPlayerId46() { SetMeasureOffOwner(46); }
    public void SetMeasureOffByPlayerId47() { SetMeasureOffOwner(47); }
    public void SetMeasureOffByPlayerId48() { SetMeasureOffOwner(48); }
    public void SetMeasureOffByPlayerId49() { SetMeasureOffOwner(49); }
    public void SetMeasureOffByPlayerId50() { SetMeasureOffOwner(50); }
    public void SetMeasureOffByPlayerId51() { SetMeasureOffOwner(51); }
    public void SetMeasureOffByPlayerId52() { SetMeasureOffOwner(52); }
    public void SetMeasureOffByPlayerId53() { SetMeasureOffOwner(53); }
    public void SetMeasureOffByPlayerId54() { SetMeasureOffOwner(54); }
    public void SetMeasureOffByPlayerId55() { SetMeasureOffOwner(55); }
    public void SetMeasureOffByPlayerId56() { SetMeasureOffOwner(56); }
    public void SetMeasureOffByPlayerId57() { SetMeasureOffOwner(57); }
    public void SetMeasureOffByPlayerId58() { SetMeasureOffOwner(58); }
    public void SetMeasureOffByPlayerId59() { SetMeasureOffOwner(59); }
    public void SetMeasureOffByPlayerId60() { SetMeasureOffOwner(60); }
    public void SetMeasureOffByPlayerId61() { SetMeasureOffOwner(61); }
    public void SetMeasureOffByPlayerId62() { SetMeasureOffOwner(62); }
    public void SetMeasureOffByPlayerId63() { SetMeasureOffOwner(63); }
    public void SetMeasureOffByPlayerId64() { SetMeasureOffOwner(64); }
    public void SetMeasureOffByPlayerId65() { SetMeasureOffOwner(65); }
    public void SetMeasureOffByPlayerId66() { SetMeasureOffOwner(66); }
    public void SetMeasureOffByPlayerId67() { SetMeasureOffOwner(67); }
    public void SetMeasureOffByPlayerId68() { SetMeasureOffOwner(68); }
    public void SetMeasureOffByPlayerId69() { SetMeasureOffOwner(69); }
    public void SetMeasureOffByPlayerId70() { SetMeasureOffOwner(70); }
    public void SetMeasureOffByPlayerId71() { SetMeasureOffOwner(71); }
    public void SetMeasureOffByPlayerId72() { SetMeasureOffOwner(72); }
    public void SetMeasureOffByPlayerId73() { SetMeasureOffOwner(73); }
    public void SetMeasureOffByPlayerId74() { SetMeasureOffOwner(74); }
    public void SetMeasureOffByPlayerId75() { SetMeasureOffOwner(75); }
    public void SetMeasureOffByPlayerId76() { SetMeasureOffOwner(76); }
    public void SetMeasureOffByPlayerId77() { SetMeasureOffOwner(77); }
    public void SetMeasureOffByPlayerId78() { SetMeasureOffOwner(78); }
    public void SetMeasureOffByPlayerId79() { SetMeasureOffOwner(79); }
    public void SetMeasureOffByPlayerId80() { SetMeasureOffOwner(80); }
    public void SetMeasureOffByPlayerId81() { SetMeasureOffOwner(81); }
    public void SetMeasureOffByPlayerId82() { SetMeasureOffOwner(82); }
    public void SetMeasureOffByPlayerId83() { SetMeasureOffOwner(83); }
    public void SetMeasureOffByPlayerId84() { SetMeasureOffOwner(84); }
    public void SetMeasureOffByPlayerId85() { SetMeasureOffOwner(85); }
    public void SetMeasureOffByPlayerId86() { SetMeasureOffOwner(86); }
    public void SetMeasureOffByPlayerId87() { SetMeasureOffOwner(87); }
    public void SetMeasureOffByPlayerId88() { SetMeasureOffOwner(88); }
    public void SetMeasureOffByPlayerId89() { SetMeasureOffOwner(89); }
    public void SetMeasureOffByPlayerId90() { SetMeasureOffOwner(90); }
    public void SetMeasureOffByPlayerId91() { SetMeasureOffOwner(91); }
    public void SetMeasureOffByPlayerId92() { SetMeasureOffOwner(92); }
    public void SetMeasureOffByPlayerId93() { SetMeasureOffOwner(93); }
    public void SetMeasureOffByPlayerId94() { SetMeasureOffOwner(94); }
    public void SetMeasureOffByPlayerId95() { SetMeasureOffOwner(95); }
    public void SetMeasureOffByPlayerId96() { SetMeasureOffOwner(96); }
    public void SetMeasureOffByPlayerId97() { SetMeasureOffOwner(97); }
    public void SetMeasureOffByPlayerId98() { SetMeasureOffOwner(98); }
    public void SetMeasureOffByPlayerId99() { SetMeasureOffOwner(99); }
    public void SetMeasureOffByPlayerId100() { SetMeasureOffOwner(100); }
    public void SetMeasureOffByPlayerId101() { SetMeasureOffOwner(101); }
    public void SetMeasureOffByPlayerId102() { SetMeasureOffOwner(102); }
    public void SetMeasureOffByPlayerId103() { SetMeasureOffOwner(103); }
    public void SetMeasureOffByPlayerId104() { SetMeasureOffOwner(104); }
    public void SetMeasureOffByPlayerId105() { SetMeasureOffOwner(105); }
    public void SetMeasureOffByPlayerId106() { SetMeasureOffOwner(106); }
    public void SetMeasureOffByPlayerId107() { SetMeasureOffOwner(107); }
    public void SetMeasureOffByPlayerId108() { SetMeasureOffOwner(108); }
    public void SetMeasureOffByPlayerId109() { SetMeasureOffOwner(109); }
    public void SetMeasureOffByPlayerId110() { SetMeasureOffOwner(110); }
    public void SetMeasureOffByPlayerId111() { SetMeasureOffOwner(111); }
    public void SetMeasureOffByPlayerId112() { SetMeasureOffOwner(112); }
    public void SetMeasureOffByPlayerId113() { SetMeasureOffOwner(113); }
    public void SetMeasureOffByPlayerId114() { SetMeasureOffOwner(114); }
    public void SetMeasureOffByPlayerId115() { SetMeasureOffOwner(115); }
    public void SetMeasureOffByPlayerId116() { SetMeasureOffOwner(116); }
    public void SetMeasureOffByPlayerId117() { SetMeasureOffOwner(117); }
    public void SetMeasureOffByPlayerId118() { SetMeasureOffOwner(118); }
    public void SetMeasureOffByPlayerId119() { SetMeasureOffOwner(119); }
    public void SetMeasureOffByPlayerId120() { SetMeasureOffOwner(120); }
    public void SetMeasureOffByPlayerId121() { SetMeasureOffOwner(121); }
    public void SetMeasureOffByPlayerId122() { SetMeasureOffOwner(122); }
    public void SetMeasureOffByPlayerId123() { SetMeasureOffOwner(123); }
    public void SetMeasureOffByPlayerId124() { SetMeasureOffOwner(124); }
    public void SetMeasureOffByPlayerId125() { SetMeasureOffOwner(125); }
    public void SetMeasureOffByPlayerId126() { SetMeasureOffOwner(126); }
    public void SetMeasureOffByPlayerId127() { SetMeasureOffOwner(127); }
    public void SetMeasureOffByPlayerId128() { SetMeasureOffOwner(128); }
    public void SetMeasureOffByPlayerId129() { SetMeasureOffOwner(129); }
    public void SetMeasureOffByPlayerId130() { SetMeasureOffOwner(130); }
    public void SetMeasureOffByPlayerId131() { SetMeasureOffOwner(131); }
    public void SetMeasureOffByPlayerId132() { SetMeasureOffOwner(132); }
    public void SetMeasureOffByPlayerId133() { SetMeasureOffOwner(133); }
    public void SetMeasureOffByPlayerId134() { SetMeasureOffOwner(134); }
    public void SetMeasureOffByPlayerId135() { SetMeasureOffOwner(135); }
    public void SetMeasureOffByPlayerId136() { SetMeasureOffOwner(136); }
    public void SetMeasureOffByPlayerId137() { SetMeasureOffOwner(137); }
    public void SetMeasureOffByPlayerId138() { SetMeasureOffOwner(138); }
    public void SetMeasureOffByPlayerId139() { SetMeasureOffOwner(139); }
    public void SetMeasureOffByPlayerId140() { SetMeasureOffOwner(140); }
    public void SetMeasureOffByPlayerId141() { SetMeasureOffOwner(141); }
    public void SetMeasureOffByPlayerId142() { SetMeasureOffOwner(142); }
    public void SetMeasureOffByPlayerId143() { SetMeasureOffOwner(143); }
    public void SetMeasureOffByPlayerId144() { SetMeasureOffOwner(144); }
    public void SetMeasureOffByPlayerId145() { SetMeasureOffOwner(145); }
    public void SetMeasureOffByPlayerId146() { SetMeasureOffOwner(146); }
    public void SetMeasureOffByPlayerId147() { SetMeasureOffOwner(147); }
    public void SetMeasureOffByPlayerId148() { SetMeasureOffOwner(148); }
    public void SetMeasureOffByPlayerId149() { SetMeasureOffOwner(149); }
    public void SetMeasureOffByPlayerId150() { SetMeasureOffOwner(150); }
    public void SetMeasureOffByPlayerId151() { SetMeasureOffOwner(151); }
    public void SetMeasureOffByPlayerId152() { SetMeasureOffOwner(152); }
    public void SetMeasureOffByPlayerId153() { SetMeasureOffOwner(153); }
    public void SetMeasureOffByPlayerId154() { SetMeasureOffOwner(154); }
    public void SetMeasureOffByPlayerId155() { SetMeasureOffOwner(155); }
    public void SetMeasureOffByPlayerId156() { SetMeasureOffOwner(156); }
    public void SetMeasureOffByPlayerId157() { SetMeasureOffOwner(157); }
    public void SetMeasureOffByPlayerId158() { SetMeasureOffOwner(158); }
    public void SetMeasureOffByPlayerId159() { SetMeasureOffOwner(159); }
    public void SetMeasureOffByPlayerId160() { SetMeasureOffOwner(160); }
    public void SetMeasureOffByPlayerId161() { SetMeasureOffOwner(161); }
    public void SetMeasureOffByPlayerId162() { SetMeasureOffOwner(162); }
    public void SetMeasureOffByPlayerId163() { SetMeasureOffOwner(163); }
    public void SetMeasureOffByPlayerId164() { SetMeasureOffOwner(164); }
    public void SetMeasureOffByPlayerId165() { SetMeasureOffOwner(165); }
    public void SetMeasureOffByPlayerId166() { SetMeasureOffOwner(166); }
    public void SetMeasureOffByPlayerId167() { SetMeasureOffOwner(167); }
    public void SetMeasureOffByPlayerId168() { SetMeasureOffOwner(168); }
    public void SetMeasureOffByPlayerId169() { SetMeasureOffOwner(169); }
    public void SetMeasureOffByPlayerId170() { SetMeasureOffOwner(170); }
    public void SetMeasureOffByPlayerId171() { SetMeasureOffOwner(171); }
    public void SetMeasureOffByPlayerId172() { SetMeasureOffOwner(172); }
    public void SetMeasureOffByPlayerId173() { SetMeasureOffOwner(173); }
    public void SetMeasureOffByPlayerId174() { SetMeasureOffOwner(174); }
    public void SetMeasureOffByPlayerId175() { SetMeasureOffOwner(175); }
    public void SetMeasureOffByPlayerId176() { SetMeasureOffOwner(176); }
    public void SetMeasureOffByPlayerId177() { SetMeasureOffOwner(177); }
    public void SetMeasureOffByPlayerId178() { SetMeasureOffOwner(178); }
    public void SetMeasureOffByPlayerId179() { SetMeasureOffOwner(179); }
    public void SetMeasureOffByPlayerId180() { SetMeasureOffOwner(180); }
    public void SetMeasureOffByPlayerId181() { SetMeasureOffOwner(181); }
    public void SetMeasureOffByPlayerId182() { SetMeasureOffOwner(182); }
    public void SetMeasureOffByPlayerId183() { SetMeasureOffOwner(183); }
    public void SetMeasureOffByPlayerId184() { SetMeasureOffOwner(184); }
    public void SetMeasureOffByPlayerId185() { SetMeasureOffOwner(185); }
    public void SetMeasureOffByPlayerId186() { SetMeasureOffOwner(186); }
    public void SetMeasureOffByPlayerId187() { SetMeasureOffOwner(187); }
    public void SetMeasureOffByPlayerId188() { SetMeasureOffOwner(188); }
    public void SetMeasureOffByPlayerId189() { SetMeasureOffOwner(189); }
    public void SetMeasureOffByPlayerId190() { SetMeasureOffOwner(190); }
    public void SetMeasureOffByPlayerId191() { SetMeasureOffOwner(191); }
    public void SetMeasureOffByPlayerId192() { SetMeasureOffOwner(192); }
    public void SetMeasureOffByPlayerId193() { SetMeasureOffOwner(193); }
    public void SetMeasureOffByPlayerId194() { SetMeasureOffOwner(194); }
    public void SetMeasureOffByPlayerId195() { SetMeasureOffOwner(195); }
    public void SetMeasureOffByPlayerId196() { SetMeasureOffOwner(196); }
    public void SetMeasureOffByPlayerId197() { SetMeasureOffOwner(197); }
    public void SetMeasureOffByPlayerId198() { SetMeasureOffOwner(198); }
    public void SetMeasureOffByPlayerId199() { SetMeasureOffOwner(199); }
    public void SetMeasureOffByPlayerId200() { SetMeasureOffOwner(200); }
    public void SetMeasureOffByPlayerId201() { SetMeasureOffOwner(201); }
    public void SetMeasureOffByPlayerId202() { SetMeasureOffOwner(202); }
    public void SetMeasureOffByPlayerId203() { SetMeasureOffOwner(203); }
    public void SetMeasureOffByPlayerId204() { SetMeasureOffOwner(204); }
    public void SetMeasureOffByPlayerId205() { SetMeasureOffOwner(205); }
    public void SetMeasureOffByPlayerId206() { SetMeasureOffOwner(206); }
    public void SetMeasureOffByPlayerId207() { SetMeasureOffOwner(207); }
    public void SetMeasureOffByPlayerId208() { SetMeasureOffOwner(208); }
    public void SetMeasureOffByPlayerId209() { SetMeasureOffOwner(209); }
    public void SetMeasureOffByPlayerId210() { SetMeasureOffOwner(210); }
    public void SetMeasureOffByPlayerId211() { SetMeasureOffOwner(211); }
    public void SetMeasureOffByPlayerId212() { SetMeasureOffOwner(212); }
    public void SetMeasureOffByPlayerId213() { SetMeasureOffOwner(213); }
    public void SetMeasureOffByPlayerId214() { SetMeasureOffOwner(214); }
    public void SetMeasureOffByPlayerId215() { SetMeasureOffOwner(215); }
    public void SetMeasureOffByPlayerId216() { SetMeasureOffOwner(216); }
    public void SetMeasureOffByPlayerId217() { SetMeasureOffOwner(217); }
    public void SetMeasureOffByPlayerId218() { SetMeasureOffOwner(218); }
    public void SetMeasureOffByPlayerId219() { SetMeasureOffOwner(219); }
    public void SetMeasureOffByPlayerId220() { SetMeasureOffOwner(220); }
    public void SetMeasureOffByPlayerId221() { SetMeasureOffOwner(221); }
    public void SetMeasureOffByPlayerId222() { SetMeasureOffOwner(222); }
    public void SetMeasureOffByPlayerId223() { SetMeasureOffOwner(223); }
    public void SetMeasureOffByPlayerId224() { SetMeasureOffOwner(224); }
    public void SetMeasureOffByPlayerId225() { SetMeasureOffOwner(225); }
    public void SetMeasureOffByPlayerId226() { SetMeasureOffOwner(226); }
    public void SetMeasureOffByPlayerId227() { SetMeasureOffOwner(227); }
    public void SetMeasureOffByPlayerId228() { SetMeasureOffOwner(228); }
    public void SetMeasureOffByPlayerId229() { SetMeasureOffOwner(229); }
    public void SetMeasureOffByPlayerId230() { SetMeasureOffOwner(230); }
    public void SetMeasureOffByPlayerId231() { SetMeasureOffOwner(231); }
    public void SetMeasureOffByPlayerId232() { SetMeasureOffOwner(232); }
    public void SetMeasureOffByPlayerId233() { SetMeasureOffOwner(233); }
    public void SetMeasureOffByPlayerId234() { SetMeasureOffOwner(234); }
    public void SetMeasureOffByPlayerId235() { SetMeasureOffOwner(235); }
    public void SetMeasureOffByPlayerId236() { SetMeasureOffOwner(236); }
    public void SetMeasureOffByPlayerId237() { SetMeasureOffOwner(237); }
    public void SetMeasureOffByPlayerId238() { SetMeasureOffOwner(238); }
    public void SetMeasureOffByPlayerId239() { SetMeasureOffOwner(239); }
    public void SetMeasureOffByPlayerId240() { SetMeasureOffOwner(240); }
    public void SetMeasureOffByPlayerId241() { SetMeasureOffOwner(241); }
    public void SetMeasureOffByPlayerId242() { SetMeasureOffOwner(242); }
    public void SetMeasureOffByPlayerId243() { SetMeasureOffOwner(243); }
    public void SetMeasureOffByPlayerId244() { SetMeasureOffOwner(244); }
    public void SetMeasureOffByPlayerId245() { SetMeasureOffOwner(245); }
    public void SetMeasureOffByPlayerId246() { SetMeasureOffOwner(246); }
    public void SetMeasureOffByPlayerId247() { SetMeasureOffOwner(247); }
    public void SetMeasureOffByPlayerId248() { SetMeasureOffOwner(248); }
    public void SetMeasureOffByPlayerId249() { SetMeasureOffOwner(249); }
    public void SetMeasureOffByPlayerId250() { SetMeasureOffOwner(250); }
    public void SetMeasureOffByPlayerId251() { SetMeasureOffOwner(251); }
    public void SetMeasureOffByPlayerId252() { SetMeasureOffOwner(252); }
    public void SetMeasureOffByPlayerId253() { SetMeasureOffOwner(253); }
    public void SetMeasureOffByPlayerId254() { SetMeasureOffOwner(254); }
    public void SetMeasureOffByPlayerId255() { SetMeasureOffOwner(255); }
    public void SetMeasureOffByPlayerId256() { SetMeasureOffOwner(256); }
    public void SetMeasureOffByPlayerId257() { SetMeasureOffOwner(257); }
    public void SetMeasureOffByPlayerId258() { SetMeasureOffOwner(258); }
    public void SetMeasureOffByPlayerId259() { SetMeasureOffOwner(259); }
    public void SetMeasureOffByPlayerId260() { SetMeasureOffOwner(260); }
    public void SetMeasureOffByPlayerId261() { SetMeasureOffOwner(261); }
    public void SetMeasureOffByPlayerId262() { SetMeasureOffOwner(262); }
    public void SetMeasureOffByPlayerId263() { SetMeasureOffOwner(263); }
    public void SetMeasureOffByPlayerId264() { SetMeasureOffOwner(264); }
    public void SetMeasureOffByPlayerId265() { SetMeasureOffOwner(265); }
    public void SetMeasureOffByPlayerId266() { SetMeasureOffOwner(266); }
    public void SetMeasureOffByPlayerId267() { SetMeasureOffOwner(267); }
    public void SetMeasureOffByPlayerId268() { SetMeasureOffOwner(268); }
    public void SetMeasureOffByPlayerId269() { SetMeasureOffOwner(269); }
    public void SetMeasureOffByPlayerId270() { SetMeasureOffOwner(270); }
    public void SetMeasureOffByPlayerId271() { SetMeasureOffOwner(271); }
    public void SetMeasureOffByPlayerId272() { SetMeasureOffOwner(272); }
    public void SetMeasureOffByPlayerId273() { SetMeasureOffOwner(273); }
    public void SetMeasureOffByPlayerId274() { SetMeasureOffOwner(274); }
    public void SetMeasureOffByPlayerId275() { SetMeasureOffOwner(275); }
    public void SetMeasureOffByPlayerId276() { SetMeasureOffOwner(276); }
    public void SetMeasureOffByPlayerId277() { SetMeasureOffOwner(277); }
    public void SetMeasureOffByPlayerId278() { SetMeasureOffOwner(278); }
    public void SetMeasureOffByPlayerId279() { SetMeasureOffOwner(279); }
    public void SetMeasureOffByPlayerId280() { SetMeasureOffOwner(280); }
    public void SetMeasureOffByPlayerId281() { SetMeasureOffOwner(281); }
    public void SetMeasureOffByPlayerId282() { SetMeasureOffOwner(282); }
    public void SetMeasureOffByPlayerId283() { SetMeasureOffOwner(283); }
    public void SetMeasureOffByPlayerId284() { SetMeasureOffOwner(284); }
    public void SetMeasureOffByPlayerId285() { SetMeasureOffOwner(285); }
    public void SetMeasureOffByPlayerId286() { SetMeasureOffOwner(286); }
    public void SetMeasureOffByPlayerId287() { SetMeasureOffOwner(287); }
    public void SetMeasureOffByPlayerId288() { SetMeasureOffOwner(288); }
    public void SetMeasureOffByPlayerId289() { SetMeasureOffOwner(289); }
    public void SetMeasureOffByPlayerId290() { SetMeasureOffOwner(290); }
    public void SetMeasureOffByPlayerId291() { SetMeasureOffOwner(291); }
    public void SetMeasureOffByPlayerId292() { SetMeasureOffOwner(292); }
    public void SetMeasureOffByPlayerId293() { SetMeasureOffOwner(293); }
    public void SetMeasureOffByPlayerId294() { SetMeasureOffOwner(294); }
    public void SetMeasureOffByPlayerId295() { SetMeasureOffOwner(295); }
    public void SetMeasureOffByPlayerId296() { SetMeasureOffOwner(296); }
    public void SetMeasureOffByPlayerId297() { SetMeasureOffOwner(297); }
    public void SetMeasureOffByPlayerId298() { SetMeasureOffOwner(298); }
    public void SetMeasureOffByPlayerId299() { SetMeasureOffOwner(299); }
    public void SetMeasureOffByPlayerId300() { SetMeasureOffOwner(300); }
    public void SetMeasureOffByPlayerId301() { SetMeasureOffOwner(301); }
    public void SetMeasureOffByPlayerId302() { SetMeasureOffOwner(302); }
    public void SetMeasureOffByPlayerId303() { SetMeasureOffOwner(303); }
    public void SetMeasureOffByPlayerId304() { SetMeasureOffOwner(304); }
    public void SetMeasureOffByPlayerId305() { SetMeasureOffOwner(305); }
    public void SetMeasureOffByPlayerId306() { SetMeasureOffOwner(306); }
    public void SetMeasureOffByPlayerId307() { SetMeasureOffOwner(307); }
    public void SetMeasureOffByPlayerId308() { SetMeasureOffOwner(308); }
    public void SetMeasureOffByPlayerId309() { SetMeasureOffOwner(309); }
    public void SetMeasureOffByPlayerId310() { SetMeasureOffOwner(310); }
    public void SetMeasureOffByPlayerId311() { SetMeasureOffOwner(311); }
    public void SetMeasureOffByPlayerId312() { SetMeasureOffOwner(312); }
    public void SetMeasureOffByPlayerId313() { SetMeasureOffOwner(313); }
    public void SetMeasureOffByPlayerId314() { SetMeasureOffOwner(314); }
    public void SetMeasureOffByPlayerId315() { SetMeasureOffOwner(315); }
    public void SetMeasureOffByPlayerId316() { SetMeasureOffOwner(316); }
    public void SetMeasureOffByPlayerId317() { SetMeasureOffOwner(317); }
    public void SetMeasureOffByPlayerId318() { SetMeasureOffOwner(318); }
    public void SetMeasureOffByPlayerId319() { SetMeasureOffOwner(319); }
    public void SetMeasureOffByPlayerId320() { SetMeasureOffOwner(320); }
    public void SetMeasureOffByPlayerId321() { SetMeasureOffOwner(321); }
    public void SetMeasureOffByPlayerId322() { SetMeasureOffOwner(322); }
    public void SetMeasureOffByPlayerId323() { SetMeasureOffOwner(323); }
    public void SetMeasureOffByPlayerId324() { SetMeasureOffOwner(324); }
    public void SetMeasureOffByPlayerId325() { SetMeasureOffOwner(325); }
    public void SetMeasureOffByPlayerId326() { SetMeasureOffOwner(326); }
    public void SetMeasureOffByPlayerId327() { SetMeasureOffOwner(327); }
    public void SetMeasureOffByPlayerId328() { SetMeasureOffOwner(328); }
    public void SetMeasureOffByPlayerId329() { SetMeasureOffOwner(329); }
    public void SetMeasureOffByPlayerId330() { SetMeasureOffOwner(330); }
    public void SetMeasureOffByPlayerId331() { SetMeasureOffOwner(331); }
    public void SetMeasureOffByPlayerId332() { SetMeasureOffOwner(332); }
    public void SetMeasureOffByPlayerId333() { SetMeasureOffOwner(333); }
    public void SetMeasureOffByPlayerId334() { SetMeasureOffOwner(334); }
    public void SetMeasureOffByPlayerId335() { SetMeasureOffOwner(335); }
    public void SetMeasureOffByPlayerId336() { SetMeasureOffOwner(336); }
    public void SetMeasureOffByPlayerId337() { SetMeasureOffOwner(337); }
    public void SetMeasureOffByPlayerId338() { SetMeasureOffOwner(338); }
    public void SetMeasureOffByPlayerId339() { SetMeasureOffOwner(339); }
    public void SetMeasureOffByPlayerId340() { SetMeasureOffOwner(340); }
    public void SetMeasureOffByPlayerId341() { SetMeasureOffOwner(341); }
    public void SetMeasureOffByPlayerId342() { SetMeasureOffOwner(342); }
    public void SetMeasureOffByPlayerId343() { SetMeasureOffOwner(343); }
    public void SetMeasureOffByPlayerId344() { SetMeasureOffOwner(344); }
    public void SetMeasureOffByPlayerId345() { SetMeasureOffOwner(345); }
    public void SetMeasureOffByPlayerId346() { SetMeasureOffOwner(346); }
    public void SetMeasureOffByPlayerId347() { SetMeasureOffOwner(347); }
    public void SetMeasureOffByPlayerId348() { SetMeasureOffOwner(348); }
    public void SetMeasureOffByPlayerId349() { SetMeasureOffOwner(349); }
    public void SetMeasureOffByPlayerId350() { SetMeasureOffOwner(350); }
    public void SetMeasureOffByPlayerId351() { SetMeasureOffOwner(351); }
    public void SetMeasureOffByPlayerId352() { SetMeasureOffOwner(352); }
    public void SetMeasureOffByPlayerId353() { SetMeasureOffOwner(353); }
    public void SetMeasureOffByPlayerId354() { SetMeasureOffOwner(354); }
    public void SetMeasureOffByPlayerId355() { SetMeasureOffOwner(355); }
    public void SetMeasureOffByPlayerId356() { SetMeasureOffOwner(356); }
    public void SetMeasureOffByPlayerId357() { SetMeasureOffOwner(357); }
    public void SetMeasureOffByPlayerId358() { SetMeasureOffOwner(358); }
    public void SetMeasureOffByPlayerId359() { SetMeasureOffOwner(359); }
    public void SetMeasureOffByPlayerId360() { SetMeasureOffOwner(360); }
    public void SetMeasureOffByPlayerId361() { SetMeasureOffOwner(361); }
    public void SetMeasureOffByPlayerId362() { SetMeasureOffOwner(362); }
    public void SetMeasureOffByPlayerId363() { SetMeasureOffOwner(363); }
    public void SetMeasureOffByPlayerId364() { SetMeasureOffOwner(364); }
    public void SetMeasureOffByPlayerId365() { SetMeasureOffOwner(365); }
    public void SetMeasureOffByPlayerId366() { SetMeasureOffOwner(366); }
    public void SetMeasureOffByPlayerId367() { SetMeasureOffOwner(367); }
    public void SetMeasureOffByPlayerId368() { SetMeasureOffOwner(368); }
    public void SetMeasureOffByPlayerId369() { SetMeasureOffOwner(369); }
    public void SetMeasureOffByPlayerId370() { SetMeasureOffOwner(370); }
    public void SetMeasureOffByPlayerId371() { SetMeasureOffOwner(371); }
    public void SetMeasureOffByPlayerId372() { SetMeasureOffOwner(372); }
    public void SetMeasureOffByPlayerId373() { SetMeasureOffOwner(373); }
    public void SetMeasureOffByPlayerId374() { SetMeasureOffOwner(374); }
    public void SetMeasureOffByPlayerId375() { SetMeasureOffOwner(375); }
    public void SetMeasureOffByPlayerId376() { SetMeasureOffOwner(376); }
    public void SetMeasureOffByPlayerId377() { SetMeasureOffOwner(377); }
    public void SetMeasureOffByPlayerId378() { SetMeasureOffOwner(378); }
    public void SetMeasureOffByPlayerId379() { SetMeasureOffOwner(379); }
    public void SetMeasureOffByPlayerId380() { SetMeasureOffOwner(380); }
    public void SetMeasureOffByPlayerId381() { SetMeasureOffOwner(381); }
    public void SetMeasureOffByPlayerId382() { SetMeasureOffOwner(382); }
    public void SetMeasureOffByPlayerId383() { SetMeasureOffOwner(383); }
    public void SetMeasureOffByPlayerId384() { SetMeasureOffOwner(384); }
    public void SetMeasureOffByPlayerId385() { SetMeasureOffOwner(385); }
    public void SetMeasureOffByPlayerId386() { SetMeasureOffOwner(386); }
    public void SetMeasureOffByPlayerId387() { SetMeasureOffOwner(387); }
    public void SetMeasureOffByPlayerId388() { SetMeasureOffOwner(388); }
    public void SetMeasureOffByPlayerId389() { SetMeasureOffOwner(389); }
    public void SetMeasureOffByPlayerId390() { SetMeasureOffOwner(390); }
    public void SetMeasureOffByPlayerId391() { SetMeasureOffOwner(391); }
    public void SetMeasureOffByPlayerId392() { SetMeasureOffOwner(392); }
    public void SetMeasureOffByPlayerId393() { SetMeasureOffOwner(393); }
    public void SetMeasureOffByPlayerId394() { SetMeasureOffOwner(394); }
    public void SetMeasureOffByPlayerId395() { SetMeasureOffOwner(395); }
    public void SetMeasureOffByPlayerId396() { SetMeasureOffOwner(396); }
    public void SetMeasureOffByPlayerId397() { SetMeasureOffOwner(397); }
    public void SetMeasureOffByPlayerId398() { SetMeasureOffOwner(398); }
    public void SetMeasureOffByPlayerId399() { SetMeasureOffOwner(399); }
    public void SetMeasureOffByPlayerId400() { SetMeasureOffOwner(400); }
    public void SetMeasureOffByPlayerId401() { SetMeasureOffOwner(401); }
    public void SetMeasureOffByPlayerId402() { SetMeasureOffOwner(402); }
    public void SetMeasureOffByPlayerId403() { SetMeasureOffOwner(403); }
    public void SetMeasureOffByPlayerId404() { SetMeasureOffOwner(404); }
    public void SetMeasureOffByPlayerId405() { SetMeasureOffOwner(405); }
    public void SetMeasureOffByPlayerId406() { SetMeasureOffOwner(406); }
    public void SetMeasureOffByPlayerId407() { SetMeasureOffOwner(407); }
    public void SetMeasureOffByPlayerId408() { SetMeasureOffOwner(408); }
    public void SetMeasureOffByPlayerId409() { SetMeasureOffOwner(409); }
    public void SetMeasureOffByPlayerId410() { SetMeasureOffOwner(410); }
    public void SetMeasureOffByPlayerId411() { SetMeasureOffOwner(411); }
    public void SetMeasureOffByPlayerId412() { SetMeasureOffOwner(412); }
    public void SetMeasureOffByPlayerId413() { SetMeasureOffOwner(413); }
    public void SetMeasureOffByPlayerId414() { SetMeasureOffOwner(414); }
    public void SetMeasureOffByPlayerId415() { SetMeasureOffOwner(415); }
    public void SetMeasureOffByPlayerId416() { SetMeasureOffOwner(416); }
    public void SetMeasureOffByPlayerId417() { SetMeasureOffOwner(417); }
    public void SetMeasureOffByPlayerId418() { SetMeasureOffOwner(418); }
    public void SetMeasureOffByPlayerId419() { SetMeasureOffOwner(419); }
    public void SetMeasureOffByPlayerId420() { SetMeasureOffOwner(420); }
    public void SetMeasureOffByPlayerId421() { SetMeasureOffOwner(421); }
    public void SetMeasureOffByPlayerId422() { SetMeasureOffOwner(422); }
    public void SetMeasureOffByPlayerId423() { SetMeasureOffOwner(423); }
    public void SetMeasureOffByPlayerId424() { SetMeasureOffOwner(424); }
    public void SetMeasureOffByPlayerId425() { SetMeasureOffOwner(425); }
    public void SetMeasureOffByPlayerId426() { SetMeasureOffOwner(426); }
    public void SetMeasureOffByPlayerId427() { SetMeasureOffOwner(427); }
    public void SetMeasureOffByPlayerId428() { SetMeasureOffOwner(428); }
    public void SetMeasureOffByPlayerId429() { SetMeasureOffOwner(429); }
    public void SetMeasureOffByPlayerId430() { SetMeasureOffOwner(430); }
    public void SetMeasureOffByPlayerId431() { SetMeasureOffOwner(431); }
    public void SetMeasureOffByPlayerId432() { SetMeasureOffOwner(432); }
    public void SetMeasureOffByPlayerId433() { SetMeasureOffOwner(433); }
    public void SetMeasureOffByPlayerId434() { SetMeasureOffOwner(434); }
    public void SetMeasureOffByPlayerId435() { SetMeasureOffOwner(435); }
    public void SetMeasureOffByPlayerId436() { SetMeasureOffOwner(436); }
    public void SetMeasureOffByPlayerId437() { SetMeasureOffOwner(437); }
    public void SetMeasureOffByPlayerId438() { SetMeasureOffOwner(438); }
    public void SetMeasureOffByPlayerId439() { SetMeasureOffOwner(439); }
    public void SetMeasureOffByPlayerId440() { SetMeasureOffOwner(440); }
    public void SetMeasureOffByPlayerId441() { SetMeasureOffOwner(441); }
    public void SetMeasureOffByPlayerId442() { SetMeasureOffOwner(442); }
    public void SetMeasureOffByPlayerId443() { SetMeasureOffOwner(443); }
    public void SetMeasureOffByPlayerId444() { SetMeasureOffOwner(444); }
    public void SetMeasureOffByPlayerId445() { SetMeasureOffOwner(445); }
    public void SetMeasureOffByPlayerId446() { SetMeasureOffOwner(446); }
    public void SetMeasureOffByPlayerId447() { SetMeasureOffOwner(447); }
    public void SetMeasureOffByPlayerId448() { SetMeasureOffOwner(448); }
    public void SetMeasureOffByPlayerId449() { SetMeasureOffOwner(449); }
    public void SetMeasureOffByPlayerId450() { SetMeasureOffOwner(450); }
    public void SetMeasureOffByPlayerId451() { SetMeasureOffOwner(451); }
    public void SetMeasureOffByPlayerId452() { SetMeasureOffOwner(452); }
    public void SetMeasureOffByPlayerId453() { SetMeasureOffOwner(453); }
    public void SetMeasureOffByPlayerId454() { SetMeasureOffOwner(454); }
    public void SetMeasureOffByPlayerId455() { SetMeasureOffOwner(455); }
    public void SetMeasureOffByPlayerId456() { SetMeasureOffOwner(456); }
    public void SetMeasureOffByPlayerId457() { SetMeasureOffOwner(457); }
    public void SetMeasureOffByPlayerId458() { SetMeasureOffOwner(458); }
    public void SetMeasureOffByPlayerId459() { SetMeasureOffOwner(459); }
    public void SetMeasureOffByPlayerId460() { SetMeasureOffOwner(460); }
    public void SetMeasureOffByPlayerId461() { SetMeasureOffOwner(461); }
    public void SetMeasureOffByPlayerId462() { SetMeasureOffOwner(462); }
    public void SetMeasureOffByPlayerId463() { SetMeasureOffOwner(463); }
    public void SetMeasureOffByPlayerId464() { SetMeasureOffOwner(464); }
    public void SetMeasureOffByPlayerId465() { SetMeasureOffOwner(465); }
    public void SetMeasureOffByPlayerId466() { SetMeasureOffOwner(466); }
    public void SetMeasureOffByPlayerId467() { SetMeasureOffOwner(467); }
    public void SetMeasureOffByPlayerId468() { SetMeasureOffOwner(468); }
    public void SetMeasureOffByPlayerId469() { SetMeasureOffOwner(469); }
    public void SetMeasureOffByPlayerId470() { SetMeasureOffOwner(470); }
    public void SetMeasureOffByPlayerId471() { SetMeasureOffOwner(471); }
    public void SetMeasureOffByPlayerId472() { SetMeasureOffOwner(472); }
    public void SetMeasureOffByPlayerId473() { SetMeasureOffOwner(473); }
    public void SetMeasureOffByPlayerId474() { SetMeasureOffOwner(474); }
    public void SetMeasureOffByPlayerId475() { SetMeasureOffOwner(475); }
    public void SetMeasureOffByPlayerId476() { SetMeasureOffOwner(476); }
    public void SetMeasureOffByPlayerId477() { SetMeasureOffOwner(477); }
    public void SetMeasureOffByPlayerId478() { SetMeasureOffOwner(478); }
    public void SetMeasureOffByPlayerId479() { SetMeasureOffOwner(479); }
    public void SetMeasureOffByPlayerId480() { SetMeasureOffOwner(480); }
    public void SetMeasureOffByPlayerId481() { SetMeasureOffOwner(481); }
    public void SetMeasureOffByPlayerId482() { SetMeasureOffOwner(482); }
    public void SetMeasureOffByPlayerId483() { SetMeasureOffOwner(483); }
    public void SetMeasureOffByPlayerId484() { SetMeasureOffOwner(484); }
    public void SetMeasureOffByPlayerId485() { SetMeasureOffOwner(485); }
    public void SetMeasureOffByPlayerId486() { SetMeasureOffOwner(486); }
    public void SetMeasureOffByPlayerId487() { SetMeasureOffOwner(487); }
    public void SetMeasureOffByPlayerId488() { SetMeasureOffOwner(488); }
    public void SetMeasureOffByPlayerId489() { SetMeasureOffOwner(489); }
    public void SetMeasureOffByPlayerId490() { SetMeasureOffOwner(490); }
    public void SetMeasureOffByPlayerId491() { SetMeasureOffOwner(491); }
    public void SetMeasureOffByPlayerId492() { SetMeasureOffOwner(492); }
    public void SetMeasureOffByPlayerId493() { SetMeasureOffOwner(493); }
    public void SetMeasureOffByPlayerId494() { SetMeasureOffOwner(494); }
    public void SetMeasureOffByPlayerId495() { SetMeasureOffOwner(495); }
    public void SetMeasureOffByPlayerId496() { SetMeasureOffOwner(496); }
    public void SetMeasureOffByPlayerId497() { SetMeasureOffOwner(497); }
    public void SetMeasureOffByPlayerId498() { SetMeasureOffOwner(498); }
    public void SetMeasureOffByPlayerId499() { SetMeasureOffOwner(499); }
    public void SetMeasureOffByPlayerId500() { SetMeasureOffOwner(500); }
}
