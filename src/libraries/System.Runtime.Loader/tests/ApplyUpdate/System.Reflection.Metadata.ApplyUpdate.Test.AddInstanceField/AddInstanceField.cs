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

        private int[] _intArrayFieldWithInit = new[] { 2, 4, 6, 8, 10, 12 };

        public void TestMethod () {
            _stringField = "abcd";
            _doubleField = 3.14159;
        }

        public int GetIntArrayLength() => _intArrayFieldWithInit?.Length ?? -1;
        public int GetIntArrayElt(int i) => _intArrayFieldWithInit[i];

        public void IncRefDouble (ref double d)
        {
            d += 1.0;
        }

        public string GetStringProp => string.Empty;

        public event EventHandler<double> ExistingEvent;

        public double Accumulator;

        private void AccumHandler (object sender, double value) => Accumulator += value;

        public double FireEvents() {
            Accumulator = 0.0;
            ExistingEvent += AccumHandler;
            ExistingEvent(this, 123.0);
            ExistingEvent -= AccumHandler;

            return Accumulator;
        }

        public DateTime GetDateTime() => default(DateTime);
    }
}
