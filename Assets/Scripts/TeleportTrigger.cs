using UnityEngine;
using System.Collections;

public class TeleportTrigger : MonoBehaviour
{
    private TeleporterManager teleporterManager;
    private Vector2Int gridPosition;

    public void Initialize(TeleporterManager manager, Vector2Int gridPos)
    {
        teleporterManager = manager;
        gridPosition = gridPos;
        Debug.Log($"Teleporter trigger initialized at grid position: {gridPos}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PacStudentController pacStudent = other.GetComponent<PacStudentController>();
            if (pacStudent != null && teleporterManager != null)
            {
                Debug.Log($"Player entered teleporter at grid position: {gridPosition}");
                teleporterManager.HandleTeleportation(gridPosition, pacStudent);
            }
        }
    }
}