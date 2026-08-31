// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Configuration
{
    public enum SettingsSerializeAs
    {
        String = 0,
        Xml = 1,
        [Obsolete(Obsoletions.BinaryFormatterMessage + @". Consider using Xml instead.")]
        Binary = 2,
        ProviderSpecific = 3
    }
}
