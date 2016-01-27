// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public sealed class RepeatedFlushContent : StringContent
    {
        public RepeatedFlushContent(string content) : base(content)
        {
        }
        
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            stream.Flush();
            stream.Flush();
            return base.SerializeToStreamAsync(stream, context);
        }
    }
}
