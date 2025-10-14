// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    class RequiresOnBaseClass
    {

        public static void Main()
        {
            DerivedFromBaseWithRUC.StaticMethod();
            DerivedFromBaseWithRDC.StaticMethod();

            new DerivedFromBaseWithRDC();
            new DerivedFromBaseWithRUC();
        }

        class DerivedFromBaseWithRUC : BaseWithRUC
        {
            [ExpectedWarning("IL2026")]
            public DerivedFromBaseWithRUC()
            {
            }

            public static void StaticMethod() { }
        }

        [RequiresUnreferencedCode(nameof(BaseWithRUC))]
        class BaseWithRUC { }

        class DerivedFromBaseWithRDC : BaseWithRDC
        {
            [ExpectedWarning("IL3050", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific Warning")]
            public DerivedFromBaseWithRDC()
            { }
            
            public static void StaticMethod() { }
        }

        [RequiresDynamicCode(nameof(BaseWithRDC))]
        class BaseWithRDC { }
    }
}
