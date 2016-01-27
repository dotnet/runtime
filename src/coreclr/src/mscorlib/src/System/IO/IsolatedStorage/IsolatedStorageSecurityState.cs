// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;

namespace System.IO.IsolatedStorage {

#if FEATURE_CORECLR
#if !FEATURE_LEGACYNETCF
    public enum IsolatedStorageSecurityOptions {
        GetRootUserDirectory = 0,
        GetGroupAndIdForApplication = 1,
        GetGroupAndIdForSite = 2,
        IncreaseQuotaForGroup = 3,
        IncreaseQuotaForApplication = 4,
        SetInnerException = 5
    }
#else  // !FEATURE_LEGACYNETCF
    public enum IsolatedStorageSecurityOptions {
        GetRootUserDirectory = 0,
        GetGroupAndIdForApplication = 1,
        GetGroupAndIdForSite = 2,
        IncreaseQuotaForGroup = 3,
        DefaultQuotaForGroup = 4,
        AvailableFreeSpace = 5,
        IsolatedStorageFolderName = 6
    }
#endif  // !FEATURE_LEGACYNETCF
#else // FEATURE_CORECLR
    public enum IsolatedStorageSecurityOptions {
        IncreaseQuotaForApplication = 4
    }
#endif // !FEATURE_CORECLR

    [SecurityCritical]
    public class IsolatedStorageSecurityState : SecurityState {

        private Int64 m_UsedSize;
        private Int64 m_Quota;

#if FEATURE_CORECLR
        private string m_Id;
        private string m_Group;
        private string m_RootUserDirectory;
#endif // FEATURE_CORECLR

#if FEATURE_LEGACYNETCF
        private string m_IsolatedStorageFolderName;
        private Int64 m_AvailableFreeSpace;
        private bool m_AvailableFreeSpaceComputed;
#endif // FEATURE_LEGACYNETCF

        private IsolatedStorageSecurityOptions m_Options;


#if FEATURE_CORECLR

        internal static IsolatedStorageSecurityState CreateStateToGetRootUserDirectory() {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetRootUserDirectory;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToGetGroupAndIdForApplication() {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetGroupAndIdForApplication;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToGetGroupAndIdForSite() {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.GetGroupAndIdForSite;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateToIncreaseQuotaForGroup(String group, Int64 newQuota, Int64 usedSize) {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.IncreaseQuotaForGroup;
            state.m_Group = group;
            state.m_Quota = newQuota;
            state.m_UsedSize = usedSize;
            return state;
        }

#if !FEATURE_LEGACYNETCF
        internal static IsolatedStorageSecurityState CreateStateToCheckSetInnerException() {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.SetInnerException;
            return state;
        }
#endif

#if FEATURE_LEGACYNETCF
        internal static IsolatedStorageSecurityState CreateStateToGetAvailableFreeSpace() {
             IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.AvailableFreeSpace;
            return state;
        }

        internal static IsolatedStorageSecurityState CreateStateForIsolatedStorageFolderName() {
             IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.IsolatedStorageFolderName;
            return state;
        }
#endif

#endif // FEATURE_CORECLR
#if !FEATURE_LEGACYNETCF
        internal static IsolatedStorageSecurityState CreateStateToIncreaseQuotaForApplication(Int64 newQuota, Int64 usedSize) {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.m_Options = IsolatedStorageSecurityOptions.IncreaseQuotaForApplication;
            state.m_Quota = newQuota;
            state.m_UsedSize = usedSize;
            return state;
        }
#endif // !FEATURE_LEGACYNETCF

        [SecurityCritical]
        private IsolatedStorageSecurityState() {

        }

        public IsolatedStorageSecurityOptions Options {
            get {
                return m_Options;
            }
        }

#if FEATURE_CORECLR

        public String Group {

            get {
                return m_Group;
            }

            set {
                m_Group = value;
            }
        }

        public String Id {

            get {
                return m_Id;
            }

            set {
                m_Id = value;
            }
        }

        public String RootUserDirectory {

            get {
                return m_RootUserDirectory;
            }

            set {
                m_RootUserDirectory = value;
            }
        }

#endif // FEATURE_CORECLR

        public Int64 UsedSize {
            get {
                return m_UsedSize;
            }
        }

        public Int64 Quota {
            get {
                return m_Quota;
            }

            set {
                m_Quota = value;
            }
        }

#if FEATURE_LEGACYNETCF
        public Int64 AvailableFreeSpace {
            get {
                return m_AvailableFreeSpace;
            }
            set {
                m_AvailableFreeSpace = value;
                m_AvailableFreeSpaceComputed = true;
            }
        }

        public bool AvailableFreeSpaceComputed {
            get { return m_AvailableFreeSpaceComputed; }
            set { m_AvailableFreeSpaceComputed = value; }
        }

        public string IsolatedStorageFolderName {
            get { return m_IsolatedStorageFolderName; }
            set { m_IsolatedStorageFolderName = value; }
        }
#endif

        [SecurityCritical]
        public override void EnsureState() {
            if(!IsStateAvailable()) {
                throw new IsolatedStorageException(Environment.GetResourceString("IsolatedStorage_Operation"));
            }
        }
    }
}
