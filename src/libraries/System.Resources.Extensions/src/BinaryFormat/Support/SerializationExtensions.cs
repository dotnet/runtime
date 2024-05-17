// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Windows.Forms.BinaryFormat;

internal static class SerializationExtensions
{
    /// <summary>
    ///  Get a typed value. Hard casts.
    /// </summary>
    public static T? GetValue<T>(this SerializationInfo info, string name) => (T?)info.GetValue(name, typeof(T));

    /// <summary>
    ///  Converts the given exception to a <see cref="SerializationException"/> if needed, nesting the original exception
    ///  and assigning the original stack trace.
    /// </summary>
    public static SerializationException ConvertToSerializationException(this Exception ex)
        => ex is SerializationException serializationException
            ? serializationException
            : (SerializationException)ExceptionDispatchInfo.SetRemoteStackTrace(
                new SerializationException(ex.Message, ex),
                ex.StackTrace ?? string.Empty);

    /// <summary>
    ///  Gets a span over any array, including multi-dimensional arrays.
    /// </summary>
    public static Span<T> GetArrayData<T>(this Array array)
    {
        if (array.GetType().UnderlyingSystemType.IsAssignableFrom(typeof(T)))
        {
            throw new InvalidCastException($"Cannot cast array of type {array.GetType().UnderlyingSystemType} to {typeof(T)}.");
        }

        Span<T> data = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T>(
            ref MemoryMarshal.GetArrayDataReference(array)),
            checked((int)array.LongLength));

        return data;
    }

    /// <summary>
    ///  Sets a value in an array by its flattened index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetArrayValueByFlattenedIndex(this Array array, object? value, int flattenedIndex)
    {
        // Fast path
        int ranks = array.Rank;
        if (ranks == 1)
        {
            array.SetValue(value, flattenedIndex);
            return;
        }

        SetArrayValueByFlattenedIndex(array, flattenedIndex, value);

        static void SetArrayValueByFlattenedIndex(Array array, int flattenedIndex, object? value)
        {
            int ranks = array.Rank;

            if (ranks == 2)
            {
                int length = array.GetLength(1);
                (int index1, int index2) = int.DivRem(flattenedIndex, length);
                array.SetValue(value, index1, index2);
                return;
            }

            Span<int> rankProducts = stackalloc int[ranks];
            rankProducts[ranks - 1] = 1;
            for (int i = ranks - 2; i >= 0; i--)
            {
                rankProducts[i] = array.GetLength(i + 1) * rankProducts[i + 1];
            }

            Span<int> indices = stackalloc int[ranks];
            for (int i = 0; i < ranks; i++)
            {
                indices[i] = flattenedIndex / rankProducts[i];
                flattenedIndex -= indices[i] * rankProducts[i];
            }

            array.SetValue(value, indices.ToArray());
        }
    }
}
