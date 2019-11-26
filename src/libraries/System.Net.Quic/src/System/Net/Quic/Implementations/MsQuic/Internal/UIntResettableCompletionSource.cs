// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class UIntResettableCompletionSource : ResettableCompletionSource<uint>
    {
        private readonly MsQuicStream _stream;

        internal UIntResettableCompletionSource(MsQuicStream stream)
            : base()
        {
            _stream = stream;
        }

        public override uint GetResult(short token)
        {
            bool isValid = token == _valueTaskSource.Version;
            try
            {
                return _valueTaskSource.GetResult(token);
            }
            finally
            {
                if (isValid)
                {
                    _valueTaskSource.Reset();
                    _stream._sendResettableCompletionSource = this;
                }
            }
        }
    }
}
