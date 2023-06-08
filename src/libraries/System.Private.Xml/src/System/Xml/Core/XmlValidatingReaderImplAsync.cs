// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    await _readBinaryHelper!.FinishAsync().ConfigureAwait(false);
                    goto case ParsingFunction.Read;
                default:
                    Debug.Fail($"Unexpected parsing function {_parsingFunction}");
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
            int readCount = await _readBinaryHelper!.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

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
            int readCount = await _readBinaryHelper!.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

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
            int readCount = await _readBinaryHelper!.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

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
            int readCount = await _readBinaryHelper!.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // setup parsingFunction
            _parsingFunction = ParsingFunction.InReadBinaryContent;
            return readCount;
        }

        //
        // Private implementation methods
        //

        private async Task ParseDtdFromParserContextAsync()
        {
            Debug.Assert(_parserContext != null);
            Debug.Assert(_coreReaderImpl.DtdInfo == null);

            if (string.IsNullOrEmpty(_parserContext.DocTypeName))
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
