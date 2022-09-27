// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddInstanceField
    {
        public AddInstanceField () {
        }

        public string GetStringField => _stringField;
        public double GetDoubleField => _doubleField;

        private string _stringField;
        private double _doubleField;

        public void TestMethod () {
            _stringField = "abcd";
            _doubleField = 3.14159;
        }

    }
}
