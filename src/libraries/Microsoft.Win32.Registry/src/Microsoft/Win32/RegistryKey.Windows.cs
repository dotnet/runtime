// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;

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
#if REGISTRY_ASSEMBLY
    public
#else
    internal
#endif
    sealed partial class RegistryKey : IDisposable
    {
        private void ClosePerfDataKey()
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

        private unsafe RegistryKey CreateSubKeyInternalCore(string subkey, bool writable, RegistryOptions registryOptions)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
            int disposition = 0;

            // By default, the new key will be writable.
            SafeRegistryHandle result = null;
            int ret = Interop.Advapi32.RegCreateKeyEx(_hkey,
                subkey,
                0,
                null,
                (int)registryOptions /* specifies if the key is volatile */,
                (int)GetRegistryKeyRights(writable) | (int)_regView,
                ref secAttrs,
                out result,
                out disposition);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, _remoteKey, false, _regView);
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
            else if (ret != 0) // syscall failed, ret is an error code.
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
                        ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyAbsent);
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
                    ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyValueAbsent);
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
            SafeRegistryHandle foreignHKey = null;
            int ret = Interop.Advapi32.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey);

            if (ret == Interop.Errors.ERROR_DLL_INIT_FAILED)
            {
                // return value indicates an error occurred
                throw new ArgumentException(SR.Arg_DllInitFailure);
            }

            if (ret != 0)
            {
                Win32ErrorStatic(ret, null);
            }

            if (foreignHKey.IsInvalid)
            {
                // return value indicates an error occurred
                throw new ArgumentException(SR.Format(SR.Arg_RegKeyNoRemoteConnect, machineName));
            }

            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr)hKey) == HKEY_PERFORMANCE_DATA, view);
            key._keyName = s_hkeyNames[index];
            return key;
        }

        private RegistryKey InternalOpenSubKeyCore(string name, RegistryRights rights, bool throwOnPermissionFailure)
        {
            SafeRegistryHandle result = null;
            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, ((int)rights | (int)_regView), out result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, IsWritable((int)rights), false, _remoteKey, false, _regView);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            if (throwOnPermissionFailure)
            {
                if (ret == Interop.Errors.ERROR_ACCESS_DENIED || ret == Interop.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
                {
                    // We need to throw SecurityException here for compatibility reason,
                    // although UnauthorizedAccessException will make more sense.
                    ThrowHelper.ThrowSecurityException(SR.Security_RegistryPermission);
                }
            }

            // Return null if we didn't find the key.
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
                SafeRegistryHandle result;
                ret = Interop.Advapi32.RegOpenKeyEx(baseKey,
                    null,
                    0,
                    (int)GetRegistryKeyRights(IsWritable()) | (int)_regView,
                    out result);

                if (ret == 0 && !result.IsInvalid)
                {
                    return result;
                }
                else
                {
                    Win32Error(ret, null);
                    throw new IOException(Interop.Kernel32.GetMessage(ret), ret);
                }
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

        private unsafe string[] InternalGetSubKeyNamesCore(int subkeys)
        {
            string[] names = new string[subkeys];
            char[] name = new char[MaxKeyLength + 1];

            int namelen;

            fixed (char* namePtr = &name[0])
            {
                for (int i = 0; i < subkeys; i++)
                {
                    namelen = name.Length; // Don't remove this. The API's doesn't work if this is not properly initialized.
                    int ret = Interop.Advapi32.RegEnumKeyEx(_hkey,
                        i,
                        namePtr,
                        ref namelen,
                        null,
                        null,
                        null,
                        null);
                    if (ret != 0)
                    {
                        Win32Error(ret, null);
                    }

                    names[i] = new string(namePtr);
                }
            }

            return names;
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
            string[] names = new string[values];
            char[] name = new char[MaxValueLength + 1];
            int namelen;

            fixed (char* namePtr = &name[0])
            {
                for (int i = 0; i < values; i++)
                {
                    namelen = name.Length;

                    int ret = Interop.Advapi32.RegEnumValue(_hkey,
                        i,
                        namePtr,
                        ref namelen,
                        IntPtr.Zero,
                        null,
                        null,
                        null);

                    if (ret != 0)
                    {
                        // ignore ERROR_MORE_DATA if we're querying HKEY_PERFORMANCE_DATA
                        if (!(IsPerfDataKey() && ret == Interop.Errors.ERROR_MORE_DATA))
                            Win32Error(ret, null);
                    }

                    names[i] = new string(namePtr);
                }
            }

            return names;
        }

        private object InternalGetValueCore(string name, object defaultValue, bool doNotExpand)
        {
            object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0)
            {
                if (IsPerfDataKey())
                {
                    int size = 65000;
                    int sizeInput = size;

                    int r;
                    byte[] blob = new byte[size];
                    while (Interop.Errors.ERROR_MORE_DATA == (r = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, blob, ref sizeInput)))
                    {
                        if (size == Int32.MaxValue)
                        {
                            // ERROR_MORE_DATA was returned however we cannot increase the buffer size beyond Int32.MaxValue
                            Win32Error(r, name);
                        }
                        else if (size > (Int32.MaxValue / 2))
                        {
                            // at this point in the loop "size * 2" would cause an overflow
                            size = Int32.MaxValue;
                        }
                        else
                        {
                            size *= 2;
                        }
                        sizeInput = size;
                        blob = new byte[size];
                    }
                    if (r != 0)
                    {
                        Win32Error(r, name);
                    }
                    return blob;
                }
                else
                {
                    // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                    // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                    // want to continue on through the function. 
                    if (ret != Interop.Errors.ERROR_MORE_DATA)
                    {
                        return data;
                    }
                }
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

                        if (!doNotExpand)
                        {
                            data = Environment.ExpandEnvironmentVariables((string)data);
                        }
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
                            Array.Resize(ref blob, blob.Length + 1);
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

                            string toAdd = null;
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
                                    Array.Resize(ref strings, stringsCount > 0 ? stringsCount * 2 : 4);
                                }
                                strings[stringsCount++] = toAdd;
                            }
                        }

                        Array.Resize(ref strings, stringsCount);
                        data = strings;
                    }
                    break;
                case Interop.Advapi32.RegistryValues.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        private RegistryValueKind GetValueKindCore(string name)
        {
            int type = 0;
            int datasize = 0;
            int ret = Interop.Advapi32.RegQueryValueEx(_hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
            {
                Win32Error(ret, null);
            }

            return
                type == Interop.Advapi32.RegistryValues.REG_NONE ? RegistryValueKind.None :
                !Enum.IsDefined(typeof(RegistryValueKind), type) ? RegistryValueKind.Unknown :
                (RegistryValueKind)type;
        }

        private unsafe void SetValueCore(string name, object value, RegistryValueKind valueKind)
        {
            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            string data = value.ToString();
                            ret = Interop.Advapi32.RegSetValueEx(_hkey,
                                name,
                                0,
                                valueKind,
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
                                    ThrowHelper.ThrowArgumentException(SR.Arg_RegSetStrArrNull);
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
                                RegistryValueKind.MultiString,
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
                            (valueKind == RegistryValueKind.None ? Interop.Advapi32.RegistryValues.REG_NONE : RegistryValueKind.Binary),
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
                                RegistryValueKind.DWord,
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
                                RegistryValueKind.QWord,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (Exception exc) when (exc is OverflowException || exc is InvalidOperationException || exc is FormatException || exc is InvalidCastException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
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
        private void Win32Error(int errorCode, string str)
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
                        _hkey = null;
                    }
                    goto default;

                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
            }
        }

        private static void Win32ErrorStatic(int errorCode, string str)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_ACCESS_DENIED:
                    throw str != null ?
                        new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str)) :
                        new UnauthorizedAccessException();

                default:
                    throw new IOException(Interop.Kernel32.GetMessage(errorCode), errorCode);
            }
        }

        private static bool IsWritable(int rights)
        {
            return (rights & (Interop.Advapi32.RegistryOperations.KEY_SET_VALUE |
                              Interop.Advapi32.RegistryOperations.KEY_CREATE_SUB_KEY |
                              (int)RegistryRights.Delete |
                              (int)RegistryRights.TakeOwnership |
                              (int)RegistryRights.ChangePermissions)) != 0;
        }
    }
}
