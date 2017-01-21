// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Class: AppDomainSetup
** 
** Purpose: Defines the settings that the loader uses to find assemblies in an
**          AppDomain
**
** Date: Dec 22, 2000
**
=============================================================================*/

namespace System
{
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Policy;
    using Path = System.IO.Path;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Collections.Generic;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AppDomainSetup
    {
        [Serializable]
        internal enum LoaderInformation
        {
            // If you add a new value, add the corresponding property
            // to AppDomain.GetData() and SetData()'s switch statements,
            // as well as fusionsetup.h.
            ApplicationBaseValue          = 0,  // LOADER_APPLICATION_BASE
            ConfigurationFileValue        = 1,  // LOADER_CONFIGURATION_BASE
            DynamicBaseValue              = 2,  // LOADER_DYNAMIC_BASE
            DevPathValue                  = 3,  // LOADER_DEVPATH
            ApplicationNameValue          = 4,  // LOADER_APPLICATION_NAME
            PrivateBinPathValue           = 5,  // LOADER_PRIVATE_PATH
            PrivateBinPathProbeValue      = 6,  // LOADER_PRIVATE_BIN_PATH_PROBE
            ShadowCopyDirectoriesValue    = 7,  // LOADER_SHADOW_COPY_DIRECTORIES
            ShadowCopyFilesValue          = 8,  // LOADER_SHADOW_COPY_FILES
            CachePathValue                = 9,  // LOADER_CACHE_PATH
            LicenseFileValue              = 10, // LOADER_LICENSE_FILE
            DisallowPublisherPolicyValue  = 11, // LOADER_DISALLOW_PUBLISHER_POLICY
            DisallowCodeDownloadValue     = 12, // LOADER_DISALLOW_CODE_DOWNLOAD
            DisallowBindingRedirectsValue = 13, // LOADER_DISALLOW_BINDING_REDIRECTS
            DisallowAppBaseProbingValue   = 14, // LOADER_DISALLOW_APPBASE_PROBING
            ConfigurationBytesValue       = 15, // LOADER_CONFIGURATION_BYTES
            LoaderMaximum                 = 18  // LOADER_MAXIMUM
        }

        // Constants from fusionsetup.h.
        private const string LOADER_OPTIMIZATION = "LOADER_OPTIMIZATION";
        private const string CONFIGURATION_EXTENSION = ".config";
        private const string APPENV_RELATIVEPATH = "RELPATH";
        private const string MACHINE_CONFIGURATION_FILE = "config\\machine.config";
        private const string ACTAG_HOST_CONFIG_FILE = "HOST_CONFIG";

        // Constants from fusionpriv.h
        private const string ACTAG_APP_CONFIG_FILE = "APP_CONFIG_FILE";
        private const string ACTAG_MACHINE_CONFIG = "MACHINE_CONFIG";
        private const string ACTAG_APP_BASE_URL = "APPBASE";
        private const string ACTAG_APP_NAME = "APP_NAME";
        private const string ACTAG_BINPATH_PROBE_ONLY = "BINPATH_PROBE_ONLY";
        private const string ACTAG_APP_CACHE_BASE = "CACHE_BASE";
        private const string ACTAG_DEV_PATH = "DEV_PATH";
        private const string ACTAG_APP_DYNAMIC_BASE = "DYNAMIC_BASE";
        private const string ACTAG_FORCE_CACHE_INSTALL = "FORCE_CACHE_INSTALL";
        private const string ACTAG_APP_PRIVATE_BINPATH = "PRIVATE_BINPATH";
        private const string ACTAG_APP_SHADOW_COPY_DIRS = "SHADOW_COPY_DIRS";
        private const string ACTAG_DISALLOW_APPLYPUBLISHERPOLICY = "DISALLOW_APP";
        private const string ACTAG_CODE_DOWNLOAD_DISABLED = "CODE_DOWNLOAD_DISABLED";
        private const string ACTAG_DISALLOW_APP_BINDING_REDIRECTS = "DISALLOW_APP_REDIRECTS";
        private const string ACTAG_DISALLOW_APP_BASE_PROBING = "DISALLOW_APP_BASE_PROBING";
        private const string ACTAG_APP_CONFIG_BLOB = "APP_CONFIG_BLOB";

        // This class has an unmanaged representation so be aware you will need to make edits in vm\object.h if you change the order
        // of these fields or add new ones.

        private string[] _Entries;
        private LoaderOptimization _LoaderOptimization;
#pragma warning disable 169
        private String _AppBase; // for compat with v1.1
#pragma warning restore 169
        [OptionalField(VersionAdded = 2)]
        private AppDomainInitializer  _AppDomainInitializer;
        [OptionalField(VersionAdded = 2)]
        private string[] _AppDomainInitializerArguments;

        // On the CoreCLR, this contains just the name of the permission set that we install in the new appdomain.
        // Not the ToXml().ToString() of an ApplicationTrust object.
        [OptionalField(VersionAdded = 2)]
        private string _ApplicationTrust;
        [OptionalField(VersionAdded = 2)]
        private byte[] _ConfigurationBytes;
#if FEATURE_COMINTEROP
        [OptionalField(VersionAdded = 3)]
        private bool _DisableInterfaceCache = false;
#endif // FEATURE_COMINTEROP
        [OptionalField(VersionAdded = 4)]
        private string _AppDomainManagerAssembly;
        [OptionalField(VersionAdded = 4)]
        private string _AppDomainManagerType;

        // A collection of strings used to indicate which breaking changes shouldn't be applied
        // to an AppDomain. We only use the keys, the values are ignored.
        [OptionalField(VersionAdded = 4)]
        private Dictionary<string, object> _CompatFlags;

        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private String _TargetFrameworkName;

        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private bool _CheckedForTargetFrameworkName;

#if FEATURE_RANDOMIZED_STRING_HASHING
        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private bool _UseRandomizedStringHashing;
#endif

        internal AppDomainSetup(AppDomainSetup copy, bool copyDomainBoundData)
        {
            string[] mine = Value;
            if(copy != null) {
                string[] other = copy.Value;
                int mineSize = _Entries.Length;
                int otherSize = other.Length;
                int size = (otherSize < mineSize) ? otherSize : mineSize;

                for (int i = 0; i < size; i++)
                    mine[i] = other[i];

                if (size < mineSize)
                {
                    // This case can happen when the copy is a deserialized version of
                    // an AppDomainSetup object serialized by Everett.
                    for (int i = size; i < mineSize; i++)
                        mine[i] = null;
                }

                _LoaderOptimization = copy._LoaderOptimization;

                _AppDomainInitializerArguments = copy.AppDomainInitializerArguments;
                _ApplicationTrust = copy._ApplicationTrust;

                if (copyDomainBoundData)
                    _AppDomainInitializer = copy.AppDomainInitializer;
                else
                    _AppDomainInitializer = null;

                _ConfigurationBytes = copy.GetConfigurationBytes();
#if FEATURE_COMINTEROP
                _DisableInterfaceCache = copy._DisableInterfaceCache;
#endif // FEATURE_COMINTEROP
                _AppDomainManagerAssembly = copy.AppDomainManagerAssembly;
                _AppDomainManagerType = copy.AppDomainManagerType;

                if (copy._CompatFlags != null)
                {
                    SetCompatibilitySwitches(copy._CompatFlags.Keys);
                }

                _TargetFrameworkName = copy._TargetFrameworkName;

#if FEATURE_RANDOMIZED_STRING_HASHING
                _UseRandomizedStringHashing = copy._UseRandomizedStringHashing;
#endif

            }
            else 
                _LoaderOptimization = LoaderOptimization.NotSpecified;
        }

        public AppDomainSetup()
        {
            _LoaderOptimization = LoaderOptimization.NotSpecified;
        }

        internal void SetupDefaults(string imageLocation, bool imageLocationAlreadyNormalized = false) {
            char[] sep = {'\\', '/'};
            int i = imageLocation.LastIndexOfAny(sep);

            if (i == -1) {
                ApplicationName = imageLocation;
            }
            else {
                ApplicationName = imageLocation.Substring(i+1);
                string appBase = imageLocation.Substring(0, i+1);

                if (imageLocationAlreadyNormalized)
                    Value[(int) LoaderInformation.ApplicationBaseValue] = appBase;
                else 
                    ApplicationBase = appBase;
            }
            ConfigurationFile = ApplicationName + AppDomainSetup.ConfigurationExtension;
        }

        internal string[] Value
        {
            get {
                if( _Entries == null)
                    _Entries = new String[(int)LoaderInformation.LoaderMaximum];
                return _Entries;
            }
        }

        internal String GetUnsecureApplicationBase()
        {
            return Value[(int) LoaderInformation.ApplicationBaseValue];
        }

        public string AppDomainManagerAssembly
        {
            get { return _AppDomainManagerAssembly; }
            set { _AppDomainManagerAssembly = value; }
        }

        public string AppDomainManagerType
        {
            get { return _AppDomainManagerType; }
            set { _AppDomainManagerType = value; }
        }

        public String ApplicationBase
        {
            [Pure]
            get {
                return VerifyDir(GetUnsecureApplicationBase(), false);
            }

            set {
                Value[(int) LoaderInformation.ApplicationBaseValue] = NormalizePath(value, false);
            }
        }

        private String NormalizePath(String path, bool useAppBase)
        {
            if(path == null)
                return null;

            // If we add very long file name support ("\\?\") to the Path class then this is unnecesary,
            // but we do not plan on doing this for now.

            // Long path checks can be quirked, and as loading default quirks too early in the setup of an AppDomain is risky
            // we'll avoid checking path lengths- we'll still fail at MAX_PATH later if we're !useAppBase when we call Path's
            // NormalizePath.
            if (!useAppBase)
                path = Security.Util.URLString.PreProcessForExtendedPathRemoval(
                    checkPathLength: false,
                    url: path,
                    isFileUrl: false);


            int len = path.Length;
            if (len == 0)
                return null;

#if !PLATFORM_UNIX
            bool UNCpath = false;
#endif // !PLATFORM_UNIX

            if ((len > 7) &&
                (String.Compare( path, 0, "file:", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)) {
                int trim;
                
                if (path[6] == '\\') {
                    if ((path[7] == '\\') || (path[7] == '/')) {

                        // Don't allow "file:\\\\", because we can't tell the difference
                        // with it for "file:\\" + "\\server" and "file:\\\" + "\localpath"
                        if ( (len > 8) && 
                             ((path[8] == '\\') || (path[8] == '/')) )
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
                        
                        // file:\\\ means local path
                        else
#if !PLATFORM_UNIX
                            trim = 8;
#else
                            // For Unix platform, trim the first 7 charcaters only.
                            // Trimming the first 8 characters will cause
                            // the root path separator to be trimmed away,
                            // and the absolute local path becomes a relative local path.
                            trim = 7;
#endif // !PLATFORM_UNIX
                    }

                    // file:\\ means remote server
                    else {
                        trim = 5;
#if !PLATFORM_UNIX
                        UNCpath = true;
#endif // !PLATFORM_UNIX
                    }
                }

                // local path
                else if (path[7] == '/')
#if !PLATFORM_UNIX
                    trim = 8;
#else
                    // For Unix platform, trim the first 7 characters only.
                    // Trimming the first 8 characters will cause
                    // the root path separator to be trimmed away,
                    // and the absolute local path becomes a relative local path.
                    trim = 7;
#endif // !PLATFORM_UNIX

                // remote
                else {
                    // file://\\remote
                    if ( (len > 8) && (path[7] == '\\') && (path[8] == '\\') )
                        trim = 7;
                    else { // file://remote
                        trim = 5;
#if !PLATFORM_UNIX
                        // Create valid UNC path by changing
                        // all occurences of '/' to '\\' in path
                        System.Text.StringBuilder winPathBuilder =
                            new System.Text.StringBuilder(len);
                        for (int i = 0; i < len; i++) {
                            char c = path[i];
                            if (c == '/')
                                winPathBuilder.Append('\\');
                            else
                                winPathBuilder.Append(c);
                        }
                        path = winPathBuilder.ToString();
#endif // !PLATFORM_UNIX
                    }
#if !PLATFORM_UNIX
                    UNCpath = true;
#endif // !PLATFORM_UNIX
                }

                path = path.Substring(trim);
                len -= trim;
            }

#if !PLATFORM_UNIX
            bool localPath;

            // UNC
            if (UNCpath ||
                ( (len > 1) &&
                  ( (path[0] == '/') || (path[0] == '\\') ) &&
                  ( (path[1] == '/') || (path[1] == '\\') ) ))
                localPath = false;

            else {
                int colon = path.IndexOf(':') + 1;

                // protocol other than file:
                if ((colon != 0) &&
                    (len > colon+1) &&
                    ( (path[colon] == '/') || (path[colon] == '\\') ) &&
                    ( (path[colon+1] == '/') || (path[colon+1] == '\\') ))
                    localPath = false;

                else
                    localPath = true;
            }

            if (localPath) 
#else
            if ( (len == 1) ||
                 ( (path[0] != '/') && (path[0] != '\\') ) ) 
#endif // !PLATFORM_UNIX
            {

                if (useAppBase &&
                    ( (len == 1) || (path[1] != ':') )) {
                    String appBase = Value[(int) LoaderInformation.ApplicationBaseValue];

                    if ((appBase == null) || (appBase.Length == 0))
                        throw new MemberAccessException(Environment.GetResourceString("AppDomain_AppBaseNotSet"));

                    StringBuilder result = StringBuilderCache.Acquire();

                    bool slash = false;
                    if ((path[0] == '/') || (path[0] == '\\')) {
                        string pathRoot = AppDomain.NormalizePath(appBase, fullCheck: false);
                        pathRoot = pathRoot.Substring(0, IO.PathInternal.GetRootLength(pathRoot));

                        if (pathRoot.Length == 0) { // URL
                            int index = appBase.IndexOf(":/", StringComparison.Ordinal);
                            if (index == -1)
                                index = appBase.IndexOf(":\\", StringComparison.Ordinal);

                            // Get past last slashes of "url:http://"
                            int urlLen = appBase.Length;
                            for (index += 1;
                                 (index < urlLen) && ((appBase[index] == '/') || (appBase[index] == '\\'));
                                 index++);

                            // Now find the next slash to get domain name
                            for(; (index < urlLen) && (appBase[index] != '/') && (appBase[index] != '\\');
                                index++);

                            pathRoot = appBase.Substring(0, index);
                        }

                        result.Append(pathRoot);
                        slash = true;
                    }
                    else
                        result.Append(appBase);

                    // Make sure there's a slash separator (and only one)
                    int aLen = result.Length - 1;
                    if ((result[aLen] != '/') &&
                        (result[aLen] != '\\')) {
                        if (!slash) {
#if !PLATFORM_UNIX
                            if (appBase.IndexOf(":/", StringComparison.Ordinal) == -1)
                                result.Append('\\');
                            else
#endif // !PLATFORM_UNIX
                                result.Append('/');
                        }
                    }
                    else if (slash)
                        result.Remove(aLen, 1);

                    result.Append(path);
                    path = StringBuilderCache.GetStringAndRelease(result);
                }
                else
                    path = AppDomain.NormalizePath(path, fullCheck: true);
            }

            return path;
        }

        public String ConfigurationFile
        {
            get {
                return VerifyDir(Value[(int) LoaderInformation.ConfigurationFileValue], true);
            }

            set {
                Value[(int) LoaderInformation.ConfigurationFileValue] = value;
            }
        }

        public byte[] GetConfigurationBytes()
        {
            if (_ConfigurationBytes == null)
                return null;

            return (byte[]) _ConfigurationBytes.Clone();
        }

        // only needed by AppDomain.Setup(). Not really needed by users. 
        internal Dictionary<string, object> GetCompatibilityFlags()
        {
            return _CompatFlags;
        }

        public void SetCompatibilitySwitches(IEnumerable<String> switches)
        {
#if FEATURE_RANDOMIZED_STRING_HASHING
            _UseRandomizedStringHashing = false;
#endif
            if (switches != null)
            {
                _CompatFlags = new Dictionary<string, object>();
                foreach (String str in switches) 
                {
#if FEATURE_RANDOMIZED_STRING_HASHING
                    if(StringComparer.OrdinalIgnoreCase.Equals("UseRandomizedStringHashAlgorithm", str)) {
                        _UseRandomizedStringHashing = true;
                    }
#endif
                    _CompatFlags.Add(str, null);
                }
            }
            else
            {
                _CompatFlags = null;
            }

        }

        // A target Framework moniker, in a format parsible by the FrameworkName class.
        public String TargetFrameworkName {
            get {
                return _TargetFrameworkName;
            }
            set {
                _TargetFrameworkName = value;
            }
        }

        private String VerifyDir(String dir, bool normalize)
        {
            if (dir != null) {
                if (dir.Length == 0)
                    dir = null;
                else {
                    if (normalize)
                        dir = NormalizePath(dir, true);
                }
            }

            return dir;
        }

        public String ApplicationName
        {
            get {
                return Value[(int) LoaderInformation.ApplicationNameValue];
            }

            set {
                Value[(int) LoaderInformation.ApplicationNameValue] = value;
            }
        }

        [XmlIgnoreMember]
        public AppDomainInitializer AppDomainInitializer
        {
            get {
                return _AppDomainInitializer;
            }

            set {
                _AppDomainInitializer = value;
            }
        }
        public string[] AppDomainInitializerArguments
        {
            get {
                return _AppDomainInitializerArguments;
            }

            set {
                _AppDomainInitializerArguments = value;
            }
        }

        internal ApplicationTrust InternalGetApplicationTrust()
        {
            if (_ApplicationTrust == null) return null;
            ApplicationTrust grantSet = new ApplicationTrust(NamedPermissionSet.GetBuiltInSet(_ApplicationTrust));
            return grantSet;
        }

        internal void InternalSetApplicationTrust(String permissionSetName)
        {
            _ApplicationTrust = permissionSetName;
        }

        [XmlIgnoreMember]
        internal ApplicationTrust ApplicationTrust
        {
            get
            {
                return InternalGetApplicationTrust();
            }
        }

        public LoaderOptimization LoaderOptimization
        {
            get {
                return _LoaderOptimization;
            }

            set {
                _LoaderOptimization = value;
            }
        }

        internal static string LoaderOptimizationKey
        {
            get {
                return LOADER_OPTIMIZATION;
            }
        }

        internal static string ConfigurationExtension
        {
            get {
                return CONFIGURATION_EXTENSION;
            }
        }

        static internal int Locate(String s)
        {
            if(String.IsNullOrEmpty(s))
                return -1;

            Debug.Assert('A' == ACTAG_APP_BASE_URL[0]        , "Assumption violated");
            if (s[0]=='A' && s == ACTAG_APP_BASE_URL)        
                return (int)LoaderInformation.ApplicationBaseValue;

            return -1;
        }

#if FEATURE_COMINTEROP
        public bool SandboxInterop
        {
            get
            {
                return _DisableInterfaceCache;
            }
            set
            {
                _DisableInterfaceCache = value;
            }
        }
#endif // FEATURE_COMINTEROP
    }
}
