// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public sealed class SyncBlockingContent : StringContent
    {
        byte[] _content;

        public SyncBlockingContent(string content) : base(content)
        {
            _content = Encoding.UTF8.GetBytes(content);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            stream.Write(_content, 0, _content.Length);
            return Task.CompletedTask;
        }
    }
}
