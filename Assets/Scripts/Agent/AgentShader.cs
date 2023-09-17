using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AgentShader : MonoBehaviour
{    
    private RawImage image;
    private Texture2D texture;

    [Header("Render Settings")]
    [SerializeField] private Vector2Int dimensions;
    [SerializeField] private bool autoUpdateDimensions = true;
    [SerializeField] private float delayBetweenUpdate;
    [SerializeField] private bool wantUpdate;
    [SerializeField] private int nbrAgents;
    [SerializeField] private float angleRandomizer;

    [Header("Shader data")]
    [SerializeField] private string kernelName;
    [SerializeField] private ComputeShader shader;
    
    [SerializeField] private SpawnPositionType spawnPositionType = SpawnPositionType.CENTER;
    [SerializeField] private BorderCollisionType borderCollisionType = BorderCollisionType.CENTER;

    private readonly Color initialBackgroundColor = Color.gray;
    private RenderTexture renderTexture;
    private ComputeBuffer agentsBuffer;
    private ComputeBuffer cellsBuffer;
    private ComputeBuffer backgroundColorBuffer;
    private float currentTime;

    private int kernelShaderUpdateIndex;

    [Serializable]
    struct Cell
    {
        public float speed;
        public Color cellColor;
    }
    [SerializeField] private List<Cell> cells;
    
    [Serializable]
    struct Agent {
        public Vector2 position;
        public float angle;
        public int cellIndex;
    }

    private Agent[] agents;
    
    [Serializable]
    struct InitialBackgroundColor
    {
        public Color backgroundColor;
    }

    [SerializeField] private List<InitialBackgroundColor> initialBackgroundColors;

    // Start is called before the first frame update
    private void Start()
    {
        image = GameObject.Find("ShaderTarget").GetComponent<RawImage>();
        if(autoUpdateDimensions)
            dimensions = new Vector2Int(Screen.width, Screen.height);
        // Constructeur du RenderTexture
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(dimensions.x, dimensions.y, 0);
            renderTexture.enableRandomWrite = true;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.Create();
        }
        
        Graphics.Blit(texture, renderTexture);
        
        // Shader setup
        kernelShaderUpdateIndex = shader.FindKernel(kernelName);
        shader.SetTexture(kernelShaderUpdateIndex, "Texture", renderTexture);
        shader.SetInt("screenSizeX", dimensions.x);
        shader.SetInt("screenSizeY", dimensions.y);
        shader.SetInt("nbrAgents", nbrAgents);
        shader.SetInt("borderCollisionType", (int)borderCollisionType);
        shader.SetInt("init", 1);

        // Buffer setup
        agents = CreateAllAgents();
        cellsBuffer = new ComputeBuffer(cells.Count, GenerateStrideForCell());
        agentsBuffer = new ComputeBuffer(agents.Length, GenerateStrideForAgent());
        backgroundColorBuffer = new ComputeBuffer(initialBackgroundColors.Count, GenerateStrideForBackgroundColor());

        // Setup data
        backgroundColorBuffer.SetData(initialBackgroundColors);
        shader.SetBuffer(kernelShaderUpdateIndex, "initialBackgroundColor", backgroundColorBuffer);
        
        UpdateTexture();
        shader.SetInt("init", 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (wantUpdate)
            currentTime += Time.deltaTime;
        if (currentTime > delayBetweenUpdate)
        {
            currentTime = 0;
            UpdateTexture();
        }
    }
    
    private void UpdateTexture()
    {
        cellsBuffer.SetData(cells);
        agentsBuffer.SetData(agents);
        shader.SetBuffer(kernelShaderUpdateIndex, "cellsBuffer", cellsBuffer);
        shader.SetBuffer(kernelShaderUpdateIndex, "agentsBuffer", agentsBuffer);
        
        shader.SetFloat("deltaTime", Time.deltaTime);

        shader.GetKernelThreadGroupSizes(kernelShaderUpdateIndex, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        shader.Dispatch(kernelShaderUpdateIndex, renderTexture.width / (int)xGroupSize, renderTexture.height / (int)yGroupSize, 1);

        agentsBuffer.GetData(agents);
        if(angleRandomizer != 0f){
            for(int i = 0; i < agents.Length; i++)
                agents[i].angle += UnityEngine.Random.Range(-angleRandomizer, angleRandomizer);
        }


        RenderTexture.active = renderTexture;
        image.material.mainTexture = renderTexture;
    }

    private void OnDestroy()
    {
        backgroundColorBuffer.Dispose();
        cellsBuffer.Dispose();
        agentsBuffer.Dispose();
    }
    
    private int GenerateStrideForBackgroundColor()
    {
        int color = sizeof(float) * 4;
        return color;
    }

    private int GenerateStrideForCell()
    {
        int speed = sizeof(float);
        int color = sizeof(float) * 4;
        return speed + color;
    }
    
    private int GenerateStrideForAgent()
    {
        int position = sizeof(float) * 2;
        int angle = sizeof(float);
        int cellIndex = sizeof(int);
        return position + angle + cellIndex;
    }

    private Agent[] CreateAllAgents()
    {
        // Create agents with initial positions and angles
        Agent[] agents = new Agent[nbrAgents];
        for (int i = 0; i < agents.Length; i++)
        {
            Vector2 spawnPosition = Vector2.zero;
            
            if (spawnPositionType == SpawnPositionType.CENTER)
            {
                spawnPosition = GetMapCenterLocation();
            }
            else
            {
                spawnPosition = GetRandomLocation();
            }

            int randomCell = UnityEngine.Random.Range(1, cells.Count + 1);
            int cellIndex = randomCell - 1;

            agents[i] = new Agent()
            {
                position = spawnPosition,
                angle = GetRandomAngle(), 
                cellIndex = cellIndex
            };
        }

        return agents;
    }

    private Vector2 GetRandomLocation()
    {
        int randomXLocation = UnityEngine.Random.Range(1, dimensions.x);
        int randomYLocation = UnityEngine.Random.Range(1, dimensions.y);
        
        return new Vector2(randomXLocation, randomYLocation);
    }

    private Vector2 GetMapCenterLocation()
    {
        return new Vector2(dimensions.x / 2f, dimensions.y / 2f);
    }

    private float GetRandomAngle()
    {
        return UnityEngine.Random.value * Mathf.PI * 2;
    }
}
