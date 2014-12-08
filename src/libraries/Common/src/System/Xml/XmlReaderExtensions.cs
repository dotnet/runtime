// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal static class XmlReaderExtensions
    {
        public static bool CanReadContentAs(this System.Xml.XmlReader reader)
        {
            const uint CanReadContentAsBitmap = 0x1E1BC;
            return 0 != (CanReadContentAsBitmap & (1 << (int)reader.NodeType));
        }

        private static string AddLineInfo(string message, System.Xml.IXmlLineInfo lineInfo)
        {
            if (lineInfo != null)
            {
                string[] lineArgs = new string[2];
                lineArgs[0] = lineInfo.LineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
                lineArgs[1] = lineInfo.LinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
                message += " " + SR.Format(SR.Xml_ErrorPosition, lineArgs);
            }
            return message;
        }

        public static Exception CreateReadContentAsException(this System.Xml.XmlReader reader, string methodName)
        {
            System.Xml.IXmlLineInfo lineInfo = reader as System.Xml.IXmlLineInfo;
            return new InvalidOperationException(AddLineInfo(SR.Format(SR.Xml_InvalidReadContentAs, methodName, reader.NodeType), lineInfo));
        }

        public static Exception CreateReadElementContentAsException(this System.Xml.XmlReader reader, string methodName)
        {
            System.Xml.IXmlLineInfo lineInfo = reader as System.Xml.IXmlLineInfo;
            return new InvalidOperationException(AddLineInfo(SR.Format(SR.Xml_InvalidReadElementContentAs, methodName, reader.NodeType), lineInfo));
        }
    }
}
