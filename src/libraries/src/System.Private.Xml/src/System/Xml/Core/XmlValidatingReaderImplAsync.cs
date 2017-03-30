// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Xml.Schema;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Versioning;

using System.Threading.Tasks;

namespace System.Xml
{
    internal sealed partial class XmlValidatingReaderImpl : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
    {
        // Returns the text value of the current node.
        public override Task<string> GetValueAsync()
        {
            return _coreReader.GetValueAsync();
        }

        // Reads and validated next node from the input data
        public override async Task<bool> ReadAsync()
        {
            switch (_parsingFunction)
            {
                case ParsingFunction.Read:
                    if (await _coreReader.ReadAsync().ConfigureAwait(false))
                    {
                        ProcessCoreReaderEvent();
                        return true;
                    }
                    else
                    {
                        _validator.CompleteValidation();
                        return false;
                    }
                case ParsingFunction.ParseDtdFromContext:
                    _parsingFunction = ParsingFunction.Read;
                    await ParseDtdFromParserContextAsync().ConfigureAwait(false);
                    goto case ParsingFunction.Read;
                case ParsingFunction.Error:
                case ParsingFunction.ReaderClosed:
                    return false;
                case ParsingFunction.Init:
                    _parsingFunction = ParsingFunction.Read; // this changes the value returned by ReadState
                    if (_coreReader.ReadState == ReadState.Interactive)
                    {
                        ProcessCoreReaderEvent();
                        return true;
                    }
                    else
                    {
                        goto case ParsingFunction.Read;
                    }
                case ParsingFunction.ResolveEntityInternally:
                    _parsingFunction = ParsingFunction.Read;
                    await ResolveEntityInternallyAsync().ConfigureAwait(false);
                    goto case ParsingFunction.Read;
                case ParsingFunction.InReadBinaryContent:
                    _parsingFunction = ParsingFunction.Read;
                    await _readBinaryHelper.FinishAsync().ConfigureAwait(false);
                    goto case ParsingFunction.Read;
                default:
                    Debug.Assert(false);
                    return false;
            }
        }

        public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper if called the first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper when called first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper if called the first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            // init ReadChunkHelper when called first time
            if (_parsingFunction != ParsingFunction.InReadBinaryContent)
            {
                _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
            }

            // set parsingFunction to Read state in order to have a normal Read() behavior when called from readBinaryHelper
            _parsingFunction = ParsingFunction.Read;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // setup parsingFunction 
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        internal async Task MoveOffEntityReferenceAsync()
        {
            if (_outerReader.NodeType == XmlNodeType.EntityReference && _parsingFunction != ParsingFunction.ResolveEntityInternally)
            {
                if (!await _outerReader.ReadAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                }
            }
        }

        // Returns typed value of the current node (based on the type specified by schema)
        public async Task<object> ReadTypedValueAsync()
        {
            if (_validationType == ValidationType.None)
            {
                return null;
            }

            switch (_outerReader.NodeType)
            {
                case XmlNodeType.Attribute:
                    return _coreReaderImpl.InternalTypedValue;
                case XmlNodeType.Element:
                    if (SchemaType == null)
                    {
                        return null;
                    }
                    XmlSchemaDatatype dtype = (SchemaType is XmlSchemaDatatype) ? (XmlSchemaDatatype)SchemaType : ((XmlSchemaType)SchemaType).Datatype;
                    if (dtype != null)
                    {
                        if (!_outerReader.IsEmptyElement)
                        {
                            for (;;)
                            {
                                if (!await _outerReader.ReadAsync().ConfigureAwait(false))
                                {
                                    throw new InvalidOperationException(SR.Xml_InvalidOperation);
                                }
                                XmlNodeType type = _outerReader.NodeType;
                                if (type != XmlNodeType.CDATA && type != XmlNodeType.Text &&
                                    type != XmlNodeType.Whitespace && type != XmlNodeType.SignificantWhitespace &&
                                    type != XmlNodeType.Comment && type != XmlNodeType.ProcessingInstruction)
                                {
                                    break;
                                }
                            }
                            if (_outerReader.NodeType != XmlNodeType.EndElement)
                            {
                                throw new XmlException(SR.Xml_InvalidNodeType, _outerReader.NodeType.ToString());
                            }
                        }
                        return _coreReaderImpl.InternalTypedValue;
                    }
                    return null;

                case XmlNodeType.EndElement:
                    return null;

                default:
                    if (_coreReaderImpl.V1Compat)
                    { //If v1 XmlValidatingReader return null
                        return null;
                    }
                    else
                    {
                        return await GetValueAsync().ConfigureAwait(false);
                    }
            }
        }

        //
        // Private implementation methods
        //

        private async Task ParseDtdFromParserContextAsync()
        {
            Debug.Assert(_parserContext != null);
            Debug.Assert(_coreReaderImpl.DtdInfo == null);

            if (_parserContext.DocTypeName == null || _parserContext.DocTypeName.Length == 0)
            {
                return;
            }

            IDtdParser dtdParser = DtdParser.Create();
            XmlTextReaderImpl.DtdParserProxy proxy = new XmlTextReaderImpl.DtdParserProxy(_coreReaderImpl);
            IDtdInfo dtdInfo = await dtdParser.ParseFreeFloatingDtdAsync(_parserContext.BaseURI, _parserContext.DocTypeName, _parserContext.PublicId, _parserContext.SystemId, _parserContext.InternalSubset, proxy).ConfigureAwait(false);
            _coreReaderImpl.SetDtdInfo(dtdInfo);

            ValidateDtd();
        }

        private async Task ResolveEntityInternallyAsync()
        {
            Debug.Assert(_coreReader.NodeType == XmlNodeType.EntityReference);
            int initialDepth = _coreReader.Depth;
            _outerReader.ResolveEntity();
            while (await _outerReader.ReadAsync().ConfigureAwait(false) && _coreReader.Depth > initialDepth) ;
        }
    }
}

