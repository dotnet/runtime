// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        public delegate void TryCode(object userData);

        public delegate void CleanupCode(object userData, bool exceptionThrown);

        /// <summary>
        /// GetSubArray helper method for the compiler to slice an array using a range.
        /// </summary>
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            Type elementType = array.GetType().GetElementType();
            Span<T> source = array.AsSpan(range);

            if (elementType.IsValueType)
            {
                return source.ToArray();
            }
            else
            {
                T[] newArray = (T[])Array.CreateInstance(elementType, source.Length);
                source.CopyTo(newArray);
                return newArray;
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