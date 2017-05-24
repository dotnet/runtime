// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
** Purpose: Defines the settings that the loader uses to find assemblies in an
**          AppDomain
**
**
=============================================================================*/
namespace System
{
    using System.Text;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using Path = System.IO.Path;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Collections.Generic;

    internal sealed class AppDomainSetup
    {
        internal enum LoaderInformation
        {
            // If you add a new value, add the corresponding property
            // to AppDomain.GetData() and SetData()'s switch statements,
            // as well as fusionsetup.h.
            ApplicationBaseValue = 0,  // LOADER_APPLICATION_BASE
            ConfigurationFileValue = 1,  // LOADER_CONFIGURATION_BASE
            DynamicBaseValue = 2,  // LOADER_DYNAMIC_BASE
            DevPathValue = 3,  // LOADER_DEVPATH
            ApplicationNameValue = 4,  // LOADER_APPLICATION_NAME
            PrivateBinPathValue = 5,  // LOADER_PRIVATE_PATH
            PrivateBinPathProbeValue = 6,  // LOADER_PRIVATE_BIN_PATH_PROBE
            ShadowCopyDirectoriesValue = 7,  // LOADER_SHADOW_COPY_DIRECTORIES
            ShadowCopyFilesValue = 8,  // LOADER_SHADOW_COPY_FILES
            CachePathValue = 9,  // LOADER_CACHE_PATH
            LicenseFileValue = 10, // LOADER_LICENSE_FILE
            DisallowPublisherPolicyValue = 11, // LOADER_DISALLOW_PUBLISHER_POLICY
            DisallowCodeDownloadValue = 12, // LOADER_DISALLOW_CODE_DOWNLOAD
            DisallowBindingRedirectsValue = 13, // LOADER_DISALLOW_BINDING_REDIRECTS
            DisallowAppBaseProbingValue = 14, // LOADER_DISALLOW_APPBASE_PROBING
            ConfigurationBytesValue = 15, // LOADER_CONFIGURATION_BYTES
            LoaderMaximum = 18  // LOADER_MAXIMUM
        }

        // Constants from fusionsetup.h.
        private const string LOADER_OPTIMIZATION = "LOADER_OPTIMIZATION";

        private const string ACTAG_APP_BASE_URL = "APPBASE";

        // This class has an unmanaged representation so be aware you will need to make edits in vm\object.h if you change the order
        // of these fields or add new ones.

        private string[] _Entries;
#pragma warning disable 169
        private String _AppBase; // for compat with v1.1
#pragma warning restore 169

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
            if (copy != null)
            {
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

                if (copy._CompatFlags != null)
                {
                    SetCompatibilitySwitches(copy._CompatFlags.Keys);
                }

                _TargetFrameworkName = copy._TargetFrameworkName;

#if FEATURE_RANDOMIZED_STRING_HASHING
                _UseRandomizedStringHashing = copy._UseRandomizedStringHashing;
#endif

            }
        }

        public AppDomainSetup()
        {
        }

        internal void SetupDefaults(string imageLocation, bool imageLocationAlreadyNormalized = false)
        {
            char[] sep = { '\\', '/' };
            int i = imageLocation.LastIndexOfAny(sep);

            if (i == -1)
            {
                ApplicationName = imageLocation;
            }
            else
            {
                ApplicationName = imageLocation.Substring(i + 1);
                string appBase = imageLocation.Substring(0, i + 1);

                if (imageLocationAlreadyNormalized)
                    Value[(int)LoaderInformation.ApplicationBaseValue] = appBase;
                else
                    ApplicationBase = appBase;
            }
        }

        internal string[] Value
        {
            get
            {
                if (_Entries == null)
                    _Entries = new String[(int)LoaderInformation.LoaderMaximum];
                return _Entries;
            }
        }

        public String ApplicationBase
        {
            [Pure]
            get
            {
                return Value[(int)LoaderInformation.ApplicationBaseValue];
            }

            set
            {
                Value[(int)LoaderInformation.ApplicationBaseValue] = (value == null || value.Length == 0)?null:Path.GetFullPath(value);
            }
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
                    if (StringComparer.OrdinalIgnoreCase.Equals("UseRandomizedStringHashAlgorithm", str))
                    {
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
        public String TargetFrameworkName
        {
            get
            {
                return _TargetFrameworkName;
            }
            set
            {
                _TargetFrameworkName = value;
            }
        }

        public String ApplicationName
        {
            get
            {
                return Value[(int)LoaderInformation.ApplicationNameValue];
            }

            set
            {
                Value[(int)LoaderInformation.ApplicationNameValue] = value;
            }
        }
    }
}
