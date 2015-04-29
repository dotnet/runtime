// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit.Sdk;

namespace Xunit
{
    [Flags]
    public enum PlatformID
    {
        Windows = 1,
        Linux = 2,
        OSX = 4,
        AnyUnix = Linux | OSX,
        Any = Windows | Linux | OSX
    }
}
