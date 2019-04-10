// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System
{
#if PROJECTN
    [Internal.Runtime.CompilerServices.RelocatedType("System.Runtime.Extensions")]
#endif
    public sealed class AppDomainSetup
    {
        internal AppDomainSetup() { }
        public string? ApplicationBase => AppContext.BaseDirectory;
        public string? TargetFrameworkName => AppContext.TargetFrameworkName;
    }
}
