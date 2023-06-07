// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.SourceGeneration
{
    public enum ClassType
    {
        /// <summary>
        /// Types that are not supported yet by source gen including types with constructor parameters.
        /// </summary>
        TypeUnsupportedBySourceGen = 0,
        Object = 1,
        BuiltInSupportType = 2,
        /// <summary>
        /// Known types such as System.Type and System.IntPtr that throw NotSupportedException.
        /// </summary>
        UnsupportedType = 3,
        TypeWithDesignTimeProvidedCustomConverter = 4,
        Enumerable = 5,
        Dictionary = 6,
        Nullable = 7,
        Enum = 8
    }
}
