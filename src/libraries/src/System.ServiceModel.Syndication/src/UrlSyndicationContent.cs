// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Syndication
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Xml;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class UrlSyndicationContent : SyndicationContent
    {
        private string _mediaType;
        private Uri _url;

        public UrlSyndicationContent(Uri url, string mediaType) : base()
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }
            _url = url;
            _mediaType = mediaType;
        }

        protected UrlSyndicationContent(UrlSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            _url = source._url;
            _mediaType = source._mediaType;
        }

        public override string Type
        {
            get { return _mediaType; }
        }

        public Uri Url
        {
            get { return _url; }
        }

        public override SyndicationContent Clone()
        {
            return new UrlSyndicationContent(this);
        }

        protected override void WriteContentsTo(XmlWriter writer)
        {
            writer.WriteAttributeString(Atom10Constants.SourceTag, string.Empty, FeedUtils.GetUriString(_url));
        }
    }
}
