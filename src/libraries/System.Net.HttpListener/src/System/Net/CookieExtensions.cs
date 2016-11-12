// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Net
{
    // TODO: #13607
    internal static class CookieExtensions
    {
        private static Func<Cookie, string> s_toServerStringFunc;

        public static string ToServerString(this Cookie cookie)
        {
            if (s_toServerStringFunc == null)
            {
                s_toServerStringFunc = (Func<Cookie, string>)typeof(Cookie).GetMethod("ToServerString").CreateDelegate(typeof(Func<Cookie, string>));
            }
            return s_toServerStringFunc(cookie);
        }

        private enum CookieVariant
        {
            Unknown,
            Plain,
            Rfc2109,
            Rfc2965,
            Default = Rfc2109
        }

        private static Func<Cookie, CookieVariant> s_getVariantFunc;

        public static bool IsRfc2965Variant(this Cookie cookie)
        {
            if (s_getVariantFunc == null)
            {
                s_getVariantFunc = (Func<Cookie, CookieVariant>)typeof(Cookie).GetProperty("Variant", BindingFlags.NonPublic).GetGetMethod(true).CreateDelegate(typeof(Func<Cookie, CookieVariant>));
            }

            CookieVariant variant = s_getVariantFunc(cookie);

            return variant == CookieVariant.Rfc2965;
        }
    }
}
