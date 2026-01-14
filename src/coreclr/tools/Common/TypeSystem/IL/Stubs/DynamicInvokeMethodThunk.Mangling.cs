// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class DynamicInvokeMethodThunk : IPrefixMangledSignature
    {
        MethodSignature IPrefixMangledSignature.BaseSignature
        {
            get
            {
                return _targetSignature;
            }
        }

        string IPrefixMangledSignature.Prefix
        {
            get
            {
                return "DynamicInvoke";
            }
        }
    }
}
