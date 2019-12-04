using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    /// <summary>
    /// Options to provide to the <see cref="QuicListener"/>.
    /// </summary>
    internal class QuicListenerOptions
    {
        public int ListenBacklog { get; set; } = 512;
        public int BidirectionalStreamCount { get; set; } = 100;
        public int UnidirectionalStreamCount { get; set; } = 100;
    }
}
