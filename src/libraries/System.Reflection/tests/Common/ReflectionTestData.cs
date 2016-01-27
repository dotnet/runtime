// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

//
// This is the "Type Library" we are going to reflect on
//

namespace System.Reflection.CustomAttributesTests.Data
{
    public enum MyColorEnum
    {
        RED = 1,
        BLUE = 2,
        GREEN = 3
    }

    public class Util
    {
        public static string ObjectToString(Object o)
        {
            string s = string.Empty;
            if (o != null)
            {
                if (o is Array)
                {
                    Array a = (Array)o;
                    for (int i = 0; i < a.Length; i += 1)
                    {
                        if (i > 0)
                        {
                            s = s + ", ";
                        }
                        if (a.GetValue(i) is Array)
                            s = s + Util.ObjectToString((Array)a.GetValue(i));
                        else
                            s = s + a.GetValue(i).ToString();
                    }
                }
                else
                    s = s + o.ToString();
            }
            return s;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Attr : Attribute
    {
        public Attr()
        {
        }

        public Attr(int i)
        {
            value = i;
            sValue = null;
        }

        public Attr(int i, string s)
        {
            value = i;
            sValue = s;
        }

        public override string ToString()
        {
            return "Attr ToString : " + value.ToString() + " " + sValue;
        }

        public string name;
        public int value;
        public string sValue;
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Int32Attr : Attribute
    {
        public Int32Attr(int i)
        {
            value = i;
            arrayValue = null;
        }

        public Int32Attr(int i, int[] a)
        {
            value = i;
            arrayValue = a;
        }

        public string name;
        public readonly int value;
        public int field;
        public readonly int[] arrayValue;
        public int[] arrayField;
        private int _property = 0;
        public int property { get { return _property; } set { _property = value; } }
        private int[] _arrayProperty;
        public int[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                    + "; value : " + Util.ObjectToString(value)
                    + "; field : " + Util.ObjectToString(field)
                    + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class Int64Attr : Attribute
    {
        public Int64Attr(long l)
        {
            value = l;
            arrayValue = null;
        }

        public Int64Attr(long l, long[] a)
        {
            value = l;
            arrayValue = a;
        }

        public string name;
        public readonly long value;
        public long field;
        public readonly long[] arrayValue;
        public long[] arrayField;
        private long _property = 0;
        public long property { get { return _property; } set { _property = value; } }
        private long[] _arrayProperty;
        public long[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                    + "; value : " + Util.ObjectToString(value)
                    + "; field : " + Util.ObjectToString(field)
                    + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class EnumAttr : Attribute
    {
        public EnumAttr(MyColorEnum e)
        {
            value = e;
            arrayValue = null;
        }

        public EnumAttr(MyColorEnum e, MyColorEnum[] a)
        {
            value = e;
            arrayValue = a;
        }

        public string name;
        public readonly MyColorEnum value;
        public MyColorEnum field;
        public readonly MyColorEnum[] arrayValue;
        public MyColorEnum[] arrayField;
        private MyColorEnum _property = 0;
        public MyColorEnum property { get { return _property; } set { _property = value; } }
        private MyColorEnum[] _arrayProperty;
        public MyColorEnum[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                    + "; value : " + Util.ObjectToString(value)
                    + "; field : " + Util.ObjectToString(field)
                    + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class StringAttr : Attribute
    {
        public StringAttr(string s)
        {
            value = s;
            arrayValue = null;
        }

        public StringAttr(string s, string[] a)
        {
            value = s;
            arrayValue = a;
        }

        public string name;
        public readonly string value;
        public string field;
        public readonly string[] arrayValue;
        public string[] arrayField;
        private string _property;
        public string property { get { return _property; } set { _property = value; } }
        private string[] _arrayProperty;
        public string[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                    + "; value : " + Util.ObjectToString(value)
                    + "; field : " + Util.ObjectToString(field)
                    + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class TypeAttr : Attribute
    {
        public TypeAttr(Type t)
        {
            value = t;
            arrayValue = null;
        }

        public TypeAttr(Type t, Type[] a)
        {
            value = t;
            arrayValue = a;
        }


        public string name;
        public readonly Type value;
        public Type field;
        public readonly Type[] arrayValue;
        public Type[] arrayField;
        private Type _property;
        public Type property { get { return _property; } set { _property = value; } }
        private Type[] _arrayProperty;
        public Type[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                                        + "; value : " + Util.ObjectToString(value)
                                        + "; field : " + Util.ObjectToString(field)
                                        + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class ObjectAttr : Attribute
    {
        public ObjectAttr(Object v)
        {
            value = v;
            arrayValue = null;
        }

        public ObjectAttr(Object v, Object[] a)
        {
            value = v;
            arrayValue = a;
        }

        public string name;
        public readonly Object value;
        public Object field;
        public readonly Object[] arrayValue;
        public Object[] arrayField;
        private Object _property = 0;
        public Object property { get { return _property; } set { _property = value; } }
        private Object[] _arrayProperty;
        public Object[] arrayProperty { get { return _arrayProperty; } set { _arrayProperty = value; } }

        public override string ToString()
        {
            return GetType().ToString() + " - name : " + name
                                        + "; value : " + Util.ObjectToString(value)
                                        + "; field : " + Util.ObjectToString(field)
                                        + "; property : " + Util.ObjectToString(property)
                                        + "; array value : " + Util.ObjectToString(arrayValue)
                                        + "; array field : " + Util.ObjectToString(arrayField)
                                        + "; array property : " + Util.ObjectToString(arrayProperty);
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class NullAttr : Attribute
    {
        public NullAttr(string s, Type t, int[] a)
        {
        }

        public NullAttr(String s)
        {
        }

        public string name;

        public string stringField;
        public Type typeField;
        public int[] arrayField;
        public string stringProperty { get { return null; } set { } }
        public Type typeProperty { get { return null; } set { } }
        public int[] arrayProperty { get { return null; } set { } }
    }
}

