// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public class ExpectBodyModifiedAttribute : BaseInAssemblyAttribute
    {
    }
}
