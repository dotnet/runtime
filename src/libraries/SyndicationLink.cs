//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace System.ServiceModel.Syndication
{
    using System.Collections.Generic;
    using System.Xml;
    using System.Collections.ObjectModel;
    using System.Xml.Serialization;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    [TypeForwardedFrom("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public class SyndicationLink : IExtensibleSyndicationObject
    {
        Uri baseUri;
        ExtensibleSyndicationObject extensions = new ExtensibleSyndicationObject();
        long length;
        string mediaType;
        string relationshipType;
        string title;
        Uri uri;

        public SyndicationLink(Uri uri)
            : this(uri, null, null, null, 0)
        {
        }

        public SyndicationLink(Uri uri, string relationshipType, string title, string mediaType, long length)
        {
            if (length < 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("length"));
            }
            this.baseUri = null;
            this.uri = uri;
            this.title = title;
            this.relationshipType = relationshipType;
            this.mediaType = mediaType;
            this.length = length;
        }

        public SyndicationLink()
            : this(null, null, null, null, 0)
        {
        }

        protected SyndicationLink(SyndicationLink source)
        {
            if (source == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("source");
            }
            this.length = source.length;
            this.mediaType = source.mediaType;
            this.relationshipType = source.relationshipType;
            this.title = source.title;
            this.baseUri = source.baseUri;
            this.uri = source.uri;
            this.extensions = source.extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return this.extensions.AttributeExtensions; }
        }

        public Uri BaseUri
        {
            get { return this.baseUri; }
            set { this.baseUri = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return this.extensions.ElementExtensions; }
        }

        public long Length
        {
            get { return this.length; }
            set
            {
                if (value < 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
                }
                this.length = value;
            }
        }

        public string MediaType
        {
            get { return mediaType; }
            set { mediaType = value; }
        }

        public string RelationshipType
        {
            get { return relationshipType; }
            set { relationshipType = value; }
        }

        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        public Uri Uri
        {
            get { return uri; }
            set { this.uri = value; }
        }

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

        public virtual SyndicationLink Clone()
        {
            return new SyndicationLink(this);
        }

        public Uri GetAbsoluteUri()
        {
            if (this.uri != null)
            {
                if (this.uri.IsAbsoluteUri)
                {
                    return this.uri;
                }
                else if (this.baseUri != null)
                {
                    return new Uri(this.baseUri, this.uri);
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
            this.extensions.WriteAttributeExtensions(writer);
        }

        protected internal virtual void WriteElementExtensions(XmlWriter writer, string version)
        {
            this.extensions.WriteElementExtensions(writer);
        }

        internal void LoadElementExtensions(XmlReader readerOverUnparsedExtensions, int maxExtensionSize)
        {
            this.extensions.LoadElementExtensions(readerOverUnparsedExtensions, maxExtensionSize);
        }

        internal void LoadElementExtensions(XmlBuffer buffer)
        {
            this.extensions.LoadElementExtensions(buffer);
        }
    }
}
