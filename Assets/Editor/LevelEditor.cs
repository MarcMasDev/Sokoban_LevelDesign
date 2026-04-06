using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

///<summary>
///Ventana principal del editor de niveles de Sokoban
///</summary>
public class LevelEditor : EditorWindow
{
    //Variables de referencias a datos
    [SerializeField] private LevelData currentLevelData;
    private CellDatabase cellDatabase;
    [SerializeField] private LevelEditorSettings editorSettings;

    //Variables de estado del editor
    private CellType selectedCellType = CellType.Wall;
    private bool isBucketModeActive = false;
    private float currentZoom = 1.0f;
    private Vector2 viewOffset = Vector2.zero;
    private const string PREFS_LAST_LEVEL_KEY = "SokobanEditor_LastLevelPath";

    //Estilos cacheados
    private GUIStyle tileButtonStyleNormal;
    private GUIStyle tileButtonStyleSelected;
    private GUIStyle bucketButtonStyle;
    private GUIStyle newButtonStyle;
    private GUIStyle furnitureLabelStyle;

    //Muebles
    private FurnitureDatabase furnitureDatabase;
    private string selectedFurnitureID = "";

    ///<summary>
    ///Metodo estatico para abrir la ventana desde el menu de Unity
    ///</summary>
    
    [MenuItem("Tools/Sokoban Level Editor")]
    public static void OpenWindow()
    {
        GetWindow<LevelEditor>("Level Editor", true);
    }

    ///<summary>
    ///Se ejecuta al abrir la ventana
    ///Inicializa las configuraciones y carga el ultimo estado
    ///</summary>
    private void OnEnable()
    {
        wantsMouseMove = true;

        LoadEditorSettings();
        LoadLastOpenedLevel();
        AutoLoadCellDatabase();

        //Hay que incluir un delay, para asegurar-nos de que no se salta ningún cambio
        EditorApplication.delayCall += AutoFitViewToGrid;
    }

    ///<summary>
    ///Bucle principal de dibujado de la interfaz gráfica del Editor
    ///</summary>

    private void OnGUI()
    {
        InitializeStyles();

        //Si no hay settings, detenemos la ejecucion de la UI y mostramos un aviso
        if (editorSettings == null)
        {
            EditorGUILayout.HelpBox("Falta el archivo LevelEditorSettings. Por favor, crea uno.", MessageType.Warning);
            return;
        }

        DrawAssetSelectionPanel();

        //Si no hay nivel cargado, detenemos la ejecucion de la UI y mostramos un aviso
        if (currentLevelData == null)
        {
            EditorGUILayout.HelpBox(editorSettings.noLevelWarning, MessageType.Info);
            return;
        }

        DrawDimensionsToolbar();
        DrawPaletteToolbar();

        if (selectedCellType == CellType.Furniture)
        {
            DrawFurnitureToolbar();
        }

        DrawMainGridArea();

        DrawFooterControls();

        RequestRepaintOnMouseMovement();
    }

    #region Inicializacion y Carga de Datos

    ///<summary>
    ///Busca y carga automaticamente el objeto de configuracion visual del editor
    ///</summary>
    private void LoadEditorSettings()
    {
        string[] guids = AssetDatabase.FindAssets("t:LevelEditorSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            editorSettings = AssetDatabase.LoadAssetAtPath<LevelEditorSettings>(path);
        }
        else
        {
            Debug.LogWarning("Sokoban Editor: No se encontro LevelEditorSettings. Crea uno haciendo click derecho en el proyecto -> Sokoban -> Editor Settings.");
        }
    }

    ///<summary>
    ///Busca y carga la base de datos de celdas (CellDatabase) automaticamente
    ///</summary>
    private void AutoLoadCellDatabase()
    {
        //CellDatabase
        string[] guids = AssetDatabase.FindAssets("t:CellDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            cellDatabase = AssetDatabase.LoadAssetAtPath<CellDatabase>(path);
        }

        //FurnitureDatabase
        string[] furnitureGuids = AssetDatabase.FindAssets("t:FurnitureDatabase");
        if (furnitureGuids.Length > 0)
        {
            string furnPath = AssetDatabase.GUIDToAssetPath(furnitureGuids[0]);
            furnitureDatabase = AssetDatabase.LoadAssetAtPath<FurnitureDatabase>(furnPath);
        }
    }

    ///<summary>
    ///Inicializa los estilos personalizados de los botones si no se han inicializado aun
    ///</summary>

    private void InitializeStyles()
    {
        if (tileButtonStyleNormal == null)
        {
            tileButtonStyleNormal = new GUIStyle(EditorStyles.miniButton) 
            { 
                fixedHeight = 20, margin = new RectOffset(2, 2, 2, 2) 
            };
            tileButtonStyleSelected = new GUIStyle(EditorStyles.miniButton) 
            { 
                fontStyle = FontStyle.Bold, fixedHeight = 30, 
                margin = new RectOffset(2, 2, 2, 2) 
            };
            bucketButtonStyle = new GUIStyle(EditorStyles.miniButton) 
            { 
                fontStyle = FontStyle.Bold, fixedHeight = 25, 
                margin = new RectOffset(2, 2, 2, 2) 
            };

            newButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 18
            };

            furnitureLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.black },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                wordWrap = false,
                clipping = TextClipping.Overflow
            };
        }
    }

    ///<summary>
    ///Intenta cargar el ultimo nivel editado leyendo la ruta guardada en las preferencias del editor
    ///</summary>
    private void LoadLastOpenedLevel()
    {
        if (EditorPrefs.HasKey(PREFS_LAST_LEVEL_KEY))
        {
            string path = EditorPrefs.GetString(PREFS_LAST_LEVEL_KEY);
            currentLevelData = AssetDatabase.LoadAssetAtPath<LevelData>(path);
        }
    }

    ///<summary>
    ///Guarda la ruta del nivel actual para recordarlo la proxima vez que se abra el editor
    ///</summary>
    private void SaveCurrentLevelPath()
    {
        if (currentLevelData != null)
        {
            string path = AssetDatabase.GetAssetPath(currentLevelData);
            EditorPrefs.SetString(PREFS_LAST_LEVEL_KEY, path);
        }
    }

    ///<summary>
    ///Abre la ventana de guardado, crea una nueva instancia de LevelData y la guarda como Asset
    ///</summary>
    private void CreateNewLevelAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            editorSettings.saveFileDialogTitle,
            editorSettings.saveFileDialogDefaultName,
            "asset",
            editorSettings.saveFileDialogMessage
        );

        if (string.IsNullOrEmpty(path)) return; //El usuario cancelo el dialogo

        LevelData newLevel = CreateInstance<LevelData>();
        newLevel.Resize(10, 10, true);

        AssetDatabase.CreateAsset(newLevel, path);
        AssetDatabase.SaveAssets();

        currentLevelData = newLevel;
        SaveCurrentLevelPath();
        AutoFitViewToGrid();
    }

    #endregion

    #region Dibujado de la UI (Paneles)

    ///<summary>
    ///Dibuja la seccion superior donde se asignan el LevelData y la base de datos de celdas
    ///Tambien incluye el boton para crear un nuevo nivel.
    ///</summary>
    private void DrawAssetSelectionPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        //Controlamos si el usuario cambia el asset manualmente
        EditorGUI.BeginChangeCheck();
        currentLevelData = (LevelData)EditorGUILayout.ObjectField("Level Asset", currentLevelData, typeof(LevelData), false);
        if (EditorGUI.EndChangeCheck())
        {
            SaveCurrentLevelPath();
        }

        //Boton de NUEVO nivel
        GUI.backgroundColor = editorSettings.newButtonColor;
        bool pressedNew = GUILayout.Button("NEW", newButtonStyle, GUILayout.Width(60));
        GUI.backgroundColor = editorSettings.defaultBackgroundColor;

        if (pressedNew)
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            CreateNewLevelAsset();

            GUIUtility.ExitGUI();
            return;
        }
        EditorGUILayout.EndHorizontal();

        cellDatabase = (CellDatabase)EditorGUILayout.ObjectField("Cell Database", cellDatabase, typeof(CellDatabase), false);
        EditorGUILayout.EndVertical();
    }

    ///<summary>
    ///Dibuja la barra de herramientas para ajustar el ancho y alto del nivel
    ///</summary>
    private void DrawDimensionsToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();

        EditorGUIUtility.labelWidth = 45f;
        int newWidth = EditorGUILayout.IntField("Width", currentLevelData.width, GUILayout.Width(90));
        GUILayout.Space(5);
        int newHeight = EditorGUILayout.IntField("Height", currentLevelData.height, GUILayout.Width(90));

        //Si las medidas cambiaron, aplicamos el redimensionamiento
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(currentLevelData, "Resize Level");
            currentLevelData.Resize(newWidth, newHeight);
            EditorUtility.SetDirty(currentLevelData);
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    ///<summary>
    ///Dibuja la barra de herramientas con la paleta de celdas y el boton del cubo de pintura (Flood Fill)
    ///</summary>
    private void DrawPaletteToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(35));

        //Dibuja el boton del cubo de pintura
        DrawBucketButton();

        GUILayout.Space(5);
        EditorGUILayout.LabelField("|", GUILayout.Width(10));

        //Dibuja dinamicamente un boton por cada tipo de celda en el enumerador
        DrawCellTypeButtons();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    ///<summary>
    ///Dibuja una barra de herramientas secundaria para seleccionar furniture
    ///</summary>

    private void DrawFurnitureToolbar()
    {
        if (furnitureDatabase == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Muebles Especiales", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(35));

        foreach (var entry in furnitureDatabase.entries)
        {
            bool isSelected = (selectedFurnitureID == entry.id);

            //Usamos el color definido en la base de datos de muebles
            GUIStyle currentStyle = tileButtonStyleNormal;
            if (isSelected) currentStyle = tileButtonStyleSelected;

            if (GUILayout.Button(entry.id, currentStyle))
            {
                selectedFurnitureID = entry.id;
                selectedCellType = CellType.Furniture;
            }
        }

        GUI.backgroundColor = editorSettings.defaultBackgroundColor;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    ///<summary>
    ///Dibuja el boton de cubo de pintura y maneja su estado visual
    ///</summary>
    private void DrawBucketButton()
    {
        if (isBucketModeActive)
        {
            GUI.backgroundColor = editorSettings.bucketActiveColor;
        }

        if (GUILayout.Button("CUBO", bucketButtonStyle, GUILayout.Width(60)))
        {
            isBucketModeActive = !isBucketModeActive;
        }

        GUI.backgroundColor = editorSettings.defaultBackgroundColor;
    }

    ///<summary>
    ///Genera los botones iterando sobre el enum CellType para seleccionar la brocha actual.
    ///</summary>
    ///
    private void DrawCellTypeButtons()
    {
        foreach (CellType type in Enum.GetValues(typeof(CellType)))
        {
            Color baseColor = GetCellColor(type);
            bool isSelected = (selectedCellType == type);

            //Atenua el color si no esta seleccionado
            if (isSelected) GUI.backgroundColor = baseColor;
            else GUI.backgroundColor = baseColor * editorSettings.inactivePaletteColorMultiplier;

            GUIStyle currentStyle = tileButtonStyleNormal;
            if (isSelected) currentStyle = tileButtonStyleSelected;

            if (GUILayout.Button(type.ToString(), currentStyle))
            {
                selectedCellType = type;

                //SI es un mueble agregamos el texto
                if (selectedCellType == CellType.Furniture && string.IsNullOrEmpty(selectedFurnitureID))
                {
                    if (furnitureDatabase != null && furnitureDatabase.entries.Count > 0)
                    {
                        selectedFurnitureID = furnitureDatabase.entries[0].id;
                    }
                }
            }
        }

        //Lo demás...
        GUI.backgroundColor = editorSettings.defaultBackgroundColor;
    }

    ///<summary>
    ///Define el area de trabajo y coordina el dibujado de la cuadricula y las interacciones de raton
    ///</summary>
    
    private void DrawMainGridArea()
    {
        Rect drawingArea = new Rect(0, editorSettings.topSpace, position.width, position.height - editorSettings.topSpace - editorSettings.bottomSpace);

        //Procesamos los inputs globales
        HandleCameraInputs(drawingArea);

        //GUI readjusment
        GUI.BeginGroup(drawingArea);
        RenderGrid(drawingArea);
        GUI.EndGroup();
    }

    ///<summary>
    ///Dibuja el panel inferior con la leyenda, los controles de zoom y el boton de limpieza total
    ///</summary>
    private void DrawFooterControls()
    {
        GUILayout.FlexibleSpace(); //Empuja hacia la parte baja de la ventana
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(30));

        Vector2 scrollFooter = Vector2.zero;
        EditorGUILayout.BeginScrollView(scrollFooter, GUIStyle.none, GUIStyle.none, GUILayout.ExpandHeight(false));
        GUILayout.Label(editorSettings.footerControlsText, EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(30));
        
        //Controles de camara manuales
        DrawZoomAndCenterButtons();

        GUILayout.Space(10);

        //Limpiar todo
        DrawClearAllButton();
        EditorGUILayout.EndHorizontal();
    }

    ///<summary>
    ///Dibuja los botones para controlar el zoom y centrar la vista manualmente
    ///</summary>
    private void DrawZoomAndCenterButtons()
    {
        if (GUILayout.Button("Zoom In", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            ModifyZoom(editorSettings.zoomStep);
        }

        if (GUILayout.Button("Zoom Out", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            ModifyZoom(-editorSettings.zoomStep);
        }

        if (GUILayout.Button("Centrar", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            AutoFitViewToGrid();
        }
    }

    ///<summary>
    ///Dibuja el boton de borrar todo el nivel y ejecuta la logica si se confirma
    ///</summary>
    private void DrawClearAllButton()
    {
        GUI.backgroundColor = editorSettings.clearButtonColor;
        if (GUILayout.Button("CLEAR ALL", EditorStyles.miniButton, GUILayout.Width(85)))
        {
            if (EditorUtility.DisplayDialog(editorSettings.clearDialogTitle, editorSettings.clearDialogMessage, editorSettings.clearDialogOk, editorSettings.clearDialogCancel))
            {
                ClearEntireLevel();
            }
        }
        GUI.backgroundColor = editorSettings.defaultBackgroundColor;
    }

    #endregion

    #region Logica de Renderizado del Grid

    ///<summary>
    ///Renderiza visualmente la cuadricula y las celdas en pantalla basandose en el zoom y offset.
    ///</summary>
    private void RenderGrid(Rect area)
    {
        //Calculos de dimensiones basados en el zoom
        float cellSize = Mathf.Round(editorSettings.baseCellSize * currentZoom);
        float gap = Mathf.Clamp(currentZoom * 2f, 1f, 6f);
        float outline = Mathf.Clamp(currentZoom * 2f, 1f, 4f);

        float totalGridWidth = currentLevelData.width * cellSize;
        float totalGridHeight = currentLevelData.height * cellSize;

        //Centrar el grid en el area disponible, aplicando el offset del usuario
        Rect gridBackgroundRect = new Rect(
            (area.width - totalGridWidth) * 0.5f + viewOffset.x,(area.height - totalGridHeight) * 0.5f + viewOffset.y,
            totalGridWidth,totalGridHeight);

        //Fondo oscuro del Grid
        EditorGUI.DrawRect(gridBackgroundRect, editorSettings.gridBackgroundColor);

        Vector2 mousePos = Event.current.mousePosition;

        //Dibujar cada celda individual
        for (int y = 0; y < currentLevelData.height; y++)
        {
            for (int x = 0; x < currentLevelData.width; x++)
            {
                DrawSingleCell(x, y, cellSize, gap, outline, gridBackgroundRect, mousePos);
            }
        }
    }

    ///<summary>
    ///Dibuja una celda especifica dentro del grid y comprueba si el raton esta interactuando con ella
    ///</summary>

    private void DrawSingleCell(int x, int y, float cellSize, float gap, float outline, Rect gridBackgroundRect, Vector2 mousePos)
    {
        int arrayIndex = x + y * currentLevelData.width;
        if (arrayIndex >= currentLevelData.cells.Length) return;

        Rect cellRect = new Rect(gridBackgroundRect.x + x * cellSize + gap * 0.5f,
            gridBackgroundRect.y + y * cellSize + gap * 0.5f,
            cellSize - gap, cellSize - gap);

        EditorGUI.DrawRect(cellRect, editorSettings.cellBackgroundColor);

        Rect innerRect = new Rect(Mathf.Round(cellRect.x + outline),Mathf.Round(cellRect.y + outline),Mathf.Round(cellRect.width - outline * 2), Mathf.Round(cellRect.height - outline * 2));

        Color finalColor = GetCellColor(currentLevelData.cells[arrayIndex]);
        EditorGUI.DrawRect(innerRect, finalColor);

        //FURNITURE
        string furnID = currentLevelData.GetFurnitureID(arrayIndex);
        if (!string.IsNullOrEmpty(furnID))
        {
            //Font Size
            furnitureLabelStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(12 * currentZoom));

            //Escribe el texto centrado
            Vector2 textSize = furnitureLabelStyle.CalcSize(new GUIContent(furnID));

            Rect centeredRect = new Rect(innerRect.x + (innerRect.width - textSize.x) * 0.5f,
                innerRect.y + (innerRect.height - textSize.y) * 0.5f,textSize.x,textSize.y);

            GUI.Label(centeredRect, furnID, furnitureLabelStyle);
        }

        //Resaltado de la celda en la que se encuentra el mouse
        if (cellRect.Contains(mousePos))
        {
            EditorGUI.DrawRect(innerRect, editorSettings.cellHoverColor);
            HandleMouseInteractionOnCell(x, y, arrayIndex);
        }
    }
    #endregion

    #region Logica de Interaccion (Inputs)

    ///<summary>
    ///Procesa el movimiento panoramico y el zoom con la rueda del raton sobre el area de dibujo
    ///</summary>
    private void HandleCameraInputs(Rect area)
    {
        Event e = Event.current;
        if (!area.Contains(e.mousePosition)) return;

        //Click medio y arrastrar para mover la camara (Offset)
        if (e.type == EventType.MouseDrag && e.button == 2)
        {
            viewOffset += e.delta;
            e.Use();
        }

        //Rueda del raton para Zoom
        if (e.type == EventType.ScrollWheel)
        {
            ModifyZoom(-e.delta.y * editorSettings.scrollSensitivity);
            e.Use();
        }
    }

    ///<summary>
    ///Procesa los clicks izquierdo y derecho sobre una celda especifica
    ///</summary>
    private void HandleMouseInteractionOnCell(int x, int y, int cellIndex)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            //Click Izquierdo: Pintar o Rellenar
            if (e.button == 0)
            {
                if (isBucketModeActive && selectedCellType != CellType.Player && e.type == EventType.MouseDown)
                {
                    ExecuteFloodFill(x, y, selectedCellType);
                }
                else if (!isBucketModeActive)
                {
                    PaintSingleCell(cellIndex, selectedCellType);
                }
                e.Use();
            }
            //Click Derecho: Borrar (Pintar como Empty)
            else if (e.button == 1)
            {
                PaintSingleCell(cellIndex, CellType.Empty);
                e.Use();
            }
        }
    }

    ///<summary>
    ///Aplica un algoritmo de relleno (Flood Fill) para pintar areas contiguas del mismo tipo
    ///</summary>
    ///

    private void ExecuteFloodFill(int startX, int startY, CellType newType)
    {
        CellType targetType = currentLevelData.cells[startX + startY * currentLevelData.width];

        if (targetType == newType) return;

        Undo.RecordObject(currentLevelData, "Bucket Fill");

        Stack<Vector2Int> pixelsToProcess = new Stack<Vector2Int>();
        pixelsToProcess.Push(new Vector2Int(startX, startY));

        while (pixelsToProcess.Count > 0)
        {
            Vector2Int currentPoint = pixelsToProcess.Pop();

            //Si está dentro del mapa...
            if (currentPoint.x < 0 || currentPoint.x >= currentLevelData.width || currentPoint.y < 0 || currentPoint.y >= currentLevelData.height) continue;

            int index = currentPoint.x + currentPoint.y * currentLevelData.width;

            if (currentLevelData.cells[index] == targetType)
            {
                currentLevelData.cells[index] = newType;

                pixelsToProcess.Push(new Vector2Int(currentPoint.x + 1, currentPoint.y));
                pixelsToProcess.Push(new Vector2Int(currentPoint.x - 1, currentPoint.y));
                pixelsToProcess.Push(new Vector2Int(currentPoint.x, currentPoint.y + 1));
                pixelsToProcess.Push(new Vector2Int(currentPoint.x, currentPoint.y - 1));
            }
        }

        EditorUtility.SetDirty(currentLevelData);
    }

    ///<summary>
    ///Cambia el tipo de una celda especifica asegurando el registro de Undo
    ///</summary>
    
    private void PaintSingleCell(int cellIndex, CellType newType)
    {
        if (currentLevelData.cells[cellIndex] != newType || currentLevelData.GetFurnitureID(cellIndex) != selectedFurnitureID)
        {
            Undo.RecordObject(currentLevelData, "Paint Tile");

            if (newType == CellType.Player)
            {
                RemoveExistingPlayer();
            }

            currentLevelData.cells[cellIndex] = newType;

            //Si pintamos una "caja" (furniture), guardamos el ID del mueble seleccionado
            //Si pintamos cualquier otra cosa, borramos el metadata de esa celda
            string idToSave = "";
            if (newType == CellType.Furniture) idToSave = selectedFurnitureID;

            currentLevelData.SetFurnitureMetadata(cellIndex, idToSave);

            EditorUtility.SetDirty(currentLevelData);
        }
    }

    #endregion

    #region Utilidades y Helpers

    ///<summary>
    ///Busca y elimina cualquier celda de tipo Player existente en el nivel
    ///</summary>
    private void RemoveExistingPlayer()
    {
        for (int j = 0; j < currentLevelData.cells.Length; j++)
        {
            if (currentLevelData.cells[j] == CellType.Player)
            {
                currentLevelData.cells[j] = CellType.Empty;
            }
        }
    }


    private void ClearEntireLevel()
    {
        Undo.RecordObject(currentLevelData, "Clear Level");
        for (int i = 0; i < currentLevelData.cells.Length; i++)
        {
            currentLevelData.cells[i] = CellType.Empty;
        }
        EditorUtility.SetDirty(currentLevelData);
    }

    ///<summary>
    ///Calcula y ajusta el zoom y la posicion para que todo el nivel quepa en pantalla
    ///</summary>
    private void AutoFitViewToGrid()
    {
        if (currentLevelData == null || editorSettings == null) return;

        viewOffset = Vector2.zero;

        float zoomBasedOnWidth = position.width * 0.7f / (currentLevelData.width * editorSettings.baseCellSize);
        float zoomBasedOnHeight = (position.height - 200f) * 0.7f / (currentLevelData.height * editorSettings.baseCellSize);

        currentZoom = Mathf.Min(zoomBasedOnWidth, zoomBasedOnHeight);
        Repaint();
    }

    private void ModifyZoom(float amount)
    {
        currentZoom = Mathf.Clamp(currentZoom + amount, editorSettings.minZoom, editorSettings.maxZoom);
    }

    private void RequestRepaintOnMouseMovement()
    {
        if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag)
        {
            Repaint();
        }
    }

    ///<summary>
    ///Devuelve el color asociado al tipo de celda consultando la base de datos
    ///</summary>
    private Color GetCellColor(CellType type)
    {
        if (cellDatabase == null || editorSettings == null)
        {
            return editorSettings != null ? editorSettings.fallbackCellColor : Color.gray;
        }

        return cellDatabase.GetColor(type);
    }

    #endregion
}