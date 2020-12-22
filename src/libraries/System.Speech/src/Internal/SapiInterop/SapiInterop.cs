// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Speech.Internal.SapiInterop
{
    #region Enum

    // See sperror.h
    internal enum SAPIErrorCodes
    {
        S_OK = 0,            // 0x00000000
        S_FALSE = 1,            // 0x00000001
        SP_NO_RULE_ACTIVE = 0x00045055,
        SP_NO_RULES_TO_ACTIVATE = 282747,       // 0x0004507B

        S_LIMIT_REACHED = 0x0004507F,

        E_FAIL = -2147467259, //  0x80004005
        SP_NO_PARSE_FOUND = 0x0004502c,
        SP_WORD_EXISTS_WITHOUT_PRONUNCIATION = 0x00045037,  // 282679

        SPERR_FIRST = -2147201023,  // 0x80045001
        SPERR_LAST = -2147200890,   // 0x80045086

        STG_E_FILENOTFOUND = -2147287038,  // 0x80030002
        CLASS_E_CLASSNOTAVAILABLE = -2147221231, // 0x80040111
        REGDB_E_CLASSNOTREG = -2147221164, // 0x80040154
        SPERR_UNSUPPORTED_FORMAT = -2147201021,  // 0x80045003
        SPERR_UNSUPPORTED_PHONEME = -2147200902,  // 0x8004507A
        SPERR_VOICE_NOT_FOUND = -2147200877,  // 0x80045093
        SPERR_NOT_IN_LEX = -2147200999,  // 0x80045019
        SPERR_TOO_MANY_GRAMMARS = -2147200990,  // 0x80045022
        SPERR_INVALID_IMPORT = -2147200988,  // 0x80045024
        SPERR_STREAM_CLOSED = -2147200968,  // 0x80045038
        SPERR_NO_MORE_ITEMS = -2147200967,  // 0x80045039
        SPERR_NOT_FOUND = -2147200966,  // 0x8004503A
        SPERR_NOT_TOPLEVEL_RULE = -2147200940,  // 0x80045054
        SPERR_SHARED_ENGINE_DISABLED = -2147200906,  // 0x80045076
        SPERR_RECOGNIZER_NOT_FOUND = -2147200905,  // 0x80045077
        SPERR_AUDIO_NOT_FOUND = -2147200904,  // 0x80045078
        SPERR_NOT_SUPPORTED_FOR_INPROC_RECOGNIZER = -2147200893,  //  0x80045083
        SPERR_LEX_INVALID_DATA = -2147200891,        // 0x80045085
        SPERR_CFG_INVALID_DATA = -2147200890         // 0x80045086
    }

    #endregion Enum

    #region SAPI constants

    internal static class SapiConstants
    {
        internal const string SPPROP_RESPONSE_SPEED = "ResponseSpeed";
        internal const string SPPROP_COMPLEX_RESPONSE_SPEED = "ComplexResponseSpeed";
        internal const string SPPROP_CFG_CONFIDENCE_REJECTION_THRESHOLD = "CFGConfidenceRejectionThreshold";

        internal const uint SPDF_ALL = 0xff;

        // Throws exception if the specified Rule does not have a valid Id.
        internal static SRID SapiErrorCode2SRID(SAPIErrorCodes code)
        {
            if (code >= SAPIErrorCodes.SPERR_FIRST && code <= SAPIErrorCodes.SPERR_LAST)
            {
                return (SRID)((int)SRID.SapiErrorUninitialized + (code - SAPIErrorCodes.SPERR_FIRST));
            }
            else
            {
                switch (code)
                {
                    case SAPIErrorCodes.SP_NO_RULE_ACTIVE:
                        return SRID.SapiErrorNoRuleActive;

                    case SAPIErrorCodes.SP_NO_RULES_TO_ACTIVATE:
                        return SRID.SapiErrorNoRulesToActivate;

                    case SAPIErrorCodes.SP_NO_PARSE_FOUND:
                        return SRID.NoParseFound;

                    case SAPIErrorCodes.S_FALSE:
                        return SRID.UnexpectedError;

                    default:
                        return (SRID)unchecked(-1);
                }
            }
        }
    }

    #endregion

    #region Interface

    [ComImport, Guid("14056589-E16C-11D2-BB90-00C04F8EE6C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpObjectToken : ISpDataKey
    {
        // ISpDataKey Methods
        [PreserveSig]
        new int SetData([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, uint cbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] pData);
        [PreserveSig]
        new int GetData([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, ref uint pcbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] pData);
        [PreserveSig]
        new int SetStringValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] string pszValue);
        [PreserveSig]
        new int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValue);
        [PreserveSig]
        new int SetDWORD([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, uint dwValue);
        [PreserveSig]
        new int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, ref uint pdwValue);
        [PreserveSig]
        new int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKeyName, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKey, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKey);
        [PreserveSig]
        new int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName);
        [PreserveSig]
        new int EnumKeys(uint Index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName);
        [PreserveSig]
        new int EnumValues(uint Index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValueName);

        // ISpObjectToken Methods
        void SetId([MarshalAs(UnmanagedType.LPWStr)] string pszCategoryId, [MarshalAs(UnmanagedType.LPWStr)] string pszTokenId, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist);
        void GetId(out IntPtr ppszCoMemTokenId);
        void Slot15(); // void GetCategory(out ISpObjectTokenCategory ppTokenCategory);
        void Slot16(); // void CreateInstance(object pUnkOuter, UInt32 dwClsContext, ref Guid riid, ref IntPtr ppvObject);
        void Slot17(); // void GetStorageFileName(ref Guid clsidCaller, [MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] string pszFileNameSpecifier, UInt32 nFolder, [MarshalAs(UnmanagedType.LPWStr)] out string ppszFilePath);
        void Slot18(); // void RemoveStorageFileName(ref Guid clsidCaller, [MarshalAs(UnmanagedType.LPWStr)] string pszKeyName, int fDeleteFile);
        void Slot19(); // void Remove(ref Guid pclsidCaller);
        void Slot20(); // void IsUISupported([MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData, object punkObject, ref Int32 pfSupported);
        void Slot21(); // void DisplayUI(UInt32 hWndParent, [MarshalAs(UnmanagedType.LPWStr)] string pszTitle, [MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData, object punkObject);
        void MatchesAttributes([MarshalAs(UnmanagedType.LPWStr)] string pszAttributes, [MarshalAs(UnmanagedType.Bool)] out bool pfMatches);
    }

    //--- ISpObjectWithToken ----------------------------------------------------
    [ComImport, Guid("5B559F40-E952-11D2-BB91-00C04F8EE6C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpObjectWithToken
    {
        [PreserveSig]
        int SetObjectToken(ISpObjectToken pToken);
        [PreserveSig]
        int GetObjectToken(out ISpObjectToken ppToken);
    };

    [ComImport, Guid("14056581-E16C-11D2-BB90-00C04F8EE6C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpDataKey
    {
        // ISpDataKey Methods
        [PreserveSig]
        int SetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint cbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
        [PreserveSig]
        int GetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pcbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] data);
        [PreserveSig]
        int SetStringValue([MarshalAs(UnmanagedType.LPWStr)] string valueName, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig]
        int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string valueName, [MarshalAs(UnmanagedType.LPWStr)] out string value);
        [PreserveSig]
        int SetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint dwValue);
        [PreserveSig]
        int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pdwValue);
        [PreserveSig]
        int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string subKeyName, out ISpDataKey ppSubKey);
        [PreserveSig]
        int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string subKey, out ISpDataKey ppSubKey);
        [PreserveSig]
        int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string subKey);
        [PreserveSig]
        int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string valueName);
        [PreserveSig]
        int EnumKeys(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName);
        [PreserveSig]
        int EnumValues(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string valueName);
    }

    [ComImport, Guid("92A66E2B-C830-4149-83DF-6FC2BA1E7A5B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRegDataKey : ISpDataKey
    {
        // ISpDataKey Methods
        [PreserveSig]
        new int SetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint cbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
        [PreserveSig]
        new int GetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pcbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] data);
        [PreserveSig]
        new int SetStringValue([MarshalAs(UnmanagedType.LPWStr)] string valueName, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig]
        new int GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValue);
        [PreserveSig]
        new int SetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint dwValue);
        [PreserveSig]
        new int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, ref uint pdwValue);
        [PreserveSig]
        new int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKeyName, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string subKey, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string subKey);
        [PreserveSig]
        new int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string valueName);
        [PreserveSig]
        new int EnumKeys(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName);
        [PreserveSig]
        new int EnumValues(uint Index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValueName);

        // ISpRegDataKey Method
        [PreserveSig]
        int SetKey(SafeRegistryHandle hkey, bool fReadOnly);
    }

    [ComImport, Guid("2D3D3845-39AF-4850-BBF9-40B49780011D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpObjectTokenCategory : ISpDataKey
    {
        // ISpDataKey Methods
        [PreserveSig]
        new int SetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint cbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data);
        [PreserveSig]
        new int GetData([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pcbData, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] data);
        [PreserveSig]
        new int SetStringValue([MarshalAs(UnmanagedType.LPWStr)] string valueName, [MarshalAs(UnmanagedType.LPWStr)] string value);
        [PreserveSig]
        new void GetStringValue([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValue);
        [PreserveSig]
        new int SetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, uint dwValue);
        [PreserveSig]
        new int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string pszValueName, ref uint pdwValue);
        [PreserveSig]
        new int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string pszSubKeyName, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string subKey, out ISpDataKey ppSubKey);
        [PreserveSig]
        new int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string subKey);
        [PreserveSig]
        new int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string valueName);
        [PreserveSig]
        new int EnumKeys(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName);
        [PreserveSig]
        new int EnumValues(uint Index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszValueName);

        // ISpObjectTokenCategory Methods
        void SetId([MarshalAs(UnmanagedType.LPWStr)] string pszCategoryId, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist);
        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemCategoryId);
        void Slot14(); // void GetDataKey(System.Speech.Internal.SPDATAKEYLOCATION spdkl, out ISpDataKey ppDataKey);
        void EnumTokens([MarshalAs(UnmanagedType.LPWStr)] string pzsReqAttribs, [MarshalAs(UnmanagedType.LPWStr)] string pszOptAttribs, out IEnumSpObjectTokens ppEnum);
        void Slot16(); // void SetDefaultTokenId([MarshalAs(UnmanagedType.LPWStr)] string pszTokenId);
        void GetDefaultTokenId([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemTokenId);
    }

    [ComImport, Guid("06B64F9E-7FDA-11D2-B4F2-00C04F797396"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IEnumSpObjectTokens
    {
        void Slot1(); // void Next(UInt32 celt, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] ISpObjectToken[] pelt, out UInt32 pceltFetched);
        void Slot2(); // void Skip(UInt32 celt);
        void Slot3(); // void Reset();
        void Slot4(); // void Clone(out IEnumSpObjectTokens ppEnum);
        void Item(uint Index, out ISpObjectToken ppToken);
        void GetCount(out uint pCount);
    }

    [ComImport, Guid("B2745EFD-42CE-48CA-81F1-A96E02538A90"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhoneticAlphabetSelection
    {
        void IsAlphabetUPS([MarshalAs(UnmanagedType.Bool)] out bool pfIsUPS);
        void SetAlphabetToUPS([MarshalAs(UnmanagedType.Bool)] bool fForceUPS);
    }

    [ComImport, Guid("EF411752-3736-4CB4-9C8C-8EF4CCB58EFE")]
    internal class SpObjectToken { }

    [ComImport, Guid("A910187F-0C7A-45AC-92CC-59EDAFB77B53")]
    internal class SpObjectTokenCategory { }

    [ComImport, Guid("D9F6EE60-58C9-458B-88E1-2F908FD7F87C")]
    internal class SpDataKey { }

    #endregion

    #region Utility Class

    internal static class SAPIGuids
    {
        internal static readonly Guid SPDFID_WaveFormatEx = new("C31ADBAE-527F-4ff5-A230-F62BB61FF70C");
        internal static readonly Guid SPDFID_Text = new("7CEEF9F9-3D13-11d2-9EE7-00C04F797396");
    }

    #endregion
}
