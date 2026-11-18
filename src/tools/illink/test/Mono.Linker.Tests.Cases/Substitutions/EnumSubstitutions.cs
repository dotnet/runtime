// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
    [ExpectedNoWarnings]
    [SetupLinkerSubstitutionFile("EnumSubstitutions.xml")]
    [IgnoreSubstitutions(false)]
    [SkipKeptItemsValidation(By = Tool.NativeAot)]
    public class EnumSubstitutions
    {
        [Kept]
        [KeptMember("value__")]
        [KeptBaseType(typeof(Enum))]
        private enum SubstitutionValue
        {
            [Kept]
            Initial,

            [Kept]
            ByName,

            [Kept]
            ByNumber,
        }

        [Kept]
        private static readonly SubstitutionValue FieldByName = SubstitutionValue.Initial;

        [Kept]
        private static readonly SubstitutionValue FieldByNumber = SubstitutionValue.Initial;

        public static void Main()
        {
            VerifyFieldByNameSubstitution();
            VerifyFieldByNumberSubstitution();
            MethodByName();
            MethodByNumber();
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "nop",
            "ldsfld Mono.Linker.Tests.Cases.Substitutions.EnumSubstitutions/SubstitutionValue Mono.Linker.Tests.Cases.Substitutions.EnumSubstitutions::FieldByName",
            "ldc.i4.1",
            "ceq",
            "ldc.i4.0",
            "ceq",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_10",
            "ret",
        })]
        private static void VerifyFieldByNameSubstitution()
        {
            if (FieldByName != SubstitutionValue.ByName)
                ReachableOnUnexpectedFieldByNameValue();
        }

        private static void ReachableOnUnexpectedFieldByNameValue()
        {
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "nop",
            "ldsfld Mono.Linker.Tests.Cases.Substitutions.EnumSubstitutions/SubstitutionValue Mono.Linker.Tests.Cases.Substitutions.EnumSubstitutions::FieldByNumber",
            "ldc.i4.2",
            "ceq",
            "ldc.i4.0",
            "ceq",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_10",
            "ret",
        })]
        private static void VerifyFieldByNumberSubstitution()
        {
            if (FieldByNumber != SubstitutionValue.ByNumber)
                ReachableOnUnexpectedFieldByNumberValue();
        }

        private static void ReachableOnUnexpectedFieldByNumberValue()
        {
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "ldc.i4 0x1",
            "ret",
        })]
        [ExpectedLocalsSequence(new string[0])]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SubstitutionValue MethodByName()
        {
            return SubstitutionValue.Initial;
        }

        [Kept]
        [ExpectedInstructionSequence(new[] {
            "ldc.i4 0x2",
            "ret",
        })]
        [ExpectedLocalsSequence(new string[0])]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SubstitutionValue MethodByNumber()
        {
            return SubstitutionValue.Initial;
        }
    }
}
