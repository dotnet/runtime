// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

namespace System.Xml
{
    //
    // XmlCharCheckingReaderWithNS
    //
    internal partial class XmlCharCheckingReader : XmlWrappingReader
    {
        public override async Task<bool> ReadAsync()
        {
            switch (_state)
            {
                case State.Initial:
                    _state = State.Interactive;
                    if (base.reader.ReadState == ReadState.Initial)
                    {
                        goto case State.Interactive;
                    }
                    break;

                case State.Error:
                    return false;

                case State.InReadBinary:
                    await FinishReadBinaryAsync().ConfigureAwait(false);
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.Interactive:
                    if (!await base.reader.ReadAsync().ConfigureAwait(false))
                    {
                        return false;
                    }
                    break;

                default:
                    Debug.Assert(false);
                    return false;
            }

            XmlNodeType nodeType = base.reader.NodeType;

            if (!_checkCharacters)
            {
                switch (nodeType)
                {
                    case XmlNodeType.Comment:
                        if (_ignoreComments)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    case XmlNodeType.Whitespace:
                        if (_ignoreWhitespace)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        if (_ignorePis)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        break;
                    case XmlNodeType.DocumentType:
                        if (_dtdProcessing == DtdProcessing.Prohibit)
                        {
                            Throw(SR.Xml_DtdIsProhibitedEx, string.Empty);
                        }
                        else if (_dtdProcessing == DtdProcessing.Ignore)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        break;
                }
                return true;
            }
            else
            {
                switch (nodeType)
                {
                    case XmlNodeType.Element:
                        if (_checkCharacters)
                        {
                            // check element name
                            ValidateQName(base.reader.Prefix, base.reader.LocalName);

                            // check values of attributes
                            if (base.reader.MoveToFirstAttribute())
                            {
                                do
                                {
                                    ValidateQName(base.reader.Prefix, base.reader.LocalName);
                                    CheckCharacters(base.reader.Value);
                                } while (base.reader.MoveToNextAttribute());

                                base.reader.MoveToElement();
                            }
                        }
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        if (_checkCharacters)
                        {
                            CheckCharacters(await base.reader.GetValueAsync().ConfigureAwait(false));
                        }
                        break;

                    case XmlNodeType.EntityReference:
                        if (_checkCharacters)
                        {
                            // check name
                            ValidateQName(base.reader.Name);
                        }
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        if (_ignorePis)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        if (_checkCharacters)
                        {
                            ValidateQName(base.reader.Name);
                            CheckCharacters(base.reader.Value);
                        }
                        break;

                    case XmlNodeType.Comment:
                        if (_ignoreComments)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        if (_checkCharacters)
                        {
                            CheckCharacters(base.reader.Value);
                        }
                        break;

                    case XmlNodeType.DocumentType:
                        if (_dtdProcessing == DtdProcessing.Prohibit)
                        {
                            Throw(SR.Xml_DtdIsProhibitedEx, string.Empty);
                        }
                        else if (_dtdProcessing == DtdProcessing.Ignore)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        if (_checkCharacters)
                        {
                            ValidateQName(base.reader.Name);
                            CheckCharacters(base.reader.Value);

                            string str;
                            str = base.reader.GetAttribute("SYSTEM");
                            if (str != null)
                            {
                                CheckCharacters(str);
                            }

                            str = base.reader.GetAttribute("PUBLIC");
                            if (str != null)
                            {
                                int i;
                                if ((i = _xmlCharType.IsPublicId(str)) >= 0)
                                {
                                    Throw(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(str, i));
                                }
                            }
                        }
                        break;

                    case XmlNodeType.Whitespace:
                        if (_ignoreWhitespace)
                        {
                            return await ReadAsync().ConfigureAwait(false);
                        }
                        if (_checkCharacters)
                        {
                            CheckWhitespace(await base.reader.GetValueAsync().ConfigureAwait(false));
                        }
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        if (_checkCharacters)
                        {
                            CheckWhitespace(await base.reader.GetValueAsync().ConfigureAwait(false));
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (_checkCharacters)
                        {
                            ValidateQName(base.reader.Prefix, base.reader.LocalName);
                        }
                        break;

                    default:
                        break;
                }
                _lastNodeType = nodeType;
                return true;
            }
        }

        public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            if (_state != State.InReadBinary)
            {
                // forward ReadBase64Chunk calls into the base (wrapped) reader if possible, i.e. if it can read binary and we 
                // should not check characters
                if (base.CanReadBinaryContent && (!_checkCharacters))
                {
                    _readBinaryHelper = null;
                    _state = State.InReadBinary;
                    return await base.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore whitespace
                else
                {
                    _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                }
            }
            else
            {
                // forward calls into wrapped reader 
                if (_readBinaryHelper == null)
                {
                    return await base.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            if (_state != State.InReadBinary)
            {
                // forward ReadBinHexChunk calls into the base (wrapped) reader if possible, i.e. if it can read chunks and we 
                // should not check characters
                if (base.CanReadBinaryContent && (!_checkCharacters))
                {
                    _readBinaryHelper = null;
                    _state = State.InReadBinary;
                    return await base.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore whitespace
                else
                {
                    _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                }
            }
            else
            {
                // forward calls into wrapped reader 
                if (_readBinaryHelper == null)
                {
                    return await base.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            if (_state != State.InReadBinary)
            {
                // forward ReadBase64Chunk calls into the base (wrapped) reader if possible, i.e. if it can read binary and we 
                // should not check characters
                if (base.CanReadBinaryContent && (!_checkCharacters))
                {
                    _readBinaryHelper = null;
                    _state = State.InReadBinary;
                    return await base.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore whitespace
                else
                {
                    _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                }
            }
            else
            {
                // forward calls into wrapped reader 
                if (_readBinaryHelper == null)
                {
                    return await base.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(false);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
        {
            // check arguments
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (ReadState != ReadState.Interactive)
            {
                return 0;
            }

            if (_state != State.InReadBinary)
            {
                // forward ReadBinHexChunk calls into the base (wrapped) reader if possible, i.e. if it can read chunks and we 
                // should not check characters
                if (base.CanReadBinaryContent && (!_checkCharacters))
                {
                    _readBinaryHelper = null;
                    _state = State.InReadBinary;
                    return await base.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                }
                // the wrapped reader cannot read chunks or we are on an element where we should check characters or ignore whitespace
                else
                {
                    _readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
                }
            }
            else
            {
                // forward calls into wrapped reader 
                if (_readBinaryHelper == null)
                {
                    return await base.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(false);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        private async Task FinishReadBinaryAsync()
        {
            _state = State.Interactive;
            if (_readBinaryHelper != null)
            {
                await _readBinaryHelper.FinishAsync().ConfigureAwait(false);
            }
        }
    }
}
