// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
