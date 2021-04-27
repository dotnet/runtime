// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Xml;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Xml
{
    //
    // XmlCharCheckingReaderWithNS
    //
    internal partial class XmlCharCheckingReader : XmlWrappingReader
    {
        //
        // Private types
        //
        private enum State
        {
            Initial,
            InReadBinary,
            Error,
            Interactive,  // Interactive means other than ReadState.Initial and ReadState.Error; still needs to call
                          // underlying XmlReader to find out if the reported ReadState should be Interactive or EndOfFile
        };

        //
        // Fields
        //
        private State _state;

        // settings
        private readonly bool _checkCharacters;
        private readonly bool _ignoreWhitespace;
        private readonly bool _ignoreComments;
        private readonly bool _ignorePis;
        private readonly DtdProcessing _dtdProcessing; // -1 means do nothing

        private XmlNodeType _lastNodeType;

        private ReadContentAsBinaryHelper? _readBinaryHelper;

        //
        // Constructor
        //
        internal XmlCharCheckingReader(XmlReader reader, bool checkCharacters, bool ignoreWhitespace, bool ignoreComments, bool ignorePis, DtdProcessing dtdProcessing)
            : base(reader)
        {
            Debug.Assert(checkCharacters || ignoreWhitespace || ignoreComments || ignorePis || (int)dtdProcessing != -1);

            _state = State.Initial;

            _checkCharacters = checkCharacters;
            _ignoreWhitespace = ignoreWhitespace;
            _ignoreComments = ignoreComments;
            _ignorePis = ignorePis;
            _dtdProcessing = dtdProcessing;

            _lastNodeType = XmlNodeType.None;
        }

        //
        // XmlReader implementation
        //
        public override XmlReaderSettings Settings
        {
            get
            {
                XmlReaderSettings? settings = reader.Settings;
                if (settings == null)
                {
                    settings = new XmlReaderSettings();
                }
                else
                {
                    settings = settings.Clone();
                }

                if (_checkCharacters)
                {
                    settings.CheckCharacters = true;
                }
                if (_ignoreWhitespace)
                {
                    settings.IgnoreWhitespace = true;
                }
                if (_ignoreComments)
                {
                    settings.IgnoreComments = true;
                }
                if (_ignorePis)
                {
                    settings.IgnoreProcessingInstructions = true;
                }
                if ((int)_dtdProcessing != -1)
                {
                    settings.DtdProcessing = _dtdProcessing;
                }
                settings.ReadOnly = true;
                return settings;
            }
        }

        public override bool MoveToAttribute(string name)
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string? ns)
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.MoveToAttribute(name, ns);
        }

        public override void MoveToAttribute(int i)
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            base.reader.MoveToAttribute(i);
        }

        public override bool MoveToFirstAttribute()
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.MoveToNextAttribute();
        }

        public override bool MoveToElement()
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.MoveToElement();
        }

        public override bool Read()
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
                    FinishReadBinary();
                    _state = State.Interactive;
                    goto case State.Interactive;

                case State.Interactive:
                    if (!base.reader.Read())
                    {
                        return false;
                    }
                    break;

                default:
                    Debug.Fail($"Unexpected state {_state}");
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
                            return Read();
                        }
                        break;
                    case XmlNodeType.Whitespace:
                        if (_ignoreWhitespace)
                        {
                            return Read();
                        }
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        if (_ignorePis)
                        {
                            return Read();
                        }
                        break;
                    case XmlNodeType.DocumentType:
                        if (_dtdProcessing == DtdProcessing.Prohibit)
                        {
                            Throw(SR.Xml_DtdIsProhibitedEx, string.Empty);
                        }
                        else if (_dtdProcessing == DtdProcessing.Ignore)
                        {
                            return Read();
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
                            CheckCharacters(base.reader.Value);
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
                            return Read();
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
                            return Read();
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
                            return Read();
                        }
                        if (_checkCharacters)
                        {
                            ValidateQName(base.reader.Name);
                            CheckCharacters(base.reader.Value);

                            string? str;
                            str = base.reader.GetAttribute("SYSTEM");
                            if (str != null)
                            {
                                CheckCharacters(str);
                            }

                            str = base.reader.GetAttribute("PUBLIC");
                            if (str != null)
                            {
                                int i;
                                if ((i = XmlCharType.IsPublicId(str)) >= 0)
                                {
                                    Throw(SR.Xml_InvalidCharacter, XmlException.BuildCharExceptionArgs(str, i));
                                }
                            }
                        }
                        break;

                    case XmlNodeType.Whitespace:
                        if (_ignoreWhitespace)
                        {
                            return Read();
                        }
                        if (_checkCharacters)
                        {
                            CheckWhitespace(base.reader.Value);
                        }
                        break;

                    case XmlNodeType.SignificantWhitespace:
                        if (_checkCharacters)
                        {
                            CheckWhitespace(base.reader.Value);
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

        public override ReadState ReadState
        {
            get
            {
                switch (_state)
                {
                    case State.Initial:
                        return base.reader.ReadState == ReadState.Closed ? ReadState.Closed : ReadState.Initial;
                    case State.Error:
                        return ReadState.Error;
                    case State.InReadBinary:
                    case State.Interactive:
                    default:
                        return base.reader.ReadState;
                }
            }
        }

        public override bool ReadAttributeValue()
        {
            if (_state == State.InReadBinary)
            {
                FinishReadBinary();
            }
            return base.reader.ReadAttributeValue();
        }

        public override bool CanReadBinaryContent
        {
            get
            {
                return true;
            }
        }

        public override int ReadContentAsBase64(byte[] buffer, int index, int count)
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
                    return base.ReadContentAsBase64(buffer, index, count);
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
                    return base.ReadContentAsBase64(buffer, index, count);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
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
                    return base.ReadContentAsBinHex(buffer, index, count);
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
                    return base.ReadContentAsBinHex(buffer, index, count);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
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
                    return base.ReadElementContentAsBase64(buffer, index, count);
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
                    return base.ReadElementContentAsBase64(buffer, index, count);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
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
                    return base.ReadElementContentAsBinHex(buffer, index, count);
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
                    return base.ReadElementContentAsBinHex(buffer, index, count);
                }
            }

            // turn off InReadBinary state in order to have a normal Read() behavior when called from readBinaryHelper
            _state = State.Interactive;

            // call to the helper
            int readCount = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);

            // turn on InReadBinary in again and return
            _state = State.InReadBinary;
            return readCount;
        }

        //
        // Private methods and properties
        //

        private void Throw(string res, string arg)
        {
            _state = State.Error;
            throw new XmlException(res, arg, (IXmlLineInfo?)null);
        }

        private void Throw(string res, string[] args)
        {
            _state = State.Error;
            throw new XmlException(res, args, (IXmlLineInfo?)null);
        }

        private void CheckWhitespace(string value)
        {
            int i;
            if ((i = XmlCharType.IsOnlyWhitespaceWithPos(value)) != -1)
            {
                Throw(SR.Xml_InvalidWhitespaceCharacter, XmlException.BuildCharExceptionArgs(value, i));
            }
        }

        private void ValidateQName(string name)
        {
            string prefix, localName;
            ValidateNames.ParseQNameThrow(name, out prefix, out localName);
        }

        private void ValidateQName(string prefix, string localName)
        {
            try
            {
                if (prefix.Length > 0)
                {
                    ValidateNames.ParseNCNameThrow(prefix);
                }
                ValidateNames.ParseNCNameThrow(localName);
            }
            catch
            {
                _state = State.Error;
                throw;
            }
        }

        private void CheckCharacters(string value)
        {
            XmlConvert.VerifyCharData(value, ExceptionType.ArgumentException, ExceptionType.XmlException);
        }

        private void FinishReadBinary()
        {
            _state = State.Interactive;
            if (_readBinaryHelper != null)
            {
                _readBinaryHelper.Finish();
            }
        }
    }

    //
    // XmlCharCheckingReaderWithNS
    //
    internal sealed class XmlCharCheckingReaderWithNS : XmlCharCheckingReader, IXmlNamespaceResolver
    {
        internal IXmlNamespaceResolver readerAsNSResolver;

        internal XmlCharCheckingReaderWithNS(XmlReader reader, IXmlNamespaceResolver readerAsNSResolver, bool checkCharacters, bool ignoreWhitespace, bool ignoreComments, bool ignorePis, DtdProcessing dtdProcessing)
            : base(reader, checkCharacters, ignoreWhitespace, ignoreComments, ignorePis, dtdProcessing)
        {
            Debug.Assert(readerAsNSResolver != null);
            this.readerAsNSResolver = readerAsNSResolver;
        }
        //
        // IXmlNamespaceResolver
        //
        IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
        {
            return readerAsNSResolver.GetNamespacesInScope(scope);
        }

        string? IXmlNamespaceResolver.LookupNamespace(string prefix)
        {
            return readerAsNSResolver.LookupNamespace(prefix);
        }

        string? IXmlNamespaceResolver.LookupPrefix(string namespaceName)
        {
            return readerAsNSResolver.LookupPrefix(namespaceName);
        }
    }
}
