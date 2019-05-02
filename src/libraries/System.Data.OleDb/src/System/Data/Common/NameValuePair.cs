// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Data.Common
{
    internal sealed class NameValuePair
    {
        readonly private string _name;
        readonly private string _value;
        readonly private int _length;
        private NameValuePair _next;

        internal NameValuePair(string name, string value, int length)
        {
            Debug.Assert(!string.IsNullOrEmpty(name), "empty keyname");
            _name = name;
            _value = value;
            _length = length;
        }

        internal int Length
        {
            get
            {
                Debug.Assert(0 < _length, "NameValuePair zero Length usage");
                return _length;
            }
        }

        internal string Name => _name;
        internal string Value => _value;

        internal NameValuePair Next
        {
            get { return _next; }
            set
            {
                if ((null != _next) || (null == value))
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.NameValuePairNext);
                }
                _next = value;
            }
        }
    }
}
