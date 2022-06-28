// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Pgo;

public class MibcConfig
{
    public string FormatVersion = "1.0";
    public string Os;
    public string Arch;
    public string Runtime;

    public override string ToString()
    {
        string str = "";
        foreach (FieldInfo field in GetType().GetFields())
        {
            string paddedName = (field.Name + ":").PadRight(18, ' ');
            str += $"{paddedName} {field.GetValue(this)}\n";
        }
        return str;
    }
}
