// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;

using Internal.Win32.SafeHandles;

//
// A minimal version of RegistryKey that supports just what CoreLib needs. 
//
// Internal.Win32 namespace avoids confusion with the public standalone Microsoft.Win32.Registry implementation 
// that lives in corefx.
//
namespace Internal.Win32
{
    internal sealed class RegistryKey : IDisposable
    {
        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  16,383 Unicode characters
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;

        private SafeRegistryHandle _hkey;

        private RegistryKey(SafeRegistryHandle hkey)
        {
            _hkey = hkey;
        }

        void IDisposable.Dispose()
        {
            if (_hkey != null)
            {
                _hkey.Dispose();
            }
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            int errorCode = Interop.Advapi32.RegDeleteValue(_hkey, name);

            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE  
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ||
                errorCode == Interop.Errors.ERROR_FILENAME_EXCED_RANGE)
            {
                if (throwOnMissingValue)
                {
                    throw new ArgumentException(SR.Arg_RegSubKeyValueAbsent);
                }
                else
                {
                    // Otherwise, reset and just return giving no indication to the user.
                    // (For compatibility)
                    errorCode = 0;
                }
            }
            // We really should throw an exception here if errorCode was bad,
            // but we can't for compatibility reasons.
            Debug.Assert(errorCode == 0, "RegDeleteValue failed.  Here's your error code: " + errorCode);
        }

        internal static RegistryKey OpenBaseKey(IntPtr hKey)
        {
            return new RegistryKey(new SafeRegistryHandle(hKey, false));
        }

        public RegistryKey? OpenSubKey(string name)
        {
            return OpenSubKey(name, false);
        }

        public RegistryKey? OpenSubKey(string name, bool writable)
        {
            // Make sure that the name does not contain double slahes
            Debug.Assert(name.IndexOf("\\\\") == -1);

            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey,
                name,
                0,
                writable ? 
                    Interop.Advapi32.RegistryOperations.KEY_READ | Interop.Advapi32.RegistryOperations.KEY_WRITE :
                    Interop.Advapi32.RegistryOperations.KEY_READ,
                out SafeRegistryHandle result);

            if (ret == 0 && !result.IsInvalid)
            {
                return new RegistryKey(result);
            }

            // Return null if we didn't find the key.
            if (ret == Interop.Errors.ERROR_ACCESS_DENIED || ret == Interop.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(SR.Security_RegistryPermission);
            }

            return null;
        }

        public string[] GetSubKeyNames()
        {
            var names = new List<string>();
            char[] name = ArrayPool<char>.Shared.Rent(MaxKeyLength + 1);

            try
            {
                int result;
                int nameLength = name.Length;

                while ((result = Interop.Advapi32.RegEnumKeyEx(
                    _hkey,
                    names.Count,
                    name,
                    ref nameLength,
                    null,
                    null,
                    null,
                    null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
                {
                    switch (result)
                    {
                        case Interop.Errors.ERROR_SUCCESS:
                            names.Add(new string(name, 0, nameLength));
                            nameLength = name.Length;
                            break;
                        default:
                            // Throw the error
                            Win32Error(result, null);
                            break;
                    }
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(name);
            }

            return names.ToArray();
        }

        public unsafe string[] GetValueNames()
        {
            var names = new List<string>();

            // Names in the registry aren't usually very long, although they can go to as large
            // as 16383 characters (MaxValueLength).
            //
            // Every call to RegEnumValue will allocate another buffer to get the data from
            // NtEnumerateValueKey before copying it back out to our passed in buffer. This can
            // add up quickly- we'll try to keep the memory pressure low and grow the buffer
            // only if needed.

            char[]? name = ArrayPool<char>.Shared.Rent(100);

            try
            {
                int result;
                int nameLength = name.Length;

                while ((result = Interop.Advapi32.RegEnumValue(
                    _hkey,
                    names.Count,
                    name,
                    ref nameLength,
                    IntPtr.Zero,
                    null,
                    null,
                    null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
                {
                    switch (result)
                    {
                        // The size is only ever reported back correctly in the case
                        // of ERROR_SUCCESS. It will almost always be changed, however.
                        case Interop.Errors.ERROR_SUCCESS:
                            names.Add(new string(name, 0, nameLength));
                            break;
                        case Interop.Errors.ERROR_MORE_DATA:
                            {
                                char[] oldName = name;
                                int oldLength = oldName.Length;
                                name = null;
                                ArrayPool<char>.Shared.Return(oldName);
                                name = ArrayPool<char>.Shared.Rent(checked(oldLength * 2));
                            }
                            break;
                        default:
                            // Throw the error
                            Win32Error(result, null);
                            break;
                    }

                    // Always set the name length back to the buffer size
                    nameLength = name.Length;
                }
            }
            finally
            {
                if (name != null)
                    ArrayPool<char>.Shared.Return(name);
            }

            return names.ToArray();
        }

        public object? GetValue(string name)
        {
            return GetValue(name, null);
        }

        public object? GetValue(string name, object? defaultValue) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        {
            object? data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, (byte[]?)null, ref datasize);

            if (ret != 0)
            {
                // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                // want to continue on through the function. 
                if (ret != Interop.Errors.ERROR_MORE_DATA)
                    return data;
            }

            if (datasize < 0)
            {
                // unexpected code path
                Debug.Fail("[InternalGetValue] RegQueryValue returned ERROR_SUCCESS but gave a negative datasize");
                datasize = 0;
            }


            switch (type)
            {
                case Interop.Advapi32.RegistryValues.REG_NONE:
                case Interop.Advapi32.RegistryValues.REG_DWORD_BIG_ENDIAN:
                case Interop.Advapi32.RegistryValues.REG_BINARY:
                    {
                        byte[] blob = new byte[datasize];
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        data = blob;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_QWORD:
                    {    // also REG_QWORD_LITTLE_ENDIAN
                        if (datasize > 8)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(long)
                            goto case Interop.Advapi32.RegistryValues.REG_BINARY;
                        }
                        long blob = 0;
                        Debug.Assert(datasize == 8, "datasize==8");
                        // Here, datasize must be 8 when calling this
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_DWORD:
                    {    // also REG_DWORD_LITTLE_ENDIAN
                        if (datasize > 4)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(int)
                            goto case Interop.Advapi32.RegistryValues.REG_QWORD;
                        }
                        int blob = 0;
                        Debug.Assert(datasize == 4, "datasize==4");
                        // Here, datasize must be four when calling this
                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;

                case Interop.Advapi32.RegistryValues.REG_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);
                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new string(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new string(blob);
                        }
                    }
                    break;

                case Interop.Advapi32.RegistryValues.REG_EXPAND_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);

                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new string(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new string(blob);
                        }

                        data = Environment.ExpandEnvironmentVariables((string)data);
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_MULTI_SZ:
                    {
                        if (datasize % 2 == 1)
                        {
                            // handle the case where the registry contains an odd-byte length (corrupt data?)
                            try
                            {
                                datasize = checked(datasize + 1);
                            }
                            catch (OverflowException e)
                            {
                                throw new IOException(SR.Arg_RegGetOverflowBug, e);
                            }
                        }
                        char[] blob = new char[datasize / 2];

                        ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref datasize);

                        // make sure the string is null terminated before processing the data
                        if (blob.Length > 0 && blob[blob.Length - 1] != (char)0)
                        {
                            Array.Resize(ref blob!, blob.Length + 1); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                        }

                        string[] strings = Array.Empty<string>();
                        int stringsCount = 0;

                        int cur = 0;
                        int len = blob.Length;

                        while (ret == 0 && cur < len)
                        {
                            int nextNull = cur;
                            while (nextNull < len && blob[nextNull] != (char)0)
                            {
                                nextNull++;
                            }

                            string? toAdd = null;
                            if (nextNull < len)
                            {
                                Debug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                if (nextNull - cur > 0)
                                {
                                    toAdd = new string(blob, cur, nextNull - cur);
                                }
                                else
                                {
                                    // we found an empty string.  But if we're at the end of the data,
                                    // it's just the extra null terminator.
                                    if (nextNull != len - 1)
                                    {
                                        toAdd = string.Empty;
                                    }
                                }
                            }
                            else
                            {
                                toAdd = new string(blob, cur, len - cur);
                            }
                            cur = nextNull + 1;

                            if (toAdd != null)
                            {
                                if (strings.Length == stringsCount)
                                {
                                    Array.Resize(ref strings!, stringsCount > 0 ? stringsCount * 2 : 4); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                                }
                                strings![stringsCount++] = toAdd; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                            }
                        }

                        Array.Resize(ref strings!, stringsCount); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                        data = strings;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        // The actual api is SetValue(string name, object value) but we only need to set Strings
        // so this is a cut-down version that supports on that.
        internal void SetValue(string name, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (name != null && name.Length > MaxValueLength)
                throw new ArgumentException(SR.Arg_RegValStrLenBug, nameof(name));

            int ret = Interop.Advapi32.RegSetValueEx(_hkey,
                name,
                0,
                Interop.Advapi32.RegistryValues.REG_SZ,
                value,
                checked(value.Length * 2 + 2));

            if (ret != 0)
            {
                Win32Error(ret, null);
            }
        }

        internal void Win32Error(int errorCode, string? str)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
                    else
                        throw new UnauthorizedAccessException();

                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
            }
        }
    }

    internal static class Registry
    {
        /// <summary>Current User Key. This key should be used as the root for all user specific settings.</summary>
        public static readonly RegistryKey CurrentUser = RegistryKey.OpenBaseKey(unchecked((IntPtr)(int)0x80000001));

        /// <summary>Local Machine key. This key should be used as the root for all machine specific settings.</summary>
        public static readonly RegistryKey LocalMachine = RegistryKey.OpenBaseKey(unchecked((IntPtr)(int)0x80000002));
    }
}
