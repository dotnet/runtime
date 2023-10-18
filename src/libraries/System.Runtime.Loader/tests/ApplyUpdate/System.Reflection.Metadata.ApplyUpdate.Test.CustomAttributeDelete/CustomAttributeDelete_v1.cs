// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    [AttributeUsage (AttributeTargets.Method, AllowMultiple=true)]
    public class MyDeleteAttribute : Attribute
    {
        public MyDeleteAttribute (string stringValue) { StringValue = stringValue; }

        public MyDeleteAttribute (Type typeValue) { TypeValue = typeValue; }

        public MyDeleteAttribute (int x) { IntValue = x; }

        public string StringValue { get; set; }
        public Type TypeValue {get; set; }
        public int IntValue {get; set; }
    }

    public class ClassWithCustomAttributeDelete
    {
        public static string Method1 () => null;

        public static string Method2 () => null;

        [MyDeleteAttribute (17, StringValue = "Not Deleted", TypeValue = typeof(object))]
        public static string Method3 () => null;
    }
}
