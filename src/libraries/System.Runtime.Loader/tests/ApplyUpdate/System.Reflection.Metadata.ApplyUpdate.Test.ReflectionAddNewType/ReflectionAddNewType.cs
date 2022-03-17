// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test.ReflectionAddNewType;

public interface IExistingInterface {
    public string ItfMethod(int i);
}

public class ZExistingClass
{
    public class PreviousNestedClass { }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple=true, Inherited=false)]
public class CustomNoteAttribute : Attribute {
    public CustomNoteAttribute(string note) {Note = note;}

    public string Note;
}
