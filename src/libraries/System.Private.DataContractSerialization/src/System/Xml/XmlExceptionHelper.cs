// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
//------------------------------------------------------------

using System.Runtime.Serialization;
using System.Globalization;

namespace System.Xml
{
    internal static class XmlExceptionHelper
    {
        private static void ThrowXmlException(XmlDictionaryReader reader, string res)
        {
            ThrowXmlException(reader, res, null);
        }

        static public void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1)
        {
            ThrowXmlException(reader, res, arg1, null);
        }

        private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1, string arg2)
        {
            ThrowXmlException(reader, res, arg1, arg2, null);
        }

        private static void ThrowXmlException(XmlDictionaryReader reader, string res, string arg1, string arg2, string arg3)
        {
            string s = SR.Format(res, arg1, arg2, arg3);
            IXmlLineInfo lineInfo = reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
            }

            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(s));
        }

        static public void ThrowXmlException(XmlDictionaryReader reader, XmlException exception)
        {
            string s = exception.Message;
            IXmlLineInfo lineInfo = reader as IXmlLineInfo;
            if (lineInfo != null && lineInfo.HasLineInfo())
            {
                s += " " + SR.Format(SR.XmlLineInfo, lineInfo.LineNumber, lineInfo.LinePosition);
            }
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(s));
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
                return SR.Format(SR.XmlFoundEndOfFile);
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

        static public void ThrowStartElementExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlStartElementExpected, GetWhatWasFound(reader));
        }

        static public void ThrowStartElementExpected(XmlDictionaryReader reader, string name)
        {
            ThrowXmlException(reader, SR.XmlStartElementNameExpected, name, GetWhatWasFound(reader));
        }

        static public void ThrowStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
        }

        static public void ThrowStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            ThrowStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
        }

        static public void ThrowFullStartElementExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementExpected, GetWhatWasFound(reader));
        }

        static public void ThrowFullStartElementExpected(XmlDictionaryReader reader, string name)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementNameExpected, name, GetWhatWasFound(reader));
        }

        static public void ThrowFullStartElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlFullStartElementLocalNameNsExpected, localName, ns, GetWhatWasFound(reader));
        }

        static public void ThrowFullStartElementExpected(XmlDictionaryReader reader, XmlDictionaryString localName, XmlDictionaryString ns)
        {
            ThrowFullStartElementExpected(reader, XmlDictionaryString.GetString(localName), XmlDictionaryString.GetString(ns));
        }

        static public void ThrowEndElementExpected(XmlDictionaryReader reader, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlEndElementExpected, localName, ns, GetWhatWasFound(reader));
        }

        static public void ThrowMaxArrayLengthExceeded(XmlDictionaryReader reader, int maxArrayLength)
        {
            ThrowXmlException(reader, SR.XmlMaxArrayLengthExceeded, maxArrayLength.ToString(NumberFormatInfo.CurrentInfo));
        }


        static public void ThrowMaxBytesPerReadExceeded(XmlDictionaryReader reader, int maxBytesPerRead)
        {
            ThrowXmlException(reader, SR.XmlMaxBytesPerReadExceeded, maxBytesPerRead.ToString(NumberFormatInfo.CurrentInfo));
        }

        static public void ThrowMaxNameTableCharCountExceeded(XmlDictionaryReader reader, int maxNameTableCharCount)
        {
            ThrowXmlException(reader, SR.XmlMaxNameTableCharCountExceeded, maxNameTableCharCount.ToString(NumberFormatInfo.CurrentInfo));
        }

        static public void ThrowBase64DataExpected(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlBase64DataExpected, GetWhatWasFound(reader));
        }

        static public void ThrowUndefinedPrefix(XmlDictionaryReader reader, string prefix)
        {
            ThrowXmlException(reader, SR.XmlUndefinedPrefix, prefix);
        }

        static public void ThrowProcessingInstructionNotSupported(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlProcessingInstructionNotSupported);
        }

        static public void ThrowInvalidXml(XmlDictionaryReader reader, byte b)
        {
            ThrowXmlException(reader, SR.XmlInvalidXmlByte, b.ToString("X2", CultureInfo.InvariantCulture));
        }

        static public void ThrowUnexpectedEndOfFile(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlUnexpectedEndOfFile, ((XmlBaseReader)reader).GetOpenElements());
        }

        static public void ThrowUnexpectedEndElement(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlUnexpectedEndElement);
        }

        static public void ThrowTokenExpected(XmlDictionaryReader reader, string expected, char found)
        {
            ThrowXmlException(reader, SR.XmlTokenExpected, expected, found.ToString());
        }

        static public void ThrowTokenExpected(XmlDictionaryReader reader, string expected, string found)
        {
            ThrowXmlException(reader, SR.XmlTokenExpected, expected, found);
        }

        static public void ThrowInvalidCharRef(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidCharRef);
        }

        static public void ThrowTagMismatch(XmlDictionaryReader reader, string expectedPrefix, string expectedLocalName, string foundPrefix, string foundLocalName)
        {
            ThrowXmlException(reader, SR.XmlTagMismatch, GetName(expectedPrefix, expectedLocalName), GetName(foundPrefix, foundLocalName));
        }

        static public void ThrowDuplicateXmlnsAttribute(XmlDictionaryReader reader, string localName, string ns)
        {
            string name;
            if (localName.Length == 0)
                name = "xmlns";
            else
                name = "xmlns:" + localName;
            ThrowXmlException(reader, SR.XmlDuplicateAttribute, name, name, ns);
        }

        static public void ThrowDuplicateAttribute(XmlDictionaryReader reader, string prefix1, string prefix2, string localName, string ns)
        {
            ThrowXmlException(reader, SR.XmlDuplicateAttribute, GetName(prefix1, localName), GetName(prefix2, localName), ns);
        }

        static public void ThrowInvalidBinaryFormat(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidFormat);
        }

        static public void ThrowInvalidRootData(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlInvalidRootData);
        }

        static public void ThrowMultipleRootElements(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlMultipleRootElements);
        }

        static public void ThrowDeclarationNotFirst(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlDeclNotFirst);
        }

        static public void ThrowConversionOverflow(XmlDictionaryReader reader, string value, string type)
        {
            ThrowXmlException(reader, SR.XmlConversionOverflow, value, type);
        }

        static public void ThrowXmlDictionaryStringIDOutOfRange(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDRange, XmlDictionaryString.MinKey.ToString(NumberFormatInfo.CurrentInfo), XmlDictionaryString.MaxKey.ToString(NumberFormatInfo.CurrentInfo));
        }

        static public void ThrowXmlDictionaryStringIDUndefinedStatic(XmlDictionaryReader reader, int key)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDUndefinedStatic, key.ToString(NumberFormatInfo.CurrentInfo));
        }

        static public void ThrowXmlDictionaryStringIDUndefinedSession(XmlDictionaryReader reader, int key)
        {
            ThrowXmlException(reader, SR.XmlDictionaryStringIDUndefinedSession, key.ToString(NumberFormatInfo.CurrentInfo));
        }

        static public void ThrowEmptyNamespace(XmlDictionaryReader reader)
        {
            ThrowXmlException(reader, SR.XmlEmptyNamespaceRequiresNullPrefix);
        }

        static public XmlException CreateConversionException(string type, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidConversionWithoutValue, type), exception);
        }

        static public XmlException CreateConversionException(string value, string type, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidConversion, value, type), exception);
        }

        static public XmlException CreateEncodingException(byte[] buffer, int offset, int count, Exception exception)
        {
            return CreateEncodingException(new System.Text.UTF8Encoding(false, false).GetString(buffer, offset, count), exception);
        }

        static public XmlException CreateEncodingException(string value, Exception exception)
        {
            return new XmlException(SR.Format(SR.XmlInvalidUTF8Bytes, value), exception);
        }
    }
}
