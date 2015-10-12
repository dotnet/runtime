// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
