using UnityEngine;
using TMPro; // 如果使用普通 Text，请将其改为 using UnityEngine.UI;

public class InputHintSwitcher : MonoBehaviour
{
    public TextMeshProUGUI hintText; // 拖入文字组件

    [Header("Messages")]
    public string keyboardMessage = "按下 [F] 切换回忆与现在";
    public string gamepadMessage = "按下 [LB] 切换回忆与现在";

    [Header("Breathe Effect")]
    public float breatheSpeed = 2f;
    public float minAlpha = 0f;
    public float maxAlpha = 1f;

    [Header("Movement Detection")]
    public float idleShowDelay = 3f;   // 站立不动多久后显示
    private float idleTimer = 0f;
    private float currentAlpha = 0f;

    void Start()
    {
        if (hintText != null) SetTextAlpha(0);
    }

    void Update()
    {
        // 1. 检测玩家移动输入 (支持 WASD、箭头和手柄左摇杆)
        bool isMoving = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
                        Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        if (isMoving)
        {
            idleTimer = 0f; // 只要在动，计时重置
            currentAlpha = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime; // 停下来，开始计时
        }

        // 2. 逻辑显示判断
        if (idleTimer >= idleShowDelay)
        {
            // 切换对应设备的文案
            hintText.text = IsGamepadConnected() ? gamepadMessage : keyboardMessage;

            // 呼吸灯效果：透明度在 0.2 到 1 之间往复
            float breatheLerp = Mathf.PingPong(Time.time * breatheSpeed, 1.0f);
            currentAlpha = Mathf.Lerp(0.2f, maxAlpha, breatheLerp);
        }
        else
        {
            currentAlpha = 0f;
        }

        SetTextAlpha(currentAlpha);
    }

    void SetTextAlpha(float alpha)
    {
        if (hintText != null)
        {
            Color c = hintText.color;
            c.a = alpha;
            hintText.color = c;
        }
    }

    bool IsGamepadConnected()
    {
        string[] names = Input.GetJoystickNames();
        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(name)) return true;
        }
        return false;
    }
}