// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// All TypeMap entries in this assembly are conditional (3-argument form).
// The trim target (AllConditionalTrimTarget) is never referenced by the test assembly,
// so ILLink drops the entry, which leaves this assembly with no surviving TypeMap entries.
// The test verifies that the TypeMapAssemblyTarget attribute pointing here is also removed.
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Reflection.Dependencies;

[assembly: TypeMap<AllConditionalGroupType>("ConditionalEntry", typeof(AllConditionalTarget), typeof(AllConditionalTrimTarget))]

namespace Mono.Linker.Tests.Cases.Reflection.Dependencies
{
    public class AllConditionalTarget;
    public class AllConditionalTrimTarget;
}
