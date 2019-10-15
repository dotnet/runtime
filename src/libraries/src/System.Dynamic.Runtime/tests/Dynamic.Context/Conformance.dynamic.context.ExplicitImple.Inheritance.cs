// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace Dynamic.Tests
{
    public class ExplicitlyImplementedInheritedInterfaceTests
    {
        [Fact]
        public void SubInterface_BaseInterface()
        {
            dynamic d = new ExplicitlyImplementedBaseAndSubInterface();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<BaseInterfaceWithOneMember1>(d);
            var y = Helpers.Cast<SubInterfaceWithOneMember1>(d);
        }

        [Fact]
        public void SubInterfaceOnly()
        {
            dynamic d = new ExplicitlyImplementedSubInterface();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<BaseInterfaceWithOneMember1>(d);
            var y = Helpers.Cast<SubInterfaceWithOneMember1>(d);
        }

        [Fact]
        public void BaseInterfaceOnly()
        {
            dynamic d = new ExplicitlyImplementedBaseInterfaceWithTwoMembers();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());
            Assert.Throws<InvalidCastException>(() => Helpers.Cast<SubInterfaceWithNoMembers>(d));
        }

        [Fact]
        public void SubInterface_WithNewMember()
        {
            dynamic d = new ExplicitlyImplementedSubInterfaceWithNewMember();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<SubInterfaceWithNoMembers>(d));
            Assert.Throws<InvalidCastException>(() => ((SubInterfaceWithNoMembers)d).Foo());
        }

        [Fact]
        public void SubInterfaceWithNewMember_SubInterfaceWithNoMembers()
        {
            dynamic d = new ExplicitlyImplementedSubInterfaceWithNewMemberAndSubInterfaceWithNoMembers();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<SubInterfaceWithNoMembers>(d);
        }

        [Fact]
        public void InterfaceWithTwoMembers_InSubClass()
        {
            dynamic d = new EmptyClass();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            Assert.Throws<InvalidCastException>(() => Helpers.Cast<BaseInterfaceWithTwoMembers>(d));
            Assert.Throws<InvalidCastException>(() => ((BaseInterfaceWithTwoMembers)d).Foo());
        }

        [Fact]
        public void InterfaceWithTwoMembers_InBaseClass()
        {
            dynamic d = new SubClassOfExplicitlyImplementedInterfaceWithTwoMembersAndEmptySubClass();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<BaseInterfaceWithTwoMembers>(d);
        }

        [Fact]
        public void NonGenericInterface_Interface_WithTwoMembers_PartialClass()
        {
            dynamic d = new ExplicitlyImplementedInterfaceInPartialClass();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<BaseInterfaceWithTwoMembers>(d);
        }

        [Fact]
        public void PartialInterfaceWithTwoMembers()
        {
            dynamic d = new ExplicitlyImlementedPartialInterface();
            Assert.Throws<RuntimeBinderException>(() => d.Foo());

            var x = Helpers.Cast<PartialInterfaceWithTwoMembers>(d);
        }

        [Fact]
        public void SubInterfaceWithOneMember_BaseClassWithOneVirtualMethod()
        {
            dynamic d = new ExplicitlyImplementedSubInterfaceWithOneMemberAndVirtualBaseClass();
            Assert.Equal(1, d.Foo());
            Assert.Equal(-1, ((SubInterfaceWithOneMember2)d).Foo());
        }
    }

    public interface BaseInterfaceWithOneMember1
    {
        int Foo();
    }

    public interface SubInterfaceWithOneMember1 : BaseInterfaceWithOneMember1
    {
        int Bar();
    }

    public class ExplicitlyImplementedBaseAndSubInterface : BaseInterfaceWithOneMember1, SubInterfaceWithOneMember1
    {
        int BaseInterfaceWithOneMember1.Foo() => 0;
        public int Bar() => 1;
    }

    public class ExplicitlyImplementedSubInterface : SubInterfaceWithOneMember1
    {
        int BaseInterfaceWithOneMember1.Foo() => 0;
        public int Bar() => 1;
    }

    public interface BaseInterfaceWithTwoMembers
    {
        int Foo();
        int Bar();
    }

    public interface SubInterfaceWithNoMembers : BaseInterfaceWithTwoMembers { }

    public interface SubInterfaceWithNewMember : BaseInterfaceWithTwoMembers
    {
        new int Foo();
    }

    public class ExplicitlyImplementedBaseInterfaceWithTwoMembers : BaseInterfaceWithTwoMembers
    {
        int BaseInterfaceWithTwoMembers.Foo() => 0;
        public int Bar() => 1;
    }

    public class ExplicitlyImplementedSubInterfaceWithNewMember : SubInterfaceWithNewMember
    {
        int SubInterfaceWithNewMember.Foo() => 0;
        int BaseInterfaceWithTwoMembers.Foo() => 2;
        public int Bar() => 1;
    }

    public class ExplicitlyImplementedSubInterfaceWithNewMemberAndSubInterfaceWithNoMembers : SubInterfaceWithNewMember, SubInterfaceWithNoMembers
    {
        int SubInterfaceWithNewMember.Foo() => 0;
        int BaseInterfaceWithTwoMembers.Foo() => 2;
        public int Bar() => 1;
    }

    public class EmptyClass { }

    public class ExplicitlyImplementedInterfaceWithTwoMembersAndEmptyBaseClass : EmptyClass, BaseInterfaceWithTwoMembers
    {
        int BaseInterfaceWithTwoMembers.Foo() => 0;
        public int Bar() => 1;
    }

    public class ExplicitlyImplementedInterfaceWithTwoMembersAndEmptySubClass : BaseInterfaceWithTwoMembers
    {
        int BaseInterfaceWithTwoMembers.Foo() => 0;
        public int Bar() => 1;
    }

    public class SubClassOfExplicitlyImplementedInterfaceWithTwoMembersAndEmptySubClass : ExplicitlyImplementedInterfaceWithTwoMembersAndEmptySubClass { }

    public partial class ExplicitlyImplementedInterfaceInPartialClass { }

    public partial class ExplicitlyImplementedInterfaceInPartialClass : BaseInterfaceWithTwoMembers
    {
        int BaseInterfaceWithTwoMembers.Foo() => 0;
        public int Bar() => 1;
    }

    public partial interface PartialInterfaceWithTwoMembers
    {
        int Foo();
    }

    public partial interface PartialInterfaceWithTwoMembers
    {
        int Bar();
    }

    public class ExplicitlyImlementedPartialInterface : PartialInterfaceWithTwoMembers
    {
        int PartialInterfaceWithTwoMembers.Foo() => 0;
        public int Bar() => 1;
    }

    public class BaseClassWithVirtualMethod
    {
        public virtual int Foo() => -1;
    }

    public interface BaseInterfaceWithOneMember2
    {
        int Bar();
    }

    public interface SubInterfaceWithOneMember2 : BaseInterfaceWithOneMember2
    {
        int Foo();
    }

    public class ExplicitlyImplementedSubInterfaceWithOneMemberAndVirtualBaseClass : BaseClassWithVirtualMethod, SubInterfaceWithOneMember2
    {
        int SubInterfaceWithOneMember2.Foo() => -1;
        public override int Foo() => 1;
        public int Bar() => 1;
    }
}
