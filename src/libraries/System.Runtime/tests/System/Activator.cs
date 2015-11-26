// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;

using ActivatorTestsTestData;
using Xunit;

public static class ActivatorTests
{
    [Fact]
    public static void TestActivatorCreateInstanceBinding()
    {
        Type t;
        object[] args;
        Choice1 c1;

        // Root the constructors
        if (string.Empty.Length > 0)
        {
            new Choice1();
            new Choice1(123);
            new Choice1("Hey");
            new Choice1(5.1);
            new Choice1(new VarArgs());
            new Choice1(new VarStringArgs());
            new Choice1(new VarIntArgs());
        }

        t = null;
        args = new object[1];
        Assert.Throws<ArgumentNullException>(() => Activator.CreateInstance(t, args));

        t = typeof(Choice1);
        args = null;   // Passing a null args is equivalent to passing an empty array of args.
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 1);

        t = typeof(Choice1);
        args = new object[] { };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 1);

        t = typeof(Choice1);
        args = new object[] { 42 };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 2);

        // Primitive widening is allowed by the binder.
        // but not by Dynamic.DelegateInvoke()... 
        {
            t = typeof(Choice1);
            args = new object[] { (short)(-2) };
            c1 = (Choice1)(Activator.CreateInstance(t, args));
            Assert.Equal(c1.I, 2);
        }

        t = typeof(Choice1);
        args = new object[] { "Hello" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 3);

        t = typeof(Choice1);
        args = new object[] { null };
        Assert.Throws<AmbiguousMatchException>(() => Activator.CreateInstance(t, args));

        // "optional" parameters are not optional as far as Activator.CreateInstance() is concerned.
        t = typeof(Choice1);
        args = new object[] { 5.1 };
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(t, args));

        t = typeof(Choice1);
        args = new object[] { 5.1, Type.Missing };
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(t, args));

        t = typeof(Choice1);
        args = new object[] { 5.1, "Yes" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 4);

        //
        // "params" arguments are honored by Activator.CreateInstance()
        //
        VarArgs varArgs = new VarArgs();
        t = typeof(Choice1);
        args = new object[] { varArgs };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 5);

        t = typeof(Choice1);
        args = new object[] { varArgs, "P1" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 5);

        t = typeof(Choice1);
        args = new object[] { varArgs, "P1", "P2" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 5);

        VarStringArgs varStringArgs = new VarStringArgs();
        t = typeof(Choice1);
        args = new object[] { varStringArgs };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 6);

        t = typeof(Choice1);
        args = new object[] { varStringArgs, "P1" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 6);

        t = typeof(Choice1);
        args = new object[] { varStringArgs, "P1", "P2" };
        c1 = (Choice1)(Activator.CreateInstance(t, args));
        Assert.Equal(c1.I, 6);

        t = typeof(Choice1);
        args = new object[] { varStringArgs, 5, 6 };
        Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(t, args));

        //
        // Primitive widening not supported for "params" arguments.
        //
        // (This is probably an accidental behavior on the desktop as the default binder specifically checks to see if the params arguments are widenable to the
        // params array element type and gives it the go-ahead if it is. Unfortunately, the binder then bollixes itself by using Array.Copy() to copy
        // the params arguments. Since Array.Copy() doesn't tolerate this sort of type mismatch, it throws an InvalidCastException which bubbles out
        // out of Activator.CreateInstance. Accidental or not, we'll inherit that behavior on .NET Native.)
        //
        VarIntArgs varIntArgs = new VarIntArgs();
        t = typeof(Choice1);
        args = new object[] { varIntArgs, 1, (short)2 };
        Assert.Throws<InvalidCastException>(() => Activator.CreateInstance(t, args));

        return;
    }

    public class TypeWithoutDefaultCtor
    {
        private TypeWithoutDefaultCtor(int x) { }
    }

    private class CustomException : Exception
    {
    }

    private struct StructTypeWithoutReflectionMetadata
    {
    }

    public class TypeWithDefaultCtorThatThrows
    {
        public TypeWithDefaultCtorThatThrows() { throw new CustomException(); }
    }

    [Fact]
    public static void TestActivatorOnNonActivatableTypes()
    {
        int x = 0;
        Assert.ThrowsAny<MissingMemberException>(() => { ++x; Activator.CreateInstance<TypeWithoutDefaultCtor>(); });
        Assert.Throws<TargetInvocationException>(() => { ++x; Activator.CreateInstance<TypeWithDefaultCtorThatThrows>(); });
    }

    [Fact]
    public static void TestActivatorWithDelegates()
    {
        Func<object> activate1 = Activator.CreateInstance<Choice1>;
        Assert.True(activate1() is Choice1);

        Func<DateTime> activateDateTime = Activator.CreateInstance<DateTime>;
        activateDateTime();

        Func<StructTypeWithoutReflectionMetadata> activateStruct = Activator.CreateInstance<StructTypeWithoutReflectionMetadata>;
        activateStruct();

        Func<object> activate2 = Activator.CreateInstance<TypeWithoutDefaultCtor>;
        Assert.ThrowsAny<MissingMemberException>(() => activate2());

        Func<object> activate3 = Activator.CreateInstance<TypeWithDefaultCtorThatThrows>;
        Assert.Throws<TargetInvocationException>(() => activate3());
    }
}

namespace ActivatorTestsTestData
{
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
}
