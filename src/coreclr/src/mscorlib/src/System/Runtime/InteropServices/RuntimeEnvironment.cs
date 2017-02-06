// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Runtime information
**          
**
=============================================================================*/

using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.Versioning;
using StackCrawlMark = System.Threading.StackCrawlMark;

namespace System.Runtime.InteropServices
{
    [System.Runtime.InteropServices.ComVisible(true)]
    static public class RuntimeEnvironment {

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern String GetModuleFileName();

        [MethodImpl (MethodImplOptions.NoInlining)]
        public static String GetSystemVersion()
        {
            return Assembly.GetExecutingAssembly().ImageRuntimeVersion;
        }

#if FEATURE_COMINTEROP
#endif // FEATURE_COMINTEROP
    }
}
