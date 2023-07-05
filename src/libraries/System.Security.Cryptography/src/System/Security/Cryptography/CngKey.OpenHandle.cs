// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        /// <summary>
        ///     Wrap an existing key handle with a CngKey object
        /// </summary>
        [SupportedOSPlatform("windows")]
        public static CngKey Open(SafeNCryptKeyHandle keyHandle, CngKeyHandleOpenOptions keyHandleOpenOptions)
        {
            ArgumentNullException.ThrowIfNull(keyHandle);

            if (keyHandle.IsClosed || keyHandle.IsInvalid)
                throw new ArgumentException(SR.Cryptography_OpenInvalidHandle, nameof(keyHandle));

            return OpenNoDuplicate(keyHandle.Duplicate(), keyHandleOpenOptions);
        }

        [SupportedOSPlatform("windows")]
        internal static CngKey OpenNoDuplicate(SafeNCryptKeyHandle keyHandle, CngKeyHandleOpenOptions keyHandleOpenOptions)
        {
            SafeNCryptProviderHandle? providerHandle = null;
            try
            {
                // Get a handle to the key's provider.
                providerHandle = new SafeNCryptProviderHandle();
                IntPtr rawProviderHandle = keyHandle.GetPropertyAsIntPtr(KeyPropertyName.ProviderHandle, CngPropertyOptions.None);
                Marshal.InitHandle(providerHandle, rawProviderHandle);

                // If we're wrapping a handle to an ephemeral key, we need to make sure that IsEphemeral is
                // set up to return true.  In the case that the handle is for an ephemeral key that was created
                // by the CLR, then we don't have anything to do as the IsEphemeral CLR property will already
                // be setup.  However, if the key was created outside of the CLR we will need to setup our
                // ephemeral detection property.
                //
                // This enables consumers of CngKey objects to always be able to rely on the result of
                // calling IsEphemeral, and also allows them to safely access the Name property.
                //
                // Finally, if we detect that this is an ephemeral key that the CLR created but we were not
                // told that it was an ephemeral key we'll throw an exception.  This prevents us from having
                // to decide who to believe -- the key property or the caller of the API.  Since other code
                // relies on the ephemeral flag being set properly to avoid tripping over bugs in CNG, we
                // need to reject the case that we suspect that the flag is incorrect.

                var key = new CngKey(providerHandle, keyHandle);

                bool openingEphemeralKey = (keyHandleOpenOptions & CngKeyHandleOpenOptions.EphemeralKey) == CngKeyHandleOpenOptions.EphemeralKey;
                if (!key.IsEphemeral)
                {
                    if (openingEphemeralKey)
                    {
                        key.IsEphemeral = true;
                    }
                }
                else if (!openingEphemeralKey)
                {
                    throw new ArgumentException(SR.Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag, nameof(keyHandleOpenOptions));
                }

                return key;
            }
            catch
            {
                // Make sure that we don't leak the handles
                providerHandle?.Dispose();
                keyHandle.Dispose();
                throw;
            }
        }
    }
}
