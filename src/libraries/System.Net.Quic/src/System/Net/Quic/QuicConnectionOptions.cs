using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    internal class QuicConnectionOptions
    {
        public int BidirectionalStreamCount { get; set; } = 100;
        public int UnidirectionalStreamCount { get; set; } = 100;
    }
}
