// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Security.Policy
{
    /// <summary>
    ///     Base class from which all objects to be used as Evidence must derive
    /// </summary>
    [ComVisible(true)]
    [Serializable]
    internal abstract class EvidenceBase
    {
        protected EvidenceBase()
        {
        }
    }
}
