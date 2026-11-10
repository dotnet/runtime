// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    [SkipKeptItemsValidation]
    class DeconstructUserDefinedConversion
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type _annotatedField;

        struct ConversionSource
        {
            public static implicit operator Type(ConversionSource value) => null;
        }

        // The converted value is the conversion operator's return value, whose type is unannotated.
        [ExpectedWarning("IL2074", nameof(ConversionSource))]
        static void Test((ConversionSource value, object instance) input)
        {
            object instance;
            (_annotatedField, instance) = input;
        }

        public static void Main() => Test(default);
    }
}
