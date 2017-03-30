// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    public class FillErrorEventArgs : EventArgs
    {
        private bool _continueFlag;
        private DataTable _dataTable;
        private Exception _errors;
        private object[] _values;

        public FillErrorEventArgs(DataTable dataTable, object[] values)
        {
            _dataTable = dataTable;
            _values = values;
            if (null == _values)
            {
                _values = Array.Empty<object>();
            }
        }

        public bool Continue
        {
            get { return _continueFlag; }
            set { _continueFlag = value; }
        }

        public DataTable DataTable => _dataTable;

        public Exception Errors
        {
            get { return _errors; }
            set { _errors = value; }
        }

        public object[] Values
        {
            get
            {
                object[] copy = new object[_values.Length];
                for (int i = 0; i < _values.Length; ++i)
                {
                    copy[i] = _values[i];
                }
                return copy;
            }
        }
    }
}
