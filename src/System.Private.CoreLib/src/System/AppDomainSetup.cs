// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Versioning;
using System.IO;

namespace System
{
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
        private string _AppBase; // for compat with v1.1
#pragma warning restore 169

        // A collection of strings used to indicate which breaking changes shouldn't be applied
        // to an AppDomain. We only use the keys, the values are ignored.
        private Dictionary<string, object> _CompatFlags;

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
                    _Entries = new string[(int)LoaderInformation.LoaderMaximum];
                return _Entries;
            }
        }

        public string ApplicationBase
        {
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

        public void SetCompatibilitySwitches(IEnumerable<string> switches)
        {
            if (switches != null)
            {
                _CompatFlags = new Dictionary<string, object>();
                foreach (string str in switches)
                {
                    _CompatFlags.Add(str, null);
                }
            }
            else
            {
                _CompatFlags = null;
            }
        }

        // The Target framework is not the framework that the process is actually running on.
        // It is the value read from the TargetFrameworkAttribute on the .exe that started the process.
        public string TargetFrameworkName => Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        public string ApplicationName
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
