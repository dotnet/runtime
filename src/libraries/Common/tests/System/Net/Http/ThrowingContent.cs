// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    /// <summary>HttpContent that mocks exceptions on serialization.</summary>
    public partial class ThrowingContent : HttpContent
    {
        private readonly Func<Exception> _exnFactory;
        private readonly int _length;

        public ThrowingContent(Func<Exception> exnFactory, int length = 10)
        {
            _exnFactory = exnFactory;
            _length = length;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            throw _exnFactory();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _length;
            return true;
        }
    }

    public class ThrowingContentException : Exception
    { }
}
