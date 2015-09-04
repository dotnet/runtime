// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Resources;
using System.Runtime.CompilerServices;

namespace System
{
    internal partial class SR
    {
        private static ResourceManager s_resourceManager;

        private static ResourceManager ResourceManager
        {
            get
            {
                if (SR.s_resourceManager == null)
                {
                    SR.s_resourceManager = new ResourceManager(SR.ResourceType);
                }

                return SR.s_resourceManager;
            }
        }

        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format. 
        // by default it returns false.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UsingResourceKeys()
        {
            return false;
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = null;
            try { resourceString = ResourceManager.GetString(resourceKey); }
            catch (MissingManifestResourceException) { }

            if (defaultString != null && resourceKey.Equals(resourceString, StringComparison.Ordinal))
            {
                return defaultString;
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + String.Join(", ", args);
                }

                return String.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1);
            }

            return String.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1, p2);
            }

            return String.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return String.Join(", ", resourceFormat, p1, p2, p3);
            }

            return String.Format(resourceFormat, p1, p2, p3);
        }
    }
}
