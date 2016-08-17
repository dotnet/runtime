// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Monitoring
{
    public class MonitorMetrics
    {
        private string _name;
        private string _strValue;
        private string _unit;
        private bool _isPrimary;
        private bool _isHigherBetter;
        private double _dblValue;
        private long _lngValue;
        private char _valueType; // D=double, L=long, S=String

        public MonitorMetrics(string name, string value, string unit, bool HigherIsBetter, bool Primary)
        {
            _name = name;
            _strValue = value;
            _unit = unit;
            _valueType = 'S';
            _isHigherBetter = HigherIsBetter;
            _isPrimary = Primary;
        }

        public MonitorMetrics(string name, double value, string unit, bool HigherIsBetter, bool Primary)
        {
            _name = name;
            _dblValue = value;
            _unit = unit;
            _valueType = 'D';
            _isHigherBetter = HigherIsBetter;
            _isPrimary = Primary;
        }

        public MonitorMetrics(string name, long value, string unit, bool HigherIsBetter, bool Primary)
        {
            _name = name;
            _lngValue = value;
            _unit = unit;
            _valueType = 'L';
            _isHigherBetter = HigherIsBetter;
            _isPrimary = Primary;
        }

        public string GetName()
        {
            return _name;
        }

        public string GetUnit()
        {
            return _unit;
        }

        public bool GetPrimary()
        {
            return _isPrimary;
        }

        public bool GetHigherIsBetter()
        {
            return _isHigherBetter;
        }

        public char GetValueType()
        {
            return _valueType;
        }

        public string GetStringValue()
        {
            if (_valueType == 'S')
                return _strValue;
            throw new Exception("Value is not a string");
        }

        public double GetDoubleValue()
        {
            if (_valueType == 'D')
                return _dblValue;
            throw new Exception("Value is not a double");
        }

        public long GetLongValue()
        {
            if (_valueType == 'L')
                return _lngValue;
            throw new Exception("Value is not a long");
        }
    }
}
