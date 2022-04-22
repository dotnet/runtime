// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class UrlSyndicationContent : SyndicationContent
    {
        private readonly string _mediaType;

        public UrlSyndicationContent(Uri url, string mediaType) : base()
        {
            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            Url = url;
            _mediaType = mediaType;
        }

        protected UrlSyndicationContent(UrlSyndicationContent source) : base(source)
        {
            Debug.Assert(source != null, "The base constructor already checks if source is valid.");
            Url = source.Url;
            _mediaType = source._mediaType;
        }

        public override string Type => _mediaType;

        public Uri Url { get; }

        public override SyndicationContent Clone() => new UrlSyndicationContent(this);

        protected override void WriteContentsTo(XmlWriter writer)
        {
            writer.WriteAttributeString(Atom10Constants.SourceTag, string.Empty, FeedUtils.GetUriString(Url));
        }
    }
}
