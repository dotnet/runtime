// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Reflection;

namespace System.Security.Cryptography
{
    internal static class CryptoPool
    {
        internal const int ClearAll = -1;

        private static readonly Func<int, bool, byte[]> s_allocateArray = GetAllocateArray();

        internal static byte[] Rent(int minimumLength) => ArrayPool<byte>.Shared.Rent(minimumLength);

        internal static void Return(ArraySegment<byte> arraySegment)
        {
            Debug.Assert(arraySegment.Array != null);
            Debug.Assert(arraySegment.Offset == 0);

            Return(arraySegment.Array, arraySegment.Count);
        }

        internal static void Return(byte[] array, int clearSize = ClearAll)
        {
            Debug.Assert(clearSize <= array.Length);
            bool clearWholeArray = clearSize < 0;

            if (!clearWholeArray && clearSize != 0)
            {
#if NETCOREAPP || NETSTANDARD2_1
                CryptographicOperations.ZeroMemory(array.AsSpan(0, clearSize));
#else
                Array.Clear(array, 0, clearSize);
#endif
            }

            ArrayPool<byte>.Shared.Return(array, clearWholeArray);
        }

        internal static byte[] AllocateArray(int length, bool pinned)
        {
            // Reflection-bound to GC.AllocateArray<byte>.
            return s_allocateArray(length, pinned);
        }

        private static Func<int, bool, byte[]> GetAllocateArray()
        {
#if NETCOREAPP3_0 || NETSTANDARD || NETFRAMEWORK
            MethodInfo? methodInfo = typeof(GC).GetMethod("AllocateArray");

            if (methodInfo != null)
            {
                MethodInfo generic = methodInfo.MakeGenericMethod(typeof(byte));
                return (Func<int, bool, byte[]>)generic.CreateDelegate(typeof(Func<int, bool, byte[]>));
            }

            return (int length, bool pinned) => new byte[length];
#else
            return GC.AllocateArray<byte>;
#endif
        }
    }
}
