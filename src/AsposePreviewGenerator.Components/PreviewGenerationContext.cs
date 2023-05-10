namespace SenseNet.Preview.Aspose.AsposePreviewGenerator
{
    public class PreviewGenerationContext : IPreviewGenerationContext
    {
        public int ContentId { get; }
        public int PreviewsFolderId { get; }
        public int StartIndex { get; }
        public int MaxPreviewCount { get; }
        public int PreviewResolution { get; }
        public string Version { get; }
        
        internal PreviewGenerationContext(int contentId, int previewsFolderId, int startIndex, int maxPreviewCount, int previewResolution, string version)
        {
            ContentId = contentId;
            PreviewsFolderId = previewsFolderId;
            StartIndex = startIndex;
            MaxPreviewCount = maxPreviewCount;
            PreviewResolution = previewResolution;
            Version = version;
        }

        public Task SetPageCountAsync(int pageCount, CancellationToken cancellationToken)
        {
            return PreviewGenerator.SetPageCountAsync(pageCount);
        }
        public void SetIndexes(int pageCount, out int firstIndex, out int lastIndex)
        {
            PreviewGenerator.SetIndexes(StartIndex, pageCount, out firstIndex, out lastIndex, MaxPreviewCount);
        }
        public Task SavePreviewAndThumbnailAsync(Stream imgStream, int page, CancellationToken cancellationToken)
        {
            return PreviewGenerator.SavePreviewAndThumbnailAsync(imgStream, page, PreviewsFolderId, cancellationToken);
        }
        public Task SaveEmptyPreviewAsync(int page, CancellationToken cancellationToken)
        {
            return PreviewGenerator.SaveEmptyPreviewAsync(page, PreviewsFolderId, cancellationToken);
        }
        public Task SaveImageAsync(Stream imgStream, int page, CancellationToken cancellationToken)
        {
            return PreviewGenerator.SaveImageAsync(imgStream, page, PreviewsFolderId, cancellationToken);
        }

        public void LogInfo(int page, string message)
        {
            Logger.WriteInfo(ContentId, page, message);
        }
        public void LogWarning(int page, string message)
        {
            Logger.WriteWarning(ContentId, page, message);
        }
        public void LogError(int page, string message = null, Exception ex = null)
        {
            Logger.WriteError(ContentId, page, message, ex, StartIndex, Version);
        }
    }
}
