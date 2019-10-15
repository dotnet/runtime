// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Tests
{
    public enum PublicEnum
    {
        Case1 = 1,
        Case2 = 2,
        Case3 = 3
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Attr : Attribute
    {
        public Attr(int i)
        {
            value = i;
        }

        public override string ToString() => $"{value}, {name}";

        public string name;
        public int value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Int32Attr : Attribute
    {
        public Int32Attr(int i)
        {
            value = i;
        }

        public string name;
        public readonly int value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Int64Attr : Attribute
    {
        public Int64Attr(long l)
        {
            value = l;
        }

        public string name;
        public readonly long value;

        public override string ToString() => $"{value}, {name}";
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class EnumAttr : Attribute
    {
        public EnumAttr(PublicEnum e)
        {
            value = e;
        }

        public string name;
        public readonly PublicEnum value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class StringAttr : Attribute
    {
        public StringAttr(string s)
        {
            value = s;
        }

        public string name;
        public readonly string value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class TypeAttr : Attribute
    {
        public TypeAttr(Type t)
        {
            value = t;
        }

        public string name;
        public readonly Type value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class ObjectAttr : Attribute
    {
        public ObjectAttr(object v)
        {
            value = v;
        }

        public string name;
        public readonly object value;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class NullAttr : Attribute { }

    public class Helpers
    {
        public static Assembly ExecutingAssembly => typeof(Helpers).GetTypeInfo().Assembly;
    }
}
