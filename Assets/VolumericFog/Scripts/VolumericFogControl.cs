using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VolumericFogControl : MonoBehaviour
{
    struct Vector3i
    {
        public int x, y, z;
        public Vector3i(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    public Transform transFog = null;

    Material material;
    RenderTexture m_VolumeScatter;
    Vector3i m_VolumeResolution = new Vector3i(160, 90, 128);


    private void Awake()
    {
        material = GetComponent<MeshRenderer>().sharedMaterial;
    }
    void InitVolume(ref RenderTexture volume)
    {
        if (volume)
            return;

        volume = new RenderTexture(m_VolumeResolution.x, m_VolumeResolution.y, 0, RenderTextureFormat.ARGBHalf);
        volume.volumeDepth = m_VolumeResolution.z;
        volume.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        volume.enableRandomWrite = true;
        volume.Create();
    }
    void OnDestroy()
    {
        Cleanup();
    }

    void OnDisable()
    {
        Cleanup();
    }
    void Cleanup()
    {
        DestroyImmediate(m_VolumeScatter);
        m_VolumeScatter = null;
    }    // Update is called once per frame
    void Update()
    {
        if (transFog == null || material == null) { return; }
        Vector3 pos = transFog.transform.position;
        material.SetVector("_boundsMin", transFog.position - transFog.localScale / 2);
        material.SetVector("_boundsMax", transFog.position + transFog.localScale / 2);

    }
}
