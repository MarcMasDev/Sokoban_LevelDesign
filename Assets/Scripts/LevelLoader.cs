using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    [Header("Configuración de Progresión")]
    public List<LevelData> levels = new List<LevelData>();
    [SerializeField] private GameObject winScreen;

    [Header("Cámara")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraMargin = 1.1f;

    [Header("Bases de Datos")]
    [SerializeField] private CellDatabase cellDatabase;
    [SerializeField] private FurnitureDatabase furnitureDatabase;

    [Header("Prefabs Genéricos")]
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject goalPrefab;
    [SerializeField] private GameObject furniturePrefab;

    [Header("Ajustes de Escena")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private float cellSize = 1f;

    //POOLING: evita sobrecarga al instanciar y destruir objetos
    private Dictionary<GameObject, Stack<GameObject>> poolDictionary = new Dictionary<GameObject, Stack<GameObject>>();
    private List<ActiveObjectNode> activeObjects = new List<ActiveObjectNode>();
    private bool loadingNextLevel = false;

    [Header("Input R (Undo / Restart)")]
    [SerializeField] private float requiredHoldTime = 1f;
    private float currentHoldTime = 0f;
    [SerializeField] private Slider restartSlider;

    private struct ActiveObjectNode
    {
        public GameObject instance;
        public GameObject sourcePrefab;
    }
    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        RestartGame();
    }

    private void Update()
    {
        HandleR();

        //Si ha ganado...
        if (GoalsCompleted() && !loadingNextLevel)
        {
            loadingNextLevel = true;
            LoadNextLevel();
        }
        //Si esta esperando para cargar el siguiente nivel
        else if (!Stats.isLoading && loadingNextLevel && !winScreen.activeSelf) LoadLevel(Stats.levelIndex);
    }
    private void HandleR()
    {
        if (!Stats.isLoading)
        {
            //1. Al presionar por primera vez
            if (Input.GetKeyDown(KeyCode.R))
            {
                currentHoldTime = 0f;

                if (winScreen != null && winScreen.activeSelf) RestartGame();
            }

            //2. Mientras se mantenga presionado
            if (Input.GetKey(KeyCode.R))
            {
                currentHoldTime += Time.deltaTime;

                //Rellenar el Slider visualmente
                restartSlider.value = currentHoldTime / requiredHoldTime;

                //Si llegamos al tiempo requerido -> RESTART
                if (currentHoldTime >= requiredHoldTime)
                {
                    if (winScreen == null || !winScreen.activeSelf) LoadLevel(Stats.levelIndex);
                }
            }

            //3. Al soltar la tecla
            if (Input.GetKeyUp(KeyCode.R))
            {
                //Si lo soltó antes del tiempo límite, significa que fue un toque corto -> UNDO
                if (currentHoldTime > 0 && currentHoldTime < requiredHoldTime)
                {
                    Player activePlayer = FindFirstObjectByType<Player>();
                    UndoManager.UndoMove(activePlayer);
                }

                //Reseteamos valores
                currentHoldTime = 0f;
                if (restartSlider != null) restartSlider.value = 0f;
            }
        }
    }
    private void RestartGame()
    {
        loadingNextLevel = false;
        Stats.levelIndex = 0;
        ClearLevel();
        LoadLevel(Stats.levelIndex);
    }
    private bool GoalsCompleted()
    {
        return Stats.goalsCompleted >= Stats.goalsToComplete;
    }

    private void LoadNextLevel()
    {
        if (Stats.levelIndex < levels.Count-1)
        {
            Stats.levelIndex++;
            Stats.isLoading = true;
        }
        else ShowWinScreen();
    }

    private void LoadLevel(int index)
    {
        if (index < 0 || index >= levels.Count) return;

        ClearLevel();

        LevelData data = levels[index];
        if (winScreen != null) winScreen.SetActive(false);

        for (int y = 0; y < data.height; y++)
        {
            for (int x = 0; x < data.width; x++)
            {
                int arrayIndex = x + y * data.width;
                CellType type = data.cells[arrayIndex];
                Vector3 pos = new Vector3(x * cellSize, -y * cellSize, 0);

                if (type != CellType.Empty)
                {
                    if (floorPrefab != null) SpawnFromPool(floorPrefab, pos);
                    SpawnElement(type, arrayIndex, pos, data);
                }
            }
        }

        AdjustCamera(data);
        loadingNextLevel = false;
    }
    private void AdjustCamera(LevelData data)
    {
        if (mainCamera == null) return;

        //Calcula el centre
        float centerX = (data.width - 1) * cellSize / 2f;
        float centerY = (data.height - 1) * cellSize / -2f;
        mainCamera.transform.position = new Vector3(centerX, centerY, -10f);

        //Calcula i aplica els "bounds" (limits)
        float verticalSize = (data.height * cellSize) / 2f;
        float horizontalSize = ((data.width * cellSize) / 2f) / mainCamera.aspect;
        mainCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize) + cameraMargin;
    }

    private void SpawnElement(CellType type, int arrayIndex, Vector3 pos, LevelData data)
    {
        GameObject prefabToSpawn = null;
        Sprite spriteToApply = null;

        switch (type)
        {
            case CellType.Wall: 
                prefabToSpawn = wallPrefab; 
                spriteToApply = GetCellSprite(CellType.Wall); 
                break;
            case CellType.Player: 
                prefabToSpawn = playerPrefab; 
                spriteToApply = GetCellSprite(CellType.Player); 
                break;
            case CellType.Goal: 
                prefabToSpawn = goalPrefab;
                spriteToApply = GetCellSprite(CellType.Goal);
                Stats.goalsToComplete++;
                break;
            case CellType.Furniture:
                prefabToSpawn = furniturePrefab;
                string id = data.GetFurnitureID(arrayIndex);
                FurnitureEntry entry = furnitureDatabase.entries.Find(e => e.id == id);
                spriteToApply = entry.icon;
                break;
        }

        if (prefabToSpawn != null)
        {
            GameObject obj = SpawnFromPool(prefabToSpawn, pos);
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

            if (sr != null && spriteToApply != null) sr.sprite = spriteToApply;
        }
    }

    private GameObject SpawnFromPool(GameObject prefab, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(prefab)) poolDictionary[prefab] = new Stack<GameObject>();

        GameObject obj;
        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Pop();
            obj.transform.position = position;
            obj.SetActive(true);
        }

        else obj = Instantiate(prefab, position, Quaternion.identity, gridParent);

        activeObjects.Add(new ActiveObjectNode{instance = obj, sourcePrefab = prefab});
        return obj;
    }

    private void ClearLevel()
    {
        foreach (var node in activeObjects)
        {
            node.instance.transform.position = new Vector2(9999, 9999); //Enviales al cementario, no queremos que se queden en el sitio
            node.instance.SetActive(false);
            poolDictionary[node.sourcePrefab].Push(node.instance);
        }

        activeObjects.Clear();
        UndoManager.ClearHistory();
        Stats.ResetStats();
    }

    private Sprite GetCellSprite(CellType type)
    {
        var cell = cellDatabase.cells.FirstOrDefault(c => c.cellType == type);
        return cell.icon;
    }

    private void ShowWinScreen()
    {
        ClearLevel();
        if (winScreen != null) winScreen.SetActive(true);
    }

    private void RemoveHistory()
    {
        
    }
}