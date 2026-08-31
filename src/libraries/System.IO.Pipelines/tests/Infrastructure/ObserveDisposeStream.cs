// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines.Tests
{
    public class ObserveDisposeStream : ReadOnlyStream
    {
        public int DisposedCount { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            DisposedCount++;

            base.Dispose(disposing);
        }
    }
}
