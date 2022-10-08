// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using static Interop.Crypt32;
using CryptProvParam = Interop.Advapi32.CryptProvParam;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Following part of CAPIHelper keeps the wrappers for all the PInvoke calls
    /// </summary>
    internal static partial class CapiHelper
    {
        private static ReadOnlySpan<byte> RgbPubKey => new byte[]
        {
                0x06, 0x02, 0x00, 0x00, 0x00, 0xa4, 0x00, 0x00,
                0x52, 0x53, 0x41, 0x31, 0x00, 0x02, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0xab, 0xef, 0xfa, 0xc6,
                0x7d, 0xe8, 0xde, 0xfb, 0x68, 0x38, 0x09, 0x92,
                0xd9, 0x42, 0x7e, 0x6b, 0x89, 0x9e, 0x21, 0xd7,
                0x52, 0x1c, 0x99, 0x3c, 0x17, 0x48, 0x4e, 0x3a,
                0x44, 0x02, 0xf2, 0xfa, 0x74, 0x57, 0xda, 0xe4,
                0xd3, 0xc0, 0x35, 0x67, 0xfa, 0x6e, 0xdf, 0x78,
                0x4c, 0x75, 0x35, 0x1c, 0xa0, 0x74, 0x49, 0xe3,
                0x20, 0x13, 0x71, 0x35, 0x65, 0xdf, 0x12, 0x20,
                0xf5, 0xf5, 0xf5, 0xc1
        };

        /// <summary>
        /// Check to see if a better CSP than the one requested is available
        /// RSA providers are supersets of each other in the following order:
        ///    1. MS_ENH_RSA_AES_PROV
        ///    2. MS_ENHANCED_PROV
        ///    3. MS_DEF_PROV
        ///
        /// This will return the best provider which is a superset of wszProvider,
        /// or NULL if there is no upgrade available on the machine.
        /// </summary>
        /// <param name="dwProvType">Provider type</param>
        /// <param name="wszProvider">Provider name</param>
        /// <returns>Returns upgrade CSP name</returns>
        public static string? UpgradeRSA(int dwProvType, string wszProvider)
        {
            bool requestedEnhanced = string.Equals(wszProvider, MS_ENHANCED_PROV, StringComparison.Ordinal);
            bool requestedBase = string.Equals(wszProvider, MS_DEF_PROV, StringComparison.Ordinal);
            string? wszUpgrade = null;
            if (requestedBase || requestedEnhanced)
            {
                SafeProvHandle safeProvHandle;

                // attempt to use the AES provider
                if (S_OK == AcquireCryptContext(out safeProvHandle, null, MS_ENH_RSA_AES_PROV,
                                                dwProvType, (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
                {
                    wszUpgrade = MS_ENH_RSA_AES_PROV;
                }

                safeProvHandle.Dispose();
            }

            return wszUpgrade;
        }

        /// <summary>
        /// Find the default provider name to be used in the case that we
        /// were not actually passed in a provider name. The main purpose
        /// of this code is really to deal with the enhanced/default provider
        /// problems given to us by CAPI.
        /// </summary>
        /// <param name="dwType">Type of the provider</param>
        /// <returns>Name of the provider to be used</returns>
        internal static string GetDefaultProvider(int dwType)
        {
            int sizeofProviderName = 0;
            //Get the size of the provider name
            if (!Interop.Advapi32.CryptGetDefaultProvider(dwType, IntPtr.Zero,
                                                Interop.Advapi32.GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT,
                                                null, ref sizeofProviderName))
            {
                throw GetErrorCode().ToCryptographicException();
            }
            //allocate memory for the provider name
            char[] providerName = new char[sizeofProviderName];

            //Now call the function CryptGetDefaultProvider again to get the name of the provider
            if (!Interop.Advapi32.CryptGetDefaultProvider(dwType, IntPtr.Zero,
                                                Interop.Advapi32.GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT,
                                                providerName, ref sizeofProviderName))
            {
                throw GetErrorCode().ToCryptographicException();
            }

            // check to see if there are upgrades available for the requested CSP
            string providerNameString = new string(providerName.AsSpan().Slice(0, providerName.AsSpan().IndexOf('\0')));
            string? wszUpgrade = null;
            if (dwType == (int)ProviderType.PROV_RSA_FULL)
            {
                wszUpgrade = UpgradeRSA(dwType, providerNameString);
            }
            else if (dwType == (int)ProviderType.PROV_DSS_DH)
            {
                wszUpgrade = UpgradeDSS(dwType, providerNameString);
            }

            return wszUpgrade ?? providerNameString;
        }

        /// <summary>
        /// Creates a new key container
        /// </summary>
        private static void CreateCSP(CspParameters parameters, bool randomKeyContainer, out SafeProvHandle safeProvHandle)
        {
            uint dwFlags = (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_NEWKEYSET;
            if (randomKeyContainer)
            {
                dwFlags |= (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
            }

            SafeProvHandle hProv;
            int ret = OpenCSP(parameters, dwFlags, out hProv);
            if (S_OK != ret)
            {
                hProv.Dispose();
                throw ret.ToCryptographicException();
            }
            safeProvHandle = hProv;
        }

        /// <summary>
        /// Acquire a handle to a crypto service provider and optionally a key container
        /// This function implements the WszCryptAcquireContext_SO_TOLERANT
        /// </summary>
        private static int AcquireCryptContext(out SafeProvHandle safeProvHandle, string? keyContainer,
                                                string providerName, int providerType, uint flags)
        {
            const uint VerifyContextFlag = (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
            const uint MachineContextFlag = (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET;

            int ret = S_OK;
            // Specifying both verify context (for an ephemeral key) and machine keyset (for a persisted machine key)
            // does not make sense.  Additionally, Windows is beginning to lock down against uses of MACHINE_KEYSET
            // (for instance in the app container), even if verify context is present.   Therefore, if we're using
            // an ephemeral key, strip out MACHINE_KEYSET from the flags.
            if (((flags & VerifyContextFlag) == VerifyContextFlag) &&
                ((flags & MachineContextFlag) == MachineContextFlag))
            {
                flags &= ~MachineContextFlag;
            }
            //Do not throw in this function. Just return the error code
            if (!Interop.Advapi32.CryptAcquireContext(out safeProvHandle, keyContainer, providerName, providerType, flags))
            {
                ret = GetErrorCode();
            }

            return ret;
        }

        /// <summary>
        /// This method opens the CSP using CRYPT_VERIFYCONTEXT
        /// KeyContainer must be null for the flag CRYPT_VERIFYCONTEXT
        /// This method asserts if keyContainer is not null
        /// </summary>
        /// <param name="cspParameters">CSPParameter to use</param>
        /// <param name="safeProvHandle">Safe provider handle</param>
        internal static void AcquireCsp(CspParameters cspParameters, out SafeProvHandle safeProvHandle)
        {
            Debug.Assert(cspParameters != null);
            Debug.Assert(cspParameters.KeyContainerName == null);

            SafeProvHandle hProv;
            //
            // We want to just open this CSP.  Passing in verify context will
            // open it and, if a container is given, map to open the container.
            //
            int ret = OpenCSP(cspParameters, (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT, out hProv);
            if (S_OK != ret)
            {
                hProv.Dispose();
                throw ret.ToCryptographicException();
            }

            safeProvHandle = hProv;
        }

        /// <summary>
        /// OpenCSP performs the core work of opening and creating CSPs and containers in CSPs
        /// </summary>
        public static int OpenCSP(CspParameters cspParameters, uint flags, out SafeProvHandle safeProvHandle)
        {
            string providerName;
            string? containerName = null;
            if (null == cspParameters)
            {
                throw new ArgumentException(SR.Format(SR.CspParameter_invalid, nameof(cspParameters)));
            }

            //look for provider type in the cspParameters
            int providerType = cspParameters.ProviderType;

            //look for provider name in the cspParameters
            //if CSP provider is not null then use the provider name from cspParameters
            if (null != cspParameters.ProviderName)
            {
                providerName = cspParameters.ProviderName;
            }
            else //Get the default provider name
            {
                providerName = GetDefaultProvider(providerType);
                cspParameters.ProviderName = providerName;
            }
            // look to see if the user specified that we should pass
            // CRYPT_MACHINE_KEYSET to CAPI to use machine key storage instead
            // of user key storage
            int cspProviderFlags = (int)cspParameters.Flags;

            // If the user specified CSP_PROVIDER_FLAGS_USE_DEFAULT_KEY_CONTAINER,
            // then ignore the key container name and hand back the default container
            if (!IsFlagBitSet((uint)cspProviderFlags, (uint)CspProviderFlags.UseDefaultKeyContainer))
            {
                //look for key container name in the cspParameters
                if (null != cspParameters.KeyContainerName)
                {
                    containerName = cspParameters.KeyContainerName;
                }
            }

            SafeProvHandle hProv;

            // Go ahead and try to open the CSP.  If we fail, make sure the CSP
            // returned is 0 as that is going to be the error check in the caller.
            flags |= MapCspProviderFlags((int)cspParameters.Flags);
            int hr = AcquireCryptContext(out hProv, containerName, providerName, providerType, flags);
            if (hr != S_OK)
            {
                hProv.Dispose();
                safeProvHandle = SafeProvHandle.InvalidHandle;
                return hr;
            }

            hProv.ContainerName = containerName;
            hProv.ProviderName = providerName;
            hProv.Types = providerType;
            hProv.Flags = flags;

            // We never want to delete a key container if it's already there.
            if (IsFlagBitSet(flags, (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
            {
                hProv.PersistKeyInCsp = false;
            }

            safeProvHandle = hProv;
            return S_OK;
        }

        /// <summary>
        /// This method acquires CSP and returns the handle of CSP
        /// </summary>
        /// <param name="parameters">Accepts the CSP Parameters</param>
        /// <param name="randomKeyContainer">Bool to indicate if key needs to be persisted</param>
        /// <returns>Returns the safehandle of CSP </returns>
        internal static SafeProvHandle CreateProvHandle(CspParameters parameters, bool randomKeyContainer)
        {
            SafeProvHandle safeProvHandle;
            uint flag = 0;
            uint hr = unchecked((uint)OpenCSP(parameters, flag, out safeProvHandle));
            //Open container failed
            if (hr != S_OK)
            {
                safeProvHandle.Dispose();

                // If UseExistingKey flag is used and the key container does not exist
                // throw an exception without attempting to create the container.
                if (IsFlagBitSet((uint)parameters.Flags, (uint)CspProviderFlags.UseExistingKey) ||
                                                        ((hr != (uint)CryptKeyError.NTE_KEYSET_NOT_DEF && hr !=
                                                        (uint)CryptKeyError.NTE_BAD_KEYSET && hr !=
                                                        (uint)CryptKeyError.NTE_FILENOTFOUND)))
                {
                    throw ((int)hr).ToCryptographicException();
                }

                //Create a new CSP. This method throws exception on failure
                CreateCSP(parameters, randomKeyContainer, out safeProvHandle);
            }

            if (parameters.ParentWindowHandle != IntPtr.Zero)
            {
                IntPtr parentWindowHandle = parameters.ParentWindowHandle;

                if (!Interop.Advapi32.CryptSetProvParam(safeProvHandle, CryptProvParam.PP_CLIENT_HWND, ref parentWindowHandle, 0))
                {
                    safeProvHandle.Dispose();
                    throw GetErrorCode().ToCryptographicException();
                }
            }

            if (parameters.KeyPassword != null)
            {
                IntPtr password = Marshal.SecureStringToCoTaskMemAnsi(parameters.KeyPassword);
                try
                {
                    CryptProvParam param =
                        (parameters.KeyNumber == (int)Interop.Advapi32.KeySpec.AT_SIGNATURE) ?
                            CryptProvParam.PP_SIGNATURE_PIN :
                            CryptProvParam.PP_KEYEXCHANGE_PIN;
                    if (!Interop.Advapi32.CryptSetProvParam(safeProvHandle, param, password, 0))
                    {
                        safeProvHandle.Dispose();
                        throw GetErrorCode().ToCryptographicException();
                    }
                }
                finally
                {
                    if (password != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeCoTaskMemAnsi(password);
                    }
                }
            }

            return safeProvHandle;
        }

        /// <summary>
        /// This method validates the flag bits set or not. Only works for flags with just one bit set
        /// </summary>
        /// <param name="dwImp">int where you want to check the flag bits</param>
        /// <param name="flag">Actual flag</param>
        /// <returns>true if bits are set or false</returns>
        internal static bool IsFlagBitSet(uint dwImp, uint flag)
        {
            return (dwImp & flag) == flag;
        }

        /// <summary>
        /// This method helps reduce the duplicate code in the GetProviderParameter method
        /// </summary>
        internal static int GetProviderParameterWorker(SafeProvHandle safeProvHandle, byte[]? impType, ref int cb, CryptProvParam flags)
        {
            int impTypeReturn = 0;
            if (!Interop.Advapi32.CryptGetProvParam(safeProvHandle, flags, impType, ref cb))
            {
                throw GetErrorCode().ToCryptographicException();
            }
            if (null != impType && cb == sizeof(uint))
            {
                impTypeReturn = BitConverter.ToInt32(impType, 0);
            }
            return impTypeReturn;
        }

        /// <summary>
        /// This method queries the key container and get some of it's properties.
        /// Those properties should never cause UI to display.
        /// </summary>
        public static object GetProviderParameter(SafeProvHandle safeProvHandle, int keyNumber, ClrPropertyId keyParam)
        {
            VerifyValidHandle(safeProvHandle);
            byte[] impType = new byte[sizeof(uint)];
            int cb = sizeof(byte) * sizeof(uint);
            SafeCapiKeyHandle safeKeyHandle = SafeCapiKeyHandle.InvalidHandle;
            int impTypeReturn;
            int returnType = 0; //using 0 for bool and 1 for string return types
            bool retVal = false;
            string? retStr = null;

            try
            {
                switch (keyParam)
                {
                    case ClrPropertyId.CLR_EXPORTABLE:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptProvParam.PP_IMPTYPE);
                        //If implementation type is not HW
                        if (!IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_HARDWARE))
                        {
                            if (!CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle))
                            {
                                throw GetErrorCode().ToCryptographicException();
                            }
                            byte[]? permissions = null;
                            int permissionsReturn = 0;
                            permissions = new byte[sizeof(uint)];
                            cb = sizeof(byte) * sizeof(uint);
                            if (!Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, Interop.Advapi32.CryptGetKeyParamFlags.KP_PERMISSIONS, permissions, ref cb, 0))
                            {
                                throw GetErrorCode().ToCryptographicException();
                            }
                            permissionsReturn = BitConverter.ToInt32(permissions, 0);
                            retVal = IsFlagBitSet((uint)permissionsReturn, (uint)Interop.Advapi32.CryptGetKeyParamFlags.CRYPT_EXPORT);
                        }
                        else
                        {
                            //Assumption HW keys are not exportable.
                            retVal = false;
                        }

                        break;
                    }
                    case ClrPropertyId.CLR_REMOVABLE:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptProvParam.PP_IMPTYPE);
                        retVal = IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_REMOVABLE);
                        break;
                    }
                    case ClrPropertyId.CLR_HARDWARE:
                    case ClrPropertyId.CLR_PROTECTED:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptProvParam.PP_IMPTYPE);
                        retVal = IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_HARDWARE);
                        break;
                    }
                    case ClrPropertyId.CLR_ACCESSIBLE:
                    {
                        retVal = CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle) ? true : false;
                        break;
                    }
                    case ClrPropertyId.CLR_UNIQUE_CONTAINER:
                    {
                        returnType = 1;
                        byte[]? pb = null;
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, pb, ref cb, CryptProvParam.PP_UNIQUE_CONTAINER);
                        pb = new byte[cb];
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, pb, ref cb, CryptProvParam.PP_UNIQUE_CONTAINER);
                        // GetProviderParameterWorker allocated the null character, we want to not interpret that.
                        Debug.Assert(cb > 0);
                        Debug.Assert(pb[cb - 1] == 0);
                        retStr = Encoding.ASCII.GetString(pb, 0, cb - 1);
                        break;
                    }
                    default:
                    {
                        Debug.Fail($"Unexpected key param {keyParam}");
                        break;
                    }
                }
            }
            finally
            {
                safeKeyHandle.Dispose();
            }

            Debug.Assert(returnType == 0 || returnType == 1);
            return returnType == 0 ? (object)retVal : retStr!;
        }

        /// <summary>
        /// Retrieves the handle for user public / private key pair.
        /// </summary>
        internal static int GetUserKey(SafeProvHandle safeProvHandle, int keySpec, out SafeCapiKeyHandle safeKeyHandle)
        {
            int hr = S_OK;
            VerifyValidHandle(safeProvHandle);
            if (!CryptGetUserKey(safeProvHandle, keySpec, out safeKeyHandle))
            {
                hr = GetErrorCode();
            }
            if (hr == S_OK)
            {
                safeKeyHandle.KeySpec = keySpec;
            }
            return hr;
        }

        /// <summary>
        /// Generates the key if provided CSP handle is valid
        /// </summary>
        internal static int GenerateKey(SafeProvHandle safeProvHandle, int algID, int flags, uint keySize, out SafeCapiKeyHandle safeKeyHandle)
        {
            int hr = S_OK;
            VerifyValidHandle(safeProvHandle);
            int capiFlags = (int)((uint)MapCspKeyFlags(flags) | ((uint)keySize << 16));
            if (!CryptGenKey(safeProvHandle, algID, capiFlags, out safeKeyHandle))
            {
                hr = GetErrorCode();
            }
            if (hr != S_OK)
            {
                throw GetErrorCode().ToCryptographicException();
            }

            safeKeyHandle.KeySpec = algID;
            return hr;
        }

        /// <summary>
        /// Maps CspProviderFlags enumeration into CAPI flags.
        /// </summary>
        internal static int MapCspKeyFlags(int flags)
        {
            int capiFlags = 0;
            if (!IsFlagBitSet((uint)flags, (uint)CspProviderFlags.UseNonExportableKey))
            {
                capiFlags |= (int)CryptGenKeyFlags.CRYPT_EXPORTABLE;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.UseArchivableKey))
            {
                capiFlags |= (int)CryptGenKeyFlags.CRYPT_ARCHIVABLE;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.UseUserProtectedKey))
            {
                capiFlags |= (int)CryptGenKeyFlags.CRYPT_USER_PROTECTED;
            }
            return capiFlags;
        }

        /// <summary>
        ///Maps CspProviderFlags enumeration into CAPI flags
        /// </summary>
        internal static uint MapCspProviderFlags(int flags)
        {
            uint cspFlags = 0;

            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.UseMachineKeyStore))
            {
                cspFlags |= (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.NoPrompt))
            {
                cspFlags |= (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_SILENT;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.CreateEphemeralKey))
            {
                cspFlags |= (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
            }
            return cspFlags;
        }

        /// <summary>
        /// This method checks if the handle is invalid then it throws error
        /// </summary>
        /// <param name="handle">Accepts handle</param>
        internal static void VerifyValidHandle(SafeHandleZeroOrMinusOneIsInvalid handle)
        {
            if (handle.IsInvalid)
            {
                throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
            }
        }

        /// <summary>
        ///Method helps get the different key properties
        /// </summary>
        /// <param name="safeKeyHandle">Key handle</param>
        /// <param name="keyParam"> Key property you want to get</param>
        /// <returns>Returns the key property</returns>
        internal static byte[] GetKeyParameter(SafeCapiKeyHandle safeKeyHandle, ClrPropertyId keyParam)
        {
            byte[]? pb = null;
            int cb = 0;
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case ClrPropertyId.CLR_KEYLEN:
                    {
                        if (!Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, Interop.Advapi32.CryptGetKeyParamFlags.KP_KEYLEN, null, ref cb, 0))
                        {
                            throw GetErrorCode().ToCryptographicException();
                        }
                        pb = new byte[cb];
                        if (!Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, Interop.Advapi32.CryptGetKeyParamFlags.KP_KEYLEN, pb, ref cb, 0))
                        {
                            throw GetErrorCode().ToCryptographicException();
                        }
                        break;
                    }
                case ClrPropertyId.CLR_PUBLICKEYONLY:
                    {
                        pb = new byte[1];
                        pb[0] = safeKeyHandle.PublicOnly ? (byte)1 : (byte)0;
                        break;
                    }
                case ClrPropertyId.CLR_ALGID:
                    {
                        // returns the algorithm ID for the key
                        if (!Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, Interop.Advapi32.CryptGetKeyParamFlags.KP_ALGID, null, ref cb, 0))
                        {
                            throw GetErrorCode().ToCryptographicException();
                        }
                        pb = new byte[cb];
                        if (!Interop.Advapi32.CryptGetKeyParam(safeKeyHandle, Interop.Advapi32.CryptGetKeyParamFlags.KP_ALGID, pb, ref cb, 0))
                        {
                            throw GetErrorCode().ToCryptographicException();
                        }
                        break;
                    }
                default:
                    {
                        Debug.Fail($"Unexpected key param {keyParam}");
                        break;
                    }
            }
            return pb;
        }

        /// <summary>
        /// Set a key property which is based on byte[]
        /// </summary>
        /// <param name="safeKeyHandle">Key handle</param>
        /// <param name="keyParam"> Key property you want to set</param>
        /// <param name="value"> Key property value you want to set</param>
        internal static void SetKeyParameter(SafeCapiKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, byte[] value)
        {
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case CryptGetKeyParamQueryType.KP_IV:
                    if (!Interop.Advapi32.CryptSetKeyParam(safeKeyHandle, (int)keyParam, value, 0))
                        throw new CryptographicException(SR.CryptSetKeyParam_Failed, GetErrorCode().ToString());

                    break;
                default:
                    Debug.Fail("Unknown param in SetKeyParameter");
                    break;
            }
        }

        /// <summary>
        /// Set a key property which is based on int
        /// </summary>
        /// <param name="safeKeyHandle">Key handle</param>
        /// <param name="keyParam"> Key property you want to set</param>
        /// <param name="value"> Key property value you want to set</param>
        internal static void SetKeyParameter(SafeCapiKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, int value)
        {
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case CryptGetKeyParamQueryType.KP_MODE:
                case CryptGetKeyParamQueryType.KP_MODE_BITS:
                case CryptGetKeyParamQueryType.KP_EFFECTIVE_KEYLEN:
                    if (!Interop.Advapi32.CryptSetKeyParam(safeKeyHandle, (int)keyParam, ref value, 0))
                        throw new CryptographicException(SR.CryptSetKeyParam_Failed, GetErrorCode().ToString());

                    break;
                default:
                    Debug.Fail("Unknown param in SetKeyParameter");
                    break;
            }
        }

        /// <summary>
        /// Helper method to save the CSP parameters.
        /// </summary>
        /// <param name="keyType">CSP algorithm type</param>
        /// <param name="userParameters">CSP Parameters passed by user</param>
        /// <param name="defaultFlags">flags </param>
        /// <param name="randomKeyContainer">identifies if it is random key container</param>
        /// <returns></returns>
        internal static CspParameters SaveCspParameters(
            CspAlgorithmType keyType,
            CspParameters? userParameters,
            CspProviderFlags defaultFlags,
            out bool randomKeyContainer)
        {
            CspParameters parameters;
            if (userParameters == null)
            {
                parameters = new CspParameters(keyType == CspAlgorithmType.Dss ?
                                                DefaultDssProviderType : DefaultRsaProviderType,
                                                null, null, defaultFlags);
            }
            else
            {
                ValidateCspFlags(userParameters.Flags);
                parameters = new CspParameters(userParameters);
            }

            if (parameters.KeyNumber == -1)
            {
                parameters.KeyNumber = keyType == CapiHelper.CspAlgorithmType.Dss ? (int)KeyNumber.Signature : (int)KeyNumber.Exchange;
            }
            else if (parameters.KeyNumber == CALG_DSS_SIGN || parameters.KeyNumber == CALG_RSA_SIGN)
            {
                parameters.KeyNumber = (int)KeyNumber.Signature;
            }
            else if (parameters.KeyNumber == CALG_RSA_KEYX)
            {
                parameters.KeyNumber = (int)KeyNumber.Exchange;
            }
            // If no key container was specified and UseDefaultKeyContainer is not used, then use CRYPT_VERIFYCONTEXT
            // to generate an ephemeral key
            randomKeyContainer = IsFlagBitSet((uint)parameters.Flags, (uint)CspProviderFlags.CreateEphemeralKey);

            if (parameters.KeyContainerName == null && !IsFlagBitSet((uint)parameters.Flags,
                (uint)CspProviderFlags.UseDefaultKeyContainer))
            {
                parameters.Flags |= CspProviderFlags.CreateEphemeralKey;
                randomKeyContainer = true;
            }

            return parameters;
        }

        /// <summary>
        /// Validates the CSP flags are expected
        /// </summary>
        /// <param name="flags">CSP provider flags</param>
        private static void ValidateCspFlags(CspProviderFlags flags)
        {
            // check that the flags are consistent.
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.UseExistingKey))
            {
                CspProviderFlags keyFlags = (CspProviderFlags.UseNonExportableKey |
                                            CspProviderFlags.UseArchivableKey |
                                            CspProviderFlags.UseUserProtectedKey);
                if ((flags & keyFlags) != CspProviderFlags.NoFlags)
                {
                    throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, flags), nameof(flags));
                }
            }
        }

        /// <summary>
        /// Helper function to get the key pair
        /// </summary>
        internal static SafeCapiKeyHandle GetKeyPairHelper(
            CspAlgorithmType keyType,
            CspParameters parameters,
            int keySize,
            SafeProvHandle safeProvHandle)
        {
            // If the key already exists, use it, else generate a new one
            SafeCapiKeyHandle hKey;
            int hr = CapiHelper.GetUserKey(safeProvHandle, parameters.KeyNumber, out hKey);
            if (hr != S_OK)
            {
                hKey.Dispose();
                if (unchecked(IsFlagBitSet((uint)parameters.Flags, (uint)CspProviderFlags.UseExistingKey) ||
                                                                   (uint)hr != (uint)CryptKeyError.NTE_NO_KEY))
                {
                    throw hr.ToCryptographicException();
                }

                // GenerateKey will check for failures and throw an exception
                CapiHelper.GenerateKey(safeProvHandle, parameters.KeyNumber, (int)parameters.Flags,
                                        (uint)keySize, out hKey);
            }

            // check that this is indeed an RSA/DSS key.
            byte[] algid = CapiHelper.GetKeyParameter(hKey, ClrPropertyId.CLR_ALGID);

            int dwAlgId = BinaryPrimitives.ReadInt32LittleEndian(algid);

            if ((keyType == CspAlgorithmType.Rsa && dwAlgId != CALG_RSA_KEYX && dwAlgId != CALG_RSA_SIGN) ||
                (keyType == CspAlgorithmType.Dss && dwAlgId != CALG_DSS_SIGN))
            {
                hKey.Dispose();
                throw new CryptographicException(SR.Format(SR.Cryptography_CSP_WrongKeySpec, keyType));
            }

            return hKey;
        }

        /// <summary>
        /// Wrapper for get last error function
        /// </summary>
        /// <returns>returns the error code</returns>
        internal static int GetErrorCode()
        {
            return Marshal.GetLastPInvokeError();
        }

        /// <summary>
        /// Returns PersistKeyInCsp value
        /// </summary>
        /// <param name="safeProvHandle">Safe Prov Handle. Expects a valid handle</param>
        /// <returns>true if key is persisted otherwise false</returns>
        internal static bool GetPersistKeyInCsp(SafeProvHandle safeProvHandle)
        {
            VerifyValidHandle(safeProvHandle);
            return safeProvHandle.PersistKeyInCsp;
        }

        /// <summary>
        /// Sets the PersistKeyInCsp
        /// </summary>
        /// <param name="safeProvHandle">Safe Prov Handle. Expects a valid handle</param>
        /// <param name="fPersistKeyInCsp">Sets the PersistKeyInCsp value</param>
        internal static void SetPersistKeyInCsp(SafeProvHandle safeProvHandle, bool fPersistKeyInCsp)
        {
            VerifyValidHandle(safeProvHandle);
            safeProvHandle.PersistKeyInCsp = fPersistKeyInCsp;
        }

        //---------------------------------------------------------------------------------------
        //
        // Decrypt a symmetric key using the private key in pKeyContext
        //
        // Arguments:
        //    pKeyContext       - private key used for decrypting pbEncryptedKey
        //    pbEncryptedKey    - [in] encrypted symmetric key
        //    cbEncryptedKey    - size, in bytes, of pbEncryptedKey
        //    fOAEP             - TRUE to use OAEP padding, FALSE to use PKCS #1 type 2 padding
        //    ohRetDecryptedKey - [out] decrypted key
        //
        // Notes:
        //    pbEncryptedKey is byte-reversed from the format that CAPI expects. This is for compatibility with
        //    previous CLR versions and other RSA implementations.
        //
        //    This method is the target of the System.Security.Cryptography.RSACryptoServiceProvider.DecryptKey QCall
        //

        // static
        internal static void DecryptKey(SafeCapiKeyHandle safeKeyHandle, byte[] encryptedData, int encryptedDataLength, bool fOAEP, out byte[] decryptedData)
        {
            VerifyValidHandle(safeKeyHandle);
            Debug.Assert(encryptedData != null, "Encrypted Data is null");
            Debug.Assert(encryptedDataLength >= 0, "Encrypted data length is less than 0");

            byte[] dataToBeDecrypted = new byte[encryptedDataLength];
            Buffer.BlockCopy(encryptedData, 0, dataToBeDecrypted, 0, encryptedDataLength);
            Array.Reverse(dataToBeDecrypted);

            int dwFlags = fOAEP ? (int)Interop.Advapi32.CryptDecryptFlags.CRYPT_OAEP : 0;
            int decryptedDataLength = encryptedDataLength;
            if (!Interop.Advapi32.CryptDecrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwFlags, dataToBeDecrypted, ref decryptedDataLength))
            {
                int ErrCode = GetErrorCode();
                // If we're using OAEP mode and we received an NTE_BAD_FLAGS error, then OAEP is not supported on
                // this platform (XP+ only).  Throw a generic cryptographic exception if we failed to decrypt OAEP
                // padded data in order to prevent a chosen ciphertext attack.  We will allow NTE_BAD_KEY out, since
                // that error does not relate to the padding.  Otherwise just throw a cryptographic exception based on
                // the error code.
                if ((uint)((uint)dwFlags & (uint)Interop.Advapi32.CryptDecryptFlags.CRYPT_OAEP) == (uint)Interop.Advapi32.CryptDecryptFlags.CRYPT_OAEP &&
                                                      unchecked((uint)ErrCode) != (uint)CryptKeyError.NTE_BAD_KEY)
                {
                    if (unchecked((uint)ErrCode) == (uint)CryptKeyError.NTE_BAD_FLAGS)
                    {
                        throw new CryptographicException("Cryptography_OAEP_XPPlus_Only");
                    }
                    else
                    {
                        throw new CryptographicException("Cryptography_OAEPDecoding");
                    }
                }
                else
                {
                    throw ErrCode.ToCryptographicException();
                }
            }


            decryptedData = new byte[decryptedDataLength];
            Buffer.BlockCopy(dataToBeDecrypted, 0, decryptedData, 0, decryptedDataLength);
            return;
        }


        //---------------------------------------------------------------------------------------
        //
        // Encrypt a symmetric key using the public key in pKeyContext
        //
        // Arguments:
        //    safeKeyHandle       [in] Key handle
        //    pbKey             - [in] symmetric key to encrypt
        //    cbKey             - size, in bytes, of pbKey
        //    fOAEP             - TRUE to use OAEP padding, FALSE to use PKCS #1 type 2 padding
        //    ohRetEncryptedKey - [out] byte array holding the encrypted key
        //
        // Notes:
        //    The returned value in ohRetEncryptedKey is byte-reversed from the version CAPI gives us.  This is for
        //    compatibility with previous releases of the CLR and other RSA implementations.
        //
        internal static void EncryptKey(SafeCapiKeyHandle safeKeyHandle, byte[] pbKey, int cbKey, bool foep, [NotNull] ref byte[]? pbEncryptedKey)
        {
            VerifyValidHandle(safeKeyHandle);
            Debug.Assert(pbKey != null, "pbKey is null");
            Debug.Assert(cbKey >= 0, $"cbKey is less than 0 ({cbKey})");

            int dwEncryptFlags = foep ? (int)Interop.Advapi32.CryptDecryptFlags.CRYPT_OAEP : 0;
            // Figure out how big the encrypted key will be
            int cbEncryptedKey = cbKey;
            if (!Interop.Advapi32.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwEncryptFlags, null, ref cbEncryptedKey, cbEncryptedKey))
            {
                throw GetErrorCode().ToCryptographicException();
            }
            // pbData is an in/out buffer for CryptEncrypt. allocate space for the encrypted key, and copy the
            // plaintext key into that space.  Since encrypted keys will have padding applied, the size of the encrypted
            // key should always be larger than the plaintext key, so use that to determine the buffer size.
            Debug.Assert(cbEncryptedKey >= cbKey);
            pbEncryptedKey = new byte[cbEncryptedKey];
            Buffer.BlockCopy(pbKey, 0, pbEncryptedKey, 0, cbKey);

            // Encrypt for real - the last parameter is the total size of the in/out buffer, while the second to last
            // parameter specifies the size of the plaintext to encrypt.
            if (!Interop.Advapi32.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwEncryptFlags, pbEncryptedKey, ref cbKey, cbEncryptedKey))
            {
                throw GetErrorCode().ToCryptographicException();
            }

            Debug.Assert(cbKey == cbEncryptedKey);
            Array.Reverse(pbEncryptedKey);
        }

        internal static int EncryptData(
            SafeCapiKeyHandle hKey,
            ReadOnlySpan<byte> input,
            Span<byte> output,
            bool isFinal)
        {
            VerifyValidHandle(hKey);

            // Figure out how big the encrypted data will be
            int cbEncryptedData = input.Length;
            if (!Interop.Advapi32.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, null, ref cbEncryptedData, cbEncryptedData))
            {
                throw GetErrorCode().ToCryptographicException();
            }

            // encryptedData is an in/out buffer for CryptEncrypt. Allocate space for the encrypted data, and copy the
            // plaintext data into that space.  Since encrypted data will have padding applied, the size of the encrypted
            // data should always be larger than the plaintext key, so use that to determine the buffer size.
            Debug.Assert(cbEncryptedData >= input.Length);
            var encryptedData = new byte[cbEncryptedData];
            input.CopyTo(encryptedData);

            // Encrypt for real - the last parameter is the total size of the in/out buffer, while the second to last
            // parameter specifies the size of the plaintext to encrypt.
            int encryptedDataLength = input.Length;
            if (!Interop.Advapi32.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, encryptedData, ref encryptedDataLength, cbEncryptedData))
            {
                throw GetErrorCode().ToCryptographicException();
            }

            // If isFinal, padding was added so ignore it by using original input length as size
            int outputCount = isFinal ? input.Length : encryptedDataLength;
            Debug.Assert(encryptedDataLength == cbEncryptedData);
            Debug.Assert(outputCount <= output.Length);

            encryptedData.AsSpan(0, outputCount).CopyTo(output);
            return outputCount;
        }

        internal static int DecryptData(
            SafeCapiKeyHandle hKey,
            ReadOnlySpan<byte> input,
            Span<byte> output)
        {
            VerifyValidHandle(hKey);

            byte[] dataToBeDecrypted = new byte[input.Length];
            input.CopyTo(dataToBeDecrypted);

            int decryptedDataLength = input.Length;

            // Always call decryption with false (not final); deal with padding manually
            if (!Interop.Advapi32.CryptDecrypt(hKey, SafeHashHandle.InvalidHandle, false, 0, dataToBeDecrypted, ref decryptedDataLength))
            {
                throw GetErrorCode().ToCryptographicException();
            }

            dataToBeDecrypted.AsSpan(0, decryptedDataLength).CopyTo(output);
            return decryptedDataLength;
        }

        /// <summary>
        /// Helper for Import CSP
        /// </summary>
        internal static void ImportKeyBlob(SafeProvHandle saveProvHandle, CspProviderFlags flags, bool addNoSaltFlag, byte[] keyBlob, out SafeCapiKeyHandle safeKeyHandle)
        {
            // Compat note: This isn't the same check as the one done by the CLR _ImportCspBlob QCall,
            // but this does match the .NET Framework CLR behavior and the only scenarios it
            // affects are cases where a corrupt blob is passed in.
            bool isPublic = keyBlob.Length > 0 && keyBlob[0] == CapiHelper.PUBLICKEYBLOB;

            int dwCapiFlags = MapCspKeyFlags((int)flags);
            if (isPublic)
            {
                dwCapiFlags &= ~(int)(CryptGenKeyFlags.CRYPT_EXPORTABLE);
            }

            if (addNoSaltFlag)
            {
                // For RC2 running in rsabase.dll compatibility mode, make sure 11 bytes of
                // zero salt are generated when using a 40 bit RC2 key.
                dwCapiFlags |= (int)CryptGenKeyFlags.CRYPT_NO_SALT;
            }

            SafeCapiKeyHandle hKey;
            if (!CryptImportKey(saveProvHandle, keyBlob, SafeCapiKeyHandle.InvalidHandle, dwCapiFlags, out hKey))
            {
                int hr = Marshal.GetHRForLastWin32Error();

                hKey.Dispose();

                throw hr.ToCryptographicException();
            }

            hKey.PublicOnly = isPublic;
            safeKeyHandle = hKey;

            return;
        }

        /// <summary>
        /// Helper for Export CSP
        /// </summary>
        internal static byte[] ExportKeyBlob(bool includePrivateParameters, SafeCapiKeyHandle safeKeyHandle)
        {
            VerifyValidHandle(safeKeyHandle);

            byte[] pbRawData;
            int cbRawData = 0;
            int dwBlobType = includePrivateParameters ? PRIVATEKEYBLOB : PUBLICKEYBLOB;

            if (!Interop.Advapi32.CryptExportKey(safeKeyHandle, SafeCapiKeyHandle.InvalidHandle, dwBlobType, 0, null, ref cbRawData))
            {
                throw GetErrorCode().ToCryptographicException();
            }
            pbRawData = new byte[cbRawData];

            if (!Interop.Advapi32.CryptExportKey(safeKeyHandle, SafeCapiKeyHandle.InvalidHandle, dwBlobType, 0, pbRawData, ref cbRawData))
            {
                throw GetErrorCode().ToCryptographicException();
            }
            return pbRawData;
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string to specify a hashing algorithm.
        /// </summary>
        public static int NameOrOidToHashAlgId(string? nameOrOid, OidGroup oidGroup)
        {
            // Default Algorithm Id is CALG_SHA1
            if (nameOrOid == null)
                return CapiHelper.CALG_SHA1;

            string oidValue = CryptoConfig.MapNameToOID(nameOrOid) ?? nameOrOid; // we were probably passed an OID value directly

            int algId = GetAlgIdFromOid(oidValue, oidGroup);
            if (algId == 0 || algId == -1)
                throw new CryptographicException(SR.Cryptography_InvalidOID);

            return algId;
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string/Type/HashAlgorithm to specify a hashing algorithm.
        /// </summary>
        public static int ObjToHashAlgId(object hashAlg)
        {
            ArgumentNullException.ThrowIfNull(hashAlg);

            string? hashAlgString = hashAlg as string;
            if (hashAlgString != null)
            {
                int algId = NameOrOidToHashAlgId(hashAlgString, OidGroup.HashAlgorithm);
                return algId;
            }
            else if (hashAlg is HashAlgorithm)
            {
                if (hashAlg is MD5)
                    return CapiHelper.CALG_MD5;

                if (hashAlg is SHA1)
                    return CapiHelper.CALG_SHA1;

                if (hashAlg is SHA256)
                    return CapiHelper.CALG_SHA_256;

                if (hashAlg is SHA384)
                    return CapiHelper.CALG_SHA_384;

                if (hashAlg is SHA512)
                    return CapiHelper.CALG_SHA_512;
            }
            else
            {
                Type? hashAlgType = hashAlg as Type;
                if (hashAlgType is not null)
                {
                    if (typeof(MD5).IsAssignableFrom(hashAlgType))
                        return CapiHelper.CALG_MD5;

                    if (typeof(SHA1).IsAssignableFrom(hashAlgType))
                        return CapiHelper.CALG_SHA1;

                    if (typeof(SHA256).IsAssignableFrom(hashAlgType))
                        return CapiHelper.CALG_SHA_256;

                    if (typeof(SHA384).IsAssignableFrom(hashAlgType))
                        return CapiHelper.CALG_SHA_384;

                    if (typeof(SHA512).IsAssignableFrom(hashAlgType))
                        return CapiHelper.CALG_SHA_512;
                }
            }

            throw new ArgumentException(SR.Argument_InvalidValue, nameof(hashAlg));
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string/Type/HashAlgorithm to specify a hashing algorithm.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        internal static HashAlgorithm ObjToHashAlgorithm(object hashAlg) =>
            ObjToHashAlgId(hashAlg) switch
            {
                CapiHelper.CALG_MD5 => MD5.Create(),
                CapiHelper.CALG_SHA1 => SHA1.Create(),
                CapiHelper.CALG_SHA_256 => SHA256.Create(),
                CapiHelper.CALG_SHA_384 => SHA384.Create(),
                CapiHelper.CALG_SHA_512 => SHA512.Create(),
                _ => throw new ArgumentException(SR.Argument_InvalidValue, nameof(hashAlg)),
            };

        internal static HashAlgorithmName AlgIdToHashAlgorithmName(int hashAlg) =>
            hashAlg switch
            {
                CapiHelper.CALG_MD5 => HashAlgorithmName.MD5,
                CapiHelper.CALG_SHA1 => HashAlgorithmName.SHA1,
                CapiHelper.CALG_SHA_256 => HashAlgorithmName.SHA256,
                CapiHelper.CALG_SHA_384 => HashAlgorithmName.SHA384,
                CapiHelper.CALG_SHA_512 => HashAlgorithmName.SHA512,
                _ => throw new ArgumentException(SR.Argument_InvalidValue, nameof(hashAlg)),
            };

        /// <summary>
        /// Convert an OID into a CAPI-1 CALG ID.
        /// </summary>
        private static int GetAlgIdFromOid(string oid, OidGroup oidGroup)
        {
            Debug.Assert(oid != null);

            // CAPI does not have ALGID mappings for all of the hash algorithms - see if we know the mapping
            // first to avoid doing an AD lookup on these values
            if (string.Equals(oid, CapiHelper.OID_OIWSEC_SHA256, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_256;
            }
            else if (string.Equals(oid, CapiHelper.OID_OIWSEC_SHA384, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_384;
            }
            else if (string.Equals(oid, CapiHelper.OID_OIWSEC_SHA512, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_512;
            }
            else
            {
                return global::Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid, oidGroup, fallBackToAllGroups: false).AlgId;
            }
        }

        /// <summary>
        /// Helper for RSACryptoServiceProvider.SignData/SignHash apis.
        /// </summary>
        public static byte[] SignValue(SafeProvHandle hProv, SafeCapiKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash)
        {
            using (SafeHashHandle hHash = hProv.CreateHashHandle(hash, calgHash))
            {
                int cbSignature = 0;
                if (!Interop.Advapi32.CryptSignHash(hHash, (Interop.Advapi32.KeySpec)keyNumber, null, Interop.Advapi32.CryptSignAndVerifyHashFlags.None, null, ref cbSignature))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                byte[] signature = new byte[cbSignature];
                if (!Interop.Advapi32.CryptSignHash(hHash, (Interop.Advapi32.KeySpec)keyNumber, null, Interop.Advapi32.CryptSignAndVerifyHashFlags.None, signature, ref cbSignature))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                switch (calgKey)
                {
                    case CALG_RSA_SIGN:
                        Array.Reverse(signature);
                        break;

                    case CALG_DSS_SIGN:
                        ReverseDsaSignature(signature, cbSignature);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
                return signature;
            }
        }

        /// <summary>
        /// Helper for RSACryptoServiceProvider.VerifyData/VerifyHash apis.
        /// </summary>
        public static bool VerifySign(SafeProvHandle hProv, SafeCapiKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature)
        {
            switch (calgKey)
            {
                case CALG_RSA_SIGN:
                    signature = signature.CloneByteArray();
                    Array.Reverse(signature);
                    break;

                case CALG_DSS_SIGN:
                    signature = signature.CloneByteArray();
                    ReverseDsaSignature(signature, signature.Length);
                    break;

                default:
                    throw new InvalidOperationException();
            }

            using (SafeHashHandle? hHash = hProv.CreateHashHandle(hash, calgHash, throwOnSizeError: false))
            {
                if (hHash == null)
                {
                    return false;
                }

                bool verified = Interop.Advapi32.CryptVerifySignature(hHash, signature, signature.Length, hKey, null, Interop.Advapi32.CryptSignAndVerifyHashFlags.None);
                return verified;
            }
        }

        /// Helper method used by PasswordDeriveBytes.CryptDeriveKey to invoke CAPI CryptDeriveKey.
        public static void DeriveKey(
            SafeProvHandle hProv,
            int algid,
            int algidHash,
            byte[] password,
            int cbPassword,
            int dwFlags,
            byte[] IV_Out,
            int cbIV_In,
            [NotNull] ref byte[]? pbKey)
        {
            VerifyValidHandle(hProv);

            SafeHashHandle? hHash = null;
            SafeCapiKeyHandle? hKey = null;
            try
            {
                if (!CryptCreateHash(hProv, algidHash, SafeCapiKeyHandle.InvalidHandle, Interop.Advapi32.CryptCreateHashFlags.None, out hHash))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Hash the password string
                if (!Interop.Advapi32.CryptHashData(hHash, password, cbPassword, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Create a block cipher session key based on the hash of the password
                if (!CryptDeriveKey(hProv, algid, hHash, dwFlags | (int)CryptGenKeyFlags.CRYPT_EXPORTABLE, out hKey))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Get the key contents
                byte[]? rgbKey = null;
                int cbKey = 0;
                UnloadKey(hProv, hKey, ref rgbKey, ref cbKey);

                // Get the length of the IV
                int cbIV = 0;
                if (!Interop.Advapi32.CryptGetKeyParam(hKey, Interop.Advapi32.CryptGetKeyParamFlags.KP_IV, null, ref cbIV, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Now allocate space for the IV
                byte[] pbIV = new byte[cbIV];
                if (!Interop.Advapi32.CryptGetKeyParam(hKey, Interop.Advapi32.CryptGetKeyParamFlags.KP_IV, pbIV, ref cbIV, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                if (cbIV != cbIV_In)
                {
                    throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_InvalidIV);
                }

                // Copy the IV
                Buffer.BlockCopy(pbIV, 0, IV_Out, 0, cbIV);

                pbKey = new byte[cbKey];
                Buffer.BlockCopy(rgbKey, 0, pbKey, 0, cbKey);
            }
            finally
            {
                hKey?.Dispose();
                hHash?.Dispose();
            }
        }

        // Helper method used by DeriveKey (above) to return the key contents.
        // WARNING: This function side-effects its first argument (hProv)
        private static void UnloadKey(SafeProvHandle hProv, SafeCapiKeyHandle hKey, [NotNull] ref byte[]? key_out, ref int cb_out)
        {
            SafeCapiKeyHandle? hPubKey = null;
            try
            {
                // Import the public key
                if (!CryptImportKey(hProv, RgbPubKey, SafeCapiKeyHandle.InvalidHandle, 0, out hPubKey))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Determine length of hKey
                int cbOut = 0;
                if (!Interop.Advapi32.CryptExportKey(hKey, hPubKey, SIMPLEBLOB, 0, null, ref cbOut))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Export hKey
                byte[] key_full = new byte[cbOut];
                if (!Interop.Advapi32.CryptExportKey(hKey, hPubKey, SIMPLEBLOB, 0, key_full, ref cbOut))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                // Get size of the key without the header parts
                int sizeOfBlobHeader = sizeof(byte) + sizeof(byte) + sizeof(ushort) + sizeof(int);
                // The format of BLOBHEADER:
                //  BYTE   bType
                //  BYTE   bVersion
                //  WORD   reserved
                //  ALG_ID aiKeyAlg
                int offsetPastHeader = sizeOfBlobHeader + sizeof(int);
                int i;
                checked
                {
                    i = cbOut - sizeOfBlobHeader - sizeof(int) - 2;
                }
                while (i > 0)
                {
                    if (key_full[i + offsetPastHeader] == 0)
                    {
                        break;
                    }

                    i--;
                }

                // Allocate and initialize the return buffer
                key_out = new byte[i];
                Buffer.BlockCopy(key_full, offsetPastHeader, key_out, 0, i);
                Array.Reverse(key_out);
                cb_out = i;
            }
            finally
            {
                hPubKey?.Dispose();
            }
        }

        /// <summary>
        /// Create a CAPI-1 hash handle that contains the specified bits as its hash value.
        /// </summary>
        private static SafeHashHandle CreateHashHandle(this SafeProvHandle hProv, byte[] hash, int calgHash)
        {
            return CreateHashHandle(hProv, hash, calgHash, throwOnSizeError: true)!;
        }

        private static SafeHashHandle? CreateHashHandle(this SafeProvHandle hProv, byte[] hash, int calgHash, bool throwOnSizeError)
        {
            SafeHashHandle? hHash;
            if (!CryptCreateHash(hProv, calgHash, SafeCapiKeyHandle.InvalidHandle, Interop.Advapi32.CryptCreateHashFlags.None, out hHash))
            {
                int hr = Marshal.GetHRForLastWin32Error();

                hHash.Dispose();

                throw hr.ToCryptographicException();
            }

            try
            {
                int dwHashSize = 0;
                int cbHashSize = sizeof(int);
                if (!Interop.Advapi32.CryptGetHashParam(hHash, Interop.Advapi32.CryptHashProperty.HP_HASHSIZE, out dwHashSize, ref cbHashSize, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                if (dwHashSize != hash.Length)
                {
                    if (throwOnSizeError)
                    {
                        throw unchecked((int)CryptKeyError.NTE_BAD_HASH).ToCryptographicException();
                    }

                    return null;
                }

                if (!Interop.Advapi32.CryptSetHashParam(hHash, Interop.Advapi32.CryptHashProperty.HP_HASHVAL, hash, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                SafeHashHandle hHashPermanent = hHash;
                hHash = null;
                return hHashPermanent;
            }
            finally
            {
                hHash?.Dispose();
            }
        }

        public static CryptographicException GetBadDataException()
        {
            const int NTE_BAD_DATA = unchecked((int)CryptKeyError.NTE_BAD_DATA);
            return NTE_BAD_DATA.ToCryptographicException();
        }

        public static CryptographicException GetEFailException()
        {
            return E_FAIL.ToCryptographicException();
        }

        public static bool CryptGetUserKey(
            SafeProvHandle safeProvHandle,
            int dwKeySpec,
            out SafeCapiKeyHandle safeKeyHandle)
        {
            bool response = Interop.Advapi32.CryptGetUserKey(safeProvHandle, dwKeySpec, out safeKeyHandle);

            safeKeyHandle.SetParent(safeProvHandle);

            return response;
        }

        public static bool CryptGenKey(
            SafeProvHandle safeProvHandle,
            int algId,
            int dwFlags,
            out SafeCapiKeyHandle safeKeyHandle)
        {
            bool response = Interop.Advapi32.CryptGenKey(safeProvHandle, algId, dwFlags, out safeKeyHandle);

            safeKeyHandle.SetParent(safeProvHandle);

            return response;
        }

        public static unsafe bool CryptImportKey(
            SafeProvHandle hProv,
            ReadOnlySpan<byte> pbData,
            SafeCapiKeyHandle hPubKey,
            int dwFlags,
            out SafeCapiKeyHandle phKey)
        {
            fixed (byte* pbDataPtr = pbData)
            {
                bool response = Interop.Advapi32.CryptImportKey(hProv, pbDataPtr, pbData.Length, hPubKey, dwFlags, out phKey);

                phKey.SetParent(hProv);

                return response;
            }
        }

        public static bool CryptCreateHash(
            SafeProvHandle hProv,
            int algId,
            SafeCapiKeyHandle hKey,
            Interop.Advapi32.CryptCreateHashFlags dwFlags,
            out SafeHashHandle phHash)
        {
            bool response = Interop.Advapi32.CryptCreateHash(hProv, algId, hKey, dwFlags, out phHash);

            phHash.SetParent(hProv);

            return response;
        }

        public static bool CryptDeriveKey(
            SafeProvHandle hProv,
            int algId,
            SafeHashHandle phHash,
            int dwFlags,
            out SafeCapiKeyHandle phKey)
        {
            bool response = Interop.Advapi32.CryptDeriveKey(hProv, algId, phHash, dwFlags, out phKey);

            phKey.SetParent(hProv);

            return response;
        }
    }

    /// <summary>
    /// All the Crypto flags are capture in following
    /// </summary>
    internal static partial class CapiHelper
    {
        internal const int CALG_DES = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 1);
        internal const int CALG_RC2 = (ALG_CLASS_DATA_ENCRYPT | ALG_TYPE_BLOCK | 2);
        internal const int CALG_MD5 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 3);
        internal const int CALG_SHA1 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 4);
        internal const int CALG_SHA_256 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 12);
        internal const int CALG_SHA_384 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 13);
        internal const int CALG_SHA_512 = (ALG_CLASS_HASH | ALG_TYPE_ANY | 14);

        internal const string OID_OIWSEC_SHA256 = "2.16.840.1.101.3.4.2.1";
        internal const string OID_OIWSEC_SHA384 = "2.16.840.1.101.3.4.2.2";
        internal const string OID_OIWSEC_SHA512 = "2.16.840.1.101.3.4.2.3";

        // MS provider names.
        internal const string MS_DEF_DH_SCHANNEL_PROV = "Microsoft DH Schannel Cryptographic Provider";
        internal const string MS_DEF_DSS_DH_PROV = "Microsoft Base DSS and Diffie-Hellman Cryptographic Provider";
        internal const string MS_DEF_DSS_PROV = "Microsoft Base DSS Cryptographic Provider";
        internal const string MS_DEF_PROV = "Microsoft Base Cryptographic Provider v1.0";
        internal const string MS_DEF_RSA_SCHANNEL_PROV = "Microsoft RSA Schannel Cryptographic Provider";
        internal const string MS_DEF_RSA_SIG_PROV = "Microsoft RSA Signature Cryptographic Provider";
        internal const string MS_ENH_DSS_DH_PROV = "Microsoft Enhanced DSS and Diffie-Hellman Cryptographic Provider";
        internal const string MS_ENH_RSA_AES_PROV = "Microsoft Enhanced RSA and AES Cryptographic Provider";
        internal const string MS_ENHANCED_PROV = "Microsoft Enhanced Cryptographic Provider v1.0";
        internal const string MS_SCARD_PROV = "Microsoft Base Smart Card Crypto Provider";
        internal const string MS_STRONG_PROV = "Microsoft Strong Cryptographic Provider";

        [Flags]
        internal enum CryptGetProvParamPPImpTypeFlags : int
        {
            CRYPT_IMPL_HARDWARE = 0x1,
            CRYPT_IMPL_SOFTWARE = 0x2,
            CRYPT_IMPL_MIXED = 0x3,
            CRYPT_IMPL_UNKNOWN = 0x4,
            CRYPT_IMPL_REMOVABLE = 0x8
        }
        //All the flags are capture here

        internal enum CryptGetKeyParamQueryType : int
        {
            KP_IV = 1,
            KP_MODE = 4,
            KP_MODE_BITS = 5,
            KP_EFFECTIVE_KEYLEN = 19,
            KP_KEYLEN = 9,  // Length of key in bits
            KP_ALGID = 7 // Key algorithm
        }

        internal enum CryptGenKeyFlags : int
        {
            // dwFlag definitions for CryptGenKey
            CRYPT_EXPORTABLE = 0x00000001,
            CRYPT_USER_PROTECTED = 0x00000002,
            CRYPT_CREATE_SALT = 0x00000004,
            CRYPT_UPDATE_KEY = 0x00000008,
            CRYPT_NO_SALT = 0x00000010,
            CRYPT_PREGEN = 0x00000040,
            CRYPT_RECIPIENT = 0x00000010,
            CRYPT_INITIATOR = 0x00000040,
            CRYPT_ONLINE = 0x00000080,
            CRYPT_SF = 0x00000100,
            CRYPT_CREATE_IV = 0x00000200,
            CRYPT_KEK = 0x00000400,
            CRYPT_DATA_KEY = 0x00000800,
            CRYPT_VOLATILE = 0x00001000,
            CRYPT_SGCKEY = 0x00002000,
            CRYPT_ARCHIVABLE = 0x00004000
        }

        internal enum CspAlgorithmType
        {
            Rsa = 0,
            Dss = 1
        }

        internal enum ClrPropertyId
        {
            CLR_KEYLEN = 1,
            CLR_PUBLICKEYONLY = 2,
            CLR_EXPORTABLE = 3,
            CLR_REMOVABLE = 4,
            CLR_HARDWARE = 5,
            CLR_ACCESSIBLE = 6,
            CLR_PROTECTED = 7,
            CLR_UNIQUE_CONTAINER = 8,
            CLR_ALGID = 9,
            CLR_PP_CLIENT_HWND = 10,
            CLR_PP_PIN = 11,
        }
    }
}
