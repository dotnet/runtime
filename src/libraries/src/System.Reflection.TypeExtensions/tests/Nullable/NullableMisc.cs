// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Reflection.Compatibility.UnitTests.Nullable
{
    public class NullableObjectTests
    {
        [Fact]
        public static void TestGetInterfaces()
        {
            // ImplementTwoInterface should get 2 interfaces
            Assert.Equal(2, typeof(ImplementTwoInterface).GetInterfaces().Length);
            // ImplementTwoInterface? should get 0 interfaces
            Assert.Equal(0, typeof(ImplementTwoInterface?).GetInterfaces().Length);
            // EmptyStruct? should get 0 interfaces
            Assert.Equal(0, typeof(EmptyStruct?).GetInterfaces().Length);
        }

        [Fact]
        public static void TestGetType()
        {
            // object.GetType() always get "object" boxed ??
            // will never get back typeof(Nullable<T>)

            NotEmptyStructGen<Type> t = new NotEmptyStructGen<Type>();
            t.Field = typeof(string);
            Assert.Equal(typeof(NotEmptyStructGen<Type>), t.GetType());

            NotEmptyStructGen<Type>? tq = t;
            Assert.Equal(typeof(NotEmptyStructGen<Type>), tq.GetType());

            //after boxing
            object boxed = (object)tq;
            Assert.Equal(typeof(NotEmptyStructGen<Type>), boxed.GetType());

            // after **FAKE** unbox
            NotEmptyStructGen<Type>? unboxed = (NotEmptyStructGen<Type>?)boxed;
            Assert.Equal(typeof(NotEmptyStructGen<Type>), unboxed.GetType());
        }

        [Fact]
        public static void TestIsAssignableFrom()
        {
            // IsAssignableFrom int? int
            Assert.Equal(true, typeof(int?).IsAssignableFrom(typeof(int)));
            // IsAssignableFrom int int?
            Assert.Equal(false, typeof(int).IsAssignableFrom(typeof(int?)));
            // IsAssignableFrom int?[] int[]
            Assert.Equal(false, typeof(int?[]).IsAssignableFrom(typeof(int[])));
            // IsAssignableFrom 1
            Assert.Equal(true, typeof(IEmpty).IsAssignableFrom(typeof(ImplementTwoInterface)));
            // IsAssignableFrom 2
            Assert.Equal(false, typeof(IEmpty).IsAssignableFrom(typeof(ImplementTwoInterface?)));
        }

        [Fact]
        public static void TestIsInstanceOfType()
        {
            // IsInstanceOfType float
            Assert.Equal(true, typeof(float).IsInstanceOfType(1.0234F));
            // IsInstanceOfType string
            Assert.Equal(true, typeof(string).IsInstanceOfType("this is a string"));
            // IsInstanceOfType int
            Assert.Equal(true, typeof(int?).IsInstanceOfType(100));
            // IsInstanceOfType int?
            Assert.Equal(true, typeof(int?).IsInstanceOfType((int?)100));
        }

        #region Helper types

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // interfaces 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public interface IEmpty { }
        public interface INotEmpty { void DoNothing(); }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // generic interfaces 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public interface IEmptyGen<T> { }
        public interface INotEmptyGen<T> { void DoNothing(); }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // struct 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public struct EmptyStruct { }

        public struct NotEmptyStruct { public int Field; }

        public struct NotEmptyStructQ { public int? Field; }

        public struct NotEmptyStructA { public int[] Field; }

        public struct NotEmptyStructQA { public int?[] Field; }

        public struct NotEmptyStructS { public string Field; }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // generic structs 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public struct EmptyStructGen<T> { }

        public struct NotEmptyStructGen<T> { public T Field; }
        public struct NotEmptyStructConstrainedGen<T> where T : struct { public T Field; }
        public struct NotEmptyStructConstrainedGenA<T> where T : struct { public T[] Field; }
        public struct NotEmptyStructConstrainedGenQ<T> where T : struct { public T? Field; }
        public struct NotEmptyStructConstrainedGenQA<T> where T : struct { public T?[] Field; }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // nested struct 
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public struct NestedStruct
        {
            public struct Nested { }
        }

        public struct NestedStructGen<T>
        {
            public struct Nested { }
        }


        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // struct with Field Offset
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-

        [StructLayout(LayoutKind.Explicit)]
        public struct ExplicitFieldOffsetStruct
        {
            [FieldOffset(0)]
            public int Field00;

            // =========================================================
            //[FieldOffset(0x0f)] 
            // which will cause failures on cross-domain invocation. 
            // also vsw#530141: System.DataMisalignedException thrown when compaing structs with StructLayout attribute
            // =========================================================
            [FieldOffset(0x10)]
            public int Field15;
        }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // struct implement interfaces
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public struct ImplementOneInterface : IEmpty { }

        public struct ImplementTwoInterface : IEmpty, INotEmpty
        {
            public void DoNothing() { }
            public int? Field;
        }

        public struct ImplementOneInterfaceGen<T> : IEmptyGen<T> { }
        public struct ImplementTwoInterfaceGen<T> : IEmptyGen<T>, INotEmptyGen<T>
        {
            public void DoNothing() { }
            public int? Field;
        }

        public struct ImplementAllInterface<T> : IEmpty, INotEmpty, IEmptyGen<T>, INotEmptyGen<T>
        {
            public void DoNothing() { }
            void INotEmptyGen<T>.DoNothing() { }
        }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // enums
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        public enum IntE { Start = 100, Default = 0, Middle = 500, End = 1000 }
        public enum ByteE : byte { Default = 0, Start = 10, Middle = 100, End = 200 }
        public enum LongE : long { Start = -1000, Default = 0, Middle = 5, End = 30000 }



        public struct WithOnlyFXTypeStruct
        {
            public Guid GUID;
            public decimal DECIMAL;
        }


        public struct AllInteger
        {
            public int? fq;
            public int f;
            public static int? sfq;
            public static int sf;
            public int?[] fqa;
            public int[] fa;
            public static int?[] sfqa;
            public static int[] sfa;
        }


        public struct AllIntegerG<T>
        {
            public int? fq;
            public int f;
            public static int? sfq;
            public static int sf;
            public int?[] fqa;
            public int[] fa;
            public static int?[] sfqa;
            public static int[] sfa;
        }



        public struct AllStruct
        {
            public NotEmptyStructQ f1;
            public NotEmptyStructQ? fq1;

            public NotEmptyStructQA f2;
            public NotEmptyStructQA? fq2;

            public NotEmptyStructS f3;
            public NotEmptyStructS? fq3;

            public NotEmptyStructGen<Type> f4;
            public NotEmptyStructGen<Type>? fq4;
        }


        public struct AllRefType
        {
            public string f1;
            public Type f2;
            public NotEmptyClass f3;
            public NotEmptyClassGen<int> f4;
            public NotEmptyClassConstrainedGen<string> f5;
        }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-
        // other types
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-

        public class EmptyClass { }

        public class NotEmptyClass { public int Field; }

        public class EmptyClassGen<T> { }

        public class NotEmptyClassGen<T> { public T Field; }

        public class NotEmptyClassConstrainedGen<T> where T : class { public T Field; }

        public class NestedClass
        {
            public struct Nested { }
        }

        public class NestedClassGen<T>
        {
            public struct Nested { }
        }

        public class ImplementOneInterfaceC : IEmpty { }

        public class ImplementTwoInterfaceC : IEmpty, INotEmpty
        {
            public void DoNothing() { }
        }

        public class ImplementOneInterfaceGenC<T> : IEmptyGen<T> { }

        public class ImplementTwoInterfaceGenC<T> : IEmptyGen<T>, INotEmptyGen<T>
        {
            public void DoNothing() { }
        }

        public class ImplementAllInterfaceC<T> : IEmpty, INotEmpty, IEmptyGen<T>, INotEmptyGen<T>
        {
            public void DoNothing() { }
            void INotEmptyGen<T>.DoNothing() { }
        }

        public sealed class SealedClass { }

        public delegate void SimpleDelegate();
        public delegate void GenericDelegate<T>();

        #endregion
    }
}