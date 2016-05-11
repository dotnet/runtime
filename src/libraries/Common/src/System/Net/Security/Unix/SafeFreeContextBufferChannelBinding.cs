// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal sealed class SafeFreeContextBufferChannelBinding : ChannelBinding
    {
        private readonly SafeChannelBindingHandle _channelBinding = null;

        public override int Size
        {
            get { return _channelBinding.Length; }
        }

        public override bool IsInvalid
        {
            get { return _channelBinding.IsInvalid; }
        }

        public SafeFreeContextBufferChannelBinding(SafeChannelBindingHandle binding)
        {
            Debug.Assert(null != binding && !binding.IsInvalid, "input channelBinding is invalid");
            bool gotRef = false;
            binding.DangerousAddRef(ref gotRef);
            handle = binding.DangerousGetHandle();
            _channelBinding = binding;
        }

        protected override bool ReleaseHandle()
        {
            _channelBinding.DangerousRelease();
            _channelBinding.Dispose();
            return true;
        }
    }
}
