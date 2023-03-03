// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class PInvokeTargetNativeMethod : IPrefixMangledMethod
    {
        MethodDesc IPrefixMangledMethod.BaseMethod
        {
            get
            {
                return _declMethod;
            }
        }

        string IPrefixMangledMethod.Prefix
        {
            get
            {
                return "rawpinvoke";
            }
        }
    }
}
