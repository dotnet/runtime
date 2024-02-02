// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

namespace System.IO.Packaging
{
    internal static class PackagingUtilities
    {
        internal const string RelationshipNamespaceUri = "http://schemas.openxmlformats.org/package/2006/relationships";
        internal static readonly ContentType RelationshipPartContentType
            = new ContentType("application/vnd.openxmlformats-package.relationships+xml");

        internal const string ContainerFileExtension = "xps";
        internal const string XamlFileExtension = "xaml";

        #region Internal Methods

        /// <summary>
        /// This method is used to determine if we support a given Encoding as per the
        /// OPC and XPS specs. Currently the only two encodings supported are UTF-8 and
        /// UTF-16 (Little Endian and Big Endian)
        /// </summary>
        /// <param name="reader">XmlReader</param>
        internal static void PerformInitialReadAndVerifyEncoding(XmlReader reader)
        {
            Debug.Assert(reader != null && reader.ReadState == ReadState.Initial);

            //If the first node is XmlDeclaration we check to see if the encoding attribute is present
            if (reader.Read() && reader.NodeType == XmlNodeType.XmlDeclaration && reader.Depth == 0)
            {
                string? encoding = reader.GetAttribute(EncodingAttribute);

                if (!string.IsNullOrEmpty(encoding))
                {
                    //If a non-empty encoding attribute is present [for example - <?xml version="1.0" encoding="utf-8" ?>]
                    //we check to see if the value is either "utf-8" or "utf-16". Only these two values are supported
                    //Note: For Byte order markings that require additional information to be specified in
                    //the encoding attribute in XmlDeclaration have already been ruled out by this check as we allow for
                    //only two valid values.
                    if (string.Equals(encoding, WebNameUTF8, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(encoding, WebNameUnicode, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    else
                    {
                        //if the encoding attribute has any other value we throw an exception
                        throw new FileFormatException(SR.EncodingNotSupported);
                    }
                }
            }

            //Previously, the logic in System.IO.Packaging was that if the XmlDeclaration is not present, or encoding attribute
            //is not present, we base our decision on byte order marking. Previously, reader was an XmlTextReader, which would
            //take that into account and return the correct value.

            //However, we can't use XmlTextReader, as it is not in the libraries.  Therefore, if there is no XmlDeclaration, or the encoding
            //attribute is not set, then we will throw now exception, and UTF-8 will be assumed.

            //TODO: in the future, we can do the work to detect the BOM, and throw an exception if the file is in an invalid encoding.
            // Eric White: IMO, this is not a serious problem.  Office will never write with the wrong encoding, nor will any of the
            // other suites.  The Open XML SDK will always write with the correct encoding.

            //The future logic would be:
            //- determine the encoding from the BOM
            //- if the encoding is not UTF-8 or UTF-16, then throw new FileFormatException(SR.EncodingNotSupported)
        }

        /// <summary>
        /// This method returns the count of xml attributes other than:
        /// 1. xmlns="namespace"
        /// 2. xmlns:someprefix="namespace"
        /// Reader should be positioned at the Element whose attributes
        /// are to be counted.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns>An integer indicating the number of non-xmlns attributes</returns>
        internal static int GetNonXmlnsAttributeCount(XmlReader reader)
        {
            Debug.Assert(reader != null, "xmlReader should not be null");
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "XmlReader should be positioned at an Element");

            int readerCount = 0;

            //If true, reader moves to the attribute
            //If false, there are no more attributes (or none)
            //and in that case the position of the reader is unchanged.
            //First time through, since the reader will be positioned at an Element,
            //MoveToNextAttribute is the same as MoveToFirstAttribute.
            while (reader.MoveToNextAttribute())
            {
                if (!string.Equals(reader.Name, XmlNamespace, StringComparison.Ordinal) &&
                    !string.Equals(reader.Prefix, XmlNamespace, StringComparison.Ordinal))
                {
                    readerCount++;
                }
            }

            //re-position the reader to the element
            reader.MoveToElement();

            return readerCount;
        }

        /// <summary>
        /// VerifyStreamReadArgs
        /// </summary>
        /// <param name="s">stream</param>
        /// <param name="buffer">buffer</param>
        /// <param name="offset">offset</param>
        /// <param name="count">count</param>
        /// <remarks>Common argument verification for Stream.Read()</remarks>
        internal static void VerifyStreamReadArgs(Stream s, byte[] buffer, int offset, int count)
        {
            if (!s.CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.OffsetNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ReadCountNegative);
            }

            checked     // catch any integer overflows
            {
                if (offset + count > buffer.Length)
                {
                    throw new ArgumentException(SR.ReadBufferTooSmall, nameof(buffer));
                }
            }
        }

        /// <summary>
        /// VerifyStreamWriteArgs
        /// </summary>
        /// <param name="s"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <remarks>common argument verification for Stream.Write</remarks>
        internal static void VerifyStreamWriteArgs(Stream s, byte[] buffer, int offset, int count)
        {
            if (!s.CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.OffsetNegative);
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.WriteCountNegative);
            }

            checked
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException(SR.WriteBufferTooSmall, nameof(buffer));
            }
        }

        #endregion Internal Methods

        /// <summary>
        /// Synchronize access to IsolatedStorage methods that can step on each-other
        /// </summary>
        /// <remarks>See PS 1468964 for details.</remarks>
        private const string XmlNamespace = "xmlns";
        private const string EncodingAttribute = "encoding";
        private const string WebNameUTF8 = "utf-8";
        private const string WebNameUnicode = "utf-16";
    }
}
