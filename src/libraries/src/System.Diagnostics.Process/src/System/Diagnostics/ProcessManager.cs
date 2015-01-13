// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        public static bool IsProcessRunning(int processId)
        {
            return IsProcessRunning(processId, GetProcessIds());
        }

        public static bool IsProcessRunning(int processId, string machineName)
        {
            return IsProcessRunning(processId, GetProcessIds(machineName));
        }

        private static bool IsProcessRunning(int processId, int[] processIds)
        {
            return Array.IndexOf(processIds, processId) >= 0;
        }
    }
}