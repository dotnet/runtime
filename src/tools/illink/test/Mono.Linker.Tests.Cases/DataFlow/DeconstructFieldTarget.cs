// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    [SkipKeptItemsValidation]
    class DeconstructFieldTarget
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type _annotatedField;

        static Type GetUnannotatedType() => null;

        // Verify that tools which model tuple fields validate assignment to an annotated static field.
        [ExpectedWarning("IL2074", nameof(GetUnannotatedType), Tool.Trimmer | Tool.NativeAot, "Analyzer cannot determine what compiles to ValueTuple or local variables.")]
        public static void Main()
        {
            object other;
            (_annotatedField, other) = (GetUnannotatedType(), new object());
        }
    }
}
