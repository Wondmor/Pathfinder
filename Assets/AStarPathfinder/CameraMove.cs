using UnityEngine;

public class CameraMove : MonoBehaviour
{
    // Reference to the TesterManager
    public TesterManager testerManager;

    // Padding around the grid to ensure all nodes are visible
    [Header("Camera Padding")]
    public float padding = 1.0f;

    void Start()
    {
        // Adjust the camera position and size
        AdjustCamera();
    }

    void AdjustCamera()
    {
        // Get the grid dimensions and offsets from the TesterManager
        int mapWidth = testerManager.mapW;
        int mapHeight = testerManager.mapH;
        float offsetX = testerManager.offsetX;
        float offsetY = testerManager.offsetY;
        int startX = testerManager.beginX;
        int startY = testerManager.beginY;

        // Calculate the center of the grid
        float centerX = startX + (mapWidth - 1) * offsetX / 2.0f;
        float centerY = startY + (mapHeight - 1) * offsetY / 2.0f;

        // Calculate the size of the grid
        float totalWidth = mapWidth * offsetX;
        float totalHeight = mapHeight * offsetY;

        // Adjust the orthographic size of the camera
        Camera.main.orthographicSize = Mathf.Max(totalWidth / Camera.main.aspect, totalHeight) / 2.0f + padding;

        // Set the camera position
        Camera.main.transform.position = new Vector3(centerX, centerY, -10); // Z = -10 for 2D perspective
    }
}