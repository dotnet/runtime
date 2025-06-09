// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal sealed class PureDataToBeSignedBuilder : ToBeSignedBuilder
    {
        private MemoryStream _stream;

        internal PureDataToBeSignedBuilder()
        {
            _stream = new MemoryStream();
        }

        internal override void AppendToBeSigned(ReadOnlySpan<byte> data)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            _stream.Write(data.ToArray(), 0, data.Length);
#else
            _stream.Write(data);
#endif
        }

        internal override void WithDataAndResetAfterOperation(Span<byte> arg, ToBeSignedOperation operation)
        {
#if NETSTANDARD2_0 || NETFRAMEWORK
            operation(arg, _stream.ToArray());
#else
            if (_stream.Length <= 128)
            {
                Span<byte> data = stackalloc byte[(int)_stream.Length];
                _stream.Position = 0;
                _stream.ReadExactly(data);
                operation(arg, data);
            }
            else
            {
                operation(arg, _stream.ToArray());
            }
#endif

            _stream.Position = 0;
            _stream.SetLength(0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
