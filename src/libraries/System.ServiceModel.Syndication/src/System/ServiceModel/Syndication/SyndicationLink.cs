// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Xml;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace System.ServiceModel.Syndication
{
    // NOTE: This class implements Clone so if you add any members, please update the copy ctor
    public class SyndicationLink : IExtensibleSyndicationObject
    {
        private Uri _baseUri;
        private ExtensibleSyndicationObject _extensions = new ExtensibleSyndicationObject();
        private long _length;
        private string _mediaType;
        private string _relationshipType;
        private string _title;
        private Uri _uri;

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
            _baseUri = null;
            _uri = uri;
            _title = title;
            _relationshipType = relationshipType;
            _mediaType = mediaType;
            _length = length;
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
            _length = source._length;
            _mediaType = source._mediaType;
            _relationshipType = source._relationshipType;
            _title = source._title;
            _baseUri = source._baseUri;
            _uri = source._uri;
            _extensions = source._extensions.Clone();
        }

        public Dictionary<XmlQualifiedName, string> AttributeExtensions
        {
            get { return _extensions.AttributeExtensions; }
        }

        public Uri BaseUri
        {
            get { return _baseUri; }
            set { _baseUri = value; }
        }

        public SyndicationElementExtensionCollection ElementExtensions
        {
            get { return _extensions.ElementExtensions; }
        }

        public long Length
        {
            get { return _length; }
            set
            {
                if (value < 0)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
                }
                _length = value;
            }
        }

        public string MediaType
        {
            get { return _mediaType; }
            set { _mediaType = value; }
        }

        public string RelationshipType
        {
            get { return _relationshipType; }
            set { _relationshipType = value; }
        }

        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        public Uri Uri
        {
            get { return _uri; }
            set { _uri = value; }
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
            if (_uri != null)
            {
                if (_uri.IsAbsoluteUri)
                {
                    return _uri;
                }
                else if (_baseUri != null)
                {
                    return new Uri(_baseUri, _uri);
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
