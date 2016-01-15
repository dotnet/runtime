// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// -----------------------------------------------------------------------

// -----------------------------------------------------------------------

namespace System.Reflection
{
    [Flags]
    public enum BindingFlags
    {
        // These flags indicate what to search for when binding
        IgnoreCase = 0x01,        // Ignore the case of Names while searching
        DeclaredOnly = 0x02,        // Only look at the members declared on the Type
        Instance = 0x04,        // Include Instance members in search
        Static = 0x08,        // Include Static members in search
        Public = 0x10,        // Include Public members in search
        NonPublic = 0x20,        // Include Non-Public members in search
        FlattenHierarchy = 0x40,        // Rollup the statics into the class.

        ExactBinding = 0x010000,        // Bind with Exact Type matching, No Change type
        OptionalParamBinding = 0x040000,
    }
}