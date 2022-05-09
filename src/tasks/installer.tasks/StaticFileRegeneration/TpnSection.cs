// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Build.Tasks
{
    public class TpnSection
    {
        public class ByHeaderNameComparer : EqualityComparer<TpnSection>
        {
            public override bool Equals(TpnSection x, TpnSection y) =>
                string.Equals(x.Header.Name, y.Header.Name, StringComparison.OrdinalIgnoreCase);

            public override int GetHashCode(TpnSection obj) => obj.Header.Name.GetHashCode();
        }

        public TpnSectionHeader Header { get; set; }
        public string Content { get; set; }

        public override string ToString() =>
            Header + Environment.NewLine + Environment.NewLine + Content;
    }
}
