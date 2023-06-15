// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;


namespace System.Reflection.Metadata.ApplyUpdate.Test
{
    public class AddInstanceField
    {
        public AddInstanceField () {
            _doubleField2 = 5.5;
            _stringField2 = "New Initial Value";
            NewStructField = new NewStruct {
                D = -1985.0,
                O = new int[2] { 15, 17 },
            };
            // a little bit ldflda testing
            IncRefDouble (ref NewStructField.D);
            IncRefDouble (ref _doubleField2);

            AddedStringAutoProp = "abcd";

            AddedEvent += MyHandler;

            void MyHandler (object sender, double data) {
            }
        }

        public void IncRefDouble (ref double d)
        {
            d += 1.0;
        }

        public string GetStringField => _stringField2;
        public double GetDoubleField => _doubleField2;

        private string _stringField;
        private string _stringField2;
        private double _doubleField;
        private double _doubleField2;

        private int[] _intArrayFieldWithInit = new[] { 2, 4, 6, 8, 10, 12 };
        private int[] _intArrayFieldWithInit2 = new[] { 1, 3, 5, 7, 9, 11 };

        public void TestMethod () {
            _stringField = "spqr";
            _stringField2 = "4567";
            _doubleField = 2.71828;
            _doubleField2 = 0.707106;
            AddedStringAutoProp = AddedStringAutoProp + "Test";
        }

        public int GetIntArrayLength() => _intArrayFieldWithInit2?.Length ?? -1;
        public int GetIntArrayElt(int i) => _intArrayFieldWithInit2[i];

        public struct NewStruct
        {
            public double D;
            public object O;
        }

        public NewStruct NewStructField;

        public string GetStringProp => AddedStringAutoProp;

        public string AddedStringAutoProp { get; set; }

        public event EventHandler<double> ExistingEvent;
        public event EventHandler<double> AddedEvent;

        public double Accumulator;

        private void AccumHandler (object sender, double value) => Accumulator += value;

        public double FireEvents() {
            Accumulator = 0.0;
            ExistingEvent += AccumHandler;
            ExistingEvent(this, 123.0);
            ExistingEvent -= AccumHandler;

            AddedEvent += AccumHandler;
            AddedEvent(this, 123.0);
            AddedEvent -= AccumHandler;

            return Accumulator;
        }

        public double AddedFirstProp {get => 0.0; set { Console.WriteLine (value+value); } }
        public short AddedSecondProp {get; set; }
    }
}
