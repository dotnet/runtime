// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid("5A9D3ED6-CC17-4FB9-8F82-0070489B7213")]
    internal partial interface INullableOutArray
    {
        void Method(int size, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] IntStructWrapper[]? array, [MarshalAs(UnmanagedType.Bool)] bool passedNull);
        // Definition
        void M(
            int bufferSize,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] IntStructWrapper[]? buffer1,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), In, Out] IntStructWrapper[]? buffer2);
    }

    [GeneratedComClass]
    internal partial class INullableOutArrayImpl : INullableOutArray
    {
        public void M(int bufferSize, IntStructWrapper[]? buffer1, IntStructWrapper[]? buffer2)
        {
            if (buffer1 is not null)
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer1[i] = new() { Value = i };
                }
            }
            if (buffer2 is not null)
            {
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer2[i] = new() { Value = i };
                }
            }
        }

        public void Method(int size, IntStructWrapper[]? array, bool passedNull)
        {
            if (passedNull)
            {
                if (array is not null)
                {
                    throw new ArgumentException("Expected array to be null when passedNull is true.");
                }
                return;
            }
            if (size == 0 || array is null)
            {
                return;
            }
            for (int i = 0; i < size; i++)
            {
                array[i] = new() { Value = i };
            }
        }
    }
}
