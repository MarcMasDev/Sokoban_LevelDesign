using UnityEngine;

///<summary>
///Contenedor de datos para la configuracion visual y de textos del Level Editor.
///Esto evita tener valores hardcoded en la logica principal.
///</summary>

[CreateAssetMenu(fileName = "LevelEditorSettings", menuName = "Sokoban/Editor Settings")]
public class LevelEditorSettings : ScriptableObject
{
    [Header("Textos y Mensajes")]
    public string windowTitle = "Level Editor";
    public string noLevelWarning = "No hay nivel seleccionado. Pulsa 'NEW'.";
    public string footerControlsText = "Boton Izquierdo: Pintar | Boton Derecho: Borrar | Rueda: Zoom/Mover";
    public string clearDialogTitle = "Limpiar";
    public string clearDialogMessage = "Seguro que quieres borrar todo?";
    public string clearDialogOk = "Si";
    public string clearDialogCancel = "No";
    public string saveFileDialogTitle = "Nuevo Nivel";
    public string saveFileDialogDefaultName = "NewLevel";
    public string saveFileDialogMessage = "Pon un nombre al nivel";

    [Header("Colores de Botones y UI")]
    public Color newButtonColor = new Color(0.4f, 1f, 0.4f);
    public Color clearButtonColor = new Color(1f, 0.4f, 0.4f);
    public Color bucketActiveColor = new Color(0.2f, 0.9f, 1f);
    public Color defaultBackgroundColor = Color.white;
    public Color inactivePaletteColorMultiplier = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Colores del Grid")]
    public Color gridBackgroundColor = new Color(0.1f, 0.1f, 0.1f);
    public Color cellBackgroundColor = new Color(1f, 1f, 1f, 0.05f);
    public Color cellHoverColor = new Color(1f, 1f, 1f, 0.3f);
    public Color fallbackCellColor = Color.gray;

    [Header("Medidas y Espaciados")]
    public float baseCellSize = 40f;
    public float topSpace = 135f;
    public float bottomSpace = 40f;
    public float minZoom = 0.1f;
    public float maxZoom = 8f;
    public float zoomStep = 0.1f;
    public float scrollSensitivity = 0.05f;
}