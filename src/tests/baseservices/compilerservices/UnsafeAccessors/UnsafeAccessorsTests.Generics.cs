// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

struct Struct { }

public static unsafe class UnsafeAccessorsTestsGenerics
{
    class ClassWithEnum<T>
    {
        public enum Enum { }
    }

    class MyList<T>
    {
        public const string StaticGenericFieldName = nameof(_GF);
        public const string StaticFieldName = nameof(_F);
        public const string GenericFieldName = nameof(_list);
        public const string GenericEnumFieldName = nameof(_enum);

        static MyList()
        {
            _F = typeof(T).ToString();
        }

        public static void SetStaticGenericField(T val) => _GF = val;
        private static T _GF;
        private static string _F;

        private List<T> _list;
        private ClassWithEnum<T>.Enum _enum;

        public MyList() => _list = new();

        private MyList(int i) => _list = new(i);

        private MyList(List<T> list) => _list = list;

        private void Clear() => _list.Clear();

        private void Add(T t) => _list.Add(t);

        private void AddWithIgnore<U>(T t, U _) => _list.Add(t);

        private bool CanCastToElementType<U>(U t) => t is T;

        private static bool CanUseElementType<U>(U t) => t is T;

        private static Type ElementType() => typeof(T);

        private void Add(int a) =>
            Unsafe.As<List<int>>(_list).Add(a);

        private void Add(string a) =>
            Unsafe.As<List<string>>(_list).Add(a);

        private void Add(Struct a) =>
            Unsafe.As<List<Struct>>(_list).Add(a);

        public int Count => _list.Count;

        public int Capacity => _list.Capacity;
    }

    static class Accessors<V>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        public extern static MyList<V> Create(int a);

        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        public extern static MyList<V> CreateWithList(List<V> a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = ".ctor")]
        public extern static void CallCtorAsMethod(MyList<V> l, List<V> a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Add")]
        public extern static void AddInt(MyList<V> l, int a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Add")]
        public extern static void AddString(MyList<V> l, string a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Add")]
        public extern static void AddStruct(MyList<V> l, Struct a);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Clear")]
        public extern static void Clear(MyList<V> l);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Add")]
        public extern static void Add(MyList<V> l, V element);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "AddWithIgnore")]
        public extern static void AddWithIgnore<W>(MyList<V> l, V element, W ignore);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CanCastToElementType")]
        public extern static bool CanCastToElementType<W>(MyList<V> l, W element);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CreateMessage")]
        public extern static string CreateMessage(GenericBase<V> b, V v);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "ElementType")]
        public extern static Type ElementType(MyList<V> l);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "CanUseElementType")]
        public extern static bool CanUseElementType<W>(MyList<V> l, W element);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=MyList<object>.GenericFieldName)]
        public extern static ref List<V> GetPrivateField(MyList<V> a);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name=MyList<int>.GenericEnumFieldName)]
        public extern static ref ClassWithEnum<V>.Enum GetPrivateEnumField(MyList<V> d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=MyList<int>.StaticGenericFieldName)]
        public extern static ref V GetPrivateStaticField(MyList<V> d);
    }

    [Fact]
    public static void Verify_Generic_AccessStaticFieldClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_AccessStaticFieldClass)}");

        Assert.Equal(typeof(int).ToString(), GetPrivateStaticFieldInt((MyList<int>)null));

        Assert.Equal(typeof(string).ToString(), GetPrivateStaticFieldString((MyList<string>)null));

        Assert.Equal(typeof(Struct).ToString(), GetPrivateStaticFieldStruct((MyList<Struct>)null));

        {
            int expected = 10;
            MyList<int>.SetStaticGenericField(expected);
            Assert.Equal(expected, Accessors<int>.GetPrivateStaticField((MyList<int>)null));
        }
        {
            string expected = "abc";
            MyList<string>.SetStaticGenericField(expected);
            Assert.Equal(expected, Accessors<string>.GetPrivateStaticField((MyList<string>)null));
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=MyList<int>.StaticFieldName)]
        extern static ref string GetPrivateStaticFieldInt(MyList<int> d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=MyList<string>.StaticFieldName)]
        extern static ref string GetPrivateStaticFieldString(MyList<string> d);

        [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name=MyList<Struct>.StaticFieldName)]
        extern static ref string GetPrivateStaticFieldStruct(MyList<Struct> d);
    }

    [Fact]
    public static void Verify_Generic_AccessFieldClass()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_AccessFieldClass)}");
        {
            MyList<int> a = new();
            Assert.NotNull(Accessors<int>.GetPrivateField(a));
            Accessors<int>.GetPrivateEnumField(a) = default;
        }
        {
            MyList<string> a = new();
            Assert.NotNull(Accessors<string>.GetPrivateField(a));
            Accessors<string>.GetPrivateEnumField(a) = default;
        }
        {
            MyList<Struct> a = new();
            Assert.NotNull(Accessors<Struct>.GetPrivateField(a));
            Accessors<Struct>.GetPrivateEnumField(a) = default;
        }
    }

    class Base
    {
        protected virtual string CreateMessageGeneric<T>(T t) => $"{nameof(Base)}:{t}";
    }

    class GenericBase<T> : Base
    {
        protected virtual string CreateMessage(T t) => $"{nameof(GenericBase<T>)}:{t}";
        protected override string CreateMessageGeneric<U>(U u) => $"{nameof(GenericBase<T>)}:{u}";
    }

    sealed class Derived1 : GenericBase<string>
    {
        protected override string CreateMessage(string u) => $"{nameof(Derived1)}:{u}";
        protected override string CreateMessageGeneric<U>(U t) => $"{nameof(Derived1)}:{t}";
    }

    sealed class Derived2 : GenericBase<string>
    {
    }

    [Fact]
    public static void Verify_Generic_InheritanceMethodResolution()
    {
        string expect = "abc";
        Console.WriteLine($"Running {nameof(Verify_Generic_InheritanceMethodResolution)}");
        {
            Base a = new();
            Assert.Equal($"{nameof(Base)}:1", CreateMessage<int>(a, 1));
            Assert.Equal($"{nameof(Base)}:{expect}", CreateMessage<string>(a, expect));
            Assert.Equal($"{nameof(Base)}:{nameof(Struct)}", CreateMessage<Struct>(a, new Struct()));
        }
        {
            GenericBase<int> a = new();
            Assert.Equal($"{nameof(GenericBase<int>)}:1", CreateMessage<int>(a, 1));
            Assert.Equal($"{nameof(GenericBase<int>)}:{expect}", CreateMessage<string>(a, expect));
            Assert.Equal($"{nameof(GenericBase<int>)}:{nameof(Struct)}", CreateMessage<Struct>(a, new Struct()));
        }
        {
            GenericBase<string> a = new();
            Assert.Equal($"{nameof(GenericBase<string>)}:1", CreateMessage<int>(a, 1));
            Assert.Equal($"{nameof(GenericBase<string>)}:{expect}", CreateMessage<string>(a, expect));
            Assert.Equal($"{nameof(GenericBase<string>)}:{nameof(Struct)}", CreateMessage<Struct>(a, new Struct()));
        }
        {
            GenericBase<Struct> a = new();
            Assert.Equal($"{nameof(GenericBase<Struct>)}:1", CreateMessage<int>(a, 1));
            Assert.Equal($"{nameof(GenericBase<Struct>)}:{expect}", CreateMessage<string>(a, expect));
            Assert.Equal($"{nameof(GenericBase<Struct>)}:{nameof(Struct)}", CreateMessage<Struct>(a, new Struct()));
        }
        {
            Derived1 a = new();
            Assert.Equal($"{nameof(Derived1)}:1", CreateMessage<int>(a, 1));
            Assert.Equal($"{nameof(Derived1)}:{expect}", CreateMessage<string>(a, expect));
            Assert.Equal($"{nameof(Derived1)}:{nameof(Struct)}", CreateMessage<Struct>(a, new Struct()));
        }
        {
            // Verify resolution of generic override logic.
            Derived1 a1 = new();
            Derived2 a2 = new();
            Assert.Equal($"{nameof(Derived1)}:{expect}", Accessors<string>.CreateMessage(a1, expect));
            Assert.Equal($"{nameof(GenericBase<string>)}:{expect}", Accessors<string>.CreateMessage(a2, expect));
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "CreateMessageGeneric")]
        extern static string CreateMessage<W>(Base b, W w);
    }

    [Fact]
    public static void Verify_Generic_CallCtor()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_CallCtor)}");

        // Call constructor with non-generic parameter
        {
            MyList<int> a = Accessors<int>.Create(1);
            Assert.Equal(1, a.Capacity);
        }
        {
            MyList<string> a = Accessors<string>.Create(2);
            Assert.Equal(2, a.Capacity);
        }
        {
            MyList<Struct> a = Accessors<Struct>.Create(3);
            Assert.Equal(3, a.Capacity);
        }

        // Call constructor using generic parameter
        {
            MyList<int> a = Accessors<int>.CreateWithList([ 1 ]);
            Assert.Equal(1, a.Count);
        }
        {
            MyList<string> a = Accessors<string>.CreateWithList([ "1", "2" ]);
            Assert.Equal(2, a.Count);
        }
        {
            MyList<Struct> a = Accessors<Struct>.CreateWithList([new Struct(), new Struct(), new Struct()]);
            Assert.Equal(3, a.Count);
        }

        // Call constructors as methods
        {
            MyList<int> a = (MyList<int>)RuntimeHelpers.GetUninitializedObject(typeof(MyList<int>));
            Accessors<int>.CallCtorAsMethod(a, [1]);
            Assert.Equal(1, a.Count);
        }
        {
            MyList<string> a = (MyList<string>)RuntimeHelpers.GetUninitializedObject(typeof(MyList<string>));
            Accessors<string>.CallCtorAsMethod(a, ["1", "2"]);
            Assert.Equal(2, a.Count);
        }
        {
            MyList<Struct> a = (MyList<Struct>)RuntimeHelpers.GetUninitializedObject(typeof(MyList<Struct>));
            Accessors<Struct>.CallCtorAsMethod(a, [new Struct(), new Struct(), new Struct()]);
            Assert.Equal(3, a.Count);
        }
    }

    [Fact]
    public static void Verify_Generic_GenericTypeNonGenericInstanceMethod()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_GenericTypeNonGenericInstanceMethod)}");
        {
            MyList<int> a = new();
            Accessors<int>.AddInt(a, 1);
            Assert.Equal(1, a.Count);
            Accessors<int>.Clear(a);
            Assert.Equal(0, a.Count);
        }
        {
            MyList<string> a = new();
            Accessors<string>.AddString(a, "1");
            Accessors<string>.AddString(a, "2");
            Assert.Equal(2, a.Count);
            Accessors<string>.Clear(a);
            Assert.Equal(0, a.Count);
        }
        {
            MyList<Struct> a = new();
            Accessors<Struct>.AddStruct(a, new Struct());
            Accessors<Struct>.AddStruct(a, new Struct());
            Accessors<Struct>.AddStruct(a, new Struct());
            Assert.Equal(3, a.Count);
            Accessors<Struct>.Clear(a);
            Assert.Equal(0, a.Count);
        }
    }

    [Fact]
    public static void Verify_Generic_GenericTypeGenericInstanceMethod()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_GenericTypeGenericInstanceMethod)}");
        {
            MyList<int> a = new();
            Assert.True(Accessors<int>.CanCastToElementType<int>(a, 1));
            Assert.False(Accessors<int>.CanCastToElementType<string>(a, string.Empty));
            Assert.False(Accessors<int>.CanCastToElementType<Struct>(a, new Struct()));
            Assert.Equal(0, a.Count);
            Accessors<int>.Add(a, 1);
            Accessors<int>.AddWithIgnore<int>(a, 1, 1);
            Accessors<int>.AddWithIgnore<string>(a, 1, string.Empty);
            Accessors<int>.AddWithIgnore<Struct>(a, 1, new Struct());
            Assert.Equal(4, a.Count);
        }
        {
            MyList<string> a = new();
            Assert.False(Accessors<string>.CanCastToElementType<int>(a, 1));
            Assert.True(Accessors<string>.CanCastToElementType<string>(a, string.Empty));
            Assert.False(Accessors<string>.CanCastToElementType<Struct>(a, new Struct()));
            Assert.Equal(0, a.Count);
            Accessors<string>.Add(a, string.Empty);
            Accessors<string>.AddWithIgnore<int>(a, string.Empty, 1);
            Accessors<string>.AddWithIgnore<string>(a, string.Empty, string.Empty);
            Accessors<string>.AddWithIgnore<Struct>(a, string.Empty, new Struct());
            Assert.Equal(4, a.Count);
        }
        {
            MyList<Struct> a = new();
            Assert.False(Accessors<Struct>.CanCastToElementType<int>(a, 1));
            Assert.False(Accessors<Struct>.CanCastToElementType<string>(a, string.Empty));
            Assert.True(Accessors<Struct>.CanCastToElementType<Struct>(a, new Struct()));
            Assert.Equal(0, a.Count);
            Accessors<Struct>.Add(a, new Struct());
            Accessors<Struct>.AddWithIgnore<int>(a, new Struct(), 1);
            Accessors<Struct>.AddWithIgnore<string>(a, new Struct(), string.Empty);
            Accessors<Struct>.AddWithIgnore<Struct>(a, new Struct(), new Struct());
            Assert.Equal(4, a.Count);
        }
    }

    [Fact]
    public static void Verify_Generic_GenericTypeNonGenericStaticMethod()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_GenericTypeNonGenericStaticMethod)}");
        {
            Assert.Equal(typeof(int), Accessors<int>.ElementType(null));
            Assert.Equal(typeof(string), Accessors<string>.ElementType(null));
            Assert.Equal(typeof(Struct), Accessors<Struct>.ElementType(null));
        }
    }

    [Fact]
    public static void Verify_Generic_GenericTypeGenericStaticMethod()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_GenericTypeGenericStaticMethod)}");
        {
            Assert.True(Accessors<int>.CanUseElementType<int>(null, 1));
            Assert.False(Accessors<int>.CanUseElementType<string>(null, string.Empty));
            Assert.False(Accessors<int>.CanUseElementType<Struct>(null, new Struct()));
        }
        {
            Assert.False(Accessors<string>.CanUseElementType<int>(null, 1));
            Assert.True(Accessors<string>.CanUseElementType<string>(null, string.Empty));
            Assert.False(Accessors<string>.CanUseElementType<Struct>(null, new Struct()));
        }
        {
            Assert.False(Accessors<Struct>.CanUseElementType<int>(null, 1));
            Assert.False(Accessors<Struct>.CanUseElementType<string>(null, string.Empty));
            Assert.True(Accessors<Struct>.CanUseElementType<Struct>(null, new Struct()));
        }
    }

    class ClassWithConstraints
    {
        private string M<T, U>() where T : U, IEquatable<T>
            => $"{typeof(T)}|{typeof(U)}";

        private static string SM<T, U>() where T : U, IEquatable<T>
            => $"{typeof(T)}|{typeof(U)}";
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/102942", TestRuntimes.Mono)]
    public static void Verify_Generic_ConstraintEnforcement()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_ConstraintEnforcement)}");

        Assert.Equal($"{typeof(string)}|{typeof(object)}", CallMethod<string, object>(new ClassWithConstraints()));
        Assert.Equal($"{typeof(string)}|{typeof(object)}", CallStaticMethod<string, object>(null));
        Assert.Throws<InvalidProgramException>(() => CallMethod_NoConstraints<string, object>(new ClassWithConstraints()));
        Assert.Throws<InvalidProgramException>(() => CallMethod_MissingConstraint<string, object>(new ClassWithConstraints()));
        Assert.Throws<InvalidProgramException>(() => CallStaticMethod_NoConstraints<string, object>(null));
        Assert.Throws<InvalidProgramException>(() => CallStaticMethod_MissingConstraint<string, object>(null));

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
        extern static string CallMethod<V,W>(ClassWithConstraints c) where V : W, IEquatable<V>;

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
        extern static string CallMethod_NoConstraints<V,W>(ClassWithConstraints c);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
        extern static string CallMethod_MissingConstraint<V,W>(ClassWithConstraints c) where V : W;

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "SM")]
        extern static string CallStaticMethod<V,W>(ClassWithConstraints c) where V : W, IEquatable<V>;

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "SM")]
        extern static string CallStaticMethod_NoConstraints<V,W>(ClassWithConstraints c);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "SM")]
        extern static string CallStaticMethod_MissingConstraint<V,W>(ClassWithConstraints c) where V : W;
    }

    class Invalid
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        public static extern string CallToString<U>(U a);
    }

    class Invalid<T>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name=nameof(ToString))]
        public static extern string CallToString(T a);
    }

    [Fact]
    public static void Verify_Generic_InvalidUseUnsafeAccessor()
    {
        Console.WriteLine($"Running {nameof(Verify_Generic_InvalidUseUnsafeAccessor)}");

        Assert.Throws<BadImageFormatException>(() => Invalid.CallToString<int>(0));
        Assert.Throws<BadImageFormatException>(() => Invalid<int>.CallToString(0));
        Assert.Throws<BadImageFormatException>(() => Invalid.CallToString<string>(string.Empty));
        Assert.Throws<BadImageFormatException>(() => Invalid<string>.CallToString(string.Empty));
        Assert.Throws<BadImageFormatException>(() => Invalid.CallToString<Struct>(new Struct()));
        Assert.Throws<BadImageFormatException>(() => Invalid<Struct>.CallToString(new Struct()));
    }
}
