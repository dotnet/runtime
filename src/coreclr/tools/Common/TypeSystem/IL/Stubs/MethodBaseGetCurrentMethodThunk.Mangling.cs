// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    internal partial class MethodBaseGetCurrentMethodThunk : IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod
        {
            get
            {
                return Method;
            }
        }

        string IPrefixMangledMethod.Prefix
        {
            get
            {
                return "GetCurrentMethod";
            }
        }
    }
}
