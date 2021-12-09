// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class StructMarshallingThunk : IPrefixMangledType
    {
        TypeDesc IPrefixMangledType.BaseType
        {
            get
            {
                return ManagedType;
            }
        }

        string IPrefixMangledType.Prefix
        {
            get
            {
                return NamePrefix;
            }
        }
    }
}
