// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Intrinsics.X86;
using System.Numerics;

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
            }

            if (Vector<byte>.Count == 32 && !Avx2.IsSupported)
            {
                result = false;
            }

            if (Vector<byte>.Count == 16 && Vector.IsHardwareAccelerated && !Sse2.IsSupported)
            {
                result = false;
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

            // Reflection call
            var issupported = "get_IsSupported";
            if (Convert.ToBoolean(typeof(Sse).GetMethod(issupported).Invoke(null, null)) != Sse.IsSupported ||
                Convert.ToBoolean(typeof(Sse2).GetMethod(issupported).Invoke(null, null)) != Sse2.IsSupported ||
                Convert.ToBoolean(typeof(Sse3).GetMethod(issupported).Invoke(null, null)) != Sse3.IsSupported ||
                Convert.ToBoolean(typeof(Ssse3).GetMethod(issupported).Invoke(null, null)) != Ssse3.IsSupported ||
                Convert.ToBoolean(typeof(Sse41).GetMethod(issupported).Invoke(null, null)) != Sse41.IsSupported ||
                Convert.ToBoolean(typeof(Sse42).GetMethod(issupported).Invoke(null, null)) != Sse42.IsSupported ||
                Convert.ToBoolean(typeof(Aes).GetMethod(issupported).Invoke(null, null)) != Aes.IsSupported ||
                Convert.ToBoolean(typeof(Avx).GetMethod(issupported).Invoke(null, null)) != Avx.IsSupported ||
                Convert.ToBoolean(typeof(Avx2).GetMethod(issupported).Invoke(null, null)) != Avx2.IsSupported ||
                Convert.ToBoolean(typeof(Fma).GetMethod(issupported).Invoke(null, null)) != Fma.IsSupported ||
                Convert.ToBoolean(typeof(Lzcnt).GetMethod(issupported).Invoke(null, null)) != Lzcnt.IsSupported ||
                Convert.ToBoolean(typeof(Bmi1).GetMethod(issupported).Invoke(null, null)) != Bmi1.IsSupported ||
                Convert.ToBoolean(typeof(Bmi2).GetMethod(issupported).Invoke(null, null)) != Bmi2.IsSupported ||
                Convert.ToBoolean(typeof(Popcnt).GetMethod(issupported).Invoke(null, null)) != Popcnt.IsSupported ||
                Convert.ToBoolean(typeof(Pclmulqdq).GetMethod(issupported).Invoke(null, null)) != Pclmulqdq.IsSupported
            )
            {
                result = false;
            }
            return result ? 100 : 0;
        }

    }
}