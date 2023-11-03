using SenseNet.Client;
using SenseNet.Tools.Configuration;

namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    #region Helper classes
    public class UploadConfig
    {
        /// <summary>
        /// Upload chunk size in bytes.
        /// </summary>
        public int ChunkSize { get; set; } = 10485760;
    }
    public class ImageGenerationConfig
    {
        public int PreviewResolution { get; set; } = 300;
        public bool CheckLicense { get; set; } = true;
    }

    public class EnvironmentConfig
    {
        public bool IsDevelopment { get; set; }
    }

    #endregion

    [OptionsClass(sectionName: "sensenet:AsposePreviewGenerator")]
    public class AsposePreviewGeneratorOptions
    {
        public UploadConfig Upload { get; } = new();
        public ImageGenerationConfig ImageGeneration { get; } = new();
        public EnvironmentConfig Environment { get; } = new();
        public RepositoryOptions[] Applications { get; set; } = Array.Empty<RepositoryOptions>();

        internal static AsposePreviewGeneratorOptions Instance { get; private set; }

        public static void Initialize(AsposePreviewGeneratorOptions options)
        {
            Logger.WriteTrace($"Configuration: chunk size: {options.Upload.ChunkSize}, " +
                              $"Preview resolution: {options.ImageGeneration.PreviewResolution}, " +
                              $"Dev environment: {options.Environment.IsDevelopment}, " +
                              $"Check license: {options.ImageGeneration.CheckLicense}");
            
            Instance = options;
        }
    }
}
