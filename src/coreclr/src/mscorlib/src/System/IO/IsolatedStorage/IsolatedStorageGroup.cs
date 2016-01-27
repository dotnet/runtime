// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Security.Permissions;

namespace System.IO.IsolatedStorage {

    public class IsolatedStorageGroup {
        public string m_Group;
        public Int64 m_Quota;
        public Int64 m_UsedSize;
        private string m_GroupPath;
        private string m_ObfuscatedId;

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        internal IsolatedStorageGroup(string group, long quota, long used, string groupPath) {
            m_Group = group;
            m_Quota = quota;
            m_UsedSize = used;
            m_ObfuscatedId = DirectoryInfo.UnsafeCreateDirectoryInfo(groupPath).Name;
            m_GroupPath = groupPath;
        }

        public string Group {
            get {
                return m_Group;
            }
        }

        public Int64 Quota {
            get {
                return m_Quota;
            }
        }

        public Int64 UsedSize {
            get {
                return m_UsedSize;
            }
        }

        public static Boolean Enabled {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            get {
                try {
                    return !File.UnsafeExists(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                } catch (IsolatedStorageException) {
                    // IsolatedStorageRoot will throw this if the host doesn't hand back a root, which can happen when running in "Private Browsing Mode".
                    return false;
                }
            }

            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
            set {
                if (!value) {
                    IsolatedStorageFile.TouchFile(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                } else {
                    try {
                        File.UnsafeDelete(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.c_DisabledFileName));
                    } catch (IOException) {
                        // If we couldn't delete the file there's nothing much we can do.
                    }
                }
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public void Remove() {
#if !FEATURE_LEGACYNETCF
            bool removedAll = true;
            FileLock groupLock = FileLock.GetFileLock(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.s_LockPathPrefix), IsolatedStorageFile.s_GroupPathPrefix + "-" + m_ObfuscatedId);

            try {
                groupLock.Lock();

                foreach (string storeDir in Directory.UnsafeGetDirectories(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.s_StorePathPrefix), "*", SearchOption.TopDirectoryOnly)) {
                    string groupFile = Path.Combine(storeDir, IsolatedStorageFile.s_GroupFileName);

                    if (m_ObfuscatedId.Equals(File.UnsafeReadAllText(groupFile))) {
                        IsolatedStorageFile f = IsolatedStorageFile.GetUserStoreFromGroupAndStorePath(Group, storeDir);
                        removedAll = removedAll & f.TryRemove();
                    }
                }

                IsolatedStorageFile.TouchFile(Path.Combine(m_GroupPath, IsolatedStorageFile.s_CleanupFileName));

                if (removedAll) {
                    IsolatedStorageAccountingInfo.RemoveAccountingInfo(m_GroupPath);
                    File.UnsafeDelete(Path.Combine(m_GroupPath, IsolatedStorageFile.s_IdFileName));
                    File.UnsafeDelete(Path.Combine(m_GroupPath, IsolatedStorageFile.s_CleanupFileName));
                    Directory.UnsafeDelete(m_GroupPath, false);
                }
            } catch (IOException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.
            } catch (UnauthorizedAccessException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
            } finally {
                groupLock.Unlock();
            }
#else // !FEATURE_LEGACYNETCF
            try {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                    isf.Remove();
                }
            } catch (IOException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.
            } catch (UnauthorizedAccessException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
            }
#endif // !FEATURE_LEGACYNETCF
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public static IEnumerable<IsolatedStorageGroup> GetGroups() {

            List<IsolatedStorageGroup> groups = new List<IsolatedStorageGroup>();
#if !FEATURE_LEGACYNETCF
            try {
                foreach (string groupDir in Directory.UnsafeGetDirectories(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.s_GroupPathPrefix), "*", SearchOption.TopDirectoryOnly)) {
                    string idFile = Path.Combine(groupDir, IsolatedStorageFile.s_IdFileName);
                    string id;

                    FileLock groupLock = FileLock.GetFileLock(Path.Combine(IsolatedStorageFile.IsolatedStorageRoot, IsolatedStorageFile.s_LockPathPrefix), IsolatedStorageFile.s_GroupPathPrefix + "-" + DirectoryInfo.UnsafeCreateDirectoryInfo(groupDir).Name);

                    try {
                        groupLock.Lock();

                        if(!File.UnsafeExists(Path.Combine(groupDir, IsolatedStorageFile.s_CleanupFileName))) {
                            id = File.UnsafeReadAllText(idFile);

                            if (IsolatedStorageAccountingInfo.IsAccountingInfoValid(groupDir)) {
                                using (IsolatedStorageAccountingInfo accountingInfo = new IsolatedStorageAccountingInfo(groupDir)) {
                                    groups.Add(new IsolatedStorageGroup(id, accountingInfo.Quota, accountingInfo.UsedSize, groupDir));
                                }
                            } else {
                                // In this case we've tried to deseriaize a group that doesn't have valid data.  We'll try to remove it.                                
                                try {
                                    new IsolatedStorageGroup(id, 0, 0, groupDir).Remove();
                                } catch (Exception) {
                                    // We couldn't remove the group for some reason.  Ignore it and move on.
                                }
                            }
                        }
                    } catch (IOException) {
                        // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.
                    } catch (UnauthorizedAccessException) {
                        // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
                    } finally {
                        groupLock.Unlock();
                    }
                }
            } catch (IOException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.
            } catch (UnauthorizedAccessException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
            }           
#else
            try {
                using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                    groups.Add(new IsolatedStorageGroup(isf.GroupName, Int64.MaxValue, 0, IsolatedStorageFile.IsolatedStorageRoot));
                }
            } catch (IOException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.
            } catch (UnauthorizedAccessException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
            } catch (IsolatedStorageException) {
                // There isn't anything we can really do about this.  Ignoring these sorts of issues shouldn't lead to corruption.          
            }
#endif

            return groups;
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public static void RemoveAll() {
            foreach (IsolatedStorageGroup g in GetGroups()) {
                g.Remove();
            }
        }
    }
}
