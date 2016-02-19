// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;


namespace System.Runtime.Serialization.Json
{
#if FEATURE_LEGACYNETCF
    public
#else
    internal
#endif
    class JavaScriptDeserializer
    {
        internal const string s_jsonBeta2Prefix = @"{""d"":";
        private JavaScriptObjectDeserializer _deserializer;

        internal object DeserializeObject()
        {
            return _deserializer.BasicDeserialize();
        }

        internal JavaScriptDeserializer(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Stream incomingStream = stream;
            if (!stream.CanSeek)
            {
                //Incoming stream if not seekable then read it in to memory.
                incomingStream = new BufferedStreamReader(stream);
            }

            Encoding streamEncoding = DetectEncoding(incomingStream.ReadByte(), incomingStream.ReadByte());
            //Move the stream back to 0
            incomingStream.Position = 0;
            //If the stream contains a BOM, StreamReader will detect it and override our encoding setting
            string input = new StreamReader(incomingStream, streamEncoding, true/*detectEncodingFromByteOrderMarks*/).ReadToEnd();

            if (string.IsNullOrEmpty(input))
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExpectingElement, XmlDictionaryString.Empty, "root")));
            }

            _deserializer = new JavaScriptObjectDeserializer(input);
        }

#if FEATURE_LEGACYNETCF
        public
#else
        internal
#endif
        class BufferedStreamReader : Stream
        {
            private byte[] _bomBuffer = new byte[2];
            private int _bufferedBytesIndex = 0;
            private Stream _internalStream;

#if FEATURE_LEGACYNETCF
            public
#else
            internal
#endif
            BufferedStreamReader(Stream stream)
            {
                _internalStream = stream;
                //Buffer the first two bytes
                stream.Read(_bomBuffer, 0, 2);
            }

            public override long Position
            {
                set
                {
                    //Set position if value is less than 2.
                    if (value < 2)
                    {
                        //Move the bufferedIndex back
                        _bufferedBytesIndex = (int)value;
                    }
                    else
                    {
                        _internalStream.Position = value;
                    }
                }

                get
                {
                    return _internalStream.Position;
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = 0;
                while (_bufferedBytesIndex < 2 && count > 0)
                {
                    //While we have buffered bytes read from buffer
                    bytesRead++;
                    buffer[offset++] = _bomBuffer[_bufferedBytesIndex++];
                    count--;
                }
                //If we have read all buffered bytes and we need to read further bytes read from the stream
                return (count > 0) ? _internalStream.Read(buffer, offset, count) + bytesRead : bytesRead;
            }

            public override int ReadByte()
            {
                //While we have buffered bytes read from buffer
                return (_bufferedBytesIndex < 2) ? _bomBuffer[_bufferedBytesIndex++] : _internalStream.ReadByte();
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush() { }

            public override long Length
            {
                get { return _internalStream.Length; }
            }

            //Rest of the functions should not be used so throwing NSE
            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }

        public static Encoding DetectEncoding(int b1, int b2)
        {
            if (b1 == -1 || b2 == -1)
            {
                //stream contains less than 2 bytes of data. Assume UTF8
                return JsonGlobals.ValidatingUTF8;
            }
            if (b1 == 0x00 && b2 != 0x00)
            {
                return JsonGlobals.ValidatingBEUTF16;
            }
            else if (b1 != 0x00 && b2 == 0x00)
            {
                // 857 It's possible to misdetect UTF-32LE as UTF-16LE, but that's OK.
                return JsonGlobals.ValidatingUTF16;
            }
            else if (b1 == 0x00 && b2 == 0x00)
            {
                // UTF-32BE not supported
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(SR.Format(SR.JsonInvalidBytes)));
            }

            return JsonGlobals.ValidatingUTF8;
        }
    }
}
