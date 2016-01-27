// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace System.Diagnostics
{
    internal static class DiagnosticsConfiguration
    {
        // setting for TraceInternal.AutoFlush
        internal static bool AutoFlush
        {
            get
            {
                return false; // the default
            }
        }

        // setting for TraceInternal.UseGlobalLock
        internal static bool UseGlobalLock
        {
            get
            {
                return true; // the default
            }
        }

        // setting for TraceInternal.IndentSize
        internal static int IndentSize
        {
            get
            {
                return 4; // the default
            }
        }
    }
}

