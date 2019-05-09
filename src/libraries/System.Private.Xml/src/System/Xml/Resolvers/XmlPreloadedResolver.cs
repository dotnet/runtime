// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using System.Net;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Xml.Resolvers
{
    // 
    // XmlPreloadedResolver is an XmlResolver that which can be pre-loaded with data.
    // By default it contains well-known DTDs for XHTML 1.0 and RSS 0.91. 
    // Custom mappings of URIs to data can be added with the Add method.
    //
    public partial class XmlPreloadedResolver : XmlResolver
    {
        //
        // PreloadedData class
        //
        private abstract class PreloadedData
        {
            // Returns preloaded data as Stream; Stream must always be supported
            internal abstract Stream AsStream();

            // Returns preloaded data as TextReader, or throws when not supported
            internal virtual TextReader AsTextReader()
            {
                throw new XmlException(SR.Xml_UnsupportedClass);
            }

            // Returns true for types that are supported for this preloaded data; Stream must always be supported
            internal virtual bool SupportsType(Type type)
            {
                if (type == null || type == typeof(Stream))
                {
                    return true;
                }
                return false;
            }
        };

        //
        // XmlKnownDtdData class
        //
        private class XmlKnownDtdData : PreloadedData
        {
            internal string publicId;
            internal string systemId;
            private string _resourceName;

            internal XmlKnownDtdData(string publicId, string systemId, string resourceName)
            {
                this.publicId = publicId;
                this.systemId = systemId;
                _resourceName = resourceName;
            }

            internal override Stream AsStream()
            {
                Assembly asm = GetType().Assembly;
                return asm.GetManifestResourceStream(_resourceName);
            }
        }

        private class ByteArrayChunk : PreloadedData
        {
            private byte[] _array;
            private int _offset;
            private int _length;

            internal ByteArrayChunk(byte[] array)
                : this(array, 0, array.Length)
            {
            }

            internal ByteArrayChunk(byte[] array, int offset, int length)
            {
                _array = array;
                _offset = offset;
                _length = length;
            }

            internal override Stream AsStream()
            {
                return new MemoryStream(_array, _offset, _length);
            }
        }

        private class StringData : PreloadedData
        {
            private string _str;

            internal StringData(string str)
            {
                _str = str;
            }

            internal override Stream AsStream()
            {
                return new MemoryStream(Encoding.Unicode.GetBytes(_str));
            }

            internal override TextReader AsTextReader()
            {
                return new StringReader(_str);
            }

            internal override bool SupportsType(Type type)
            {
                if (type == typeof(TextReader))
                {
                    return true;
                }
                return base.SupportsType(type);
            }
        }

        //
        // Fields
        //
        private XmlResolver _fallbackResolver;
        private Dictionary<Uri, PreloadedData> _mappings;
        private XmlKnownDtds _preloadedDtds;

        //
        // Static/constant fiels
        //
        private static XmlKnownDtdData[] s_xhtml10_Dtd = new XmlKnownDtdData[] {
            new XmlKnownDtdData( "-//W3C//DTD XHTML 1.0 Strict//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd", "xhtml1-strict.dtd" ),
            new XmlKnownDtdData( "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", "xhtml1-transitional.dtd" ),
            new XmlKnownDtdData( "-//W3C//DTD XHTML 1.0 Frameset//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd", "xhtml1-frameset.dtd" ),
            new XmlKnownDtdData( "-//W3C//ENTITIES Latin 1 for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent", "xhtml-lat1.ent" ),
            new XmlKnownDtdData( "-//W3C//ENTITIES Symbols for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent", "xhtml-symbol.ent" ),
            new XmlKnownDtdData( "-//W3C//ENTITIES Special for XHTML//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent", "xhtml-special.ent" ),
        };

        private static XmlKnownDtdData[] s_rss091_Dtd = new XmlKnownDtdData[] {
            new XmlKnownDtdData( "-//Netscape Communications//DTD RSS 0.91//EN", "http://my.netscape.com/publish/formats/rss-0.91.dtd", "rss-0.91.dtd" ),
        };

        //
        // Constructors
        //
        public XmlPreloadedResolver()
            : this(null)
        {
        }

        public XmlPreloadedResolver(XmlKnownDtds preloadedDtds)
            : this(null, preloadedDtds, null)
        {
        }

        public XmlPreloadedResolver(XmlResolver fallbackResolver)
            : this(fallbackResolver, XmlKnownDtds.All, null)
        {
        }

        public XmlPreloadedResolver(XmlResolver fallbackResolver, XmlKnownDtds preloadedDtds)
            : this(fallbackResolver, preloadedDtds, null)
        {
        }

        public XmlPreloadedResolver(XmlResolver fallbackResolver, XmlKnownDtds preloadedDtds, IEqualityComparer<Uri> uriComparer)
        {
            _fallbackResolver = fallbackResolver;
            _mappings = new Dictionary<Uri, PreloadedData>(16, uriComparer);
            _preloadedDtds = preloadedDtds;

            // load known DTDs
            if (preloadedDtds != 0)
            {
                if ((preloadedDtds & XmlKnownDtds.Xhtml10) != 0)
                {
                    AddKnownDtd(s_xhtml10_Dtd);
                }
                if ((preloadedDtds & XmlKnownDtds.Rss091) != 0)
                {
                    AddKnownDtd(s_rss091_Dtd);
                }
            }
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            // 1) special-case well-known public IDs
            // 2) To make FxCop happy we need to use StartsWith() overload that takes StringComparison ->
            //   .StartsWith(string) is equal to .StartsWith(string, StringComparison.CurrentCulture);
            if (relativeUri != null && relativeUri.StartsWith("-//", StringComparison.CurrentCulture))
            {
                // 1) XHTML 1.0 public IDs
                // 2) To make FxCop happy we need to use StartsWith() overload that takes StringComparison ->
                //   .StartsWith(string) is equal to .StartsWith(string, StringComparison.CurrentCulture);
                if ((_preloadedDtds & XmlKnownDtds.Xhtml10) != 0 && relativeUri.StartsWith("-//W3C//", StringComparison.CurrentCulture))
                {
                    for (int i = 0; i < s_xhtml10_Dtd.Length; i++)
                    {
                        if (relativeUri == s_xhtml10_Dtd[i].publicId)
                        {
                            return new Uri(relativeUri, UriKind.Relative);
                        }
                    }
                }
                // RSS 0.91 public IDs
                if ((_preloadedDtds & XmlKnownDtds.Rss091) != 0)
                {
                    Debug.Assert(s_rss091_Dtd.Length == 1);
                    if (relativeUri == s_rss091_Dtd[0].publicId)
                    {
                        return new Uri(relativeUri, UriKind.Relative);
                    }
                }
            }
            return base.ResolveUri(baseUri, relativeUri);
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException(nameof(absoluteUri));
            }

            PreloadedData data;
            if (!_mappings.TryGetValue(absoluteUri, out data))
            {
                if (_fallbackResolver != null)
                {
                    return _fallbackResolver.GetEntity(absoluteUri, role, ofObjectToReturn);
                }
                throw new XmlException(SR.Format(SR.Xml_CannotResolveUrl, absoluteUri));
            }

            if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
            {
                return data.AsStream();
            }
            else if (ofObjectToReturn == typeof(TextReader))
            {
                return data.AsTextReader();
            }
            else
            {
                throw new XmlException(SR.Xml_UnsupportedClass);
            }
        }

        public override ICredentials Credentials
        {
            set
            {
                if (_fallbackResolver != null)
                {
                    _fallbackResolver.Credentials = value;
                }
            }
        }

        public override bool SupportsType(Uri absoluteUri, Type type)
        {
            if (absoluteUri == null)
            {
                throw new ArgumentNullException(nameof(absoluteUri));
            }

            PreloadedData data;
            if (!_mappings.TryGetValue(absoluteUri, out data))
            {
                if (_fallbackResolver != null)
                {
                    return _fallbackResolver.SupportsType(absoluteUri, type);
                }
                return base.SupportsType(absoluteUri, type);
            }

            return data.SupportsType(type);
        }

        public void Add(Uri uri, byte[] value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            Add(uri, new ByteArrayChunk(value, 0, value.Length));
        }

        public void Add(Uri uri, byte[] value, int offset, int count)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (value.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Add(uri, new ByteArrayChunk(value, offset, count));
        }

        public void Add(Uri uri, Stream value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value.CanSeek)
            {
                // stream of known length -> allocate the byte array and read all data into it
                int size = checked((int)value.Length);
                byte[] bytes = new byte[size];
                value.Read(bytes, 0, size);
                Add(uri, new ByteArrayChunk(bytes));
            }
            else
            {
                // stream of unknown length -> read into memory stream and then get internal the byte array
                MemoryStream ms = new MemoryStream();
                byte[] buffer = new byte[4096];
                int read;
                while ((read = value.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                int size = checked((int)ms.Position);
                byte[] bytes = new byte[size];
                Array.Copy(ms.ToArray(), 0, bytes, 0, size);
                Add(uri, new ByteArrayChunk(bytes));
            }
        }

        public void Add(Uri uri, string value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            Add(uri, new StringData(value));
        }

        public IEnumerable<Uri> PreloadedUris
        {
            get
            {
                // read-only collection of keys
                return _mappings.Keys;
            }
        }

        public void Remove(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            _mappings.Remove(uri);
        }

        //
        // Private implementation methods
        //
        private void Add(Uri uri, PreloadedData data)
        {
            Debug.Assert(uri != null);

            // override if exists
            if (_mappings.ContainsKey(uri))
            {
                _mappings[uri] = data;
            }
            else
            {
                _mappings.Add(uri, data);
            }
        }

        private void AddKnownDtd(XmlKnownDtdData[] dtdSet)
        {
            for (int i = 0; i < dtdSet.Length; i++)
            {
                XmlKnownDtdData dtdInfo = dtdSet[i];
                _mappings.Add(new Uri(dtdInfo.publicId, UriKind.RelativeOrAbsolute), dtdInfo);
                _mappings.Add(new Uri(dtdInfo.systemId, UriKind.RelativeOrAbsolute), dtdInfo);
            }
        }
    }
}
