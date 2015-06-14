// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Internal.Cryptography.Pal.Native
{
    /// <summary>
    /// Base class for safe handles representing NULL-based pointers.
    /// </summary>
    internal abstract class SafePointerHandle<T> : SafeHandle where T : SafeHandle, new()
    {
        protected SafePointerHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public sealed override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        public static T InvalidHandle
        {
            get { return SafeHandleCache<T>.GetInvalidHandle(() => new T()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<T>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// SafeHandle for the CERT_CONTEXT structure defined by crypt32.
    /// </summary>
    internal class SafeCertContextHandle : SafePointerHandle<SafeCertContextHandle>
    {
        protected override bool ReleaseHandle()
        {
            Interop.crypt32.CertFreeCertificateContext(handle);  // CertFreeCertificateContext always returns true so checking the return value is pointless.
            return true;
        }

        public unsafe CERT_CONTEXT* CertContext
        {
            get { return (CERT_CONTEXT*)handle; }
        }

        // Extract the raw CERT_CONTEXT* pointer and reset the SafeHandle to the invalid state so it no longer auto-destroys the CERT_CONTEXT.
        public unsafe CERT_CONTEXT* Disconnect()
        {
            CERT_CONTEXT* pCertContext = (CERT_CONTEXT*)handle;
            SetHandle(IntPtr.Zero);
            return pCertContext;
        }

        public bool ContainsPrivateKey
        {
            get
            {
                int cb = 0;
                bool containsPrivateKey = Interop.crypt32.CertGetCertificateContextProperty(this, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cb);
                return containsPrivateKey;
            }
        }

        public SafeCertContextHandle Duplicate()
        {
            return Interop.crypt32.CertDuplicateCertificateContext(handle);
        }
    }

    /// <summary>
    /// SafeHandle for the CERT_CONTEXT structure defined by crypt32. Unlike SafeCertContextHandle, disposition already deletes any associated key containers.
    /// </summary>
    internal sealed class SafeCertContextHandleWithKeyContainerDeletion : SafeCertContextHandle
    {
        protected sealed override bool ReleaseHandle()
        {
            using (SafeCertContextHandle certContext = Interop.crypt32.CertDuplicateCertificateContext(handle))
            {
                DeleteKeyContainer(certContext);
            }
            base.ReleaseHandle();
            return true;
        }

        public static void DeleteKeyContainer(SafeCertContextHandle pCertContext)
        {
            if (pCertContext.IsInvalid)
                return;

            int cb = 0;
            bool containsPrivateKey = Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cb);
            if (!containsPrivateKey)
                return;

            byte[] provInfoAsBytes = new byte[cb];
            if (!Interop.crypt32.CertGetCertificateContextProperty(pCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, provInfoAsBytes, ref cb))
                return;

            unsafe
            {
                fixed (byte* pProvInfoAsBytes = provInfoAsBytes)
                {
                    CRYPT_KEY_PROV_INFO* pProvInfo = (CRYPT_KEY_PROV_INFO*)pProvInfoAsBytes;
                    CryptAcquireContextFlags flags = (pProvInfo->dwFlags & CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) | CryptAcquireContextFlags.CRYPT_DELETEKEYSET;
                    IntPtr hProv;
                    bool success = Interop.advapi32.CryptAcquireContext(out hProv, pProvInfo->pwszContainerName, pProvInfo->pwszProvName, pProvInfo->dwProvType, flags);

                    // Called CryptAcquireContext solely for the side effect of deleting the key containers. When called with these flags, no actual
                    // hProv is returned (so there's nothing to clean up.)
                    Debug.Assert(hProv == IntPtr.Zero);
                }
            }
        }
    }

    /// <summary>
    /// SafeHandle for the HCERTSTORE handle defined by crypt32.
    /// </summary>
    internal sealed class SafeCertStoreHandle : SafePointerHandle<SafeCertStoreHandle>
    {
        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.crypt32.CertCloseStore(handle, 0);
            return success;
        }
    }

    /// <summary>
    /// SafeHandle for the HCRYPTMSG handle defined by crypt32.
    /// </summary>
    internal sealed class SafeCryptMsgHandle : SafePointerHandle<SafeCryptMsgHandle>
    {
        protected sealed override bool ReleaseHandle()
        {
            bool success = Interop.crypt32.CryptMsgClose(handle);
            return success;
        }
    }

    /// <summary>
    /// SafeHandle for LocalAlloc'd memory.
    /// </summary>
    internal sealed class SafeLocalAllocHandle : SafePointerHandle<SafeLocalAllocHandle>
    {
        public static SafeLocalAllocHandle Create(int cb)
        {
            var h = new SafeLocalAllocHandle();
            h.SetHandle(Marshal.AllocHGlobal(cb));
            return h;
        }

        protected sealed override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
