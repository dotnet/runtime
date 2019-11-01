// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Loader
{
    internal partial struct LibraryNameVariation
    {
        public string Prefix;
        public string Suffix;

        public LibraryNameVariation(string prefix, string suffix)
        {
            Prefix = prefix;
            Suffix = suffix;
        }
    }
}
