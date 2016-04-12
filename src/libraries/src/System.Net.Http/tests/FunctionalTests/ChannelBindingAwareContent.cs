// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    internal sealed class ChannelBindingAwareContent : HttpContent
    {
        private readonly byte[] _content;

        public ChannelBindingAwareContent(string content)
        {
            _content = Encoding.UTF8.GetBytes(content);
        }
        
        public ChannelBinding ChannelBinding { get ; private set; }
        
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            ChannelBinding = context.GetChannelBinding(ChannelBindingKind.Endpoint);
            return stream.WriteAsync(_content, 0, _content.Length);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _content.Length;
            return true;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(new MemoryStream(_content, 0, _content.Length, writable: false));
        }
    }
}
