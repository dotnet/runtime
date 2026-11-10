// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace SharedTypes.ComInterfaces
{
    [GeneratedComInterface]
    [Guid(IID)]
    internal partial interface IDimMembers
    {
        public const string IID = "BCFE6CEA-9CE3-48B0-A19F-EC73EE9D3FA1";

        // ABI methods that flow through the COM vtable.
        double ReadValue();
        void WriteValue(double value);
        int ReadCount();
        void WriteSink(int value);

        // Default-implemented wrapper property: no vtable slot, lights up
        // property-style access on RCWs and CCWs by forwarding to the ABI
        // accessors above.
        double Value
        {
            get => ReadValue();
            set => WriteValue(value);
        }

        int CountProperty => ReadCount();

        int SinkProperty
        {
            set => WriteSink(value);
        }

        // Default-implemented helper method (no vtable slot).
        int DoubleIt(int value) => value * 2;
    }

    [GeneratedComClass]
    internal partial class DimMembers : IDimMembers
    {
        private double _value;
        private int _writeOnlyIntSink;

        public double ReadValue() => _value;

        public void WriteValue(double value) => _value = value;

        public int ReadCount() => 111;

        public void WriteSink(int value) => _writeOnlyIntSink = value;

        // Non-interface accessor so the WriteSink side effect is observable from tests.
        internal int WriteOnlyIntSink => _writeOnlyIntSink;
    }
}
