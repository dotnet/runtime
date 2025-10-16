// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Security.Cryptography.Cose
{
    internal abstract class ToBeSignedBuilder : IDisposable
    {
        // arg is passthrough - we don't do anything with it but all usages need to pass in extra Span and it's not allowed to do through closure.
        internal delegate void ToBeSignedOperation(Span<byte> arg, ReadOnlySpan<byte> data);

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        internal abstract void AppendToBeSigned(ReadOnlySpan<byte> data);

        // arg is passthrough - we don't do anything with it but all usages need to pass in extra Span and it's not allowed to do through closure.
        internal abstract void WithDataAndResetAfterOperation(Span<byte> arg, ToBeSignedOperation operation);

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
