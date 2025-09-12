using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ChatComposer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TMP_InputField inputField;
    [SerializeField] Button sendButton;
    [SerializeField] ScrollRect scrollRect;          // 指向这个 Scroll View
    [SerializeField] RectTransform content;          // 指向 Scroll View/Viewport/Content
    [SerializeField] GameObject userBoxPrefab;       // 你的 userBox 预制体（里含 TMP_Text）

    [Header("Hooks (预留给 LLM)")]
    public UnityEvent<string> onUserMessage;         // 发送后把原文抛出去；Inspector 可绑调用

    void Awake()
    {
        if (sendButton) sendButton.onClick.AddListener(Send);
    }

    void OnDestroy()
    {
        if (sendButton) sendButton.onClick.RemoveListener(Send);
    }

    public void Send()
    {
        if (!inputField) return;
        var text = inputField.text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // 1) 生成一条用户气泡
        CreateUserBubble(text);

        // 2) 清空输入框 & 失焦（可选）
        inputField.text = string.Empty;
        inputField.DeactivateInputField();

        // 3) 抛给外部（LLM接口可在 Inspector 绑定到此事件）
        onUserMessage?.Invoke(text);
    }

    public void SendTest(string content)
    {
        // 1) 生成一条用户气泡
        CreateUserBubble(content);
    }

    void CreateUserBubble(string text)
    {
        if (!userBoxPrefab || !content) return;

        var go = Instantiate(userBoxPrefab, content);
        // 找到这条消息里的 TMP_Text（你也可以给 userBox 写个脚本来 SetText）
        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp) tmp.text = text;

        // 若 userBox 上有你之前的 ChatBubbleFitter，会自动自适应尺寸

        // 强制刷新一次布局再滚到底
        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        if (!scrollRect) return;
        // 先刷新，再把滚动条拉到底
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;  // 0=底部, 1=顶部
        Canvas.ForceUpdateCanvases();
    }

    // 可选：支持按下 Enter 发送（移动端也适用外接键盘）
    void Update()
    {
        if (inputField && inputField.isFocused && Input.GetKeyDown(KeyCode.Return))
        {
            Send();
        }
    }
}
