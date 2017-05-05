// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    public class ReaderWriterFactory
    {
        public enum ReaderWriterType
        {
            Binary,
            Text,
            WebData,
            WrappedWebData
        };

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary, OnXmlDictionaryReaderClose onClose)
        {
            XmlReader result = null;
            switch (rwType)
            {
                case ReaderWriterType.Binary:
                    result = XmlDictionaryReader.CreateBinaryReader(buffer, 0, buffer.Length, dictionary, quotas, null, onClose);
                    break;
                case ReaderWriterType.Text:
                    result = XmlDictionaryReader.CreateTextReader(buffer, 0, buffer.Length, encoding, quotas, onClose);
                    break;
                case ReaderWriterType.WebData:
                    if (quotas != XmlDictionaryReaderQuotas.Max)
                    {
                        throw new InvalidOperationException("Cannot enforce quotas on the Webdata readers!");
                    }

                    if (onClose != null)
                    {
                        throw new InvalidOperationException("Webdata readers do not support the OnClose callback!");
                    }

                    XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                    result = XmlReader.Create(new MemoryStream(buffer), new XmlReaderSettings(), context);
                    break;
                case ReaderWriterType.WrappedWebData:
                    if (quotas != XmlDictionaryReaderQuotas.Max)
                    {
                        throw new InvalidOperationException("There is no overload to create the webdata readers with quotas!");
                    }

                    if (onClose != null)
                    {
                        throw new InvalidOperationException("Webdata readers do not support the OnClose callback!");
                    }

                    XmlParserContext context2 = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                    result = XmlReader.Create(new MemoryStream(buffer), new XmlReaderSettings(), context2);
                    result = XmlDictionaryReader.CreateDictionaryReader(result);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rwType));
            }

            return result;
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary, OnXmlDictionaryReaderClose onClose)
        {
            XmlReader result = null;
            switch (rwType)
            {
                case ReaderWriterType.Binary:
                    result = XmlDictionaryReader.CreateBinaryReader(stream, dictionary, quotas, null, onClose);
                    break;
                case ReaderWriterType.Text:
                    result = XmlDictionaryReader.CreateTextReader(stream, encoding, quotas, onClose);
                    break;
                case ReaderWriterType.WebData:
                case ReaderWriterType.WrappedWebData:
                    if (quotas != XmlDictionaryReaderQuotas.Max)
                    {
                        throw new InvalidOperationException("Webdata readers do not support quotas!");
                    }

                    if (onClose != null)
                    {
                        throw new InvalidOperationException("Webdata readers do not support the OnClose callback!");
                    }

                    XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                    result = XmlReader.Create(stream, new XmlReaderSettings(), context);
                    if (rwType == ReaderWriterType.WrappedWebData)
                    {
                        result = XmlDictionaryReader.CreateDictionaryReader(result);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rwType));
            }
            return result;
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary)
        {
            return CreateXmlReader(rwType, buffer, encoding, quotas, dictionary, null);
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary)
        {
            return CreateXmlReader(rwType, stream, encoding, quotas, dictionary, null);
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas)
        {
            return CreateXmlReader(rwType, buffer, encoding, quotas, null);
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas)
        {
            return CreateXmlReader(rwType, stream, encoding, quotas, null);
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding)
        {
            return CreateXmlReader(rwType, buffer, encoding, XmlDictionaryReaderQuotas.Max);
        }

        public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding)
        {
            return CreateXmlReader(rwType, stream, encoding, XmlDictionaryReaderQuotas.Max);
        }

        public static XmlWriter CreateXmlWriter(ReaderWriterType rwType, Stream stream, Encoding encoding)
        {
            return CreateXmlWriter(rwType, stream, encoding, null);
        }

        public static XmlWriter CreateXmlWriter(ReaderWriterType rwType, Stream stream, Encoding encoding, IXmlDictionary dictionary)
        {
            XmlWriter result = null;
            switch (rwType)
            {
                case ReaderWriterType.Binary:
                    result = XmlDictionaryWriter.CreateBinaryWriter(stream, dictionary);
                    break;
                case ReaderWriterType.Text:
                    result = XmlDictionaryWriter.CreateTextWriter(stream, encoding);
                    break;
                case ReaderWriterType.WebData:
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Encoding = encoding;
                    result = XmlWriter.Create(stream, settings);
                    break;
                case ReaderWriterType.WrappedWebData:
                    XmlWriterSettings settings2 = new XmlWriterSettings();
                    settings2.Encoding = encoding;
                    result = XmlWriter.Create(stream, settings2);
                    result = XmlDictionaryWriter.CreateDictionaryWriter(result);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rwType));
            }

            return result;
        }
    }

    public class Helper
    {
        public static void DumpToFile(byte[] buffer, int offset, int count)
        {
            using (var testFile = TempFile.Create())
            {
                try
                {
                    FileStream fs = File.Create(testFile.Path);
                    fs.Write(buffer, offset, count);
                    fs.Close();
                }
                catch (Exception e)
                {
                    // This is just for debugging purposes, no problem if it fails.
                    System.Diagnostics.Trace.WriteLine("Failed dumping to file: " + e);
                }
            }
        }

        public static void DumpToFile(byte[] buffer)
        {
            DumpToFile(buffer, 0, buffer.Length);
        }
    }
}