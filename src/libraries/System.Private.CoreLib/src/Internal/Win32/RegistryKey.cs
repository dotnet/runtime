// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

using Internal.Win32.SafeHandles;

//
// A minimal version of RegistryKey that supports just what CoreLib needs.
//
// Internal.Win32 namespace avoids confusion with the public standalone Microsoft.Win32.Registry implementation
// that lives in https://github.com/dotnet/runtime/tree/main/src/libraries/Microsoft.Win32.Registry
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

        private readonly SafeRegistryHandle _hkey;

        private RegistryKey(SafeRegistryHandle hkey)
        {
            _hkey = hkey;
        }

        void IDisposable.Dispose()
        {
            _hkey?.Dispose();
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
            Debug.Assert(errorCode == 0, $"RegDeleteValue failed.  Here's your error code: {errorCode}");
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
            Debug.Assert(!name.Contains(@"\\"));

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

            result.Dispose();

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

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public unsafe object? GetValue(string name, object? defaultValue)
        {
            // Create an initial stack buffer large enough to satisfy many reg keys.  We need to call RegQueryValueEx
            // in order to determine the type of the value, and we can avoid further retries if all of the data can be
            // retrieved in that single call with this buffer.  If we do need to grow, we grow into a pooled array. The
            // caller is always handed back a copy of this data, either in an array, a string, or a boxed integer.
            Span<byte> span = stackalloc byte[512];
            byte[]? pooledArray = null;

            try
            {
                // Loop in case we need to try again with a larger buffer size.
                while (true)
                {
                    int type = 0;
                    int result;
                    int dataLength = span.Length;

                    fixed (byte* lpData = &MemoryMarshal.GetReference(span))
                    {
                        result = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, &type, lpData, (uint*)&dataLength);
                        if (dataLength < 0)
                        {
                            // Greater than 2GB values aren't supported.
                            throw new IOException(SR.Arg_RegValueTooLarge);
                        }
                    }

                    // If RegQueryValueEx told us we need a larger buffer, get one and then loop around to try again.
                    if (result == Interop.Errors.ERROR_MORE_DATA)
                    {
                        if (pooledArray is not null)
                        {
                            // This should only happen if the registry key was changed concurrently, such that
                            // we called RegQueryValueEx with our initial buffer size that was too small, we then
                            // rented a buffer of the reportedly right size and called RegQueryValueEx again, but
                            // it still came back with ERROR_MORE_DATA again.
                            byte[] toReturn = pooledArray;
                            pooledArray = null;
                            ArrayPool<byte>.Shared.Return(toReturn);
                        }

                        // Greater than 2GB values aren't supported.
                        if (dataLength < 0)
                        {
                            throw new IOException(SR.Arg_RegValueTooLarge);
                        }

                        span = pooledArray = ArrayPool<byte>.Shared.Rent(dataLength);

                        continue;
                    }

                    // For any other error, return the default value.  This might be ERROR_FILE_NOT_FOUND if the reg key
                    // wasn't found, or any other system error value for unspecified reasons. For compat, an exception
                    // is thrown for perf keys rather than returning the default value.
                    if (result != Interop.Errors.ERROR_SUCCESS)
                    {
                        return defaultValue;
                    }

                    // We only get here for a successful query of the data. Process and return the results.
                    Debug.Assert((uint)dataLength <= span.Length, $"Expected {dataLength} <= {span.Length}");
                    switch (type)
                    {
                        case Interop.Advapi32.RegistryValues.REG_NONE:
                        case Interop.Advapi32.RegistryValues.REG_BINARY:
                        case Interop.Advapi32.RegistryValues.REG_DWORD_BIG_ENDIAN:
                            return span.Slice(0, dataLength).ToArray();

                        case Interop.Advapi32.RegistryValues.REG_DWORD:
                        case Interop.Advapi32.RegistryValues.REG_QWORD:
                            return dataLength switch
                            {
                                4 => MemoryMarshal.Read<int>(span),
                                8 => MemoryMarshal.Read<long>(span),
                                _ => span.Slice(0, dataLength).ToArray(), // This shouldn't happen, but the previous implementation included it defensively.
                            };

                        case Interop.Advapi32.RegistryValues.REG_SZ:
                        case Interop.Advapi32.RegistryValues.REG_EXPAND_SZ:
                        case Interop.Advapi32.RegistryValues.REG_MULTI_SZ:
                            {
                                // Handle the case where the registry contains an odd-byte length (corrupt data?)
                                // by increasing the data by a single zero byte.
                                if (dataLength % 2 == 1)
                                {
                                    if (dataLength == int.MaxValue)
                                    {
                                        throw new IOException(SR.Arg_RegValueTooLarge);
                                    }

                                    if (dataLength >= span.Length)
                                    {
                                        byte[] newPooled = ArrayPool<byte>.Shared.Rent(dataLength + 1);
                                        span.CopyTo(newPooled);
                                        if (pooledArray is not null)
                                        {
                                            byte[] toReturn = pooledArray;
                                            pooledArray = null;
                                            ArrayPool<byte>.Shared.Return(toReturn);
                                        }
                                        span = pooledArray = newPooled;
                                    }

                                    span[dataLength++] = 0;
                                }

                                // From here on, we interpret the read bytes as chars; span and dataLength should no longer be used.
                                ReadOnlySpan<char> chars = MemoryMarshal.Cast<byte, char>(span.Slice(0, dataLength));

                                if (type == Interop.Advapi32.RegistryValues.REG_MULTI_SZ)
                                {
                                    string[] strings = Array.Empty<string>();
                                    int count = 0;

                                    while (chars.Length > 1 || (chars.Length == 1 && chars[0] != '\0'))
                                    {
                                        int nullPos = chars.IndexOf('\0');
                                        string toAdd;
                                        if (nullPos < 0)
                                        {
                                            toAdd = chars.ToString();
                                            chars = default;
                                        }
                                        else
                                        {
                                            toAdd = chars.Slice(0, nullPos).ToString();
                                            chars = chars.Slice(nullPos + 1);
                                        }

                                        if (count == strings.Length)
                                        {
                                            Array.Resize(ref strings, count == 0 ? 4 : count * 2);
                                        }
                                        strings[count++] = toAdd;
                                    }

                                    if (count != 0)
                                    {
                                        Array.Resize(ref strings, count);
                                    }

                                    return strings;
                                }
                                else
                                {
                                    if (chars.Length == 0)
                                    {
                                        return string.Empty;
                                    }

                                    // Remove null termination if it exists.
                                    if (chars[^1] == 0)
                                    {
                                        chars = chars[0..^1];
                                    }

                                    // Get the resulting string from the bytes.
                                    string str = chars.ToString();
                                    if (type == Interop.Advapi32.RegistryValues.REG_EXPAND_SZ)
                                    {
                                        str = Environment.ExpandEnvironmentVariables(str);
                                    }

                                    return str;
                                }
                            }

                        default:
                            return defaultValue;
                    }
                }
            }
            finally
            {
                if (pooledArray is not null)
                {
                    ArrayPool<byte>.Shared.Return(pooledArray);
                }
            }
        }

        // The actual api is SetValue(string name, object value) but we only need to set Strings
        // so this is a cut-down version that supports on that.
        internal void SetValue(string name, string value)
        {
            ArgumentNullException.ThrowIfNull(value);

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

        internal static void Win32Error(int errorCode, string? str)
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
