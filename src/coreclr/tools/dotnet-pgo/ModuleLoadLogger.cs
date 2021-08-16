// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class ModuleLoadLogger
    {
        HashSet<string> _simpleNamesReported = new HashSet<string>();

        public ModuleLoadLogger(Logger logger)
        {
            _logger = logger;
        }

        Logger _logger;

        public void LogModuleLoadFailure(string simpleName, string filePath)
        {
            if (_simpleNamesReported.Add(simpleName))
            {
                string str = $"Failed to load assembly '{simpleName}' from '{filePath}'";

                if (String.Compare("System.Private.CoreLib", simpleName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _logger.PrintError(str);
                }
                else
                {
                    _logger.PrintWarning(str);
                }
            }
        }

        public void LogModuleLoadFailure(string simpleName)
        {
            if (_simpleNamesReported.Add(simpleName))
            {
                string str = $"Failed to load assembly '{simpleName}'";

                if (String.Compare("System.Private.CoreLib", simpleName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    _logger.PrintError(str);
                }
                else
                {
                    _logger.PrintWarning(str);
                }
            }
        }

        public void LogModuleLoadSuccess(string simpleName, string filePath)
        {
            _logger.PrintDetailedMessage($"Loaded '{simpleName}' from '{filePath}'");
        }
    }
}
