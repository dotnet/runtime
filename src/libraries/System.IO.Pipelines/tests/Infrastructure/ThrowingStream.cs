// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines.Tests
{
    public class ThrowingStream : ThrowAfterNWritesStream
    {
        public ThrowingStream() : base(0)
        {
        }
    }
}
