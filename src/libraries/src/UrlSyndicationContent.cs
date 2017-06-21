// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.ServiceModel.Syndication
{
    using System;
    using System.Xml;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class UrlSyndicationContent : SyndicationContent
    {
        string mediaType;
        Uri url;

        public UrlSyndicationContent(Uri url, string mediaType) : base()
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }
            this.url = url;
            this.mediaType = mediaType;
        }

        protected UrlSyndicationContent(UrlSyndicationContent source)
            : base(source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            this.url = source.url;
            this.mediaType = source.mediaType;
        }

        public override string Type
        {
            get { return this.mediaType; }
        }

        public Uri Url
        {
            get { return this.url; }
        }

        public override SyndicationContent Clone()
        {
            return new UrlSyndicationContent(this);
        }

        protected override void WriteContentsTo(XmlWriter writer)
        {
            writer.WriteAttributeString(Atom10Constants.SourceTag, string.Empty, FeedUtils.GetUriString(this.url));
        }
    }
}
