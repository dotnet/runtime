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

        // Verify that assigning a deconstructed value to an annotated static field is validated.
        [ExpectedWarning("IL2074", nameof(GetUnannotatedType))]
        public static void Main()
        {
            object other;
            (_annotatedField, other) = (GetUnannotatedType(), new object());
        }
    }
}
