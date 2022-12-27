using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOfWar : MonoBehaviour
{
    // Buffer structs
    [System.Serializable]
    struct Rule{
        // 10 to cancel
        [Range(0,10)]
        public uint lonelinessFactor;
        [Range(0,10)]
        public uint overPopulationFactor;
        [Range(0,10)]
        public uint teamConqueerFactor;
        [Range(0,10)]
        public uint cellBirthFactor;
    };

    [System.Serializable]
    struct Team{
        public Color aliveColor;
        public Color initialColorAndGreater;
        public Color initialColorAndLesser;
    }
    private RawImage image;
    private int kernelIndex;
    private RenderTexture renderTexture;
    private Texture2D texture;
    private float currentTime;
    ComputeBuffer ruleBuffer;
    ComputeBuffer teamBuffer;

    [Header("Render Settings")]
    [SerializeField] private Vector2Int dimensions;
    [SerializeField] private bool autoUpdateDimensions = true;
    [SerializeField] private float delayBetweenUpdate;
    [SerializeField] private bool wantUpdate;

    [Header("Shader data")]
    [SerializeField] private string kernelName;
    [SerializeField] private ComputeShader shader;

    [Header("Buffer data")]
    [SerializeField] private Rule rules;
    [SerializeField] private List<Team> teams;

    private void Start()
    {
        image = GameObject.Find("ShaderTarget").GetComponent<RawImage>();
        if(autoUpdateDimensions)
            dimensions = new Vector2Int(Screen.width, Screen.height);
        // Initialisation
        kernelIndex = shader.FindKernel(kernelName);

        // Generation de la texture 2D perlin noise
        texture = GenerateRandomWhites();

        // Constructeur du RenderTexture
        if(renderTexture == null){
            renderTexture = new RenderTexture(dimensions.x, dimensions.y, 0);
            renderTexture.enableRandomWrite = true;
            renderTexture.filterMode = FilterMode.Point;
            renderTexture.Create();
        }
        Graphics.Blit(texture, renderTexture);

        shader.SetTexture(kernelIndex, "Texture", renderTexture);
        shader.SetInt("screenSizeX", (int) dimensions.x);
        shader.SetInt("screenSizeY", (int) dimensions.y);

        ruleBuffer = new ComputeBuffer(1, GenerateStrideNumberRule());
        teamBuffer = new ComputeBuffer(teams.Count, GenerateStrideNumberTeam());

        UpdateTexture(1); // Initialization
    }

    private void Update()
    {
        if(wantUpdate)
            currentTime += Time.deltaTime;
        if(currentTime > delayBetweenUpdate){
            currentTime = 0;
            UpdateTexture(0); // Update
        }  
    }

    private void UpdateTexture(int init){
        shader.SetInt("init", init);

        // Setting the RuleBuffer datas
        ruleBuffer.SetData(new List<Rule>{rules});
        shader.SetBuffer(kernelIndex, "RuleBuffer", ruleBuffer);
        // Setting the AgentBuffer datas
        List<Team> newOrder = new List<Team>();
        Team firstTeam = teams[0];
        teams.Remove(firstTeam);
        foreach(Team team in teams){
            newOrder.Add(team);
        }
        newOrder.Add(firstTeam);
        teams = newOrder;
        
        teamBuffer.SetData(teams);
        shader.SetBuffer(kernelIndex, "TeamBuffer", teamBuffer);


        shader.GetKernelThreadGroupSizes(kernelIndex, out uint xGroupSize, out uint yGroupSize, out uint zGroupSize);
        shader.Dispatch(kernelIndex, renderTexture.width / (int) xGroupSize, renderTexture.height / (int) yGroupSize, 1);

        RenderTexture.active = renderTexture;
        image.material.mainTexture = renderTexture;        
    }

    private void OnDestroy() {
        ruleBuffer.Dispose();
        teamBuffer.Dispose();
    }

    // Random color generation
    private Texture2D GenerateRandomWhites(){
        Texture2D texture = new Texture2D(dimensions.x, dimensions.y);
        for(int x = 0; x < dimensions.x; x ++){
            for(int y = 0; y < dimensions.y; y ++){
                float randomWhite = Random.Range(0f,1f);
                Color color = new Color(randomWhite, randomWhite, randomWhite);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    // Stride numbers generators
    private int GenerateStrideNumberRule(){
        int factors = sizeof(uint) * 4;
        int strideNumber = factors;
        return strideNumber;
    }
    private int GenerateStrideNumberTeam(){
        int colorSize = sizeof(float) * 4 * 3;
        int strideNumber = colorSize;
        return strideNumber;
    }

    private int GenerateStrideNumberInactive(){
        int colorSize = sizeof(float) * 4;
        int strideNumber = colorSize;
        return strideNumber;
    }
}
