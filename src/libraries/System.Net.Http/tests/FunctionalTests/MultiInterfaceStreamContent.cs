// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public class MultiInterfaceStreamContent : StreamContent
    {
        Stream _content;

        public MultiInterfaceStreamContent(Stream content) : base(content)
        {
            _content = content;
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            return Task.FromResult<Stream>(_content);
        }
    }
}
