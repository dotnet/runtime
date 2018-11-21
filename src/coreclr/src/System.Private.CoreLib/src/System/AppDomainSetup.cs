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
        private string _appBase;

        // A collection of strings used to indicate which breaking changes shouldn't be applied
        // to an AppDomain. We only use the keys, the values are ignored.
        private Dictionary<string, object> _CompatFlags;

        internal AppDomainSetup(AppDomainSetup copy)
        {
            if (copy != null)
            {
                _appBase = copy._appBase;

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

            if (i != -1)
            {
                string appBase = imageLocation.Substring(0, i + 1);

                if (imageLocationAlreadyNormalized)
                    _appBase = appBase;
                else
                    ApplicationBase = appBase;
            }
        }

        public string ApplicationBase
        {
            get
            {
                return _appBase;
            }

            set
            {
                _appBase = (value == null || value.Length == 0)? null:Path.GetFullPath(value);
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
    }
}
