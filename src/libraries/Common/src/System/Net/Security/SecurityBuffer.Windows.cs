// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    // Until we have stackalloc Span<ReferenceType> support, these two
    // structs allow us to do the equivalent of stackalloc SecurityBuffer[2]
    // and stackalloc SecurityBuffer[3], with code like:
    //     TwoSecurityBuffers tmp = default;
    //     Span<SecurityBuffer> buffers = MemoryMarshal.CreateSpan<ref tmp._item0, 2);

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct TwoSecurityBuffers
    {
        internal SecurityBuffer _item0;
        private SecurityBuffer _item1;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct ThreeSecurityBuffers
    {
        internal SecurityBuffer _item0;
        private SecurityBuffer _item1;
        private SecurityBuffer _item2;
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct SecurityBuffer
    {
        public int offset;
        public int size;
        public SecurityBufferType type;
        public byte[] token;
        public SafeHandle unmanagedToken;

        public SecurityBuffer(byte[] data, int offset, int size, SecurityBufferType tokentype)
        {
            if (offset < 0 || offset > (data == null ? 0 : data.Length))
            {
                NetEventSource.Fail(typeof(SecurityBuffer), $"'offset' out of range.  [{offset}]");
            }

            if (size < 0 || size > (data == null ? 0 : data.Length - offset))
            {
                NetEventSource.Fail(typeof(SecurityBuffer), $"'size' out of range.  [{size}]");
            }

            this.offset = data == null || offset < 0 ? 0 : Math.Min(offset, data.Length);
            this.size = data == null || size < 0 ? 0 : Math.Min(size, data.Length - this.offset);
            this.type = tokentype;
            this.token = size == 0 ? null : data;
            this.unmanagedToken = null;
        }

        public SecurityBuffer(byte[] data, SecurityBufferType tokentype)
        {
            this.offset = 0;
            this.size = data == null ? 0 : data.Length;
            this.type = tokentype;
            this.token = size == 0 ? null : data;
            this.unmanagedToken = null;
        }

        public SecurityBuffer(int size, SecurityBufferType tokentype)
        {
            if (size < 0)
            {
                NetEventSource.Fail(typeof(SecurityBuffer), $"'size' out of range.  [{size}]");
            }

            this.offset = 0;
            this.size = size;
            this.type = tokentype;
            this.token = size == 0 ? null : new byte[size];
            this.unmanagedToken = null;
        }

        public SecurityBuffer(ChannelBinding binding)
        {
            this.offset = 0;
            this.size = (binding == null ? 0 : binding.Size);
            this.type = SecurityBufferType.SECBUFFER_CHANNEL_BINDINGS;
            this.token = null;
            this.unmanagedToken = binding;
        }
    }
}
