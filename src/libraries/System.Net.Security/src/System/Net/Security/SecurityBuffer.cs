// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    internal class SecurityBuffer
    {
        public int size;
        public SecurityBufferType type;
        public byte[] token;
        public SafeHandle unmanagedToken;
        public int offset;

        public SecurityBuffer(byte[] data, int offset, int size, SecurityBufferType tokentype)
        {
            if (offset < 0 || offset > (data == null ? 0 : data.Length))
            {
                NetEventSource.Fail(this, $"'offset' out of range.  [{offset}]");
            }

            if (size < 0 || size > (data == null ? 0 : data.Length - offset))
            {
                NetEventSource.Fail(this, $"'size' out of range.  [{size}]");
            }

            this.offset = data == null || offset < 0 ? 0 : Math.Min(offset, data.Length);
            this.size = data == null || size < 0 ? 0 : Math.Min(size, data.Length - this.offset);
            this.type = tokentype;
            this.token = size == 0 ? null : data;
        }

        public SecurityBuffer(byte[] data, SecurityBufferType tokentype)
        {
            this.size = data == null ? 0 : data.Length;
            this.type = tokentype;
            this.token = size == 0 ? null : data;
        }

        public SecurityBuffer(int size, SecurityBufferType tokentype)
        {
            if (size < 0)
            {
                NetEventSource.Fail(this, $"'size' out of range.  [{size}]");
            }

            this.size = size;
            this.type = tokentype;
            this.token = size == 0 ? null : new byte[size];
        }

        public SecurityBuffer(ChannelBinding binding)
        {
            this.size = (binding == null ? 0 : binding.Size);
            this.type = SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS;
            this.unmanagedToken = binding;
        }
    }
}
