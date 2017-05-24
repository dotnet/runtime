// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        public string[] Verbs
        {
            get
            {
                string extension = Path.GetExtension(FileName);
                if (string.IsNullOrEmpty(extension))
                    return Array.Empty<string>();

                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (key == null)
                        return Array.Empty<string>();

                    string value = key.GetValue(string.Empty) as string;
                    if (string.IsNullOrEmpty(value))
                        return Array.Empty<string>();

                    using (RegistryKey subKey = Registry.ClassesRoot.OpenSubKey(value + "\\shell"))
                    {
                        if (subKey == null)
                            return Array.Empty<string>();

                        string[] names = subKey.GetSubKeyNames();
                        List<string> verbs = new List<string>();
                        foreach (string name in names)
                        {
                            if (!string.Equals(name, "new", StringComparison.OrdinalIgnoreCase))
                            {
                                verbs.Add(name);
                            }
                        }
                        return verbs.ToArray();
                    }
                }
            }
        }

        public bool UseShellExecute { get; set; }
    }
}
