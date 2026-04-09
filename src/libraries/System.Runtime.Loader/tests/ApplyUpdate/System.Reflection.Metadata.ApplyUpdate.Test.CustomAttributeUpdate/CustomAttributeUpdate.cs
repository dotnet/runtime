// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    [AttributeUsage (AttributeTargets.Method, AllowMultiple=true)]
    public class MyAttribute : Attribute
    {
        public MyAttribute (string stringValue) { StringValue = stringValue; }

        public MyAttribute (Type typeValue) { TypeValue = typeValue; }

        public MyAttribute (int x) { IntValue = x; }

        public string StringValue { get; set; }
        public Type TypeValue {get; set; }
        public int IntValue {get; set; }
    }

    public class ClassWithCustomAttributeUpdates
    {
        [MyAttribute ("abcd")]
        public static string Method1 () => null;

        [MyAttribute (typeof(Exception))]
        public static string Method2 () => null;

        [MyAttribute (42, StringValue = "hijkl", TypeValue = typeof(Type))]
        [MyAttribute (17, StringValue = "", TypeValue = typeof(object))]
        public static string Method3 () => null;

    }
}
