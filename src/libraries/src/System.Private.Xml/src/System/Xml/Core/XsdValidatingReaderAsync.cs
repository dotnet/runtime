// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;

using System.Threading.Tasks;

namespace System.Xml
{
    internal partial class XsdValidatingReader : XmlReader, IXmlSchemaInfo, IXmlLineInfo, IXmlNamespaceResolver
    {
        // Gets the text value of the current node.
        public override Task<string> GetValueAsync()
        {
            if ((int)_validationState < 0)
            {
                return Task.FromResult(_cachedNode.RawValue);
            }
            return _coreReader.GetValueAsync();
        }

        public override Task<object> ReadContentAsObjectAsync()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsObject));
            }

            return InternalReadContentAsObjectAsync(true);
        }

        public override async Task<string> ReadContentAsStringAsync()
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAsString));
            }
            object typedValue = await InternalReadContentAsObjectAsync().ConfigureAwait(false);
            XmlSchemaType xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToString(typedValue);
                }
                else
                {
                    return typedValue as string;
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
        }

        public override async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (!CanReadContentAs(this.NodeType))
            {
                throw CreateReadContentAsException(nameof(ReadContentAs));
            }
            string originalStringValue;

            var tuple_0 = await InternalReadContentAsObjectTupleAsync(false).ConfigureAwait(false);
            originalStringValue = tuple_0.Item1;

            object typedValue = tuple_0.Item2;

            XmlSchemaType xmlType = NodeType == XmlNodeType.Attribute ? AttributeXmlType : ElementXmlType;
            try
            {
                if (xmlType != null)
                {
                    // special-case convertions to DateTimeOffset; typedValue is by default a DateTime 
                    // which cannot preserve time zone, so we need to convert from the original string
                    if (returnType == typeof(DateTimeOffset) && xmlType.Datatype is Datatype_dateTimeBase)
                    {
                        typedValue = originalStringValue;
                    }
                    return xmlType.ValueConverter.ChangeType(typedValue, returnType);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ChangeType(typedValue, returnType, namespaceResolver);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
        }

        public override async Task<object> ReadElementContentAsObjectAsync()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsObject));
            }

            var tuple_1 = await InternalReadElementContentAsObjectAsync(true).ConfigureAwait(false);

            return tuple_1.Item2;
        }

        public override async Task<string> ReadElementContentAsStringAsync()
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAsString));
            }
            XmlSchemaType xmlType;

            var tuple_9 = await InternalReadElementContentAsObjectAsync().ConfigureAwait(false);
            xmlType = tuple_9.Item1;

            object typedValue = tuple_9.Item2;

            try
            {
                if (xmlType != null)
                {
                    return xmlType.ValueConverter.ToString(typedValue);
                }
                else
                {
                    return typedValue as string;
                }
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, "String", e, this as IXmlLineInfo);
            }
        }

        public override async Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
        {
            if (this.NodeType != XmlNodeType.Element)
            {
                throw CreateReadElementContentAsException(nameof(ReadElementContentAs));
            }
            XmlSchemaType xmlType;
            string originalStringValue;

            var tuple_10 = await InternalReadElementContentAsObjectTupleAsync(false).ConfigureAwait(false);
            xmlType = tuple_10.Item1;
            originalStringValue = tuple_10.Item2;

            object typedValue = tuple_10.Item3;

            try
            {
                if (xmlType != null)
                {
                    // special-case convertions to DateTimeOffset; typedValue is by default a DateTime 
                    // which cannot preserve time zone, so we need to convert from the original string
                    if (returnType == typeof(DateTimeOffset) && xmlType.Datatype is Datatype_dateTimeBase)
                    {
                        typedValue = originalStringValue;
                    }
                    return xmlType.ValueConverter.ChangeType(typedValue, returnType, namespaceResolver);
                }
                else
                {
                    return XmlUntypedConverter.Untyped.ChangeType(typedValue, returnType, namespaceResolver);
                }
            }
            catch (FormatException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (InvalidCastException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
            catch (OverflowException e)
            {
                throw new XmlException(SR.Xml_ReadContentAsFormatException, returnType.ToString(), e, this as IXmlLineInfo);
            }
        }

        private Task<bool> ReadAsync_Read(Task<bool> task)
        {
            if (task.IsSuccess())
            {
                if (task.Result)
                {
                    return ProcessReaderEventAsync().ReturnTrueTaskWhenFinishAsync();
                }
                else
                {
                    _validator.EndValidation();
                    if (_coreReader.EOF)
                    {
                        _validationState = ValidatingReaderState.EOF;
                    }
                    return AsyncHelper.DoneTaskFalse;
                }
            }
            else
            {
                return _ReadAsync_Read(task);
            }
        }

        private async Task<bool> _ReadAsync_Read(Task<bool> task)
        {
            if (await task.ConfigureAwait(false))
            {
                await ProcessReaderEventAsync().ConfigureAwait(false);
                return true;
            }
            else
            {
                _validator.EndValidation();
                if (_coreReader.EOF)
                {
                    _validationState = ValidatingReaderState.EOF;
                }
                return false;
            }
        }

        private Task<bool> ReadAsync_ReadAhead(Task task)
        {
            if (task.IsSuccess())
            {
                _validationState = ValidatingReaderState.Read;
                return AsyncHelper.DoneTaskTrue; ;
            }
            else
            {
                return _ReadAsync_ReadAhead(task);
            }
        }

        private async Task<bool> _ReadAsync_ReadAhead(Task task)
        {
            await task.ConfigureAwait(false);
            _validationState = ValidatingReaderState.Read;
            return true;
        }

        // Reads the next node from the stream/TextReader.
        public override Task<bool> ReadAsync()
        {
            switch (_validationState)
            {
                case ValidatingReaderState.Read:
                    Task<bool> readTask = _coreReader.ReadAsync();
                    return ReadAsync_Read(readTask);

                case ValidatingReaderState.ParseInlineSchema:
                    return ProcessInlineSchemaAsync().ReturnTrueTaskWhenFinishAsync();

                case ValidatingReaderState.OnAttribute:
                case ValidatingReaderState.OnDefaultAttribute:
                case ValidatingReaderState.ClearAttributes:
                case ValidatingReaderState.OnReadAttributeValue:
                    ClearAttributesInfo();
                    if (_inlineSchemaParser != null)
                    {
                        _validationState = ValidatingReaderState.ParseInlineSchema;
                        goto case ValidatingReaderState.ParseInlineSchema;
                    }
                    else
                    {
                        _validationState = ValidatingReaderState.Read;
                        goto case ValidatingReaderState.Read;
                    }

                case ValidatingReaderState.ReadAhead: //Will enter here on calling Skip() 
                    ClearAttributesInfo();
                    Task task = ProcessReaderEventAsync();
                    return ReadAsync_ReadAhead(task);

                case ValidatingReaderState.OnReadBinaryContent:
                    _validationState = _savedState;
                    return _readBinaryHelper.FinishAsync().CallBoolTaskFuncWhenFinishAsync(thisRef => thisRef.ReadAsync(), this);

                case ValidatingReaderState.Init:
                    _validationState = ValidatingReaderState.Read;
                    if (_coreReader.ReadState == ReadState.Interactive)
                    { //If the underlying reader is already positioned on a ndoe, process it
                        return ProcessReaderEventAsync().ReturnTrueTaskWhenFinishAsync();
                    }
                    else
                    {
                        goto case ValidatingReaderState.Read;
                    }

                case ValidatingReaderState.ReaderClosed:
                case ValidatingReaderState.EOF:
                    return AsyncHelper.DoneTaskFalse;

                default:
                    return AsyncHelper.DoneTaskFalse;
            }
        }

        // Skips to the end tag of the current element.
        public override async Task SkipAsync()
        {
            int startDepth = Depth;
            switch (NodeType)
            {
                case XmlNodeType.Element:
                    if (_coreReader.IsEmptyElement)
                    {
                        break;
                    }
                    bool callSkipToEndElem = true;
                    //If union and unionValue has been parsed till EndElement, then validator.ValidateEndElement has been called
                    //Hence should not call SkipToEndElement as the current context has already been popped in the validator
                    if ((_xmlSchemaInfo.IsUnionType || _xmlSchemaInfo.IsDefault) && _coreReader is XsdCachingReader)
                    {
                        callSkipToEndElem = false;
                    }
                    await _coreReader.SkipAsync().ConfigureAwait(false);
                    _validationState = ValidatingReaderState.ReadAhead;
                    if (callSkipToEndElem)
                    {
                        _validator.SkipToEndElement(_xmlSchemaInfo);
                    }
                    break;

                case XmlNodeType.Attribute:
                    MoveToElement();
                    goto case XmlNodeType.Element;
            }
            //For all other NodeTypes Skip() same as Read()
            await ReadAsync().ConfigureAwait(false);
            return;
        }

        public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadContentAsBinaryHelper when called first time
            if (_validationState != ValidatingReaderState.OnReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                _savedState = _validationState;
            }

            // restore original state in order to have a normal Read() behavior when called from readBinaryHelper
            _validationState = _savedState;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // set OnReadBinaryContent state again and return
            _savedState = _validationState;
            _validationState = ValidatingReaderState.OnReadBinaryContent;
            return readCount;
        }

        private Task ProcessReaderEventAsync()
        {
            if (_replayCache)
            { //if in replay mode, do nothing since nodes have been validated already
                //If NodeType == XmlNodeType.EndElement && if manageNamespaces, may need to pop namespace scope, since scope is not popped in ReadAheadForMemberType

                return Task.CompletedTask;
            }
            switch (_coreReader.NodeType)
            {
                case XmlNodeType.Element:

                    return ProcessElementEventAsync();

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    _validator.ValidateWhitespace(GetStringValue);
                    break;

                case XmlNodeType.Text:          // text inside a node
                case XmlNodeType.CDATA:         // <![CDATA[...]]>
                    _validator.ValidateText(GetStringValue);
                    break;

                case XmlNodeType.EndElement:

                    return ProcessEndElementEventAsync();

                case XmlNodeType.EntityReference:
                    throw new InvalidOperationException();

                case XmlNodeType.DocumentType:
#if TEMP_HACK_FOR_SCHEMA_INFO
                    validator.SetDtdSchemaInfo((SchemaInfo)coreReader.DtdInfo);
#else
                    _validator.SetDtdSchemaInfo(_coreReader.DtdInfo);
#endif
                    break;

                default:
                    break;
            }

            return Task.CompletedTask;
        }

        // SxS: This function calls ValidateElement on XmlSchemaValidator which is annotated with ResourceExposure attribute.
        // Since the resource names (namespace location) are not provided directly by the user (they are read from the source
        // document) and the function does not expose any resources it is fine to suppress the SxS warning.
        private async Task ProcessElementEventAsync()
        {
            if (_processInlineSchema && IsXSDRoot(_coreReader.LocalName, _coreReader.NamespaceURI) && _coreReader.Depth > 0)
            {
                _xmlSchemaInfo.Clear();
                _attributeCount = _coreReaderAttributeCount = _coreReader.AttributeCount;
                if (!_coreReader.IsEmptyElement)
                { //If its not empty schema, then parse else ignore
                    _inlineSchemaParser = new Parser(SchemaType.XSD, _coreReaderNameTable, _validator.SchemaSet.GetSchemaNames(_coreReaderNameTable), _validationEvent);
                    await _inlineSchemaParser.StartParsingAsync(_coreReader, null).ConfigureAwait(false);
                    _inlineSchemaParser.ParseReaderNode();
                    _validationState = ValidatingReaderState.ParseInlineSchema;
                }
                else
                {
                    _validationState = ValidatingReaderState.ClearAttributes;
                }
            }
            else
            { //Validate element
                //Clear previous data
                _atomicValue = null;
                _originalAtomicValueString = null;
                _xmlSchemaInfo.Clear();

                if (_manageNamespaces)
                {
                    _nsManager.PushScope();
                }
                //Find Xsi attributes that need to be processed before validating the element
                string xsiSchemaLocation = null;
                string xsiNoNamespaceSL = null;
                string xsiNil = null;
                string xsiType = null;
                if (_coreReader.MoveToFirstAttribute())
                {
                    do
                    {
                        string objectNs = _coreReader.NamespaceURI;
                        string objectName = _coreReader.LocalName;
                        if (Ref.Equal(objectNs, _nsXsi))
                        {
                            if (Ref.Equal(objectName, _xsiSchemaLocation))
                            {
                                xsiSchemaLocation = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiNoNamespaceSchemaLocation))
                            {
                                xsiNoNamespaceSL = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiType))
                            {
                                xsiType = _coreReader.Value;
                            }
                            else if (Ref.Equal(objectName, _xsiNil))
                            {
                                xsiNil = _coreReader.Value;
                            }
                        }
                        if (_manageNamespaces && Ref.Equal(_coreReader.NamespaceURI, _nsXmlNs))
                        {
                            _nsManager.AddNamespace(_coreReader.Prefix.Length == 0 ? string.Empty : _coreReader.LocalName, _coreReader.Value);
                        }
                    } while (_coreReader.MoveToNextAttribute());
                    _coreReader.MoveToElement();
                }
                _validator.ValidateElement(_coreReader.LocalName, _coreReader.NamespaceURI, _xmlSchemaInfo, xsiType, xsiNil, xsiSchemaLocation, xsiNoNamespaceSL);
                ValidateAttributes();
                _validator.ValidateEndOfAttributes(_xmlSchemaInfo);
                if (_coreReader.IsEmptyElement)
                {
                    await ProcessEndElementEventAsync().ConfigureAwait(false);
                }
                _validationState = ValidatingReaderState.ClearAttributes;
            }
        }

        private async Task ProcessEndElementEventAsync()
        {
            _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
            _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
            if (_xmlSchemaInfo.IsDefault)
            { //The atomicValue returned is a default value
                Debug.Assert(_atomicValue != null);
                int depth = _coreReader.Depth;
                _coreReader = GetCachingReader();
                _cachingReader.RecordTextNode(_xmlSchemaInfo.XmlType.ValueConverter.ToString(_atomicValue), _originalAtomicValueString, depth + 1, 0, 0);
                _cachingReader.RecordEndElementNode();
                await _cachingReader.SetToReplayModeAsync().ConfigureAwait(false);
                _replayCache = true;
            }
            else if (_manageNamespaces)
            {
                _nsManager.PopScope();
            }
        }

        private async Task ProcessInlineSchemaAsync()
        {
            Debug.Assert(_inlineSchemaParser != null);
            if (await _coreReader.ReadAsync().ConfigureAwait(false))
            {
                if (_coreReader.NodeType == XmlNodeType.Element)
                {
                    _attributeCount = _coreReaderAttributeCount = _coreReader.AttributeCount;
                }
                else
                { //Clear attributes info if nodeType is not element
                    ClearAttributesInfo();
                }
                if (!_inlineSchemaParser.ParseReaderNode())
                {
                    _inlineSchemaParser.FinishParsing();
                    XmlSchema schema = _inlineSchemaParser.XmlSchema;
                    _validator.AddSchema(schema);
                    _inlineSchemaParser = null;
                    _validationState = ValidatingReaderState.Read;
                }
            }
        }

        private Task<object> InternalReadContentAsObjectAsync()
        {
            return InternalReadContentAsObjectAsync(false);
        }

        private async Task<object> InternalReadContentAsObjectAsync(bool unwrapTypedValue)
        {
            var tuple_11 = await InternalReadContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(false);

            return tuple_11.Item2;
        }

        private async Task<Tuple<string, object>> InternalReadContentAsObjectTupleAsync(bool unwrapTypedValue)
        {
            Tuple<string, object> tuple;
            string originalStringValue;

            XmlNodeType nodeType = this.NodeType;
            if (nodeType == XmlNodeType.Attribute)
            {
                originalStringValue = this.Value;
                if (_attributePSVI != null && _attributePSVI.typedAttributeValue != null)
                {
                    if (_validationState == ValidatingReaderState.OnDefaultAttribute)
                    {
                        XmlSchemaAttribute schemaAttr = _attributePSVI.attributeSchemaInfo.SchemaAttribute;
                        originalStringValue = (schemaAttr.DefaultValue != null) ? schemaAttr.DefaultValue : schemaAttr.FixedValue;
                    }

                    tuple = new Tuple<string, object>(originalStringValue, ReturnBoxedValue(_attributePSVI.typedAttributeValue, AttributeSchemaInfo.XmlType, unwrapTypedValue));
                    return tuple;
                }
                else
                { //return string value
                    tuple = new Tuple<string, object>(originalStringValue, this.Value);
                    return tuple;
                }
            }
            else if (nodeType == XmlNodeType.EndElement)
            {
                if (_atomicValue != null)
                {
                    originalStringValue = _originalAtomicValueString;

                    tuple = new Tuple<string, object>(originalStringValue, _atomicValue);
                    return tuple;
                }
                else
                {
                    originalStringValue = string.Empty;

                    tuple = new Tuple<string, object>(originalStringValue, string.Empty);
                    return tuple;
                }
            }
            else
            { //Positioned on text, CDATA, PI, Comment etc
                if (_validator.CurrentContentType == XmlSchemaContentType.TextOnly)
                {  //if current element is of simple type
                    object value = ReturnBoxedValue(await ReadTillEndElementAsync().ConfigureAwait(false), _xmlSchemaInfo.XmlType, unwrapTypedValue);
                    originalStringValue = _originalAtomicValueString;

                    tuple = new Tuple<string, object>(originalStringValue, value);
                    return tuple;
                }
                else
                {
                    XsdCachingReader cachingReader = _coreReader as XsdCachingReader;
                    if (cachingReader != null)
                    {
                        originalStringValue = cachingReader.ReadOriginalContentAsString();
                    }
                    else
                    {
                        originalStringValue = await InternalReadContentAsStringAsync().ConfigureAwait(false);
                    }

                    tuple = new Tuple<string, object>(originalStringValue, originalStringValue);
                    return tuple;
                }
            }
        }

        private Task<Tuple<XmlSchemaType, object>> InternalReadElementContentAsObjectAsync()
        {
            return InternalReadElementContentAsObjectAsync(false);
        }

        private async Task<Tuple<XmlSchemaType, object>> InternalReadElementContentAsObjectAsync(bool unwrapTypedValue)
        {
            var tuple_13 = await InternalReadElementContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(false);

            return new Tuple<XmlSchemaType, object>(tuple_13.Item1, tuple_13.Item3);
        }

        private async Task<Tuple<XmlSchemaType, string, object>> InternalReadElementContentAsObjectTupleAsync(bool unwrapTypedValue)
        {
            Tuple<XmlSchemaType, string, object> tuple;
            XmlSchemaType xmlType;
            string originalString;

            Debug.Assert(this.NodeType == XmlNodeType.Element);
            object typedValue = null;
            xmlType = null;
            //If its an empty element, can have default/fixed value
            if (this.IsEmptyElement)
            {
                if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                {
                    typedValue = ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue);
                }
                else
                {
                    typedValue = _atomicValue;
                }
                originalString = _originalAtomicValueString;
                xmlType = ElementXmlType; //Set this for default values 
                await this.ReadAsync().ConfigureAwait(false);

                tuple = new Tuple<XmlSchemaType, string, object>(xmlType, originalString, typedValue);
                return tuple;
            }
            // move to content and read typed value
            await this.ReadAsync().ConfigureAwait(false);

            if (this.NodeType == XmlNodeType.EndElement)
            { //If IsDefault is true, the next node will be EndElement
                if (_xmlSchemaInfo.IsDefault)
                {
                    if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
                    {
                        typedValue = ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue);
                    }
                    else
                    { //anyType has default value
                        typedValue = _atomicValue;
                    }
                    originalString = _originalAtomicValueString;
                }
                else
                { //Empty content
                    typedValue = string.Empty;
                    originalString = string.Empty;
                }
            }
            else if (this.NodeType == XmlNodeType.Element)
            { //the first child is again element node
                throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
            }
            else
            {
                var tuple_14 = await InternalReadContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(false);
                originalString = tuple_14.Item1;

                typedValue = tuple_14.Item2;

                // ReadElementContentAsXXX cannot be called on mixed content, if positioned on node other than EndElement, Error
                if (this.NodeType != XmlNodeType.EndElement)
                {
                    throw new XmlException(SR.Xml_MixedReadElementContentAs, string.Empty, this as IXmlLineInfo);
                }
            }
            xmlType = ElementXmlType; //Set this as we are moving ahead to the next node

            // move to next node
            await this.ReadAsync().ConfigureAwait(false);

            tuple = new Tuple<XmlSchemaType, string, object>(xmlType, originalString, typedValue);
            return tuple;
        }

        private async Task<object> ReadTillEndElementAsync()
        {
            if (_atomicValue == null)
            {
                while (await _coreReader.ReadAsync().ConfigureAwait(false))
                {
                    if (_replayCache)
                    { //If replaying nodes in the cache, they have already been validated
                        continue;
                    }
                    switch (_coreReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            await ProcessReaderEventAsync().ConfigureAwait(false);
                            goto breakWhile;

                        case XmlNodeType.Text:
                        case XmlNodeType.CDATA:
                            _validator.ValidateText(GetStringValue);
                            break;

                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            _validator.ValidateWhitespace(GetStringValue);
                            break;

                        case XmlNodeType.Comment:
                        case XmlNodeType.ProcessingInstruction:
                            break;

                        case XmlNodeType.EndElement:
                            _atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
                            _originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
                            if (_manageNamespaces)
                            {
                                _nsManager.PopScope();
                            }
                            goto breakWhile;
                    }
                    continue;
                breakWhile:
                    break;
                }
            }
            else
            { //atomicValue != null, meaning already read ahead - Switch reader
                if (_atomicValue == this)
                { //switch back invalid marker; dont need it since coreReader moved to endElement
                    _atomicValue = null;
                }
                SwitchReader();
            }
            return _atomicValue;
        }
    }
}

