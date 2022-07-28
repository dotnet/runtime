// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

/*
  Note on transaction support:
  Eventually we will want to add support for NT's transactions to our
  RegistryKey API's.  When we do this, here's
  the list of API's we need to make transaction-aware:

  RegCreateKeyEx
  RegDeleteKey
  RegDeleteValue
  RegEnumKeyEx
  RegEnumValue
  RegOpenKeyEx
  RegQueryInfoKey
  RegQueryValueEx
  RegSetValueEx

  We can ignore RegConnectRegistry (remote registry access doesn't yet have
  transaction support) and RegFlushKey.  RegCloseKey doesn't require any
  additional work.
 */

/*
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to
  them.  So if you set an ACL to deny read access to yourself, you'll still be
  able to read with that handle, but not with new handles.

  Another peculiarity is a Terminal Server app compatibility hack.  The OS
  will second guess your attempt to open a handle sometimes.  If a certain
  combination of Terminal Server app compat registry keys are set, then the
  OS will try to reopen your handle with lesser permissions if you couldn't
  open it in the specified mode.  So on some machines, we will see handles that
  may not be able to read or write to a registry key.  It's very strange.  But
  the real test of these handles is attempting to read or set a value in an
  affected registry key.

  For reference, at least two registry keys must be set to particular values
  for this behavior:
  HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Terminal Server\RegistryExtensionFlags, the least significant bit must be 1.
  HKLM\SYSTEM\CurrentControlSet\Control\TerminalServer\TSAppCompat must be 1
  There might possibly be an interaction with yet a third registry key as well.
*/

namespace Microsoft.Win32
{
    public sealed partial class RegistryKey : MarshalByRefObject, IDisposable
    {
        private static void ClosePerfDataKey()
        {
            // System keys should never be closed.  However, we want to call RegCloseKey
            // on HKEY_PERFORMANCE_DATA when called from PerformanceCounter.CloseSharedResources
            // (i.e. when disposing is true) so that we release the PERFLIB cache and cause it
            // to be refreshed (by re-reading the registry) when accessed subsequently.
            // This is the only way we can see the just installed perf counter.
            // NOTE: since HKEY_PERFORMANCE_DATA is process wide, there is inherent race in closing
            // the key asynchronously. While Vista is smart enough to rebuild the PERFLIB resources
            // in this situation the down level OSes are not. We have a small window of race between
            // the dispose below and usage elsewhere (other threads). This is By Design.
            // This is less of an issue when OS > NT5 (i.e Vista & higher), we can close the perfkey
            // (to release & refresh PERFLIB resources) and the OS will rebuild PERFLIB as necessary.
            Interop.Advapi32.RegCloseKey(HKEY_PERFORMANCE_DATA);
        }

        private void FlushCore()
        {
            if (_hkey != null && IsDirty())
            {
                Interop.Advapi32.RegFlushKey(_hkey);
            }
        }

        private unsafe RegistryKey CreateSubKeyInternalCore(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default;

            // By default, the new key will be writable.
            int ret = Interop.Advapi32.RegCreateKeyEx(_hkey,
                subkey,
                0,
                null,
                (int)registryOptions /* specifies if the key is volatile */,
                GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree) | (int)_regView,
                ref secAttrs,
                out SafeRegistryHandle result,
                out int _);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false, _remoteKey, false, _regView);
                key._checkMode = permissionCheck;

                if (subkey.Length == 0)
                {
                    key._keyName = _keyName;
                }
                else
                {
                    key._keyName = _keyName + "\\" + subkey;
                }
                return key;
            }

            result.Dispose();

            if (ret != 0) // syscall failed, ret is an error code.
            {
                Win32Error(ret, _keyName + "\\" + subkey);  // Access denied?
            }

            Debug.Fail("Unexpected code path in RegistryKey::CreateSubKey");
            return null;
        }

        private void DeleteSubKeyCore(string subkey, bool throwOnMissingSubKey)
        {
            int ret = Interop.Advapi32.RegDeleteKeyEx(_hkey, subkey, (int)_regView, 0);

            if (ret != 0)
            {
                if (ret == Interop.Errors.ERROR_FILE_NOT_FOUND)
                {
                    if (throwOnMissingSubKey)
                    {
                        throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
                    }
                }
                else
                {
                    Win32Error(ret, null);
                }
            }
        }

        private void DeleteSubKeyTreeCore(string subkey)
        {
            int ret = Interop.Advapi32.RegDeleteKeyEx(_hkey, subkey, (int)_regView, 0);
            if (ret != 0)
            {
                Win32Error(ret, null);
            }
        }

        private void DeleteValueCore(string name, bool throwOnMissingValue)
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

        /// <summary>
        /// Retrieves a new RegistryKey that represents the requested key. Valid
        /// values are:
        /// HKEY_CLASSES_ROOT,
        /// HKEY_CURRENT_USER,
        /// HKEY_LOCAL_MACHINE,
        /// HKEY_USERS,
        /// HKEY_PERFORMANCE_DATA,
        /// HKEY_CURRENT_CONFIG.
        /// </summary>
        /// <param name="hKeyHive">HKEY_* to open.</param>
        /// <param name="view">Which view over the registry to employ.</param>
        /// <returns>The RegistryKey requested.</returns>
        private static RegistryKey OpenBaseKeyCore(RegistryHive hKeyHive, RegistryView view)
        {
            IntPtr hKey = (IntPtr)((int)hKeyHive);

            int index = ((int)hKey) & 0x0FFFFFFF;
            Debug.Assert(index >= 0 && index < s_hkeyNames.Length, "index is out of range!");
            Debug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;

            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf);

            RegistryKey key = new RegistryKey(srh, true, true, false, isPerf, view);
            key._checkMode = RegistryKeyPermissionCheck.Default;
            key._keyName = s_hkeyNames[index];
            return key;
        }

        private static RegistryKey OpenRemoteBaseKeyCore(RegistryHive hKey, string machineName, RegistryView view)
        {
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= s_hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000)
            {
                throw new ArgumentException(SR.Arg_RegKeyOutOfRange);
            }

            // connect to the specified remote registry
            int ret = Interop.Advapi32.RegConnectRegistry(machineName, new IntPtr((int)hKey), out SafeRegistryHandle foreignHKey);
            if (ret == 0 && !foreignHKey.IsInvalid)
            {
                RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr)hKey) == HKEY_PERFORMANCE_DATA, view);
                key._checkMode = RegistryKeyPermissionCheck.Default;
                key._keyName = s_hkeyNames[index];
                return key;
            }

            foreignHKey.Dispose();

            if (ret != 0)
            {
                if (ret == Interop.Errors.ERROR_DLL_INIT_FAILED)
                {
                    // return value indicates an error occurred
                    throw new ArgumentException(SR.Arg_DllInitFailure);
                }

                Win32ErrorStatic(ret, null);
            }

            // return value indicates an error occurred
            throw new ArgumentException(SR.Format(SR.Arg_RegKeyNoRemoteConnect, machineName));
        }

        private RegistryKey? InternalOpenSubKeyCore(string name, RegistryKeyPermissionCheck permissionCheck, int rights)
        {
            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, (rights | (int)_regView), out SafeRegistryHandle result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, (permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree), false, _remoteKey, false, _regView);
                key._keyName = _keyName + "\\" + name;
                key._checkMode = permissionCheck;
                return key;
            }

            result.Dispose();

            if (ret == Interop.Errors.ERROR_ACCESS_DENIED || ret == Interop.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reason,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(SR.Security_RegistryPermission);
            }

            // Return null if we didn't find the key.
            return null;
        }

        private RegistryKey? InternalOpenSubKeyCore(string name, bool writable)
        {
            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, (GetRegistryKeyAccess(writable) | (int)_regView), out SafeRegistryHandle result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, _remoteKey, false, _regView);
                key._checkMode = GetSubKeyPermissionCheck(writable);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            result.Dispose();

            if (ret == Interop.Errors.ERROR_ACCESS_DENIED || ret == Interop.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(SR.Security_RegistryPermission);
            }

            // Return null if we didn't find the key.
            return null;
        }

        internal RegistryKey? InternalOpenSubKeyWithoutSecurityChecksCore(string name, bool writable)
        {
            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, (GetRegistryKeyAccess(writable) | (int)_regView), out SafeRegistryHandle result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, _remoteKey, false, _regView);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            result.Dispose();

            return null;
        }

        private SafeRegistryHandle SystemKeyHandle
        {
            get
            {
                Debug.Assert(IsSystemKey());

                int ret = Interop.Errors.ERROR_INVALID_HANDLE;
                IntPtr baseKey = (IntPtr)0;
                switch (_keyName)
                {
                    case "HKEY_CLASSES_ROOT":
                        baseKey = HKEY_CLASSES_ROOT;
                        break;
                    case "HKEY_CURRENT_USER":
                        baseKey = HKEY_CURRENT_USER;
                        break;
                    case "HKEY_LOCAL_MACHINE":
                        baseKey = HKEY_LOCAL_MACHINE;
                        break;
                    case "HKEY_USERS":
                        baseKey = HKEY_USERS;
                        break;
                    case "HKEY_PERFORMANCE_DATA":
                        baseKey = HKEY_PERFORMANCE_DATA;
                        break;
                    case "HKEY_CURRENT_CONFIG":
                        baseKey = HKEY_CURRENT_CONFIG;
                        break;
                    default:
                        Win32Error(ret, null);
                        break;
                }

                // open the base key so that RegistryKey.Handle will return a valid handle
                ret = Interop.Advapi32.RegOpenKeyEx(baseKey,
                    null,
                    0,
                    GetRegistryKeyAccess(IsWritable()) | (int)_regView,
                    out SafeRegistryHandle result);

                if (ret != 0 || result.IsInvalid)
                {
                    result.Dispose();
                    Win32Error(ret, null);
                }

                return result;
            }
        }

        private int InternalSubKeyCountCore()
        {
            int subkeys = 0;
            int junk = 0;
            int ret = Interop.Advapi32.RegQueryInfoKey(_hkey,
                                      null,
                                      null,
                                      IntPtr.Zero,
                                      ref subkeys,  // subkeys
                                      null,
                                      null,
                                      ref junk,     // values
                                      null,
                                      null,
                                      null,
                                      null);

            if (ret != 0)
            {
                Win32Error(ret, null);
            }

            return subkeys;
        }

        private string[] InternalGetSubKeyNamesCore(int subkeys)
        {
            var names = new List<string>(subkeys);
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

        private int InternalValueCountCore()
        {
            int values = 0;
            int junk = 0;
            int ret = Interop.Advapi32.RegQueryInfoKey(_hkey,
                                      null,
                                      null,
                                      IntPtr.Zero,
                                      ref junk,     // subkeys
                                      null,
                                      null,
                                      ref values,   // values
                                      null,
                                      null,
                                      null,
                                      null);
            if (ret != 0)
            {
                Win32Error(ret, null);
            }

            return values;
        }

        /// <summary>Retrieves an array of strings containing all the value names.</summary>
        /// <returns>All value names.</returns>
        private unsafe string[] GetValueNamesCore(int values)
        {
            var names = new List<string>(values);

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
                            if (IsPerfDataKey())
                            {
                                // Enumerating the values for Perf keys always returns
                                // ERROR_MORE_DATA, but has a valid name. Buffer does need
                                // to be big enough however. 8 characters is the largest
                                // known name. The size isn't returned, but the string is
                                // null terminated.
                                fixed (char* c = &name[0])
                                {
                                    names.Add(new string(c));
                                }
                            }
                            else
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

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private unsafe object? InternalGetValueCore(string? name, object? defaultValue, bool doNotExpand)
        {
            // Create an initial stack buffer large enough to satisfy many reg keys.  We need to call RegQueryValueEx
            // in order to determine the type of the value, and we can avoid further retries if all of the data can be
            // retrieved in that single call with this buffer.  If we do need to grow, we grow into a pooled array. The
            // caller is always handed back a copy of this data, either in an array, a string, or a boxed integer.
            Span<byte> span = stackalloc byte[512];
            byte[]? pooledArray = null;
            if (IsPerfDataKey())
            {
                // If this is a performance key (rare usage), we're not actually retrieving data stored in the registry:
                // calling RegQueryValueEx causes the system to collect the data from the appropriate provider. The value
                // is expected to be much larger than for other keys, such that our stack-allocated space is less likely
                // to be sufficient, so we immediately grow into the ArrayPool, using the same buffer size chosen
                // in .NET Framework (until such time as a better estimate is selected).  Additionally, the returned
                // length when dealing with perf data keys can't be trusted, so the buffer needs to be zero'd.
                span = pooledArray = ArrayPool<byte>.Shared.Rent(65_000);
                span.Clear();
            }

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
                        if (IsPerfDataKey())
                        {
                            // In the case of a performance key, dataLength is not reliable, but we know the existing
                            // size was insufficient, so double the buffer size.
                            dataLength = span.Length * 2;
                        }

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
                        if (IsPerfDataKey())
                        {
                            span.Clear();
                        }

                        continue;
                    }

                    // For any other error, return the default value.  This might be ERROR_FILE_NOT_FOUND if the reg key
                    // wasn't found, or any other system error value for unspecified reasons. For compat, an exception
                    // is thrown for perf keys rather than returning the default value.
                    if (result != Interop.Errors.ERROR_SUCCESS)
                    {
                        if (IsPerfDataKey())
                        {
                            Win32Error(result, name);
                        }
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
                                    if (type == Interop.Advapi32.RegistryValues.REG_EXPAND_SZ && !doNotExpand)
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

        private unsafe RegistryValueKind GetValueKindCore(string? name)
        {
            int type = 0;
            int datasize = 0;
            int ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, &type, (byte*)null, (uint*)&datasize);
            if (ret != 0)
            {
                Win32Error(ret, null);
            }

            return
                type == Interop.Advapi32.RegistryValues.REG_NONE ? RegistryValueKind.None :
                !Enum.IsDefined(typeof(RegistryValueKind), type) ? RegistryValueKind.Unknown :
                (RegistryValueKind)type;
        }

        private unsafe void SetValueCore(string? name, object value, RegistryValueKind valueKind)
        {
            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            string data = value.ToString()!;
                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                (int)valueKind,
                                data,
                                checked(data.Length * 2 + 2));
                            break;
                        }

                    case RegistryValueKind.MultiString:
                        {
                            // Other thread might modify the input array after we calculate the buffer length.
                            // Make a copy of the input array to be safe.
                            string[] dataStrings = (string[])(((string[])value).Clone());

                            // First determine the size of the array
                            //
                            // Format is null terminator between strings and final null terminator at the end.
                            //    e.g. str1\0str2\0str3\0\0
                            //
                            int sizeInChars = 1; // no matter what, we have the final null terminator.
                            for (int i = 0; i < dataStrings.Length; i++)
                            {
                                if (dataStrings[i] == null)
                                {
                                    throw new ArgumentException(SR.Arg_RegSetStrArrNull);
                                }
                                sizeInChars = checked(sizeInChars + (dataStrings[i].Length + 1));
                            }
                            int sizeInBytes = checked(sizeInChars * sizeof(char));

                            // Write out the strings...
                            //
                            char[] dataChars = new char[sizeInChars];
                            int destinationIndex = 0;
                            for (int i = 0; i < dataStrings.Length; i++)
                            {
                                int length = dataStrings[i].Length;
                                dataStrings[i].CopyTo(0, dataChars, destinationIndex, length);
                                destinationIndex += (length + 1); // +1 for null terminator, which is already zero-initialized in new array.
                            }

                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                Interop.Advapi32.RegistryValues.REG_MULTI_SZ,
                                dataChars,
                                sizeInBytes);

                            break;
                        }

                    case RegistryValueKind.None:
                    case RegistryValueKind.Binary:
                        byte[] dataBytes = (byte[])value;
                        ret = Interop.Advapi32.RegSetValueEx(_hkey,
                            name,
                            0,
                            (valueKind == RegistryValueKind.None ? Interop.Advapi32.RegistryValues.REG_NONE : Interop.Advapi32.RegistryValues.REG_BINARY),
                            dataBytes,
                            dataBytes.Length);
                        break;

                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail.
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                Interop.Advapi32.RegistryValues.REG_DWORD,
                                ref data,
                                4);
                            break;
                        }

                    case RegistryValueKind.QWord:
                        {
                            long data = Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                Interop.Advapi32.RegistryValues.REG_QWORD,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (Exception exc) when (exc is OverflowException || exc is InvalidOperationException || exc is FormatException || exc is InvalidCastException)
            {
                throw new ArgumentException(SR.Arg_RegSetMismatchedKind);
            }

            if (ret == 0)
            {
                SetDirty();
            }
            else
            {
                Win32Error(ret, null);
            }
        }

        /// <summary>
        /// After calling GetLastWin32Error(), it clears the last error field,
        /// so you must save the HResult and pass it to this method.  This method
        /// will determine the appropriate exception to throw dependent on your
        /// error, and depending on the error, insert a string into the message
        /// gotten from the ResourceManager.
        /// </summary>
        private void Win32Error(int errorCode, string? str)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_ACCESS_DENIED:
                    throw str != null ?
                        new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str)) :
                        new UnauthorizedAccessException();

                case Interop.Errors.ERROR_INVALID_HANDLE:
                    // For normal RegistryKey instances we dispose the SafeRegHandle and throw IOException.
                    // However, for HKEY_PERFORMANCE_DATA (on a local or remote machine) we avoid disposing the
                    // SafeRegHandle and only throw the IOException.  This is to workaround reentrancy issues
                    // in PerformanceCounter.NextValue() where the API could throw {NullReference, ObjectDisposed, ArgumentNull}Exception
                    // on reentrant calls because of this error code path in RegistryKey
                    //
                    // Normally we'd make our caller synchronize access to a shared RegistryKey instead of doing something like this,
                    // however we shipped PerformanceCounter.NextValue() un-synchronized in v2.0RTM and customers have taken a dependency on
                    // this behavior (being able to simultaneously query multiple remote-machine counters on multiple threads, instead of
                    // having serialized access).
                    if (!IsPerfDataKey())
                    {
                        _hkey.SetHandleAsInvalid();
                        _hkey = null!;
                    }
                    goto default;

                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
            }
        }

        private static void Win32ErrorStatic(int errorCode, string? str) =>
            throw errorCode switch
            {
                Interop.Errors.ERROR_ACCESS_DENIED => str != null ?
                       new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str)) :
                       new UnauthorizedAccessException(),

                _ => new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode),
            };

        private static int GetRegistryKeyAccess(bool isWritable)
        {
            int winAccess;
            if (!isWritable)
            {
                winAccess = Interop.Advapi32.RegistryOperations.KEY_READ;
            }
            else
            {
                winAccess = Interop.Advapi32.RegistryOperations.KEY_READ | Interop.Advapi32.RegistryOperations.KEY_WRITE;
            }

            return winAccess;
        }

        private static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode)
        {
            int winAccess = 0;
            switch (mode)
            {
                case RegistryKeyPermissionCheck.ReadSubTree:
                case RegistryKeyPermissionCheck.Default:
                    winAccess = Interop.Advapi32.RegistryOperations.KEY_READ;
                    break;

                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Interop.Advapi32.RegistryOperations.KEY_READ | Interop.Advapi32.RegistryOperations.KEY_WRITE;
                    break;

                default:
                    Debug.Fail("unexpected code path");
                    break;
            }

            return winAccess;
        }
    }
}
