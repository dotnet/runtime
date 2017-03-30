// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;

namespace System.Diagnostics
{
    [Flags]
    public enum SourceLevels
    {
        Off = 0,
        Critical = 0x01,
        Error = 0x03,
        Warning = 0x07,
        Information = 0x0F,
        Verbose = 0x1F,

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        ActivityTracing = 0xFF00,
        All = unchecked((int)0xFFFFFFFF),
    }
}

