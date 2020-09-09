using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Fog Volume Component.
    /// </summary>
    [Serializable, VolumeComponentMenu("Fog")]
    public class Fog : VolumeComponent
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Enables the fog.")]
        public BoolParameter enabled = new BoolParameter(false);

        /// <summary>Fog color mode.</summary>
        public FogColorParameter colorMode = new FogColorParameter(FogColorMode.SkyColor);
        /// <summary>Fog color.</summary>
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Specifies the tint of the fog when using Sky Color.</summary>
        [Tooltip("Specifies the tint of the fog.")]
        public ColorParameter tint = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Maximum fog distance.</summary>
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.</summary>
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.</summary>
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        // Height Fog
        /// <summary>Height fog base height.</summary>
        public FloatParameter baseHeight = new FloatParameter(0.0f);
        /// <summary>Height fog maximum height.</summary>
        public FloatParameter maximumHeight = new FloatParameter(50.0f);
        /// <summary>Fog mean free path.</summary>
        [DisplayInfo(name = "Fog Attenuation Distance")]
        public MinFloatParameter meanFreePath = new MinFloatParameter(400.0f, 1.0f);

        // Optional Volumetric Fog
        /// <summary>Enable volumetric fog.</summary>
        [DisplayInfo(name = "Volumetric Fog")]
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        // Common Fog Parameters (Exponential/Volumetric)
        /// <summary>Fog albedo.</summary>
        public ColorParameter albedo = new ColorParameter(Color.white);
        /// <summary>Multiplier for ambient probe contribution.</summary>
        [DisplayInfo(name = "Ambient Light Probe Dimmer")]
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer. The lower the distance is, the higher the fog quality is.</summary>
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        /// <summary> Controls which version of the effect should be used. </summary>
        [Tooltip("Defines which technique is used for denoising the volumetric fog. Reprojection is very effective for static lighting, but can lead to severe ghosting for highly dynamic lighting. Gaussian is a good alternative for dynamic lighting. Using both techniques can give high quality results but will increase significantly the overall cost of the effect.")]
        public FogDenoisingModeParameter denoisingMode = new FogDenoisingModeParameter(FogDenoisingMode.Gaussian);

        // Advanced parameters
        /// <summary>Volumetric fog anisotropy.</summary>
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        /// <summary>Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.</summary>
        [Tooltip("Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);

        // Limit parameters for the fog quality
        internal const float minFogScreenResolutionPercentage = (1.0f / 16.0f) * 100;
        internal const float optimalFogScreenResolutionPercentage = (1.0f / 8.0f) * 100;
        internal const float maxFogScreenResolutionPercentage = 0.5f * 100;
        internal const int maxFogSliceCount = 512;

        /// <summary>Two modes are available for controlling the performance and quality for the volumetric fog. The balance mode allows for an intuitive, performance oriented approach. The manual mode gives access to the internal set of parameters required for the evaluation.</summary>
        [Tooltip("Two modes are available for controlling the performance and quality for the volumetric fog. The balance mode allows for an intuitive, performance oriented approach. The manual mode gives access to the internal set of parameters required for the evaluation.")]
        public FogControlParameter fogControlMode = new FogControlParameter(FogControl.Balance);
        /// <summary>Resolution of the volumetric buffer (3D texture) along the X and Y axes relative to the resolution of the frame buffer.</summary>
        [Tooltip("Resolution of the volumetric buffer, along the x-axis and y-axis, relative to the resolution of the frame buffer.")]
        public ClampedFloatParameter screenResolutionPercentage = new ClampedFloatParameter(optimalFogScreenResolutionPercentage, minFogScreenResolutionPercentage, maxFogScreenResolutionPercentage);
        /// <summary>Number of slices of the volumetric buffer (3D texture) along the camera's focal axis.</summary>
        [Tooltip("Number of slices of the volumetric buffer (3D texture) along the camera's focal axis.")]
        public ClampedIntParameter volumeSliceCount = new ClampedIntParameter(64, 1, maxFogSliceCount);
        /// <summary>The slider controls in a linear way the cost and quality of the volumetric fog. The value 0 being the least expensive and 1 the highest quality.</summary>
        [Tooltip("The slider controls in a linear way the cost and quality of the volumetric fog. The value 0 being the least expensive and 1 the highest quality.")]
        public ClampedFloatParameter volumetricFogBudget = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
        /// <summary>This parameters controls how the budget is shared between Screen (XY) and Depth (Z) resolutions. The value 0 means that all of the budget is used for the XY resolution, which will reduce aliasing, but will increase noise. On the other hand a value of 1 will put more emphasis on the Z resolution reducing the noise, but increasing the aliasing. The parameters allows linear interpolation between the two configurations.</summary>
        [Tooltip("This parameters controls how the budget is shared between Screen (XY) and Depth (Z) resolutions. The value 0 means that all of the budget is used for the XY resolution, which will reduce aliasing, but will increase the noise. On the other hand a value of 1 will put more emphasis on the Z resolution reducing the noise, but increasing the aliasing. The parameters allows linear interpolation between the two configurations.")]
        public ClampedFloatParameter resolutionDepthRatio = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>Includes or excludes non-directional light types into evaluating the volumetric fog.</summary>
        [Tooltip("Controls if non-directional light types are included in the volumetric fog. Including them will have an impact on performance.")]
        public BoolParameter directionalLightsOnly = new BoolParameter(false);

        internal static bool IsFogEnabled(HDCamera hdCamera)
        {
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) && hdCamera.volumeStack.GetComponent<Fog>().enabled.value;
        }

        internal static bool IsVolumetricFogEnabled(HDCamera hdCamera)
        {
            var fog = hdCamera.volumeStack.GetComponent<Fog>();

            bool a = fog.enableVolumetricFog.value;
            bool b = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);
            bool c = CoreUtils.IsSceneViewFogEnabled(hdCamera.camera);

            return a && b && c;
        }

        internal static bool IsPBRFogEnabled(HDCamera hdCamera)
        {
            var visualEnv = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
            // For now PBR fog (coming from the PBR sky) is disabled until we improve it
            return false;
            //return (visualEnv.skyType.value == (int)SkyType.PhysicallyBased) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering);
        }

        static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        static void UpdateShaderVariablesGlobalCBNeutralParameters(ref ShaderVariablesGlobal cb)
        {
            cb._FogEnabled = 0;
            cb._EnableVolumetricFog = 0;
            cb._HeightFogBaseScattering = Vector3.zero;
            cb._HeightFogBaseExtinction = 0.0f;
            cb._HeightFogExponents = Vector2.one;
            cb._HeightFogBaseHeight = 0.0f;
            cb._GlobalFogAnisotropy = 0.0f;
        }

        internal static void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            // TODO Handle user override
            var fogSettings = hdCamera.volumeStack.GetComponent<Fog>();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) || !fogSettings.enabled.value)
            {
                UpdateShaderVariablesGlobalCBNeutralParameters(ref cb);
            }
            else
            {
                fogSettings.UpdateShaderVariablesGlobalCBFogParameters(ref cb, hdCamera);
            }
        }

        void UpdateShaderVariablesGlobalCBFogParameters(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            bool enableVolumetrics = enableVolumetricFog.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);

            cb._FogEnabled = 1;
            cb._PBRFogEnabled = IsPBRFogEnabled(hdCamera) ? 1 : 0;
            cb._EnableVolumetricFog = enableVolumetrics ? 1 : 0;
            cb._MaxFogDistance = maxFogDistance.value;

            Color fogColor = (colorMode.value == FogColorMode.ConstantColor) ? color.value : tint.value;
            cb._FogColorMode = (float)colorMode.value;
            cb._FogColor = new Color(fogColor.r, fogColor.g, fogColor.b, 0.0f);
            cb._MipFogParameters  = new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, 0.0f);

            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            DensityVolumeEngineData data = param.ConvertToEngineData();

            cb._HeightFogBaseScattering = data.scattering;
            cb._HeightFogBaseExtinction = data.extinction;

            float crBaseHeight = baseHeight.value;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            float layerDepth = Mathf.Max(0.01f, maximumHeight.value - baseHeight.value);
            float H = ScaleHeightFromLayerDepth(layerDepth);
            cb._HeightFogExponents = new Vector2(1.0f / H, H);
            cb._HeightFogBaseHeight = crBaseHeight;
            cb._GlobalFogAnisotropy = anisotropy.value;
        }
    }

    /// <summary>
    /// Fog Color Mode.
    /// </summary>
    [GenerateHLSL]
    public enum FogColorMode
    {
        /// <summary>Fog is a constant color.</summary>
        ConstantColor,
        /// <summary>Fog uses the current sky to determine its color.</summary>
        SkyColor,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    sealed class FogTypeParameter : VolumeParameter<FogType>
    {
        public FogTypeParameter(FogType value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Fog Color parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        /// <summary>
        /// Fog Color Parameter constructor.
        /// </summary>
        /// <param name="value">Fog Color Parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// This enum defines the two modes for controlling the quality and cost of the volumetric fog.
    /// </summary>
    public enum FogControl
    {
        /// <summary>
        /// In this mode, the user changes the parameters on higher abstraction level centered around performance.
        /// </summary>
        Balance,

        /// <summary>
        /// In this mode, the user has a direct access to the internal parameters for the volumetric fog.
        /// </summary>
        Manual
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ExposureMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class FogControlParameter : VolumeParameter<FogControl>
    {
        /// <summary>
        /// Creates a new <see cref="FogControlParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FogControlParameter(FogControl value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// This enum defines which denoising algorithms should be used on the volumetric fog signal.
    /// </summary>
    public enum FogDenoisingMode
    {
        /// <summary>
        /// In this mode, the volumetric fog is not filtered.
        /// </summary>
        None = 0,
        /// <summary>
        /// This technique re-projects data from previous frames to denoise the signal. While being very effective, it can lead to severe ghosting for highly dynamic lighting.
        /// </summary>
        Reprojection = 1 << 0,
        /// <summary>
        /// Convolution to reduce the aliasing patterns that can appear on the volumetric fog.
        /// </summary>
        Gaussian = 1 << 1,
        /// <summary>
        /// In this mode, both filtering techniques are used. This can lead to high visual quality, but will increase significantly the overall cost of the effect.
        /// </summary>
        Both = Reprojection | Gaussian
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FogDenoisingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class FogDenoisingModeParameter : VolumeParameter<FogDenoisingMode>
    {
        /// <summary>
        /// Creates a new <see cref="FogDenoisingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FogDenoisingModeParameter(FogDenoisingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
