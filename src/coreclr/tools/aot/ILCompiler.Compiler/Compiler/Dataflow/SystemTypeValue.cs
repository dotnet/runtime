// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
    internal sealed partial record SystemTypeValue : SingleValue
    {
        public SystemTypeValue(in TypeProxy representedType)
        {
            RepresentedType = representedType;
        }
    }
}
