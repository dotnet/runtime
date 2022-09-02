// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    public partial class NativeStructType : IPrefixMangledType
    {
        TypeDesc IPrefixMangledType.BaseType => ManagedStructType;

        string IPrefixMangledType.Prefix => "NativeStructType";
    }
}
