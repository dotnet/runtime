// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Xml
{
    internal static class DiagnosticsSwitches
    {
        private static volatile BooleanSwitch? s_keepTempFiles;
        private static volatile BooleanSwitch? s_nonRecursiveTypeLoading;

        public static BooleanSwitch KeepTempFiles
        {
            get
            {
                if (s_keepTempFiles == null)
                {
                    s_keepTempFiles = new BooleanSwitch("XmlSerialization.Compilation", "Keep XmlSerialization generated (temp) files.");
                }

                return s_keepTempFiles;
            }
        }

        public static BooleanSwitch NonRecursiveTypeLoading
        {
            get
            {
                if (s_nonRecursiveTypeLoading == null)
                {
                    s_nonRecursiveTypeLoading = new BooleanSwitch("XmlSerialization.NonRecursiveTypeLoading", "Turn on non-recursive algorithm generating XmlMappings for CLR types.");
                }

                return s_nonRecursiveTypeLoading;
            }
        }
    }
}
