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
using System.Security.AccessControl;
using System.Text;
using Microsoft.Win32.SafeHandles;

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
    /// <summary>Registry encapsulation. To get an instance of a RegistryKey use the Registry class's static members then call OpenSubKey.</summary>
    public sealed partial class RegistryKey : MarshalByRefObject, IDisposable
    {
        private static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
        private static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
        private static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
        private static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));
        private static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

        /// <summary>Names of keys.  This array must be in the same order as the HKEY values listed above.</summary>
        private static readonly string[] s_hkeyNames = new string[]
        {
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

        private volatile SafeRegistryHandle _hkey;
        private volatile string _keyName;
        private readonly bool _remoteKey;
        private volatile StateFlags _state;
        private volatile RegistryKeyPermissionCheck _checkMode;
        private readonly RegistryView _regView = RegistryView.Default;

        /// <summary>
        /// Creates a RegistryKey. This key is bound to hkey, if writable is <b>false</b> then no write operations will be allowed.
        /// </summary>
        private RegistryKey(SafeRegistryHandle hkey, bool writable, RegistryView view) :
            this(hkey, writable, false, false, false, view)
        {
        }

        /// <summary>
        /// Creates a RegistryKey.
        /// This key is bound to hkey, if writable is <b>false</b> then no write operations
        /// will be allowed. If systemkey is set then the hkey won't be released
        /// when the object is GC'ed.
        /// The remoteKey flag when set to true indicates that we are dealing with registry entries
        /// on a remote machine and requires the program making these calls to have full trust.
        /// </summary>
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view)
        {
            ValidateKeyView(view);

            _hkey = hkey;
            _keyName = "";
            _remoteKey = remoteKey;
            _regView = view;

            if (systemkey)
            {
                _state |= StateFlags.SystemKey;
            }
            if (writable)
            {
                _state |= StateFlags.WriteAccess;
            }
            if (isPerfData)
            {
                _state |= StateFlags.PerfData;
            }
        }

        public void Flush()
        {
            if (_hkey != null && IsDirty())
            {
                Interop.Advapi32.RegFlushKey(_hkey);
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_hkey != null)
            {
                if (!IsSystemKey())
                {
                    try
                    {
                        _hkey.Dispose();
                    }
                    catch (IOException)
                    {
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        _hkey = null!;
                    }
                }
                else if (IsPerfDataKey())
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
            }
        }

        /// <summary>Creates a new subkey, or opens an existing one.</summary>
        /// <param name="subkey">Name or path to subkey to create or open.</param>
        /// <returns>The subkey, or <b>null</b> if the operation failed.</returns>
        public RegistryKey CreateSubKey(string subkey)
        {
            return CreateSubKey(subkey, _checkMode);
        }

        public RegistryKey CreateSubKey(string subkey, bool writable)
        {
            return CreateSubKey(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, bool writable, RegistryOptions options)
        {
            return CreateSubKey(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, options);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck)
        {
            return CreateSubKey(subkey, permissionCheck, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions, RegistrySecurity? registrySecurity)
        {
            return CreateSubKey(subkey, permissionCheck, registryOptions);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistrySecurity? registrySecurity)
        {
            return CreateSubKey(subkey, permissionCheck, RegistryOptions.None);
        }

        public RegistryKey CreateSubKey(string subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions registryOptions)
        {
            ValidateKeyOptions(registryOptions);
            ValidateKeyName(subkey);
            ValidateKeyMode(permissionCheck);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // only keys opened under read mode is not writable
            if (!_remoteKey)
            {
                RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree));
                if (key != null)
                {
                    // Key already exits
                    key._checkMode = permissionCheck;
                    return key;
                }
            }

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

        /// <summary>
        /// Deletes the specified subkey. Will throw an exception if the subkey has
        /// subkeys. To delete a tree of subkeys use, DeleteSubKeyTree.
        /// </summary>
        /// <param name="subkey">SubKey to delete.</param>
        /// <exception cref="InvalidOperationException">Thrown if the subkey has child subkeys.</exception>
        public void DeleteSubKey(string subkey)
        {
            DeleteSubKey(subkey, true);
        }

        public void DeleteSubKey(string subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open.
            //
            RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, false);
            if (key != null)
            {
                using (key)
                {
                    if (key.SubKeyCount > 0)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_RegRemoveSubKey);
                    }
                }

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
            else // there is no key which also means there is no subkey
            {
                if (throwOnMissingSubKey)
                {
                    throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
                }
            }
        }

        /// <summary>Recursively deletes a subkey and any child subkeys.</summary>
        /// <param name="subkey">SubKey to delete.</param>
        public void DeleteSubKeyTree(string subkey)
        {
            DeleteSubKeyTree(subkey, throwOnMissingSubKey: true);
        }

        public void DeleteSubKeyTree(string subkey, bool throwOnMissingSubKey)
        {
            ValidateKeyName(subkey);

            // Security concern: Deleting a hive's "" subkey would delete all
            // of that hive's contents.  Don't allow "".
            if (subkey.Length == 0 && IsSystemKey())
            {
                throw new ArgumentException(SR.Arg_RegKeyDelHive);
            }

            EnsureWriteable();

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash

            // If the key has values, it must be opened with KEY_SET_VALUE,
            // or RegDeleteTree will fail with ERROR_ACCESS_DENIED.

            RegistryKey? key = InternalOpenSubKeyWithoutSecurityChecks(subkey, true);
            if (key != null)
            {
                using (key)
                {
                    int ret = Interop.Advapi32.RegDeleteTree(key._hkey, string.Empty);
                    if (ret != 0)
                    {
                        Win32Error(ret, null);
                    }

                    // RegDeleteTree doesn't self-delete when lpSubKey is empty.
                    // Manually delete the key to restore old behavior.

                    ret = Interop.Advapi32.RegDeleteKeyEx(key._hkey, string.Empty, (int)_regView, 0);
                    if (ret != 0)
                    {
                        Win32Error(ret, null);
                    }
                }
            }
            else
            {
                if (throwOnMissingSubKey)
                {
                    throw new ArgumentException(SR.Arg_RegSubKeyAbsent);
                }
            }
        }

        /// <summary>Deletes the specified value from this key.</summary>
        /// <param name="name">Name of value to delete.</param>
        public void DeleteValue(string name)
        {
            DeleteValue(name, true);
        }

        public void DeleteValue(string name, bool throwOnMissingValue)
        {
            EnsureWriteable();
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
        /// Retrieves a new <see cref="RegistryKey"/>  that represents the requested <see cref="RegistryHive"/> .
        /// </summary>
        /// <param name="hKey">The hive to open.</param>
        /// <param name="view">Which view over the registry to employ.</param>
        /// <returns>The <see cref="RegistryKey"/> requested.</returns>
        public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view)
        {
            ValidateKeyView(view);

            nint nKey = (int)hKey;

            int index = ((int)nKey) & 0x0FFFFFFF;
            Debug.Assert(index >= 0 && index < s_hkeyNames.Length, "index is out of range!");
            Debug.Assert((((int)nKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            bool isPerf = nKey == HKEY_PERFORMANCE_DATA;

            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA.
            SafeRegistryHandle srh = new SafeRegistryHandle(nKey, isPerf);

            RegistryKey key = new RegistryKey(srh, true, true, false, isPerf, view);
            key._checkMode = RegistryKeyPermissionCheck.Default;
            key._keyName = s_hkeyNames[index];
            return key;
        }

        /// <summary>Retrieves a new RegistryKey that represents the requested key on a foreign machine.</summary>
        /// <param name="hKey">hKey HKEY_* to open.</param>
        /// <param name="machineName">Name the machine to connect to.</param>
        /// <returns>The RegistryKey requested.</returns>
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName)
        {
            return OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
        }

        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, string machineName, RegistryView view)
        {
            ArgumentNullException.ThrowIfNull(machineName);

            ValidateKeyView(view);

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

        /// <summary>Returns a subkey with read only permissions.</summary>
        /// <param name="name">Name or path of subkey to open.</param>
        /// <returns>The Subkey requested, or <b>null</b> if the operation failed.</returns>
        public RegistryKey? OpenSubKey(string name)
        {
            return OpenSubKey(name, false);
        }

        /// <summary>
        /// Retrieves a subkey. If readonly is <b>true</b>, then the subkey is opened with
        /// read-only access.
        /// </summary>
        /// <param name="name">Name or the path of subkey to open.</param>
        /// <param name="writable">Set to <b>true</b> if you only need readonly access.</param>
        /// <returns>the Subkey requested, or <b>null</b> if the operation failed.</returns>
        public RegistryKey? OpenSubKey(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name);

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

        public RegistryKey? OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck)
        {
            ValidateKeyMode(permissionCheck);

            return OpenSubKey(name, permissionCheck, (RegistryRights)GetRegistryKeyAccess(permissionCheck));
        }

        public RegistryKey? OpenSubKey(string name, RegistryRights rights)
        {
            return OpenSubKey(name, _checkMode, rights);
        }

        public RegistryKey? OpenSubKey(string name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights)
        {
            ValidateKeyName(name);
            ValidateKeyMode(permissionCheck);

            ValidateKeyRights(rights);

            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, (int)rights | (int)_regView, out SafeRegistryHandle result);
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

        internal RegistryKey? InternalOpenSubKeyWithoutSecurityChecks(string name, bool writable)
        {
            ValidateKeyName(name);
            EnsureNotDisposed();

            int ret = Interop.Advapi32.RegOpenKeyEx(_hkey, name, 0, GetRegistryKeyAccess(writable) | (int)_regView, out SafeRegistryHandle result);
            if (ret == 0 && !result.IsInvalid)
            {
                RegistryKey key = new RegistryKey(result, writable, false, _remoteKey, false, _regView);
                key._keyName = _keyName + "\\" + name;
                return key;
            }

            result.Dispose();

            return null;
        }

        public RegistrySecurity GetAccessControl()
        {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public RegistrySecurity GetAccessControl(AccessControlSections includeSections)
        {
            EnsureNotDisposed();
            return new RegistrySecurity(Handle, includeSections);
        }

        public void SetAccessControl(RegistrySecurity registrySecurity)
        {
            EnsureWriteable();
            ArgumentNullException.ThrowIfNull(registrySecurity);

            registrySecurity.Persist(Handle);
        }

        /// <summary>Retrieves the count of subkeys.</summary>
        /// <returns>A count of subkeys.</returns>
        public int SubKeyCount
        {
            get
            {
                EnsureNotDisposed();
                int subkeys = 0;
                int junk = 0;
                int ret = Interop.Advapi32.RegQueryInfoKey(_hkey,
                                          null,
                                          null,
                                          0,
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
        }

        public RegistryView View
        {
            get
            {
                EnsureNotDisposed();
                return _regView;
            }
        }

        public SafeRegistryHandle Handle
        {
            get
            {
                EnsureNotDisposed();
                return IsSystemKey() ? GetSystemKeyHandle() : _hkey;
            }
        }

        private SafeRegistryHandle GetSystemKeyHandle()
        {
            Debug.Assert(IsSystemKey());

            int ret = Interop.Errors.ERROR_INVALID_HANDLE;
            IntPtr baseKey = 0;
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

        public static RegistryKey FromHandle(SafeRegistryHandle handle)
        {
            return FromHandle(handle, RegistryView.Default);
        }

        public static RegistryKey FromHandle(SafeRegistryHandle handle, RegistryView view)
        {
            ArgumentNullException.ThrowIfNull(handle);

            ValidateKeyView(view);

            return new RegistryKey(handle, writable: true, view: view);
        }

        /// <summary>Retrieves an array of strings containing all the subkey names.</summary>
        /// <returns>All subkey names.</returns>
        public string[] GetSubKeyNames()
        {
            int subkeys = SubKeyCount;

            if (subkeys <= 0)
            {
                return Array.Empty<string>();
            }

            string[] names = new string[subkeys];
            Span<char> nameSpan = stackalloc char[MaxKeyLength + 1];

            int result;
            int nameLength = nameSpan.Length;
            int cpt = 0;

            while ((result = Interop.Advapi32.RegEnumKeyEx(
                _hkey,
                cpt,
                ref MemoryMarshal.GetReference(nameSpan),
                ref nameLength,
                null,
                null,
                null,
                null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
            {
                switch (result)
                {
                    case Interop.Errors.ERROR_SUCCESS:
                        if (cpt >= names.Length) // possible new item during loop
                        {
                            Array.Resize(ref names, names.Length * 2);
                        }

                        names[cpt++] = new string(nameSpan.Slice(0, nameLength));
                        nameLength = nameSpan.Length;
                        break;

                    default:
                        // Throw the error
                        Win32Error(result, null);
                        break;
                }
            }

            // Shrink array to fit found items, if necessary
            if (cpt < names.Length)
            {
                Array.Resize(ref names, cpt);
            }

            return names;
        }

        /// <summary>Retrieves the count of values.</summary>
        /// <returns>A count of values.</returns>
        public int ValueCount
        {
            get
            {
                EnsureNotDisposed();
                int values = 0;
                int junk = 0;
                int ret = Interop.Advapi32.RegQueryInfoKey(_hkey,
                                          null,
                                          null,
                                          0,
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
        }

        /// <summary>Retrieves an array of strings containing all the value names.</summary>
        /// <returns>All value names.</returns>
        public unsafe string[] GetValueNames()
        {
            int values = ValueCount;

            if (values <= 0)
            {
                return Array.Empty<string>();
            }

            string[] names = new string[values];

            // Names in the registry aren't usually very long, although they can go to as large
            // as 16383 characters (MaxValueLength).
            //
            // Every call to RegEnumValue will allocate another buffer to get the data from
            // NtEnumerateValueKey before copying it back out to our passed in buffer. This can
            // add up quickly- we'll try to keep the memory pressure low and grow the buffer
            // only if needed.

            char[]? name = ArrayPool<char>.Shared.Rent(100);
            int cpt = 0;

            try
            {
                int result;
                int nameLength = name.Length;

                while ((result = Interop.Advapi32.RegEnumValue(
                    _hkey,
                    cpt,
                    name,
                    ref nameLength,
                    0,
                    null,
                    null,
                    null)) != Interop.Errors.ERROR_NO_MORE_ITEMS)
                {
                    switch (result)
                    {
                        // The size is only ever reported back correctly in the case
                        // of ERROR_SUCCESS. It will almost always be changed, however.
                        case Interop.Errors.ERROR_SUCCESS:
                            if (cpt >= names.Length) // possible new item during loop
                            {
                                Array.Resize(ref names, names.Length * 2);
                            }

                            names[cpt++] = new string(name, 0, nameLength);
                            break;
                        case Interop.Errors.ERROR_MORE_DATA:
                            if (IsPerfDataKey())
                            {
                                // Enumerating the values for Perf keys always returns
                                // ERROR_MORE_DATA, but has a valid name. Buffer does need
                                // to be big enough however. 8 characters is the largest
                                // known name. The size isn't returned, but the string is
                                // null terminated.

                                if (cpt >= names.Length) // possible new item during loop
                                {
                                    Array.Resize(ref names, names.Length * 2);
                                }

                                fixed (char* c = &name[0])
                                {
                                    names[cpt++] = new string(c);
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

            // Shrink array to fit found items, if necessary
            if (cpt < names.Length)
            {
                Array.Resize(ref names, cpt);
            }

            return names;
        }

        /// <summary>Retrieves the specified value. <b>null</b> is returned if the value doesn't exist</summary>
        /// <remarks>
        /// Note that <var>name</var> can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.
        /// </remarks>
        /// <param name="name">Name of value to retrieve.</param>
        /// <returns>The data associated with the value.</returns>
        public object? GetValue(string? name)
        {
            return InternalGetValue(name, null, false);
        }

        /// <summary>Retrieves the specified value. <i>defaultValue</i> is returned if the value doesn't exist.</summary>
        /// <remarks>
        /// Note that <var>name</var> can be null or "", at which point the
        /// unnamed or default value of this Registry key is returned, if any.
        /// The default values for RegistryKeys are OS-dependent.  NT doesn't
        /// have them by default, but they can exist and be of any type.  On
        /// Win95, the default value is always an empty key of type REG_SZ.
        /// Win98 supports default values of any type, but defaults to REG_SZ.
        /// </remarks>
        /// <param name="name">Name of value to retrieve.</param>
        /// <param name="defaultValue">Value to return if <i>name</i> doesn't exist.</param>
        /// <returns>The data associated with the value.</returns>
        [return: NotNullIfNotNull(nameof(defaultValue))]
        public object? GetValue(string? name, object? defaultValue)
        {
            return InternalGetValue(name, defaultValue, false);
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        public object? GetValue(string? name, object? defaultValue, RegistryValueOptions options)
        {
            if (options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)options), nameof(options));
            }
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            return InternalGetValue(name, defaultValue, doNotExpand);
        }

        [return: NotNullIfNotNull(nameof(defaultValue))]
        private unsafe object? InternalGetValue(string? name, object? defaultValue, bool doNotExpand)
        {
            EnsureNotDisposed();

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

        public unsafe RegistryValueKind GetValueKind(string? name)
        {
            EnsureNotDisposed();
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

        public string Name
        {
            get
            {
                EnsureNotDisposed();
                return _keyName;
            }
        }

        /// <summary>Sets the specified value.</summary>
        /// <param name="name">Name of value to store data in.</param>
        /// <param name="value">Data to store.</param>
        public void SetValue(string? name, object value)
        {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        public unsafe void SetValue(string? name, object value, RegistryValueKind valueKind)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (name != null && name.Length > MaxValueLength)
            {
                throw new ArgumentException(SR.Arg_RegValStrLenBug, nameof(name));
            }

            if (!Enum.IsDefined(valueKind))
            {
                throw new ArgumentException(SR.Arg_RegBadKeyKind, nameof(valueKind));
            }

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

        private static RegistryValueKind CalculateValueKind(object value)
        {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is int)
            {
                return RegistryValueKind.DWord;
            }
            else if (value is Array)
            {
                if (value is byte[])
                {
                    return RegistryValueKind.Binary;
                }
                else if (value is string[])
                {
                    return RegistryValueKind.MultiString;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Arg_RegSetBadArrType, value.GetType().Name));
                }
            }
            else
            {
                return RegistryValueKind.String;
            }
        }

        /// <summary>Retrieves a string representation of this key.</summary>
        /// <returns>A string representing the key.</returns>
        public override string ToString()
        {
            EnsureNotDisposed();
            return _keyName;
        }

        private static string FixupName(string name)
        {
            Debug.Assert(name != null, "[FixupName]name!=null");

            if (!name.Contains('\\'))
            {
                return name;
            }

            StringBuilder sb = new StringBuilder(name);
            FixupPath(sb);
            int temp = sb.Length - 1;
            if (temp >= 0 && sb[temp] == '\\') // Remove trailing slash
            {
                sb.Length = temp;
            }

            return sb.ToString();
        }

        private static void FixupPath(StringBuilder path)
        {
            Debug.Assert(path != null);

            int length = path.Length;
            bool fixup = false;
            char markerChar = (char)0xFFFF;

            int i = 1;
            while (i < length - 1)
            {
                if (path[i] == '\\')
                {
                    i++;
                    while (i < length && path[i] == '\\')
                    {
                        path[i] = markerChar;
                        i++;
                        fixup = true;
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

        private void EnsureNotDisposed()
        {
            if (_hkey == null)
            {
                throw new ObjectDisposedException(_keyName, SR.ObjectDisposed_RegKeyClosed);
            }
        }

        private void EnsureWriteable()
        {
            EnsureNotDisposed();
            if (!IsWritable())
            {
                throw new UnauthorizedAccessException(SR.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissionCheck(bool subkeyWritable)
        {
            if (_checkMode == RegistryKeyPermissionCheck.Default)
            {
                return _checkMode;
            }

            if (subkeyWritable)
            {
                return RegistryKeyPermissionCheck.ReadWriteSubTree;
            }
            else
            {
                return RegistryKeyPermissionCheck.ReadSubTree;
            }
        }

        private static void ValidateKeyName(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            int nextSlash = name.IndexOf('\\');
            int current = 0;
            while (nextSlash >= 0)
            {
                if ((nextSlash - current) > MaxKeyLength)
                {
                    throw new ArgumentException(SR.Arg_RegKeyStrLenBug, nameof(name));
                }
                current = nextSlash + 1;
                nextSlash = name.IndexOf('\\', current);
            }

            if ((name.Length - current) > MaxKeyLength)
            {
                throw new ArgumentException(SR.Arg_RegKeyStrLenBug, nameof(name));
            }
        }

        private static void ValidateKeyMode(RegistryKeyPermissionCheck mode)
        {
            if (mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryKeyPermissionCheck, nameof(mode));
            }
        }

        private static void ValidateKeyOptions(RegistryOptions options)
        {
            if (options < RegistryOptions.None || options > RegistryOptions.Volatile)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryOptionsCheck, nameof(options));
            }
        }

        private static void ValidateKeyView(RegistryView view)
        {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64)
            {
                throw new ArgumentException(SR.Argument_InvalidRegistryViewCheck, nameof(view));
            }
        }

        private static void ValidateKeyRights(RegistryRights rights)
        {
            if (0 != (rights & ~RegistryRights.FullControl))
            {
                // We need to throw SecurityException here for compatibility reason,
                // although UnauthorizedAccessException will make more sense.
                throw new SecurityException(SR.Security_RegistryPermission);
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

        /// <summary>Retrieves the current state of the dirty property.</summary>
        /// <remarks>A key is marked as dirty if any operation has occurred that modifies the contents of the key.</remarks>
        /// <returns><b>true</b> if the key has been modified.</returns>
        private bool IsDirty() => (_state & StateFlags.Dirty) != 0;

        private bool IsSystemKey() => (_state & StateFlags.SystemKey) != 0;

        private bool IsWritable() => (_state & StateFlags.WriteAccess) != 0;

        private bool IsPerfDataKey() => (_state & StateFlags.PerfData) != 0;

        private void SetDirty() => _state |= StateFlags.Dirty;

        [Flags]
        private enum StateFlags
        {
            /// <summary>Dirty indicates that we have munged data that should be potentially written to disk.</summary>
            Dirty = 0x0001,
            /// <summary>SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened" or "closed".</summary>
            SystemKey = 0x0002,
            /// <summary>Access</summary>
            WriteAccess = 0x0004,
            /// <summary>Indicates if this key is for HKEY_PERFORMANCE_DATA</summary>
            PerfData = 0x0008
        }
    }
}
