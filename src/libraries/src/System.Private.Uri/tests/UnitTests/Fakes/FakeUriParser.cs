// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public class UriParser
    {
        internal static bool DontEnableStrictRFC3986ReservedCharacterSets
        {
            get
            {
                return false;
            }
        }

        internal static bool DontKeepUnicodeBidiFormattingCharacters
        {
            get
            {
                return false;
            }
        }
    }
}
