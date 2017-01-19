// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Configuration
{
    public sealed class ContextInformation
    {
        private readonly BaseConfigurationRecord _configRecord;
        private object _hostingContext;
        private bool _hostingContextEvaluated;

        internal ContextInformation(BaseConfigurationRecord configRecord)
        {
            Debug.Assert(configRecord != null, "configRecord != null");

            _hostingContextEvaluated = false;
            _hostingContext = null;
            _configRecord = configRecord;
        }

        public object HostingContext
        {
            get
            {
                if (!_hostingContextEvaluated)
                {
                    // Retrieve Context
                    _hostingContext = _configRecord.ConfigContext;

                    _hostingContextEvaluated = true;
                }

                return _hostingContext;
            }
        }

        // Is this the machine.config file or not?  If it is not
        // then use the Hosting Context to determine where you are
        // and in what hierarchy you are in
        public bool IsMachineLevel => _configRecord.IsMachineConfig;

        // Get a Section within the context of where we are.  What
        // ever section you retrieve here will be at the same level
        // in the hierarchy as we are.
        //
        // Note: Watch out for a situation where you request a section
        //       that will call you.
        public object GetSection(string sectionName)
        {
            return _configRecord.GetSection(sectionName);
        }
    }
}