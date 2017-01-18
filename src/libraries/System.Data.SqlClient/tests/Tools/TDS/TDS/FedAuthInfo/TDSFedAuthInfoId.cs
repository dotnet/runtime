// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.TDS.Authentication
{
    public enum TDSFedAuthInfoId
    {
        /// <summary>
        /// STS URL as Token Endpoint
        /// </summary>
        STSURL = 0x01,

        /// <summary>
        /// Service Principal Name
        /// </summary>
        SPN = 0x02,

        /// <summary>
        /// Invalid InfoId
        /// </summary>
        Invalid = 0xEE
    }
}
