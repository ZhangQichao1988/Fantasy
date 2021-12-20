using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;

using UnityEngine.Rendering.LWRP;
[ExecuteInEditMode]
public class connectSuntoVolumeFogURP : MonoBehaviour
{
    [Header("------------------------------------------------------")]
    [Header("General Volumetric Lighting and Fog Setup")]
    [Header("------------------------------------------------------")]
    public bool enableFog = true;
    public bool allowHDR = false;
    public Transform sun;
    //v1.6
    public Camera reflectCamera;

    //v1.7
    [Tooltip("Local Lights count (Max 6)")]
    [Range(1, 6)]
    public int lightCount = 3;

    //v1.5
    [Tooltip("Global volumetric lighting Intensity")]
    public float blendVolumeLighting = 0;
    [Tooltip("Volumetric lighting Sampling steps")]
    public float LightRaySamples = 8;


    [Header("------------------------------------------------------")]
    [Header("Volume Wind Noise power & speed, Fog vs Volume control")]
    [Header("------------------------------------------------------")]

    [Tooltip("Local Lights power (y-z), Wind Noise Strength(w)")]
    public Vector4 stepsControl = new Vector4(0, 0, 1, 1);
    [Tooltip("Volume lighting and Fog power (x-y), Wind Freq (z) & Speed(w)")]
    public Vector4 lightNoiseControl = new Vector4(0.6f, 0.75f, 1, 1);

    

    [Header("------------------------------------------------------")]
    [Header("Impostor Lights Array (List of Spot-Point disabled lights)")]
    [Header("------------------------------------------------------")]
    //v1.9.9.1
    public List<Light> lightsArray = new List<Light>();
    //FOG
    [Tooltip("Single Impostor Local Volume Light")]
    public Light localLightA;
    public float localLightIntensity;
    public float localLightRadius;
    [Tooltip("Single Impostor Local Fog Light")]
    public Vector4 PointL = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
    public Vector4 PointLParams = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

    [Header("------------------------------------------------------")]
    [Header("Light volumes Controls (By color and power)")]
    [Header("------------------------------------------------------")]
    //v1.9.9
    [Tooltip("Sun volume power (x), Global Local lights Power (y), Local Light A-B Power (z-w)")]
    public Vector4 lightControlA = new Vector4(1, 1, 1, 1);
    [Tooltip("Local Light C_D-E-F Powers")]
    public Vector4 lightControlB = new Vector4(1, 1, 1, 1);
    public bool controlByColor = false;
    [Tooltip("Local light to activate, put any of R-G-B component at 128")]
    public Light lightA;
    [Tooltip("Local light to activate (Disabled - to be used in next Ethereal update")]
    public Light lightB;//grab colors of the two lights to apply volume to


    [Header("------------------------------------------------------")]
    [Header("Volumetric Heigth Fog parameters")]
    [Header("------------------------------------------------------")]

    //FOG URP /////////////
    //FOG URP /////////////
    //FOG URP /////////////
    //public float blend =  0.5f;
    public Color _FogColor = Color.white / 2;
    //fog params
    [Tooltip("Select 2D (0) or 3D noise (1)")]
    public int noise3D = 1;
    public Texture2D noiseTexture;
    public float _startDistance = 30f;
    public float _fogHeight = 0.75f;
    public float _fogDensity = 1f;
    public float _cameraRoll = 0.0f;
    public Vector4 _cameraDiff = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
    public float _cameraTiltSign = 1;
    public float heightDensity = 1;
    public float noiseDensity = 1;
    public float noiseScale = 1;
    public float noiseThickness = 1;
    public Vector3 noiseSpeed = new Vector4(1f, 1f, 1f);
    public float startDistance = 1;

    [Header("------------------------------------------------------")]
    [Header("Fog Occlusion by scene Objects control")]
    [Header("------------------------------------------------------")]
    public float occlusionDrop = 1f;
    public float occlusionExp = 1f;



    [Header("------------------------------------------------------")]
    [Header("Volumetric Fog Atmospheric Scattering controls")]
    [Header("------------------------------------------------------")]

    public float luminance = 1;
    public float lumFac = 1;
    public float ScatterFac = 1;
    public float TurbFac = 1;
    public float HorizFac = 1;
    public float turbidity = 1;
    public float reileigh = 1;
    public float mieCoefficient = 1;
    public float mieDirectionalG = 1;
    public float bias = 1;
    public float contrast = 1;
    public Color TintColor = new Color(1, 1, 1, 1);
    public Vector3 TintColorK = new Vector3(0, 0, 0);
    public Vector3 TintColorL = new Vector3(0, 0, 0);
    public Vector4 Sun = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

    [Header("------------------------------------------------------")]
    [Header("Volumetric Fog Sky Blending control")]
    [Header("------------------------------------------------------")]
    public bool FogSky = true;
    public float ClearSkyFac = 1f;


    [Header("------------------------------------------------------")]
    [Header("Screen Space Sun Shafts module")]
    [Header("------------------------------------------------------")]

    //v1.9.9.2
    public bool enableSunShafts = false;//simple screen space sun shafts
    public bool _useRadialDistance = false;
    public bool _fadeToSkybox = true;
    //END FOG URP //////////////////
    //END FOG URP //////////////////
    //END FOG URP //////////////////

    //SUN SHAFTS
    public BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode screenBlendMode = BlitVolumeFogSRP.BlitSettings.ShaftsScreenBlendMode.Screen;
    //public Vector3 sunTransform = new Vector3(0f, 0f, 0f); 
    public int radialBlurIterations = 2;
    public Color sunColor = Color.white;
    public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);
    public float sunShaftBlurRadius = 2.5f;
    public float sunShaftIntensity = 1.15f;
    public float maxRadius = 0.75f;
    [Tooltip("Use Depth Texture based occlusion, Depth must be enabled on camera")]
    public bool useDepthTexture = true;
    //PostProcessProfile postProfile;

    // Start is called before the first frame update
    void Start()
    {
        //postProfile = GetComponent<PostProcessVolume>().profile;
        //v1.6
        //if (reflectCamera == null)
        //{
        //    PlanarReflectionsSM_LWRP reflectScript = GetComponent<PlanarReflectionsSM_LWRP>();
        //    if (reflectScript != null && reflectScript.outReflectionCamera != null)
        //    {
        //        reflectCamera = reflectScript.outReflectionCamera;
        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if(sun != null)
        {
            UpdateFOG();
        }

        //v1.6
        //if(reflectCamera == null)
        //{
        //    PlanarReflectionsSM_LWRP reflectScript = GetComponent<PlanarReflectionsSM_LWRP>();
        //    if(reflectScript != null && reflectScript.outReflectionCamera != null)
        //    {
        //        reflectCamera = reflectScript.outReflectionCamera;
        //    }
        //}
    }


    //FOG

    // Update is called once per frame
    //float _cameraRoll;
   // Vector4 _cameraDiff;
   // Vector4 PointLParams;
   // Vector4 Sun;
   // Vector4 PointL;
   // int _cameraTiltSign;
    void UpdateFOG()
    {
        var volFog = this; //The custom forward renderer will read variables from this script

        //var volFog = postProfile.GetSetting<VolumeFogSM_SRP>();
        if (volFog != null)
        {
            if (localLightA != null)
            {

                //volFog.sunTransform.value = sun.transform.position;
            }
            Camera cam = Camera.main;//Camera cam = Camera.current; //v1.7.1 - Solve editor flickering
            if (cam == null)
            {
                cam = Camera.main;
            }
            volFog._cameraRoll = cam.transform.eulerAngles.z;

            volFog._cameraDiff = cam.transform.eulerAngles;// - prevRot;

            if (cam.transform.eulerAngles.y > 360)
            {
                volFog._cameraDiff.y = cam.transform.eulerAngles.y % 360;
            }
            if (cam.transform.eulerAngles.y > 180)
            {
                volFog._cameraDiff.y = -(360 - volFog._cameraDiff.y);
            }

            //slipt in 90 degs, 90 to 180 mapped to 90 to zero
            //volFog._cameraDiff.value.w = 1;
            if (volFog._cameraDiff.y > 90 && volFog._cameraDiff.y < 180)
            {
                volFog._cameraDiff.y = 180 - volFog._cameraDiff.y;
                volFog._cameraDiff.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w ,- 1, Time.deltaTime * 20);
            }
            else if (volFog._cameraDiff.y < -90 && volFog._cameraDiff.y > -180)
            {
                volFog._cameraDiff.y = -180 - volFog._cameraDiff.y;
                volFog._cameraDiff.w = -1;
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, -1, Time.deltaTime * 20);
                //Debug.Log("dde");
            }
            else
            {
                //volFog._cameraDiff.value.w = Mathf.Lerp(volFog._cameraDiff.value.w, 1, Time.deltaTime * 20);
                volFog._cameraDiff.w = 1;
            }

            //vertical fix
            if (cam.transform.eulerAngles.x > 360)
            {
                volFog._cameraDiff.x = cam.transform.eulerAngles.x % 360;
            }
            if (cam.transform.eulerAngles.x > 180)
            {
                volFog._cameraDiff.x = 360 - volFog._cameraDiff.x;
            }
            //Debug.Log(cam.transform.eulerAngles.x);
            if (cam.transform.eulerAngles.x > 0 && cam.transform.eulerAngles.x < 180)
            {
                volFog._cameraTiltSign = 1;
            }
            else
            {
                // Debug.Log(cam.transform.eulerAngles.x);
                volFog._cameraTiltSign = -1;
            }
            if (sun != null)
            {
                Vector3 sunDir = sun.transform.forward;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.y, Vector3.up) * -sunDir;
                sunDir = Quaternion.AngleAxis(cam.transform.eulerAngles.x, Vector3.left) * sunDir;
                sunDir = Quaternion.AngleAxis(-cam.transform.eulerAngles.z, Vector3.forward) * sunDir;
                // volFog.Sun.value = -new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
                volFog.Sun = new Vector4(sunDir.x, sunDir.y, sunDir.z, 1);
                //volFog.sun.position = new Vector3(sunDir.x, sunDir.y, sunDir.z);////// vector 4 to vector3
            }
            else
            {
                volFog.Sun = new Vector4(15, 0, 1, 1);
            }
            if (localLightA != null)
            {
                volFog.PointL = new Vector4(localLightA.transform.position.x, localLightA.transform.position.y, localLightA.transform.position.z, localLightIntensity);
                volFog.PointLParams = new Vector4(localLightA.color.r, localLightA.color.g, localLightA.color.b, localLightRadius);
            }
            //Debug.Log(volFog._cameraDiff.value);
            //prevRot = cam.transform.eulerAngles;
        }
    }

    //END FOG

}
