// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public interface ICryptoTransform : IDisposable
    {
        int InputBlockSize { get; }

        int OutputBlockSize { get; }

        // CanTransformMultipleBlocks == true implies that TransformBlock() can accept any number
        // of whole blocks, not just a single block.  If CanTransformMultipleBlocks is false, you have
        // to feed blocks one at a time.  
        bool CanTransformMultipleBlocks { get; }

        // If CanReuseTransform is true, then after a call to TransformFinalBlock() the transform
        // resets its internal state to its initial configuration (with Key and IV loaded) and can
        // be used to perform another encryption/decryption.
        bool CanReuseTransform { get; }

        // The return value of TransformBlock is the number of bytes returned to outputBuffer and is
        // always <= OutputBlockSize.  If CanTransformMultipleBlocks is true, then inputCount may be
        // any positive multiple of InputBlockSize
        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);

        // Special function for transforming the last block or partial block in the stream.  The
        // return value is an array containing the remaining transformed bytes.
        // We return a new array here because the amount of information we send back at the end could 
        // be larger than a single block once padding is accounted for.
        byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
    }
}
