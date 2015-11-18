// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Http
{
    internal class CurlChannelBinding : ChannelBinding
    {
        private SafeChannelBindingHandle _bindingHandle;
        private string _description = string.Empty;

        internal CurlChannelBinding()
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return _bindingHandle == null ? true : _bindingHandle.IsInvalid;
            }
        }

        public override int Size
        {
            get
            {
                return _bindingHandle == null ? 0 : _bindingHandle.Length;
            }
        }

        public override string ToString()
        {
            return _description;
        }

        protected override bool ReleaseHandle()
        {
            if (_bindingHandle != null)
            {
                _bindingHandle.Dispose();
                SetHandle(IntPtr.Zero);
            }
            return true;
        }

        internal void SetToken(X509Certificate2 cert)
        {
            // Parity with WinHTTP : CurHandler only supports retrieval of ChannelBindingKind.Endpoint for CBT.
            _bindingHandle = new SafeChannelBindingHandle(ChannelBindingKind.Endpoint);
            using (HashAlgorithm hashAlgo = Interop.OpenSsl.GetHashForChannelBinding(cert))
            {
                byte[] bindingHash = hashAlgo.ComputeHash(cert.RawData);
                _bindingHandle.SetCertHash(bindingHash);
                _description = BitConverter.ToString(bindingHash).Replace('-', ' ');
                SetHandle(_bindingHandle.DangerousGetHandle());
            }
        }
    }
}
