// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.TestFramework;

[assembly: VerifyKeptAttributeAttributeWorks.NoArguments]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.NoArgumentsAttribute))]

[assembly: VerifyKeptAttributeAttributeWorks.NoArgumentsWithDuplicates]
[assembly: VerifyKeptAttributeAttributeWorks.NoArgumentsWithDuplicates]
// Roslyn will dedupe so only need one assert
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.NoArgumentsWithDuplicatesAttribute))]

[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsLooseAssertAttribute))]

[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(VerifyKeptAttributeAttributeWorks.Foo))]
// Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(VerifyKeptAttributeAttributeWorks.Foo))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]

[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssert<int>("inst1-arg1", 1, "inst1-arg3", typeof(int))]
[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssert<string>("inst2-arg1", 2, "inst2-arg3", typeof(string))]
[assembly: VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssert<VerifyKeptAttributeAttributeWorks.Foo>("inst3-arg1", 3, "inst3-arg3", typeof(VerifyKeptAttributeAttributeWorks.Foo))]
// Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<int>), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<VerifyKeptAttributeAttributeWorks.Foo>), "inst3-arg1", 3, "inst3-arg3", typeof(VerifyKeptAttributeAttributeWorks.Foo))]
[assembly: KeptAttributeAttribute(typeof(VerifyKeptAttributeAttributeWorks.WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<string>), "inst2-arg1", 2, "inst2-arg3", typeof(string))]

namespace Mono.Linker.Tests.Cases.TestFramework;

public class VerifyKeptAttributeAttributeWorks
{
    public static void Main()
    {
        var f = new Foo();
        f.Method();
        f.Field = 1;
        f.Property = 1;
        f.Event += delegate(object sender, EventArgs e)
        {
        };
    }

    [Kept]
    [KeptMember(".ctor()")]

    [NoArguments]
    [KeptAttributeAttribute(typeof(NoArgumentsAttribute))]

    [NoArgumentsWithDuplicates]
    [NoArgumentsWithDuplicates]
    [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]
    [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]

    [WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
    [KeptAttributeAttribute(typeof(WithArgumentsLooseAssertAttribute))]

    [WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
    [KeptAttributeAttribute(typeof(WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

    [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
    [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
    [WithArgumentsWithDuplicatesLooseAssert("arg1", 2, "arg3", typeof(int))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]

    [WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
    [WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
    [WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
    [WithArgumentsWithDuplicatesExplicitAssert(null, 4, null, null)]
    // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), null, 4, null, null)]

    [WithArgumentsWithGenericWithDuplicatesExplicitAssert<int>("inst1-arg1", 1, "inst1-arg3", typeof(int))]
    [WithArgumentsWithGenericWithDuplicatesExplicitAssert<string>("inst2-arg1", 2, "inst2-arg3", typeof(string))]
    [WithArgumentsWithGenericWithDuplicatesExplicitAssert<Foo>("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
    // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
    [KeptAttributeAttribute(typeof(WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<int>), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<Foo>), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
    [KeptAttributeAttribute(typeof(WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<string>), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
    public class Foo
    {
        [Kept]

        [NoArguments]
        [KeptAttributeAttribute(typeof(NoArgumentsAttribute))]

        [NoArgumentsWithDuplicates]
        [NoArgumentsWithDuplicates]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]

        [WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsLooseAssertAttribute))]

        [WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 2, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]

        [WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
        public int Field;

        [Kept]

        [NoArguments]
        [KeptAttributeAttribute(typeof(NoArgumentsAttribute))]

        [NoArgumentsWithDuplicates]
        [NoArgumentsWithDuplicates]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]

        [WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsLooseAssertAttribute))]

        [WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 2, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]

        [WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
        public void Method()
        {
        }

        [Kept]

        [NoArguments]
        [KeptAttributeAttribute(typeof(NoArgumentsAttribute))]

        [NoArgumentsWithDuplicates]
        [NoArgumentsWithDuplicates]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]

        [WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsLooseAssertAttribute))]

        [WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 2, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]

        [WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
        public int Property;

        [Kept]
        [KeptBackingField]
        [KeptEventAddMethod]
        [KeptEventRemoveMethod]

        [NoArguments]
        [KeptAttributeAttribute(typeof(NoArgumentsAttribute))]

        [NoArgumentsWithDuplicates]
        [NoArgumentsWithDuplicates]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]
        [KeptAttributeAttribute(typeof(NoArgumentsWithDuplicatesAttribute))]

        [WithArgumentsLooseAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsLooseAssertAttribute))]

        [WithArgumentsExplicitAssert("arg1", 1, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsExplicitAssertAttribute), "arg1", 1, "arg3", typeof(int))]

        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 1, "arg3", typeof(int))]
        [WithArgumentsWithDuplicatesLooseAssert("arg1", 2, "arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesLooseAssertAttribute))]

        [WithArgumentsWithDuplicatesExplicitAssert("inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst2-arg1", 2, "inst2-arg3", typeof(string))]
        [WithArgumentsWithDuplicatesExplicitAssert("inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        // Intentionally make kept attribute ordering different than the usages to verify we don't require ordering of kept attributes to match the usages
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst1-arg1", 1, "inst1-arg3", typeof(int))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst3-arg1", 3, "inst3-arg3", typeof(Foo))]
        [KeptAttributeAttribute(typeof(WithArgumentsWithDuplicatesExplicitAssertAttribute), "inst2-arg1", 2, "inst2-arg3", typeof(string))]
        public event EventHandler Event;
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All)]
    public class NoArgumentsAttribute : Attribute
    {
    }

    [Kept]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All)]
    public class WithArgumentsLooseAssertAttribute : Attribute
    {
        [Kept]
        public WithArgumentsLooseAssertAttribute(object arg1, int arg2, string arg3, Type arg4)
        {
        }
    }

    [Kept]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All)]
    public class WithArgumentsExplicitAssertAttribute : Attribute
    {
        [Kept]
        public WithArgumentsExplicitAssertAttribute(object arg1, int arg2, string arg3, Type arg4)
        {
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class NoArgumentsWithDuplicatesAttribute : Attribute
    {
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class WithArgumentsWithDuplicatesLooseAssertAttribute : Attribute
    {
        [Kept]
        public WithArgumentsWithDuplicatesLooseAssertAttribute(object arg1, int arg2, string arg3, Type arg4)
        {
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class WithArgumentsWithDuplicatesExplicitAssertAttribute : Attribute
    {
        [Kept]
        public WithArgumentsWithDuplicatesExplicitAssertAttribute(object arg1, int arg2, string arg3, Type arg4)
        {
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    [KeptBaseType(typeof(Attribute))]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute))]
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute<T> : Attribute
    {
        [Kept]
        public WithArgumentsWithGenericWithDuplicatesExplicitAssertAttribute(object arg1, int arg2, string arg3, Type arg4)
        {
        }
    }
}
