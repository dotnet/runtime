// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;

namespace Microsoft.SqlServer.Server
{
    internal class SmiGettersStream : Stream
    {
        private SmiEventSink_Default _sink;
        private ITypedGettersV3 _getters;
        private int _ordinal;
        private long _readPosition;
        private SmiMetaData _metaData;

        internal SmiGettersStream(SmiEventSink_Default sink, ITypedGettersV3 getters, int ordinal, SmiMetaData metaData)
        {
            Debug.Assert(null != sink);
            Debug.Assert(null != getters);
            Debug.Assert(0 <= ordinal);
            Debug.Assert(null != metaData);

            _sink = sink;
            _getters = getters;
            _ordinal = ordinal;
            _readPosition = 0;
            _metaData = metaData;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        // If CanSeek is false, Position, Seek, Length, and SetLength should throw.
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return ValueUtilsSmi.GetBytesInternal(_sink, _getters, _ordinal, _metaData, 0, null, 0, 0, false);
            }
        }

        public override long Position
        {
            get
            {
                return _readPosition;
            }
            set
            {
                throw SQL.StreamSeekNotSupported();
            }
        }

        public override void Flush()
        {
            throw SQL.StreamWriteNotSupported();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw SQL.StreamSeekNotSupported();
        }

        public override void SetLength(long value)
        {
            throw SQL.StreamWriteNotSupported();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long bytesRead = ValueUtilsSmi.GetBytesInternal(_sink, _getters, _ordinal, _metaData, _readPosition, buffer, offset, count, false);
            _readPosition += bytesRead;

            return checked((int)bytesRead);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw SQL.StreamWriteNotSupported();
        }
    }
}

