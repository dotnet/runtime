// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Xunit
{
    [Flags]
    public enum TestPlatforms
    {
        Windows = 1 << 0,
        Linux = 1 << 1,
        OSX = 1 << 2,
        FreeBSD = 1 << 3,
        NetBSD = 1 << 4,
        illumos = 1 << 5,
        Solaris = 1 << 6,
        iOS = 1 << 7,
        tvOS = 1 << 8,
        Android = 1 << 9,
        Browser = 1 << 10,
        MacCatalyst = 1 << 11,
        LinuxBionic = 1 << 12,
        Wasi = 1 << 13,
        Haiku = 1 << 14,
        OpenBSD = 1 << 15,

        AnyApple = OSX | iOS | tvOS | MacCatalyst,
        AnyUnix = AnyApple | Linux | FreeBSD | NetBSD | OpenBSD | illumos | Solaris | Android | Browser | LinuxBionic | Wasi | Haiku,
        Any = ~0
    }
}
