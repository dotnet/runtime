// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

using System;
using System.Data.Common;
using System.Diagnostics;


namespace System.Data.Common
{
    sealed internal class NameValuePair
    {
        readonly private string _name;
        readonly private string _value;
        readonly private int _length;
        private NameValuePair _next;

        internal NameValuePair(string name, string value, int length)
        {
            System.Diagnostics.Debug.Assert(!ADP.IsEmpty(name), "empty keyname");
            _name = name;
            _value = value;
            _length = length;
        }

        internal int Length
        {
            get
            {
                // this property won't exist when deserialized from Everett to Whidbey
                // it shouldn't matter for DbConnectionString/DbDataPermission
                // which should only use Length during construction
                // not deserialization or post-ctor runtime
                Debug.Assert(0 < _length, "NameValuePair zero Length usage");
                return _length;
            }
        }
        internal string Name
        {
            get
            {
                return _name;
            }
        }
        internal NameValuePair Next
        {
            get
            {
                return _next;
            }
            set
            {
                if ((null != _next) || (null == value))
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.NameValuePairNext);
                }
                _next = value;
            }
        }
        internal string Value
        {
            get
            {
                return _value;
            }
        }
    }
}
