﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Questionnaire : UdonSharpBehaviour
{
    [SerializeField] private GameObject _panelInit; //status:0
    [SerializeField] private GameObject _panelQ; //status:1 ~ _questionNum
    [SerializeField] private GameObject _panelConfirm; //status:_questionNum + 1
    [SerializeField] private GameObject _panelEnd; //status:_questionNum + 2
    [SerializeField] private QuestionnaireSend _questionnaireSendGimmick;
    [SerializeField] private GameObject[] _confirmText;
    private int _questionNum; // 最大11まで。それ以上の値は設定しないこと
    private const int ANSWER_NUM = 6;
    private int _status = 0;
    private float _elaspedTime = 0.0f;
    [Header("======== Questions ========")]
    [Header("質問文と選択肢の設定。QuestionsのSizeは11以下とすること。")]
    [Header("各Elementは第一項が質問文、それ以降は選択肢（最大6個まで）。各ElementのSizeは7以下とすること。")]
    [Header("========================")]
    [OdinSerialize] public string[][] _questions = new string[][] { // _questionNumで指定した件数を指定すること。各行は、第一項が質問文、それ以降は選択肢（最大6個まで）
        new string[] {"当イベントを何で知りましたか", "Twitter", "人に紹介してもらった", "フレンドにJoinした", "フレンドにInviteされた", "YouTubeチャンネルを見た", "その他"},
        new string[] {"何のTwitterで知りましたか？", "公式Twitterアカウント", "フレンドのTwitter", "その他のTwitter"},
        new string[] {"イベントに満足しましたか？", "とても満足した", "満足した", "どちらでもない", "満足していない", "とても満足していない"},
        new string[] {"運営スタッフに満足しましたか？", "とても満足した", "満足した", "どちらでもない", "満足していない", "とても満足していない"},
        new string[] {"イベント用ワールドに満足しましたか？", "とても満足した", "満足した", "どちらでもない", "満足していない", "とても満足していない"},
        new string[] {"イベントで参加者同士と交流できましたか？", "とてもできた", "たぶんできた", "どちらでもない", "あまりできなかった", "できなかった"},
        new string[] {"一番面白かったコンテンツを教えてください。", "AAA", "BBB", "CCC", "参加者との交流", "その他"},
        new string[] {"次回、このイベントがあれば参加したいですか？", "とても参加したい", "参加したい", "どちらでもない", "あまり参加したくない", "参加したくない"},
    };
    [Header("======== Questions Fontsize ========")]
    [Header("質問文と選択肢のフォントサイズ。QuestionsのSizeと合わせること。")]
    [Header("===============================")]
    [OdinSerialize] public int[][] _questionsFontsize = new int[][] { // 質問文、選択肢のフォントサイズ
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
        new int[] {223, 110, 110, 110, 110, 110, 110},
    };
    [Header("======== Next Question ========")]
    [Header("選択肢毎の次の質問番号。QuestionsのSizeと合わせること。")]
    [Header("質問1の質問番号は[0]、質問2の質問番号は[1]...となる。")]
    [Header("質問番号を[QuestionsのSize]とすると、アンケート確認画面に遷移する。")]
    [Header("===========================")]
    [OdinSerialize] public int[][] _nextQuestion = new int[][] { // 選択肢毎の次の質問番号。質問1の質問番号は[0]、質問2の質問番号は[1]...となる。質問番号[_questionNum]とすると、アンケート確認画面に遷移する。
        new int[] { 1   ,  2    ,  2    ,  2    ,  2    ,  2}, // [0] 質問1の選択肢毎の次の質問の番号
        new int[] { 2   ,  2    ,  2    ,  2    ,  2    ,  2}, // [1] 質問2の選択肢毎の次の質問の番号
        new int[] { 3   ,  3    ,  3    ,  3    ,  3    ,  3}, // [2] 質問3の選択肢毎の次の質問の番号
        new int[] { 4   ,  4    ,  4    ,  4    ,  4    ,  4}, // [3] 質問4の選択肢毎の次の質問の番号
        new int[] { 5   ,  5    ,  5    ,  5    ,  5    ,  5}, // [4] 質問5の選択肢毎の次の質問の番号
        new int[] { 6   ,  6    ,  6    ,  6    ,  6    ,  6}, // [5] 質問6の選択肢毎の次の質問の番号
        new int[] { 7   ,  7    ,  7    ,  7    ,  7    ,  7}, // [6] 質問7の選択肢毎の次の質問の番号
        new int[] { 8   ,  8    ,  8    ,  8    ,  8    ,  8}, // [7] 質問8の選択肢毎の次の質問の番号
    };
    private GameObject[] _answerButton = new GameObject[ANSWER_NUM];
    private int[] _answer; // [0]にPlayerId, 以降は回答番号。skipの場合は-1

    //================================
    //  UIボタン押下処理
    //================================
    public void Answer1()
    {
        AnswerQuestion(1);
    }
    public void Answer2()
    {
        AnswerQuestion(2);
    }
    public void Answer3()
    {
        AnswerQuestion(3);
    }
    public void Answer4()
    {
        AnswerQuestion(4);
    }
    public void Answer5()
    {
        AnswerQuestion(5);
    }
    public void Answer6()
    {
        AnswerQuestion(6);
    }
    public void AnswerOK()
    {
        if (_status == 0) {
            _status = 1;
            ClearAnswer();
            UpdatePanel();
        } else if (_status == _questionNum + 1) {
            _status = _questionNum + 2;
            UpdatePanel();
        }
    }
    public void AnswerNG()
    {
        if (_status == _questionNum + 1) {
            _status = 1;
            ClearAnswer();
            UpdatePanel();
        }
    }
    private void ClearAnswer()
    {
        // -1で埋める
        for (int i=0; i<_answer.Length; i++) {
            _answer[i] = -1;
        }
        // ハイフンで埋める＋余計な分は非表示
        for (int i=0; i<_confirmText.Length; i++) {
            if (_questionNum >= i+1) {
                _confirmText[i].transform.Find("Text_Q").GetComponent<UnityEngine.UI.Text>().text = _questions[i][0];
                _confirmText[i].transform.Find("Text_A").GetComponent<UnityEngine.UI.Text>().text = "―";
                _confirmText[i].transform.Find("Text_Q").GetComponent<UnityEngine.UI.Text>().text = "―";
            } else {
                _confirmText[i].SetActive(false);
            }
        }
    }

    //================================
    //  外部関数
    //================================
    public int GetAnswerLen()
    {
        // 質問数 + 1(PlayerId分)を返す
        return _questions.Length + 1;
    }

    //================================
    //  内部処理
    //================================
    void Start()
    {
        _status = 0;
        _answerButton[0] = _panelQ.transform.Find("Button_AA").gameObject;
        _answerButton[1] = _panelQ.transform.Find("Button_AB").gameObject;
        _answerButton[2] = _panelQ.transform.Find("Button_AC").gameObject;
        _answerButton[3] = _panelQ.transform.Find("Button_AD").gameObject;
        _answerButton[4] = _panelQ.transform.Find("Button_AE").gameObject;
        _answerButton[5] = _panelQ.transform.Find("Button_AF").gameObject;
        _questionNum = _questions.Length;
        _answer = new int[_questionNum + 1]; // [0]にPlayerId, 以降は回答番号。skipの場合は-1

        UpdatePanel();
    }
    void Update()
    {
        if (_questionnaireSendGimmick.GetRequestSend()) {
            // 送信リクエスト中
            _elaspedTime += Time.deltaTime;
            if (_elaspedTime > 1.0) {
                if (_questionnaireSendGimmick.GetSendResult()) {
                    // 送信完了
                } else {
                    // 送信未完了
                    if (Networking.LocalPlayer.IsOwner(_questionnaireSendGimmick.gameObject)) {
                        // _resultSendGimmickの同期変数を更新
                        _answer[0] = Networking.LocalPlayer.playerId;
                        _questionnaireSendGimmick.SetAwnser(_answer);
                    } else {
                        // アンケート結果送信ギミックの権限取得
                        Networking.SetOwner(Networking.LocalPlayer, _questionnaireSendGimmick.gameObject);
                    }
                }
                _elaspedTime = 0.0f;
            }
        }
    }
    private void UpdatePanel()
    {
        HidePanel();
        if (_status == 0) {
            _panelInit.SetActive(true);
        } else if (1 <= _status && _status <= _questionNum) {
            SetQuestionPanel();
        } else if (_status == _questionNum + 1) {
            _panelConfirm.SetActive(true);
        } else if (_status == _questionNum + 2) {
            _panelEnd.SetActive(true);
            _questionnaireSendGimmick.RequestSend();
        }
    }
    private void HidePanel()
    {
        _panelInit.SetActive(false);
        _panelQ.SetActive(false);
        _panelConfirm.SetActive(false);
        _panelEnd.SetActive(false);
    }
    private void SetQuestionPanel()
    {
        _panelQ.SetActive(true);

        // Question No
        _panelQ.transform.Find("Text_Num").GetComponent<UnityEngine.UI.Text>().text = _status + "/" + _questionNum;

        // Question String
        _panelQ.transform.Find("Text_Desc").GetComponent<UnityEngine.UI.Text>().text = _questions[_status - 1][0];
        _panelQ.transform.Find("Text_Desc").GetComponent<UnityEngine.UI.Text>().fontSize = _questionsFontsize[_status - 1][0];

        // Question Awnser
        for (int i=0; i<ANSWER_NUM; i++) {
            if (i < _questions[_status - 1].Length - 1) {
                _answerButton[i].SetActive(true);
                _answerButton[i].transform.Find("Text").GetComponent<UnityEngine.UI.Text>().text = _questions[_status - 1][i + 1];
                _answerButton[i].transform.Find("Text").GetComponent<UnityEngine.UI.Text>().fontSize = _questionsFontsize[_status - 1][i + 1];
            } else {
                _answerButton[i].SetActive(false);
            }
        }
    }
    private void AnswerQuestion(int answer)
    {
        _answer[_status] = answer;
        _confirmText[_status - 1].transform.Find("Text_A").GetComponent<UnityEngine.UI.Text>().text = _questions[_status - 1][_answer[_status]];
        _confirmText[_status - 1].transform.Find("Text_Q").GetComponent<UnityEngine.UI.Text>().text = _questions[_status - 1][0];
        _status = _nextQuestion[_status - 1][answer - 1] + 1;
        UpdatePanel();
    }

    //================================
    //  アンケート結果処理
    //================================
    public void OutputAnswerLog(int[] answer)
    {
        string log = "";
        log += "[Answer]";
        for (int i=0; i<answer.Length - 1; i++) {
            if (answer[i + 1] == -1) {
                log += "\"" + _questions[i][0] + "\",\"skip\"";
            } else {
                log += "\"" + _questions[i][0] + "\",\"" + _questions[i][answer[i + 1]] + "\"";
            }
            if (i != answer.Length - 2) {
                log += ",";
            }
        }
        Debug.Log(log);
    }
}
