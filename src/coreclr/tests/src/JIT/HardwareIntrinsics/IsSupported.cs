// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Intrinsics.X86;

namespace IntelHardwareIntrinsicTest
{
    class Program
    {
        static int Main(string[] args)
        {
            bool result = true;
            if (Avx2.IsSupported)
            {
                if (Avx.IsSupported)
                {
                    if (!Sse42.IsSupported)
                    {
                        result = false;
                    }

                    if (Sse41.IsSupported)
                    {
                        if (Ssse3.IsSupported)
                        {
                            if (Sse3.IsSupported)
                            {
                                if (Sse2.IsSupported && Sse.IsSupported)
                                {
                                    result = result && true;
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                            else
                            {
                                result = false;
                            }
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else
                    {
                        result = false;
                    }
                }
                if (Aes.IsSupported && Bmi1.IsSupported && Bmi2.IsSupported && Fma.IsSupported && 
                        Lzcnt.IsSupported && Popcnt.IsSupported && Pclmulqdq.IsSupported)
                    {
                        result = result && true;
                    }
                    else
                    {
                        result = false;
                    }
            }

            // Non-X86 platforms
            if (!(Sse.IsSupported))
            {
                if (Sse2.IsSupported  ||
                    Sse3.IsSupported  ||
                    Ssse3.IsSupported ||
                    Sse41.IsSupported ||
                    Sse42.IsSupported ||
                    Avx.IsSupported   ||
                    Avx2.IsSupported  ||
                    Aes.IsSupported   ||
                    Bmi1.IsSupported  ||
                    Bmi2.IsSupported  ||
                    Fma.IsSupported   ||
                    Lzcnt.IsSupported ||
                    Popcnt.IsSupported||
                    Pclmulqdq.IsSupported)
                {
                    result = false;
                }
            }
            return result ? 100 : 0;
        }

    }
}