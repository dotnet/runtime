// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration.Internal;
using System.Xml;

namespace System.Configuration
{
    internal sealed class PropertySourceInfo
    {
        internal PropertySourceInfo(XmlReader reader)
        {
            FileName = GetFilename(reader);
            LineNumber = GetLineNumber(reader);
        }

        internal string FileName { get; }

        internal int LineNumber { get; }

        private static string GetFilename(XmlReader reader)
        {
            IConfigErrorInfo err = reader as IConfigErrorInfo;

            return err != null ? err.Filename : "";
        }

        private static int GetLineNumber(XmlReader reader)
        {
            IConfigErrorInfo err = reader as IConfigErrorInfo;

            return err?.LineNumber ?? 0;
        }
    }
}
