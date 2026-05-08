// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace ILAssembler
{
    internal static class NameHelpers
    {
        public static (string Namespace, string Name) SplitDottedNameToNamespaceAndName(string dottedName)
        {
            int lastDotIndex = dottedName.LastIndexOf('.');

            if (lastDotIndex > 0 && dottedName[lastDotIndex - 1] == '.')
            {
                // Handle cases like "a.b..ctor".
                lastDotIndex -= 1;
            }

            return (
                lastDotIndex != -1
                    ? dottedName.Substring(0, lastDotIndex)
                    : string.Empty,
                lastDotIndex != -1
                    ? dottedName.Substring(lastDotIndex + 1)
                    : dottedName);
        }
    }
}
