// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

namespace System.DirectoryServices.Protocols
{
    // Declared as partial in order to be able to set the different StructLayout
    // attributes in the Windows and Linux specific files.
    // This is a layout-controlled struct, do not alter property ordering.
    internal partial struct SortKeyInterop
    {
        public SortKeyInterop(SortKey sortKey)
        {
            if (sortKey == null)
                throw new ArgumentNullException(nameof(sortKey));

            AttributeName = sortKey.AttributeName;
            MatchingRule = sortKey.MatchingRule;
            ReverseOrder = sortKey.ReverseOrder;
        }

        internal string AttributeName { get; set; }

        internal string MatchingRule { get; set; }

        internal bool ReverseOrder { get; set; }
    }
}
