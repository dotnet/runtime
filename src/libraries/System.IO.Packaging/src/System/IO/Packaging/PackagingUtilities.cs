// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//
//---------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;               // For XmlReader
using System.Diagnostics;       // For Debug.Assert
using System.Text;              // For Encoding

namespace System.IO.Packaging
{
    internal static class PackagingUtilities
    {
        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------
        internal static readonly string RelationshipNamespaceUri = "http://schemas.openxmlformats.org/package/2006/relationships";
        internal static readonly ContentType RelationshipPartContentType
            = new ContentType("application/vnd.openxmlformats-package.relationships+xml");

        internal const string ContainerFileExtension = "xps";
        internal const string XamlFileExtension = "xaml";

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// This method is used to determine if we support a given Encoding as per the
        /// OPC and XPS specs. Currently the only two encodings supported are UTF-8 and
        /// UTF-16 (Little Endian and Big Endian)
        /// </summary>
        /// <param name="reader">XmlReader</param>
        /// <returns>throws an exception if the encoding is not UTF-8 or UTF-16</returns>
        internal static void PerformInitialReadAndVerifyEncoding(XmlReader reader)
        {
            Debug.Assert(reader != null && reader.ReadState == ReadState.Initial);

            //If the first node is XmlDeclaration we check to see if the encoding attribute is present
            if (reader.Read() && reader.NodeType == XmlNodeType.XmlDeclaration && reader.Depth == 0)
            {
                string encoding = reader.GetAttribute(EncodingAttribute);

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

            //However, we can't use XmlTextReader, as it is not in COREFX.  Therefore, if there is no XmlDeclaration, or the encoding
            //attribute is not set, then we will throw now exception, and UTF-8 will be assumed.

            //TODO: in the future, we can do the work to detect the BOM, and throw an exception if the file is in an invalid encoding.
            // Eric White: IMO, this is not a serious problem.  Office will never write with the wrong encoding, nor will any of the
            // other suites.  The Open XML SDK will always write with the correct encoding.

            //The future logic would be:
            //- determine the encoding from the BOM
            //- if the encoding is not UTF-8 or UTF-16, then throw new FileFormatException(SR.EncodingNotSupported)
        }

        /// <summary>
        /// VerifyStreamReadArgs
        /// </summary>
        /// <param name="s">stream</param>
        /// <param name="buffer">buffer</param>
        /// <param name="offset">offset</param>
        /// <param name="count">count</param>
        /// <remarks>Common argument verification for Stream.Read()</remarks>
        static internal void VerifyStreamReadArgs(Stream s, byte[] buffer, int offset, int count)
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
        static internal void VerifyStreamWriteArgs(Stream s, byte[] buffer, int offset, int count)
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

        /// <summary>
        /// Read utility that is guaranteed to return the number of bytes requested
        /// if they are available.
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset in buffer to write to</param>
        /// <param name="count">bytes to read</param>
        /// <returns>bytes read</returns>
        /// <remarks>Normal Stream.Read does not guarantee how many bytes it will
        /// return.  This one does.</remarks>
        internal static int ReliableRead(Stream stream, byte[] buffer, int offset, int count)
        {
            return ReliableRead(stream, buffer, offset, count, count);
        }

        /// <summary>
        /// Read utility that is guaranteed to return the number of bytes requested
        /// if they are available.
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset in buffer to write to</param>
        /// <param name="requestedCount">count of bytes that we would like to read (max read size to try)</param>
        /// <param name="requiredCount">minimal count of bytes that we would like to read (min read size to achieve)</param>
        /// <returns>bytes read</returns>
        /// <remarks>Normal Stream.Read does not guarantee how many bytes it will
        /// return.  This one does.</remarks>
        internal static int ReliableRead(Stream stream, byte[] buffer, int offset, int requestedCount, int requiredCount)
        {
            Debug.Assert(stream != null);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length > 0);
            Debug.Assert(offset >= 0);
            Debug.Assert(requestedCount >= 0);
            Debug.Assert(requiredCount >= 0);
            Debug.Assert(checked(offset + requestedCount <= buffer.Length));
            Debug.Assert(requiredCount <= requestedCount);

            // let's read the whole block into our buffer 
            int totalBytesRead = 0;
            while (totalBytesRead < requiredCount)
            {
                int bytesRead = stream.Read(buffer,
                                offset + totalBytesRead,
                                requestedCount - totalBytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        /// <summary>
        /// Read utility that is guaranteed to return the number of bytes requested
        /// if they are available.
        /// </summary>
        /// <param name="reader">BinaryReader to read from</param>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset in buffer to write to</param>
        /// <param name="count">bytes to read</param>
        /// <returns>bytes read</returns>
        /// <remarks>Normal Stream.Read does not guarantee how many bytes it will
        /// return.  This one does.</remarks>
        internal static int ReliableRead(BinaryReader reader, byte[] buffer, int offset, int count)
        {
            return ReliableRead(reader, buffer, offset, count, count);
        }

        /// <summary>
        /// Read utility that is guaranteed to return the number of bytes requested
        /// if they are available.
        /// </summary>
        /// <param name="reader">BinaryReader to read from</param>
        /// <param name="buffer">buffer to read into</param>
        /// <param name="offset">offset in buffer to write to</param>
        /// <param name="requestedCount">count of bytes that we would like to read (max read size to try)</param>
        /// <param name="requiredCount">minimal count of bytes that we would like to read (min read size to achieve)</param>
        /// <returns>bytes read</returns>
        /// <remarks>Normal Stream.Read does not guarantee how many bytes it will
        /// return.  This one does.</remarks>
        internal static int ReliableRead(BinaryReader reader, byte[] buffer, int offset, int requestedCount, int requiredCount)
        {
            Debug.Assert(reader != null);
            Debug.Assert(buffer != null);
            Debug.Assert(buffer.Length > 0);
            Debug.Assert(offset >= 0);
            Debug.Assert(requestedCount >= 0);
            Debug.Assert(requiredCount >= 0);
            Debug.Assert(checked(offset + requestedCount <= buffer.Length));
            Debug.Assert(requiredCount <= requestedCount);

            // let's read the whole block into our buffer 
            int totalBytesRead = 0;
            while (totalBytesRead < requiredCount)
            {
                int bytesRead = reader.Read(buffer,
                                offset + totalBytesRead,
                                requestedCount - totalBytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        /// <summary>
        /// CopyStream utility that is guaranteed to return the number of bytes copied (may be less then requested,
        /// if source stream doesn't have enough data)
        /// </summary>
        /// <param name="sourceStream">stream to read from</param>
        /// <param name="targetStream">stream to write to </param>
        /// <param name="bytesToCopy">number of bytes to be copied(use Int64.MaxValue if the whole stream needs to be copied)</param>
        /// <param name="bufferSize">number of bytes to be copied (usually it is 4K for scenarios where we expect a lot of data 
        ///  like in SparseMemoryStream case it could be larger </param>
        /// <returns>bytes copied (might be less than requested if source stream is too short</returns>
        /// <remarks>Neither source nor target stream are seeked; it is up to the caller to make sure that their positions are properly set.
        ///  Target stream isn't truncated even if it has more data past the area that was copied.</remarks> 
        internal static long CopyStream(Stream sourceStream, Stream targetStream, long bytesToCopy, int bufferSize)
        {
            Debug.Assert(sourceStream != null);
            Debug.Assert(targetStream != null);
            Debug.Assert(bytesToCopy >= 0);
            Debug.Assert(bufferSize > 0);

            byte[] buffer = new byte[bufferSize];

            // let's read the whole block into our buffer 
            long bytesLeftToCopy = bytesToCopy;
            while (bytesLeftToCopy > 0)
            {
                int bytesRead = sourceStream.Read(buffer, 0, (int)Math.Min(bytesLeftToCopy, (long)bufferSize));
                if (bytesRead == 0)
                {
                    targetStream.Flush();
                    return bytesToCopy - bytesLeftToCopy;
                }

                targetStream.Write(buffer, 0, bytesRead);
                bytesLeftToCopy -= bytesRead;
            }

            // It must not be negative
            Debug.Assert(bytesLeftToCopy == 0);

            targetStream.Flush();
            return bytesToCopy;
        }


        /// <summary>
        /// Calculate overlap between two blocks, returning the offset and length of the overlap
        /// </summary>
        /// <param name="block1Offset"></param>
        /// <param name="block1Size"></param>
        /// <param name="block2Offset"></param>
        /// <param name="block2Size"></param>
        /// <param name="overlapBlockOffset"></param>
        /// <param name="overlapBlockSize"></param>
        internal static void CalculateOverlap(long block1Offset, long block1Size,
                                              long block2Offset, long block2Size,
                                              out long overlapBlockOffset, out long overlapBlockSize)
        {
            checked
            {
                overlapBlockOffset = Math.Max(block1Offset, block2Offset);
                overlapBlockSize = Math.Min(block1Offset + block1Size, block2Offset + block2Size) - overlapBlockOffset;

                if (overlapBlockSize <= 0)
                {
                    overlapBlockSize = 0;
                }
            }
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

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
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
