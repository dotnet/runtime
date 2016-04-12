// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Http
{
    internal sealed class CurlChannelBinding : ChannelBinding
    {
        private SafeChannelBindingHandle _bindingHandle;
        private byte[] _bindingHash;

        public override bool IsInvalid => _bindingHandle == null ? true : _bindingHandle.IsInvalid;

        public override int Size => _bindingHandle == null ? 0 : _bindingHandle.Length;

        public override string ToString() => 
            _bindingHash != null && !IsInvalid ? BitConverter.ToString(_bindingHash).Replace('-', ' ') : null;

        protected override bool ReleaseHandle()
        {
            if (_bindingHandle != null)
            {
                SetHandle(IntPtr.Zero);
                _bindingHandle.Dispose();
                _bindingHandle = null;
            }
            return true;
        }

        internal void SetToken(X509Certificate2 cert)
        {
            // Parity with WinHTTP: only support retrieval of CBT for ChannelBindingKind.Endpoint.
            _bindingHandle = new SafeChannelBindingHandle(ChannelBindingKind.Endpoint);
            using (HashAlgorithm hashAlgo = Interop.OpenSsl.GetHashForChannelBinding(cert))
            {
                _bindingHash = hashAlgo.ComputeHash(cert.RawData);
                _bindingHandle.SetCertHash(_bindingHash);
                SetHandle(_bindingHandle.DangerousGetHandle());
            }
        }
    }
}
