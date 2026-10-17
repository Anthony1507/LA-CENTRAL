using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class BuildingEntrance : MonoBehaviour
{
    [SerializeField] private string buildingId = "B01";
    [SerializeField] private string displayName = "Casa Almendro";
    [SerializeField] private DeviceController[] devices;

    public string BuildingId => buildingId;
    public string DisplayName => displayName;

    public DeviceController[] Devices
    {
        get
        {
            if (devices == null || devices.Length == 0)
                devices = GetComponentsInChildren<DeviceController>(true);
            return devices;
        }
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }
}
