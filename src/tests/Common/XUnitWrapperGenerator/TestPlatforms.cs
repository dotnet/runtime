// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Xunit
{
    [Flags]
    public enum TestPlatforms
    {
        Windows = 1,
        Linux = 2,
        OSX = 4,
        FreeBSD = 8,
        NetBSD = 16,
        illumos= 32,
        Solaris = 64,
        iOS = 128,
        tvOS = 256,
        Android = 512,
        Browser = 1024,
        MacCatalyst = 2048,
        AnyUnix = FreeBSD | Linux | NetBSD | OSX | illumos | Solaris | iOS | tvOS | MacCatalyst | Android | Browser,
        Any = ~0
    }
}
