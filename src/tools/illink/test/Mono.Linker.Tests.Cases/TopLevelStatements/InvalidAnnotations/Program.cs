// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

[assembly: ExpectedNoWarnings]

Test ();

[UnexpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101211")]
[Kept]
static void Test () {
    RequireAll (GetUnsupportedType ());
}

[ExpectedWarning ("IL2098", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101215")]
[Kept]
static void RequireAll(
    [KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
    [DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] UnsupportedType f) {}

[Kept]
static UnsupportedType GetUnsupportedType () => new UnsupportedType ();

[Kept]
[KeptMember (".ctor()")]
class UnsupportedType {}
