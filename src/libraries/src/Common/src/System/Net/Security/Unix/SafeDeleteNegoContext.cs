// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal sealed class SafeDeleteNegoContext : SafeDeleteContext
    {
        private SafeGssNameHandle _targetNameKerberos;
        private SafeGssNameHandle _targetNameNtlm;
        private SafeGssContextHandle _context;
        private bool _isNtlmFallback;
        private bool _isNtlmUsed;

        public SafeGssNameHandle TargetNameKerberos
        {
            get { return _targetNameKerberos; }
        }

        public SafeGssNameHandle TargetNameNtlm
        {
            get { return _targetNameNtlm; }
        }

        // Property represents if SPNEGO needed to fall back from Kerberos to NTLM when
        // generating initial context token.
        public bool IsNtlmFallback
        {
            get { return _isNtlmFallback; }
            set { _isNtlmFallback = value; }
        }

        // Property represents if final protocol negotiated is Ntlm or not.
        public bool IsNtlmUsed
        {
            get { return _isNtlmUsed; }
        }

        public SafeGssContextHandle GssContext
        {
            get { return _context; }
        }

        public SafeDeleteNegoContext(SafeFreeNegoCredentials credential, string targetName)
            : base(credential)
        {
            Debug.Assert((null != credential), "Null credential in SafeDeleteNegoContext");
            try
            {
                _targetNameKerberos = SafeGssNameHandle.CreateTarget(targetName, isNtlmTarget: false);
                _targetNameNtlm = SafeGssNameHandle.CreateTarget(targetName, isNtlmTarget: true);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void SetGssContext(SafeGssContextHandle context)
        {
            Debug.Assert(context != null && !context.IsInvalid, "Invalid context passed to SafeDeleteNegoContext");
            _context = context;
        }

        public void SetAuthenticationPackage(bool isNtlmUsed)
        {
            _isNtlmUsed = isNtlmUsed;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != _context)
                {
                    _context.Dispose();
                    _context = null;
                }

                if (_targetNameKerberos != null)
                {
                    _targetNameKerberos.Dispose();
                    _targetNameKerberos = null;
                }

                if (_targetNameNtlm != null)
                {
                    _targetNameNtlm.Dispose();
                    _targetNameNtlm = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
