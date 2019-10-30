// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
                s_toServerStringFunc = (Func<Cookie, string>)typeof(Cookie).GetMethod("ToServerString", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<Cookie, string>));
            }

            Debug.Assert(s_toServerStringFunc != null, "Reflection failed for Cookie.ToServerString().");
            return s_toServerStringFunc(cookie);
        }

        private static Func<Cookie, Cookie> s_cloneFunc;

        public static Cookie Clone(this Cookie cookie)
        {
            if (s_cloneFunc == null)
            {
                s_cloneFunc = (Func<Cookie, Cookie>)typeof(Cookie).GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<Cookie, Cookie>));
            }

            Debug.Assert(s_cloneFunc != null, "Reflection failed for Cookie.Clone().");
            return s_cloneFunc(cookie);
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
                s_getVariantFunc = (Func<Cookie, CookieVariant>)typeof(Cookie).GetProperty("Variant", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true).CreateDelegate(typeof(Func<Cookie, CookieVariant>));
            }

            Debug.Assert(s_getVariantFunc != null, "Reflection failed for Cookie.Variant.");
            CookieVariant variant = s_getVariantFunc(cookie);

            return variant == CookieVariant.Rfc2965;
        }
    }

    internal static class CookieCollectionExtensions
    {
        private static Func<CookieCollection, Cookie, bool, int> s_internalAddFunc;

        public static int InternalAdd(this CookieCollection cookieCollection, Cookie cookie, bool isStrict)
        {
            if (s_internalAddFunc == null)
            {
                s_internalAddFunc = (Func<CookieCollection, Cookie, bool, int>)typeof(CookieCollection).GetMethod("InternalAdd", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate(typeof(Func<CookieCollection, Cookie, bool, int>));
            }

            Debug.Assert(s_internalAddFunc != null, "Reflection failed for CookieCollection.InternalAdd().");
            return s_internalAddFunc(cookieCollection, cookie, isStrict);
        }
    }
}
