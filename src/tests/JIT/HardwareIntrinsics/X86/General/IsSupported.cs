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
        [Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/75767", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
        [Fact]
        public static void IsSupported()
        {
            bool result = true;

            if (Sse.IsSupported && int.TryParse(Environment.GetEnvironmentVariable("DOTNET_EnableIncompleteISAClass"), out var enableIncompleteIsa) && (enableIncompleteIsa != 0))
            {
                // X86 platforms
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
                Convert.ToBoolean(typeof(Sse41.X64).GetMethod(issupported).Invoke(null, null)) != Sse41.X64.IsSupported ||
                Convert.ToBoolean(typeof(Sse42.X64).GetMethod(issupported).Invoke(null, null)) != Sse42.X64.IsSupported ||
                Convert.ToBoolean(typeof(Lzcnt.X64).GetMethod(issupported).Invoke(null, null)) != Lzcnt.X64.IsSupported ||
                Convert.ToBoolean(typeof(Popcnt.X64).GetMethod(issupported).Invoke(null, null)) != Popcnt.X64.IsSupported ||
                Convert.ToBoolean(typeof(Bmi1.X64).GetMethod(issupported).Invoke(null, null)) != Bmi1.X64.IsSupported ||
                Convert.ToBoolean(typeof(Bmi2.X64).GetMethod(issupported).Invoke(null, null)) != Bmi2.X64.IsSupported
            )
            {
                result = false;
            }
            Assert.True(result);
        }
    }
}
