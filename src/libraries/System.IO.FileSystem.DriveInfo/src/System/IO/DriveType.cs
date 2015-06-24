// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    // Matches Win32's DRIVE_XXX #defines from winbase.h
    public enum DriveType
    {
        Unknown = 0,
        NoRootDirectory = 1,
        Removable = 2,
        Fixed = 3,
        Network = 4,
        CDRom = 5,
        Ram = 6
    }
}
