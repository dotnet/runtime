// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Globalization;
using OLEDB.Test.ModuleCore;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Summary description for Class1
/// </summary>
namespace XmlCoreTest.Common
{
    public class aaaa { };

    public class AsyncUtil
    {
        public static bool IsAsyncEnabled
        {
            get
            {
                // looking for ltm command line option like "/Async true"
                string asyncOption = CModInfo.Options["Async"] as string;
                if (!string.IsNullOrEmpty(asyncOption))
                {
                    if (string.Compare("TRUE", asyncOption) == 0)
                        return true;
                }

                return false;
            }
        }

        private static bool _redirectReader = true;   // for debugging, set this to false if want to stop the async XmlReader testing temporary during debugging
        public static bool RedirectReader
        {
            get
            {
                return _redirectReader;
            }
        }

        private static bool _redirectWriter = true;   // for debugging, set this to false if want to stop the async XmlWriter testing temporary during debugging
        public static bool RedirectWriter
        {
            get
            {
                return _redirectWriter;
            }
        }
    }
    public class RedirectSyncCallToAsyncCallXmlReader : XmlReader, IXmlLineInfo   //inherit from XmlReader so that we don't have the change the return type of function that returns a XmlReader
    {
        // the real XmlReader
        private XmlReader _reader = null;

        public XmlReader CoreReader
        {
            get
            {
                return _reader;
            }
            set
            {
                _reader = value;
            }
        }

        public RedirectSyncCallToAsyncCallXmlReader(XmlReader xr)
        {
            CoreReader = xr;
        }

        // #region Rewrite Methods has async counterpart

        public override bool Read()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<bool> t = CoreReader.ReadAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.Read();
            }
        }
        public override Task<bool> ReadAsync()
        {
            return CoreReader.ReadAsync();
        }


        public override string Value
        {
            get
            {
                if (AsyncUtil.RedirectReader)
                {
                    try
                    {
                        Task<string> t = CoreReader.GetValueAsync();
                        t.Wait();
                        return t.Result;
                    }
                    catch (AggregateException ae)
                    {
                        throw ae.InnerException;
                    }
                }
                else
                {
                    return CoreReader.Value;
                }
            }
        }
        public override Task<string> GetValueAsync()
        {
            return CoreReader.GetValueAsync();
        }

        public override XmlNodeType MoveToContent()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<XmlNodeType> t = CoreReader.MoveToContentAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.MoveToContent();
            }
        }
        public override Task<XmlNodeType> MoveToContentAsync()
        {
            return CoreReader.MoveToContentAsync();
        }


        public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<object> t = CoreReader.ReadContentAsAsync(returnType, namespaceResolver);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadContentAs(returnType, namespaceResolver);
            }
        }
        public override Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return CoreReader.ReadContentAsAsync(returnType, namespaceResolver);
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<int> t = CoreReader.ReadContentAsBase64Async(buffer, index, count);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadContentAsBase64(buffer, index, count);
            }
        }
        public override Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            return CoreReader.ReadContentAsBase64Async(buffer, index, count);
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<int> t = CoreReader.ReadContentAsBinHexAsync(buffer, index, count);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadContentAsBinHex(buffer, index, count);
            }
        }
        public override Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            return CoreReader.ReadContentAsBinHexAsync(buffer, index, count);
        }

        public override object ReadContentAsObject()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<object> t = CoreReader.ReadContentAsObjectAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadContentAsObject();
            }
        }
        public override Task<object> ReadContentAsObjectAsync()
        {
            return CoreReader.ReadContentAsObjectAsync();
        }

        public override string ReadContentAsString()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<string> t = CoreReader.ReadContentAsStringAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadContentAsString();
            }
        }
        public override Task<string> ReadContentAsStringAsync()
        {
            return CoreReader.ReadContentAsStringAsync();
        }

        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<object> t = CoreReader.ReadElementContentAsAsync(returnType, namespaceResolver);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadElementContentAs(returnType, namespaceResolver);
            }
        }
        public override Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            return CoreReader.ReadElementContentAsAsync(returnType, namespaceResolver);
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<int> t = CoreReader.ReadElementContentAsBase64Async(buffer, index, count);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadElementContentAsBase64(buffer, index, count);
            }
        }
        public override Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            return CoreReader.ReadElementContentAsBase64Async(buffer, index, count);
        }


        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<int> t = CoreReader.ReadElementContentAsBinHexAsync(buffer, index, count);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadElementContentAsBinHex(buffer, index, count);
            }
        }
        public override Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            return CoreReader.ReadElementContentAsBinHexAsync(buffer, index, count);
        }

        public override object ReadElementContentAsObject()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<object> t = CoreReader.ReadElementContentAsObjectAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadElementContentAsObject();
            }
        }
        public override Task<object> ReadElementContentAsObjectAsync()
        {
            return CoreReader.ReadElementContentAsObjectAsync();
        }

        public override string ReadElementContentAsString()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<string> t = CoreReader.ReadElementContentAsStringAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadElementContentAsString();
            }
        }
        public override Task<string> ReadElementContentAsStringAsync()
        {
            return CoreReader.ReadElementContentAsStringAsync();
        }

        public override string ReadInnerXml()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<string> t = CoreReader.ReadInnerXmlAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadInnerXml();
            }
        }
        public override Task<string> ReadInnerXmlAsync()
        {
            return CoreReader.ReadInnerXmlAsync();
        }


        public override string ReadOuterXml()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<string> t = CoreReader.ReadOuterXmlAsync();
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadOuterXml();
            }
        }
        public override Task<string> ReadOuterXmlAsync()
        {
            return CoreReader.ReadOuterXmlAsync();
        }

        public override int ReadValueChunk(char[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    Task<int> t = CoreReader.ReadValueChunkAsync(buffer, index, count);
                    t.Wait();
                    return t.Result;
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                return CoreReader.ReadValueChunk(buffer, index, count);
            }
        }
        public override Task<int> ReadValueChunkAsync(char[] buffer, int index, int count)
        {
            return CoreReader.ReadValueChunkAsync(buffer, index, count);
        }

        public override void Skip()
        {
            if (AsyncUtil.RedirectReader)
            {
                try
                {
                    CoreReader.SkipAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreReader.Skip();
            }
        }
        public override Task SkipAsync()
        {
            return CoreReader.SkipAsync();
        }
        //#endregion

        #region Forward the call to the core reader
        /// <summary>
        /// Attributes and methods that don't need redirect
        /// </summary>
        public override XmlReaderSettings Settings
        {
            get
            {
                return CoreReader.Settings;
            }
        }

        public override XmlNodeType NodeType
        {
            get
            {
                return CoreReader.NodeType;
            }
        }

        public override string Name
        {
            get
            {
                return CoreReader.Name;
            }
        }

        public override string LocalName
        {
            get
            {
                return CoreReader.LocalName;
            }
        }

        public override string NamespaceURI
        {
            get
            {
                return CoreReader.NamespaceURI;
            }
        }

        public override string Prefix
        {
            get
            {
                return CoreReader.Prefix;
            }
        }

        public override bool HasValue
        {
            get
            {
                return CoreReader.HasValue;
            }
        }

        public override int Depth
        {
            get
            {
                return CoreReader.Depth;
            }
        }

        public override string BaseURI
        {
            get
            {
                return CoreReader.BaseURI;
            }
        }

        public override bool IsEmptyElement
        {
            get
            {
                return CoreReader.IsEmptyElement;
            }
        }

        public override bool IsDefault
        {
            get
            {
                return CoreReader.IsDefault;
            }
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return CoreReader.XmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                return CoreReader.XmlLang;
            }
        }

        public override System.Type ValueType
        {
            get
            {
                return CoreReader.ValueType;
            }
        }

        public override int AttributeCount
        {
            get
            {
                return CoreReader.AttributeCount;
            }
        }

        public override string GetAttribute(string name)
        {
            return CoreReader.GetAttribute(name);
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return CoreReader.GetAttribute(name, namespaceURI);
        }

        public override string GetAttribute(int i)
        {
            return CoreReader.GetAttribute(i);
        }

        public override string this[int i]
        {
            get
            {
                return CoreReader.GetAttribute(i);
            }
        }

        public override string this[string name]
        {
            get
            {
                return CoreReader.GetAttribute(name);
            }
        }

        public override string this[string name, string namespaceURI]
        {
            get
            {
                return CoreReader.GetAttribute(name, namespaceURI);
            }
        }

        public override bool MoveToAttribute(string name)
        {
            return CoreReader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return CoreReader.MoveToAttribute(name, ns);
        }

        public override void MoveToAttribute(int i)
        {
            CoreReader.MoveToAttribute(i);
        }

        // Moves to the first attribute of the current node.
        public override bool MoveToFirstAttribute()
        {
            return CoreReader.MoveToFirstAttribute();
        }

        // Moves to the next attribute.
        public override bool MoveToNextAttribute()
        {
            return CoreReader.MoveToNextAttribute();
        }

        // Moves to the element that contains the current attribute node.
        public override bool MoveToElement()
        {
            return CoreReader.MoveToElement();
        }

        public override bool ReadAttributeValue()
        {
            return CoreReader.ReadAttributeValue();
        }

        public override bool EOF
        {
            get
            {
                return CoreReader.EOF;
            }
        }

        public override ReadState ReadState
        {
            get
            {
                return CoreReader.ReadState;
            }
        }

        public override XmlNameTable NameTable
        {
            get
            {
                return CoreReader.NameTable;
            }
        }

        public override string LookupNamespace(string prefix)
        {
            return CoreReader.LookupNamespace(prefix);
        }

        public override bool CanResolveEntity
        {
            get
            {
                return CoreReader.CanResolveEntity;
            }
        }

        public override void ResolveEntity()
        {
            CoreReader.ResolveEntity();
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return CoreReader.CanReadBinaryContent;
            }
        }

        public override bool CanReadValueChunk
        {
            get
            {
                return CoreReader.CanReadValueChunk;
            }
        }

        public override bool IsStartElement()
        {
            return CoreReader.IsStartElement();
        }

        public override bool IsStartElement(string name)
        {
            return CoreReader.IsStartElement(name);
        }

        public override bool IsStartElement(string localname, string ns)
        {
            return CoreReader.IsStartElement(localname, ns);
        }

        public override XmlReader ReadSubtree()
        {
            return CoreReader.ReadSubtree();
        }

        public override bool HasAttributes
        {
            get
            {
                return CoreReader.HasAttributes;
            }
        }

        public new void Dispose()
        {
            CoreReader.Dispose();
        }

        public bool HasLineInfo()
        {
            IXmlLineInfo ili = CoreReader as IXmlLineInfo;
            if (ili != null)
                return ili.HasLineInfo();
            return false;
        }

        public override bool ReadContentAsBoolean()
        {
            return CoreReader.ReadContentAsBoolean();
        }

        public override DateTimeOffset ReadContentAsDateTimeOffset()
        {
            return CoreReader.ReadContentAsDateTimeOffset();
        }
        public override double ReadContentAsDouble()
        {
            return CoreReader.ReadContentAsDouble();
        }
        public override float ReadContentAsFloat()
        {
            return CoreReader.ReadContentAsFloat();
        }
        public override decimal ReadContentAsDecimal()
        {
            return CoreReader.ReadContentAsDecimal();
        }
        public override int ReadContentAsInt()
        {
            return CoreReader.ReadContentAsInt();
        }
        public override long ReadContentAsLong()
        {
            return CoreReader.ReadContentAsLong();
        }
        public override object ReadElementContentAsObject(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsObject(localName, namespaceURI);
        }
        public override bool ReadElementContentAsBoolean()
        {
            return CoreReader.ReadElementContentAsBoolean();
        }
        public override bool ReadElementContentAsBoolean(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsBoolean(localName, namespaceURI);
        }
        public override double ReadElementContentAsDouble()
        {
            return CoreReader.ReadElementContentAsDouble();
        }
        public override double ReadElementContentAsDouble(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsDouble(localName, namespaceURI);
        }
        public override float ReadElementContentAsFloat()
        {
            return CoreReader.ReadElementContentAsFloat();
        }
        public override float ReadElementContentAsFloat(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsFloat(localName, namespaceURI);
        }
        public override decimal ReadElementContentAsDecimal()
        {
            return CoreReader.ReadElementContentAsDecimal();
        }
        public override decimal ReadElementContentAsDecimal(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsDecimal(localName, namespaceURI);
        }
        public override int ReadElementContentAsInt()
        {
            return CoreReader.ReadElementContentAsInt();
        }
        public override int ReadElementContentAsInt(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsInt(localName, namespaceURI);
        }
        public override long ReadElementContentAsLong()
        {
            return CoreReader.ReadElementContentAsLong();
        }
        public override long ReadElementContentAsLong(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsLong(localName, namespaceURI);
        }
        public override string ReadElementContentAsString(string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAsString(localName, namespaceURI);
        }
        public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver, string localName, string namespaceURI)
        {
            return CoreReader.ReadElementContentAs(returnType, namespaceResolver, localName, namespaceURI);
        }
        public override void ReadStartElement()
        {
            CoreReader.ReadStartElement();
        }
        public override void ReadStartElement(string name)
        {
            CoreReader.ReadStartElement(name);
        }
        public override void ReadStartElement(string localname, string ns)
        {
            CoreReader.ReadStartElement(localname, ns);
        }
        public override void ReadEndElement()
        {
            CoreReader.ReadEndElement();
        }
        public override bool ReadToFollowing(string name)
        {
            return CoreReader.ReadToFollowing(name);
        }
        public override bool ReadToFollowing(string localName, string namespaceURI)
        {
            return CoreReader.ReadToFollowing(localName, namespaceURI);
        }
        public override bool ReadToDescendant(string name)
        {
            return CoreReader.ReadToDescendant(name);
        }
        public override bool ReadToDescendant(string localName, string namespaceURI)
        {
            return CoreReader.ReadToDescendant(localName, namespaceURI);
        }
        public override bool ReadToNextSibling(string name)
        {
            return CoreReader.ReadToNextSibling(name);
        }
        public override bool ReadToNextSibling(string localName, string namespaceURI)
        {
            return CoreReader.ReadToNextSibling(localName, namespaceURI);
        }

        // Returns the line number of the current node
        public int LineNumber
        {
            get
            {
                IXmlLineInfo ili = CoreReader as IXmlLineInfo;
                if (ili != null)
                    return ili.LineNumber;
                return 0;
            }
        }

        // Returns the line position of the current node
        public int LinePosition
        {
            get
            {
                IXmlLineInfo ili = CoreReader as IXmlLineInfo;
                if (ili != null)
                    return ili.LinePosition;
                return 0;
            }
        }
        #endregion

    }

    /// <summary>
    /// This class provide serials Create() methods to create async XmlReader, designed to replace XmlReaderCreate() with XmlReaderAsync.Create().
    /// Every XmlReader created by this class will forced to be async.
    /// </summary>
    public class XmlReaderAsync
    {
        public static XmlReader Create(String inputUri, XmlReaderSettings settings = null, XmlParserContext inputContext = null)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }

            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            inputContext = null;
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(inputUri, settings));
        }

        public static XmlReader Create(Stream input)
        {
            return Create(input, (XmlReaderSettings)null, (string)string.Empty);
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings)
        {
            return Create(input, settings, string.Empty);
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings, string baseUri)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            baseUri = null;
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(input, settings));
        }

        public static XmlReader Create(Stream input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(input, settings, inputContext));
        }

        public static XmlReader Create(TextReader input)
        {
            return Create(input, (XmlReaderSettings)null, (string)string.Empty);
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings)
        {
            return Create(input, settings, string.Empty);
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings, String baseUri)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            baseUri = null;
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(input, settings));
        }

        public static XmlReader Create(TextReader input, XmlReaderSettings settings, XmlParserContext inputContext)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(input, settings, inputContext));
        }

        public static XmlReader Create(XmlReader reader, XmlReaderSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlReaderSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;    // force async
            }
            return new RedirectSyncCallToAsyncCallXmlReader(XmlReader.Create(reader, settings));
        }
    }

    public class RedirectSyncCallToAsyncCallXmlWriter : XmlWriter
    {
        private XmlWriter _writer = null;   // the core writer

        public XmlWriter CoreWriter
        {
            get
            {
                return _writer;
            }
            set
            {
                _writer = value;
            }
        }
        public RedirectSyncCallToAsyncCallXmlWriter(XmlWriter xw)
        {
            CoreWriter = xw;
        }

        public override void Flush()
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.FlushAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.Flush();
            }
        }
        public override Task FlushAsync()
        {
            return CoreWriter.FlushAsync();
        }


        public override void WriteAttributes(XmlReader reader, bool defattr)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteAttributesAsync(reader, defattr).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteAttributes(reader, defattr);
            }
        }
        public override Task WriteAttributesAsync(XmlReader reader, bool defattr)
        {
            return CoreWriter.WriteAttributesAsync(reader, defattr);
        }

        public new void WriteAttributeString(string prefix, string localName, string ns, string value)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteAttributeStringAsync(prefix, localName, ns, value).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteAttributeString(prefix, localName, ns, value);
            }
        }
        public new Task WriteAttributeStringAsync(string prefix, string localName, string ns, string value)
        {
            return CoreWriter.WriteAttributeStringAsync(prefix, localName, ns, value);
        }

        public override void WriteBase64(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteBase64Async(buffer, index, count).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteBase64(buffer, index, count);
            }
        }
        public override Task WriteBase64Async(byte[] buffer, int index, int count)
        {
            return CoreWriter.WriteBase64Async(buffer, index, count);
        }

        public override void WriteBinHex(byte[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteBinHexAsync(buffer, index, count).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteBinHex(buffer, index, count);
            }
        }
        public override Task WriteBinHexAsync(byte[] buffer, int index, int count)
        {
            return CoreWriter.WriteBinHexAsync(buffer, index, count);
        }

        public override void WriteCData(string text)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteCDataAsync(text).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteCData(text);
            }
        }
        public override Task WriteCDataAsync(string text)
        {
            return CoreWriter.WriteCDataAsync(text);
        }

        public override void WriteCharEntity(char ch)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteCharEntityAsync(ch).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteCharEntity(ch);
            }
        }
        public override Task WriteCharEntityAsync(char ch)
        {
            return CoreWriter.WriteCharEntityAsync(ch);
        }

        public override void WriteChars(char[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteCharsAsync(buffer, index, count).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteChars(buffer, index, count);
            }
        }
        public override Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            return CoreWriter.WriteCharsAsync(buffer, index, count);
        }

        public override void WriteComment(string text)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteCommentAsync(text).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteComment(text);
            }
        }
        public override Task WriteCommentAsync(string text)
        {
            return CoreWriter.WriteCommentAsync(text);
        }

        public override void WriteDocType(string name, string pubid, string sysid, string subset)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteDocTypeAsync(name, pubid, sysid, subset).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteDocType(name, pubid, sysid, subset);
            }
        }
        public override Task WriteDocTypeAsync(string name, string pubid, string sysid, string subset)
        {
            return CoreWriter.WriteDocTypeAsync(name, pubid, sysid, subset);
        }

        public new void WriteElementString(string prefix, String localName, String ns, String value)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteElementStringAsync(prefix, localName, ns, value).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteElementString(prefix, localName, ns, value);
            }
        }
        public new Task WriteElementStringAsync(string prefix, String localName, String ns, String value)
        {
            return CoreWriter.WriteElementStringAsync(prefix, localName, ns, value);
        }

        public override void WriteEndDocument()
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteEndDocumentAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteEndDocument();
            }
        }
        public override Task WriteEndDocumentAsync()
        {
            return CoreWriter.WriteEndDocumentAsync();
        }

        public override void WriteEndElement()
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteEndElementAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteEndElement();
            }
        }
        public override Task WriteEndElementAsync()
        {
            return CoreWriter.WriteEndElementAsync();
        }


        public override void WriteEntityRef(string name)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteEntityRefAsync(name).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteEntityRef(name);
            }
        }
        public override Task WriteEntityRefAsync(string name)
        {
            return CoreWriter.WriteEntityRefAsync(name);
        }

        public override void WriteFullEndElement()
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteFullEndElementAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteFullEndElement();
            }
        }
        public override Task WriteFullEndElementAsync()
        {
            return CoreWriter.WriteFullEndElementAsync();
        }

        public override void WriteName(string name)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteNameAsync(name).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteName(name);
            }
        }
        public override Task WriteNameAsync(string name)
        {
            return CoreWriter.WriteNameAsync(name);
        }

        public override void WriteNmToken(string name)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteNmTokenAsync(name).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteNmToken(name);
            }
        }
        public override Task WriteNmTokenAsync(string name)
        {
            return CoreWriter.WriteNmTokenAsync(name);
        }

        public override void WriteNode(XmlReader reader, bool defattr)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteNodeAsync(reader, defattr).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteNode(reader, defattr);
            }
        }

        public override Task WriteNodeAsync(XmlReader reader, bool defattr)
        {
            return CoreWriter.WriteNodeAsync(reader, defattr);
        }

        public override void WriteProcessingInstruction(string name, string text)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteProcessingInstructionAsync(name, text).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteProcessingInstruction(name, text);
            }
        }

        public override Task WriteProcessingInstructionAsync(string name, string text)
        {
            return CoreWriter.WriteProcessingInstructionAsync(name, text);
        }

        public override void WriteQualifiedName(string localName, string ns)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteQualifiedNameAsync(localName, ns).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteQualifiedName(localName, ns);
            }
        }

        public override Task WriteQualifiedNameAsync(string localName, string ns)
        {
            return CoreWriter.WriteQualifiedNameAsync(localName, ns);
        }

        public override void WriteRaw(char[] buffer, int index, int count)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteRawAsync(buffer, index, count).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteRaw(buffer, index, count);
            }
        }
        public override Task WriteRawAsync(char[] buffer, int index, int count)
        {
            return CoreWriter.WriteRawAsync(buffer, index, count);
        }

        public override void WriteRaw(string data)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteRawAsync(data).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteRaw(data);
            }
        }

        public override Task WriteRawAsync(string data)
        {
            return CoreWriter.WriteRawAsync(data);
        }

        public override void WriteStartDocument()
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteStartDocumentAsync().Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteStartDocument();
            }
        }

        public override Task WriteStartDocumentAsync()
        {
            return CoreWriter.WriteStartDocumentAsync();
        }

        public override void WriteStartDocument(bool standalone)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteStartDocumentAsync(standalone).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteStartDocument(standalone);
            }
        }

        public override Task WriteStartDocumentAsync(bool standalone)
        {
            return CoreWriter.WriteStartDocumentAsync(standalone);
        }

        public override void WriteStartElement(string prefix, string localName, string ns)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteStartElementAsync(prefix, localName, ns).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteStartElement(prefix, localName, ns);
            }
        }

        public override Task WriteStartElementAsync(string prefix, string localName, string ns)
        {
            return CoreWriter.WriteStartElementAsync(prefix, localName, ns);
        }

        public override void WriteString(string text)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteStringAsync(text).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteString(text);
            }
        }
        public override Task WriteStringAsync(string text)
        {
            return CoreWriter.WriteStringAsync(text);
        }

        public override void WriteSurrogateCharEntity(char lowChar, char highChar)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteSurrogateCharEntityAsync(lowChar, highChar).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteSurrogateCharEntity(lowChar, highChar);
            }
        }

        public override Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            return CoreWriter.WriteSurrogateCharEntityAsync(lowChar, highChar);
        }

        public override void WriteWhitespace(string ws)
        {
            if (AsyncUtil.RedirectWriter)
            {
                try
                {
                    CoreWriter.WriteWhitespaceAsync(ws).Wait();
                }
                catch (AggregateException ae)
                {
                    throw ae.InnerException;
                }
            }
            else
            {
                CoreWriter.WriteWhitespace(ws);
            }
        }
        public override Task WriteWhitespaceAsync(string ws)
        {
            return CoreWriter.WriteWhitespaceAsync(ws);
        }

        /// <summary>
        /// ////////////////////////////////
        /// </summary>
        #region public methods without async version

        public override XmlWriterSettings Settings
        {
            get
            {
                return CoreWriter.Settings;
            }
        }

        public override WriteState WriteState
        {
            get
            {
                return CoreWriter.WriteState;
            }
        }

        public override string LookupPrefix(string ns)
        {
            return CoreWriter.LookupPrefix(ns);
        }

        public override XmlSpace XmlSpace
        {
            get
            {
                return CoreWriter.XmlSpace;
            }
        }

        public override string XmlLang
        {
            get
            {
                return CoreWriter.XmlLang;
            }
        }

        public new void Dispose()
        {
            CoreWriter.Dispose();
        }

        public new void WriteStartElement(string localName, string ns)
        {
            CoreWriter.WriteStartElement(localName, ns);
        }

        public new void WriteStartElement(string localName)
        {
            CoreWriter.WriteStartElement(localName);
        }

        public new void WriteAttributeString(string localName, string ns, string value)
        {
            CoreWriter.WriteAttributeString(localName, ns, value);
        }

        public new void WriteAttributeString(string localName, string value)
        {
            CoreWriter.WriteAttributeString(localName, value);
        }

        public override void WriteStartAttribute(string prefix, string localName, string ns)
        {
            CoreWriter.WriteStartAttribute(prefix, localName, ns);
        }

        public new void WriteStartAttribute(string localName)
        {
            CoreWriter.WriteStartAttribute(localName);
        }

        public override void WriteEndAttribute()
        {
            CoreWriter.WriteEndAttribute();
        }

        public override void WriteValue(object value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(string value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(bool value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(DateTimeOffset value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(double value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(float value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(decimal value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(int value)
        {
            CoreWriter.WriteValue(value);
        }

        public override void WriteValue(long value)
        {
            CoreWriter.WriteValue(value);
        }

        public new void WriteElementString(string localName, String value)
        {
            CoreWriter.WriteElementString(localName, value);
        }

        public new void WriteElementString(string localName, String ns, String value)
        {
            CoreWriter.WriteElementString(localName, ns, value);
        }
        #endregion
    }

    public class XmlWriterAsync
    {
        public static XmlWriter Create(string outputFileName)
        {
            return Create(outputFileName, null);
        }

        public static XmlWriter Create(string outputFileName, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;
            }
            return null;
        }

        public static XmlWriter Create(Stream output)
        {
            return Create(output, null);
        }

        public static XmlWriter Create(Stream output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;
            }
            return new RedirectSyncCallToAsyncCallXmlWriter(XmlWriter.Create(output, settings));
        }

        public static XmlWriter Create(TextWriter output)
        {
            return Create(output, null);
        }

        public static XmlWriter Create(TextWriter output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;
            }
            return new RedirectSyncCallToAsyncCallXmlWriter(XmlWriter.Create(output, settings));
        }

        public static XmlWriter Create(StringBuilder output)
        {
            return Create(output, null);
        }

        public static XmlWriter Create(StringBuilder output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;
            }
            return new RedirectSyncCallToAsyncCallXmlWriter(XmlWriter.Create(output, settings));
        }

        public static XmlWriter Create(XmlWriter output)
        {
            return Create(output, null);
        }

        public static XmlWriter Create(XmlWriter output, XmlWriterSettings settings)
        {
            if (settings == null)
            {
                settings = new XmlWriterSettings();
            }
            if (AsyncUtil.RedirectWriter)
            {
                settings.Async = true;
            }
            return new RedirectSyncCallToAsyncCallXmlWriter(XmlWriter.Create(output, settings));
        }
    }
}
