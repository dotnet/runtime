// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.CompilerServices
{
    internal static class DeveloperExperienceState
    {
        public static bool DeveloperExperienceModeEnabled
        {
            get
            {
                return true;  // ILC will rewrite to this "return false" if run with "/buildType:ret"
            }
        }
    }
}
