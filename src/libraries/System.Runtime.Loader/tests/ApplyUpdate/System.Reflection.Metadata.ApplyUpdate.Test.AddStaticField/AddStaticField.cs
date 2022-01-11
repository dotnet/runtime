// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddStaticField
    {
        public AddStaticField () {
        }

        public string GetField => s_field;

        private static string s_field;

        public void TestMethod () {
            s_field = "abcd";
        }

    }
}
