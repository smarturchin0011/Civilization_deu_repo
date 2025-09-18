using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[System.Serializable]
public class YuanqiMessage
{
    public string role;
    public List<MessageContent> content = new List<MessageContent>();
}

[System.Serializable]
public class MessageContent
{
    public string type;
    public string text;
    public FileUrlContent file_url;
}

[System.Serializable]
public class FileUrlContent
{
    public string type;
    public string url;
}

[System.Serializable]
public class APIRequest
{
    public string assistant_id;
    public string user_id;
    public bool stream = false;
    public List<YuanqiMessage> messages = new List<YuanqiMessage>();
}

/// <summary>
/// 调用腾讯元器API的主要代码，支持对话历史和记忆功能
/// </summary>
public class TencentApiManager : MonoBehaviour
{
    // API配置参数
    private string apiUrl = "https://open.hunyuan.tencent.com/openapi/v1/agent/chat/completions";
    [SerializeField] public string assistantId;
    [SerializeField] public string apiToken;
    [SerializeField] public string userId;
    public string result;
    //public Action Act;

    private void Start()
    {
        
    }

    // 对话历史
    private static List<YuanqiMessage> conversationHistory = new List<YuanqiMessage>();
    [Tooltip("Maximum number of messages (user+assistant) to keep in history")]    
    private int historyLimit;

    /// <summary>
    /// 设置对话历史的最大长度
    /// </summary>
    public void SetHistoryLimit(int limit)
    {
        historyLimit = Mathf.Max(4, limit);
        print("已设置最大历史");
        print("当前最大历史为："+historyLimit);
        TrimHistory();
        
    }

    /// <summary>
    /// 清空对话历史
    /// </summary>
    public void ClearHistory()
    {
        conversationHistory.Clear();
    }

    // 发送文本消息
    public void SendTextMessage(string userMessage, Action<string> callback, Action<string> errorCallback)
    {
        // 将用户消息加入历史
        AddMessageToHistory("user", userMessage, null);

        // 创建包含历史的请求
        var requestData = CreateRequestFromHistory();
        StartCoroutine(SendRequestCoroutine(requestData, responseText =>
        {
            // 将助手回复加入历史
            AddMessageToHistory("assistant", responseText, null);
            callback?.Invoke(responseText);
        }, errorCallback));
        //Act?.Invoke();
    }
    
    /// <summary>
    /// 发送图文消息
    /// </summary>
    /// <param name="imageUrl">图片Url地址</param>
    /// <param name="userMessage">用户文本内容</param>
    /// <param name="callback">反馈委托函数</param>
    /// <param name="errorCallback">报错委托函数</param>
    public void SendImageTextMessage(string imageUrl, string userMessage, Action<string> callback, Action<string> errorCallback)
    {

        // 构造当前图文消息，不包含历史
        var compositeMessage = new List<MessageContent>();

        if (!string.IsNullOrEmpty(userMessage))
        {
            compositeMessage.Add(new MessageContent
            {
                type = "text",
                text = userMessage
            });
        }

        compositeMessage.Add(new MessageContent
        {
            type = "file_url",
            file_url = new FileUrlContent
            {
                type = "image",
                url = imageUrl
            }
        });

        // 构建用户消息（无历史，不影响记录）
        var imageMessage = new YuanqiMessage
        {
            role = "user",
            content = compositeMessage
        };

        // 只包含当前图文内容的请求体
        var requestData = new APIRequest
        {
            assistant_id = assistantId,
            user_id = userId,
            messages = new List<YuanqiMessage> { imageMessage }
        };

        // 发送请求（不更新本地历史）
        StartCoroutine(SendRequestCoroutine(requestData, responseText =>
        {
            // 仅回调返回，不更新历史
            callback?.Invoke(responseText);

            // 更新状态
            //Act?.Invoke();
        }, errorCallback));
    }

    /// <summary>
    /// 将消息加入本地历史，自动裁剪超出长度的部分
    /// </summary>
    private void AddMessageToHistory(string role, string text, FileUrlContent fileUrl)
    {
        var message = new YuanqiMessage { role = role };
        var content = new MessageContent();

        if (!string.IsNullOrEmpty(text))
        {
            content.type = "text";
            content.text = text;
        }
        else if (fileUrl != null)
        {
            content.type = "file_url";
            content.file_url = fileUrl;
        }

        message.content.Add(content);
        conversationHistory.Add(message);
        TrimHistory();
        print("当前List有: "+conversationHistory.Count);
    }

    /// <summary>
    /// 裁剪历史至最大长度
    /// </summary>
    private void TrimHistory()
    {
        int excess = conversationHistory.Count - historyLimit;
        if (excess > 0)
        {
            print("正在清除历史...");
            conversationHistory.RemoveRange(0, excess);
            print("当前历史："+conversationHistory.Count);
        }
    }

    /// <summary>
    /// 基于当前历史创建请求体
    /// </summary>
    private APIRequest CreateRequestFromHistory()
    {
        var request = new APIRequest
        {
            assistant_id = assistantId,
            user_id = userId,
            messages = new List<YuanqiMessage>(conversationHistory),
        };
        return request;
    }

    /// <summary>
    /// 协程发送请求
    /// </summary>
    private IEnumerator SendRequestCoroutine(APIRequest requestData, Action<string> callback, Action<string> errorCallback)
    {
        string jsonData = JsonUtility.ToJson(requestData);
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Source", "openapi");
            request.SetRequestHeader("Authorization", $"Bearer {apiToken}");

            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                errorCallback?.Invoke($"Request failed: {request.error}");
                yield break;
            }

            try
            {
                Debug.Log(request.downloadHandler.text);
                var response = JsonUtility.FromJson<YuanqiResponse>(request.downloadHandler.text);
           
                if (response.choices.Count > 0)
                {
                    var assistantReply = response.choices[0].message.content;
                    result = assistantReply;

                    callback?.Invoke(assistantReply);
                    
                    //调用SentToTTS方法
                    //Act?.Invoke();
                    
                }
            }
            catch (Exception e)
            {
                errorCallback?.Invoke($"Parse error: {e.Message}");
            }
        }
    }
}

// 响应数据结构
[System.Serializable]
public class YuanqiResponse
{
    public string id;
    public string created;
    public List<Choice> choices = new List<Choice>();
    public Usage usage;
}

[System.Serializable]
public class Choice
{
    public int index;
    public string finish_reason;
    public MessageData message;
}

[System.Serializable]
public class MessageData
{
    public string role;
    public string content;
    public List<Step> steps;
}

[System.Serializable]
public class Step
{
    public string role;
    public string content;
}

[System.Serializable]
public class Usage
{
    public int prompt_tokens;
    public int completion_tokens;
    public int total_tokens;
}
