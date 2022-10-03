// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Runtime.Loader
{
    internal partial struct LibraryNameVariation
    {
        private const string LibraryNameSuffix = ".dll";

        internal static IEnumerable<LibraryNameVariation> DetermineLibraryNameVariations(string libName, bool isRelativePath)
        {
            // This is a copy of the logic in DetermineLibNameVariations in dllimport.cpp in CoreCLR

            yield return new LibraryNameVariation(string.Empty, string.Empty);

            if (isRelativePath
                && !libName.EndsWith(".", StringComparison.OrdinalIgnoreCase)
                && !libName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                && !libName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                yield return new LibraryNameVariation(string.Empty, LibraryNameSuffix);
            }
        }
    }
}
