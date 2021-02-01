// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices
{
    [SupportedOSPlatform("windows")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public sealed partial class IDispatchConstantAttribute : CustomConstantAttribute
    {
        public IDispatchConstantAttribute() { }

        public override object Value => new DispatchWrapper(null);
    }
}
