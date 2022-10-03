// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddNestedClass
    {
        public static Action<string> X; // make the linker happy
        public static Delegate Y;
        public event Action<string> Evt;
        public void R () { Evt ("123"); }
        public AddNestedClass()
        {
        }

        public string TestMethod()
        {
            return "123";
        }

    }
}
