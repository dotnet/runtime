using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.MsQuic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal class IntResettableCompletionSource : ResettableCompletionSource<int>
    {
        private readonly MsQuicStream _stream;

        internal IntResettableCompletionSource(MsQuicStream stream)
            : base()
        {
            _stream = stream;
        }

        public override int GetResult(short token)
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
                    _stream._receiveResettableCompletionSource = this;
                }
            }
        }
    }
}
