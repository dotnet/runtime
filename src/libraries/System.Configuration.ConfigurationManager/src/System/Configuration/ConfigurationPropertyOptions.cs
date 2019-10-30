// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    [Flags]
    public enum ConfigurationPropertyOptions
    {
        None = 0,
        IsDefaultCollection = 0x00000001,
        IsRequired = 0x00000002,
        IsKey = 0x00000004,
        IsTypeStringTransformationRequired = 0x00000008,
        IsAssemblyStringTransformationRequired = 0x00000010,
        IsVersionCheckRequired = 0x00000020,
    }
}