// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml
{
    //
    // XmlCharCheckingWriter
    //
    internal sealed partial class XmlCharCheckingWriter : XmlWrappingWriter
    {
        public override async Task WriteDocTypeAsync(string name, string? pubid, string? sysid, string? subset)
        {
            if (_checkNames)
            {
                ValidateQName(name);
            }

            if (_checkValues)
            {
                if (pubid != null)
                {
                    int i;
                    if ((i = XmlCharType.IsPublicId(pubid)) >= 0)
                    {
                        throw XmlConvert.CreateInvalidCharException(pubid, i);
                    }
                }

                if (sysid != null)
                {
                    CheckCharacters(sysid);
                }

                if (subset != null)
                {
                    CheckCharacters(subset);
                }
            }

            if (_replaceNewLines)
            {
                sysid = ReplaceNewLines(sysid);
                pubid = ReplaceNewLines(pubid);
                subset = ReplaceNewLines(subset);
            }

            await writer.WriteDocTypeAsync(name, pubid, sysid, subset).ConfigureAwait(false);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName, string? ns)
        {
            if (_checkNames)
            {
                ArgumentException.ThrowIfNullOrEmpty(localName);

                ValidateNCName(localName);

                if (prefix != null && prefix.Length > 0)
                {
                    ValidateNCName(prefix);
                }
            }
            await writer.WriteStartElementAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        protected internal override async Task WriteStartAttributeAsync(string? prefix, string localName, string? ns)
        {
            if (_checkNames)
            {
                ArgumentException.ThrowIfNullOrEmpty(localName);

                ValidateNCName(localName);

                if (prefix != null && prefix.Length > 0)
                {
                    ValidateNCName(prefix);
                }
            }

            await writer.WriteStartAttributeAsync(prefix, localName, ns).ConfigureAwait(false);
        }

        public override async Task WriteCDataAsync(string? text)
        {
            if (text != null)
            {
                if (_checkValues)
                {
                    CheckCharacters(text);
                }

                if (_replaceNewLines)
                {
                    text = ReplaceNewLines(text);
                }

                int i;
                while ((i = text.IndexOf("]]>", StringComparison.Ordinal)) >= 0)
                {
                    await writer.WriteCDataAsync(text.Substring(0, i + 2)).ConfigureAwait(false);
                    text = text.Substring(i + 2);
                }
            }

            await writer.WriteCDataAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteCommentAsync(string? text)
        {
            if (text != null)
            {
                if (_checkValues)
                {
                    CheckCharacters(text);
                    text = InterleaveInvalidChars(text, '-', '-');
                }
                if (_replaceNewLines)
                {
                    text = ReplaceNewLines(text);
                }
            }
            await writer.WriteCommentAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteProcessingInstructionAsync(string name, string? text)
        {
            if (_checkNames)
            {
                ValidateNCName(name);
            }

            if (text != null)
            {
                if (_checkValues)
                {
                    CheckCharacters(text);
                    text = InterleaveInvalidChars(text, '?', '>');
                }
                if (_replaceNewLines)
                {
                    text = ReplaceNewLines(text);
                }
            }

            await writer.WriteProcessingInstructionAsync(name, text!).ConfigureAwait(false);
        }

        public override async Task WriteEntityRefAsync(string name)
        {
            if (_checkNames)
            {
                ValidateQName(name);
            }
            await writer.WriteEntityRefAsync(name).ConfigureAwait(false);
        }

        public override async Task WriteWhitespaceAsync(string? ws)
        {
            ws ??= string.Empty;

            // "checkNames" is intentional here; if false, the whitespace is checked in XmlWellformedWriter
            if (_checkNames)
            {
                int i;
                if ((i = XmlCharType.IsOnlyWhitespaceWithPos(ws)) != -1)
                {
                    throw new ArgumentException(SR.Format(SR.Xml_InvalidWhitespaceCharacter, XmlException.BuildCharExceptionArgs(ws, i)));
                }
            }

            if (_replaceNewLines)
            {
                ws = ReplaceNewLines(ws);
            }

            await writer.WriteWhitespaceAsync(ws).ConfigureAwait(false);
        }

        public override async Task WriteStringAsync(string? text)
        {
            if (text != null)
            {
                if (_checkValues)
                {
                    CheckCharacters(text);
                }

                if (_replaceNewLines && WriteState != WriteState.Attribute)
                {
                    text = ReplaceNewLines(text);
                }
            }

            await writer.WriteStringAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteSurrogateCharEntityAsync(char lowChar, char highChar)
        {
            await writer.WriteSurrogateCharEntityAsync(lowChar, highChar).ConfigureAwait(false);
        }

        public override async Task WriteCharsAsync(char[] buffer, int index, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - index);

            if (_checkValues)
            {
                CheckCharacters(buffer, index, count);
            }

            if (_replaceNewLines && WriteState != WriteState.Attribute)
            {
                string? text = ReplaceNewLines(buffer, index, count);
                if (text != null)
                {
                    await WriteStringAsync(text).ConfigureAwait(false);
                }
            }

            await writer.WriteCharsAsync(buffer, index, count).ConfigureAwait(false);
        }

        public override async Task WriteNmTokenAsync(string name)
        {
            if (_checkNames)
            {
                ArgumentException.ThrowIfNullOrEmpty(name);
                XmlConvert.VerifyNMTOKEN(name);
            }
            await writer.WriteNmTokenAsync(name).ConfigureAwait(false);
        }

        public override async Task WriteNameAsync(string name)
        {
            if (_checkNames)
            {
                XmlConvert.VerifyQName(name, ExceptionType.XmlException);
            }
            await writer.WriteNameAsync(name).ConfigureAwait(false);
        }

        public override async Task WriteQualifiedNameAsync(string localName, string? ns)
        {
            if (_checkNames)
            {
                ValidateNCName(localName);
            }

            await writer.WriteQualifiedNameAsync(localName, ns).ConfigureAwait(false);
        }
    }
}
