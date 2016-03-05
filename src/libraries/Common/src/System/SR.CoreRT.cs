// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    // This is a placeholder for resources access implementation for corert. 
    // At some point we’ll have the real implementation which expect to be shared between 
    // System.IO, System.Runtime.Extensions and System.Resources.ResourceManager.   
    
    internal static partial class SR
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string GetResourceString(string resourceKey)
        {
            return resourceKey;
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            return defaultString != null ? defaultString : resourceKey;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1)
        {
            return string.Format(resourceFormat, p1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1, object p2)
        {
            return string.Format(resourceFormat, p1, p2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            return string.Format(resourceFormat, p1, p2, p3);
        }
    }
}
