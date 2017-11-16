// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
        private SafeCertContextHandle _parent;

        public SafeCertContextHandle() { }

        public SafeCertContextHandle(SafeCertContextHandle parent)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            Debug.Assert(!parent.IsInvalid);
            Debug.Assert(!parent.IsClosed);

            bool ignored = false;
            parent.DangerousAddRef(ref ignored);
            _parent = parent;

            SetHandle(_parent.handle);
        }

        protected override bool ReleaseHandle()
        {
            if (_parent != null)
            {
                _parent.DangerousRelease();
                _parent = null;
            }
            else
            {
                Interop.crypt32.CertFreeCertificateContext(handle);
            }

            SetHandle(IntPtr.Zero);
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

        public bool HasPersistedPrivateKey
        {
            get { return CertHasProperty(CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID); }
        }

        public bool HasEphemeralPrivateKey
        {
            get { return CertHasProperty(CertContextPropId.CERT_KEY_CONTEXT_PROP_ID); }
        }

        public bool ContainsPrivateKey
        {
            get { return HasPersistedPrivateKey || HasEphemeralPrivateKey; }
        }

        public SafeCertContextHandle Duplicate()
        {
            return Interop.crypt32.CertDuplicateCertificateContext(handle);
        }

        private bool CertHasProperty(CertContextPropId propertyId)
        {
            int cb = 0;
            bool hasProperty = Interop.crypt32.CertGetCertificateContextProperty(
                this,
                propertyId,
                null,
                ref cb);

            return hasProperty;
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

                    if (pProvInfo->dwProvType == 0)
                    {
                        // dwProvType being 0 indicates that the key is stored in CNG.
                        // dwProvType being non-zero indicates that the key is stored in CAPI.

                        string providerName = Marshal.PtrToStringUni((IntPtr)(pProvInfo->pwszProvName));
                        string keyContainerName = Marshal.PtrToStringUni((IntPtr)(pProvInfo->pwszContainerName));

                        try
                        {
                            using (CngKey cngKey = CngKey.Open(keyContainerName, new CngProvider(providerName)))
                            {
                                cngKey.Delete();
                            }
                        }
                        catch (CryptographicException)
                        {
                            // While leaving the file on disk is undesirable, an inability to perform this cleanup
                            // should not manifest itself to a user.
                        }
                    }
                    else
                    {
                        CryptAcquireContextFlags flags = (pProvInfo->dwFlags & CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) | CryptAcquireContextFlags.CRYPT_DELETEKEYSET;
                        IntPtr hProv;
                        bool success = Interop.cryptoapi.CryptAcquireContext(out hProv, pProvInfo->pwszContainerName, pProvInfo->pwszProvName, pProvInfo->dwProvType, flags);

                        // Called CryptAcquireContext solely for the side effect of deleting the key containers. When called with these flags, no actual
                        // hProv is returned (so there's nothing to clean up.)
                        Debug.Assert(hProv == IntPtr.Zero);
                    }
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
