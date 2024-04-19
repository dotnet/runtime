// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Xml
{
    internal static class XmlExceptionHelper
    {
        [DoesNotReturn]
        private static void ThrowXmlException(XmlDictionaryReader reader, string res)
        {
            ThrowXmlException(reader, res, null);
        }

        [DoesNotReturn]
        public static void ThrowXmlException(XmlDictionaryReader reader, string res, string? arg1)
        {
            ThrowXmlException(reader, res, arg1, null);
        }

        [DoesNotReturn]
        private static void ThrowXmlException(XmlDictionaryReader reader, string res, string? arg1, string? arg2)
        {
            ThrowXmlException(reader, res, arg1, arg2, null);
        }

        [DoesNotReturn]
        private static void ThrowXmlException(XmlDictionaryReader reader, string res, string? arg1, string? arg2, string? arg3)
        {
            string s = SR.Format(res, arg1, arg2, arg3);
            if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
            }

            throw new XmlException(s);
        }

        [DoesNotReturn]
        public static void ThrowXmlException(XmlDictionaryReader reader, XmlException exception)
        {
            string s = exception.Message;
            if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
            }
            throw new XmlException(s);
        }

        private static string GetName(string prefix, string localName)
        {
            if (prefix.Length == 0)
                return localName;
            else
                return string.Concat(prefix, ":", localName);
        }

        private static string GetWhatWasFound(XmlDictionaryReader reader)
        {
            if (reader.EOF)
                return SR.XmlFoundEndOfFile;
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    return SR.Format(SR.XmlFoundElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                case XmlNodeType.EndElement:
                    return SR.Format(SR.XmlFoundEndElement, GetName(reader.Prefix, reader.LocalName), reader.NamespaceURI);
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return SR.Format(SR.XmlFoundText, reader.Value);
                case XmlNodeType.Comment:
                    return SR.Format(SR.XmlFoundComment, reader.Value);
                case XmlNodeType.CDATA:
                    return SR.Format(SR.XmlFoundCData, reader.Value);
            }
            return SR.Format(SR.XmlFoundNodeType, reader.NodeType);
        }

        [DoesNotReturn]
        public static void ThrowStartElementExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowStartElementExpected(XmlDictionaryReader reader, string name)
        {
            ThrowXmlException(reader, SR.XmlStartElementNameExpected, name, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            ThrowStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
        }

        [DoesNotReturn]
        public static void ThrowFullStartElementExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementExpected, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, string name)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementNameExpected, name, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowFullStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            ThrowFullStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
        }

        [DoesNotReturn]
        public static void ThrowEndElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlEndElementExpected, localName, ns, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowMaxArrayLengthExceeded(XmlDictionaryReader reader, int maxArrayLength)
        {
            ThrowXmlException(reader, SR.XmlMaxArrayLengthExceeded, maxArrayLength.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowMaxArrayLengthOrMaxItemsQuotaExceeded(XmlDictionaryReader reader, int maxQuota)
        {
            ThrowXmlException(reader, SR.XmlMaxArrayLengthOrMaxItemsQuotaExceeded, maxQuota.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowMaxBytesPerReadExceeded(XmlDictionaryReader reader, int maxBytesPerRead)
        {
            ThrowXmlException(reader, SR.XmlMaxBytesPerReadExceeded, maxBytesPerRead.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowMaxDepthExceeded(XmlDictionaryReader reader, int maxDepth)
        {
            ThrowXmlException(reader, SR.XmlMaxDepthExceeded, maxDepth.ToString());
        }

        [DoesNotReturn]
        public static void ThrowMaxStringContentLengthExceeded(XmlDictionaryReader reader, int maxStringContentLength)
        {
            ThrowXmlException(reader, SR.XmlMaxStringContentLengthExceeded, maxStringContentLength.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowMaxNameTableCharCountExceeded(XmlDictionaryReader reader, int maxNameTableCharCount)
        {
            ThrowXmlException(reader, SR.XmlMaxNameTableCharCountExceeded, maxNameTableCharCount.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowBase64DataExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlBase64DataExpected, GetWhatWasFound(reader));
        }

        [DoesNotReturn]
        public static void ThrowUndefinedPrefix(XmlDictionaryReader reader, string prefix)
        {
            ThrowXmlException(reader, SR.XmlUndefinedPrefix, prefix);
        }

        [DoesNotReturn]
        public static void ThrowProcessingInstructionNotSupported(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlProcessingInstructionNotSupported);
        }

        [DoesNotReturn]
        public static void ThrowInvalidXml(XmlDictionaryReader reader, byte b)
        {
            ThrowXmlException(reader, SR.XmlInvalidXmlByte, b.ToString("X2", CultureInfo.InvariantCulture));
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedEndOfFile(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlUnexpectedEndOfFile, ((XmlBaseReader)reader).GetOpenElements());
        }

        [DoesNotReturn]
        public static void ThrowUnexpectedEndElement(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlUnexpectedEndElement);
        }

        [DoesNotReturn]
        public static void ThrowTokenExpected(XmlDictionaryReader reader, string expected, char found)
        {
            ThrowXmlException(reader, SR.XmlTokenExpected, expected, found.ToString());
        }

        [DoesNotReturn]
        public static void ThrowTokenExpected(XmlDictionaryReader reader, string expected, string found)
        {
            ThrowXmlException(reader, SR.XmlTokenExpected, expected, found);
        }

        [DoesNotReturn]
        public static void ThrowInvalidCharRef(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidCharRef);
        }

        [DoesNotReturn]
        public static void ThrowTagMismatch(XmlDictionaryReader reader, string expectedPrefix, string expectedLocalName, string foundPrefix, string foundLocalName)
        {
            ThrowXmlException(reader, SR.XmlTagMismatch, GetName(expectedPrefix, expectedLocalName), GetName(foundPrefix, foundLocalName));
        }

        [DoesNotReturn]
        public static void ThrowDuplicateXmlnsAttribute(XmlDictionaryReader reader, string localName, string ns)
        {
            string name;
            if (localName.Length == 0)
                name = "xmlns";
            else
                name = "xmlns:" + localName;
            ThrowXmlException(reader, SR.XmlDuplicateAttribute, name, name, ns);
        }

        [DoesNotReturn]
        public static void ThrowDuplicateAttribute(XmlDictionaryReader reader, string prefix1, string prefix2, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlDuplicateAttribute, GetName(prefix1, localName), GetName(prefix2, localName), ns);
        }

        [DoesNotReturn]
        public static void ThrowInvalidBinaryFormat(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidFormat);
        }

        [DoesNotReturn]
        public static void ThrowInvalidRootData(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidRootData);
        }

        [DoesNotReturn]
        public static void ThrowMultipleRootElements(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlMultipleRootElements);
        }

        [DoesNotReturn]
        public static void ThrowDeclarationNotFirst(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlDeclNotFirst);
        }

        [DoesNotReturn]
        public static void ThrowConversionOverflow(XmlDictionaryReader reader, string value, string type)
        {
            ThrowXmlException(reader, SR.XmlConversionOverflow, value, type);
        }

        [DoesNotReturn]
        public static void ThrowXmlDictionaryStringIDOutOfRange(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDRange, XmlDictionaryString.MinKey.ToString(NumberFormatInfo.CurrentInfo), XmlDictionaryString.MaxKey.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowXmlDictionaryStringIDUndefinedStatic(XmlDictionaryReader reader, int key)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDUndefinedStatic, key.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowXmlDictionaryStringIDUndefinedSession(XmlDictionaryReader reader, int key)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDUndefinedSession, key.ToString(NumberFormatInfo.CurrentInfo));
        }

        [DoesNotReturn]
        public static void ThrowEmptyNamespace(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlEmptyNamespaceRequiresNullPrefix);
        }

        public static XmlException CreateConversionException(string type, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidConversionWithoutValue, type), exception);
        }

        public static XmlException CreateConversionException(string value, string type, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidConversion, value, type), exception);
        }

        public static XmlException CreateEncodingException(byte[] buffer, int offset, int count, Exception exception)
        {
            return CreateEncodingException(new System.Text.UTF8Encoding(false, false).GetString(buffer, offset, count), exception);
        }

        public static XmlException CreateEncodingException(string value, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidUTF8Bytes, value), exception);
        }
    }
}
