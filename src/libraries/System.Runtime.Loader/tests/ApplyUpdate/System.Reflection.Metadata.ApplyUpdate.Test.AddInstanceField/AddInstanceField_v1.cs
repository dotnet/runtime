// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddInstanceField
    {
        public AddInstanceField () {
            _doubleField2 = -5.5e12;
            _stringField2 = "New Initial Value";
        }

        public string GetStringField => _stringField2;
        public double GetDoubleField => _doubleField2;

        private string _stringField;
        private string _stringField2;
        private double _doubleField;
        private double _doubleField2;

        public void TestMethod () {
            _stringField = "spqr";
            _stringField2 = "4567";
            _doubleField = 2.71828;
            _doubleField2 = 0.707106;
        }

    }
}
