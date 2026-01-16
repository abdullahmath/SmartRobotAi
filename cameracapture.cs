using UnityEngine;
using System.Collections;
using System.IO;

public class RobotCameraCapture : MonoBehaviour
{
    public Camera robotCamera;
    public float captureInterval = 0.4f;
    public int imageWidth = 500;
    public int imageHeight = 500;
    public int jpegQuality = 70;
    public float velocityThreshold = 0.1f;
    public bool includeMetadata = true;

    private Rigidbody rb;
    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private string datasetPath;

    IEnumerator Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        robotCamera = robotCamera ? robotCamera : GetComponent<Camera>();

        renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
        robotCamera.targetTexture = renderTexture;
        texture2D = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
        datasetPath = Path.Combine(Application.persistentDataPath, "RobotDataset");
        Directory.CreateDirectory(datasetPath);

        while (true)
        {
            yield return new WaitForSeconds(captureInterval);
            if (rb.velocity.magnitude > velocityThreshold) CaptureImage();
        }
    }

    void CaptureImage()
    {
        robotCamera.Render();
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        texture2D.Apply();

        string fileName = $"capture_{System.DateTime.Now:yyyyMMdd_HHmmssfff}.jpg";
        File.WriteAllBytes(Path.Combine(datasetPath, fileName), texture2D.EncodeToJPG(jpegQuality));

        if (includeMetadata)
        {
            File.WriteAllText(
                Path.ChangeExtension(fileName, ".json"),
                JsonUtility.ToJson(new Metadata
                {
                    timestamp = System.DateTime.Now.ToString("o"),
                    position = transform.position,
                    rotation = transform.eulerAngles,
                    velocity = rb.velocity,
                    angularVelocity = rb.angularVelocity
                }, true)
            );
        }
    }

    [System.Serializable]
    private class Metadata
    {
        public string timestamp;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
    }
}