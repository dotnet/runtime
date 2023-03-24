// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    internal sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
    {
        public static readonly IIUnknownInterfaceDetailsStrategy Instance = new DefaultIUnknownInterfaceDetailsStrategy();

        public IUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
        {
            return IUnknownDerivedDetails.GetFromAttribute(type);
        }
    }
}
