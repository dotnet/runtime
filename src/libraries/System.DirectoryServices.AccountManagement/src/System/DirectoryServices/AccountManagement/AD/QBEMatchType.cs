// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.DirectoryServices.AccountManagement
{
    internal sealed class QbeMatchType
    {
        private object _value;
        private MatchType _matchType;

        internal QbeMatchType(object value, MatchType matchType)
        {
            _value = value;
            _matchType = matchType;
        }

        internal object Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        internal MatchType Match
        {
            get
            {
                return _matchType;
            }
            set
            {
                _matchType = value;
            }
        }
    }
}
