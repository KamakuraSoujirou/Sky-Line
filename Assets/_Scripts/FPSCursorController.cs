using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCursorController : MonoBehaviour
{
    private bool isLocked = true;

    void Start()
    {
        SetCursorState(true);
    }

    void Update()
    {
        // ESCキーが押されたら、カーソルのロック状態を反転させる
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            isLocked = !isLocked;
            SetCursorState(isLocked);
        }

        // カーソルが解放されている時に、ゲーム画面を左クリックしたら再ロックする
        if (!isLocked && Pointer.current != null && Pointer.current.press.isPressed)
        {
            // UIなどをクリックしている場合は除外したいなら、ここにEventSystemのチェックを入れる
            isLocked = true;
            SetCursorState(true);
        }
    }

    // カーソルの状態をまとめて切り替えるメソッド
    void SetCursorState(bool locked)
    {
        if (locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
