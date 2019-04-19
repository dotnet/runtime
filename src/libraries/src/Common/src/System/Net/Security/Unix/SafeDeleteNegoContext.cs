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
        private SafeGssNameHandle _targetName;
        private SafeGssContextHandle _context;
        private bool _isNtlmUsed;
        
        public SafeGssNameHandle TargetName
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
        }

        public SafeDeleteNegoContext(SafeFreeNegoCredentials credential, string targetName)
            : this(credential)
        {
            try
            {
                // Convert any "SERVICE/HOST" style of targetName to use "SERVICE@HOST" style.
                // This is because the System.Net.Security.Native GSS-API layer uses
                // GSS_C_NT_HOSTBASED_SERVICE format for targetName.
                _targetName = SafeGssNameHandle.CreateTarget(targetName.Replace('/', '@'));
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

                if (_targetName != null)
                {
                    _targetName.Dispose();
                    _targetName = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
