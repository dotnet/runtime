// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.Runtime.TypeLoader;

namespace Internal.TypeSystem
{
    // Includes functionality for runtime type loading
    public partial class DefType
    {
        internal static readonly LayoutInt MaximumAlignmentPossible = new LayoutInt(8);

        public virtual bool HasNativeLayout
        {
            get
            {
                // Attempt to compute the template, if there isn't one, then there isn't native layout
                return ComputeTemplate(false) != null;
            }
        }
    }
}
