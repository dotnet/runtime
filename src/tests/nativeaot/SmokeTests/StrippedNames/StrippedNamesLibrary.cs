// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
namespace StrippedNamesLibrary
{
    public class Widget
    {
        public int Field;

        public Widget() { }

        public int Count { get; set; }

        public event EventHandler Changed;

        public string FirstMethod() => "first";
        public string SecondMethod() => "second";

        public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

        public class Nested { }
    }

    public class Base
    {
        public virtual string Hello() => "Base";
    }

    public class Derived : Base
    {
        public override string Hello() => "Derived";
    }

    // Generic virtual methods are matched by name and signature at runtime, so they keep their names.
    public class GvmBase
    {
        public virtual string Describe<T>() => "GvmBase";
        public virtual string Other<T>() => "GvmBase.Other";
    }

    public class GvmDerived : GvmBase
    {
        public override string Describe<T>() => "GvmDerived";
        public override string Other<T>() => "GvmDerived.Other";
    }

    public interface IConverter<T>
    {
        U Convert<U>(T value);
    }

    public class IntConverter : IConverter<int>
    {
        public U Convert<U>(int value) => (U)(object)(value + 1);
    }

    public class GenericHolder<T>
    {
        public T Value;
    }

    public enum Color
    {
        Red,
        Green,
    }

    public delegate int Callback(int x);

    [AttributeUsage(AttributeTargets.All)]
    public class MarkerAttribute : Attribute
    {
        public string Label { get; set; }
        public int Number;
    }

    [Marker(Label = "label", Number = 42)]
    public class Marked { }

    public class UniqueStrippedTypeNameMarker
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UniqueStrippedThrowingMethodMarker() => throw new InvalidOperationException();
    }
}
