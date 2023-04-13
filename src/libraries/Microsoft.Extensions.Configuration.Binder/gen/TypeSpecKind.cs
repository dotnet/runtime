// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal enum TypeSpecKind
    {
        Unknown = 0,
        ParsableFromString = 1,
        Object = 2,
        Array = 3,
        Enumerable = 4,
        Dictionary = 5,
        IConfigurationSection = 6,
        Nullable = 7,
    }

    internal enum StringParsableTypeKind
    {
        None = 0,
        ConfigValue = 1,
        Enum = 2,
        ByteArray = 3,
        Integer = 4,
        Float = 5,
        Parse = 6,
        ParseInvariant = 7,
        CultureInfo = 8,
        Uri = 9,
    }
}
