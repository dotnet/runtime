// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace System.DirectoryServices.Protocols
{
    internal partial struct SortKeyInterop
    {
        private string _name;
        private string _rule;
        private bool _order;

        public SortKeyInterop(SortKey sortKey)
        {
            if (sortKey == null)
                throw new ArgumentNullException(nameof(sortKey));

            _name = sortKey.AttributeName;
            _rule = sortKey.MatchingRule;
            _order = sortKey.ReverseOrder;
        }

        public string AttributeName
        {
            get => _name;
            set => _name = value;
        }

        public string MatchingRule
        {
            get => _rule;
            set => _rule = value;
        }

        public bool ReverseOrder
        {
            get => _order;
            set => _order = value;
        }
    }
}
