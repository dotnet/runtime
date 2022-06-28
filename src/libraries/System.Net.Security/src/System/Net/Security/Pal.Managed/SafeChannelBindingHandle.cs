// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;

namespace System.Net.Security
{
    internal sealed class SafeChannelBindingHandle : ChannelBinding
    {
        private const int CertHashMaxSize = 128;
        private static readonly int s_secChannelBindingSize = Marshal.SizeOf<SecChannelBindings>();

        private readonly int _cbtPrefixByteArraySize;
        internal int Length { get; private set; }
        internal IntPtr CertHashPtr { get; }
        public override int Size => Length;

        internal void SetCertHash(byte[] certHashBytes)
        {
            Debug.Assert(certHashBytes != null, "check certHashBytes is not null");
            Debug.Assert(certHashBytes.Length <= CertHashMaxSize);

            int length = certHashBytes.Length;
            Marshal.Copy(certHashBytes, 0, CertHashPtr, length);
            SetCertHashLength(length);
        }

        internal unsafe SafeChannelBindingHandle(ChannelBindingKind kind)
        {
            Debug.Assert(kind == ChannelBindingKind.Endpoint || kind == ChannelBindingKind.Unique);
            ReadOnlySpan<byte> cbtPrefix = kind == ChannelBindingKind.Endpoint ?
                "tls-server-end-point:"u8 :
                "tls-unique:"u8;

            _cbtPrefixByteArraySize = cbtPrefix.Length;
            handle = Marshal.AllocHGlobal(s_secChannelBindingSize + _cbtPrefixByteArraySize + CertHashMaxSize);
            IntPtr cbtPrefixPtr = handle + s_secChannelBindingSize;
            cbtPrefix.CopyTo(new Span<byte>((byte*)cbtPrefixPtr, cbtPrefix.Length));
            CertHashPtr = cbtPrefixPtr + _cbtPrefixByteArraySize;
            Length = CertHashMaxSize;
        }

        internal void SetCertHashLength(int certHashLength)
        {
            int cbtLength = _cbtPrefixByteArraySize + certHashLength;
            Length = s_secChannelBindingSize + cbtLength;

            SecChannelBindings channelBindings = new SecChannelBindings()
            {
                ApplicationDataLength = cbtLength,
                ApplicationDataOffset = s_secChannelBindingSize
            };
            Marshal.StructureToPtr(channelBindings, handle, true);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
