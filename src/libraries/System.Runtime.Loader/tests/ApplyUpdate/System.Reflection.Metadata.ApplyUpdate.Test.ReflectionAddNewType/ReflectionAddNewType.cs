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
}

[AttributeUsage(AttributeTargets.All, AllowMultiple=true, Inherited=false)]
public class CustomNoteAttribute : Attribute {
    public CustomNoteAttribute(string note) {Note = note;}

    public string Note;
}
