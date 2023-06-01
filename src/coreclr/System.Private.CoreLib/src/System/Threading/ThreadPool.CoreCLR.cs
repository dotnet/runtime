// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{

    public static partial class ThreadPool
    {
        internal static bool EnsureConfigInitialized()
        {
            return s_initialized;
        }

        private static readonly bool s_initialized = InitializeConfig();

        private static unsafe bool InitializeConfig()
        {
            int configVariableIndex = 1;
            while (true)
            {
                int nextConfigVariableIndex =
                    GetNextConfigUInt32Value(
                        configVariableIndex,
                        out uint configValue,
                        out bool isBoolean,
                        out char* appContextConfigNameUnsafe);
                if (nextConfigVariableIndex < 0)
                {
                    break;
                }

                Debug.Assert(nextConfigVariableIndex > configVariableIndex);
                configVariableIndex = nextConfigVariableIndex;

                Debug.Assert(appContextConfigNameUnsafe != null);

                var appContextConfigName = new string(appContextConfigNameUnsafe);
                if (isBoolean)
                {
                    AppContext.SetSwitch(appContextConfigName, configValue != 0);
                }
                else
                {
                    AppContext.SetData(appContextConfigName, configValue);
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe int GetNextConfigUInt32Value(
            int configVariableIndex,
            out uint configValue,
            out bool isBoolean,
            out char* appContextConfigName);

    }
}
