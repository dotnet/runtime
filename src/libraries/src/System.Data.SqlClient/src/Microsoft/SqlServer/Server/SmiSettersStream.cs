// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace Microsoft.SqlServer.Server
{
    internal class SmiSettersStream : Stream
    {
        private SmiEventSink_Default _sink;
        private ITypedSettersV3 _setters;
        private int _ordinal;
        private long _lengthWritten;
        private SmiMetaData _metaData;

        internal SmiSettersStream(SmiEventSink_Default sink, ITypedSettersV3 setters, int ordinal, SmiMetaData metaData)
        {
            Debug.Assert(null != sink);
            Debug.Assert(null != setters);
            Debug.Assert(0 <= ordinal);
            Debug.Assert(null != metaData);

            _sink = sink;
            _setters = setters;
            _ordinal = ordinal;
            _lengthWritten = 0;
            _metaData = metaData;
        }

        public override bool CanRead
        {
            get
            {
                return false;
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
                return true;
            }
        }

        public override long Length
        {
            get
            {
                return _lengthWritten;
            }
        }

        public override long Position
        {
            get
            {
                return _lengthWritten;
            }
            set
            {
                throw SQL.StreamSeekNotSupported();
            }
        }

        public override void Flush()
        {
            _lengthWritten = ValueUtilsSmi.SetBytesLength(_sink, _setters, _ordinal, _metaData, _lengthWritten);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw SQL.StreamSeekNotSupported();
        }

        public override void SetLength(long value)
        {
            if (value < 0)
            {
                throw ADP.ArgumentOutOfRange(nameof(value));
            }
            ValueUtilsSmi.SetBytesLength(_sink, _setters, _ordinal, _metaData, value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw SQL.StreamReadNotSupported();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _lengthWritten += ValueUtilsSmi.SetBytes(_sink, _setters, _ordinal, _metaData, _lengthWritten, buffer, offset, count);
        }
    }
}
