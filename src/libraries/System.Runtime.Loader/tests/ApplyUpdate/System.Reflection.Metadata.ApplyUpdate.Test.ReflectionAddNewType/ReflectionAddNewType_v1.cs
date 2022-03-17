// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType;

public interface IExistingInterface {
    public string ItfMethod(int i);
}

public struct QExistingStruct
{
}

public enum FExistingEnum {
    One, Two
}

public class ZExistingClass
{
    public class PreviousNestedClass {
        public static DateTime Now; // make the linker happy
        public static ICloneable C;
        public event EventHandler<string> E;
        public void R() { E(this,"123"); }
    }
    public class NewNestedClass {};


    public string NewMethod (string s, int i) => s + i.ToString();

    // Mono doesn't support instance fields yet
#if false
    public int NewField;
#endif

    public static DateTime NewStaticField;

    public static double NewProp { get; set; }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple=true, Inherited=false)]
public class CustomNoteAttribute : Attribute {
    public CustomNoteAttribute(string note) {Note = note;}

    public string Note;
}

[CustomNote("123")]
public class NewToplevelClass : IExistingInterface, ICloneable {
    public string ItfMethod(int i) {
        return i.ToString();
    }

    [CustomNote("abcd")]
    public void SomeMethod(int x) {}

    public virtual object Clone() {
        return new NewToplevelClass();
    }

    public class AlsoNested { }

    [CustomNote("hijkl")]
    public float NewProp {get; set;}

    public byte[] OtherNewProp {get; set;}

    public event EventHandler<string> NewEvent;
    public event EventHandler<byte> OtherNewEvent;
}

public class NewGenericClass<T> : NewToplevelClass {
    public override object Clone() {
        return new NewGenericClass<T>();
    }
}

public struct NewToplevelStruct  {
}

public interface INewInterface : IExistingInterface {
    public int AddedItfMethod (string s);
}

public enum NewEnum {
    Red, Yellow, Green
}
