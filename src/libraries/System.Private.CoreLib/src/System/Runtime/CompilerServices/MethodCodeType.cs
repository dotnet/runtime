// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public enum MethodCodeType
    {
        IL = MethodImplAttributes.IL,
        Native = MethodImplAttributes.Native,
        OPTIL = MethodImplAttributes.OPTIL,
        Runtime = MethodImplAttributes.Runtime
    }
}
