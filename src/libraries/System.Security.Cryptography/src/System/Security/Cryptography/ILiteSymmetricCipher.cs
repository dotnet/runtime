// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Security.Cryptography
{
    internal interface ILiteSymmetricCipher : IDisposable
    {
        int TransformFinal(ReadOnlySpan<byte> input, Span<byte> output);
        int Transform(ReadOnlySpan<byte> input, Span<byte> output);
        void Reset(ReadOnlySpan<byte> iv);

        int BlockSizeInBytes { get; }
        int PaddingSizeInBytes { get; }
    }
}
