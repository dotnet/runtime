// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Data.Common
{
    internal sealed class NameValuePair
    {
        private readonly string _name;
        private readonly string? _value;
        private readonly int _length;
        private NameValuePair? _next;

        internal NameValuePair(string name, string? value, int length)
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
                Debug.Assert(_length > 0, "NameValuePair zero Length usage");
                return _length;
            }
        }

        internal string Name => _name;
        internal string? Value => _value;

        internal NameValuePair? Next
        {
            get { return _next; }
            set
            {
                if ((_next != null) || (value == null))
                {
                    throw ADP.InternalError(ADP.InternalErrorCode.NameValuePairNext);
                }
                _next = value;
            }
        }
    }
}
