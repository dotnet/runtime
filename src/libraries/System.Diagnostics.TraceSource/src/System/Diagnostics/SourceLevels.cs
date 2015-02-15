// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        All = unchecked((int)0xFFFFFFFF),
    }
}

