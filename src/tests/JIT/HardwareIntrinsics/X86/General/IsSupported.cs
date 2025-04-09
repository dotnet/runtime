// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

namespace IntelHardwareIntrinsicTest.General
{
    public partial class Program
    {
        [Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/91392", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
        [Fact]
        public static void IsSupported()
        {
            bool result = true;

            if (Sse.IsSupported && int.TryParse(Environment.GetEnvironmentVariable("DOTNET_EnableIncompleteISAClass"), out var enableIncompleteIsa) && (enableIncompleteIsa != 0))
            {
                // X86 platforms
                if (Vector<byte>.Count == 64 && !Avx512F.IsSupported)
                {
                    result = false;
                }

                if (Vector<byte>.Count == 32 && !Avx2.IsSupported)
                {
                    result = false;
                }

                if (Vector<byte>.Count == 16 && Vector.IsHardwareAccelerated && !Sse2.IsSupported)
                {
                    result = false;
                }
            }

            // Reflection call
            var issupported = "get_IsSupported";
            if (Convert.ToBoolean(typeof(Sse).GetMethod(issupported).Invoke(null, null)) != Sse.IsSupported ||
                Convert.ToBoolean(typeof(Sse2).GetMethod(issupported).Invoke(null, null)) != Sse2.IsSupported ||
                Convert.ToBoolean(typeof(Sse3).GetMethod(issupported).Invoke(null, null)) != Sse3.IsSupported ||
                Convert.ToBoolean(typeof(Ssse3).GetMethod(issupported).Invoke(null, null)) != Ssse3.IsSupported ||
                Convert.ToBoolean(typeof(Sse41).GetMethod(issupported).Invoke(null, null)) != Sse41.IsSupported ||
                Convert.ToBoolean(typeof(Sse42).GetMethod(issupported).Invoke(null, null)) != Sse42.IsSupported ||
                Convert.ToBoolean(typeof(Avx).GetMethod(issupported).Invoke(null, null)) != Avx.IsSupported ||
                Convert.ToBoolean(typeof(Avx2).GetMethod(issupported).Invoke(null, null)) != Avx2.IsSupported ||
                Convert.ToBoolean(typeof(Lzcnt).GetMethod(issupported).Invoke(null, null)) != Lzcnt.IsSupported ||
                Convert.ToBoolean(typeof(Popcnt).GetMethod(issupported).Invoke(null, null)) != Popcnt.IsSupported ||
                Convert.ToBoolean(typeof(Bmi1).GetMethod(issupported).Invoke(null, null)) != Bmi1.IsSupported ||
                Convert.ToBoolean(typeof(Bmi2).GetMethod(issupported).Invoke(null, null)) != Bmi2.IsSupported ||
                Convert.ToBoolean(typeof(Sse.X64).GetMethod(issupported).Invoke(null, null)) != Sse.X64.IsSupported ||
                Convert.ToBoolean(typeof(Sse2.X64).GetMethod(issupported).Invoke(null, null)) != Sse2.X64.IsSupported ||
                Convert.ToBoolean(typeof(Sse3.X64).GetMethod(issupported).Invoke(null, null)) != Sse3.X64.IsSupported ||
                Convert.ToBoolean(typeof(Ssse3.X64).GetMethod(issupported).Invoke(null, null)) != Ssse3.X64.IsSupported ||
                Convert.ToBoolean(typeof(Sse41.X64).GetMethod(issupported).Invoke(null, null)) != Sse41.X64.IsSupported ||
                Convert.ToBoolean(typeof(Sse42.X64).GetMethod(issupported).Invoke(null, null)) != Sse42.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx.X64).GetMethod(issupported).Invoke(null, null)) != Avx.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx2.X64).GetMethod(issupported).Invoke(null, null)) != Avx2.X64.IsSupported ||
                Convert.ToBoolean(typeof(Lzcnt.X64).GetMethod(issupported).Invoke(null, null)) != Lzcnt.X64.IsSupported ||
                Convert.ToBoolean(typeof(Popcnt.X64).GetMethod(issupported).Invoke(null, null)) != Popcnt.X64.IsSupported ||
                Convert.ToBoolean(typeof(Bmi1.X64).GetMethod(issupported).Invoke(null, null)) != Bmi1.X64.IsSupported ||
                Convert.ToBoolean(typeof(Bmi2.X64).GetMethod(issupported).Invoke(null, null)) != Bmi2.X64.IsSupported ||
                Convert.ToBoolean(typeof(Aes).GetMethod(issupported).Invoke(null, null)) != Aes.IsSupported ||
                Convert.ToBoolean(typeof(Aes.X64).GetMethod(issupported).Invoke(null, null)) != Aes.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx512BW).GetMethod(issupported).Invoke(null, null)) != Avx512BW.IsSupported ||
                Convert.ToBoolean(typeof(Avx512BW.VL).GetMethod(issupported).Invoke(null, null)) != Avx512BW.VL.IsSupported ||
                Convert.ToBoolean(typeof(Avx512BW.X64).GetMethod(issupported).Invoke(null, null)) != Avx512BW.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx512CD).GetMethod(issupported).Invoke(null, null)) != Avx512CD.IsSupported ||
                Convert.ToBoolean(typeof(Avx512CD.VL).GetMethod(issupported).Invoke(null, null)) != Avx512CD.VL.IsSupported ||
                Convert.ToBoolean(typeof(Avx512CD.X64).GetMethod(issupported).Invoke(null, null)) != Avx512CD.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx512DQ).GetMethod(issupported).Invoke(null, null)) != Avx512DQ.IsSupported ||
                Convert.ToBoolean(typeof(Avx512DQ.VL).GetMethod(issupported).Invoke(null, null)) != Avx512DQ.VL.IsSupported ||
                Convert.ToBoolean(typeof(Avx512DQ.X64).GetMethod(issupported).Invoke(null, null)) != Avx512DQ.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx512F).GetMethod(issupported).Invoke(null, null)) != Avx512F.IsSupported ||
                Convert.ToBoolean(typeof(Avx512F.VL).GetMethod(issupported).Invoke(null, null)) != Avx512F.VL.IsSupported ||
                Convert.ToBoolean(typeof(Avx512F.X64).GetMethod(issupported).Invoke(null, null)) != Avx512F.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx512Vbmi).GetMethod(issupported).Invoke(null, null)) != Avx512Vbmi.IsSupported ||
                Convert.ToBoolean(typeof(Avx512Vbmi.VL).GetMethod(issupported).Invoke(null, null)) != Avx512Vbmi.VL.IsSupported ||
                Convert.ToBoolean(typeof(Avx512Vbmi.X64).GetMethod(issupported).Invoke(null, null)) != Avx512Vbmi.X64.IsSupported ||
                Convert.ToBoolean(typeof(AvxVnni).GetMethod(issupported).Invoke(null, null)) != AvxVnni.IsSupported ||
                Convert.ToBoolean(typeof(AvxVnni.X64).GetMethod(issupported).Invoke(null, null)) != AvxVnni.X64.IsSupported ||
                Convert.ToBoolean(typeof(Fma).GetMethod(issupported).Invoke(null, null)) != Fma.IsSupported ||
                Convert.ToBoolean(typeof(Fma.X64).GetMethod(issupported).Invoke(null, null)) != Fma.X64.IsSupported ||
                Convert.ToBoolean(typeof(Gfni).GetMethod(issupported).Invoke(null, null)) != Gfni.IsSupported ||
                Convert.ToBoolean(typeof(Gfni.V256).GetMethod(issupported).Invoke(null, null)) != Gfni.V256.IsSupported ||
                Convert.ToBoolean(typeof(Gfni.V512).GetMethod(issupported).Invoke(null, null)) != Gfni.V512.IsSupported ||
                Convert.ToBoolean(typeof(Gfni.X64).GetMethod(issupported).Invoke(null, null)) != Gfni.X64.IsSupported ||
                Convert.ToBoolean(typeof(Pclmulqdq).GetMethod(issupported).Invoke(null, null)) != Pclmulqdq.IsSupported ||
                Convert.ToBoolean(typeof(Pclmulqdq.V256).GetMethod(issupported).Invoke(null, null)) != Pclmulqdq.V256.IsSupported ||
                Convert.ToBoolean(typeof(Pclmulqdq.V512).GetMethod(issupported).Invoke(null, null)) != Pclmulqdq.V512.IsSupported ||
                Convert.ToBoolean(typeof(Pclmulqdq.X64).GetMethod(issupported).Invoke(null, null)) != Pclmulqdq.X64.IsSupported ||
                Convert.ToBoolean(typeof(X86Base).GetMethod(issupported).Invoke(null, null)) != X86Base.IsSupported ||
                Convert.ToBoolean(typeof(X86Base.X64).GetMethod(issupported).Invoke(null, null)) != X86Base.X64.IsSupported ||
                Convert.ToBoolean(typeof(X86Serialize).GetMethod(issupported).Invoke(null, null)) != X86Serialize.IsSupported ||
                Convert.ToBoolean(typeof(X86Serialize.X64).GetMethod(issupported).Invoke(null, null)) != X86Serialize.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx10v1).GetMethod(issupported).Invoke(null, null)) != Avx10v1.IsSupported ||
                Convert.ToBoolean(typeof(Avx10v1.X64).GetMethod(issupported).Invoke(null, null)) != Avx10v1.X64.IsSupported ||
                Convert.ToBoolean(typeof(Avx10v1.V512).GetMethod(issupported).Invoke(null, null)) != Avx10v1.V512.IsSupported ||
                Convert.ToBoolean(typeof(Avx10v1.V512.X64).GetMethod(issupported).Invoke(null, null)) != Avx10v1.V512.X64.IsSupported)
            {
                result = false;
            }
            Assert.True(result);
        }
    }
}
