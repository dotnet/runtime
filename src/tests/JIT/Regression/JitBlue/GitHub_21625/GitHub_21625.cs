// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace GitHub_21625
{
    public class test
    {
        public static Vector128<ushort> CreateScalar(ushort value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ConvertScalarToVector128UInt32(value).AsUInt16();
            }
 
            return SoftwareFallback(value);
 
            Vector128<ushort> SoftwareFallback(ushort x)
            {
                var result = Vector128<ushort>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref result), value);
                return result;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            ushort value = TestLibrary.Generator.GetUInt16();
            Vector128<ushort> result = CreateScalar(value);

            if (result.GetElement(0) != value)
            {
                return 0;
            }

            for (int i = 1; i < Vector128<ushort>.Count; i++)
            {
                if (result.GetElement(i) != 0)
                {
                    return 0;
                }
            }

            return 100;
        }
    }
}
