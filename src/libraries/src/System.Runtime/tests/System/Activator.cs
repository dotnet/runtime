// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

using Xunit;

public static class ActivatorTests
{
    [Fact]
    public static void TestCreateInstance()
    {
        // Passing null args is equivilent to an empty array of args.
        Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), null));
        Assert.Equal(1, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { }));
        Assert.Equal(1, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { 42 }));
        Assert.Equal(2, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { "Hello" }));
        Assert.Equal(3, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { 5.1, "Hello" }));
        Assert.Equal(4, c.I);

        Activator.CreateInstance(typeof(StructTypeWithoutReflectionMetadata));
    }

    [Fact]
    public static void TestCreateInstance_ConstructorWithPrimitive_PerformsPrimitiveWidening()
    {
        // Primitive widening is allowed by the binder, but not by Dynamic.DelegateInvoke().
        Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { (short)-2 }));
        Assert.Equal(2, c.I);
    }

    [Fact]
    public static void TestCreateInstance_ConstructorWithParamsParameter()
    {
        // C# params arguments are honored by Activator.CreateInstance()
        Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs() }));
        Assert.Equal(5, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs(), "P1" }));
        Assert.Equal(5, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs(), "P1", "P2" }));
        Assert.Equal(5, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs() }));
        Assert.Equal(6, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), "P1" }));
        Assert.Equal(6, c.I);

        c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), "P1", "P2" }));
        Assert.Equal(6, c.I);
    }

    [Fact]
    public static void TestCreateInstance_Invalid()
    {
        Assert.Throws<ArgumentNullException>("type", () => Activator.CreateInstance(null)); // Type is null
        Assert.Throws<ArgumentNullException>("type", () => Activator.CreateInstance(null, new object[0])); // Type is null

        Assert.Throws<AmbiguousMatchException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { null }));

        // C# designated optional parameters are not optional as far as Activator.CreateInstance() is concerned.
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1 }));
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1, Type.Missing }));

        // Invalid params args
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), 5, 6 }));

        // Primitive widening not supported for "params" arguments.
        //
        // (This is probably an accidental behavior on the desktop as the default binder specifically checks to see if the params arguments are widenable to the
        // params array element type and gives it the go-ahead if it is. Unfortunately, the binder then bollixes itself by using Array.Copy() to copy
        // the params arguments. Since Array.Copy() doesn't tolerate this sort of type mismatch, it throws an InvalidCastException which bubbles out
        // out of Activator.CreateInstance. Accidental or not, we'll inherit that behavior on .NET Native.)
        Assert.Throws<InvalidCastException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarIntArgs(), 1, (short)2 }));

        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<TypeWithoutDefaultCtor>()); // Type has no default constructor
        Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<TypeWithDefaultCtorThatThrows>()); // Type has a default constructor throws an exception
    }

    [Fact]
    public static void TestCreateInstance_Generic()
    {
        Choice1 c = Activator.CreateInstance<Choice1>();
        Assert.Equal(1, c.I);

        Activator.CreateInstance<DateTime>();
        Activator.CreateInstance<StructTypeWithoutReflectionMetadata>();
    }

    [Fact]
    public static void TestCreateInstance_Generic_Invalid()
    {
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<int[]>()); // Cannot create array type

        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<TypeWithoutDefaultCtor>()); // Type has no default constructor
        Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<TypeWithDefaultCtorThatThrows>()); // Type has a default constructor that throws
    }


    class PrivateType
    {
        public PrivateType() { }
    }

    class PrivateTypeWithDefaultCtor
    {
        private PrivateTypeWithDefaultCtor() { }
    }

    class PrivateTypeWithoutDefaultCtor
    {
        private PrivateTypeWithoutDefaultCtor(int x) { }
    }

    class PrivateTypeWithDefaultCtorThatThrows
    {
        public PrivateTypeWithDefaultCtorThatThrows() { throw new Exception(); }
    }

    [Fact]
    public static void TestCreateInstance_Type_Bool()
    {
        Assert.Equal(typeof(PrivateType), Activator.CreateInstance(typeof(PrivateType), true).GetType());
        Assert.Equal(typeof(PrivateType), Activator.CreateInstance(typeof(PrivateType), false).GetType());

        Assert.Equal(typeof(PrivateTypeWithDefaultCtor), Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtor), true).GetType());
        Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtor), false).GetType());

        Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtorThatThrows), true).GetType());
        Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtorThatThrows), false).GetType());

        Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithoutDefaultCtor), true).GetType());
        Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithoutDefaultCtor), false).GetType());
    }

    public class Choice1 : Attribute
    {
        public Choice1()
        {
            I = 1;
        }

        public Choice1(int i)
        {
            I = 2;
        }

        public Choice1(string s)
        {
            I = 3;
        }

        public Choice1(double d, string optionalS = "Hey")
        {
            I = 4;
        }

        public Choice1(VarArgs varArgs, params object[] parameters)
        {
            I = 5;
        }

        public Choice1(VarStringArgs varArgs, params string[] parameters)
        {
            I = 6;
        }

        public Choice1(VarIntArgs varArgs, params int[] parameters)
        {
            I = 7;
        }

        public int I;
    }

    public class VarArgs
    {
    }

    public class VarStringArgs
    {
    }

    public class VarIntArgs
    {
    }

    public struct StructTypeWithoutReflectionMetadata
    {
    }

    public class TypeWithoutDefaultCtor
    {
        private TypeWithoutDefaultCtor(int x) { }
    }

    public class TypeWithDefaultCtorThatThrows
    {
        public TypeWithDefaultCtorThatThrows() { throw new Exception(); }
    }
}
