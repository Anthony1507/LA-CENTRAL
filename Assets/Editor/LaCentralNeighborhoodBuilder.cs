
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LaCentralNeighborhoodBuilder
{
    private const float MapWidth = 60f;
    private const float MapHeight = 36f;

    private sealed class DeviceInfo
    {
        public readonly string objectName;
        public readonly string id;
        public readonly string displayName;
        public readonly DeviceKind kind;
        public readonly int volume;
        public readonly int channel;

        public DeviceInfo(
            string objectName,
            string id,
            string displayName,
            DeviceKind kind,
            int volume,
            int channel = 0)
        {
            this.objectName = objectName;
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.volume = volume;
            this.channel = channel;
        }
    }

    private sealed class Site
    {
        public readonly string id;
        public readonly string objectName;
        public readonly string displayName;
        public readonly Vector2 position;
        public readonly DeviceInfo[] devices;

        public Site(
            string id,
            string objectName,
            string displayName,
            float x,
            float y,
            params DeviceInfo[] devices)
        {
            this.id = id;
            this.objectName = objectName;
            this.displayName = displayName;
            position = new Vector2(x, y);
            this.devices = devices;
        }
    }

    private static readonly Site[] Sites =
    {
        new Site("B01", "B01_CasaAlmendro", "Casa Almendro",
            -24, 12,
            new DeviceInfo("B01_TV", "TV_MAIN",
                "Televisor principal", DeviceKind.Television, 90, 2)),

        new Site("B02", "B02_CasaFarol", "Casa del Farol",
            -8, 12,
            new DeviceInfo("B02_Radio", "RADIO_MAIN",
                "Radio principal", DeviceKind.Radio, 85, 1)),

        new Site("B03", "B03_Panaderia", "Panaderia Norte",
            8, 12,
            new DeviceInfo("B03_Radio", "RADIO_MAIN",
                "Radio de la panaderia", DeviceKind.Radio, 40)),

        new Site("B06", "B06_TiendaSur", "Tienda Sur",
            24, 12,
            new DeviceInfo("B06_TV", "TV_MAIN",
                "Televisor de la tienda", DeviceKind.Television, 30)),

        new Site("B04", "B04_CafeEsquina", "Cafe Esquina",
            -24, 0,
            new DeviceInfo("B04_Radio", "RADIO_MAIN",
                "Radio del cafe", DeviceKind.Radio, 0)),

        new Site("B05", "B05_Floristeria", "Floristeria",
            -8, 0),

        new Site("B09", "B09_CasaAzul", "Casa Azul",
            8, 0,
            new DeviceInfo("B09_TV", "TV_MAIN",
                "Televisor del salon", DeviceKind.Television, 75, 2)),

        new Site("B07", "B07_Biblioteca", "Biblioteca",
            24, 0,
            new DeviceInfo("B07_Speaker", "SPEAKER_MAIN",
                "Altavoz de la biblioteca", DeviceKind.Speaker, 70)),

        new Site("B08", "B08_Estudio", "Estudio del Barrio",
            -24, -12,
            new DeviceInfo("B08_Speaker", "SPEAKER_MAIN",
                "Altavoz del estudio", DeviceKind.Speaker, 95)),

        new Site("B11", "B11_SalaEnsayo", "Sala de Ensayo",
            -8, -12,
            new DeviceInfo("B11_Speaker", "SPEAKER_MAIN",
                "Altavoz de ensayo", DeviceKind.Speaker, 90)),

        new Site("B10", "B10_CentroCivico", "Centro Civico",
            8, -12,
            new DeviceInfo("B10_Speaker", "SPEAKER_MAIN",
                "Megafonia principal", DeviceKind.Speaker, 80),
            new DeviceInfo("B10_Radio", "RADIO_MAIN",
                "Radio de retransmision", DeviceKind.Radio, 30)),

        new Site("B12", "B12_PlazaMayor", "Plaza Mayor",
            24, -12,
            new DeviceInfo("B12_Speaker", "SPEAKER_MAIN",
                "Altavoz de la plaza", DeviceKind.Speaker, 10))
    };

    [MenuItem("LA CENTRAL/Graybox/Construir barrio completo")]
    public static void BuildNeighborhood()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "LA CENTRAL",
                "Sal del modo Play antes de construir el barrio.",
                "Aceptar");
            return;
        }

        GameObject ground = GameObject.Find("Ground");

        if (ground == null ||
            !ground.TryGetComponent(out SpriteRenderer groundSprite) ||
            groundSprite.sprite == null)
        {
            EditorUtility.DisplayDialog(
                "LA CENTRAL",
                "Necesitas un objeto Ground con SpriteRenderer y un sprite Square.",
                "Aceptar");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Construir barrio LA CENTRAL");

        Sprite square = groundSprite.sprite;

        Undo.RecordObject(ground.transform, "Ajustar terreno");
        ground.transform.position = Vector3.zero;
        ground.transform.localScale =
            new Vector3(MapWidth, MapHeight, 1f);

        Undo.RecordObject(groundSprite, "Ajustar orden de suelo");
        groundSprite.sortingOrder = -50;

        Transform roads = GetOrCreateRoot("_LC_Roads");
        Transform buildings = GetOrCreateRoot("_LC_Buildings");
        Transform entrances = GetOrCreateRoot("_LC_Entrances");
        Transform labels = GetOrCreateRoot("_LC_Labels");

        ClearChildren(roads);

        BuildRoads(roads, square);

        foreach (Site site in Sites)
        {
            BuildSite(site, buildings, entrances, labels, square);
        }

        GameObject player = GameObject.Find("Player");

        if (player != null)
        {
            Undo.RecordObject(player.transform, "Punto de inicio");
            player.transform.position = new Vector3(-24, 7.2f, 0);
        }

        EditorSceneManager.MarkSceneDirty(
            SceneManager.GetActiveScene());

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "LA CENTRAL",
            "Barrio construido.\n\n" +
            "12 ubicaciones, calles, entradas y dispositivos.\n" +
            "Se conservaron los componentes existentes de B01 y B04.\n\n" +
            "Guarda la escena con Ctrl+S.",
            "Aceptar");
    }

    private static Transform GetOrCreateRoot(string name)
    {
        GameObject root = GameObject.Find(name);

        if (root == null)
        {
            root = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(root, "Crear grupo");
        }

        return root.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        while (parent.childCount > 0)
        {
            Undo.DestroyObjectImmediate(
                parent.GetChild(0).gameObject);
        }
    }

    private static GameObject CreateRectangle(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        Color color,
        int sortingOrder)
    {
        GameObject obj = new GameObject(name);

        Undo.RegisterCreatedObjectUndo(obj, "Crear graybox");

        obj.transform.SetParent(parent, false);
        obj.transform.position =
            new Vector3(position.x, position.y, 0);

        obj.transform.localScale =
            new Vector3(size.x, size.y, 1);

        SpriteRenderer renderer =
            Undo.AddComponent<SpriteRenderer>(obj);

        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        return obj;
    }

    private static void BuildRoads(
        Transform root,
        Sprite square)
    {
        Color roadColor =
            new Color(0.19f, 0.21f, 0.25f);

        Color sidewalkColor =
            new Color(0.49f, 0.50f, 0.53f);

        float[] vertical = { -16f, 0f, 16f };

        foreach (float x in vertical)
        {
            CreateRectangle(
                "Sidewalk_V_" + x,
                root, square,
                new Vector2(x, 0),
                new Vector2(6f, MapHeight),
                sidewalkColor, -45);

            CreateRectangle(
                "Road_V_" + x,
                root, square,
                new Vector2(x, 0),
                new Vector2(4f, MapHeight),
                roadColor, -40);
        }

        float[] horizontal = { -6f, 6f };

        foreach (float y in horizontal)
        {
            CreateRectangle(
                "Sidewalk_H_" + y,
                root, square,
                new Vector2(0, y),
                new Vector2(MapWidth, 6f),
                sidewalkColor, -45);

            CreateRectangle(
                "Road_H_" + y,
                root, square,
                new Vector2(0, y),
                new Vector2(MapWidth, 4f),
                roadColor, -40);
        }

        CreateRectangle(
            "PlazaMayor_Floor",
            root, square,
            new Vector2(24, -12),
            new Vector2(11, 9),
            new Color(0.63f, 0.63f, 0.63f),
            -46);
    }

    private static void BuildSite(
        Site site,
        Transform buildingsRoot,
        Transform entrancesRoot,
        Transform labelsRoot,
        Sprite square)
    {
        GameObject building = GameObject.Find(site.objectName);

        if (building == null)
        {
            building = new GameObject(site.objectName);
            Undo.RegisterCreatedObjectUndo(
                building, "Crear edificio");

            building.transform.SetParent(
                buildingsRoot, false);
        }

        Undo.RecordObject(
            building.transform, "Colocar edificio");

        building.transform.position =
            new Vector3(site.position.x, site.position.y, 0);

        building.transform.localScale =
            new Vector3(6f, 4f, 1f);

        SpriteRenderer renderer =
            building.GetComponent<SpriteRenderer>();

        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(building);

        Undo.RecordObject(renderer, "Apariencia graybox");
        renderer.sprite = square;
        renderer.color = site.id == "B12"
            ? new Color(0.36f, 0.43f, 0.47f)
            : new Color(0.38f, 0.39f, 0.43f);

        renderer.sortingOrder = 1;

        BoxCollider2D buildingCollider =
            building.GetComponent<BoxCollider2D>();

        if (buildingCollider == null)
            buildingCollider =
                Undo.AddComponent<BoxCollider2D>(building);

        Undo.RecordObject(buildingCollider, "Colision edificio");
        buildingCollider.isTrigger = false;
        buildingCollider.size = Vector2.one;

        BuildLabel(site, labelsRoot);

        if (site.devices.Length == 0)
            return;

        string entranceName = site.id + "_Entrance";

        GameObject entrance = GameObject.Find(entranceName);

        if (entrance == null)
        {
            entrance = new GameObject(entranceName);
            Undo.RegisterCreatedObjectUndo(
                entrance, "Crear entrada");

            entrance.transform.SetParent(
                entrancesRoot, false);
        }

        float direction = site.position.y < -6f ? 1f : -1f;

        Undo.RecordObject(
            entrance.transform, "Colocar entrada");

        entrance.transform.position = new Vector3(
            site.position.x,
            site.position.y + direction * 3.05f,
            0);

        entrance.transform.localScale = Vector3.one;

        BoxCollider2D trigger =
            entrance.GetComponent<BoxCollider2D>();

        if (trigger == null)
            trigger = Undo.AddComponent<BoxCollider2D>(entrance);

        Undo.RecordObject(trigger, "Configurar entrada");
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 1.8f);

        BuildingEntrance entranceScript =
            entrance.GetComponent<BuildingEntrance>();

        if (entranceScript == null)
        {
            entranceScript =
                Undo.AddComponent<BuildingEntrance>(entrance);
        }

        List<DeviceController> createdDevices =
            new List<DeviceController>();

        foreach (DeviceInfo info in site.devices)
        {
            GameObject device = GameObject.Find(info.objectName);

            if (device == null)
            {
                device = new GameObject(info.objectName);

                Undo.RegisterCreatedObjectUndo(
                    device, "Crear dispositivo");

                device.transform.SetParent(
                    entrance.transform, false);

                device.transform.localPosition = Vector3.zero;
            }

            DeviceController controller =
                device.GetComponent<DeviceController>();

            if (controller == null)
                controller =
                    Undo.AddComponent<DeviceController>(device);

            SerializedObject serialized =
                new SerializedObject(controller);

            SetString(serialized, "buildingId", site.id);
            SetString(serialized, "deviceId", info.id);
            SetString(serialized, "deviceName", info.displayName);
            SetInt(serialized, "kind", (int)info.kind);
            SetInt(serialized, "volume", info.volume);
            SetInt(serialized, "channelIndex", info.channel);
            SetBool(serialized, "poweredOn", true);

            serialized.ApplyModifiedProperties();

            createdDevices.Add(controller);
        }

        SerializedObject entranceData =
            new SerializedObject(entranceScript);

        SetString(entranceData, "buildingId", site.id);
        SetString(entranceData, "displayName", site.displayName);

        SerializedProperty deviceArray =
            entranceData.FindProperty("devices");

        if (deviceArray != null)
        {
            deviceArray.arraySize = createdDevices.Count;

            for (int i = 0; i < createdDevices.Count; i++)
            {
                deviceArray
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue = createdDevices[i];
            }
        }

        entranceData.ApplyModifiedProperties();
    }

    private static void BuildLabel(
        Site site,
        Transform labelsRoot)
    {
        string name = "LC_Label_" + site.id;

        GameObject label = GameObject.Find(name);

        if (label == null)
        {
            label = new GameObject(
                name,
                typeof(RectTransform));

            Undo.RegisterCreatedObjectUndo(
                label, "Crear etiqueta");

            label.transform.SetParent(labelsRoot, false);
        }

        TextMeshPro text = label.GetComponent<TextMeshPro>();

        if (text == null)
            text = Undo.AddComponent<TextMeshPro>(label);

        Undo.RecordObject(text, "Texto de edificio");
        Undo.RecordObject(label.transform, "Posicion etiqueta");

        label.transform.position = new Vector3(
            site.position.x,
            site.position.y,
            -0.1f);

        label.transform.localScale = Vector3.one;

        text.text = site.id + "\n" + site.displayName;
        text.fontSize = 2.5f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = 30;

        text.rectTransform.sizeDelta = new Vector2(7f, 2.5f);
    }

    private static void SetString(
        SerializedObject obj,
        string name,
        string value)
    {
        SerializedProperty prop = obj.FindProperty(name);

        if (prop != null)
            prop.stringValue = value;
    }

    private static void SetInt(
        SerializedObject obj,
        string name,
        int value)
    {
        SerializedProperty prop = obj.FindProperty(name);

        if (prop != null)
            prop.intValue = value;
    }

    private static void SetBool(
        SerializedObject obj,
        string name,
        bool value)
    {
        SerializedProperty prop = obj.FindProperty(name);

        if (prop != null)
            prop.boolValue = value;
    }
}

#endif
