using UnityEngine;

public class LookWithMouse : MonoBehaviour
{
    public float mouseSensitivity = 100f;

    public Transform playerBody;

    private float m_XRotation;

    private float mouseX;
    private float mouseY;

    // Start is called before the first frame update
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    private void Update()
    {
        mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        m_XRotation -= mouseY;
        m_XRotation = Mathf.Clamp(m_XRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(m_XRotation, 0f, 0f);

        playerBody.Rotate(Vector3.up * mouseX);
    }
}