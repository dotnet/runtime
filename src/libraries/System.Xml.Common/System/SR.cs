// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System
{
    internal class SR : global::Resources.Strings
    {
        private SR()
        {
        }

        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                return String.Format(resourceFormat, args);
            }

            return resourceFormat;
        }

        internal static string Format(string resourceFormat, object p1)
        {
            return String.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object p1, object p2)
        {
            return String.Format(resourceFormat, p1, p2);
        }

        internal static string Format(string resourceFormat, object p1, object p2, object p3)
        {
            return String.Format(resourceFormat, p1, p2, p3);
        }
    }
}
