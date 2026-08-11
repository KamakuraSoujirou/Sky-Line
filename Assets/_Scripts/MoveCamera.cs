using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    [SerializeField]private Transform _cameraPosition; // カメラの位置を指定するTransform

    void Start()
    {
        if (_cameraPosition == null)
        {
            Debug.LogError("Camera position Transform is not assigned.");
        }
    }
    void LateUpdate()
    {
        //カメラをRigidBodyの子オブジェクトにすると、不安定になる可能性があるため、カメラの位置を直接更新する
        transform.position = _cameraPosition.position;
    }
}
