// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    using System;
    using System.Security;
    using System.Globalization;
    using System.Text;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // This is the enumeration for Normalization Forms
[System.Runtime.InteropServices.ComVisible(true)]
    public enum NormalizationForm
    {
#if !FEATURE_NORM_IDNA_ONLY     
        FormC    = 1,
        FormD    = 2,
        FormKC   = 5,
        FormKD   = 6
#endif // !FEATURE_NORM_IDNA_ONLY       
    }

    internal enum ExtendedNormalizationForms
    {
#if !FEATURE_NORM_IDNA_ONLY     
        FormC    = 1,
        FormD    = 2,
        FormKC   = 5,
        FormKD   = 6,
#endif // !FEATURE_NORM_IDNA_ONLY        
        FormIdna = 0xd,
#if !FEATURE_NORM_IDNA_ONLY
        FormCDisallowUnassigned     = 0x101,
        FormDDisallowUnassigned     = 0x102,
        FormKCDisallowUnassigned    = 0x105,
        FormKDDisallowUnassigned    = 0x106,
#endif // !FEATURE_NORM_IDNA_ONLY        
        FormIdnaDisallowUnassigned  = 0x10d
    }

    // This internal class wraps up our normalization behavior

    internal class Normalization
    {
        //
        // Flags that track whether given normalization form was initialized
        //
#if !FEATURE_NORM_IDNA_ONLY
        private static volatile bool NFC;
        private static volatile bool NFD;
        private static volatile bool NFKC;
        private static volatile bool NFKD;
#endif // !FEATURE_NORM_IDNA_ONLY                
        private static volatile bool IDNA;
#if !FEATURE_NORM_IDNA_ONLY        
        private static volatile bool NFCDisallowUnassigned;
        private static volatile bool NFDDisallowUnassigned;
        private static volatile bool NFKCDisallowUnassigned;
        private static volatile bool NFKDDisallowUnassigned;
#endif // !FEATURE_NORM_IDNA_ONLY    
        private static volatile bool IDNADisallowUnassigned;
        private static volatile bool Other;

        // These are error codes we get back from the Normalization DLL
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_NOT_ENOUGH_MEMORY = 8;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_NO_UNICODE_TRANSLATION = 1113;

        [System.Security.SecurityCritical]  // auto-generated
        static private unsafe void InitializeForm(NormalizationForm form, String strDataFile)
        {
#if FEATURE_COREFX_GLOBALIZATION
            //TODO: Implement this fully.  We might need a PAL here.
            throw new NotImplementedException();
#else
            byte* pTables = null;

            // Normalization uses OS on Win8
            if (!Environment.IsWindows8OrAbove)
            {
                if (strDataFile == null)
                {
                    // They were supposed to have a form that we know about!
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_InvalidNormalizationForm"));
                }

                // Tell the DLL where to find our data
                pTables = GlobalizationAssembly.GetGlobalizationResourceBytePtr(
                   typeof(Normalization).Assembly, strDataFile);
                if (pTables == null)
                {
                    // Unable to load the specified normalizationForm,
                    // tables not loaded from file
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_InvalidNormalizationForm"));
                }
            }

            nativeNormalizationInitNormalization(form, pTables);
#endif
        }

        [System.Security.SecurityCritical]  // auto-generated
        static private void EnsureInitialized(NormalizationForm form)
        {
            switch ((ExtendedNormalizationForms)form)
            {
#if !FEATURE_NORM_IDNA_ONLY
                case ExtendedNormalizationForms.FormC:
                    if (NFC) return;
                    InitializeForm(form, "normnfc.nlp");
                    NFC = true;
                    break;

                case ExtendedNormalizationForms.FormD:
                    if (NFD) return;
                    InitializeForm(form, "normnfd.nlp");
                    NFD = true;
                    break;

                case ExtendedNormalizationForms.FormKC:
                    if (NFKC) return;
                    InitializeForm(form, "normnfkc.nlp");
                    NFKC = true;
                    break;

                case ExtendedNormalizationForms.FormKD:
                    if (NFKD) return;
                    InitializeForm(form, "normnfkd.nlp");
                    NFKD = true;
                    break;
#endif // !FEATURE_NORM_IDNA_ONLY

                case ExtendedNormalizationForms.FormIdna:
                    if (IDNA) return;
                    InitializeForm(form, "normidna.nlp");
                    IDNA = true;
                    break;

#if !FEATURE_NORM_IDNA_ONLY
                case ExtendedNormalizationForms.FormCDisallowUnassigned:
                    if (NFCDisallowUnassigned) return;
                    InitializeForm(form, "normnfc.nlp");
                    NFCDisallowUnassigned = true;
                    break;

                case ExtendedNormalizationForms.FormDDisallowUnassigned:
                    if (NFDDisallowUnassigned) return;
                    InitializeForm(form, "normnfd.nlp");
                    NFDDisallowUnassigned = true;
                    break;

                case ExtendedNormalizationForms.FormKCDisallowUnassigned:
                    if (NFKCDisallowUnassigned) return;
                    InitializeForm(form, "normnfkc.nlp");
                    NFKCDisallowUnassigned = true;
                    break;

                case ExtendedNormalizationForms.FormKDDisallowUnassigned:
                    if (NFKDDisallowUnassigned) return;
                    InitializeForm(form, "normnfkd.nlp");
                    NFKDDisallowUnassigned = true;
                    break;
#endif // !FEATURE_NORM_IDNA_ONLY

                case ExtendedNormalizationForms.FormIdnaDisallowUnassigned:
                    if (IDNADisallowUnassigned) return;
                    InitializeForm(form, "normidna.nlp");
                    IDNADisallowUnassigned = true;
                    break;

                default:
                    if (Other) return;
                    InitializeForm(form, null);
                    Other = true;
                    break;
            }
        }

        [System.Security.SecurityCritical]
        internal static bool IsNormalized(String strInput, NormalizationForm normForm)
        {
            Contract.Requires(strInput != null);

            EnsureInitialized(normForm);

            int iError = ERROR_SUCCESS;
            bool result = nativeNormalizationIsNormalizedString(
                                normForm, 
                                ref iError, 
                                strInput, 
                                strInput.Length);

            switch(iError)
            {
                // Success doesn't need to do anything
                case ERROR_SUCCESS:
                    break;

                // Do appropriate stuff for the individual errors:
                case ERROR_INVALID_PARAMETER:
                case ERROR_NO_UNICODE_TRANSLATION:
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex" ),
                        "strInput");
                case ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException(
                        Environment.GetResourceString("Arg_OutOfMemoryException"));
                default:
                    throw new InvalidOperationException(
                        Environment.GetResourceString("UnknownError_Num", iError));
            }

            return result;
        }

        [System.Security.SecurityCritical]
        internal static String Normalize(String strInput, NormalizationForm normForm)
        {
            Contract.Requires(strInput != null);

            EnsureInitialized(normForm);

            int iError = ERROR_SUCCESS;

            // Guess our buffer size first
            int iLength = nativeNormalizationNormalizeString(normForm, ref iError, strInput, strInput.Length, null, 0);

            // Could have an error (actually it'd be quite hard to have an error here)
            if (iError != ERROR_SUCCESS)
            {
                if (iError == ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(
                        Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex" ),
                        "strInput");

                // We shouldn't really be able to get here..., guessing length is
                // a trivial math function...
                // Can't really be Out of Memory, but just in case:
                if (iError == ERROR_NOT_ENOUGH_MEMORY)
                    throw new OutOfMemoryException(
                        Environment.GetResourceString("Arg_OutOfMemoryException"));

                // Who knows what happened?  Not us!
                throw new InvalidOperationException(
                    Environment.GetResourceString("UnknownError_Num", iError));
            }

            // Don't break for empty strings (only possible for D & KD and not really possible at that)
            if (iLength == 0) return String.Empty;

            // Someplace to stick our buffer
            char[] cBuffer = null;

            for (;;)
            {
                // (re)allocation buffer and normalize string
                cBuffer = new char[iLength];

                iLength = nativeNormalizationNormalizeString(
                                    normForm, 
                                    ref iError,
                                    strInput, 
                                    strInput.Length, 
                                    cBuffer, 
                                    cBuffer.Length);
                
                if (iError == ERROR_SUCCESS)
                    break;

                // Could have an error (actually it'd be quite hard to have an error here)
                switch(iError)
                {
                    // Do appropriate stuff for the individual errors:
                    case ERROR_INSUFFICIENT_BUFFER:
                        Contract.Assert(iLength > cBuffer.Length, "Buffer overflow should have iLength > cBuffer.Length");
                        continue;

                    case ERROR_INVALID_PARAMETER:
                    case ERROR_NO_UNICODE_TRANSLATION:
                        // Illegal code point or order found.  Ie: FFFE or D800 D800, etc.
                        throw new ArgumentException(
                            Environment.GetResourceString("Argument_InvalidCharSequence", iLength ),
                            "strInput");
                    case ERROR_NOT_ENOUGH_MEMORY:
                        throw new OutOfMemoryException(
                            Environment.GetResourceString("Arg_OutOfMemoryException"));

                    default:
                        // We shouldn't get here...
                        throw new InvalidOperationException(
                            Environment.GetResourceString("UnknownError_Num", iError));
                }
            }

            // Copy our buffer into our new string, which will be the appropriate size
            return new String(cBuffer, 0, iLength);
        }

#if !FEATURE_CORECLR
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe private static extern int nativeNormalizationNormalizeString(
            NormalizationForm normForm, ref int iError,
            String lpSrcString, int cwSrcLength,
            char[] lpDstString, int cwDstLength);

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        unsafe private static extern bool nativeNormalizationIsNormalizedString(
            NormalizationForm normForm, ref int iError,
            String lpString, int cwLength);

        [System.Security.SecurityCritical]  // auto-generated
        [SuppressUnmanagedCodeSecurity]
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        unsafe private static extern void nativeNormalizationInitNormalization(
            NormalizationForm normForm, byte* pTableData);
#else
        unsafe private static int nativeNormalizationNormalizeString(
            NormalizationForm normForm, ref int iError,
            String lpSrcString, int cwSrcLength,
            char[] lpDstString, int cwDstLength)
        {
            throw new NotImplementedException();
        }

        unsafe private static bool nativeNormalizationIsNormalizedString(
            NormalizationForm normForm, ref int iError,
            String lpString, int cwLength)
        {
            throw new NotImplementedException();
        }

        unsafe private static void nativeNormalizationInitNormalization(
            NormalizationForm normForm, byte* pTableData)
        {
            throw new NotImplementedException();
        }
#endif
    }
}
