// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        public delegate void TryCode(object userData);

        public delegate void CleanupCode(object userData, bool exceptionThrown);

        /// <summary>
        /// Slices the specified array using the specified range.
        /// </summary>
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            Range.OffsetAndLength offLen = range.GetOffsetAndLength(array.Length);

            if (default(T) != null || typeof(T[]) == array.GetType())
            {
                // We know the type of the array to be exactly T[].

                if (offLen.Length == 0)
                {
                    return Array.Empty<T>();
                }

                var dest = new T[offLen.Length];
                Buffer.Memmove(
                    ref Unsafe.As<byte, T>(ref dest.GetRawSzArrayData()),
                    ref Unsafe.Add(ref Unsafe.As<byte, T>(ref array.GetRawSzArrayData()), offLen.Offset),
                    (uint)offLen.Length);
                return dest;
            }
            else
            {
                // The array is actually a U[] where U:T.
                T[] dest = (T[])Array.CreateInstance(array.GetType().GetElementType(), offLen.Length);
                Array.Copy(array, offLen.Offset, dest, 0, offLen.Length);
                return dest;
            }
        }

        public static object GetUninitializedObject(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type), SR.ArgumentNull_Type);
            }
            
            if (!type.IsRuntimeImplemented())
            {
                throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type.ToString()));
            }

            return GetUninitializedObjectInternal(type);
        }

        public static void PrepareContractedDelegate(Delegate d)
        {
        }

        public static void ProbeForSufficientStack()
        {
        }

        public static void PrepareConstrainedRegions()
        {
        }

        public static void PrepareConstrainedRegionsNoOP()
        {
        }
    }
}
