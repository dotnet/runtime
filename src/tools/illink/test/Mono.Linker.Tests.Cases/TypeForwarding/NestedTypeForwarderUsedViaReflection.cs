// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding;

[SetupCompileBefore("Forwarder.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
[SetupCompileBefore("Implementation.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
[SetupCompileBefore("FallbackForwarder.dll", new[] { "Dependencies/NestedTypeForwarderFallbackReference.il" }, addAsReference: false)]
[SetupCompileAfter("Forwarder.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, references: new[] { "Implementation.dll" }, defines: new[] { "INCLUDE_FORWARDERS" })]
[SetupCompileAfter("FallbackImplementation.dll", new[] { "Dependencies/NestedTypeForwarderFallbackImplementation.il" })]
[SetupCompileAfter("FallbackForwarder.dll", new[] { "Dependencies/NestedTypeForwarderFallback.il" }, references: new[] { "FallbackImplementation.dll" })]
[KeptAssembly("Forwarder.dll")]
[KeptAssembly("FallbackForwarder.dll")]
[RemovedAssembly("FallbackImplementation.dll")]
[KeptTypeInAssembly("FallbackForwarder.dll", "Fallback.Generic`1")]
[RemovedAssemblyReference("FallbackForwarder.dll", "FallbackImplementation")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary`1")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary`1/Nested")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo/NestedThree")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary`1")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary`1/Nested")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo")]
[KeptTypeInAssembly("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo/NestedThree")]
public class NestedTypeForwarderUsedViaReflection
{
    public static void Main()
    {
        Type.GetType("Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedGenericNestedTypeLibrary`1+Nested, Forwarder");
        Type.GetType("Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary+NestedOne+NestedTwo+NestedThree, Forwarder");
        Type.GetType("Fallback.Generic`1[[System.Environment+SpecialFolder]], FallbackForwarder");
    }
}
