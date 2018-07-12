// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public partial class AccessTests
    {
        public abstract class PublicReferenceType
        {
            public abstract int IntValueProperty { get; }
        }

        public interface ITestIFace
        {
        }

        public interface ITestIFaceCons<out T> where T : ITestIFace
        {
        }

        private class Container
        {
            private abstract class ReferenceTypeIntermediary : PublicReferenceType
            {
            }

            private class ReferenceType : ReferenceTypeIntermediary, ITestIFace
            {
                public override int IntValueProperty => 23;
            }

            private struct PrivateValueType
            {
            }

            private protected struct PrivateProtectedValueType
            {
            }

            internal struct InternalValueType
            {
            }

            protected internal struct ProtectedInternalValueType
            {
            }

            private interface IPrivateInterface
            {
            }

            public static dynamic GetReferenceType() => new ReferenceType();

            public static dynamic TenReferenceTypesArray() => new ReferenceType[10];

            public static dynamic TenReferenceTypesRepeat() => Enumerable.Repeat(new ReferenceType(), 10);

            public static dynamic ReferenceTypeDelegate() =>
                (Func<int, IEnumerable<ReferenceType>>)(i => Enumerable.Repeat(new ReferenceType(), i));

            public static dynamic ValueTypeArray() => new PrivateValueType[2];

            public static dynamic PrivateProtectedValueTypeArray() => new PrivateProtectedValueType[2];

            public static dynamic InternalValueTypeArray() => new InternalValueType[2];

            public static dynamic ProtectedInternalValueTypeArray() => new ProtectedInternalValueType[2];

            private delegate int PrivateFunc<T>(T arg);

            public static dynamic PrivateDelegateType() => (PrivateFunc<ReferenceType>)(r => r.IntValueProperty);

            public static dynamic ValueTypeDelegate() => (Func<PrivateValueType>)(() => new PrivateValueType());

            public unsafe static dynamic PointerArray() => new PrivateValueType*[4];

            public static dynamic PrivateInterfaceDelegate() => (Func<IPrivateInterface>)(() => null);

            public static dynamic PrivateConstraintInterfaceDelegate() => (Func<ITestIFaceCons<ReferenceType>>)(() => null);

            public static dynamic PrivateProtectedInstance() => PrivateProtectedValueTypeArray()[1];
        }

        private class TypeWithFields
        {
            public int Public;
            protected int Protected;
            internal int Internal;
            protected internal int ProtectedInternal;
            private protected int PrivateProtected;
#pragma warning disable 414
            private int Private;
#pragma warning restore 414

            public TypeWithFields()
            {
                Public = 1;
                Protected = 2;
                Internal = 3;
                ProtectedInternal = 4;
                PrivateProtected = 5;
                Private = 6;
            }
        }

        private class TypeWithFieldsDerived : TypeWithFields
        {
            public void AccessibleFields()
            {
                dynamic d = this;
                Assert.Equal(1, d.Public);
                Assert.Equal(2, d.Protected);
                Assert.Equal(3, d.Internal);
                Assert.Equal(4, d.ProtectedInternal);
                Assert.Equal(5, d.PrivateProtected);
            }

            public void InAccessibleFields()
            {
                dynamic d = this;
                Assert.Throws<RuntimeBinderException>(() => d.Private);
            }
        }

        [Fact]
        public void CanGetAccessibleBaseOfInaccessibleType() => Assert.Equal(23, Container.GetReferenceType().IntValueProperty);

        [Fact]
        public void AccessibleArray() => Assert.Equal(10, Enumerable.Count(Container.TenReferenceTypesArray()));

        [Fact]
        public void AccessibleInterface() => Assert.Equal(10, Enumerable.Count(Container.TenReferenceTypesRepeat()));

        [Fact]
        public void AccessCovariantDelegate()
        {
            IEnumerable<PublicReferenceType> prts = Container.ReferenceTypeDelegate()(4);
            Assert.Equal(4, prts.Count());
            foreach (PublicReferenceType prt in prts)
            {
                Assert.Equal(23, prt.IntValueProperty);
            }
        }

        [Fact]
        public void NonCovariantArrayToArrayType() => Assert.Equal(2, Container.ValueTypeArray().Length);

        [Fact]
        public void NonCovariantArrayNotCastToIndexableType()
        {
            dynamic array = Container.ValueTypeArray();
            Assert.Throws<RuntimeBinderException>(() => array[0]);
        }

        [Fact]
        public void PointerArrayToArrayType() => Assert.Equal(4, Container.PointerArray().Length);

        [Fact]
        public void PointerArrayNotCastToIndexableType()
        {
            dynamic array = Container.PointerArray();
            Assert.Throws<RuntimeBinderException>(() => array[0]);
        }

        [Fact]
        public void PrivateDelegateType()
        {
            dynamic d = Container.PrivateDelegateType();
            dynamic a = Container.GetReferenceType();
            Assert.Throws<RuntimeBinderException>(() => d(a));
            d.DynamicInvoke(a); // Can use as MulticastDelegate.
        }

        [Fact]
        public void PrivateValueTypeDelegateType()
        {
            dynamic d = Container.ValueTypeDelegate();
            Assert.Throws<RuntimeBinderException>(() => d());
            ValueType result = d.DynamicInvoke(); // Can use as MulticastDelegate.
        }

        [Fact]
        public void PrivateIFaceDelegateType()
        {
            dynamic d = Container.PrivateInterfaceDelegate();
            Assert.Null(d());
        }

        [Fact]
        public void PrivateInterfaceConstraint()
        {
            // Casting Func<ITestIFaceCons<ReferenceType>> to Func<ITestIFaceCons<PublicReferenceType>>
            // would be illegal because ITestIFaceCons<PublicReferenceType> violates the ITestIFaceCons
            // constraints, so the binder has to detect that, and cast to Func<object>.
            dynamic d = Container.PrivateConstraintInterfaceDelegate();
            Assert.Null(d());
        }

        private struct SomeValueType
        {
            public override string ToString() => "test";
        }

        [Fact]
        public void NullableOfInaccessible()
        {
            // ValueType members work without access to the type.
            CallSite<Func<CallSite, SomeValueType?, object>> site =
                CallSite<Func<CallSite, SomeValueType?, object>>.Create(
                    Binder.InvokeMember(
                        CSharpBinderFlags.None, "ToString", null, null,
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                        }));
            Func<CallSite, SomeValueType?, object> target = site.Target;
            Assert.Equal("test", target(site, new SomeValueType()));

            // Nullable<T> members work with access to the type.
            site = CallSite<Func<CallSite, SomeValueType?, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.None, "GetValueOrDefault", null, GetType(),
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    }));
            target = site.Target;
            Assert.Equal(new SomeValueType(), target(site, new SomeValueType()));

            // Nullable<T> members don't work without access to the type.
            site = CallSite<Func<CallSite, SomeValueType?, object>>.Create(
                Binder.InvokeMember(
                    CSharpBinderFlags.None, "GetValueOrDefault", null, null,
                    new[]
                    {
                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                    }));
            target = site.Target;
            Assert.Throws<RuntimeBinderException>(() => target(site, new SomeValueType()));
        }

        [Fact]
        public void AccessNestedInternalOnlySameAssembly()
        {
            Assert.NotNull(Container.InternalValueTypeArray()[1]);
            Assert.NotNull(Container.ProtectedInternalValueTypeArray()[1]);
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void AccessNestedPrivateProtectedAssembly()
        {
            dynamic d = Container.PrivateProtectedValueTypeArray();
            Assert.Throws<RuntimeBinderException>(() => d[1]);
            Assert.NotNull(Container.PrivateProtectedInstance());
        }

        [Fact]
        public void AccessibleFields()
        {
            dynamic d = new TypeWithFields();
            Assert.Equal(1, d.Public);
            Assert.Equal(3, d.Internal);
            Assert.Equal(4, d.ProtectedInternal);
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void InaccessibleFields()
        {
            dynamic d = new TypeWithFields();
            Assert.Throws<RuntimeBinderException>(() => d.Protected);
            Assert.Throws<RuntimeBinderException>(() => d.PrivateProtected);
            Assert.Throws<RuntimeBinderException>(() => d.Private);
        }

        [Fact]
        public void AccessibleToDerivedFields()
        {
            new TypeWithFieldsDerived().AccessibleFields();
        }

        [Fact]
        public void InaccessibleToDerivedFields()
        {
            new TypeWithFieldsDerived().AccessibleFields();
        }
    }
}
