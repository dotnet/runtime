// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    [Flags]
    internal enum ConfigurationValueFlags
    {
        Default = 0,
        Inherited = 1,
        Modified = 2,
        Locked = 4,
        XmlParentInherited = 8,
    }
}
