// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Data.SqlTypes;
using System.IO;
using System.Runtime.CompilerServices;
using System.Data.Common;

namespace System.Data.SqlClient
{
    // Caches the bytes returned from partial length prefixed datatypes, like XML
    sealed internal class SqlCachedBuffer : System.Data.SqlTypes.INullable
    {
        public static readonly SqlCachedBuffer Null = new SqlCachedBuffer();
        private const int _maxChunkSize = 2048; // Arbitrary value for chunk size. Revisit this later for better perf

        private List<byte[]> _cachedBytes;

        private SqlCachedBuffer()
        {
            // For constructing Null
        }

        private SqlCachedBuffer(List<byte[]> cachedBytes)
        {
            _cachedBytes = cachedBytes;
        }

        internal List<byte[]> CachedBytes
        {
            get { return _cachedBytes; }
        }

        // Reads off from the network buffer and caches bytes. Only reads one column value in the current row.
        internal static bool TryCreate(SqlMetaDataPriv metadata, TdsParser parser, TdsParserStateObject stateObj, out SqlCachedBuffer buffer)
        {
            int cb = 0;
            ulong plplength;
            byte[] byteArr;

            List<byte[]> cachedBytes = new List<byte[]>();
            buffer = null;

            // the very first length is already read.
            if (!parser.TryPlpBytesLeft(stateObj, out plplength))
            {
                return false;
            }
            // For now we  only handle Plp data from the parser directly.
            Debug.Assert(metadata.metaType.IsPlp, "SqlCachedBuffer call on a non-plp data");
            do
            {
                if (plplength == 0)
                    break;
                do
                {
                    cb = (plplength > (ulong)_maxChunkSize) ? _maxChunkSize : (int)plplength;
                    byteArr = new byte[cb];
                    if (!stateObj.TryReadPlpBytes(ref byteArr, 0, cb, out cb))
                    {
                        return false;
                    }
                    Debug.Assert(cb == byteArr.Length);
                    if (cachedBytes.Count == 0)
                    {
                        // Add the Byte order mark if needed if we read the first array
                        AddByteOrderMark(byteArr, cachedBytes);
                    }
                    cachedBytes.Add(byteArr);
                    plplength -= (ulong)cb;
                } while (plplength > 0);
                if (!parser.TryPlpBytesLeft(stateObj, out plplength))
                {
                    return false;
                }
            } while (plplength > 0);
            Debug.Assert(stateObj._longlen == 0 && stateObj._longlenleft == 0);

            buffer = new SqlCachedBuffer(cachedBytes);
            return true;
        }

        private static void AddByteOrderMark(byte[] byteArr, List<byte[]> cachedBytes)
        {
            // Need to find out if we should add byte order mark or not. 
            // We need to add this if we are getting ntext xml, not if we are getting binary xml
            // Binary Xml always begins with the bytes 0xDF and 0xFF
            // If we aren't getting these, then we are getting Unicode xml
            if ((byteArr.Length < 2) || (byteArr[0] != 0xDF) || (byteArr[1] != 0xFF))
            {
                Debug.Assert(cachedBytes.Count == 0);
                cachedBytes.Add(TdsEnums.XMLUNICODEBOMBYTES);
            }
        }

        internal Stream ToStream()
        {
            return new SqlCachedStream(this);
        }

        override public string ToString()
        {
            if (IsNull)
                throw new SqlNullValueException();

            if (_cachedBytes.Count == 0)
            {
                return String.Empty;
            }
            SqlXml sxml = new SqlXml(ToStream());
            return sxml.Value;
        }

        internal SqlString ToSqlString()
        {
            if (IsNull)
                return SqlString.Null;
            string str = ToString();
            return new SqlString(str);
        }

        internal SqlXml ToSqlXml()
        {
            SqlXml sx = new SqlXml(ToStream());
            return sx;
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal XmlReader ToXmlReader()
        {
            return SqlTypeWorkarounds.SqlXmlCreateSqlXmlReader(ToStream(), closeInput: false);
        }

        public bool IsNull
        {
            get
            {
                return (_cachedBytes == null) ? true : false;
            }
        }


    }
}
