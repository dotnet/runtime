// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationLink : IExtensibleSyndicationObject
    {
        private ExtensibleSyndicationObject _extensions;
        private long _length;

        public SyndicationLink(Uri uri) : this(uri, null, null, null, 0)
        {
        }

        public SyndicationLink(Uri uri, string relationshipType, string title, string mediaType, long length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            BaseUri = null;
            Uri = uri;
            Title = title;
            RelationshipType = relationshipType;
            MediaType = mediaType;
            _length = length;
        }

        public SyndicationLink() : this(null, null, null, null, 0)
        {
        }

        protected SyndicationLink(SyndicationLink source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _length = source._length;
            MediaType = source.MediaType;
            RelationshipType = source.RelationshipType;
            Title = source.Title;
            BaseUri = source.BaseUri;
            Uri = source.Uri;
            _extensions = source._extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions => _extensions.AttributeExtensions;

        public Uri BaseUri { get; set; }

        public SyndicationElementExtensionCollection ElementExtensions => _extensions.ElementExtensions;

        public long Length
        {
            get => _length;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _length = value;
            }
        }

        public string MediaType { get; set; }

        public string RelationshipType { get; set; }

        public string Title { get; set; }

        public Uri Uri { get; set; }

        public static SyndicationLink CreateAlternateLink(Uri uri)
        {
            return CreateAlternateLink(uri, null);
        }

        public static SyndicationLink CreateAlternateLink(Uri uri, string mediaType)
        {
            return new SyndicationLink(uri, Atom10Constants.AlternateTag, null, mediaType, 0);
        }

        public static SyndicationLink CreateMediaEnclosureLink(Uri uri, string mediaType, long length)
        {
            return new SyndicationLink(uri, Rss20Constants.EnclosureTag, null, mediaType, length);
        }

        public static SyndicationLink CreateSelfLink(Uri uri)
        {
            return CreateSelfLink(uri, null);
        }

        public static SyndicationLink CreateSelfLink(Uri uri, string mediaType)
        {
            return new SyndicationLink(uri, Atom10Constants.SelfTag, null, mediaType, 0);
        }

        public virtual SyndicationLink Clone() => new SyndicationLink(this);

        public Uri GetAbsoluteUri()
        {
            if (Uri != null)
            {
                if (Uri.IsAbsoluteUri)
                {
                    return Uri;
                }
                else if (BaseUri != null)
                {
                    return new Uri(BaseUri, Uri);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        protected internal virtual bool TryParseAttribute(string name, string ns, string value, string version)
        {
            return false;
        }

        protected internal virtual bool TryParseElement(XmlReader reader, string version)
        {
            return false;
        }

        protected internal virtual void WriteAttributeExtensions(XmlWriter writer, string version)
        {
            _extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            _extensions.WriteElementExtensions(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            _extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            _extensions.LoadElementExtensions(buffer);
        }
    }
}
