// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    // Note: this test's goal is to validate that the product correctly reports unrecognized patterns
    //   - so the main validation is done by the ExpectedWarning attributes.
    [SkipKeptItemsValidation]
    [SetupCompileArgument("/unsafe")]
    [Define("IL_ASSEMBLY_AVAILABLE")]
    [SetupCompileBefore("library.dll", new[] { "Dependencies/ModifierDataFlow.il" })]
    [ExpectedNoWarnings]
    [LogContains("IL2074: Library.ModifierDataFlow.WriteModReqType().*'Library.ModifierDataFlow.modReqType'.*GetUnknownType()", regexMatch: true)]
    [LogContains("IL2074: Library.ModifierDataFlow.WriteMultipleModReqType().*'Library.ModifierDataFlow.multipleModReqType'.*GetUnknownType()", regexMatch: true)]
    [LogContains("IL2074: Library.ModifierDataFlow.WriteModOptType().*'Library.ModifierDataFlow.modOptType'.*GetUnknownType()", regexMatch: true)]
    [LogContains("IL2074: Library.ModifierDataFlow.WriteModReqModOptType().*'Library.ModifierDataFlow.modReqModOptType'.*GetUnknownType()", regexMatch: true)]
    [LogContains("IL2074: Library.ModifierDataFlow.WriteModOptModReqType().*'Library.ModifierDataFlow.modOptModReqType'.*GetUnknownType()", regexMatch: true)]
    [LogDoesNotContain("IL2074")]
    [LogContains("IL2097:.*Library.ModifierDataFlow.arrayModReqType", regexMatch: true)]
    [LogContains("IL2097:.*Library.ModifierDataFlow.modReqArrayType", regexMatch: true)]
    [LogContains("IL2097:.*Library.ModifierDataFlow.modReqArrayModReqType", regexMatch: true)]
    [LogDoesNotContain("IL2097")]
    public class ModifierDataFlow
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static volatile Type volatileType;

        [ExpectedWarning("IL2074", nameof(GetUnknownType), nameof(volatileType))]
        static void WriteVolatileType()
        {
            volatileType = GetUnknownType();
        }

        static Type GetUnknownType() => null;

        [ExpectedWarning("IL2097")]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static volatile Type[] volatileTypeArray;

        static void WriteVolatileTypeArray()
        {
            volatileTypeArray = new Type[] { GetUnknownType() };
        }

        public static void Main()
        {
            WriteVolatileType();
            WriteVolatileTypeArray();
#if IL_ASSEMBLY_AVAILABLE
            Library.ModifierDataFlow.WriteModReqType();
            Library.ModifierDataFlow.WriteMultipleModReqType();
            Library.ModifierDataFlow.WriteModOptType();
            Library.ModifierDataFlow.WriteModReqModOptType();
            Library.ModifierDataFlow.WriteModOptModReqType();
            Library.ModifierDataFlow.WriteModReqArrayType();
            Library.ModifierDataFlow.WriteArrayModReqType();
#endif
        }
    }
}
