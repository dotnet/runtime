using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic.Implementations.MsQuic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Text;
using System.Threading.Tasks;

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
