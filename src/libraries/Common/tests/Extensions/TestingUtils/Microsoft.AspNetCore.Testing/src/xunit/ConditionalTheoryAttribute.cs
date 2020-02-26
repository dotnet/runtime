// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [XunitTestCaseDiscoverer("Microsoft.AspNetCore.Testing." + nameof(ConditionalTheoryDiscoverer), "Microsoft.AspNetCore.Testing")]
    public class ConditionalTheoryAttribute : TheoryAttribute
    {
    }
}
