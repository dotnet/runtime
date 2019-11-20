using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicStatusHelper
    {
        internal static bool Succeeded(uint status)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return status < 0x80000000;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return (int)status <= 0;
            }

            return false;
        }
    }
}
