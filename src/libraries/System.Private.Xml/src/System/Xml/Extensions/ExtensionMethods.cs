// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;


namespace System.Xml.Extensions
{
    internal static class ExtensionMethods
    {
        internal static Uri ToUri(string s)
        {
            if (s != null && s.Length > 0)
            { //string.Empty is a valid uri but not "   "
                s = s.Trim(new char[] { ' ', '\t', '\n', '\r' });
                if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
                {
                    throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
                }
            }
            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
            }

            return uri;
        }
    }
}
