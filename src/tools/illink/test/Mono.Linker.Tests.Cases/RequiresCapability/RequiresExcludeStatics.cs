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
    class RequiresExcludeStatics
    {
        public static void Main()
        {
            ClassWithRequires.Test();
            DerivedWithRequiresExcludeStatics.Test();
            DerivedWithoutRequires.Test();
            TestDerivedWithRequires();
            TestAttributeWithRequires();
            GenericWithRequires<int>.Test();
        }

        [RequiresUnreferencedCode("--ClassWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("--ClassWithRequires--", ExcludeStatics = true)]
        class ClassWithRequires
        {
            [ExpectedWarning("IL2026", "--Requires--")]
            [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            static ClassWithRequires()
            {
                Requires();
            }

            [ExpectedWarning("IL2026", "--Requires--")]
            [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            static void StaticMethod() => Requires();

            [RequiresUnreferencedCode("--AnnotatedStaticMethod--")]
            [RequiresDynamicCode("--AnnotatedStaticMethod--")]
            static void AnnotatedStaticMethod() => Requires();

            [RequiresUnreferencedCode("--AnnotatedStaticMethodExcludeStatics--", ExcludeStatics = true)]
            [RequiresDynamicCode("--AnnotatedStaticMethodExcludeStatics--", ExcludeStatics = true)]
            static void AnnotatedStaticMethodExcludeStatics() => Requires();

            void InstanceMethod() => Requires();

            static int StaticField;

            int InstanceField;

            static bool StaticProperty
            {
                [ExpectedWarning("IL2026", "--Requires--")]
                [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
                get
                {
                    Requires();
                    return true;
                }
                [ExpectedWarning("IL2026", "--Requires--")]
                [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
                set
                {
                    Requires();
                }
            }

            bool InstanceProperty
            {
                get
                {
                    Requires();
                    return true;
                }
                set
                {
                    Requires();
                }
            }

            class Nested
            {
                [ExpectedWarning("IL2026", "--Requires--")]
                [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
                public static void StaticMethod() => Requires();

                [ExpectedWarning("IL2026", "--Requires--")]
                [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
                public void InstanceMethod() => Requires();
            }

            [ExpectedWarning("IL2026", "ClassWithRequires.ClassWithRequires()")]
            [ExpectedWarning("IL3050", "ClassWithRequires.ClassWithRequires()", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            [ExpectedWarning("IL2026", "--AnnotatedStaticMethod--")]
            [ExpectedWarning("IL3050", "--AnnotatedStaticMethod--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            [ExpectedWarning("IL2026", "--AnnotatedStaticMethodExcludeStatics--")]
            [ExpectedWarning("IL3050", "--AnnotatedStaticMethodExcludeStatics--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            public static void Test()
            {
                StaticMethod();
                StaticField = 42;
                _ = StaticProperty;
                StaticProperty = true;

                AnnotatedStaticMethod();
                AnnotatedStaticMethodExcludeStatics();

                var instance = new ClassWithRequires();
                instance.InstanceMethod();
                instance.InstanceField = 42;
                _ = instance.InstanceProperty;
                instance.InstanceProperty = true;

                Nested.StaticMethod();
                var nestedInstance = new Nested();
                nestedInstance.InstanceMethod();
            }
        }

        [RequiresUnreferencedCode("--BaseWithRequires--")]
        [RequiresDynamicCode("--BaseWithRequires--")]
        class BaseWithRequires
        {
            protected static void StaticMethod() => Requires();
        }

        [RequiresUnreferencedCode("--DerivedWithRequiresExcludeStatics--", ExcludeStatics = true)]
        [RequiresDynamicCode("--DerivedWithRequiresExcludeStatics--", ExcludeStatics = true)]
        class DerivedWithRequiresExcludeStatics : BaseWithRequires
        {
            [ExpectedWarning("IL2026", "--Requires--")]
            [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            static void DerivedStaticMethod() => Requires();

            [ExpectedWarning("IL2026", "StaticMethod", "--BaseWithRequires--")]
            [ExpectedWarning("IL3050", "StaticMethod", "--BaseWithRequires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            public static void Test()
            {
                StaticMethod();
                DerivedStaticMethod();
            }
        }

        class DerivedWithoutRequires : BaseWithRequires
        {
            [ExpectedWarning("IL2026", "--Requires--")]
            [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            static void DerivedStaticMethod() => Requires();

            [ExpectedWarning("IL2026", "StaticMethod", "--BaseWithRequires--")]
            [ExpectedWarning("IL3050", "StaticMethod", "--BaseWithRequires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            public static void Test()
            {
                StaticMethod();
                DerivedStaticMethod();
            }
        }

        [RequiresUnreferencedCode("--BaseWithRequiresExcludeStatics--", ExcludeStatics = true)]
        [RequiresDynamicCode("--BaseWithRequiresExcludeStatics--", ExcludeStatics = true)]
        class BaseWithRequiresExcludeStatics
        {
            [ExpectedWarning("IL2026", "--Requires--")]
            [ExpectedWarning("IL3050", "--Requires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            public static void StaticMethod() => Requires();
        }

        [RequiresUnreferencedCode("--DerivedWithRequiresExcludeStatics--")]
        [RequiresDynamicCode("--DerivedWithRequiresExcludeStatics--")]
        class DerivedWithRequires : BaseWithRequiresExcludeStatics
        {
            public static void DerivedStaticMethod() => Requires();
        }

        [ExpectedWarning("IL2026", "DerivedWithRequires")]
        [ExpectedWarning("IL3050", "DerivedWithRequires", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
        static void TestDerivedWithRequires()
        {
            DerivedWithRequires.StaticMethod();
            DerivedWithRequires.DerivedStaticMethod();
        }

        [RequiresUnreferencedCode("--AttributeWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("--AttributeWithRequires--", ExcludeStatics = true)]
        class AttributeWithRequiresAttribute : Attribute
        {
        }

        [ExpectedWarning("IL2026", "--AttributeWithRequires--", Tool.Analyzer | Tool.Trimmer, "https://github.com/dotnet/runtime/issues/117899")]
        [ExpectedWarning("IL3050", "--AttributeWithRequires--", Tool.Analyzer, "NativeAOT Specific warning, https://github.com/dotnet/runtime/issues/117899")]
        [AttributeWithRequires]
        static void TestAttributeWithRequires()
        {
        }

        [RequiresUnreferencedCode("--GenericWithRequires--", ExcludeStatics = true)]
        [RequiresDynamicCode("--GenericWithRequires--", ExcludeStatics = true)]
        class GenericWithRequires<T>
        {
            class Requires<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>
            {
            }

            [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
            static Requires<T> StaticField;

            [UnexpectedWarning("IL2091", Tool.Trimmer, "https://github.com/dotnet/runtime/issues/113249")]
            Requires<T> InstanceField;

            [ExpectedWarning("IL2091", "PublicFields", "Requires<T>")]
            [ExpectedWarning("IL2091", "PublicFields", "Requires<T>")]
            [ExpectedWarning("IL2026", "--GenericWithRequires--")]
            [ExpectedWarning("IL3050", "--GenericWithRequires--", Tool.Analyzer | Tool.NativeAot, "NativeAOT Specific warning")]
            public static void Test()
            {
                StaticField = new Requires<T>();
                var instance = new GenericWithRequires<T>();
                instance.InstanceField = new Requires<T>();
            }
        }

        [RequiresUnreferencedCode("--Requires--")]
        [RequiresDynamicCode("--Requires--")]
        static void Requires()
        {
        }
    }
}
