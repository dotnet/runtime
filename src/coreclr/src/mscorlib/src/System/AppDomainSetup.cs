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

namespace System {
    using System;
#if FEATURE_CLICKONCE        
    using System.Deployment.Internal.Isolation;
    using System.Deployment.Internal.Isolation.Manifest;
    using System.Runtime.Hosting;    
#endif
    using System.Runtime.CompilerServices;
    using System.Runtime;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Globalization;
    using Path = System.IO.Path;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AppDomainSetup :
        IAppDomainSetup
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

#if FEATURE_FUSION
        private const string LICENSE_FILE = "LICENSE_FILE";
#endif

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
#if FEATURE_CLICKONCE
        [OptionalField(VersionAdded = 2)]
        private ActivationArguments _ActivationArguments;
#endif
#if FEATURE_CORECLR
        // On the CoreCLR, this contains just the name of the permission set that we install in the new appdomain.
        // Not the ToXml().ToString() of an ApplicationTrust object.
#endif
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

#if FEATURE_APTCA
        [OptionalField(VersionAdded = 4)]
        private string[] _AptcaVisibleAssemblies;
#endif

        // A collection of strings used to indicate which breaking changes shouldn't be applied
        // to an AppDomain. We only use the keys, the values are ignored.
        [OptionalField(VersionAdded = 4)]
        private Dictionary<string, object> _CompatFlags;

        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private String _TargetFrameworkName;

#if !FEATURE_CORECLR
        [NonSerialized]
        internal AppDomainSortingSetupInfo _AppDomainSortingSetupInfo;
#endif

        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private bool _CheckedForTargetFrameworkName;

#if FEATURE_RANDOMIZED_STRING_HASHING
        [OptionalField(VersionAdded = 5)] // This was added in .NET FX v4.5
        private bool _UseRandomizedStringHashing;
#endif

        [SecuritySafeCritical]
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
#if FEATURE_CLICKONCE
                _ActivationArguments = copy.ActivationArguments;
#endif
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
#if FEATURE_APTCA
                _AptcaVisibleAssemblies = copy.PartialTrustVisibleAssemblies;
#endif

                if (copy._CompatFlags != null)
                {
                    SetCompatibilitySwitches(copy._CompatFlags.Keys);
                }

#if !FEATURE_CORECLR
                if(copy._AppDomainSortingSetupInfo != null)
                {
                    _AppDomainSortingSetupInfo = new AppDomainSortingSetupInfo(copy._AppDomainSortingSetupInfo);
                }
#endif
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

#if FEATURE_CLICKONCE
        // Creates an AppDomainSetup object from an application identity.
        public AppDomainSetup (ActivationContext activationContext) : this (new ActivationArguments(activationContext)) {}

        [System.Security.SecuritySafeCritical]  // auto-generated
        public AppDomainSetup (ActivationArguments activationArguments) {
            if (activationArguments == null)
                throw new ArgumentNullException("activationArguments");
            Contract.EndContractBlock();

            _LoaderOptimization = LoaderOptimization.NotSpecified;
            ActivationArguments = activationArguments;

            Contract.Assert(activationArguments.ActivationContext != null, "Cannot set base directory without activation context");
            string entryPointPath = CmsUtils.GetEntryPointFullPath(activationArguments);
            if (!String.IsNullOrEmpty(entryPointPath))
                SetupDefaults(entryPointPath);
            else
                ApplicationBase = activationArguments.ActivationContext.ApplicationDirectory;

        }
#endif // !FEATURE_CLICKONCE

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
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

#if FEATURE_APTCA
        public string[] PartialTrustVisibleAssemblies
        {
            get { return _AptcaVisibleAssemblies; }
            set { 
                if (value != null) {
                    _AptcaVisibleAssemblies = (string[])value.Clone(); 
                    Array.Sort<string>(_AptcaVisibleAssemblies, StringComparer.OrdinalIgnoreCase);
                }
                else {
                    _AptcaVisibleAssemblies = null;
                }
            }
        }
#endif

        public String ApplicationBase
        {
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #else
            [System.Security.SecuritySafeCritical]
            #endif
            [Pure]
            get {
                return VerifyDir(GetUnsecureApplicationBase(), false);
            }

            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #endif
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

        private bool IsFilePath(String path)
        {
#if !PLATFORM_UNIX
            return (path[1] == ':') || ( (path[0] == '\\') && (path[1] == '\\') );
#else
            return (path[0] == '/');
#endif // !PLATFORM_UNIX
        }

        internal static String ApplicationBaseKey
        {
            get {
                return ACTAG_APP_BASE_URL;
            }
        }

        public String ConfigurationFile
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return VerifyDir(Value[(int) LoaderInformation.ConfigurationFileValue], true);
            }

            set {
                Value[(int) LoaderInformation.ConfigurationFileValue] = value;
            }
        }

        // Used by the ResourceManager internally.  This must not do any 
        // security checks to avoid infinite loops.
        internal String ConfigurationFileInternal
        {
            get {
                return NormalizePath(Value[(int) LoaderInformation.ConfigurationFileValue], true);
            }
        }

        internal static String ConfigurationFileKey
        {
            get {
                return ACTAG_APP_CONFIG_FILE;
            }
        }

        public byte[] GetConfigurationBytes()
        {
            if (_ConfigurationBytes == null)
                return null;

            return (byte[]) _ConfigurationBytes.Clone();
        }

        public void SetConfigurationBytes(byte[] value)
        {
            _ConfigurationBytes = value;
        }

        private static String ConfigurationBytesKey
        {
            get {
                return ACTAG_APP_CONFIG_BLOB;
            }
        }

        // only needed by AppDomain.Setup(). Not really needed by users. 
        internal Dictionary<string, object> GetCompatibilityFlags()
        {
            return _CompatFlags;
        }

        public void SetCompatibilitySwitches(IEnumerable<String> switches)
        {

#if !FEATURE_CORECLR
            if(_AppDomainSortingSetupInfo != null)
            {
                _AppDomainSortingSetupInfo._useV2LegacySorting = false;
                _AppDomainSortingSetupInfo._useV4LegacySorting = false;
            }
#endif

#if FEATURE_RANDOMIZED_STRING_HASHING
            _UseRandomizedStringHashing = false;
#endif
            if (switches != null)
            {
                _CompatFlags = new Dictionary<string, object>();
                foreach (String str in switches) 
                {
#if !FEATURE_CORECLR
                    if(StringComparer.OrdinalIgnoreCase.Equals("NetFx40_Legacy20SortingBehavior", str)) {
                        if(_AppDomainSortingSetupInfo == null)
                        {
                            _AppDomainSortingSetupInfo = new AppDomainSortingSetupInfo();
                        }
                        _AppDomainSortingSetupInfo._useV2LegacySorting = true;
                    }

                    if(StringComparer.OrdinalIgnoreCase.Equals("NetFx45_Legacy40SortingBehavior", str)) {
                        if(_AppDomainSortingSetupInfo == null)
                        {
                            _AppDomainSortingSetupInfo = new AppDomainSortingSetupInfo();
                        }
                        _AppDomainSortingSetupInfo._useV4LegacySorting = true;
                    }
#endif

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

        internal bool CheckedForTargetFrameworkName
        {
            get { return _CheckedForTargetFrameworkName; }
            set { _CheckedForTargetFrameworkName = value; }
        }

#if !FEATURE_CORECLR
        [SecurityCritical]
        public void SetNativeFunction(string functionName, int functionVersion, IntPtr functionPointer) 
        {
            if(functionName == null) 
            {
                throw new ArgumentNullException("functionName");
            }

            if(functionPointer == IntPtr.Zero)
            {
                throw new ArgumentNullException("functionPointer");
            }

            if(String.IsNullOrWhiteSpace(functionName))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_NPMSInvalidName"), "functionName");
            }

            Contract.EndContractBlock();

            if(functionVersion < 1)
            {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_MinSortingVersion", 1, functionName));
            }

            if(_AppDomainSortingSetupInfo == null)
            {
                _AppDomainSortingSetupInfo = new AppDomainSortingSetupInfo();
            }

            if(String.Equals(functionName, "IsNLSDefinedString", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnIsNLSDefinedString = functionPointer;
            }

            if (String.Equals(functionName, "CompareStringEx", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnCompareStringEx = functionPointer;
            }

            if (String.Equals(functionName, "LCMapStringEx", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnLCMapStringEx = functionPointer;
            }

            if (String.Equals(functionName, "FindNLSStringEx", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnFindNLSStringEx = functionPointer;
            }

            if (String.Equals(functionName, "CompareStringOrdinal", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnCompareStringOrdinal = functionPointer;
            }

            if (String.Equals(functionName, "GetNLSVersionEx", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnGetNLSVersionEx = functionPointer;
            }

            if (String.Equals(functionName, "FindStringOrdinal", StringComparison.OrdinalIgnoreCase))
            {
                _AppDomainSortingSetupInfo._pfnFindStringOrdinal = functionPointer;
            }
        }
#endif

        public String DynamicBase
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return VerifyDir(Value[(int) LoaderInformation.DynamicBaseValue], true);
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            set {
                if (value == null)
                    Value[(int) LoaderInformation.DynamicBaseValue] = null;
                else {
                    if(ApplicationName == null)
                        throw new MemberAccessException(Environment.GetResourceString("AppDomain_RequireApplicationName"));
                    
                    StringBuilder s = new StringBuilder( NormalizePath(value, false) );
                    s.Append('\\');
                    string h = ParseNumbers.IntToString(ApplicationName.GetLegacyNonRandomizedHashCode(),
                                                        16, 8, '0', ParseNumbers.PrintAsI4);
                    s.Append(h);
                    
                    Value[(int) LoaderInformation.DynamicBaseValue] = s.ToString();
                }
            }
        }

        internal static String DynamicBaseKey
        {
            get {
                return ACTAG_APP_DYNAMIC_BASE;
            }
        }


        public bool DisallowPublisherPolicy
        {
            get 
            {
                return (Value[(int) LoaderInformation.DisallowPublisherPolicyValue] != null);
            }
            set
            {
                if (value)
                    Value[(int) LoaderInformation.DisallowPublisherPolicyValue]="true";
                else
                    Value[(int) LoaderInformation.DisallowPublisherPolicyValue]=null;
            }
        }


        public bool DisallowBindingRedirects
        {
            get 
            {
                return (Value[(int) LoaderInformation.DisallowBindingRedirectsValue] != null);
            }
            set
            {
                if (value)
                    Value[(int) LoaderInformation.DisallowBindingRedirectsValue] = "true";
                else
                    Value[(int) LoaderInformation.DisallowBindingRedirectsValue] = null;
            }
        }

        public bool DisallowCodeDownload
        {
            get 
            {
                return (Value[(int) LoaderInformation.DisallowCodeDownloadValue] != null);
            }
            set
            {
                if (value)
                    Value[(int) LoaderInformation.DisallowCodeDownloadValue] = "true";
                else
                    Value[(int) LoaderInformation.DisallowCodeDownloadValue] = null;
            }
        }


        public bool DisallowApplicationBaseProbing
        {
            get 
            {
                return (Value[(int) LoaderInformation.DisallowAppBaseProbingValue] != null);
            }
            set
            {
                if (value)
                    Value[(int) LoaderInformation.DisallowAppBaseProbingValue] = "true";
                else
                    Value[(int) LoaderInformation.DisallowAppBaseProbingValue] = null;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private String VerifyDir(String dir, bool normalize)
        {
            if (dir != null) {
                if (dir.Length == 0)
                    dir = null;
                else {
                    if (normalize)
                        dir = NormalizePath(dir, true);

                // The only way AppDomainSetup is exposed in coreclr is through the AppDomainManager 
                // and the AppDomainManager is a SecurityCritical type. Also, all callers of callstacks 
                // leading from VerifyDir are SecurityCritical. So we can remove the Demand because 
                // we have validated that all callers are SecurityCritical
#if !FEATURE_CORECLR
                    if (IsFilePath(dir))
                        new FileIOPermission( FileIOPermissionAccess.PathDiscovery, dir ).Demand();
#endif // !FEATURE_CORECLR
                }
            }

            return dir;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void VerifyDirList(String dirs)
        {
            if (dirs != null) {
                String[] dirArray = dirs.Split(';');
                int len = dirArray.Length;
                
                for (int i = 0; i < len; i++)
                    VerifyDir(dirArray[i], true);
            }
        }

        internal String DeveloperPath
        {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                String dirs = Value[(int) LoaderInformation.DevPathValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set {
                if(value == null)
                    Value[(int) LoaderInformation.DevPathValue] = null;
                else {
                    String[] directories = value.Split(';');
                    int size = directories.Length;
                    StringBuilder newPath = StringBuilderCache.Acquire();
                    bool fDelimiter = false;
                        
                    for(int i = 0; i < size; i++) {
                        if(directories[i].Length != 0) {
                            if(fDelimiter) 
                                newPath.Append(";");
                            else
                                fDelimiter = true;
                            
                            newPath.Append(Path.GetFullPathInternal(directories[i]));
                        }
                    }
                    
                    String newString = StringBuilderCache.GetStringAndRelease(newPath);
                    if (newString.Length == 0)
                        Value[(int) LoaderInformation.DevPathValue] = null;
                    else
                        Value[(int) LoaderInformation.DevPathValue] = newString;
                }
            }
        }
        
        internal static String DisallowPublisherPolicyKey
        {
            get
            {
                return ACTAG_DISALLOW_APPLYPUBLISHERPOLICY;
            }
        }

        internal static String DisallowCodeDownloadKey
        {
            get
            {
                return ACTAG_CODE_DOWNLOAD_DISABLED;
            }
        }

        internal static String DisallowBindingRedirectsKey
        {
            get
            {
                return ACTAG_DISALLOW_APP_BINDING_REDIRECTS;
            }
        }

        internal static String DeveloperPathKey
        {
            get {
                return ACTAG_DEV_PATH;
            }
        }

        internal static String DisallowAppBaseProbingKey
        {
            get
            {
                return ACTAG_DISALLOW_APP_BASE_PROBING;
            }
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

        internal static String ApplicationNameKey
        {
            get {
                return ACTAG_APP_NAME;
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

#if FEATURE_CLICKONCE
        [XmlIgnoreMember]
        public ActivationArguments ActivationArguments {
            [Pure]
            get {
                return _ActivationArguments;
            }
            set {
                _ActivationArguments = value;
            }
        }
#endif // !FEATURE_CLICKONCE

        internal ApplicationTrust InternalGetApplicationTrust()
        {
            
            if (_ApplicationTrust == null) return null;


#if FEATURE_CORECLR            
            ApplicationTrust grantSet = new ApplicationTrust(NamedPermissionSet.GetBuiltInSet(_ApplicationTrust));
#else
            SecurityElement securityElement = SecurityElement.FromString(_ApplicationTrust);
            ApplicationTrust grantSet = new ApplicationTrust();
            grantSet.FromXml(securityElement);
#endif
            return grantSet;
        }

#if FEATURE_CORECLR
        internal void InternalSetApplicationTrust(String permissionSetName)
        {
            _ApplicationTrust = permissionSetName;
        }
#else
        internal void InternalSetApplicationTrust(ApplicationTrust value)
        {
            if (value != null)
            {
                _ApplicationTrust = value.ToXml().ToString();
            }
            else
            {
                _ApplicationTrust = null;
            }
        }
#endif

#if FEATURE_CLICKONCE
        [XmlIgnoreMember]
        public ApplicationTrust ApplicationTrust 
        {
            get {
                return InternalGetApplicationTrust();
            }
            set {
                InternalSetApplicationTrust(value);
            }
        }
#else // FEATURE_CLICKONCE
        [XmlIgnoreMember]
        internal ApplicationTrust ApplicationTrust
        {
            get {
                return InternalGetApplicationTrust();
            }
#if !FEATURE_CORECLR            
            set {
                InternalSetApplicationTrust(value);
            }
#endif 
        }
#endif // FEATURE_CLICKONCE

        public String PrivateBinPath
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                String dirs = Value[(int) LoaderInformation.PrivateBinPathValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set {
                Value[(int) LoaderInformation.PrivateBinPathValue] = value;
            }
        }

        internal static String PrivateBinPathKey
        {
            get {
                return ACTAG_APP_PRIVATE_BINPATH;
            }
        }


        public String PrivateBinPathProbe
        {
            get {
                return Value[(int) LoaderInformation.PrivateBinPathProbeValue];
            }

            set {
                Value[(int) LoaderInformation.PrivateBinPathProbeValue] = value;
            }
        }

        internal static String PrivateBinPathProbeKey
        {
            get {
                return ACTAG_BINPATH_PROBE_ONLY;
            }
        }

        public String ShadowCopyDirectories
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                String dirs = Value[(int) LoaderInformation.ShadowCopyDirectoriesValue];
                VerifyDirList(dirs);
                return dirs;
            }

            set {
                Value[(int) LoaderInformation.ShadowCopyDirectoriesValue] = value;
            }
        }

        internal static String ShadowCopyDirectoriesKey
        {
            get {
                return ACTAG_APP_SHADOW_COPY_DIRS;
            }
        }

        public String ShadowCopyFiles
        {
            get {
                return Value[(int) LoaderInformation.ShadowCopyFilesValue];
            }

            set {
                if((value != null) && 
                   (String.Compare(value, "true", StringComparison.OrdinalIgnoreCase) == 0))
                    Value[(int) LoaderInformation.ShadowCopyFilesValue] = value;
                else
                    Value[(int) LoaderInformation.ShadowCopyFilesValue] = null;
            }
        }

        internal static String ShadowCopyFilesKey
        {
            get {
                return ACTAG_FORCE_CACHE_INSTALL;
            }
        }

        public String CachePath
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return VerifyDir(Value[(int) LoaderInformation.CachePathValue], false);
            }

            set {
                Value[(int) LoaderInformation.CachePathValue] = NormalizePath(value, false);
            }
        }

        internal static String CachePathKey
        {
            get {
                return ACTAG_APP_CACHE_BASE;
            }
        }

        public String LicenseFile
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                return VerifyDir(Value[(int) LoaderInformation.LicenseFileValue], true);
            }

            set {
                Value[(int) LoaderInformation.LicenseFileValue] = value;
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

        internal static String PrivateBinPathEnvironmentVariable
        {
            get {
                return APPENV_RELATIVEPATH;
            }
        }

        internal static string RuntimeConfigurationFile
        {
            get {
                return MACHINE_CONFIGURATION_FILE;
            }
        }

        internal static string MachineConfigKey
        {
            get {
                return ACTAG_MACHINE_CONFIG;
            }
        }

        internal static string HostBindingKey
        {
            get {
                return ACTAG_HOST_CONFIG_FILE;
            }
        }

#if FEATURE_FUSION
        [SecurityCritical]
        internal bool UpdateContextPropertyIfNeeded(LoaderInformation FieldValue, String FieldKey, String UpdatedField, IntPtr fusionContext, AppDomainSetup oldADS)
        {
            String FieldString = Value[(int) FieldValue],
                   OldFieldString = (oldADS == null ? null : oldADS.Value[(int) FieldValue]);
            if (FieldString != OldFieldString) { // Compare references since strings are immutable
                UpdateContextProperty(fusionContext, FieldKey, UpdatedField == null ? FieldString : UpdatedField);
                return true;
            }

	     return false;
        }

        [SecurityCritical]
        internal void UpdateBooleanContextPropertyIfNeeded(LoaderInformation FieldValue, String FieldKey, IntPtr fusionContext, AppDomainSetup oldADS)
        {
            if (Value[(int) FieldValue] != null)
                UpdateContextProperty(fusionContext, FieldKey, "true");
            else if (oldADS != null && oldADS.Value[(int) FieldValue] != null)
                UpdateContextProperty(fusionContext, FieldKey, "false");
        }

        [SecurityCritical]
        internal static bool ByteArraysAreDifferent(Byte[] A, Byte[] B)
        {
		int length = A.Length;
		if (length != B.Length)
		    return true;

		for(int i = 0; i < length; i++) {
		    if (A[i] != B[i])
		        return true;				
	       }

		return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static void UpdateByteArrayContextPropertyIfNeeded(Byte[] NewArray, Byte[] OldArray, String FieldKey, IntPtr fusionContext)
        {
            if ((NewArray != null && OldArray == null) ||
		   (NewArray == null && OldArray != null) ||
		   (NewArray != null && OldArray != null && ByteArraysAreDifferent(NewArray, OldArray)))
                UpdateContextProperty(fusionContext, FieldKey, NewArray);
        }
		
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetupFusionContext(IntPtr fusionContext, AppDomainSetup oldADS)
        {
            UpdateContextPropertyIfNeeded(LoaderInformation.ApplicationBaseValue, ApplicationBaseKey, null, fusionContext, oldADS);
            UpdateContextPropertyIfNeeded(LoaderInformation.PrivateBinPathValue, PrivateBinPathKey, null, fusionContext, oldADS);
            UpdateContextPropertyIfNeeded(LoaderInformation.DevPathValue, DeveloperPathKey, null, fusionContext, oldADS);

            UpdateBooleanContextPropertyIfNeeded(LoaderInformation.DisallowPublisherPolicyValue, DisallowPublisherPolicyKey, fusionContext, oldADS);
            UpdateBooleanContextPropertyIfNeeded(LoaderInformation.DisallowCodeDownloadValue, DisallowCodeDownloadKey, fusionContext, oldADS);
            UpdateBooleanContextPropertyIfNeeded(LoaderInformation.DisallowBindingRedirectsValue, DisallowBindingRedirectsKey, fusionContext, oldADS);
            UpdateBooleanContextPropertyIfNeeded(LoaderInformation.DisallowAppBaseProbingValue, DisallowAppBaseProbingKey, fusionContext, oldADS);

            if(UpdateContextPropertyIfNeeded(LoaderInformation.ShadowCopyFilesValue, ShadowCopyFilesKey, ShadowCopyFiles, fusionContext, oldADS)) {

                // If we are asking for shadow copy directories then default to
                // only to the ones that are in the private bin path.
                if(Value[(int) LoaderInformation.ShadowCopyDirectoriesValue] == null)
                    ShadowCopyDirectories = BuildShadowCopyDirectories();

                UpdateContextPropertyIfNeeded(LoaderInformation.ShadowCopyDirectoriesValue, ShadowCopyDirectoriesKey, null, fusionContext, oldADS);
            }

            UpdateContextPropertyIfNeeded(LoaderInformation.CachePathValue, CachePathKey, null, fusionContext, oldADS);
            UpdateContextPropertyIfNeeded(LoaderInformation.PrivateBinPathProbeValue, PrivateBinPathProbeKey, PrivateBinPathProbe, fusionContext, oldADS);
            UpdateContextPropertyIfNeeded(LoaderInformation.ConfigurationFileValue, ConfigurationFileKey, null, fusionContext, oldADS);

            UpdateByteArrayContextPropertyIfNeeded(_ConfigurationBytes, oldADS == null ? null : oldADS.GetConfigurationBytes(), ConfigurationBytesKey, fusionContext);

            UpdateContextPropertyIfNeeded(LoaderInformation.ApplicationNameValue, ApplicationNameKey, ApplicationName, fusionContext, oldADS);
            UpdateContextPropertyIfNeeded(LoaderInformation.DynamicBaseValue, DynamicBaseKey, null, fusionContext, oldADS);

            // Always add the runtime configuration file to the appdomain
            UpdateContextProperty(fusionContext, MachineConfigKey, RuntimeEnvironment.GetRuntimeDirectoryImpl() + RuntimeConfigurationFile);

            String hostBindingFile = RuntimeEnvironment.GetHostBindingFile();
            if(hostBindingFile != null || oldADS != null) // If oldADS != null, we don't know the old value of the hostBindingFile, so we force an update even when hostBindingFile == null.
                UpdateContextProperty(fusionContext, HostBindingKey, hostBindingFile);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void UpdateContextProperty(IntPtr fusionContext, string key, Object value);
#endif // FEATURE_FUSION

        static internal int Locate(String s)
        {
            if(String.IsNullOrEmpty(s))
                return -1;
#if FEATURE_FUSION

            // verify assumptions hardcoded into the switch below
            Contract.Assert('A' == ACTAG_APP_CONFIG_FILE[0]     , "Assumption violated");
            Contract.Assert('A' == ACTAG_APP_NAME[0]            , "Assumption violated");
            Contract.Assert('A' == ACTAG_APP_BASE_URL[0]        , "Assumption violated");
            Contract.Assert('B' == ACTAG_BINPATH_PROBE_ONLY[0]  , "Assumption violated");
            Contract.Assert('C' == ACTAG_APP_CACHE_BASE[0]      , "Assumption violated");
            Contract.Assert('D' == ACTAG_DEV_PATH[0]            , "Assumption violated");
            Contract.Assert('D' == ACTAG_APP_DYNAMIC_BASE[0]    , "Assumption violated");
            Contract.Assert('F' == ACTAG_FORCE_CACHE_INSTALL[0] , "Assumption violated");
            Contract.Assert('L' == LICENSE_FILE[0]              , "Assumption violated");
            Contract.Assert('P' == ACTAG_APP_PRIVATE_BINPATH[0] , "Assumption violated");
            Contract.Assert('S' == ACTAG_APP_SHADOW_COPY_DIRS[0], "Assumption violated");
            Contract.Assert('D' == ACTAG_DISALLOW_APPLYPUBLISHERPOLICY[0], "Assumption violated");
            Contract.Assert('C' == ACTAG_CODE_DOWNLOAD_DISABLED[0], "Assumption violated");
            Contract.Assert('D' == ACTAG_DISALLOW_APP_BINDING_REDIRECTS[0], "Assumption violated");
            Contract.Assert('D' == ACTAG_DISALLOW_APP_BASE_PROBING[0], "Assumption violated");
            Contract.Assert('A' == ACTAG_APP_CONFIG_BLOB[0], "Assumption violated");

            switch (s[0]) {
                case 'A':
                    if (s == ACTAG_APP_CONFIG_FILE)     return (int)LoaderInformation.ConfigurationFileValue;
                    if (s == ACTAG_APP_NAME)            return (int)LoaderInformation.ApplicationNameValue;
                    if (s == ACTAG_APP_BASE_URL)        return (int)LoaderInformation.ApplicationBaseValue;
                    if (s == ACTAG_APP_CONFIG_BLOB)     return (int)LoaderInformation.ConfigurationBytesValue;
                    break;
                case 'B':
                    if (s == ACTAG_BINPATH_PROBE_ONLY)  return (int)LoaderInformation.PrivateBinPathProbeValue;
                    break;
                case 'C':
                    if (s == ACTAG_APP_CACHE_BASE)      return (int)LoaderInformation.CachePathValue;
                    if (s == ACTAG_CODE_DOWNLOAD_DISABLED) return (int)LoaderInformation.DisallowCodeDownloadValue;
                    break;
                case 'D':
                    if (s == ACTAG_DEV_PATH)            return (int)LoaderInformation.DevPathValue;
                    if (s == ACTAG_APP_DYNAMIC_BASE)    return (int)LoaderInformation.DynamicBaseValue;
                    if (s == ACTAG_DISALLOW_APPLYPUBLISHERPOLICY) return (int)LoaderInformation.DisallowPublisherPolicyValue;
                    if (s == ACTAG_DISALLOW_APP_BINDING_REDIRECTS) return (int)LoaderInformation.DisallowBindingRedirectsValue;
                    if (s == ACTAG_DISALLOW_APP_BASE_PROBING) return (int)LoaderInformation.DisallowAppBaseProbingValue;
                   break;
                case 'F':
                    if (s == ACTAG_FORCE_CACHE_INSTALL) return (int)LoaderInformation.ShadowCopyFilesValue;
                    break;
                case 'L':
                    if (s == LICENSE_FILE)              return (int)LoaderInformation.LicenseFileValue;
                    break;
                case 'P':
                    if (s == ACTAG_APP_PRIVATE_BINPATH) return (int)LoaderInformation.PrivateBinPathValue;
                    break;
                case 'S':
                    if (s == ACTAG_APP_SHADOW_COPY_DIRS) return (int)LoaderInformation.ShadowCopyDirectoriesValue;
                    break;
            }
#else
            Contract.Assert('A' == ACTAG_APP_BASE_URL[0]        , "Assumption violated");
            if (s[0]=='A' && s == ACTAG_APP_BASE_URL)        
                return (int)LoaderInformation.ApplicationBaseValue;

#endif //FEATURE_FUSION

            return -1;
        }
#if FEATURE_FUSION
        private string BuildShadowCopyDirectories()
        {
            // Default to only to the ones that are in the private bin path.
            String binPath = Value[(int) LoaderInformation.PrivateBinPathValue];
            if(binPath == null)
                return null;

            StringBuilder result = StringBuilderCache.Acquire();
            String appBase = Value[(int) LoaderInformation.ApplicationBaseValue];
            if(appBase != null) {
                char[] sep = {';'};
                string[] directories = binPath.Split(sep);
                int size = directories.Length;
                bool appendSlash = !( (appBase[appBase.Length-1] == '/') ||
                                      (appBase[appBase.Length-1] == '\\') );

                if (size == 0) {
                    result.Append(appBase);
                    if (appendSlash)
                        result.Append('\\');
                    result.Append(binPath);
                }
                else {
                    for(int i = 0; i < size; i++) {
                        result.Append(appBase);
                        if (appendSlash)
                            result.Append('\\');
                        result.Append(directories[i]);
                        
                        if (i < size-1)
                            result.Append(';');
                    }
                }
            }
            
            return StringBuilderCache.GetStringAndRelease(result);
        }
#endif // FEATURE_FUSION		

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
