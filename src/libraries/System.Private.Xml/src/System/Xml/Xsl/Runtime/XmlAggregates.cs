// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Computes aggregates over a sequence of Int32 values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct Int32Aggregator
    {
        private int _result;
        private int _cnt;

        public void Create()
        {
            _cnt = 0;
        }

        public void Sum(int value)
        {
            if (_cnt == 0)
            {
                _result = value;
                _cnt = 1;
            }
            else
            {
                _result += value;
            }
        }

        public void Average(int value)
        {
            if (_cnt == 0)
                _result = value;
            else
                _result += value;

            _cnt++;
        }

        public void Minimum(int value)
        {
            if (_cnt == 0 || value < _result)
                _result = value;

            _cnt = 1;
        }

        public void Maximum(int value)
        {
            if (_cnt == 0 || value > _result)
                _result = value;

            _cnt = 1;
        }

        public int SumResult { get { return _result; } }
        public int AverageResult { get { return _result / _cnt; } }
        public int MinimumResult { get { return _result; } }
        public int MaximumResult { get { return _result; } }

        public bool IsEmpty { get { return _cnt == 0; } }
    }


    /// <summary>
    /// Computes aggregates over a sequence of Int64 values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct Int64Aggregator
    {
        private long _result;
        private int _cnt;

        public void Create()
        {
            _cnt = 0;
        }

        public void Sum(long value)
        {
            if (_cnt == 0)
            {
                _result = value;
                _cnt = 1;
            }
            else
            {
                _result += value;
            }
        }

        public void Average(long value)
        {
            if (_cnt == 0)
                _result = value;
            else
                _result += value;

            _cnt++;
        }

        public void Minimum(long value)
        {
            if (_cnt == 0 || value < _result)
                _result = value;

            _cnt = 1;
        }

        public void Maximum(long value)
        {
            if (_cnt == 0 || value > _result)
                _result = value;

            _cnt = 1;
        }

        public long SumResult { get { return _result; } }
        public long AverageResult { get { return _result / (long)_cnt; } }
        public long MinimumResult { get { return _result; } }
        public long MaximumResult { get { return _result; } }

        public bool IsEmpty { get { return _cnt == 0; } }
    }


    /// <summary>
    /// Computes aggregates over a sequence of Decimal values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct DecimalAggregator
    {
        private decimal _result;
        private int _cnt;

        public void Create()
        {
            _cnt = 0;
        }

        public void Sum(decimal value)
        {
            if (_cnt == 0)
            {
                _result = value;
                _cnt = 1;
            }
            else
            {
                _result += value;
            }
        }

        public void Average(decimal value)
        {
            if (_cnt == 0)
                _result = value;
            else
                _result += value;

            _cnt++;
        }

        public void Minimum(decimal value)
        {
            if (_cnt == 0 || value < _result)
                _result = value;

            _cnt = 1;
        }

        public void Maximum(decimal value)
        {
            if (_cnt == 0 || value > _result)
                _result = value;

            _cnt = 1;
        }

        public decimal SumResult { get { return _result; } }
        public decimal AverageResult { get { return _result / (decimal)_cnt; } }
        public decimal MinimumResult { get { return _result; } }
        public decimal MaximumResult { get { return _result; } }

        public bool IsEmpty { get { return _cnt == 0; } }
    }


    /// <summary>
    /// Computes aggregates over a sequence of Double values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct DoubleAggregator
    {
        private double _result;
        private int _cnt;

        public void Create()
        {
            _cnt = 0;
        }

        public void Sum(double value)
        {
            if (_cnt == 0)
            {
                _result = value;
                _cnt = 1;
            }
            else
            {
                _result += value;
            }
        }

        public void Average(double value)
        {
            if (_cnt == 0)
                _result = value;
            else
                _result += value;

            _cnt++;
        }

        public void Minimum(double value)
        {
            if (_cnt == 0 || value < _result || double.IsNaN(value))
                _result = value;

            _cnt = 1;
        }

        public void Maximum(double value)
        {
            if (_cnt == 0 || value > _result || double.IsNaN(value))
                _result = value;

            _cnt = 1;
        }

        public double SumResult { get { return _result; } }
        public double AverageResult { get { return _result / (double)_cnt; } }
        public double MinimumResult { get { return _result; } }
        public double MaximumResult { get { return _result; } }

        public bool IsEmpty { get { return _cnt == 0; } }
    }
}
