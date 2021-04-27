// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace System.Speech
{
    internal static class SR
    {
        private static ResourceManager s_resourceManager = new("ExceptionStringTable", typeof(SR).Assembly);

        internal static string Get(SRID id, params object[] args)
        {
            string text = s_resourceManager.GetString(id.ToString());
            if (string.IsNullOrEmpty(text))
            {
                text = s_resourceManager.GetString("Unavailable");
            }
            else if (args != null && args.Length != 0)
            {
                text = string.Format(text, args);
            }
            return text;
        }
    }
}
