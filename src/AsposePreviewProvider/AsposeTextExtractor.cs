﻿using Aspose.Pdf.Text;
using System.IO;
using System.Threading.Tasks;
using SenseNet.ContentRepository.Search;
using AsposePdf = Aspose.Pdf;
using AsposeWords = Aspose.Words;
using SenseNet.ContentRepository.Search.Indexing;

namespace SenseNet.Preview.Aspose
{
    public class AsposePdfTextExtractor : TextExtractor
    {
        private readonly IIndexManager _indexManager;
        public override bool IsSlow => false;

        public AsposePdfTextExtractor(IIndexManager indexManager)
        {
            _indexManager = indexManager;
        }

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            Task.Run(() =>
            {
                AsposePreviewProvider.CheckLicense(AsposePreviewProvider.LicenseProvider.Pdf);
                var document = new AsposePdf.Document(stream);
                var textAbsorber = new TextAbsorber();
                document.Pages.Accept(textAbsorber);
                _indexManager.AddTextExtract(context.VersionId, textAbsorber.Text);
            });

            return string.Empty;
        }

    }

    public class AsposeRtfTextExtractor : TextExtractor
    {
        private readonly IIndexManager _indexManager;
        public override bool IsSlow => false;

        public AsposeRtfTextExtractor(IIndexManager indexManager)
        {
            _indexManager = indexManager;
        }

        public override string Extract(Stream stream, TextExtractorContext context)
        {
            Task.Run(() =>
            {
                AsposePreviewProvider.CheckLicense(AsposePreviewProvider.LicenseProvider.Words);

                var document = new AsposeWords.Document(stream);

                _indexManager.AddTextExtract(context.VersionId, document.GetText());
            });

            return string.Empty;
        }
    }
}
