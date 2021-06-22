using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    internal partial class NativeExportsNE
    {
        internal partial class CallingConventions
        {
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "add_integers_cdecl")]
            [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
            public static partial long AddLongsCdecl(long i, long j, long k, long l, long m, long n, long o, long p, long q);
            [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "add_integers_stdcall")]
            [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvStdcall) })]
            public static partial long AddLongsStdcall(long i, long j, long k, long l, long m, long n, long o, long p, long q);
        }
    }

    public class CallingConventionTests
    {
        [Fact]
        public void UnmanagedCallConvPropagated()
        {
            Random rng = new Random(1234);
            long i = rng.Next();
            long j = rng.Next();
            long k = rng.Next();
            long l = rng.Next();
            long m = rng.Next();
            long n = rng.Next();
            long o = rng.Next();
            long p = rng.Next();
            long q = rng.Next();
            long expected = i + j + k + l + m + n + o + p + q;
            Assert.Equal(expected, NativeExportsNE.CallingConventions.AddLongsCdecl(i, j, k, l, m, n, o, p, q));
            Assert.Equal(expected, NativeExportsNE.CallingConventions.AddLongsStdcall(i, j, k, l, m, n, o, p, q));
        }
    }
}
