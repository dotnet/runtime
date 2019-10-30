// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace System.Xml.Linq
{
    class StreamLoader
    {
        class StreamIterator : IEnumerable<XElement>
        {
            private StreamLoader _loader;
            private int _index;
            internal XElement source;

            public StreamIterator(StreamLoader loader, int index, XElement source)
            {
                _loader = loader;
                _index = index;
                this.source = source;
            }

            public IEnumerator<XElement> GetEnumerator()
            {
                if (_loader._index != _index - 1 || (_index - 1 >= 0 && _loader._iterators[_index - 1] != null && _loader._iterators[_index - 1].source != source)) yield break;
                int depth = _loader._baseDepth + _index + 1;
                XName name = _loader._streamNames[_index];
                XName streamName = _loader._streamNames.Length > _index + 1 ? _loader._streamNames[_index + 1] : null;
                if (_loader.SkipContentUntil(depth, name))
                {
                    _loader._iterators[_index] = this;
                    _loader._index = _index;
                    do
                    {
                        source = new XElement(name);
                        _loader.ReadElementUntil(source, streamName);
                        if (streamName != null)
                        {
                            source.AddAnnotation(_loader);
                        }
                        yield return source;
                        if (_loader._iterators[_index] != this) yield break;
                        if (_loader._index != _index)
                        {
                            for (int i = _index + 1; i <= _loader._index; i++)
                            {
                                _loader._iterators[i] = null;
                            }
                            _loader._index = _index;
                        }
                    } while (_loader.SkipContentUntil(depth, name));
                    _loader._iterators[_index] = null;
                    _loader._index = _index - 1;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return (IEnumerator)GetEnumerator();
            }
        }

        private XmlReader _reader;
        private int _baseDepth;

        private XName[] _streamNames;
        private StreamIterator[] _iterators;
        private int _index;

        public StreamLoader(XmlReader reader, XName[] streamNames)
        {
            _reader = reader;
            _baseDepth = reader.Depth;
            _streamNames = streamNames;
            _iterators = new StreamIterator[streamNames.Length];
            _index = -1;
        }

        public IEnumerable<XElement> Stream(XElement e)
        {
            if (_index >= 0 && _iterators[_index].source != e) return XElement.EmptySequence;
            return new StreamIterator(this, _index + 1, e);
        }

        public void ReadElementUntil(XElement source, XName match)
        {
            if (_reader.ReadState != ReadState.Interactive) throw new InvalidOperationException("The reader state should be Interactive.");
            if (source.Name != XNamespace.Get(_reader.NamespaceURI).GetName(_reader.LocalName)) throw new InvalidOperationException(string.Format("The reader should be on an element with the name '{0}'.", source.Name));
            if (_reader.MoveToFirstAttribute())
            {
                do
                {
                    XNamespace ns = _reader.Prefix.Length == 0 ? XNamespace.None : XNamespace.Get(_reader.NamespaceURI);
                    source.Add(new XAttribute(ns.GetName(_reader.LocalName), _reader.Value));
                } while (_reader.MoveToNextAttribute());
                _reader.MoveToElement();
            }
            if (!_reader.IsEmptyElement)
            {
                _reader.Read();
                if (match != null)
                {
                    if (ReadPrologUntil(source, match)) return;
                }
                else
                {
                    ReadContent(source);
                }
            }
            _reader.Read();
        }

        void ReadContent(XElement source)
        {
            if (_reader.ReadState != ReadState.Interactive) throw new InvalidOperationException("The reader state should be Interactive.");
            if (_reader.NodeType != XmlNodeType.EndElement)
            {
                do
                {
                    source.Add(XNode.ReadFrom(_reader));
                } while (_reader.NodeType != XmlNodeType.EndElement);
            }
            else
            {
                source.Add(string.Empty);
            }
        }

        bool ReadPrologUntil(XElement source, XName match)
        {
            if (_reader.ReadState != ReadState.Interactive) throw new InvalidOperationException("The reader state should be Interactive.");
            do
            {
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Element:
                        XName name = XNamespace.Get(_reader.NamespaceURI).GetName(_reader.LocalName);
                        if (name == match) return true;
                        XElement e = new XElement(name);
                        if (_reader.MoveToFirstAttribute())
                        {
                            do
                            {
                                XNamespace ns = _reader.Prefix.Length == 0 ? XNamespace.None : XNamespace.Get(_reader.NamespaceURI);
                                e.Add(new XAttribute(ns.GetName(_reader.LocalName), _reader.Value));
                            } while (_reader.MoveToNextAttribute());
                            _reader.MoveToElement();
                        }
                        source.Add(e);
                        if (!_reader.IsEmptyElement)
                        {
                            _reader.Read();
                            ReadContent(e);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        return false;
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Comment:
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.DocumentType:
                        break;
                    case XmlNodeType.EntityReference:
                        if (!_reader.CanResolveEntity) throw new InvalidOperationException("The reader cannot resolve entity references.");
                        _reader.ResolveEntity();
                        break;
                    case XmlNodeType.EndEntity:
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("The reader should not be on a node of type '{0}'.", _reader.NodeType));
                }
            } while (_reader.Read());
            return false;
        }

        bool SkipContentUntil(int depth, XName match)
        {
            if (_reader.ReadState != ReadState.Interactive) return false;
            do
            {
                int d = _reader.Depth;
                if (d == depth)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        XName name = XNamespace.Get(_reader.NamespaceURI).GetName(_reader.LocalName);
                        if (name == match) return true;
                    }
                }
                else if (d < depth)
                {
                    break;
                }
            } while (_reader.Read());
            return false;
        }
    }

    public static class StreamExtensions
    {
        public static XElement LoadStream(string uri, XName rootName, params XName[] streamNames)
        {
            return LoadStream(XmlReader.Create(uri, GetXmlReaderSettings()), rootName, streamNames);
        }

        public static XElement LoadStream(TextReader textReader, XName rootName, params XName[] streamNames)
        {
            return LoadStream(XmlReader.Create(textReader, GetXmlReaderSettings()), rootName, streamNames);
        }

        public static XElement LoadStream(XmlReader reader, XName rootName, params XName[] streamNames)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (rootName == null) throw new ArgumentNullException(nameof(rootName));
            if (streamNames == null) throw new ArgumentNullException(nameof(streamNames));
            for (int i = 0; i < streamNames.Length; i++)
            {
                if (streamNames[i] == null) throw new ArgumentNullException("streamNames[" + i + "]");
            }
            if (reader.MoveToContent() != XmlNodeType.Element) throw new InvalidOperationException(string.Format("The reader should be on a node of type '{0}'.", XmlNodeType.Element));
            XElement source = new XElement(rootName);
            StreamLoader loader = new StreamLoader(reader, streamNames);
            XName streamName = streamNames.Length > 0 ? streamNames[0] : null;
            loader.ReadElementUntil(source, streamName);
            if (streamName != null)
            {
                source.AddAnnotation(loader);
            }
            return source;
        }

        public static IEnumerable<XElement> Stream(this XElement source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            StreamLoader loader = source.Annotation<StreamLoader>();
            if (loader == null) throw new InvalidOperationException("No stream associated with the element.");
            return loader.Stream(source);
        }

        public static IEnumerable<XElement> Stream(this IEnumerable<XElement> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return Enumerable.SelectMany(source, e => e.Stream());
        }

        static XmlReaderSettings GetXmlReaderSettings()
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.DtdProcessing = DtdProcessing.Ignore;
            readerSettings.IgnoreWhitespace = true;
            return readerSettings;
        }
    }
}
