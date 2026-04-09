// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public enum ImportDefinitionKind
    {
        ImportNamespace = 1,
        ImportAssemblyNamespace = 2,
        ImportType = 3,
        ImportXmlNamespace = 4,
        ImportAssemblyReferenceAlias = 5,
        AliasAssemblyReference = 6,
        AliasNamespace = 7,
        AliasAssemblyNamespace = 8,
        AliasType = 9
    }
}
