using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NativeExports
{
    class CallingConventions
    {
        // Use 9 long arguments to ensure we spill to the stack on all platforms.
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "add_integers_cdecl")]
        public static long AddLongsCdecl(long i, long j, long k, long l, long m, long n, long o, long p, long q)
        {
            return i + j + k + l + m + n + o + p + q;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) }, EntryPoint = "add_integers_stdcall")]
        public static long AddLongsStdcall(long i, long j, long k, long l, long m, long n, long o, long p, long q)
        {
            return i + j + k + l + m + n + o + p + q;
        }
    }
}
