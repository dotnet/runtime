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
    public class NewNestedClass {};
}

public class NewToplevelClass : IExistingInterface, ICloneable {
    public string ItfMethod(int i) {
        return i.ToString();
    }

    public virtual object Clone() {
        return new NewToplevelClass();
    }

    public class AlsoNested { }
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
