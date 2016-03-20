// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    internal static partial class SR
    {
        //
        // Lower level libraries like System.Private.CoreLib, System.Private.Uri and other System.Private.* libraries.
        // cannot depend on System.Resources.ResourceManager directly, so we'll call Windows Runtime ResourceManager 
        // directly using Internal.Runtime.Augments. 
        // For other assemblies, we use System.Resources.ResourceManager to do the resources lookup. it is important to 
        // not have such assemblies depend on internal contracts as we can decide to make these assemblies portable.
        //

        private static object s_resourceMap;
        private const string MoreInfoLink = @". For more information, visit http://go.microsoft.com/fwlink/?LinkId=623485";

        private static object ResourceMap
        {
            get
            {
                return s_resourceMap = s_resourceMap ?? WinRTInterop.Callbacks.GetResourceMap(s_resourcesName);
            }
        }

        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format. 
        // by default it returns false. We overwrite the implementation of this method to return true through IL transformer 
        // when compiling ProjectN app as retail build. 
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool UsingResourceKeys()
        {
            return false;
        }

        // TODO: Resource generation tool should be modified to call this version in release build
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string GetResourceString(string resourceKey)
        {
            return WinRTInterop.Callbacks.GetResourceString(ResourceMap, resourceKey, null);
        }

        internal static string GetResourceString(string resourceKey, string defaultString)
        {
            string resourceString = GetResourceString(resourceKey);

            // if we are running on desktop, GetResourceString will just return resourceKey. so
            // in this case we'll return defaultString (if it is not null). 
            if (defaultString != null && resourceKey.Equals(resourceString))
            {
                return defaultString;
            }

            if (resourceString == null)
            {
                // It is not expected to have resourceString is null at this point.
                // this means our framework resources is missing while it is expected to be there. 
                // we have to throw on that or otherwise we'll eventually get stack overflow exception.
                // we have to use hardcode the exception message here as we cannot lookup the resources for other keys.
                // We cannot throw MissingManifestResourceException as we cannot depend on the System.Resources here.

                throw new InvalidProgramException("Unable to find resource for the key " + resourceKey + ".");
            }

            return resourceString;
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                if (UsingResourceKeys())
                {
                    return resourceFormat + string.Join(", ", args) + MoreInfoLink;
                }

                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1) + MoreInfoLink;
            }

            return string.Format(resourceFormat, p1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1, object p2)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2) + MoreInfoLink;
            }

            return string.Format(resourceFormat, p1, p2);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2, p3) + MoreInfoLink;
            }
            return string.Format(resourceFormat, p1, p2, p3);
        }
    }
}
