// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Data.Common
{
    [Serializable] // MDAC 83147
    internal sealed class NameValuePair
    {
        private readonly string _name;
        private readonly string _value;
        [OptionalField(VersionAdded = 2)]
        private readonly int _length;
        private NameValuePair _next;

        internal NameValuePair(string name, string value, int length)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(name), "empty keyname");
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
