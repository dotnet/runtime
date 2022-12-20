// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Build.Tasks
{
    public enum TpnSectionHeaderFormat
    {
        /// <summary>
        /// {blank line}
        /// {3+ section separator chars}
        /// {blank line}
        /// {name}
        /// </summary>
        Separated,

        /// <summary>
        /// {blank line}
        /// {name (multiline)}
        /// {3+ section separator chars}
        /// {blank line}
        /// </summary>
        Underlined,

        /// <summary>
        /// {blank line}
        /// {number}.{tab}{name}
        /// {blank line}
        /// </summary>
        Numbered
    }
}
