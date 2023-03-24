// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem.Interop
{
    public partial class PInvokeDelegateWrapper : IPrefixMangledType
    {
        TypeDesc IPrefixMangledType.BaseType
        {
            get
            {
                return DelegateType;
            }
        }

        string IPrefixMangledType.Prefix
        {
            get
            {
                return "PInvokeDelegateWrapper";
            }
        }
    }
}
