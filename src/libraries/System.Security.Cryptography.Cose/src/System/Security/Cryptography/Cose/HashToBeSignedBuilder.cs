// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal sealed class HashToBeSignedBuilder : ToBeSignedBuilder
    {
        private IncrementalHash _incrementalHash;

        internal HashToBeSignedBuilder(HashAlgorithmName hashAlgorithmName)
        {
            _incrementalHash = IncrementalHash.CreateHash(hashAlgorithmName);
        }

        internal override void AppendToBeSigned(ReadOnlySpan<byte> data)
        {
            _incrementalHash.AppendData(data);
        }

        internal override void WithDataAndResetAfterOperation(Span<byte> arg, ToBeSignedOperation operation)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            operation(arg, _incrementalHash.GetHashAndReset());
#else
            Debug.Assert(_incrementalHash.HashLengthInBytes <= 512 / 8); // largest hash we can get (SHA512).
            Span<byte> hash = stackalloc byte[_incrementalHash.HashLengthInBytes];
            _incrementalHash.GetHashAndReset(hash);
            operation(arg, hash);
#endif
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _incrementalHash.Dispose();
                _incrementalHash = null!;
            }

            base.Dispose(disposing);
        }
    }
}
