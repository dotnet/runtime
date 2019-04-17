// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited = false)]
    public sealed partial class IDispatchConstantAttribute : CustomConstantAttribute
    {
        public IDispatchConstantAttribute() { }

#pragma warning disable CS8608 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268
        public override object Value => new DispatchWrapper(null);
#pragma warning restore CS8608
    }
}
