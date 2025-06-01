using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Displays a 3x3 grid of colored cubes based on characters read from an external text file.
/// Each character corresponds to a color, and the grid can be navigated using WASD keys.
/// The system wraps around the edges of the text file, creating a toroidal navigation behavior.
/// </summary>
public class GridVisualizer : MonoBehaviour
{
    private FileSystemWatcher fileWatcher;
    private bool fileChanged;
    private string dataFilePath; 
    
    [Header("Text File Input")]
    public TextAsset textFile; // Optional default file from inspector

    [TextArea(10, 20)]
    public string editableText; // Editable area in Inspector

    [Header("Prefab and Materials")]
    [Tooltip("Prefab of the cube to instantiate")]
    public GameObject cubePrefab;

    [Tooltip("Material for symbol '1' (Red)")]
    public Material red;

    [Tooltip("Material for symbol '2' (Yellow)")]
    public Material yellow;

    [Tooltip("Material for symbol '3' (Blue)")]
    public Material blue;

    [Tooltip("Material for symbol '4' (Purple)")]
    public Material purple;

    /// <summary>
    /// Array containing each line of the text file.
    /// Each line represents a row in the virtual grid.
    /// </summary>
    private string[] dataLines;

    /// <summary>
    /// The current center row in the grid display.
    /// </summary>
    private int currentRow;

    /// <summary>
    /// The current center column in the grid display.
    /// </summary>
    private int currentCol = 0;

    /// <summary>
    /// All currently displayed cube GameObjects. Cleared and refreshed each frame.
    /// </summary>
    private List<GameObject> activeCubes = new List<GameObject>();

    /// <summary>
    /// Called once at the beginning of the scene. 
    /// Loads the data file, selects a random row as the starting point, and displays the initial 3x3 grid.
    /// </summary>
    void Start()
    {
        // Combine path to StreamingAssets/data.txt file
        string path = Path.Combine(Application.streamingAssetsPath, "data.txt");

        // Check if the file exists before attempting to read it
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return;
        }

        // Read all lines into the dataLines array
        dataLines = File.ReadAllLines(path);

        // Check if the file was empty
        if (dataLines.Length == 0)
        {
            Debug.LogError("The file is empty.");
            return;
        }

        dataFilePath = Path.Combine(Application.streamingAssetsPath, "data.txt");
        dataLines = File.ReadAllLines(dataFilePath);

        if (textFile != null)
        {
            editableText = textFile.text;
        }

        if (File.Exists(dataFilePath))
        {
            editableText = File.ReadAllText(dataFilePath);
        }
        else
        {
            // Save default if file doesn't exist
            File.WriteAllText(dataFilePath, editableText);
        }

        dataLines = editableText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        // Select a random row from the file to start from
        currentRow = Random.Range(0, dataLines.Length);
        currentCol = 0;

        SetupFileWatcher();
        // Display initial grid of cubes
        DisplayCubes();
    }

    /// <summary>
    /// Called once per frame. Handles WASD input for navigation in the data grid.
    /// Triggers redrawing of the 3x3 cube grid centered on the new location.
    /// </summary>
    void Update()
    {
        // Move up in the grid (W key)
        if (Input.GetKeyDown(KeyCode.W))
        {
            currentRow = Mod(currentRow - 1, dataLines.Length);
            DisplayCubes();
        }

        // Move down in the grid (S key)
        if (Input.GetKeyDown(KeyCode.S))
        {
            currentRow = Mod(currentRow + 1, dataLines.Length);
            DisplayCubes();
        }

        // Move left in the grid (A key)
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentCol = Mod(currentCol - 1, dataLines[0].Length);
            DisplayCubes();
        }

        // Move right in the grid (D key)
        if (Input.GetKeyDown(KeyCode.D))
        {
            currentCol = Mod(currentCol + 1, dataLines[0].Length);
            DisplayCubes();
        }

        if (fileChanged)
        {
            fileChanged = false;
            ReloadData();
        }
    }

    /// <summary>
    /// Destroys all existing cubes and creates a fresh 3x3 grid based on the currentRow and currentCol.
    /// Characters from the dataLines determine the color of each cube.
    /// Grid wraps around edges using modular arithmetic.
    /// </summary>
    void DisplayCubes()
    {
        ClearCubes();

        // Define spacing between cubes
        float spacing = 1.1f;

        // Loop over vertical (-1 to 1 = 3 rows)
        for (int y = -1; y <= 1; y++)
        {
            int row = Mod(currentRow + y, dataLines.Length); // Wrap around top/bottom
            string line = dataLines[row];

            // Loop over horizontal (-1 to 1 = 3 columns)
            for (int x = -1; x <= 1; x++)
            {
                int col = Mod(currentCol + x, line.Length); // Wrap around left/right
                char symbol = line[col];

                // Get color for the character
                Material mat = GetMaterialFromChar(symbol);
                if (mat == null) continue;

                // NEW: Add spacing between cubes
                Vector3 position = new Vector3(x * spacing, -y * spacing, 0);

                // Instantiate cube
                GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
                cube.GetComponent<Renderer>().material = mat;
                activeCubes.Add(cube);
            }
        }

    }
    
    /// <summary>
     /// Sets up a file watcher that listens to changes in the data file and triggers reloading.
     /// </summary>
    void SetupFileWatcher()
    {
        string directory = Path.GetDirectoryName(dataFilePath);
        string fileName = Path.GetFileName(dataFilePath);

        fileWatcher = new FileSystemWatcher(directory, fileName);
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;

        fileWatcher.Changed += (sender, args) =>
        {
            // Delay to avoid file lock during write
            Thread.Sleep(100);
            fileChanged = true;
        };

        fileWatcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Reloads the data from file and refreshes both editable text and cube visualization.
    /// </summary>
    public void ReloadData()
    {
        try
        {
            // Reload text from file
            editableText = File.ReadAllText(dataFilePath);

            // Update internal data lines
            dataLines = editableText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            // Refresh the cubes
            DisplayCubes();

            Debug.Log("Data file reloaded from disk.");
        }
        catch (IOException ex)
        {
            Debug.LogWarning("Failed to reload data: " + ex.Message);
        }
    }
    /// <summary>
    /// Converts a character from the text file into its corresponding material (color).
    /// Returns null if character is not recognized.
    /// </summary>
    /// <param name="c">Character from text file (1, 2, 3, or 4)</param>
    /// <returns>Material corresponding to the given character</returns>
    Material GetMaterialFromChar(char c)
    {
        return c switch
        {
            '1' => red,
            '2' => yellow,
            '3' => blue,
            '4' => purple,
            _ => null // Unknown symbol
        };
    }

    /// <summary>
    /// Computes modulus that correctly handles negative values to ensure wrapping.
    /// Used to wrap around both rows and columns.
    /// </summary>
    /// <param name="x">Current index</param>
    /// <param name="m">Maximum value (length)</param>
    /// <returns>Wrapped index</returns>
    int Mod(int x, int m) => (x % m + m) % m;

    /// <summary>
    /// Destroys all currently displayed cubes and clears the tracking list.
    /// Called before redrawing a new 3x3 grid.
    /// </summary>
    void ClearCubes()
    {
        foreach (var cube in activeCubes)
        {
            Destroy(cube);
        }

        activeCubes.Clear();
    }

    private string lastSyncedText;

    /// <summary>
    /// Auto-save editable text to file when changed from Unity inspector in Play Mode.
    /// </summary>
    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) return;

        // Prevent infinite loop: only save if changed
        if (editableText != lastSyncedText && !string.IsNullOrEmpty(editableText))
        {
            dataFilePath = Path.Combine(Application.streamingAssetsPath, "data.txt");
            File.WriteAllText(dataFilePath, editableText);
            lastSyncedText = editableText;

            ReloadData();
            Debug.Log("Text from inspector saved to file and applied.");
        }
#endif
    }
}
