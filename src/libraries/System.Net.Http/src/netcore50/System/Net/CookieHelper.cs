// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Internal;
using InternalCookieException = System.Net.Internal.CookieException;

namespace System.Net
{
    internal static class CookieHelper 
    {
        internal static IEnumerable<string> GetCookiesFromHeader(string setCookieHeader) 
        {
            List<string> cookieStrings = new List<string>();
            
            try
            {
                CookieParser parser = new CookieParser(setCookieHeader);
                string cookieString;

                while ((cookieString = parser.GetString()) != null)
                {
                    cookieStrings.Add(cookieString);
                }
            }
            catch (InternalCookieException)
            {
                // TODO: We should log this.  But there isn't much we can do about it other
                // than to drop the rest of the cookies.
            }
            
            return cookieStrings;
        }
    }
}
