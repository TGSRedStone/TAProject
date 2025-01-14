using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[ExecuteInEditMode]
public class LocalToWorld : MonoBehaviour
{
    public Transform DirectionalLight;
    public Material SkyBoxMaterial;
    public Material CloudMaterial;
    private Matrix4x4 LtoW_Matrix = Matrix4x4.identity;
    private float AuroraStrength;
    private static readonly int LtoW = Shader.PropertyToID("_LtoW");
    
    [Range(1, 64)]
    public int SampleCount = 16;

    [ColorUsage(false, true)]
    public Color IncomingLight = new Color(4, 4, 4, 4);
    [Range(0, 10.0f)]
    public float MieScatterCoef = 1;
    [Range(0, 10.0f)]
    public float MieExtinctionCoef = 1;
    [Range(0.0f, 0.999f)]
    public float MieG = 0.76f;

    public float AtmosphereHeight = 80000.0f;
    public float PlanetRadius = 6371000.0f;
    public Vector4 DensityScale = new Vector4(7994.0f, 1200.0f, 0, 0);
    public Vector4 MieSct = new Vector4(2.0f, 2.0f, 2.0f, 0.0f) * 0.00001f;

    [ColorUsage(false, true)]
    public Color Color1 = new Color(1.45882356f,0.800000012f,0.203921571f,0f);
    [ColorUsage(false, true)]
    public Color Color2 = new Color(10.6806269f,2.77130342f,0f,0f);
    [ColorUsage(false, true)]
    public Color Color3 = new Color(1.33507836f,1.33507836f,1.33507836f,0f);
    
    private void Update()
    {
        LtoW_Matrix = DirectionalLight.localToWorldMatrix;
        SkyBoxMaterial.SetMatrix(LtoW, LtoW_Matrix);
        
        SkyBoxMaterial.SetFloat("_AtmosphereHeight", AtmosphereHeight);
        SkyBoxMaterial.SetFloat("_PlanetRadius", PlanetRadius);
        SkyBoxMaterial.SetVector("_DensityScaleHeight", DensityScale);

        SkyBoxMaterial.SetVector("_ScatteringM", MieSct * MieScatterCoef);
        SkyBoxMaterial.SetVector("_ExtinctionM", MieSct * MieExtinctionCoef);
        
        SkyBoxMaterial.SetColor("_IncomingLight", IncomingLight);
        SkyBoxMaterial.SetFloat("_MieG", MieG);
        SkyBoxMaterial.SetFloat("_AuroraStrength", AuroraStrength);

        if (DirectionalLight.eulerAngles.x >= 330 && DirectionalLight.eulerAngles.x <= 360)
        {
            var eulerAngles = DirectionalLight.eulerAngles;
            IncomingLight = Color.Lerp(Color1, Color2, eulerAngles.x % 330 / 30f);
            AuroraStrength = Mathf.Lerp(0.5f, 0.1f, eulerAngles.x % 330 / 30f);
            CloudMaterial.SetColor("_Color", Color.Lerp(new Color(0.18f, 0.282f, 0.396f), new Color(0.117f, 0.349f, 0.529f),  eulerAngles.x % 330 / 30f));
            CloudMaterial.SetFloat("_EdgePower", Mathf.Lerp(60, 40, eulerAngles.x % 330 / 30f));
        }
        if (DirectionalLight.eulerAngles.x <= 30 && DirectionalLight.eulerAngles.x >= 0)
        {
            var eulerAngles = DirectionalLight.eulerAngles;
            IncomingLight = Color.Lerp(Color2, Color3, eulerAngles.x / 30f);
            AuroraStrength = Mathf.Lerp(0.1f, 0, eulerAngles.x % 330 / 30f);
            CloudMaterial.SetColor("_Color", Color.Lerp(new Color(0.117f, 0.349f, 0.529f), new Color(0f, 0.486f, 0.788f), eulerAngles.x / 30f));
            CloudMaterial.SetFloat("_EdgePower", Mathf.Lerp(40, 25, eulerAngles.x / 30f));
        }
    }
}
