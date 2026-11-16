using UnityEngine;
using System.Collections.Generic;

public class FlashlightManager : MonoBehaviour
{
    public static FlashlightManager Instance;

    List<FlashlightVolumeController> flashlights = new List<FlashlightVolumeController>();

    void Awake()
    {
        Instance = this;
    }

    public void Register(FlashlightVolumeController f)
    {
        if (!flashlights.Contains(f))
            flashlights.Add(f);
    }

    public void Unregister(FlashlightVolumeController f)
    {
        flashlights.Remove(f);
    }

    void LateUpdate()
    {
        int count = Mathf.Min(flashlights.Count, 4);

        Shader.SetGlobalInt("_FlashlightCount", count);

        Vector4[] pos = new Vector4[4];
        Matrix4x4[] mats = new Matrix4x4[4];
        float[] intensity = new float[4];
        float[] sphereRadius = new float[4];
        float[] sphereSoftness = new float[4];

        float[] beamLength = new float[4];
        float[] beamStartW = new float[4];
        float[] beamStartH = new float[4];
        float[] beamEndW = new float[4];
        float[] beamEndH = new float[4];
        float[] beamSoft = new float[4];

        for (int i = 0; i < count; i++)
        {
            var f = flashlights[i];

            pos[i] = f.flashlightTransform.position;
            mats[i] = f.flashlightTransform.worldToLocalMatrix;

            intensity[i] = f.flashIntensity;

            sphereRadius[i] = f.sphereRadius;
            sphereSoftness[i] = f.sphereSoftness;

            beamLength[i] = f.beamLength;
            beamStartW[i] = f.beamStartHalfWidth;
            beamStartH[i] = f.beamStartHalfHeight;
            beamEndW[i] = f.beamEndHalfWidth;
            beamEndH[i] = f.beamEndHalfHeight;
            beamSoft[i] = f.beamSoftness;
        }

        Shader.SetGlobalVectorArray("_FlashlightPosWS", pos);
        Shader.SetGlobalMatrixArray("_FlashlightWorldToLocal", mats);

        Shader.SetGlobalFloatArray("_FlashIntensity", intensity);
        Shader.SetGlobalFloatArray("_SphereRadius", sphereRadius);
        Shader.SetGlobalFloatArray("_SphereSoftness", sphereSoftness);

        Shader.SetGlobalFloatArray("_BeamLength", beamLength);
        Shader.SetGlobalFloatArray("_BeamStartHalfWidth", beamStartW);
        Shader.SetGlobalFloatArray("_BeamStartHalfHeight", beamStartH);
        Shader.SetGlobalFloatArray("_BeamEndHalfWidth", beamEndW);
        Shader.SetGlobalFloatArray("_BeamEndHalfHeight", beamEndH);
        Shader.SetGlobalFloatArray("_BeamSoftness", beamSoft);
    }
}