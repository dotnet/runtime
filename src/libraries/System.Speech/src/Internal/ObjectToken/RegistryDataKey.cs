// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Speech.Internal.SapiInterop;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Speech.Internal.ObjectTokens
{
    [DebuggerDisplay("{Name}")]
    internal class RegistryDataKey : ISpDataKey, IEnumerable<RegistryDataKey>, IDisposable
    {
        #region Constructors

        protected RegistryDataKey(string fullPath, SafeRegistryHandle regHandle)
        {
            ISpRegDataKey regKey = (ISpRegDataKey)new SpDataKey();
            SAPIErrorCodes hresult = (SAPIErrorCodes)regKey.SetKey(regHandle, false);
            regHandle?.Close();
            if ((hresult != SAPIErrorCodes.S_OK) && (hresult != SAPIErrorCodes.SPERR_ALREADY_INITIALIZED))
            {
                throw new InvalidOperationException();
            }

            _sapiRegKey = regKey;
            _sKeyId = fullPath;
            _disposeSapiKey = true;
        }

        protected RegistryDataKey(string fullPath, RegistryKey managedRegKey) :
            this(fullPath, managedRegKey.Handle)
        {
        }

        protected RegistryDataKey(string fullPath, RegistryDataKey copyKey)
        {
            this._sKeyId = fullPath;
            this._sapiRegKey = copyKey._sapiRegKey;
            this._disposeSapiKey = copyKey._disposeSapiKey;
        }

        protected RegistryDataKey(string fullPath, ISpDataKey copyKey, bool shouldDispose)
        {
            this._sKeyId = fullPath;
            this._sapiRegKey = copyKey;
            this._disposeSapiKey = shouldDispose;
        }

        protected RegistryDataKey(ISpObjectToken sapiToken) :
            this(GetTokenIdFromToken(sapiToken), sapiToken, false)
        {
        }

        internal static RegistryDataKey Open(string registryPath, bool fCreateIfNotExist)
        {
            // Sanity check
            if (string.IsNullOrEmpty(registryPath))
            {
                return null;
            }

            // If the last character is a '\', get rid of it
            registryPath = registryPath.Trim(new char[] { '\\' });

            string rootPath = GetFirstKeyAndParseRemainder(ref registryPath);

            // Get the native registry handle and subkey path
            SafeRegistryHandle regHandle = RootHKEYFromRegPath(rootPath);

            // If there's no root, we can't do anything.
            if (regHandle == null || regHandle.IsInvalid)
            {
                return null;
            }

            RegistryDataKey rootKey = new(rootPath, regHandle);

            // If the path was only a root, we can directly return the key; otherwise,
            // we need to open a subkey and return that.
            if (string.IsNullOrEmpty(registryPath))
            {
                return rootKey;
            }
            else
            {
                RegistryDataKey subKey = OpenSubKey(rootKey, registryPath, fCreateIfNotExist);
                return subKey;
            }
        }

        internal static RegistryDataKey Create(string keyId, RegistryKey hkey)
        {
            return new RegistryDataKey(keyId, hkey);
        }

        private static RegistryDataKey OpenSubKey(RegistryDataKey baseKey, string registryPath, bool createIfNotExist)
        {
            if (string.IsNullOrEmpty(registryPath) || null == baseKey)
            {
                return null;
            }

            string nextKeyPath = GetFirstKeyAndParseRemainder(ref registryPath);

            RegistryDataKey nextKey = createIfNotExist ? baseKey.CreateKey(nextKeyPath) : baseKey.OpenKey(nextKeyPath);

            if (string.IsNullOrEmpty(registryPath))
            {
                return nextKey;
            }
            else
            {
                RegistryDataKey recursiveKey = OpenSubKey(nextKey, registryPath, createIfNotExist);
                return recursiveKey;
            }
        }

        private static string GetTokenIdFromToken(ISpObjectToken sapiToken)
        {
            IntPtr sapiTokenId = IntPtr.Zero;
            string tokenId;

            try
            {
                sapiToken.GetId(out sapiTokenId);
                tokenId = Marshal.PtrToStringUni(sapiTokenId);
            }
            finally
            {
                Marshal.FreeCoTaskMem(sapiTokenId);
            }

            return tokenId;
        }

        /// <summary>
        /// Needed by IEnumerable
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region internal Methods

        #region ISpDataKey Implementation

        // ISpDataKey Methods

        /// <summary>
        /// Writes the specified binary data to the registry.
        /// </summary>
        [PreserveSig]
        public int SetData(
            [MarshalAs(UnmanagedType.LPWStr)] string valueName,
            [MarshalAs(UnmanagedType.SysUInt)] uint cbData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] byte[] data)
        {
            return _sapiRegKey.SetData(valueName, cbData, data);
        }

        /// <summary>
        /// Reads the specified binary data from the registry.
        /// </summary>
        [PreserveSig]
        public int GetData(
            [MarshalAs(UnmanagedType.LPWStr)] string valueName,
            [MarshalAs(UnmanagedType.SysUInt)] ref uint pcbData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] data)
        {
            return _sapiRegKey.GetData(valueName, ref pcbData, data);
        }

        /// <summary>
        /// Writes the specified string value from the registry. If valueName
        /// is NULL then the default value of the registry key is read.
        /// </summary>
        [PreserveSig]
        public int SetStringValue(
            [MarshalAs(UnmanagedType.LPWStr)] string valueName,
            [MarshalAs(UnmanagedType.LPWStr)] string value)
        {
            return _sapiRegKey.SetStringValue(valueName, value);
        }

        /// <summary>
        /// Reads the specified string value to the registry. If valueName is
        /// NULL then the default value of the registry key is read.
        /// </summary>
        [PreserveSig]
        public int GetStringValue(
            [MarshalAs(UnmanagedType.LPWStr)] string valueName,
            [MarshalAs(UnmanagedType.LPWStr)] out string value)
        {
            return _sapiRegKey.GetStringValue(valueName, out value);
        }

        /// <summary>
        /// Writes the specified DWORD to the registry.
        /// </summary>
        [PreserveSig]
        public int SetDWORD(
            [MarshalAs(UnmanagedType.LPWStr)] string valueName,
            [MarshalAs(UnmanagedType.SysUInt)] uint value)
        {
            return _sapiRegKey.SetDWORD(valueName, value);
        }

        /// <summary>
        /// Reads the specified DWORD from the registry.
        /// </summary>
        [PreserveSig]
        public int GetDWORD([MarshalAs(UnmanagedType.LPWStr)] string valueName, ref uint pdwValue)
        {
            return _sapiRegKey.GetDWORD(valueName, ref pdwValue);
        }

        /// <summary>
        /// Opens a sub-key and returns a new object which supports SpDataKey
        /// for the specified sub-key.
        /// </summary>
        [PreserveSig]
        public int OpenKey([MarshalAs(UnmanagedType.LPWStr)] string subKeyName, out ISpDataKey ppSubKey)
        {
            return _sapiRegKey.OpenKey(subKeyName, out ppSubKey);
        }

        /// <summary>
        /// Creates a sub-key and returns a new object which supports SpDataKey
        /// for the specified sub-key.
        /// </summary>
        [PreserveSig]
        public int CreateKey([MarshalAs(UnmanagedType.LPWStr)] string subKeyName, out ISpDataKey ppSubKey)
        {
            return _sapiRegKey.CreateKey(subKeyName, out ppSubKey);
        }

        /// <summary>
        /// Deletes the specified key.
        /// </summary>
        [PreserveSig]
        public int DeleteKey([MarshalAs(UnmanagedType.LPWStr)] string subKeyName)
        {
            return _sapiRegKey.DeleteKey(subKeyName);
        }

        /// <summary>
        /// Deletes the specified value from the key.
        /// </summary>
        [PreserveSig]
        public int DeleteValue([MarshalAs(UnmanagedType.LPWStr)] string valueName)
        {
            return _sapiRegKey.DeleteValue(valueName);
        }

        /// <summary>
        /// Retrieve a key name by index
        /// </summary>
        [PreserveSig]
        public int EnumKeys(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string ppszSubKeyName)
        {
            return _sapiRegKey.EnumKeys(index, out ppszSubKeyName);
        }

        /// <summary>
        /// Retrieves a key value by index
        /// </summary>
        [PreserveSig]
        public int EnumValues(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string valueName)
        {
            return _sapiRegKey.EnumValues(index, out valueName);
        }

        #endregion

        /// <summary>
        /// Full path and name for the key
        /// </summary>
        internal string Id
        {
            get
            {
                return _sKeyId;
            }
        }

        /// <summary>
        /// Key Name (no path)
        /// </summary>
        internal string Name
        {
            get
            {
                int iPosSlash = _sKeyId.LastIndexOf('\\');
                return _sKeyId.Substring(iPosSlash + 1);
            }
        }

        // Disable parameter validation check

        /// <summary>
        /// Reads the specified string value to the registry. If valueName is
        /// NULL then the default value of the registry key is read.
        /// </summary>
        internal bool TryGetString(string valueName, out string value)
        {
            if (null == valueName)
            {
                valueName = string.Empty;
            }

            return 0 == GetStringValue(valueName, out value);
        }

        /// <summary>
        /// Opens a sub-key and returns a new object which supports SpDataKey
        /// for the specified sub-key.
        /// </summary>
        internal bool HasValue(string valueName)
        {
            string unusedString;
            uint unusedUint = 0;
            byte[] unusedBytes = Array.Empty<byte>();

            return (
                0 == _sapiRegKey.GetStringValue(valueName, out unusedString) ||
                0 == _sapiRegKey.GetDWORD(valueName, ref unusedUint) ||
                0 == _sapiRegKey.GetData(valueName, ref unusedUint, unusedBytes));
        }

        /// <summary>
        /// Reads the specified DWORD from the registry.
        /// </summary>
        internal bool TryGetDWORD(string valueName, ref uint value)
        {
            if (string.IsNullOrEmpty(valueName))
            {
                return false;
            }

            return 0 == _sapiRegKey.GetDWORD(valueName, ref value);
        }

        /// <summary>
        /// Opens a sub-key and returns a new object which supports SpDataKey
        /// for the specified sub-key.
        /// </summary>
        internal RegistryDataKey OpenKey(string keyName)
        {
            Helpers.ThrowIfEmptyOrNull(keyName, nameof(keyName));

            ISpDataKey sapiSubKey;
            if (0 != _sapiRegKey.OpenKey(keyName, out sapiSubKey))
            {
                return null;
            }
            else
            {
                return new RegistryDataKey(_sKeyId + @"\" + keyName, sapiSubKey, true);
            }
        }

        /// <summary>
        /// Creates a sub-key and returns a new object which supports SpDataKey
        /// for the specified sub-key.
        /// </summary>
        internal RegistryDataKey CreateKey(string keyName)
        {
            Helpers.ThrowIfEmptyOrNull(keyName, nameof(keyName));

            ISpDataKey sapiSubKey;

            if (0 != _sapiRegKey.CreateKey(keyName, out sapiSubKey))
            {
                return null;
            }
            else
            {
                return new RegistryDataKey(_sKeyId + @"\" + keyName, sapiSubKey, true);
            }
        }

        /// <summary>
        /// returns the name for all the values in this registry entry
        /// </summary>
        internal string[] GetValueNames()
        {
            List<string> valueNames = new();

            string valueName;

            for (uint i = 0; 0 == _sapiRegKey.EnumValues(i, out valueName); i++)
            {
                valueNames.Add(valueName);
            }

            return valueNames.ToArray();
        }

        #region IEnumerable implementation

        IEnumerator<RegistryDataKey> IEnumerable<RegistryDataKey>.GetEnumerator()
        {
            string childKeyName = string.Empty;

            for (uint i = 0; 0 == _sapiRegKey.EnumKeys(i, out childKeyName); i++)
            {
                yield return this.CreateKey(childKeyName);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<RegistryDataKey>)this).GetEnumerator();
        }

        #endregion

        #endregion

        #region Protected Methods
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _sapiRegKey != null && _disposeSapiKey)
            {
                Marshal.ReleaseComObject(_sapiRegKey);
                _sapiRegKey = null;
            }
        }

        #endregion

        #region Internal Fields

        internal string _sKeyId;

        internal ISpDataKey _sapiRegKey;

        internal bool _disposeSapiKey;

        #endregion

        #region Private Methods

        private static SafeRegistryHandle RootHKEYFromRegPath(string rootPath)
        {
            RegistryKey rootKey = RegKeyFromRootPath(rootPath);

            if (null == rootKey)
            {
                return null;
            }

            return rootKey.Handle;
        }

        private static string GetFirstKeyAndParseRemainder(ref string registryPath)
        {
            int index = registryPath.IndexOf('\\');

            string firstKey;

            if (index >= 0)
            {
                firstKey = registryPath.Substring(0, index);
                registryPath = registryPath.Substring(index + 1);
            }
            else
            {
                firstKey = registryPath;
                registryPath = string.Empty;
            }

            return firstKey;
        }

        private static RegistryKey RegKeyFromRootPath(string rootPath)
        {
            RegistryKey[] roots = new RegistryKey[] {
                Registry.ClassesRoot,
                Registry.LocalMachine,
                Registry.CurrentUser,
                Registry.CurrentConfig
            };

            foreach (RegistryKey key in roots)
            {
                if (key.Name.Equals(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }

            return null;
        }

        #endregion

        #region private Types

        internal enum SAPIErrorCodes
        {
            STG_E_FILENOTFOUND = -2147287038,  // 0x80030002
            SPERR_ALREADY_INITIALIZED = -2147201022, // 0x80045002
            SPERR_UNSUPPORTED_FORMAT = -2147201021,  // 0x80045003
            SPERR_DEVICE_BUSY = -2147201018,  // 0x80045006
            SPERR_DEVICE_NOT_SUPPORTED = -2147201017,  // 0x80045007
            SPERR_DEVICE_NOT_ENABLED = -2147201016,  // 0x80045008
            SPERR_NO_DRIVER = -2147201015,  // 0x80045009
            SPERR_TOO_MANY_GRAMMARS = -2147200990,  // 0x80045022
            SPERR_INVALID_IMPORT = -2147200988,  // 0x80045024
            SPERR_AUDIO_BUFFER_OVERFLOW = -2147200977,  // 0x8004502F
            SPERR_NO_AUDIO_DATA = -2147200976,  // 0x80045030
            SPERR_NO_MORE_ITEMS = -2147200967,  // 0x80045039
            SPERR_NOT_FOUND = -2147200966,  // 0x8004503A
            SPERR_GENERIC_MMSYS_ERROR = -2147200964,  // 0x8004503C
            SPERR_NOT_TOPLEVEL_RULE = -2147200940,  // 0x80045054
            SPERR_NOT_ACTIVE_SESSION = -2147200925,  // 0x80045063
            SPERR_SML_GENERATION_FAIL = -2147200921,  // 0x80045067
            SPERR_SHARED_ENGINE_DISABLED = -2147200906,  // 0x80045076
            SPERR_RECOGNIZER_NOT_FOUND = -2147200905,  // 0x80045077
            SPERR_AUDIO_NOT_FOUND = -2147200904,  // 0x80045078
            S_OK = 0,            // 0x00000000
            S_FALSE = 1,            // 0x00000001
            E_INVALIDARG = -2147024809, // 0x80070057
            SP_NO_RULES_TO_ACTIVATE = 282747,       // 0x0004507B
            ERROR_MORE_DATA = 0x50EA,
        }

        #endregion
    }
}
