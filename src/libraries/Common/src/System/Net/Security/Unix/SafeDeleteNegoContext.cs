// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal sealed class SafeDeleteNegoContext : SafeDeleteContext
    {
        private SafeGssCredHandle? _acceptorCredential;
        private SafeGssNameHandle? _targetName;
        private SafeGssContextHandle _context;
        private bool _isNtlmUsed;

        public SafeGssCredHandle AcceptorCredential
        {
            get
            {
                _acceptorCredential ??= SafeGssCredHandle.CreateAcceptor();
                return _acceptorCredential;
            }
        }

        public SafeGssNameHandle? TargetName
        {
            get { return _targetName; }
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

        public SafeDeleteNegoContext(SafeFreeNegoCredentials credential)
            : base(credential)
        {
            Debug.Assert((null != credential), "Null credential in SafeDeleteNegoContext");
            _context = new SafeGssContextHandle();
        }

        public SafeDeleteNegoContext(SafeFreeNegoCredentials credential, string targetName)
            : this(credential)
        {
            try
            {
                _targetName = SafeGssNameHandle.CreateTarget(targetName);
                _context = new SafeGssContextHandle();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void SetGssContext(SafeGssContextHandle context)
        {
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
                _context.Dispose();

                if (_targetName != null)
                {
                    _targetName.Dispose();
                    _targetName = null;
                }

                if (_acceptorCredential != null)
                {
                    _acceptorCredential.Dispose();
                    _acceptorCredential = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
