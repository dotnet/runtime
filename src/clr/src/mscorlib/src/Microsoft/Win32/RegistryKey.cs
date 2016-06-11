// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*
  Note on transaction support:
  Eventually we will want to add support for NT's transactions to our
  RegistryKey API's (possibly Whidbey M3?).  When we do this, here's
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
  additional work.  .
 */

/*
  Note on ACL support:
  The key thing to note about ACL's is you set them on a kernel object like a
  registry key, then the ACL only gets checked when you construct handles to 
  them.  So if you set an ACL to deny read access to yourself, you'll still be
  able to read with that handle, but not with new handles.

  Another peculiarity is a Terminal Server app compatibility workaround.  The OS
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


namespace Microsoft.Win32 {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security;
#if FEATURE_MACL
    using System.Security.AccessControl;
#endif
    using System.Security.Permissions;
    using System.Text;
    using System.Threading;
    using System.IO;
    using System.Runtime.Remoting;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.Versioning;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    /**
     * Registry hive values.  Useful only for GetRemoteBaseKey
     */
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum RegistryHive
    {
        ClassesRoot = unchecked((int)0x80000000),
        CurrentUser = unchecked((int)0x80000001),
        LocalMachine = unchecked((int)0x80000002),
        Users = unchecked((int)0x80000003),
        PerformanceData = unchecked((int)0x80000004),
        CurrentConfig = unchecked((int)0x80000005),
#if !FEATURE_CORECLR
        DynData = unchecked((int)0x80000006),
#endif
    }

    /**
     * Registry encapsulation. To get an instance of a RegistryKey use the
     * Registry class's static members then call OpenSubKey.
     *
     * @see Registry
     * @security(checkDllCalls=off)
     * @security(checkClassLinking=on)
     */
#if FEATURE_REMOTING
    [ComVisible(true)]
    public sealed class RegistryKey : MarshalByRefObject, IDisposable 
#else
    [ComVisible(true)]
    public sealed class RegistryKey : IDisposable 
#endif
    {

        // We could use const here, if C# supported ELEMENT_TYPE_I fully.
        internal static readonly IntPtr HKEY_CLASSES_ROOT         = new IntPtr(unchecked((int)0x80000000));
        internal static readonly IntPtr HKEY_CURRENT_USER         = new IntPtr(unchecked((int)0x80000001));
        internal static readonly IntPtr HKEY_LOCAL_MACHINE        = new IntPtr(unchecked((int)0x80000002));
        internal static readonly IntPtr HKEY_USERS                = new IntPtr(unchecked((int)0x80000003));
        internal static readonly IntPtr HKEY_PERFORMANCE_DATA     = new IntPtr(unchecked((int)0x80000004));
        internal static readonly IntPtr HKEY_CURRENT_CONFIG       = new IntPtr(unchecked((int)0x80000005));
#if !FEATURE_CORECLR
        internal static readonly IntPtr HKEY_DYN_DATA             = new IntPtr(unchecked((int)0x80000006));
#endif

        // Dirty indicates that we have munged data that should be potentially
        // written to disk.
        //
        private const int STATE_DIRTY        = 0x0001;

        // SystemKey indicates that this is a "SYSTEMKEY" and shouldn't be "opened"
        // or "closed".
        //
        private const int STATE_SYSTEMKEY    = 0x0002;

        // Access
        //
        private const int STATE_WRITEACCESS  = 0x0004;

        // Indicates if this key is for HKEY_PERFORMANCE_DATA
        private const int STATE_PERF_DATA    = 0x0008;

        // Names of keys.  This array must be in the same order as the HKEY values listed above.
        //
        private static readonly String[] hkeyNames = new String[] {
                "HKEY_CLASSES_ROOT",
                "HKEY_CURRENT_USER",
                "HKEY_LOCAL_MACHINE",
                "HKEY_USERS",
                "HKEY_PERFORMANCE_DATA",
                "HKEY_CURRENT_CONFIG",
#if !FEATURE_CORECLR
                "HKEY_DYN_DATA"
#endif
                };

        // MSDN defines the following limits for registry key names & values:
        // Key Name: 255 characters
        // Value name:  16,383 Unicode characters
        // Value: either 1 MB or current available memory, depending on registry format.
        private const int MaxKeyLength = 255;
        private const int MaxValueLength = 16383;

        [System.Security.SecurityCritical] // auto-generated
        private volatile SafeRegistryHandle hkey = null;
        private volatile int state = 0;
        private volatile String keyName;
        private volatile bool remoteKey = false;
        private volatile RegistryKeyPermissionCheck checkMode;
        private volatile RegistryView regView = RegistryView.Default;

        /**
         * RegistryInternalCheck values.  Useful only for CheckPermission
         */
        private enum RegistryInternalCheck {
            CheckSubKeyWritePermission            = 0,
            CheckSubKeyReadPermission             = 1,
            CheckSubKeyCreatePermission           = 2,
            CheckSubTreeReadPermission            = 3,
            CheckSubTreeWritePermission           = 4,
            CheckSubTreeReadWritePermission       = 5,
            CheckValueWritePermission             = 6,
            CheckValueCreatePermission            = 7,
            CheckValueReadPermission              = 8,
            CheckKeyReadPermission                = 9,
            CheckSubTreePermission                = 10,
            CheckOpenSubKeyWithWritablePermission = 11,
            CheckOpenSubKeyPermission             = 12
        };


        /**
         * Creates a RegistryKey.
         *
         * This key is bound to hkey, if writable is <b>false</b> then no write operations
         * will be allowed.
         */
        [System.Security.SecurityCritical]  // auto-generated
        private RegistryKey(SafeRegistryHandle  hkey, bool writable, RegistryView view)
            : this(hkey, writable, false, false, false, view) {
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
        [System.Security.SecurityCritical]  // auto-generated
        private RegistryKey(SafeRegistryHandle hkey, bool writable, bool systemkey, bool remoteKey, bool isPerfData, RegistryView view) {
            this.hkey = hkey;
            this.keyName = "";
            this.remoteKey = remoteKey;
            this.regView = view;
            if (systemkey) {
                this.state |= STATE_SYSTEMKEY;
            }
            if (writable) {
                this.state |= STATE_WRITEACCESS;
            }
            if (isPerfData)
                this.state |= STATE_PERF_DATA;
            ValidateKeyView(view);
        }

        /**
         * Closes this key, flushes it to disk if the contents have been modified.
         */
        public void Close() {
            Dispose(true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void Dispose(bool disposing) {
            if (hkey != null) {

                if (!IsSystemKey()) {
                    try {
                        hkey.Dispose();
                    }
                    catch (IOException){
                        // we don't really care if the handle is invalid at this point
                    }
                    finally
                    {
                        hkey = null;
                    }
                }
                else if (disposing && IsPerfDataKey()) {
                    // System keys should never be closed.  However, we want to call RegCloseKey
                    // on HKEY_PERFORMANCE_DATA when called from PerformanceCounter.CloseSharedResources
                    // (i.e. when disposing is true) so that we release the PERFLIB cache and cause it
                    // to be refreshed (by re-reading the registry) when accessed subsequently. 
                    // This is the only way we can see the just installed perf counter.  
                    // NOTE: since HKEY_PERFORMANCE_DATA is process wide, there is inherent race condition in closing
                    // the key asynchronously. While Vista is smart enough to rebuild the PERFLIB resources
                    // in this situation the down level OSes are not. We have a small window between  
                    // the dispose below and usage elsewhere (other threads). This is By Design. 
                    // This is less of an issue when OS > NT5 (i.e Vista & higher), we can close the perfkey  
                    // (to release & refresh PERFLIB resources) and the OS will rebuild PERFLIB as necessary. 
                    SafeRegistryHandle.RegCloseKey(RegistryKey.HKEY_PERFORMANCE_DATA);
                }  
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Flush() {
            if (hkey != null) {
                 if (IsDirty()) {
                     Win32Native.RegFlushKey(hkey);
                }
            }
        }

#if FEATURE_CORECLR
        void IDisposable.Dispose()
#else
        public void Dispose()
#endif
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
        public RegistryKey CreateSubKey(String subkey) {
            return CreateSubKey(subkey, checkMode);
        }

        [ComVisible(false)]
        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck) 
        {
            return CreateSubKeyInternal(subkey, permissionCheck, null, RegistryOptions.None);
        }

        [ComVisible(false)]
        public RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck, RegistryOptions options) 
        {
            return CreateSubKeyInternal(subkey, permissionCheck, null, options);
        }

        [ComVisible(false)]
        public RegistryKey CreateSubKey(String subkey, bool writable)
        {
            return CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, RegistryOptions.None);
        }

        [ComVisible(false)]
        public RegistryKey CreateSubKey(String subkey, bool writable, RegistryOptions options)
        {
            return CreateSubKeyInternal(subkey, writable ? RegistryKeyPermissionCheck.ReadWriteSubTree : RegistryKeyPermissionCheck.ReadSubTree, null, options);
        }


#if FEATURE_MACL
        [ComVisible(false)]
        public unsafe RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck,  RegistrySecurity registrySecurity) 
        {
            return CreateSubKeyInternal(subkey, permissionCheck, registrySecurity, RegistryOptions.None);
        }
        
        [ComVisible(false)]
        public unsafe RegistryKey CreateSubKey(String subkey, RegistryKeyPermissionCheck permissionCheck,  RegistryOptions registryOptions, RegistrySecurity registrySecurity) 
        {
            return CreateSubKeyInternal(subkey, permissionCheck, registrySecurity, registryOptions);
        }        
#endif

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        private unsafe RegistryKey CreateSubKeyInternal(String subkey, RegistryKeyPermissionCheck permissionCheck, object registrySecurityObj, RegistryOptions registryOptions)
        {
            ValidateKeyOptions(registryOptions);
            ValidateKeyName(subkey);
            ValidateKeyMode(permissionCheck);            
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash
            
            // only keys opened under read mode is not writable
            if (!remoteKey) {
                RegistryKey key = InternalOpenSubKey(subkey, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree));
                if (key != null)  { // Key already exits
                    CheckPermission(RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
                    CheckPermission(RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);
                    key.checkMode = permissionCheck;
                    return key;
                }
            }

            CheckPermission(RegistryInternalCheck.CheckSubKeyCreatePermission, subkey, false, RegistryKeyPermissionCheck.Default);      
            
            Win32Native.SECURITY_ATTRIBUTES secAttrs = null;
#if FEATURE_MACL
            RegistrySecurity registrySecurity = (RegistrySecurity)registrySecurityObj;
            // For ACL's, get the security descriptor from the RegistrySecurity.
            if (registrySecurity != null) {
                secAttrs = new Win32Native.SECURITY_ATTRIBUTES();
                secAttrs.nLength = (int)Marshal.SizeOf(secAttrs);

                byte[] sd = registrySecurity.GetSecurityDescriptorBinaryForm();
                // We allocate memory on the stack to improve the speed.
                // So this part of code can't be refactored into a method.
                byte* pSecDescriptor = stackalloc byte[sd.Length];
                Buffer.Memcpy(pSecDescriptor, 0, sd, 0, sd.Length);
                secAttrs.pSecurityDescriptor = pSecDescriptor;
            }
#endif
            int disposition = 0;

            // By default, the new key will be writable.
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegCreateKeyEx(hkey,
                subkey,
                0,
                null,
                (int)registryOptions /* specifies if the key is volatile */,
                GetRegistryKeyAccess(permissionCheck != RegistryKeyPermissionCheck.ReadSubTree) | (int)regView,
                secAttrs,
                out result,
                out disposition);

            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck != RegistryKeyPermissionCheck.ReadSubTree), false, remoteKey, false, regView);                
                CheckPermission(RegistryInternalCheck.CheckSubTreePermission, subkey, false, permissionCheck);                
                key.checkMode = permissionCheck;
                
                if (subkey.Length == 0)
                    key.keyName = keyName;
                else
                    key.keyName = keyName + "\\" + subkey;
                return key;
            }
            else if (ret != 0) // syscall failed, ret is an error code.
                Win32Error(ret, keyName + "\\" + subkey);  // Access denied?

            BCLDebug.Assert(false, "Unexpected code path in RegistryKey::CreateSubKey");
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
        public void DeleteSubKey(String subkey) {
            DeleteSubKey(subkey, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void DeleteSubKey(String subkey, bool throwOnMissingSubKey) {
            ValidateKeyName(subkey);        
            EnsureWriteable();
            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash
            CheckPermission(RegistryInternalCheck.CheckSubKeyWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);
            
            // Open the key we are deleting and check for children. Be sure to
            // explicitly call close to avoid keeping an extra HKEY open.
            //
            RegistryKey key = InternalOpenSubKey(subkey,false);
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_RegRemoveSubKey);
                    }
                }
                finally {
                    key.Close();
                }

                int ret;

                try {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException) {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }

                if (ret!=0) {
                    if (ret == Win32Native.ERROR_FILE_NOT_FOUND) {
                        if (throwOnMissingSubKey)
                            ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
                    }
                    else
                        Win32Error(ret, null);
                }
            }
            else { // there is no key which also means there is no subkey
                if (throwOnMissingSubKey)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }

        /**
         * Recursively deletes a subkey and any child subkeys.
         *
         * @param subkey SubKey to delete.
         */
        public void DeleteSubKeyTree(String subkey) {
            DeleteSubKeyTree(subkey, true /*throwOnMissingSubKey*/);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public void DeleteSubKeyTree(String subkey, Boolean throwOnMissingSubKey) {
            ValidateKeyName(subkey);
            
            // Security concern: Deleting a hive's "" subkey would delete all
            // of that hive's contents.  Don't allow "".
            if (subkey.Length==0 && IsSystemKey()) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyDelHive);
            }

            EnsureWriteable();

            subkey = FixupName(subkey); // Fixup multiple slashes to a single slash
            CheckPermission(RegistryInternalCheck.CheckSubTreeWritePermission, subkey, false, RegistryKeyPermissionCheck.Default);            

            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) {
                        String[] keys = key.InternalGetSubKeyNames();

                        for (int i=0; i<keys.Length; i++) {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally {
                    key.Close();
                }

                int ret;
                try {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException) {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }
        
                if (ret!=0) Win32Error(ret, null);
            }
            else if(throwOnMissingSubKey) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);
            }
        }

        // An internal version which does no security checks or argument checking.  Skipping the 
        // security checks should give us a slight perf gain on large trees. 
        [System.Security.SecurityCritical]  // auto-generated
        private void DeleteSubKeyTreeInternal(string subkey) {
            RegistryKey key = InternalOpenSubKey(subkey, true);
            if (key != null) {
                try {
                    if (key.InternalSubKeyCount() > 0) {
                        String[] keys = key.InternalGetSubKeyNames();

                        for (int i=0; i<keys.Length; i++) {
                            key.DeleteSubKeyTreeInternal(keys[i]);
                        }
                    }
                }
                finally {
                    key.Close();
                }

                int ret;
                try {
                    ret = Win32Native.RegDeleteKeyEx(hkey, subkey, (int)regView, 0);
                }
                catch (EntryPointNotFoundException) {
                    ret = Win32Native.RegDeleteKey(hkey, subkey);
                }
                if (ret!=0) Win32Error(ret, null);
            }
            else {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyAbsent);                
            }
        }
        
        /**
         * Deletes the specified value from this key.
         *
         * @param name Name of value to delete.
         */
        public void DeleteValue(String name) {
            DeleteValue(name, true);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void DeleteValue(String name, bool throwOnMissingValue) {
            EnsureWriteable();
            CheckPermission(RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);            
            int errorCode = Win32Native.RegDeleteValue(hkey, name);
            
            //
            // From windows 2003 server, if the name is too long we will get error code ERROR_FILENAME_EXCED_RANGE  
            // This still means the name doesn't exist. We need to be consistent with previous OS.
            //
            if (errorCode == Win32Native.ERROR_FILE_NOT_FOUND || errorCode == Win32Native.ERROR_FILENAME_EXCED_RANGE) {
                if (throwOnMissingValue) {                
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSubKeyValueAbsent);                                    
                }
                // Otherwise, just return giving no indication to the user.
                // (For compatibility)
            }
            // We really should throw an exception here if errorCode was bad,
            // but we can't for compatibility reasons.
            BCLDebug.Correctness(errorCode == 0, "RegDeleteValue failed.  Here's your error code: "+errorCode);
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
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA.
         *
         * @param hKey HKEY_* to open.
         *
         * @return the RegistryKey requested.
         */
        [System.Security.SecurityCritical]  // auto-generated
        internal static RegistryKey GetBaseKey(IntPtr hKey) {
            return GetBaseKey(hKey, RegistryView.Default);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static RegistryKey GetBaseKey(IntPtr hKey, RegistryView view) {

            int index = ((int)hKey) & 0x0FFFFFFF;
            BCLDebug.Assert(index >= 0  && index < hkeyNames.Length, "index is out of range!");
            BCLDebug.Assert((((int)hKey) & 0xFFFFFFF0) == 0x80000000, "Invalid hkey value!");

            bool isPerf = hKey == HKEY_PERFORMANCE_DATA;
            // only mark the SafeHandle as ownsHandle if the key is HKEY_PERFORMANCE_DATA.
            SafeRegistryHandle srh = new SafeRegistryHandle(hKey, isPerf);

            RegistryKey key = new RegistryKey(srh, true, true,false, isPerf, view);
            key.checkMode = RegistryKeyPermissionCheck.Default;
            key.keyName = hkeyNames[index];
            return key;
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public static RegistryKey OpenBaseKey(RegistryHive hKey, RegistryView view) {
            ValidateKeyView(view);
            CheckUnmanagedCodePermission();
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
         * HKEY_CURRENT_CONFIG,
         * HKEY_DYN_DATA.
         *
         * @param hKey HKEY_* to open.
         * @param machineName the machine to connect to
         *
         * @return the RegistryKey requested.
         */
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName) {
            return OpenRemoteBaseKey(hKey, machineName, RegistryView.Default);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public static RegistryKey OpenRemoteBaseKey(RegistryHive hKey, String machineName, RegistryView view) {
            if (machineName==null)
                throw new ArgumentNullException("machineName");
            int index = (int)hKey & 0x0FFFFFFF;
            if (index < 0 || index >= hkeyNames.Length || ((int)hKey & 0xFFFFFFF0) != 0x80000000) {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyOutOfRange"));
            }
            ValidateKeyView(view);

            CheckUnmanagedCodePermission();
            // connect to the specified remote registry
            SafeRegistryHandle foreignHKey = null;
            int ret = Win32Native.RegConnectRegistry(machineName, new SafeRegistryHandle(new IntPtr((int)hKey), false), out foreignHKey);

            if (ret == Win32Native.ERROR_DLL_INIT_FAILED)
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_DllInitFailure"));

            if (ret != 0)
                Win32ErrorStatic(ret, null);

            if (foreignHKey.IsInvalid)
                // return value indicates an error occurred
                throw new ArgumentException(Environment.GetResourceString("Arg_RegKeyNoRemoteConnect", machineName));

            RegistryKey key = new RegistryKey(foreignHKey, true, false, true, ((IntPtr) hKey) == HKEY_PERFORMANCE_DATA, view);
            key.checkMode = RegistryKeyPermissionCheck.Default;
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
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public RegistryKey OpenSubKey(string name, bool writable ) {
            ValidateKeyName(name);
            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash            

            CheckPermission(RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission, name, writable, RegistryKeyPermissionCheck.Default);                            
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey,
                name,
                0,
                GetRegistryKeyAccess(writable) | (int)regView,
                out result);

            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, writable, false, remoteKey, false, regView);                
                key.checkMode = GetSubKeyPermissonCheck(writable);
                key.keyName = keyName + "\\" + name;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) {
                // We need to throw SecurityException here for compatibility reasons,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);
            }
            
            return null;
        }

#if FEATURE_MACL

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck) { 
            ValidateKeyMode(permissionCheck);            
            return InternalOpenSubKey(name, permissionCheck, GetRegistryKeyAccess(permissionCheck));
        }

        [System.Security.SecuritySafeCritical]
        [ComVisible(false)]
        public RegistryKey OpenSubKey(String name, RegistryRights rights)
        {
            return InternalOpenSubKey(name, this.checkMode, (int)rights);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public RegistryKey OpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, RegistryRights rights) {
            return InternalOpenSubKey(name, permissionCheck, (int)rights);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private RegistryKey InternalOpenSubKey(String name, RegistryKeyPermissionCheck permissionCheck, int rights) {
            ValidateKeyName(name);
            ValidateKeyMode(permissionCheck);            

            ValidateKeyRights(rights);

            EnsureNotDisposed();
            name = FixupName(name); // Fixup multiple slashes to a single slash

            CheckPermission(RegistryInternalCheck.CheckOpenSubKeyPermission, name, false, permissionCheck);                        
            CheckPermission(RegistryInternalCheck.CheckSubTreePermission, name, false, permissionCheck);
            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey, name, 0, (rights | (int)regView), out result);
            if (ret == 0 && !result.IsInvalid) {
                RegistryKey key = new RegistryKey(result, (permissionCheck == RegistryKeyPermissionCheck.ReadWriteSubTree), false, remoteKey, false, regView);
                key.keyName = keyName + "\\" + name;
                key.checkMode = permissionCheck;
                return key;
            }

            // Return null if we didn't find the key.
            if (ret == Win32Native.ERROR_ACCESS_DENIED || ret == Win32Native.ERROR_BAD_IMPERSONATION_LEVEL) {
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);                
            }
            
            return null;                        
        }    
#endif

        // This required no security checks. This is to get around the Deleting SubKeys which only require
        // write permission. They call OpenSubKey which required read. Now instead call this function w/o security checks
        [System.Security.SecurityCritical]  // auto-generated
        internal RegistryKey InternalOpenSubKey(String name, bool writable) {
            ValidateKeyName(name);
            EnsureNotDisposed();

            SafeRegistryHandle result = null;
            int ret = Win32Native.RegOpenKeyEx(hkey,
                name,
                0,
                GetRegistryKeyAccess(writable) | (int)regView,
                out result);

            if (ret == 0 && !result.IsInvalid) {
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
#if FEATURE_CORECLR
        [System.Security.SecurityCritical] 
#endif
        public RegistryKey OpenSubKey(String name) {
            return OpenSubKey(name, false);
        }

        /**
         * Retrieves the count of subkeys.
         *
         * @return a count of subkeys.
         */
        public int SubKeyCount {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
                return InternalSubKeyCount();
            }
        }

        [ComVisible(false)]
        public RegistryView View {
            [System.Security.SecuritySafeCritical]
            get {
                EnsureNotDisposed();
                return regView;
            }
        }

#if !FEATURE_CORECLR
        [ComVisible(false)]
        public SafeRegistryHandle Handle {
            [System.Security.SecurityCritical]
            [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get {
                EnsureNotDisposed();
                int ret = Win32Native.ERROR_INVALID_HANDLE;
                if (IsSystemKey()) {
                    IntPtr baseKey = (IntPtr)0;
                    switch (keyName) {
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
                        case "HKEY_DYN_DATA":
                            baseKey = HKEY_DYN_DATA;
                            break;
                        default:
                            Win32Error(ret, null);
                            break;
                    }
                    // open the base key so that RegistryKey.Handle will return a valid handle
                    SafeRegistryHandle result;
                    ret = Win32Native.RegOpenKeyEx(baseKey,
                        null,
                        0,
                        GetRegistryKeyAccess(IsWritable()) | (int)regView,
                        out result);

                    if (ret == 0 && !result.IsInvalid) {
                        return result;
                    }
                    else {
                        Win32Error(ret, null);
                    }
                }
                else {
                    return hkey;
                }
                throw new IOException(Win32Native.GetMessage(ret), ret);
            }
        }

        [System.Security.SecurityCritical]
        [ComVisible(false)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static RegistryKey FromHandle(SafeRegistryHandle handle) {
            return FromHandle(handle, RegistryView.Default);
        }

        [System.Security.SecurityCritical]
        [ComVisible(false)]
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public static RegistryKey FromHandle(SafeRegistryHandle handle, RegistryView view) {
            if (handle == null) throw new ArgumentNullException("handle");
            ValidateKeyView(view);

            return new RegistryKey(handle, true /* isWritable */, view);
        }
#endif

        [System.Security.SecurityCritical]  // auto-generated
        internal int InternalSubKeyCount() {
                EnsureNotDisposed();

                int subkeys = 0;
                int junk = 0;
                int ret = Win32Native.RegQueryInfoKey(hkey,
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
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public String[] GetSubKeyNames() {
            CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
            return InternalGetSubKeyNames();
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe String[] InternalGetSubKeyNames() {
            EnsureNotDisposed();
            int subkeys = InternalSubKeyCount();
            String[] names = new String[subkeys];  // Returns 0-length array if empty.

            if (subkeys > 0) {
                char[] name = new char[MaxKeyLength + 1];
                
                int namelen;

                fixed (char *namePtr = &name[0])
                {
                    for (int i=0; i<subkeys; i++) {
                        namelen = name.Length; // Don't remove this. The API's doesn't work if this is not properly initialised.
                        int ret = Win32Native.RegEnumKeyEx(hkey,
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
            }

            return names;
        }

        /**
         * Retrieves the count of values.
         *
         * @return a count of values.
         */
        public int ValueCount {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
                return InternalValueCount();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal int InternalValueCount() {
            EnsureNotDisposed();
            int values = 0;
            int junk = 0;
            int ret = Win32Native.RegQueryInfoKey(hkey,
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
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe String[] GetValueNames() {
            CheckPermission(RegistryInternalCheck.CheckKeyReadPermission, null, false, RegistryKeyPermissionCheck.Default);
            EnsureNotDisposed();

            int values = InternalValueCount();
            String[] names = new String[values];

            if (values > 0) {
                char[] name = new char[MaxValueLength + 1];
                int namelen;

                fixed (char *namePtr = &name[0])
                {
                    for (int i=0; i<values; i++) {
                        namelen = name.Length;

                        int ret = Win32Native.RegEnumValue(hkey,
                            i,
                            namePtr,
                            ref namelen,
                            IntPtr.Zero,
                            null,
                            null,
                            null);

                        if (ret != 0) {
                            // ignore ERROR_MORE_DATA if we're querying HKEY_PERFORMANCE_DATA
                            if (!(IsPerfDataKey() && ret == Win32Native.ERROR_MORE_DATA))
                                Win32Error(ret, null);
                        }

                        names[i] = new String(namePtr);
                    }
                }
            }

            return names;
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
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Object GetValue(String name) {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);        
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
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        public Object GetValue(String name, Object defaultValue) {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);        
            return InternalGetValue(name, defaultValue, false, true);
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [ComVisible(false)]
        public Object GetValue(String name, Object defaultValue, RegistryValueOptions options) {
            if( options < RegistryValueOptions.None || options > RegistryValueOptions.DoNotExpandEnvironmentNames) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)options), "options");
            }
            bool doNotExpand = (options == RegistryValueOptions.DoNotExpandEnvironmentNames);
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);            
            return InternalGetValue(name, defaultValue, doNotExpand, true);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal Object InternalGetValue(String name, Object defaultValue, bool doNotExpand, bool checkSecurity) {            
            if (checkSecurity) {       
                // Name can be null!  It's the most common use of RegQueryValueEx
                EnsureNotDisposed();
            }

            Object data = defaultValue;
            int type = 0;
            int datasize = 0;

            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);

            if (ret != 0) {
                if (IsPerfDataKey()) {
                    int size = 65000;
                    int sizeInput = size;
  
                    int r;
                    byte[] blob = new byte[size];
                    while (Win32Native.ERROR_MORE_DATA == (r = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref sizeInput))) {
                        if (size == Int32.MaxValue) {
                            // ERROR_MORE_DATA was returned however we cannot increase the buffer size beyond Int32.MaxValue
                            Win32Error(r, name);
                        }
                        else if (size  > (Int32.MaxValue / 2)) {
                            // at this point in the loop "size * 2" would cause an overflow
                            size = Int32.MaxValue;
                        }
                        else {
                        size *= 2;
                        }
                        sizeInput = size;
                        blob = new byte[size];
                    }
                    if (r != 0)
                        Win32Error(r, name);
                    return blob;
                }
                else {
                    // For stuff like ERROR_FILE_NOT_FOUND, we want to return null (data).
                    // Some OS's returned ERROR_MORE_DATA even in success cases, so we 
                    // want to continue on through the function. 
                    if (ret != Win32Native.ERROR_MORE_DATA) 
                        return data;
                }
            }

            if (datasize < 0) {
                // unexpected code path
                BCLDebug.Assert(false, "[InternalGetValue] RegQueryValue returned ERROR_SUCCESS but gave a negative datasize");
                datasize = 0;
            }


            switch (type) {
            case Win32Native.REG_NONE:
            case Win32Native.REG_DWORD_BIG_ENDIAN:
            case Win32Native.REG_BINARY: {
                                 byte[] blob = new byte[datasize];
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                 data = blob;
                             }
                             break;
            case Win32Native.REG_QWORD:
                             {    // also REG_QWORD_LITTLE_ENDIAN
                                 if (datasize > 8) {
                                     // prevent an AV in the edge case that datasize is larger than sizeof(long)
                                     goto case Win32Native.REG_BINARY;
                                 }
                                 long blob = 0;
                                 BCLDebug.Assert(datasize==8, "datasize==8");
                                 // Here, datasize must be 8 when calling this
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                                 data = blob;
                             }
                             break;
            case Win32Native.REG_DWORD:
                             {    // also REG_DWORD_LITTLE_ENDIAN
                                 if (datasize > 4) {
                                     // prevent an AV in the edge case that datasize is larger than sizeof(int)
                                     goto case Win32Native.REG_QWORD;
                                 }
                                 int blob = 0;
                                 BCLDebug.Assert(datasize==4, "datasize==4");
                                 // Here, datasize must be four when calling this
                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, ref blob, ref datasize);

                                 data = blob;
                             }
                             break;

            case Win32Native.REG_SZ:
                             {
                                 if (datasize % 2 == 1) {
                                     // handle the case where the registry contains an odd-byte length (corrupt data?)
                                     try {
                                         datasize = checked(datasize + 1);
                                     }
                                     catch (OverflowException e) {
                                         throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                                     }
                                 }
                                 char[] blob = new char[datasize/2];

                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);
                                 if (blob.Length > 0 && blob[blob.Length - 1] == (char)0) {
                                     data = new String(blob, 0, blob.Length - 1);
                                 }
                                 else {
                                     // in the very unlikely case the data is missing null termination, 
                                     // pass in the whole char[] to prevent truncating a character
                                     data = new String(blob);
                                 }
                             }
                             break;

            case Win32Native.REG_EXPAND_SZ:
                              {
                                 if (datasize % 2 == 1) {
                                     // handle the case where the registry contains an odd-byte length (corrupt data?)
                                     try {
                                         datasize = checked(datasize + 1);
                                     }
                                     catch (OverflowException e) {
                                         throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                                     }
                                 }
                                 char[] blob = new char[datasize/2];

                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                                 if (blob.Length > 0 && blob[blob.Length - 1] == (char)0) {
                                     data = new String(blob, 0, blob.Length - 1);
                                 }
                                 else {
                                     // in the very unlikely case the data is missing null termination, 
                                     // pass in the whole char[] to prevent truncating a character
                                     data = new String(blob);
                                 }

                                 if (!doNotExpand)
                                    data = Environment.ExpandEnvironmentVariables((String)data);
                             }
                             break;
            case Win32Native.REG_MULTI_SZ:
                             {
                                 if (datasize % 2 == 1) {
                                     // handle the case where the registry contains an odd-byte length (corrupt data?)
                                     try {
                                         datasize = checked(datasize + 1);
                                     }
                                     catch (OverflowException e) {
                                         throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                                     }
                                 }
                                 char[] blob = new char[datasize/2];

                                 ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, blob, ref datasize);

                                 // make sure the string is null terminated before processing the data
                                 if (blob.Length > 0 && blob[blob.Length - 1] != (char)0) {
                                     try {
                                         char[] newBlob = new char[checked(blob.Length + 1)];
                                         for (int i = 0; i < blob.Length; i++) {
                                             newBlob[i] = blob[i];
                                         }
                                         newBlob[newBlob.Length - 1] = (char)0;
                                         blob = newBlob;
                                     }
                                     catch (OverflowException e) {
                                         throw new IOException(Environment.GetResourceString("Arg_RegGetOverflowBug"), e);
                                     }
                                     blob[blob.Length - 1] = (char)0;
                                 }
               

                                 IList<String> strings = new List<String>();
                                 int cur = 0;
                                 int len = blob.Length;

                                 while (ret == 0 && cur < len) {
                                     int nextNull = cur;
                                     while (nextNull < len && blob[nextNull] != (char)0) {
                                         nextNull++;
                                     }

                                     if (nextNull < len) {
                                         BCLDebug.Assert(blob[nextNull] == (char)0, "blob[nextNull] should be 0");
                                         if (nextNull-cur > 0) {
                                             strings.Add(new String(blob, cur, nextNull-cur));
                                         }
                                         else {
                                            // we found an empty string.  But if we're at the end of the data, 
                                            // it's just the extra null terminator. 
                                            if (nextNull != len-1) 
                                                strings.Add(String.Empty);
                                         }
                                     }
                                     else {
                                         strings.Add(new String(blob, cur, len-cur));
                                     }
                                     cur = nextNull+1;
                                 }
    
                                 data = new String[strings.Count];
                                 strings.CopyTo((String[])data, 0);
                             }
                             break;
            case Win32Native.REG_LINK:
            default:
                break;
            }

            return data;
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [ComVisible(false)]
        public RegistryValueKind GetValueKind(string name) {
            CheckPermission(RegistryInternalCheck.CheckValueReadPermission, name, false, RegistryKeyPermissionCheck.Default);
            EnsureNotDisposed();

            int type = 0;
            int datasize = 0;
            int ret = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
            if (ret != 0)
                Win32Error(ret, null);
            if (type == Win32Native.REG_NONE)
                return RegistryValueKind.None;
            else if (!Enum.IsDefined(typeof(RegistryValueKind), type))
                return RegistryValueKind.Unknown;
            else
                return (RegistryValueKind) type;
        }

        /**
         * Retrieves the current state of the dirty property.
         *
         * A key is marked as dirty if any operation has occurred that modifies the
         * contents of the key.
         *
         * @return <b>true</b> if the key has been modified.
         */
        private bool IsDirty() {
            return (this.state & STATE_DIRTY) != 0;
        }

        private bool IsSystemKey() {
            return (this.state & STATE_SYSTEMKEY) != 0;
        }

        private bool IsWritable() {
            return (this.state & STATE_WRITEACCESS) != 0;
        }

        private bool IsPerfDataKey() {
            return (this.state & STATE_PERF_DATA) != 0;
        }

        public String Name {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get { 
                EnsureNotDisposed();
                return keyName; 
            }
        }

        private void SetDirty() {
            this.state |= STATE_DIRTY;
        }

        /**
         * Sets the specified value.
         *
         * @param name Name of value to store data in.
         * @param value Data to store.
         */
        public void SetValue(String name, Object value) {
            SetValue(name, value, RegistryValueKind.Unknown);
        }

        [System.Security.SecuritySafeCritical] //auto-generated
        [ComVisible(false)]
        public unsafe void SetValue(String name, Object value, RegistryValueKind valueKind) {
            if (value==null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);

            if (name != null && name.Length > MaxValueLength) {
                throw new ArgumentException(Environment.GetResourceString("Arg_RegValStrLenBug"));
            }

            if (!Enum.IsDefined(typeof(RegistryValueKind), valueKind))
                throw new ArgumentException(Environment.GetResourceString("Arg_RegBadKeyKind"), "valueKind");

            EnsureWriteable();

            if (!remoteKey && ContainsRegistryValue(name)) { // Existing key 
                CheckPermission(RegistryInternalCheck.CheckValueWritePermission, name, false, RegistryKeyPermissionCheck.Default);
            }                    
             else { // Creating a new value
                CheckPermission(RegistryInternalCheck.CheckValueCreatePermission, name, false, RegistryKeyPermissionCheck.Default);             
            }

            if (valueKind == RegistryValueKind.Unknown) {
                // this is to maintain compatibility with the old way of autodetecting the type.
                // SetValue(string, object) will come through this codepath.
                valueKind = CalculateValueKind(value);
            }

            int ret = 0;
            try {
                switch (valueKind) {
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.String:
                        {
                            String data = value.ToString();
                                ret = Win32Native.RegSetValueEx(hkey,
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
                            int sizeInBytes = 0;

                            // First determine the size of the array
                            //
                                for (int i=0; i<dataStrings.Length; i++) {
                                    if (dataStrings[i] == null) {
                                        ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetStrArrNull);
                                    }
                                    sizeInBytes = checked(sizeInBytes + (dataStrings[i].Length+1) * 2);
                                }
                                sizeInBytes = checked(sizeInBytes + 2);

                            byte[] basePtr = new byte[sizeInBytes];
                            fixed(byte* b = basePtr) {
                                IntPtr currentPtr = new IntPtr( (void *) b);

                                // Write out the strings...
                                //
                                for (int i=0; i<dataStrings.Length; i++) {
                                    // Assumes that the Strings are always null terminated.
                                        String.InternalCopy(dataStrings[i],currentPtr,(checked(dataStrings[i].Length*2)));
                                        currentPtr = new IntPtr((long)currentPtr + (checked(dataStrings[i].Length*2)));
                                        *(char*)(currentPtr.ToPointer()) = '\0';
                                        currentPtr = new IntPtr((long)currentPtr + 2);
                                    }

                                    *(char*)(currentPtr.ToPointer()) = '\0';
                                    currentPtr = new IntPtr((long)currentPtr + 2);

                                ret = Win32Native.RegSetValueEx(hkey,
                                    name,
                                    0,
                                    RegistryValueKind.MultiString,
                                    basePtr,
                                    sizeInBytes);
                            }
                            break;
                        }

                    case RegistryValueKind.None:
                    case RegistryValueKind.Binary:
                        byte[] dataBytes = (byte[]) value;
                        ret = Win32Native.RegSetValueEx(hkey,
                            name,
                            0,
                            (valueKind == RegistryValueKind.None ? Win32Native.REG_NONE: RegistryValueKind.Binary),
                            dataBytes,
                            dataBytes.Length);
                        break;

                    case RegistryValueKind.DWord:
                        {
                            // We need to use Convert here because we could have a boxed type cannot be
                            // unboxed and cast at the same time.  I.e. ((int)(object)(short) 5) will fail.
                            int data = Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);

                            ret = Win32Native.RegSetValueEx(hkey,
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

                            ret = Win32Native.RegSetValueEx(hkey,
                                name,
                                0,
                                RegistryValueKind.QWord,
                                ref data,
                                8);
                            break;
                        }
                }
            }
            catch (OverflowException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);
            }
            catch (InvalidOperationException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);                
            }
            catch (FormatException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);                
            }
            catch (InvalidCastException) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegSetMismatchedKind);                
            }

            if (ret == 0) {
                SetDirty();
            }
            else
                Win32Error(ret, null);

        }

        private RegistryValueKind CalculateValueKind(Object value) {
            // This logic matches what used to be in SetValue(string name, object value) in the v1.0 and v1.1 days.
            // Even though we could add detection for an int64 in here, we want to maintain compatibility with the
            // old behavior.
            if (value is Int32)
                return RegistryValueKind.DWord;
            else if (value is Array) {
                if (value is byte[])
                    return RegistryValueKind.Binary;
                else if (value is String[])
                    return RegistryValueKind.MultiString;
                else
                    throw new ArgumentException(Environment.GetResourceString("Arg_RegSetBadArrType", value.GetType().Name));
            }
            else
                return RegistryValueKind.String;
        }

        /**
         * Retrieves a string representation of this key.
         *
         * @return a string representing the key.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override String ToString() {
            EnsureNotDisposed();
            return keyName;
        }

#if FEATURE_MACL
        public RegistrySecurity GetAccessControl() {
            return GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RegistrySecurity GetAccessControl(AccessControlSections includeSections) {
            EnsureNotDisposed();
            return new RegistrySecurity(hkey, keyName, includeSections);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void SetAccessControl(RegistrySecurity registrySecurity) {
            EnsureWriteable();
            if (registrySecurity == null)
                throw new ArgumentNullException("registrySecurity");

            registrySecurity.Persist(hkey, keyName);
        }
#endif

        /**
         * After calling GetLastWin32Error(), it clears the last error field,
         * so you must save the HResult and pass it to this method.  This method
         * will determine the appropriate exception to throw dependent on your
         * error, and depending on the error, insert a string into the message
         * gotten from the ResourceManager.
         */
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal void Win32Error(int errorCode, String str) {
            switch (errorCode) {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();

                case Win32Native.ERROR_INVALID_HANDLE:
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
                     */
                    if (!IsPerfDataKey()) {
                    this.hkey.SetHandleAsInvalid();
                    this.hkey = null;
                    }
                        goto default;

                case Win32Native.ERROR_FILE_NOT_FOUND:
                    throw new IOException(Environment.GetResourceString("Arg_RegKeyNotFound"), errorCode);                    

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        [SecuritySafeCritical]
        internal static void Win32ErrorStatic(int errorCode, String str) {
            switch (errorCode) {
                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str != null)
                        throw new UnauthorizedAccessException(Environment.GetResourceString("UnauthorizedAccess_RegistryKeyGeneric_Key", str));
                    else
                        throw new UnauthorizedAccessException();
    
                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), errorCode);
            }
        }

        internal static String FixupName(String name)
        {
            BCLDebug.Assert(name!=null,"[FixupName]name!=null");
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
            int length  = path.Length;
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
                    if(path[i] == markerChar)
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

        //
        // Read/Write/Create SubKey Permission
        //
        private void GetSubKeyReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Read;
            path   = keyName + "\\" + subkeyName + "\\.";
        }
        private void GetSubKeyWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            // If we want to open a subkey of a read-only key as writeable, we need to do the check.
            access = RegistryPermissionAccess.Write;
            path   = keyName + "\\" + subkeyName + "\\.";
        }
        private void GetSubKeyCreatePermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Create;
            path   = keyName + "\\" + subkeyName + "\\."; 
        }

        //
        // Read/Write/ReadWrite SubTree Permission
        //
        private void GetSubTreeReadPermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Read;
            path   = keyName + "\\" + subkeyName + "\\";
        }
        private void GetSubTreeWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Write;
            path   = keyName + "\\" + subkeyName + "\\";
        }
        private void GetSubTreeReadWritePermission(string subkeyName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Write | RegistryPermissionAccess.Read;
            path   = keyName + "\\" + subkeyName;
        }

        //
        // Read/Write/Create Value Permission
        //
        private void GetValueReadPermission(string valueName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Read;
            path   = keyName+"\\"+valueName;
        }
        private void GetValueWritePermission(string valueName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Write;
            path   = keyName+"\\"+valueName;                      
        }
        private void GetValueCreatePermission(string valueName, out RegistryPermissionAccess access, out string path) {
            access = RegistryPermissionAccess.Create;
            path   = keyName+"\\"+valueName;
        }

        // Read Key Permission
        private void GetKeyReadPermission(out RegistryPermissionAccess access, out string path) {
           access = RegistryPermissionAccess.Read;
           path   = keyName + "\\.";
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void CheckPermission(RegistryInternalCheck check, string item, bool subKeyWritable, RegistryKeyPermissionCheck subKeyCheck) {
            bool demand = false;
            RegistryPermissionAccess access = RegistryPermissionAccess.NoAccess;
            string path = null;

#if !FEATURE_CORECLR
            if (CodeAccessSecurityEngine.QuickCheckForAllDemands()) {
                return; // full trust fast path
            }
#endif // !FEATURE_CORECLR
    
            switch (check) {
                //
                // Read/Write/Create SubKey Permission
                //
                case RegistryInternalCheck.CheckSubKeyReadPermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode == RegistryKeyPermissionCheck.Default, "Should be called from a key opened under default mode only!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");     
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        demand = true;
                        GetSubKeyReadPermission(item, out access, out path);       
                    }
                    break;
                case RegistryInternalCheck.CheckSubKeyWritePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            demand = true;
                            GetSubKeyWritePermission(item, out access, out path);                        
                        }
                    }
                    break;
                case RegistryInternalCheck.CheckSubKeyCreatePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating sub key under read-only key!");            
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");      
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");        
                        if( checkMode == RegistryKeyPermissionCheck.Default) {  
                            demand = true;
                            GetSubKeyCreatePermission(item, out access, out path);
                        }
                    }
                    break;
                //
                // Read/Write/ReadWrite SubTree Permission
                //
                case RegistryInternalCheck.CheckSubTreeReadPermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");  
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");            
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            demand = true;
                            GetSubTreeReadPermission(item, out access, out path);
                        }
                    }
                    break;
                case RegistryInternalCheck.CheckSubTreeWritePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");  
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");             
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            demand = true;
                            GetSubTreeWritePermission(item, out access, out path);
                        }
                    }
                    break;
                case RegistryInternalCheck.CheckSubTreeReadWritePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");             
                        // If we want to open a subkey of a read-only key as writeable, we need to do the check.
                        demand = true;
                        GetSubTreeReadWritePermission(item, out access, out path);
                    }
                    break;
                //
                // Read/Write/Create Value Permission
                //
                case RegistryInternalCheck.CheckValueReadPermission:
                    ///*** no remoteKey check ***///
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");  
                    BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");            
                    if( checkMode == RegistryKeyPermissionCheck.Default) {
                        // only need to check for default mode (dynamice check)
                        demand = true;
                        GetValueReadPermission(item, out access, out path);
                    }
                    break;
                case RegistryInternalCheck.CheckValueWritePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow writing value to read-only key!");  
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");             
                        // skip the security check if the key is opened under write mode            
                        if( checkMode == RegistryKeyPermissionCheck.Default) {  
                            demand = true;
                            GetValueWritePermission(item, out access, out path);
                        }
                    }
                    break;
                case RegistryInternalCheck.CheckValueCreatePermission:
                    if (remoteKey) {
                        CheckUnmanagedCodePermission();
                    }
                    else {
                        BCLDebug.Assert(checkMode != RegistryKeyPermissionCheck.ReadSubTree, "We shouldn't allow creating value under read-only key!");            
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");            
                        // skip the security check if the key is opened under write mode
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            demand = true;
                            GetValueCreatePermission(item, out access, out path);
                        }
                    }
                    break;
                //
                // CheckKeyReadPermission
                //
                case RegistryInternalCheck.CheckKeyReadPermission:
                    ///*** no remoteKey check ***///
                    if( checkMode == RegistryKeyPermissionCheck.Default) {            
                        BCLDebug.Assert(item == null, "CheckKeyReadPermission should never have a non-null item parameter!");  
                        BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                        BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");             

                        // only need to check for default mode (dynamice check)   
                        demand = true;
                        GetKeyReadPermission(out access, out path);           
                    }
                    break;
                //
                // CheckSubTreePermission
                //
                case RegistryInternalCheck.CheckSubTreePermission:
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)");   
                    if( subKeyCheck == RegistryKeyPermissionCheck.ReadSubTree) {
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            if( remoteKey) {
                                CheckUnmanagedCodePermission();                
                            }
                            else {
                                demand = true;
                                GetSubTreeReadPermission(item, out access, out path);
                            }
                        }            
                    }
                    else if(subKeyCheck == RegistryKeyPermissionCheck.ReadWriteSubTree) {
                        if( checkMode != RegistryKeyPermissionCheck.ReadWriteSubTree) {
                            if( remoteKey) {
                                CheckUnmanagedCodePermission();                
                            }
                            else {
                                demand = true;
                                GetSubTreeReadWritePermission(item, out access, out path);
                            }
                        }                        
                    }
                    break;

                //
                // CheckOpenSubKeyWithWritablePermission uses the 'subKeyWritable' parameter
                //
                case RegistryInternalCheck.CheckOpenSubKeyWithWritablePermission:
                    BCLDebug.Assert(subKeyCheck == RegistryKeyPermissionCheck.Default, "subKeyCheck should be Default (unused)");            
                    // If the parent key is not opened under default mode, we have access already.
                    // If the parent key is opened under default mode, we need to check for permission.                        
                    if(checkMode == RegistryKeyPermissionCheck.Default) { 
                        if( remoteKey) {
                            CheckUnmanagedCodePermission(); 
                        }
                        else {               
                            demand = true;
                            GetSubKeyReadPermission(item, out access, out path);
                        }
                        break;
                    }                        
                    if( subKeyWritable && (checkMode == RegistryKeyPermissionCheck.ReadSubTree)) {
                        if( remoteKey) {
                            CheckUnmanagedCodePermission(); 
                        }
                        else { 
                            demand = true;
                            GetSubTreeReadWritePermission(item, out access, out path);
                        }
                        break;
                    }
                    break;

                //
                // CheckOpenSubKeyPermission uses the 'subKeyCheck' parameter
                //
                case RegistryInternalCheck.CheckOpenSubKeyPermission:
                    BCLDebug.Assert(subKeyWritable == false, "subKeyWritable should be false (unused)"); 
                    if(subKeyCheck == RegistryKeyPermissionCheck.Default) {                
                        if( checkMode == RegistryKeyPermissionCheck.Default) {
                            if(remoteKey) {
                                CheckUnmanagedCodePermission();
                            }
                            else {
                                demand = true;
                                GetSubKeyReadPermission(item, out access, out path);
                            }
                        }
                    }  
                    break;

                default:
                    BCLDebug.Assert(false, "CheckPermission default switch case should never be hit!");
                    break;
            }

            if (demand) {
                new RegistryPermission(access, path).Demand();
            }
        }
     
        [System.Security.SecurityCritical]  // auto-generated
        static private void  CheckUnmanagedCodePermission() {               
#pragma warning disable 618
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();        
#pragma warning restore 618
        }

        [System.Security.SecurityCritical]  // auto-generated
        private bool ContainsRegistryValue(string name) {
                int type = 0;
                int datasize = 0;
                int retval = Win32Native.RegQueryValueEx(hkey, name, null, ref type, (byte[])null, ref datasize);
                return retval == 0;            
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureNotDisposed(){
            if (hkey == null) {
                ThrowHelper.ThrowObjectDisposedException(keyName, ExceptionResource.ObjectDisposed_RegKeyClosed);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void EnsureWriteable() {
            EnsureNotDisposed();
            if (!IsWritable()) {
                ThrowHelper.ThrowUnauthorizedAccessException(ExceptionResource.UnauthorizedAccess_RegistryNoWrite);
            }
        }

        static int GetRegistryKeyAccess(bool isWritable) {
            int winAccess;
            if (!isWritable) {
                winAccess = Win32Native.KEY_READ;
            }
            else {
                winAccess = Win32Native.KEY_READ | Win32Native.KEY_WRITE;
            }

            return winAccess;
        }

        static int GetRegistryKeyAccess(RegistryKeyPermissionCheck mode) {
            int winAccess = 0;
            switch(mode) {
                case RegistryKeyPermissionCheck.ReadSubTree:        
                case RegistryKeyPermissionCheck.Default:                    
                    winAccess =  Win32Native.KEY_READ;
                    break;
                                        
                case RegistryKeyPermissionCheck.ReadWriteSubTree:
                    winAccess = Win32Native.KEY_READ| Win32Native.KEY_WRITE;                    
                    break;                    
                    
               default:
                    BCLDebug.Assert(false, "unexpected code path");
                    break;
            }      

            return winAccess;
        }

        private RegistryKeyPermissionCheck GetSubKeyPermissonCheck(bool subkeyWritable) {
            if( checkMode == RegistryKeyPermissionCheck.Default) {
                return checkMode;
            }                        
            
            if(subkeyWritable) {
                return RegistryKeyPermissionCheck.ReadWriteSubTree;
            }
            else {
                return RegistryKeyPermissionCheck.ReadSubTree;
            }
        }

        static private void ValidateKeyName(string name) {
            Contract.Ensures(name != null);
            if (name == null) {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.name);
            }

            int nextSlash = name.IndexOf("\\", StringComparison.OrdinalIgnoreCase);
            int current = 0;
            while (nextSlash != -1) {
                if ((nextSlash - current) > MaxKeyLength)
                    ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);

                current = nextSlash + 1;
                nextSlash = name.IndexOf("\\", current, StringComparison.OrdinalIgnoreCase);
            }

            if ((name.Length - current) > MaxKeyLength)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RegKeyStrLenBug);
                
        }
        
        static private void ValidateKeyMode(RegistryKeyPermissionCheck mode) {
            if( mode < RegistryKeyPermissionCheck.Default || mode > RegistryKeyPermissionCheck.ReadWriteSubTree) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryKeyPermissionCheck, ExceptionArgument.mode);  
            }            
        }

        static private void ValidateKeyOptions(RegistryOptions options) {
            if (options < RegistryOptions.None || options > RegistryOptions.Volatile) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryOptionsCheck, ExceptionArgument.options);
            }
        }

        static private void ValidateKeyView(RegistryView view) {
            if (view != RegistryView.Default && view != RegistryView.Registry32 && view != RegistryView.Registry64) {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidRegistryViewCheck, ExceptionArgument.view);
            }
        }


#if FEATURE_MACL
        static private void ValidateKeyRights(int rights) {
            if(0 != (rights & ~((int)RegistryRights.FullControl))) {
                // We need to throw SecurityException here for compatiblity reason,
                // although UnauthorizedAccessException will make more sense.
                ThrowHelper.ThrowSecurityException(ExceptionResource.Security_RegistryPermission);           
            }            
        }
#endif
        // Win32 constants for error handling
        private const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_SYSTEM    = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;
    }

    [Flags]
    public enum RegistryValueOptions {
        None = 0,
        DoNotExpandEnvironmentNames = 1
    }    

    // the name for this API is meant to mimic FileMode, which has similar values

    public enum RegistryKeyPermissionCheck {
        Default = 0,
        ReadSubTree = 1,
        ReadWriteSubTree = 2
    }    
}
