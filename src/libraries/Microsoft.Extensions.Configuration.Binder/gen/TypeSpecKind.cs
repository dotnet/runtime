// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal enum TypeSpecKind
    {
        StringBasedParse = 0,
        Enum = 1,
        Object = 2,
        Array = 3,
        Enumerable = 4,
        Dictionary = 5,
        IConfigurationSection = 6,
        System_Object = 7,
        ByteArray = 8,
        Nullable = 9,
    }
}
