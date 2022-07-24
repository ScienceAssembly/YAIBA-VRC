
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class QuestionnaireSend : UdonSharpBehaviour
{
    [SerializeField] private Questionnaire _questionnaire;
    [SerializeField] private StaffSetting _staffSetting;
    private bool _requestSend = false;
    private bool _sendResult = false;
    private bool _isStaff = false;
    private int _lastPlayerId = 0;

    // =================================
    // 同期変数
    // =================================
    [UdonSynced] private int[] _answer;
    private int _answerNum = 0;

    // =================================
    // 同期変数更新時の処理
    // =================================
    public override void OnDeserialization()
    {
        AnswerChanged();
    }
    private void AnswerChanged()
    {
        if (_isStaff) {
            if (_answer.Length == _answerNum) {
                if (_lastPlayerId != _answer[0]) {
                    _questionnaire.OutputAnswerLog(_answer);
                    _lastPlayerId = _answer[0];
                }
            }
        }
    }

    //================================
    //  外部関数
    //================================
    public void RequestSend()
    {
        _requestSend = true;
        _sendResult = false;
    }
    public bool GetRequestSend()
    {
        return _requestSend;
    }
    public bool GetSendResult()
    {
        if (_sendResult) {
            _requestSend = false;
        }
        return _sendResult;
    }
    public void SetAwnser(int[] answer)
    {
        _answer = answer;
        RequestSerialization();
        _sendResult = true;
        AnswerChanged();
    }
    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi requestedOwner)
    {
        if (Networking.LocalPlayer.playerId == requestingPlayer.playerId) {
            return true;
        } else {
            return !_requestSend;
        }
    }

    //================================
    //  内部処理
    //================================
    void Start()
    {
        _isStaff = _staffSetting.IsStaff(Networking.LocalPlayer.displayName);
        _answerNum = _questionnaire.GetAnswerLen();
    }
}
