// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Xml
{
    internal static class XmlNameHelper
    {
        public static int GetHashCode(string name)
        {
            int hashCode = 0;
            if (name != null)
            {
                for (int i = name.Length - 1; i >= 0; i--)
                {
                    char ch = name[i];
                    if (ch == ':') break;
                    hashCode += (hashCode << 7) ^ ch;
                }
                hashCode -= hashCode >> 17;
                hashCode -= hashCode >> 11;
                hashCode -= hashCode >> 5;
            }
            return hashCode;
        }
    }
}
