// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.AccessControl;

namespace Microsoft.Win32
{
    /**
     * Registry hive values.  Useful only for GetRemoteBaseKey
     */
    public enum RegistryHive
    {
        ClassesRoot = unchecked((int)0x80000000),
        CurrentUser = unchecked((int)0x80000001),
        LocalMachine = unchecked((int)0x80000002),
        Users = unchecked((int)0x80000003),
        PerformanceData = unchecked((int)0x80000004),
        CurrentConfig = unchecked((int)0x80000005),
    }

    /**
     * Registry encapsulation. To get an instance of a RegistryKey use the
     * Registry class's static members then call OpenSubKey.
     *
     * @see Registry
     * @security(checkDllCalls=off)
     * @security(checkClassLinking=on)
     */
    public sealed class RegistryKey : IDisposable
    {
        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        internal static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
        internal static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

        // Dirty indicates that we have munged data that should be potentially
        // written to disk.
        //
        private const int STATE_DIRTY = 0x0001;

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened"
        // or "closed".
        //
        private const int STATE_SYSTEMKEY = 0x0002;

        // Access
        //
        private const int STATE_WRITEACCESS = 0x0004;

        // Indicates if this key is for HKEY_PERFORMANCE_DATA
        private const int STATE_PERF_DATA = 0x0008;

        // Names of keys.  This array must be in the same order as the HKEY values listed above.
        //
        private static readonly String[] hkeyNames = new String[] {
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS",
                "HKEY_PERFORMANCE_DATA",
                "HKEY_CURRENT_CONFIG"
                };

        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  16,383 Unicode characters
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;

        [System.Security.SecurityCritical] 
        private volatile SafeRegistryHandle hkey = null;
        private volatile int state = 0;
        private volatile String keyName;
        private volatile bool remoteKey = false;
        private volatile RegistryView regView = RegistryView.Default;

        /**
         * RegistryInternalCheck values.  Useful only for CheckPermission
         */
        private enum RegistryInternalCheck
        {
            CheckSubKeyWritePermission = 0,
            CheckSubKeyReadPermission = 1,
            CheckSubKeyCreatePermission = 2,
            CheckSubTreeReadPermission = 3,
            CheckSubTreeWritePermission = 4,
            CheckSubTreeReadWritePermission = 5,
            CheckValueWritePermission = 6,
            CheckValueCreatePermission = 7,
            CheckValueReadPermission = 8,
            CheckKeyReadPermission = 9,
            CheckSubTreePermission = 10,
            CheckOpenSubKeyWithWritablePermission = 11,
            CheckOpenSubKeyPermission = 12
        };


        /**
         * Creates a RegistryKey.
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations
         * will be allowed.
         */
        [System.Security.SecurityCritical]  // auto-generated
        private RegistryKey(SafeRegistryHandle hkey, bool writable, RegistryView view)
            : this(hkey, writable, false, false, false, view)
        {
        }


        /**
         * Creates a RegistryKey.
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations
         * will be allowed. If systemkey is set then the hkey won't be released
         * when the object is GC'ed.
         * The remoteKey flag when set to true indicates that we are dealing with registry entries
         * on a remote machine and requires the program making these calls to have full trust.
         */
        [System.Security.SecurityCritical]  
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view)
        {
            this.hkey = hkey;
            this.keyName = "";
            this.remoteKey = remoteKey;
            this.regView = view;
            if (systemkey)
            {
                this.state |= STATE_SYSTEMKEY;
            }
            if (writable)
            {
                this.state |= STATE_WRITEACCESS;
            }
            if (isPerfData)
                this.state |= STATE_PERF_DATA;
            ValidateKeyView(view);
        }

        /**
         * Closes this key, flushes it to disk if the contents have been modified.
         */
        internal void Close()
        {
            Dispose(true);
        }

        [System.Security.SecuritySafeCritical]  
        private void Dispose(bool disposing)
        {
            if (hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        hkey.Dispose();
                    }
                    catch (IOException)
                    {
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        hkey = null;
                    }
                }
                else if (disposing && IsPerfDataKey())
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
                    Interop.mincore.RegCloseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
                }
            }
        }

        [System.Security.SecuritySafeCritical]  
        public void Flush()
        {
            if (hkey != null)
            {
                if (IsDirty())
                {
                    Interop.mincore.RegFlushKey(hkey);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        /**
         * Creates a new subkey, or opens an existing one.
         *
         * @param subkey Name or path to subkey to create or open.
         *
         * @return the subkey, or <b>null</b> if the operation failed.
         */
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        public RegistryKey CreateSubKey(String subkey)
        {
            return CreateSubKey(subkey, IsWritable());
        }

        public RegistryKey CreateSubKey(String subkey, bool writable)
        {
            return CreateSubKeyInternal(subkey, writable, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(String subkey, bool writable, RegistryOptions options)
        {
            return CreateSubKeyInternal(subkey, writable, options);
        }

        [System.Security.SecuritySafeCritical]  
        private unsafe RegistryKey CreateSubKeyInternal(String subkey, bool writable, RegistryOptions registryOptions)
        {
            ValidateKeyOptions(registryOptions);
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // only keys opened under read mode is not writable
            if (!remoteKey)
            {
                RegistryKey key = InternalOpenSubKey(subkey, writable);
                if (key != null)
                { // Key already exits
                    return key;
                }
            }

            Interop.mincore.SECURITY_ATTRIBUTES secAttrs = default(Interop.mincore.SECURITY_ATTRIBUTES);
            int disposition = 0;

            // By default, the new key will be writable.
            SafeRegistryHandle result = null;
            int ret = Interop.mincore.RegCreateKeyEx(hkey,
                subkey,
                0,
                null,
                (int)registryOptions /* specifies if the key is volatile */,
                GetRegistryKeyAccess(writable) | (int)regView,
                ref secAttrs,
                out result,
                out disposition);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false, regView);

                if (subkey.Length == 0)
                    key.keyName = keyName;
                else
                    key.keyName = keyName + "\\" + subkey;
                return key;
            }
            else if (ret != 0) // syscall failed, ret is an error code.
                Win32Error(ret, keyName + "\\" + subkey);  // Access denied?

            Debug.Fail("Unexpected code path in RegistryKey::CreateSubKey");
            return null;
        }

        /**
         * Deletes the specified subkey. Will throw an exception if the subkey has
         * subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
         *
         * @param subkey SubKey to delete.
         *
         * @exception InvalidOperationException thrown if the subkey has child subkeys.
         */
        public void DeleteSubKey(String subkey)
        {
            DeleteSubKey(subkey, true);
        }

        [System.Security.SecuritySafeCritical]  
        public void DeleteSubKey(String subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open.
            //
            RegistryKey key = InternalOpenSubKey(subkey, false);
            if (key != null)
            {
                using (key)
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        ThrowHelper.ThrowInvalidOperationException(SR.InvalidOperation_RegRemoveSubKey);
                    }
                }

                int ret = Interop.mincore.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);

                if (ret != 0)
                {
                    if (ret == Interop.mincore.Errors.ERROR_FILE_NOT_FOUND)
                    {
                        if (throwOnMissingSubKey)
                            ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyAbsent);
                    }
                    else
                        Win32Error(ret, null);
                }
            }
            else
            { // there is no key which also means there is no subkey
                if (throwOnMissingSubKey)
                    ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyAbsent);
            }
        }

        /**
         * Recursively deletes a subkey and any child subkeys.
         *
         * @param subkey SubKey to delete.
         */
        public void DeleteSubKeyTree(String subkey)
        {
            DeleteSubKeyTree(subkey, true /*throwOnMissingSubKey*/);
        }

        [System.Security.SecuritySafeCritical]  
        public void DeleteSubKeyTree(String subkey, Boolean throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);

            // Security concern: Deleting a hive's "" subkey would delete all
            // of that hive's contents.  Don't allow "".
            if (subkey.Length == 0 && IsSystemKey())
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegKeyDelHive);
            }

            EnsureWriteable();

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                using (key)
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        String[] keys = key.InternalGetSubKeyNames();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }

                int ret = Interop.mincore.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);

                if (ret != 0) Win32Error(ret, null);
            }
            else if (throwOnMissingSubKey)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyAbsent);
            }
        }

        // An internal version which does no security checks or argument checking.  Skipping the 
        // security checks should give us a slight perf gain on large trees. 
        [System.Security.SecurityCritical]  
        private void DeleteSubKeyTreeInternal(string subkey)
        {
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null)
            {
                using (key)
                {
                    if (key.InternalSubKeyCount() > 0)
                    {
                        String[] keys = key.InternalGetSubKeyNames();

                        for (int i = 0; i < keys.Length; i++)
                        {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }

                int ret = Interop.mincore.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);

                if (ret != 0) Win32Error(ret, null);
            }
            else
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSubKeyAbsent);
            }
        }

        /**
         * Deletes the specified value from this key.
         *
         * @param name Name of value to delete.
         */
        public void DeleteValue(String name)
        {
            DeleteValue(name, true);
        }

        [System.Security.SecuritySafeCritical]  
        public void DeleteValue(String name, bool throwOnMissingValue)
        {
            EnsureWriteable();
            int errorCode = Interop.mincore.RegDeleteValue(hkey, name);

            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE  
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Interop.mincore.Errors.ERROR_FILE_NOT_FOUND || errorCode == Interop.mincore.Errors.ERROR_FILENAME_EXCED_RANGE)
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

        /**
         * Retrieves a new RegistryKey that represents the requested key. Valid
         * values are:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER,
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG.
         *
         * @param hKey HKEY_* to open.
         *
         * @return the RegistryKey requested.
         */
        [System.Security.SecurityCritical]  
        internal static RegistryKey GetBaseKey(IntPtr hKey)
        {
            return GetBaseKey(hKey, RegistryView.Default);
        }

        [System.Security.SecurityCritical]  
        internal static RegistryKey GetBaseKey(IntPtr hKey, RegistryView view)
        {
            int index = ((int)hKey) & 0x0FFFFFFF;
            Debug.Assert(index >= 0 && index < hkeyNames.Length, "index is out of range!");
            Debug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf);

            RegistryKey key = new RegistryKey(srh, true, true, false, isPerf, view);
            key.keyName = hkeyNames[index];
            return key;
        }


        [System.Security.SecuritySafeCritical]  
        public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view)
        {
            ValidateKeyView(view);
            return GetBaseKey((IntPtr)((int)hKey), view);
        }

        /**
         * Retrieves a new RegistryKey that represents the requested key on a foreign
         * machine.  Valid values for hKey are members of the RegistryHive enum, or
         * Win32 integers such as:
         *
         * HKEY_CLASSES_ROOT,
         * HKEY_CURRENT_USER,
         * HKEY_LOCAL_MACHINE,
         * HKEY_USERS,
         * HKEY_PERFORMANCE_DATA,
         * HKEY_CURRENT_CONFIG.
         *
         * @param hKey HKEY_* to open.
         * @param machineName the machine to connect to
         *
         * @return the RegistryKey requested.
         */
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName)
        {
            return OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
        }

        [System.Security.SecuritySafeCritical]  
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName, RegistryView view)
        {
            if (machineName == null)
                throw new ArgumentNullException(nameof(machineName));
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000)
            {
                throw new ArgumentException(SR.Arg_RegKeyOutOfRange);
            }
            ValidateKeyView(view);

            // connect to the specified remote registry
            SafeRegistryHandle foreignHKey = null;
            int ret = Interop.mincore.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey);

            if (ret == Interop.mincore.Errors.ERROR_DLL_INIT_FAILED)
                // return value indicates an error occurred
                throw new ArgumentException(SR.Arg_DllInitFailure);

            if (ret != 0)
                Win32ErrorStatic(ret, null);

            if (foreignHKey.IsInvalid)
                // return value indicates an error occurred
                throw new ArgumentException(SR.Format(SR.Arg_RegKeyNoRemoteConnect, machineName));

            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr)hKey) == HKEY_PERFORMANCE_DATA, view);
            key.keyName = hkeyNames[index];
            return key;
        }

        /**
         * Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with
         * read-only access.
         *
         * @param name Name or path of subkey to open.
         * @param readonly Set to <b>true</b> if you only need readonly access.
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */
        [System.Security.SecuritySafeCritical]
        public RegistryKey OpenSubKey(string name, bool writable)
        {
            return InternalOpenSubKey(name, GetRegistryKeyAccess(writable));
        }

        public RegistryKey OpenSubKey(String name, RegistryRights rights)
        {
            return InternalOpenSubKey(name, (int)rights);
        }

        [System.Security.SecurityCritical]  
        private RegistryKey InternalOpenSubKey(String name, int rights)
        {
            ValidateKeyName(name);

            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            SafeRegistryHandle result = null;
            int ret = Interop.mincore.RegOpenKeyEx(hkey, name, 0, (rights | (int)regView), out result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, IsWritable(rights), false, remoteKey, false, regView);
                key.keyName = keyName + "\\" + name;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Interop.mincore.Errors.ERROR_ACCESS_DENIED || ret == Interop.mincore.Errors.ERROR_BAD_IMPERSONATION_LEVEL)
            {
                // We need to throw SecurityException here for compatibility reason,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(SR.Security_RegistryPermission);
            }

            return null;
        }

        // This required no security checks. This is to get around the Deleting SubKeys which only require
        // write permission. They call OpenSubKey which required read. Now instead call this function w/o security checks
        [System.Security.SecurityCritical]  
        internal RegistryKey InternalOpenSubKey(String name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();

            SafeRegistryHandle result = null;
            int ret = Interop.mincore.RegOpenKeyEx(hkey,
                name,
                0,
                GetRegistryKeyAccess(writable) | (int)regView,
                out result);

            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false, regView);
                key.keyName = keyName + "\\" + name;
                return key;
            }
            return null;
        }

        /**
         * Returns a subkey with read only permissions.
         *
         * @param name Name or path of subkey to open.
         *
         * @return the Subkey requested, or <b>null</b> if the operation failed.
         */
        [System.Security.SecurityCritical]
        public RegistryKey OpenSubKey(String name)
        {
            return OpenSubKey(name, false);
        }

        /**
         * Retrieves the count of subkeys.
         *
         * @return a count of subkeys.
         */
        public int SubKeyCount
        {
            [System.Security.SecuritySafeCritical]  
            get
            {
                return InternalSubKeyCount();
            }
        }

        public RegistryView View
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                EnsureNotDisposed();
                return regView;
            }
        }

        public SafeRegistryHandle Handle
        {
            [System.Security.SecurityCritical]
            get
            {
                EnsureNotDisposed();
                int ret = Interop.mincore.Errors.ERROR_INVALID_HANDLE;
                if (IsSystemKey())
                {
                    IntPtr baseKey = (IntPtr)0;
                    switch (keyName)
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
                    ret = Interop.mincore.RegOpenKeyEx(baseKey,
                        null,
                        0,
                        GetRegistryKeyAccess(IsWritable()) | (int)regView,
                        out result);

                    if (ret == 0 && !result.IsInvalid)
                    {
                        return result;
                    }
                    else
                    {
                        Win32Error(ret, null);
                    }
                }
                else
                {
                    return hkey;
                }
                throw new IOException(Interop.mincore.GetMessage(ret), ret);
            }
        }

        [System.Security.SecurityCritical]
        public static RegistryKey FromHandle(SafeRegistryHandle handle)
        {
            return FromHandle(handle, RegistryView.Default);
        }

        [System.Security.SecurityCritical]
        public static RegistryKey FromHandle(SafeRegistryHandle handle, RegistryView view)
        {
            if (handle == null) throw new ArgumentNullException(nameof(handle));
            ValidateKeyView(view);

            return new RegistryKey(handle, true /* isWritable */, view);
        }

        [System.Security.SecurityCritical]  
        internal int InternalSubKeyCount()
        {
            EnsureNotDisposed();

            int subkeys = 0;
            int junk = 0;
            int ret = Interop.mincore.RegQueryInfoKey(hkey,
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
                Win32Error(ret, null);
            return subkeys;
        }

        /**
         * Retrieves an array of strings containing all the subkey names.
         *
         * @return all subkey names.
         */
        [System.Security.SecurityCritical] 
        public String[] GetSubKeyNames()
        {
            return InternalGetSubKeyNames();
        }

        [System.Security.SecurityCritical]  
        internal unsafe String[] InternalGetSubKeyNames()
        {
            EnsureNotDisposed();
            int subkeys = InternalSubKeyCount();

            if (subkeys > 0)
            {
                String[] names = new String[subkeys];
                char[] name = new char[MaxKeyLength + 1];

                int namelen;

                fixed (char* namePtr = &name[0])
                {
                    for (int i = 0; i < subkeys; i++)
                    {
                        namelen = name.Length; // Don't remove this. The API's doesn't work if this is not properly initialized.
                        int ret = Interop.mincore.RegEnumKeyEx(hkey,
                            i,
                            namePtr,
                            ref namelen,
                            null,
                            null,
                            null,
                            null);
                        if (ret != 0)
                            Win32Error(ret, null);
                        names[i] = new String(namePtr);
                    }
                }

                return names;
            }

            return Array.Empty<String>();
        }

        /**
         * Retrieves the count of values.
         *
         * @return a count of values.
         */
        public int ValueCount
        {
            [System.Security.SecuritySafeCritical]  
            get
            {
                return InternalValueCount();
            }
        }

        [System.Security.SecurityCritical]  
        internal int InternalValueCount()
        {
            EnsureNotDisposed();
            int values = 0;
            int junk = 0;
            int ret = Interop.mincore.RegQueryInfoKey(hkey,
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
                Win32Error(ret, null);
            return values;
        }

        /**
         * Retrieves an array of strings containing all the value names.
         *
         * @return all value names.
         */
        [System.Security.SecuritySafeCritical]  
        public unsafe String[] GetValueNames()
        {
            EnsureNotDisposed();

            int values = InternalValueCount();

            if (values > 0)
            {
                String[] names = new String[values];
                char[] name = new char[MaxValueLength + 1];
                int namelen;

                fixed (char* namePtr = &name[0])
                {
                    for (int i = 0; i < values; i++)
                    {
                        namelen = name.Length;

                        int ret = Interop.mincore.RegEnumValue(hkey,
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
                            if (!(IsPerfDataKey() && ret == Interop.mincore.Errors.ERROR_MORE_DATA))
                                Win32Error(ret, null);
                        }

                        names[i] = new String(namePtr);
                    }
                }

                return names;
            }

            return Array.Empty<String>();
        }

        /**
         * Retrieves the specified value. <b>null</b> is returned if the value
         * doesn't exist.
         *
         * Note that <var>name</var> can be null or "", at which point the
         * unnamed or default value of this Registry key is returned, if any.
         *
         * @param name Name of value to retrieve.
         *
         * @return the data associated with the value.
         */
        [System.Security.SecuritySafeCritical]  
        public Object GetValue(String name)
        {
            return InternalGetValue(name, null, false, true);
        }

        /**
         * Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.
         *
         * Note that <var>name</var> can be null or "", at which point the
         * unnamed or default value of this Registry key is returned, if any.
         * The default values for RegistryKeys are OS-dependent.  NT doesn't
         * have them by default, but they can exist and be of any type.  On
         * Win95, the default value is always an empty key of type REG_SZ.
         * Win98 supports default values of any type, but defaults to REG_SZ.
         *
         * @param name Name of value to retrieve.
         * @param defaultValue Value to return if <i>name</i> doesn't exist.
         *
         * @return the data associated with the value.
         */
        [System.Security.SecuritySafeCritical]
        public Object GetValue(String name, Object defaultValue)
        {
            return InternalGetValue(name, defaultValue, false, true);
        }

        [System.Security.SecuritySafeCritical]
        public Object GetValue(String name, Object defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        [System.Security.SecurityCritical]  
        internal Object InternalGetValue(String name, Object defaultValue, bool doNotExpand, bool checkSecurity)
        {
            if (checkSecurity)
            {
                // Name can be null!  It's the most common use of RegQueryValueEx
                EnsureNotDisposed();
            }

            Object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0)
            {
                if (IsPerfDataKey())
                {
                    int size = 65000;
                    int sizeInput = size;

                    int r;
                    byte[] blob = new byte[size];
                    while (Interop.mincore.Errors.ERROR_MORE_DATA == (r = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, blob, ref sizeInput)))
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
                        Win32Error(r, name);
                    return blob;
                }
                else
                {
                    // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                    // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                    // want to continue on through the function. 
                    if (ret != Interop.mincore.Errors.ERROR_MORE_DATA)
                        return data;
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
                case Interop.mincore.RegistryValues.REG_NONE:
                case Interop.mincore.RegistryValues.REG_DWORD_BIG_ENDIAN:
                case Interop.mincore.RegistryValues.REG_BINARY:
                    {
                        byte[] blob = new byte[datasize];
                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                        data = blob;
                    }
                    break;
                case Interop.mincore.RegistryValues.REG_QWORD:
                    {    // also REG_QWORD_LITTLE_ENDIAN
                        if (datasize > 8)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(long)
                            goto case Interop.mincore.RegistryValues.REG_BINARY;
                        }
                        long blob = 0;
                        Debug.Assert(datasize == 8, "datasize==8");
                        // Here, datasize must be 8 when calling this
                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;
                case Interop.mincore.RegistryValues.REG_DWORD:
                    {    // also REG_DWORD_LITTLE_ENDIAN
                        if (datasize > 4)
                        {
                            // prevent an AV in the edge case that datasize is larger than sizeof(int)
                            goto case Interop.mincore.RegistryValues.REG_QWORD;
                        }
                        int blob = 0;
                        Debug.Assert(datasize == 4, "datasize==4");
                        // Here, datasize must be four when calling this
                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                        data = blob;
                    }
                    break;

                case Interop.mincore.RegistryValues.REG_SZ:
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

                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new String(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new String(blob);
                        }
                    }
                    break;

                case Interop.mincore.RegistryValues.REG_EXPAND_SZ:
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

                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                        if (blob.Length > 0 && blob[blob.Length - 1] == (char)0)
                        {
                            data = new String(blob, 0, blob.Length - 1);
                        }
                        else
                        {
                            // in the very unlikely case the data is missing null termination, 
                            // pass in the whole char[] to prevent truncating a character
                            data = new String(blob);
                        }

                        if (!doNotExpand)
                            data = Environment.ExpandEnvironmentVariables((String)data);
                    }
                    break;
                case Interop.mincore.RegistryValues.REG_MULTI_SZ:
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

                        ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                        // make sure the string is null terminated before processing the data
                        if (blob.Length > 0 && blob[blob.Length - 1] != (char)0)
                        {
                            Array.Resize(ref blob, blob.Length + 1);
                        }

                        var strings = new List<String>();
                        int cur = 0;
                        int len = blob.Length;

                        while (ret == 0 && cur < len)
                        {
                            int nextNull = cur;
                            while (nextNull < len && blob[nextNull] != (char)0)
                            {
                                nextNull++;
                            }

                            if (nextNull < len)
                            {
                                Debug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                if (nextNull - cur > 0)
                                {
                                    strings.Add(new String(blob, cur, nextNull - cur));
                                }
                                else
                                {
                                    // we found an empty string.  But if we're at the end of the data, 
                                    // it's just the extra null terminator. 
                                    if (nextNull != len - 1)
                                        strings.Add(String.Empty);
                                }
                            }
                            else
                            {
                                strings.Add(new String(blob, cur, len - cur));
                            }
                            cur = nextNull + 1;
                        }

                        data = strings.ToArray();
                    }
                    break;
                case Interop.mincore.RegistryValues.REG_LINK:
                default:
                    break;
            }

            return data;
        }

        [System.Security.SecuritySafeCritical]  
        public RegistryValueKind GetValueKind(string name)
        {
            EnsureNotDisposed();

            int type = 0;
            int datasize = 0;
            int ret = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null);
            if (type == Interop.mincore.RegistryValues.REG_NONE)
                return RegistryValueKind.None;
            else if (!Enum.IsDefined(typeof(RegistryValueKind), type))
                return RegistryValueKind.Unknown;
            else
                return (RegistryValueKind)type;
        }

        /**
         * Retrieves the current state of the dirty property.
         *
         * A key is marked as dirty if any operation has occurred that modifies the
         * contents of the key.
         *
         * @return <b>true</b> if the key has been modified.
         */
        private bool IsDirty()
        {
            return (this.state & STATE_DIRTY) != 0;
        }

        private bool IsSystemKey()
        {
            return (this.state & STATE_SYSTEMKEY) != 0;
        }

        private bool IsWritable()
        {
            return (this.state & STATE_WRITEACCESS) != 0;
        }

        private bool IsPerfDataKey()
        {
            return (this.state & STATE_PERF_DATA) != 0;
        }

        public String Name
        {
            [System.Security.SecuritySafeCritical]  
            get
            {
                EnsureNotDisposed();
                return keyName;
            }
        }

        private void SetDirty()
        {
            this.state |= STATE_DIRTY;
        }

        /**
         * Sets the specified value.
         *
         * @param name Name of value to store data in.
         * @param value Data to store.
         */
        public void SetValue(String name, Object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        [System.Security.SecuritySafeCritical] 
        public unsafe void SetValue(String name, Object value, RegistryValueKind valueKind)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException("value");

            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(SR.Arg_RegValStrLenBug);
            }

            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
                throw new ArgumentException(SR.Arg_RegBadKeyKind, nameof(valueKind));

            EnsureWriteable();

            if (valueKind == RegistryValueKind.Unknown)
            {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value);
            }

            int ret = 0;
            try
            {
                switch (valueKind)
                {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            String data = value.ToString();
                            ret = Interop.mincore.RegSetValueEx(hkey,
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

                            ret = Interop.mincore.RegSetValueEx(hkey,
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
                        ret = Interop.mincore.RegSetValueEx(hkey,
                            name,
                            0,
                            (valueKind == RegistryValueKind.None ? Interop.mincore.RegistryValues.REG_NONE : RegistryValueKind.Binary),
                            dataBytes,
                            dataBytes.Length);
                        break;

                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail.
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Interop.mincore.RegSetValueEx(hkey,
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

                            ret = Interop.mincore.RegSetValueEx(hkey,
                                name,
                                0,
                                RegistryValueKind.QWord,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (OverflowException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
            }
            catch (InvalidOperationException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
            }
            catch (FormatException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
            }
            catch (InvalidCastException)
            {
                ThrowHelper.ThrowArgumentException(SR.Arg_RegSetMismatchedKind);
            }

            if (ret == 0)
            {
                SetDirty();
            }
            else
                Win32Error(ret, null);
        }

        private RegistryValueKind CalculateValueKind(Object value)
        {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array)
            {
                if (value is byte[])
                    return RegistryValueKind.Binary;
                else if (value is String[])
                    return RegistryValueKind.MultiString;
                else
                    throw new ArgumentException(SR.Format(SR.Arg_RegSetBadArrType, value.GetType().Name));
            }
            else
                return RegistryValueKind.String;
        }

        /**
         * Retrieves a string representation of this key.
         *
         * @return a string representing the key.
         */
        [System.Security.SecuritySafeCritical]  
        public override String ToString()
        {
            EnsureNotDisposed();
            return keyName;
        }

        /**
         * After calling GetLastWin32Error(), it clears the last error field,
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager.
         */
        [System.Security.SecuritySafeCritical]  
        internal void Win32Error(int errorCode, String str)
        {
            switch (errorCode)
            {
                case Interop.mincore.Errors.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
                    else
                        throw new UnauthorizedAccessException();

                case Interop.mincore.Errors.ERROR_INVALID_HANDLE:
                    /**
                     * For normal RegistryKey instances we dispose the SafeRegHandle and throw IOException.
                     * However, for HKEY_PERFORMANCE_DATA (on a local or remote machine) we avoid disposing the
                     * SafeRegHandle and only throw the IOException.  This is to workaround reentrancy issues
                     * in PerformanceCounter.NextValue() where the API could throw {NullReference, ObjectDisposed, ArgumentNull}Exception
                     * on reentrant calls because of this error code path in RegistryKey
                     *
                     * Normally we'd make our caller synchronize access to a shared RegistryKey instead of doing something like this,
                     * however we shipped PerformanceCounter.NextValue() un-synchronized in v2.0RTM and customers have taken a dependency on 
                     * this behavior (being able to simultaneously query multiple remote-machine counters on multiple threads, instead of 
                     * having serialized access).
                     *
                     */
                    if (!IsPerfDataKey())
                    {
                        this.hkey.SetHandleAsInvalid();
                        this.hkey = null;
                    }
                    goto default;

                case Interop.mincore.Errors.ERROR_FILE_NOT_FOUND:
                    throw new IOException(SR.Arg_RegKeyNotFound, errorCode);

                default:
                    throw new IOException(Interop.mincore.GetMessage(errorCode), errorCode);
            }
        }

        [System.Security.SecuritySafeCritical]
        internal static void Win32ErrorStatic(int errorCode, String str)
        {
            switch (errorCode)
            {
                case Interop.mincore.Errors.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_RegistryKeyGeneric_Key, str));
                    else
                        throw new UnauthorizedAccessException();

                default:
                    throw new IOException(Interop.mincore.GetMessage(errorCode), errorCode);
            }
        }

        internal static String FixupName(String name)
        {
            Debug.Assert(name != null, "[FixupName]name!=null");
            if (name.IndexOf('\\') == -1)
                return name;

            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (temp >= 0 && sb[temp] == '\\') // Remove trailing slash
                sb.Length = temp;
            return sb.ToString();
        }


        private static void FixupPath(StringBuilder path)
        {
            Contract.Requires(path != null);
            int length = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;

            int i = 1;
            while (i < length - 1)
            {
                if (path[i] == '\\')
                {
                    i++;
                    while (i < length)
                    {
                        if (path[i] == '\\')
                        {
                            path[i] = markerChar;
                            i++;
                            fixup = true;
                        }
                        else
                            break;
                    }
                }
                i++;
            }

            if (fixup)
            {
                i = 0;
                int j = 0;
                while (i < length)
                {
                    if (path[i] == markerChar)
                    {
                        i++;
                        continue;
                    }
                    path[j] = path[i];
                    i++;
                    j++;
                }
                path.Length += j - i;
            }
        }

        [System.Security.SecurityCritical]  
        private bool ContainsRegistryValue(string name)
        {
            int type = 0;
            int datasize = 0;
            int retval = Interop.mincore.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            return retval == 0;
        }

        [System.Security.SecurityCritical]  
        private void EnsureNotDisposed()
        {
            if (hkey == null)
            {
                ThrowHelper.ThrowObjectDisposedException(keyName, SR.ObjectDisposed_RegKeyClosed);
            }
        }

        [System.Security.SecurityCritical]  
        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                ThrowHelper.ThrowUnauthorizedAccessException(SR.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        static int GetRegistryKeyAccess(bool isWritable)
        {
            int winAccess;
            if (!isWritable)
            {
                winAccess = Interop.mincore.RegistryOperations.KEY_READ;
            }
            else
            {
                winAccess = Interop.mincore.RegistryOperations.KEY_READ | Interop.mincore.RegistryOperations.KEY_WRITE;
            }

            return winAccess;
        }

        static bool IsWritable(int rights)
        {
            return (rights & (Interop.mincore.RegistryOperations.KEY_SET_VALUE |
                              Interop.mincore.RegistryOperations.KEY_CREATE_SUB_KEY |
                              (int)RegistryRights.Delete |
                              (int)RegistryRights.TakeOwnership |
                              (int)RegistryRights.ChangePermissions)) != 0;
        }

        static private void ValidateKeyName(string name)
        {
            Contract.Ensures(name != null);
            if (name == null)
            {
                ThrowHelper.ThrowArgumentNullException("name");
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0;
            while (nextSlash != -1)
            {
                if ((nextSlash - current) > MaxKeyLength)
                    ThrowHelper.ThrowArgumentException(SR.Arg_RegKeyStrLenBug);

                current = nextSlash + 1;
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength)
                ThrowHelper.ThrowArgumentException(SR.Arg_RegKeyStrLenBug);
        }

        static private void ValidateKeyOptions(RegistryOptions options)
        {
            if (options < RegistryOptions.None || options > RegistryOptions.Volatile)
            {
                ThrowHelper.ThrowArgumentException(SR.Argument_InvalidRegistryOptionsCheck, "options");
            }
        }

        static private void ValidateKeyView(RegistryView view)
        {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
            {
                ThrowHelper.ThrowArgumentException(SR.Argument_InvalidRegistryViewCheck, "view");
            }
        }
    }

    [Flags]
    public enum RegistryValueOptions
    {
        None = 0,
        DoNotExpandEnvironmentNames = 1
    }
}
