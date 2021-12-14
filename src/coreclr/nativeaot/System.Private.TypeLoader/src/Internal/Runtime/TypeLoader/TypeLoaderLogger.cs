// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.Runtime.TypeLoader
{
    internal static class TypeLoaderLogger
    {
        /// <summary>
        /// Variable used to pause the runtime when a given message appears. To use this feature
        /// attach a debugger to the process and set s_pauseHash to the hash code early in process
        /// execution
        /// </summary>
        internal static int s_pauseHash;

        [Conditional("TYPE_LOADER_TRACE")]
        public static void WriteLine(string message)
        {
            int hash = message.GetHashCode();

            if (s_pauseHash != 0)
            {
                if (s_pauseHash == message.GetHashCode())
                    Debugger.Break();
            }
            Debug.WriteLine("[" + hash.LowLevelToString() + "]  " + message);
        }
    }
}
