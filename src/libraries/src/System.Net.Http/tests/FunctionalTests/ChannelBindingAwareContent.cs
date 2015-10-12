// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public sealed class ChannelBindingAwareContent : StringContent
    {
        public ChannelBindingAwareContent(string content) : base(content)
        {
        }
        
        public ChannelBinding ChannelBinding { get ; private set; }
        
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            ChannelBinding = context.GetChannelBinding(ChannelBindingKind.Endpoint);
            
            return base.SerializeToStreamAsync(stream, context);
        }
    }
}
