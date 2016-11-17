// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static Interop.Crypt32;
using Libraries = Interop.Libraries;

namespace Internal.NativeCrypto
{
    /// <summary>
    /// Following part of CAPIHelper keeps the wrappers for all the PInvoke calls
    /// </summary>
    internal static partial class CapiHelper
    {
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
        public static string UpgradeRSA(int dwProvType, string wszProvider)
        {
            bool requestedEnhanced = string.Equals(wszProvider, MS_ENHANCED_PROV, StringComparison.Ordinal);
            bool requestedBase = string.Equals(wszProvider, MS_DEF_PROV, StringComparison.Ordinal);
            string wszUpgrade = null;
            if (requestedBase || requestedEnhanced)
            {
                SafeProvHandle safeProvHandle;

                // attempt to use the AES provider
                if (S_OK == AcquireCryptContext(out safeProvHandle, null, MS_ENH_RSA_AES_PROV,
                                                dwProvType, (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
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
            if (!Interop.CryptGetDefaultProvider(dwType, IntPtr.Zero,
                                                (int)GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT,
                                                null, ref sizeofProviderName))
            {
                throw new CryptographicException(SR.Format(SR.CryptGetDefaultProvider_Fail, Convert.ToString(GetErrorCode())));
            }
            //allocate memory for the provider name
            StringBuilder providerName = new StringBuilder((int)sizeofProviderName);

            //Now call the function CryptGetDefaultProvider again to get the name of the provider             
            if (!Interop.CryptGetDefaultProvider(dwType, IntPtr.Zero,
                                                (int)GetDefaultProviderFlags.CRYPT_MACHINE_DEFAULT,
                                                providerName, ref sizeofProviderName))
            {
                throw new CryptographicException(SR.Format(SR.CryptGetDefaultProvider_Fail, Convert.ToString(GetErrorCode())));
            }
            // check to see if there are upgrades available for the requested CSP
            string wszUpgrade = null;
            if (dwType == (int)ProviderType.PROV_RSA_FULL)
            {
                wszUpgrade = UpgradeRSA(dwType, providerName.ToString());
            }
            else if (dwType == (int)ProviderType.PROV_DSS_DH)
            {
                wszUpgrade = UpgradeDSS(dwType, providerName.ToString());
            }
            if (null != wszUpgrade)
            {
                //Overwrite the provider name with the upgraded provider name
                providerName = new StringBuilder(wszUpgrade);
            }
            return providerName.ToString();
        }

        /// <summary>
        /// Creates a new key container
        /// </summary>
        private static void CreateCSP(CspParameters parameters, bool randomKeyContainer, out SafeProvHandle safeProvHandle)
        {
            uint dwFlags = (uint)CryptAcquireContextFlags.CRYPT_NEWKEYSET;
            if (randomKeyContainer)
            {
                dwFlags |= (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
            }

            SafeProvHandle hProv;
            int ret = OpenCSP(parameters, dwFlags, out hProv);
            if (S_OK != ret)
            {
                hProv.Dispose();
                throw new CryptographicException(SR.Format(SR.OpenCSP_Failed, Convert.ToString(ret)));
            }
            safeProvHandle = hProv;
        }

        /// <summary>
        /// Acquire a handle to a crypto service provider and optionally a key container
        /// This function implements the WszCryptAcquireContext_SO_TOLERANT
        /// </summary>
        private static int AcquireCryptContext(out SafeProvHandle safeProvHandle, string keyContainer,
                                                string providerName, int providerType, uint flags)
        {
            const uint VerifyContextFlag = (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
            const uint MachineContextFlag = (uint)CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET;

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
            if (!Interop.CryptAcquireContext(out safeProvHandle, keyContainer, providerName, providerType, flags))
            {
                ret = GetErrorCode();
            }

            return ret;
        }


        /// <summary>
        /// Acquire a handle to a crypto service provider and optionally a key container
        /// </summary>
        public static bool CryptAcquireContext(out SafeProvHandle psafeProvHandle, string pszContainer, string pszProvider, int dwProvType, uint dwFlags)
        {
            return Interop.CryptAcquireContext(out psafeProvHandle, pszContainer, pszProvider, dwProvType, dwFlags);
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
            int ret = OpenCSP(cspParameters, (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT, out hProv);
            if (S_OK != ret)
            {
                hProv.Dispose();
                throw new CryptographicException(SR.Format(SR.OpenCSP_Failed, Convert.ToString(ret)));
            }

            safeProvHandle = hProv;
        }

        /// <summary>
        /// OpenCSP performs the core work of opening and creating CSPs and containers in CSPs
        /// </summary>
        public static int OpenCSP(CspParameters cspParameters, uint flags, out SafeProvHandle safeProvHandle)
        {
            string providerName = null;
            string containerName = null;
            if (null == cspParameters)
            {
                throw new ArgumentException(SR.Format(SR.CspParameter_invalid, nameof(cspParameters)));
            }

            //look for provider type in the cspParameters
            int providerType = cspParameters.ProviderType;

            //look for provider name in the cspParamters 
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
            if (S_OK != AcquireCryptContext(out hProv, containerName, providerName, providerType, flags))
            {
                hProv.Dispose();
                safeProvHandle = SafeProvHandle.InvalidHandle;
                return GetErrorCode();
            }

            hProv.ContainerName = containerName;
            hProv.ProviderName = providerName;
            hProv.Types = providerType;
            hProv.Flags = flags;

            // We never want to delete a key container if it's already there.
            if (IsFlagBitSet(flags, (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
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
            uint hr = (uint)OpenCSP(parameters, flag, out safeProvHandle);
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
                    throw new CryptographicException(SR.Format(SR.OpenCSP_Failed, Convert.ToString(hr)));
                }

                //Create a new CSP. This method throws exception on failure
                CreateCSP(parameters, randomKeyContainer, out safeProvHandle);
            }

            if (parameters.ParentWindowHandle != IntPtr.Zero)
            {
                IntPtr parentWindowHandle = parameters.ParentWindowHandle;
                Interop.CryptSetProvParam(safeProvHandle, CryptGetProvParam.PP_CLIENT_HWND, ref parentWindowHandle, 0);
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
        internal static int GetProviderParameterWorker(SafeProvHandle safeProvHandle, byte[] impType, ref int cb, CryptGetProvParam flags)
        {
            int impTypeReturn = 0;
            if (!Interop.CryptGetProvParam(safeProvHandle, (int)flags, impType, ref cb, 0))
            {
                throw new CryptographicException(SR.Format(SR.CryptGetProvParam_Failed, Convert.ToString(GetErrorCode())));
            }
            if (null != impType && cb == Constants.SIZE_OF_DWORD)
            {
                impTypeReturn = BitConverter.ToInt32(impType, 0);
            }
            return impTypeReturn;
        }

        /// <summary>
        /// This method queries the key container and get some of it's properties. 
        /// Those properties should never cause UI to display. 
        /// </summary>                
        public static object GetProviderParameter(SafeProvHandle safeProvHandle, int keyNumber, int keyParam)
        {
            VerifyValidHandle(safeProvHandle);
            byte[] impType = new byte[Constants.SIZE_OF_DWORD];
            int cb = sizeof(byte) * Constants.SIZE_OF_DWORD;
            SafeKeyHandle safeKeyHandle = SafeKeyHandle.InvalidHandle;
            int impTypeReturn = 0;
            int returnType = 0; //using 0 for bool and 1 for string return types
            bool retVal = false;
            string retStr = null;

            try
            {
                switch (keyParam)
                {
                    case Constants.CLR_EXPORTABLE:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptGetProvParam.PP_IMPTYPE);
                        //If implementation type is not HW
                        if (!IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_HARDWARE))
                        {
                            if (!Interop.CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle))
                            {
                                throw new CryptographicException(SR.Format(SR.CryptGetUserKey_Failed, Convert.ToString(GetErrorCode())));
                            }
                            byte[] permissions = null;
                            int permissionsReturn = 0;
                            permissions = new byte[Constants.SIZE_OF_DWORD];
                            cb = sizeof(byte) * Constants.SIZE_OF_DWORD;
                            if (!Interop.CryptGetKeyParam(safeKeyHandle, (int)CryptGetKeyParamFlags.KP_PERMISSIONS, permissions, ref cb, 0))
                            {
                                throw new CryptographicException(SR.Format(SR.CryptGetKeyParam_Failed, Convert.ToString(GetErrorCode())));
                            }
                            permissionsReturn = BitConverter.ToInt32(permissions, 0);
                            retVal = IsFlagBitSet((uint)permissionsReturn, (uint)CryptGetKeyParamFlags.CRYPT_EXPORT);
                        }
                        else
                        {
                            //Assumption HW keys are not exportable.
                            retVal = false;
                        }

                        break;
                    }
                    case Constants.CLR_REMOVABLE:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptGetProvParam.PP_IMPTYPE);
                        retVal = IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_REMOVABLE);
                        break;
                    }
                    case Constants.CLR_HARDWARE:
                    case Constants.CLR_PROTECTED:
                    {
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, impType, ref cb, CryptGetProvParam.PP_IMPTYPE);
                        retVal = IsFlagBitSet((uint)impTypeReturn, (uint)CryptGetProvParamPPImpTypeFlags.CRYPT_IMPL_HARDWARE);
                        break;
                    }
                    case Constants.CLR_ACCESSIBLE:
                    {
                        retVal = Interop.CryptGetUserKey(safeProvHandle, keyNumber, out safeKeyHandle) ? true : false;
                        break;
                    }
                    case Constants.CLR_UNIQUE_CONTAINER:
                    {
                        returnType = 1;
                        byte[] pb = null;
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, pb, ref cb, CryptGetProvParam.PP_UNIQUE_CONTAINER);
                        pb = new byte[cb];
                        impTypeReturn = GetProviderParameterWorker(safeProvHandle, pb, ref cb, CryptGetProvParam.PP_UNIQUE_CONTAINER);
                        // GetProviderParameterWorker allocated the null character, we want to not interpret that.
                        Debug.Assert(cb > 0);
                        Debug.Assert(pb[cb - 1] == 0);
                        retStr = Encoding.ASCII.GetString(pb, 0, cb - 1);
                        break;
                    }
                    default:
                    {
                        Debug.Assert(false);
                        break;
                    }
                }
            }
            finally
            {
                safeKeyHandle.Dispose();
            }

            Debug.Assert(returnType == 0 || returnType == 1);
            return returnType == 0 ? (object)retVal : retStr;
        }

        /// <summary>
        /// Retrieves the handle for user public / private key pair. 
        /// </summary>
        internal static int GetUserKey(SafeProvHandle safeProvHandle, int keySpec, out SafeKeyHandle safeKeyHandle)
        {
            int hr = S_OK;
            VerifyValidHandle(safeProvHandle);
            if (!Interop.CryptGetUserKey(safeProvHandle, keySpec, out safeKeyHandle))
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
        internal static int GenerateKey(SafeProvHandle safeProvHandle, int algID, int flags, uint keySize, out SafeKeyHandle safeKeyHandle)
        {
            int hr = S_OK;
            VerifyValidHandle(safeProvHandle);
            int capiFlags = (int)((uint)MapCspKeyFlags(flags) | ((uint)keySize << 16));
            if (!Interop.CryptGenKey(safeProvHandle, algID, capiFlags, out safeKeyHandle))
            {
                hr = GetErrorCode();
            }
            if (hr != S_OK)
            {
                throw new CryptographicException(SR.Format(SR.CryptGenKey_Failed, Convert.ToString(GetErrorCode())));
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
                cspFlags |= (uint)CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.NoPrompt))
            {
                cspFlags |= (uint)CryptAcquireContextFlags.CRYPT_SILENT;
            }
            if (IsFlagBitSet((uint)flags, (uint)CspProviderFlags.CreateEphemeralKey))
            {
                cspFlags |= (uint)CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
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
                throw new CryptographicException(SR.Format(SR.Cryptography_OpenInvalidHandle, "Handle"));
            }
        }

        /// <summary>
        ///Method helps get the different key properties
        /// </summary>
        /// <param name="safeKeyHandle">Key handle</param>
        /// <param name="dwKeyParam"> Key property you want to get</param>
        /// <returns>Returns the key property</returns>
        internal static byte[] GetKeyParameter(SafeKeyHandle safeKeyHandle, int keyParam)
        {
            byte[] pb = null;
            int cb = 0;
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case Constants.CLR_KEYLEN:
                    {
                        if (!Interop.CryptGetKeyParam(safeKeyHandle, (int)CryptGetKeyParamQueryType.KP_KEYLEN, null, ref cb, 0))
                        {
                            throw new CryptographicException(SR.Format(SR.CryptGetKeyParam_Failed, Convert.ToString(GetErrorCode())));
                        }
                        pb = new byte[cb];
                        if (!Interop.CryptGetKeyParam(safeKeyHandle, (int)CryptGetKeyParamQueryType.KP_KEYLEN, pb, ref cb, 0))
                        {
                            throw new CryptographicException(SR.Format(SR.CryptGetKeyParam_Failed, Convert.ToString(GetErrorCode())));
                        }
                        break;
                    }
                case Constants.CLR_PUBLICKEYONLY:
                    {
                        pb = new byte[1];
                        pb[0] = safeKeyHandle.PublicOnly ? (byte)1 : (byte)0;
                        break;
                    }
                case Constants.CLR_ALGID:
                    {
                        // returns the algorithm ID for the key
                        if (!Interop.CryptGetKeyParam(safeKeyHandle, (int)CryptGetKeyParamQueryType.KP_ALGID, null, ref cb, 0))
                        {
                            throw new CryptographicException(SR.Format(SR.CryptGetKeyParam_Failed, Convert.ToString(GetErrorCode())));
                        }
                        pb = new byte[cb];
                        if (!Interop.CryptGetKeyParam(safeKeyHandle, (int)CryptGetKeyParamQueryType.KP_ALGID, pb, ref cb, 0))
                        {
                            throw new CryptographicException(SR.Format(SR.CryptGetKeyParam_Failed, Convert.ToString(GetErrorCode())));
                        }
                        break;
                    }
                default:
                    {
                        Debug.Assert(false);
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
        internal static void SetKeyParameter(SafeKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, byte[] value)
        {
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case CryptGetKeyParamQueryType.KP_IV:
                    if (!Interop.CryptSetKeyParam(safeKeyHandle, (int)keyParam, value, 0))
                        throw new CryptographicException(SR.CryptSetKeyParam_Failed, Convert.ToString(GetErrorCode()));

                    break;
                default:
                    Debug.Fail("Unkown param in SetKeyParameter");
                    break;
            }
        }

        /// <summary>
        /// Set a key property which is based on int
        /// </summary>
        /// <param name="safeKeyHandle">Key handle</param>
        /// <param name="keyParam"> Key property you want to set</param>
        /// <param name="value"> Key property value you want to set</param>
        internal static void SetKeyParameter(SafeKeyHandle safeKeyHandle, CryptGetKeyParamQueryType keyParam, int value)
        {
            VerifyValidHandle(safeKeyHandle); //This will throw if handle is invalid

            switch (keyParam)
            {
                case CryptGetKeyParamQueryType.KP_MODE:
                case CryptGetKeyParamQueryType.KP_MODE_BITS:
                case CryptGetKeyParamQueryType.KP_EFFECTIVE_KEYLEN:
                    if (! Interop.CryptSetKeyParamInt(safeKeyHandle, (int)keyParam, ref value, 0))
                        throw new CryptographicException(SR.CryptSetKeyParam_Failed, Convert.ToString(GetErrorCode()));

                    break;
                default:
                    Debug.Fail("Unkown param in SetKeyParameter");
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
            CspParameters userParameters,
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
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidValue, Convert.ToString(flags)));
                }
            }
        }

        /// <summary>
        /// Helper function to get the key pair
        /// </summary>
        internal static SafeKeyHandle GetKeyPairHelper(
            CspAlgorithmType keyType,
            CspParameters parameters,
            int keySize,
            SafeProvHandle safeProvHandle)
        {
            // If the key already exists, use it, else generate a new one
            SafeKeyHandle hKey;
            int hr = CapiHelper.GetUserKey(safeProvHandle, parameters.KeyNumber, out hKey);
            if (hr != S_OK)
            {
                hKey.Dispose();
                if (IsFlagBitSet((uint)parameters.Flags, (uint)CspProviderFlags.UseExistingKey) ||
                                                         (uint)hr != (uint)CryptKeyError.NTE_NO_KEY)
                {
                    throw new CryptographicException(SR.Format(SR.CryptGetUserKey_Failed, Convert.ToString(hr)));
                }

                // GenerateKey will check for failures and throw an exception
                CapiHelper.GenerateKey(safeProvHandle, parameters.KeyNumber, (int)parameters.Flags,
                                        (uint)keySize, out hKey);
            }

            // check that this is indeed an RSA/DSS key.
            byte[] algid = CapiHelper.GetKeyParameter(hKey, Constants.CLR_ALGID);

            int dwAlgId = (algid[0] | (algid[1] << 8) | (algid[2] << 16) | (algid[3] << 24));

            if ((keyType == CspAlgorithmType.Rsa && dwAlgId != CALG_RSA_KEYX && dwAlgId != CALG_RSA_SIGN) ||
                (keyType == CspAlgorithmType.Dss && dwAlgId != CALG_DSS_SIGN))
            {
                hKey.Dispose();
                throw new CryptographicException(SR.Format(SR.Cryptography_CSP_WrongKeySpec, Convert.ToString(keyType)));
            }

            return hKey;
        }

        /// <summary>
        /// Wrapper for get last error function
        /// </summary>
        /// <returns>returns the error code</returns>
        internal static int GetErrorCode()
        {
            return Marshal.GetLastWin32Error();
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
        internal static void DecryptKey(SafeKeyHandle safeKeyHandle, byte[] encryptedData, int encryptedDataLength, bool fOAEP, out byte[] decryptedData)
        {
            VerifyValidHandle(safeKeyHandle);
            if (null == encryptedData)
            {
                throw new CryptographicException(SR.Format(SR.Argument_InvalidValue, "Encrypted Data is null"));
            }
            if (encryptedDataLength < 0)
            {
                throw new CryptographicException(SR.Format(SR.Argument_InvalidValue, "Encrypted data length is less than 0"));
            }
            byte[] dataTobeDecrypted = new byte[encryptedDataLength];
            Buffer.BlockCopy(encryptedData, 0, dataTobeDecrypted, 0, encryptedDataLength);
            Array.Reverse(dataTobeDecrypted);

            int dwFlags = fOAEP ? (int)CryptDecryptFlags.CRYPT_OAEP : 0;
            int decryptedDataLength = encryptedDataLength;
            if (!Interop.CryptDecrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwFlags, dataTobeDecrypted, ref decryptedDataLength))
            {
                int ErrCode = GetErrorCode();
                // If we're using OAEP mode and we received an NTE_BAD_FLAGS error, then OAEP is not supported on
                // this platform (XP+ only).  Throw a generic cryptographic exception if we failed to decrypt OAEP
                // padded data in order to prevent a chosen ciphertext attack.  We will allow NTE_BAD_KEY out, since
                // that error does not relate to the padding.  Otherwise just throw a cryptographic exception based on
                // the error code.
                if ((uint)((uint)dwFlags & (uint)CryptDecryptFlags.CRYPT_OAEP) == (uint)CryptDecryptFlags.CRYPT_OAEP &&
                                                                    (uint)ErrCode != (uint)CryptKeyError.NTE_BAD_KEY)
                {
                    if ((uint)ErrCode == (uint)CryptKeyError.NTE_BAD_FLAGS)
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
                    throw new CryptographicException(SR.Format(SR.CryptDecrypt_Failed, Convert.ToString(ErrCode)));
                }
            }


            decryptedData = new byte[decryptedDataLength];
            Buffer.BlockCopy(dataTobeDecrypted, 0, decryptedData, 0, decryptedDataLength);
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
        internal static void EncryptKey(SafeKeyHandle safeKeyHandle, byte[] pbKey, int cbKey, bool foep, ref byte[] pbEncryptedKey)
        {
            VerifyValidHandle(safeKeyHandle);
            if (null == pbKey)
            {
                throw new CryptographicException(SR.Format(SR.Argument_InvalidValue, "pbKey is null"));
            }
            if (cbKey < 0)
            {
                throw new CryptographicException(SR.Format(SR.Argument_InvalidValue, "cbKey is less than 0"));
            }
            int dwEncryptFlags = foep ? (int)CryptDecryptFlags.CRYPT_OAEP : 0;
            // Figure out how big the encrypted key will be
            int cbEncryptedKey = cbKey;
            if (!Interop.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwEncryptFlags, null, ref cbEncryptedKey, cbEncryptedKey))
            {
                throw new CryptographicException(SR.Format(SR.CryptEncrypt_Failed, Convert.ToString(GetErrorCode())));
            }
            // pbData is an in/out buffer for CryptEncrypt. allocate space for the encrypted key, and copy the
            // plaintext key into that space.  Since encrypted keys will have padding applied, the size of the encrypted
            // key should always be larger than the plaintext key, so use that to determine the buffer size.
            Debug.Assert(cbEncryptedKey >= cbKey);
            pbEncryptedKey = new byte[cbEncryptedKey];
            Buffer.BlockCopy(pbKey, 0, pbEncryptedKey, 0, cbKey);

            // Encrypt for real - the last parameter is the total size of the in/out buffer, while the second to last
            // parameter specifies the size of the plaintext to encrypt.
            if (!Interop.CryptEncrypt(safeKeyHandle, SafeHashHandle.InvalidHandle, true, dwEncryptFlags, pbEncryptedKey, ref cbKey, cbEncryptedKey))
            {
            }
            Debug.Assert(cbKey == cbEncryptedKey);
            Array.Reverse(pbEncryptedKey);
        }

        internal static int EncryptData(
            SafeKeyHandle hKey,
            byte[] input,
            int inputOffset,
            int inputCount,
            byte[] output,
            int outputOffset,
            int outputCount,
            bool isFinal)
        {
            VerifyValidHandle(hKey);
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);
            Debug.Assert((inputCount % 8) == 0);
            Debug.Assert((outputCount % 8) == 0);

            // Figure out how big the encrypted data will be
            int cbEncryptedData = inputCount;
            if (!Interop.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, null, ref cbEncryptedData, cbEncryptedData))
            {
                throw new CryptographicException(SR.Format(SR.CryptEncrypt_Failed, Convert.ToString(GetErrorCode())));
            }

            // encryptedData is an in/out buffer for CryptEncrypt. Allocate space for the encrypted data, and copy the
            // plaintext data into that space.  Since encrypted data will have padding applied, the size of the encrypted
            // data should always be larger than the plaintext key, so use that to determine the buffer size.
            Debug.Assert(cbEncryptedData >= inputCount);
            var encryptedData = new byte[cbEncryptedData];
            Buffer.BlockCopy(input, inputOffset, encryptedData, 0, inputCount);

            // Encrypt for real - the last parameter is the total size of the in/out buffer, while the second to last
            // parameter specifies the size of the plaintext to encrypt.
            int encryptedDataLength = inputCount;
            if (!Interop.CryptEncrypt(hKey, SafeHashHandle.InvalidHandle, isFinal, 0, encryptedData, ref encryptedDataLength, cbEncryptedData))
            {
                int errCode = GetErrorCode();
                throw new CryptographicException(SR.CryptEncrypt_Failed, Convert.ToString(errCode));
            }
            Debug.Assert(encryptedDataLength == cbEncryptedData);

            // If isFinal, padding was added so ignore it by using outputCount as size
            Buffer.BlockCopy(encryptedData, 0, output, outputOffset, outputCount);

            return outputCount;
        }

        internal static int DecryptData(
            SafeKeyHandle hKey,
            byte[] input,
            int inputOffset,
            int inputCount,
            byte[] output,
            int outputOffset,
            int outputCount)
        {
            VerifyValidHandle(hKey);
            Debug.Assert(input != null);
            Debug.Assert(inputOffset >= 0);
            Debug.Assert(inputCount >= 0);
            Debug.Assert(inputCount <= input.Length - inputOffset);
            Debug.Assert(output != null);
            Debug.Assert(outputOffset >= 0);
            Debug.Assert(outputCount >= 0);
            Debug.Assert(outputCount <= output.Length - outputOffset);
            Debug.Assert((inputCount % 8) == 0);
            Debug.Assert((outputCount % 8) == 0);

            byte[] dataTobeDecrypted = new byte[inputCount];
            Buffer.BlockCopy(input, inputOffset, dataTobeDecrypted, 0, inputCount);

            int decryptedDataLength = inputCount;
            // Always call decryption with false (not final); deal with padding manually
            if (!Interop.CryptDecrypt(hKey, SafeHashHandle.InvalidHandle, false, 0, dataTobeDecrypted, ref decryptedDataLength))
            {
                int errCode = GetErrorCode();
                throw new CryptographicException(SR.CryptDecrypt_Failed, Convert.ToString(errCode));
            }

            Buffer.BlockCopy(dataTobeDecrypted, 0, output, outputOffset, outputCount);

            return decryptedDataLength;
        }

        /// <summary>
        /// Helper for Import CSP
        /// </summary>
        internal static void ImportKeyBlob(SafeProvHandle saveProvHandle, CspProviderFlags flags, bool addNoSaltFlag, byte[] keyBlob, out SafeKeyHandle safeKeyHandle)
        {
            // Compat note: This isn't the same check as the one done by the CLR _ImportCspBlob QCall,
            // but this does match the desktop CLR behavior and the only scenarios it
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

            SafeKeyHandle hKey;
            if (!Interop.CryptImportKey(saveProvHandle, keyBlob, keyBlob.Length, SafeKeyHandle.InvalidHandle, dwCapiFlags, out hKey))
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
        internal static byte[] ExportKeyBlob(bool includePrivateParameters, SafeKeyHandle safeKeyHandle)
        {
            VerifyValidHandle(safeKeyHandle);

            byte[] pbRawData = null;
            int cbRawData = 0;
            int dwBlobType = includePrivateParameters ? PRIVATEKEYBLOB : PUBLICKEYBLOB;

            if (!Interop.CryptExportKey(safeKeyHandle, SafeKeyHandle.InvalidHandle, dwBlobType, 0, null, ref cbRawData))
            {
                throw new CryptographicException(SR.Format(SR.CryptExportKey_Failed, Convert.ToString(GetErrorCode())));
            }
            pbRawData = new byte[cbRawData];

            if (!Interop.CryptExportKey(safeKeyHandle, SafeKeyHandle.InvalidHandle, dwBlobType, 0, pbRawData, ref cbRawData))
            {
                throw new CryptographicException(SR.Format(SR.CryptExportKey_Failed, Convert.ToString(GetErrorCode())));
            }
            return pbRawData;
        }

        /// <summary>
        /// Helper for RsaCryptoServiceProvider.ImportParameters()
        /// </summary>
        internal static byte[] ToKeyBlob(this RSAParameters rsaParameters, int algId)
        {
            // The original FCall this helper emulates supports other algId's - however, the only algid we need to support is CALG_RSA_KEYX. We will not
            // port the codepaths dealing with other algid's.
            if (algId != CapiHelper.CALG_RSA_KEYX)
                throw new PlatformNotSupportedException();

            const int NTE_BAD_DATA = unchecked((int)CryptKeyError.NTE_BAD_DATA);

            // Validate the RSA structure first.
            if (rsaParameters.Modulus == null)
                throw NTE_BAD_DATA.ToCryptographicException();

            if (rsaParameters.Exponent == null || rsaParameters.Exponent.Length > 4)
                throw NTE_BAD_DATA.ToCryptographicException();

            int modulusLength = rsaParameters.Modulus.Length;
            int halfModulusLength = (modulusLength + 1) / 2;

            // We assume that if P != null, then so are Q, DP, DQ, InverseQ and D
            if (rsaParameters.P != null)
            {
                if (rsaParameters.P.Length != halfModulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();

                if (rsaParameters.Q == null || rsaParameters.Q.Length != halfModulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();

                if (rsaParameters.DP == null || rsaParameters.DP.Length != halfModulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();

                if (rsaParameters.DQ == null || rsaParameters.DQ.Length != halfModulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();

                if (rsaParameters.InverseQ == null || rsaParameters.InverseQ.Length != halfModulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();

                if (rsaParameters.D == null || rsaParameters.D.Length != modulusLength)
                    throw NTE_BAD_DATA.ToCryptographicException();
            }

            bool isPrivate = (rsaParameters.P != null && rsaParameters.P.Length != 0);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            // Write out the BLOBHEADER
            bw.Write((byte)(isPrivate ? PRIVATEKEYBLOB : PUBLICKEYBLOB));  // BLOBHEADER.bType
            bw.Write((byte)(BLOBHEADER_CURRENT_BVERSION));                 // BLOBHEADER.bVersion
            bw.Write((ushort)0);                                           // BLOBHEADER.wReserved
            bw.Write((uint)algId);                                         // BLOBHEADER.aiKeyAlg

            // Write the RSAPubKey header
            bw.Write((int)(isPrivate ? RSA_PRIV_MAGIC : RSA_PUB_MAGIC));   // RSAPubKey.magic
            bw.Write((uint)(modulusLength * 8));                           // RSAPubKey.bitLen

            uint expAsDword = 0;
            for (int i = 0; i < rsaParameters.Exponent.Length; i++)
            {
                expAsDword <<= 8;
                expAsDword |= rsaParameters.Exponent[i];
            }
            bw.Write((uint)expAsDword);                                    // RSAPubKey.pubExp

            bw.WriteReversed(rsaParameters.Modulus);                       // Copy over the modulus for both public and private

            if (isPrivate)
            {
                bw.WriteReversed(rsaParameters.P);
                bw.WriteReversed(rsaParameters.Q);
                bw.WriteReversed(rsaParameters.DP);
                bw.WriteReversed(rsaParameters.DQ);
                bw.WriteReversed(rsaParameters.InverseQ);
                bw.WriteReversed(rsaParameters.D);
            }

            bw.Flush();
            byte[] key = ms.ToArray();
            return key;
        }

        /// <summary>
        /// Write out a byte array in reverse order.
        /// </summary>
        private static void WriteReversed(this BinaryWriter bw, byte[] bytes)
        {
            byte[] reversedBytes = bytes.CloneByteArray();
            Array.Reverse(reversedBytes);
            bw.Write(reversedBytes);
            return;
        }

        /// <summary>
        /// Helper for RsaCryptoServiceProvider.ExportParameters()
        /// </summary>
        internal static RSAParameters ToRSAParameters(this byte[] cspBlob, bool includePrivateParameters)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(cspBlob));

                byte bType = br.ReadByte();    // BLOBHEADER.bType: Expected to be 0x6 (PUBLICKEYBLOB) or 0x7 (PRIVATEKEYBLOB), though there's no check for backward compat reasons. 
                byte bVersion = br.ReadByte(); // BLOBHEADER.bVersion: Expected to be 0x2, though there's no check for backward compat reasons.
                br.ReadUInt16();               // BLOBHEADER.wReserved
                int algId = br.ReadInt32();    // BLOBHEADER.aiKeyAlg
                if (algId != CALG_RSA_KEYX)
                    throw new PlatformNotSupportedException();  // The FCall this code was ported from supports other algid's but we're only porting what we use.

                int magic = br.ReadInt32();    // RSAPubKey.magic: Expected to be 0x31415352 ('RSA1') or 0x32415352 ('RSA2') 
                int bitLen = br.ReadInt32();   // RSAPubKey.bitLen

                int modulusLength = bitLen / 8;
                int halfModulusLength = (modulusLength + 1) / 2;

                uint expAsDword = br.ReadUInt32();

                RSAParameters rsaParameters = new RSAParameters();
                rsaParameters.Exponent = ExponentAsBytes(expAsDword);
                rsaParameters.Modulus = br.ReadReversed(modulusLength);
                if (includePrivateParameters)
                {
                    rsaParameters.P = br.ReadReversed(halfModulusLength);
                    rsaParameters.Q = br.ReadReversed(halfModulusLength);
                    rsaParameters.DP = br.ReadReversed(halfModulusLength);
                    rsaParameters.DQ = br.ReadReversed(halfModulusLength);
                    rsaParameters.InverseQ = br.ReadReversed(halfModulusLength);
                    rsaParameters.D = br.ReadReversed(modulusLength);
                }

                return rsaParameters;
            }
            catch (EndOfStreamException)
            {
                // For compat reasons, we throw an E_FAIL CrytoException if CAPI returns a smaller blob than expected.
                // For compat reasons, we ignore the extra bits if the CAPI returns a larger blob than expected.
                throw E_FAIL.ToCryptographicException();
            }
        }

        /// <summary>
        /// Helper for converting a UInt32 exponent to bytes.
        /// </summary>
        private static byte[] ExponentAsBytes(uint exponent)
        {
            if (exponent <= 0xFF)
            {
                return new[] { (byte)exponent };
            }
            else if (exponent <= 0xFFFF)
            {
                return new[]
                {
                    (byte)(exponent >> 8),
                    (byte)(exponent)
                };
            }
            else if (exponent <= 0xFFFFFF)
            {
                return new[]
                {
                    (byte)(exponent >> 16),
                    (byte)(exponent >> 8),
                    (byte)(exponent)
                };
            }
            else
            {
                return new[]
                {
                    (byte)(exponent >> 24),
                    (byte)(exponent >> 16),
                    (byte)(exponent >> 8),
                    (byte)(exponent)
                };
            }
        }

        /// <summary>
        /// Read in a byte array in reverse order.
        /// </summary>
        private static byte[] ReadReversed(this BinaryReader br, int count)
        {
            byte[] data = br.ReadBytes(count);
            Array.Reverse(data);
            return data;
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string to specify a hashing algorithm.
        /// </summary>
        public static int NameOrOidToHashAlgId(String nameOrOid)
        {
            // Default Algorithm Id is CALG_SHA1
            if (nameOrOid == null)
                return CapiHelper.CALG_SHA1;
            String oidValue = new Oid(nameOrOid).Value;
            if (oidValue == null)
                oidValue = nameOrOid; // we were probably passed an OID value directly

            int algId = GetAlgIdFromOid(oidValue, OidGroup.HashAlgorithm);
            if (algId == 0 || algId == -1)
                throw new CryptographicException(SR.Cryptography_InvalidOID);

            return algId;
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string/Type/HashAlgorithm to specify a hashing algorithm.
        /// </summary>
        public static int ObjToHashAlgId(Object hashAlg)
        {
            if (hashAlg == null)
                throw new ArgumentNullException(nameof(hashAlg));

            String hashAlgString = hashAlg as String;
            if (hashAlgString != null)
            {
                int algId = NameOrOidToHashAlgId(hashAlgString);
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
            else if (hashAlg is Type)
            {
                TypeInfo hashAlgTypeInfo = ((Type)hashAlg).GetTypeInfo();

                if (typeof(MD5).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
                    return CapiHelper.CALG_MD5;

                if (typeof(SHA1).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
                    return CapiHelper.CALG_SHA1;

                if (typeof(SHA256).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
                    return CapiHelper.CALG_SHA_256;

                if (typeof(SHA384).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
                    return CapiHelper.CALG_SHA_384;

                if (typeof(SHA512).GetTypeInfo().IsAssignableFrom(hashAlgTypeInfo))
                    return CapiHelper.CALG_SHA_512;
            }

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        /// <summary>
        /// Helper for signing and verifications that accept a string/Type/HashAlgorithm to specify a hashing algorithm.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5351", Justification = "MD5 is used when the user asks for it.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "SHA1 is used when the user asks for it.")]
        internal static HashAlgorithm ObjToHashAlgorithm(Object hashAlg)
        {
            int algId = ObjToHashAlgId(hashAlg);
            switch (algId)
            {
                case CapiHelper.CALG_MD5:
                    return MD5.Create();

                case CapiHelper.CALG_SHA1:
                    return SHA1.Create();

                case CapiHelper.CALG_SHA_256:
                    return SHA256.Create();

                case CapiHelper.CALG_SHA_384:
                    return SHA384.Create();

                case CapiHelper.CALG_SHA_512:
                    return SHA512.Create();

                default:
                    throw new ArgumentException(SR.Argument_InvalidValue);
            }
        }

        /// <summary>
        /// Convert an OID into a CAPI-1 CALG ID.
        /// </summary>
        private static int GetAlgIdFromOid(string oid, OidGroup oidGroup)
        {
            Debug.Assert(oid != null);

            // CAPI does not have ALGID mappings for all of the hash algorithms - see if we know the mapping
            // first to avoid doing an AD lookup on these values
            if (String.Equals(oid, CapiHelper.OID_OIWSEC_SHA256, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_256;
            }
            else if (String.Equals(oid, CapiHelper.OID_OIWSEC_SHA384, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_384;
            }
            else if (String.Equals(oid, CapiHelper.OID_OIWSEC_SHA512, StringComparison.Ordinal))
            {
                return CapiHelper.CALG_SHA_512;
            }
            else
            {
                return global::Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid, OidGroup.HashAlgorithm, fallBackToAllGroups: false).AlgId;
            }
        }

        /// <summary>
        /// Helper for RSACryptoServiceProvider.SignData/SignHash apis.
        /// </summary>
        public static byte[] SignValue(SafeProvHandle hProv, SafeKeyHandle hKey, int keyNumber, int calgKey, int calgHash, byte[] hash)
        {
            using (SafeHashHandle hHash = hProv.CreateHashHandle(hash, calgHash))
            {
                int cbSignature = 0;
                if (!Interop.CryptSignHash(hHash, (KeySpec)keyNumber, null, CryptSignAndVerifyHashFlags.None, null, ref cbSignature))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }

                byte[] signature = new byte[cbSignature];
                if (!Interop.CryptSignHash(hHash, (KeySpec)keyNumber, null, CryptSignAndVerifyHashFlags.None, signature, ref cbSignature))
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
        public static bool VerifySign(SafeProvHandle hProv, SafeKeyHandle hKey, int calgKey, int calgHash, byte[] hash, byte[] signature)
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

            using (SafeHashHandle hHash = hProv.CreateHashHandle(hash, calgHash))
            {
                bool verified = Interop.CryptVerifySignature(hHash, signature, signature.Length, hKey, null, CryptSignAndVerifyHashFlags.None);
                return verified;
            }
        }

        /// <summary>
        /// Create a CAPI-1 hash handle that contains the specified bits as its hash value.
        /// </summary>
        private static SafeHashHandle CreateHashHandle(this SafeProvHandle hProv, byte[] hash, int calgHash)
        {
            SafeHashHandle hHash;
            if (!Interop.CryptCreateHash(hProv, calgHash, SafeKeyHandle.InvalidHandle, CryptCreateHashFlags.None, out hHash))
            {
                int hr = Marshal.GetHRForLastWin32Error();

                hHash.Dispose();

                throw hr.ToCryptographicException();
            }

            try
            {
                int dwHashSize = 0;
                int cbHashSize = sizeof(int);
                if (!Interop.CryptGetHashParam(hHash, CryptHashProperty.HP_HASHSIZE, out dwHashSize, ref cbHashSize, 0))
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    throw hr.ToCryptographicException();
                }
                if (dwHashSize != hash.Length)
                    throw unchecked((int)CryptKeyError.NTE_BAD_HASH).ToCryptographicException();

                if (!Interop.CryptSetHashParam(hHash, CryptHashProperty.HP_HASHVAL, hash, 0))
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
                if (hHash != null)
                {
                    hHash.Dispose();
                }
            }
        }

        /// <summary>
        /// Destroy a crypto provider.
        /// </summary>
        public static bool CryptReleaseContext(IntPtr safeProvHandle, int dwFlags)
        {
            return Interop.CryptReleaseContext(safeProvHandle, dwFlags);
        }

        /// <summary>
        /// Destroy a crypto key.
        /// </summary>
        public static bool CryptDestroyKey(IntPtr hKey)
        {
            return Interop.CryptDestroyKey(hKey);
        }

        /// <summary>
        /// Destroy a crypto hash.
        /// </summary>
        public static bool CryptDestroyHash(IntPtr hHash)
        {
            return Interop.CryptDestroyHash(hHash);
        }
    }//End of class CapiHelper : Wrappers

    //
    /// <summary>
    /// All the PInvoke are captured in following part of CapiHelper class 
    /// </summary>
    internal static partial class CapiHelper
    {
        private static class Interop
        {
            [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptGetDefaultProviderW")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptGetDefaultProvider(int dwProvType, IntPtr pdwReserved, int dwFlags,
                                                              StringBuilder pszProvName, ref int IntPtrProvName);

            [DllImport(Libraries.Advapi32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CryptAcquireContextW")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptAcquireContext(out SafeProvHandle psafeProvHandle, string pszContainer,
                                                            string pszProvider, int dwProvType, uint dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptGetProvParam(SafeProvHandle safeProvHandle, int dwParam, byte[] pbData,
                                                        ref int dwDataLen, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptSetProvParam(SafeProvHandle safeProvHandle, CryptGetProvParam dwParam, ref IntPtr pbData, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true, EntryPoint = "CryptGetUserKey")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool _CryptGetUserKey(SafeProvHandle safeProvHandle, int dwKeySpec, out SafeKeyHandle safeKeyHandle);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptGetKeyParam(SafeKeyHandle safeKeyHandle, int dwParam, byte[] pbData,
                                                        ref int pdwDataLen, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptSetKeyParam(SafeKeyHandle safeKeyHandle, int dwParam, byte[] pbData, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true, EntryPoint = "CryptSetKeyParam")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptSetKeyParamInt(SafeKeyHandle safeKeyHandle, int dwParam, ref int pdw, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true, EntryPoint = "CryptGenKey")]
            private static extern bool _CryptGenKey(SafeProvHandle safeProvHandle, int Algid, int dwFlags, out SafeKeyHandle safeKeyHandle);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            public static extern bool CryptReleaseContext(IntPtr safeProvHandle, int dwFlags);

            [DllImport(Libraries.Advapi32, SetLastError = true)]
            public static extern bool CryptDecrypt(SafeKeyHandle safeKeyHandle, SafeHashHandle safeHashHandle, bool Final,
                                                    int dwFlags, byte[] pbData, ref int pdwDataLen);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptEncrypt(SafeKeyHandle safeKeyHandle, SafeHashHandle safeHashHandle,
                                                    bool Final, int dwFlags, byte[] pbData, ref int pdwDataLen,
                                                    int dwBufLen);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptExportKey(SafeKeyHandle hKey, SafeKeyHandle hExpKey, int dwBlobType,
                                                    int dwFlags, [In, Out] byte[] pbData, ref int dwDataLen);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptImportKey")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool _CryptImportKey(SafeProvHandle hProv, byte[] pbData, int dwDataLen, SafeKeyHandle hPubKey, int dwFlags, out SafeKeyHandle phKey);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptCreateHash")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool _CryptCreateHash(SafeProvHandle hProv, int algId, SafeKeyHandle hKey, CryptCreateHashFlags dwFlags, out SafeHashHandle phHash);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptGetHashParam(SafeHashHandle hHash, CryptHashProperty dwParam, out int pbData, [In, Out] ref int pdwDataLen, int dwFlags);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptSetHashParam(SafeHashHandle hHash, CryptHashProperty dwParam, byte[] buffer, int dwFlags);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptSignHashW")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptSignHash(SafeHashHandle hHash, KeySpec dwKeySpec, String sDescription, CryptSignAndVerifyHashFlags dwFlags, [Out] byte[] pbSignature, [In, Out] ref int pdwSigLen);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptVerifySignatureW")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptVerifySignature(SafeHashHandle hHash, byte[] pbSignature, int dwSigLen, SafeKeyHandle hPubKey, String sDescription, CryptSignAndVerifyHashFlags dwFlags);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptDestroyKey(IntPtr hKey);

            [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CryptDestroyHash(IntPtr hHash);

            public static bool CryptGetUserKey(
                SafeProvHandle safeProvHandle,
                int dwKeySpec,
                out SafeKeyHandle safeKeyHandle)
            {
                bool response = _CryptGetUserKey(safeProvHandle, dwKeySpec, out safeKeyHandle);

                safeKeyHandle.SetParent(safeProvHandle);

                return response;
            }

            public static bool CryptGenKey(
                SafeProvHandle safeProvHandle,
                int algId,
                int dwFlags,
                out SafeKeyHandle safeKeyHandle)
            {
                bool response = _CryptGenKey(safeProvHandle, algId, dwFlags, out safeKeyHandle);

                safeKeyHandle.SetParent(safeProvHandle);

                return response;
            }

            public static bool CryptImportKey(
                SafeProvHandle hProv,
                byte[] pbData,
                int dwDataLen,
                SafeKeyHandle hPubKey,
                int dwFlags,
                out SafeKeyHandle phKey)
            {
                bool response = _CryptImportKey(hProv, pbData, dwDataLen, hPubKey, dwFlags, out phKey);

                phKey.SetParent(hProv);

                return response;
            }

            public static bool CryptCreateHash(
                SafeProvHandle hProv,
                int algId,
                SafeKeyHandle hKey,
                CryptCreateHashFlags dwFlags,
                out SafeHashHandle phHash)
            {
                bool response = _CryptCreateHash(hProv, algId, hKey, dwFlags, out phHash);

                phHash.SetParent(hProv);

                return response;
            }
        }
    } //End CapiHelper : Pinvokes

    /// <summary>
    /// All the Crypto flags are capture in following 
    /// </summary>
    internal static partial class CapiHelper
    {
        internal const int S_OK = 0;
        internal const int E_FAIL = unchecked((int)0x80004005);

        // Provider type to use by default for RSA operations. We want to use RSA-AES CSP
        // since it enables access to SHA-2 operations. All currently supported OSes support RSA-AES.
        internal const int DefaultRsaProviderType = (int)ProviderType.PROV_RSA_AES;
        //Leaving these constants same as they are defined in Windows
        internal const int ALG_TYPE_DSS = (1 << 9);
        internal const int ALG_TYPE_RSA = (2 << 9);
        internal const int ALG_TYPE_BLOCK = (3 << 9);
        internal const int ALG_CLASS_SIGNATURE = (1 << 13);
        internal const int ALG_CLASS_DATA_ENCRYPT = (3 << 13);
        internal const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);
        internal const int CALG_RSA_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_RSA | 0);
        internal const int CALG_DSS_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_DSS | 0);
        internal const int CALG_RSA_KEYX = (ALG_CLASS_KEY_EXCHANGE | ALG_TYPE_RSA | 0);

        internal const int ALG_CLASS_HASH = (4 << 13);
        internal const int ALG_TYPE_ANY = (0);

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

        internal const int PUBLICKEYBLOB = 0x6;
        internal const int PRIVATEKEYBLOB = 0x7;
        internal const int PLAINTEXTKEYBLOB = 0x8;
        internal const byte BLOBHEADER_CURRENT_BVERSION = 0x2;
        internal const int CRYPT_BLOB_VER3 = 0x00000080; // export version 3 of a blob type
        internal const int RSA_PUB_MAGIC = 0x31415352;
        internal const int RSA_PRIV_MAGIC = 0x32415352;

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

        internal enum CryptDecryptFlags : int
        {
            CRYPT_OAEP = 0x00000040,
            CRYPT_DECRYPT_RSA_NO_PADDING_CHECK = 0x00000020
        }
        internal enum GetDefaultProviderFlags : int
        {
            CRYPT_MACHINE_DEFAULT = 0x00000001,
            CRYPT_USER_DEFAULT = 0x00000002
        }

        internal enum CryptGetKeyParamFlags : int
        {
            CRYPT_EXPORT = 0x0004,
            KP_PERMISSIONS = 6
        }

        internal enum CryptGetProvParam : int
        {
            PP_CLIENT_HWND = 1,
            PP_IMPTYPE = 3,
            PP_UNIQUE_CONTAINER = 36
        }

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
        [Flags]
        internal enum CryptAcquireContextFlags : uint
        {
            None = 0x00000000,
            CRYPT_NEWKEYSET = 0x00000008,                         // CRYPT_NEWKEYSET
            CRYPT_DELETEKEYSET = 0x00000010,                      // CRYPT_DELETEKEYSET
            CRYPT_MACHINE_KEYSET = 0x00000020,                     // CRYPT_MACHINE_KEYSET
            CRYPT_SILENT = 0x00000040,                            // CRYPT_SILENT
            CRYPT_VERIFYCONTEXT = 0xF0000000      // CRYPT_VERIFYCONTEXT
        }

        /// <summary>
        /// dwProvType is fourth parameter in the CryptAcquireContext and it uses following
        /// </summary>
        internal enum ProviderType : int
        {
            PROV_RSA_FULL = 1,
            PROV_DSS_DH = 13,
            PROV_RSA_AES = 24
        }

        internal enum CryptKeyError : uint
        {
            NTE_NO_KEY = 0x8009000D, // Key does not exist.
            NTE_BAD_KEYSET = 0x80090016, // Keyset does not exist.
            NTE_KEYSET_NOT_DEF = 0x80090019, // The keyset is not defined.
            NTE_BAD_KEY_STATE = 0x8009000B,
            NTE_BAD_KEYSET_PARAM = 0x8009001F,
            NTE_KEYSET_ENTRY_BAD = 0x8009001A,
            NTE_FILENOTFOUND = 0x80070002,
            NTE_BAD_KEY = 0x80090003,
            NTE_BAD_FLAGS = 0x80090009,
            NTE_BAD_DATA = 0x80090005,
            NTE_BAD_HASH = 0x80090003,
        }
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

        [Flags]
        internal enum CryptCreateHashFlags : int
        {
            None = 0,
        }

        internal enum CryptHashProperty : int
        {
            HP_ALGID = 0x0001,  // Hash algorithm
            HP_HASHVAL = 0x0002,  // Hash value
            HP_HASHSIZE = 0x0004,  // Hash value size
            HP_HMAC_INFO = 0x0005,  // information for creating an HMAC
            HP_TLS1PRF_LABEL = 0x0006,  // label for TLS1 PRF
            HP_TLS1PRF_SEED = 0x0007,  // seed for TLS1 PRF
        }

        internal enum KeySpec : int
        {
            AT_KEYEXCHANGE = 1,
            AT_SIGNATURE = 2,
        }

        [Flags]
        internal enum CryptSignAndVerifyHashFlags : int
        {
            None = 0x00000000,
            CRYPT_NOHASHOID = 0x00000001,
            CRYPT_TYPE2_FORMAT = 0x00000002,  // Not supported
            CRYPT_X931_FORMAT = 0x00000004,  // Not supported
        }
    } //End CapiHelper:Flags
} //End Namespace Internal.NativeCrypto
