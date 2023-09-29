// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddStaticField
    {
        public AddStaticField () {
        }

        public string GetField => s_field2;

        private static string s_field;

        private static string s_field2;

        public void TestMethod () {
            s_field = "spqr";
            s_field2 = "4567";
        }

    }
    public class AddStaticField2
    {
        private static int A {get; set;}
        public static int Test()
        {
            A = 11;
            return A + A;
        }
    }
}
