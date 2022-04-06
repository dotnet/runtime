// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class DelegateMarshallingMethodThunk : IPrefixMangledType
    {
        TypeDesc IPrefixMangledType.BaseType
        {
            get
            {
                return _delegateType;
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
