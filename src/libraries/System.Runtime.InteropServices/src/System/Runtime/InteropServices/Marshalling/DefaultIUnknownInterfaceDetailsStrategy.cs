// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    internal sealed class DefaultIUnknownInterfaceDetailsStrategy : IIUnknownInterfaceDetailsStrategy
    {
        public static readonly IIUnknownInterfaceDetailsStrategy Instance = new DefaultIUnknownInterfaceDetailsStrategy();

        public IComExposedDetails? GetComExposedTypeDetails(RuntimeTypeHandle type)
        {
            return IComExposedDetails.GetFromAttribute(type);
        }

        public IIUnknownDerivedDetails? GetIUnknownDerivedDetails(RuntimeTypeHandle type)
        {
            return IIUnknownDerivedDetails.GetFromAttribute(type);
        }
    }
}
