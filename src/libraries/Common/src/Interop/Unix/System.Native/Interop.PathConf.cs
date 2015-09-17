// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static int DEFAULT_PC_NAME_MAX = 255;

        internal enum PathConfName : int
        {
            PC_LINK_MAX         = 1,
            PC_MAX_CANON        = 2,
            PC_MAX_INPUT        = 3,
            PC_NAME_MAX         = 4,
            PC_PATH_MAX         = 5,
            PC_PIPE_BUF         = 6,
            PC_CHOWN_RESTRICTED = 7,
            PC_NO_TRUNC         = 8,
            PC_VDISABLE         = 9,
        }

        /// <summary>The maximum path length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int s_maxPath = -1;

        /// <summary>The maximum name length for the system.  -1 if it hasn't yet been initialized.</summary>
        private static int s_maxName = -1;

        internal static int MaxPath
        {
            get
            {
                // Benign race condition on cached value
                if (s_maxPath < 0) 
                {
                    // GetMaximumPath returns a long from PathConf
                    // but our callers expect an int so we need to convert.
                    long temp = GetMaximumPath();
                    if (temp > int.MaxValue)
                        s_maxPath = int.MaxValue;
                    else
                        s_maxPath = Convert.ToInt32(temp);
                }
                return s_maxPath; 
            }
        }

        internal static int MaxName
        {
            get
            {
                // Benign race condition on cached value
                if (s_maxName < 0)
                {
                    int result = PathConf("/", PathConfName.PC_NAME_MAX);
                    s_maxName = result >= 0 ? result : DEFAULT_PC_NAME_MAX;
                }
                
                return s_maxName;
            }
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        private static extern int PathConf(string path, PathConfName name);

        [DllImport(Libraries.SystemNative)]
        private static extern long GetMaximumPath();
    }
}
