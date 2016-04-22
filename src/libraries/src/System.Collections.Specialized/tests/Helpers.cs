// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized.Tests
{
    public static class Helpers
    {
        public static StringDictionary CreateStringDictionary(int count)
        {
            StringDictionary stringDictionary = new StringDictionary();

            for (int i = 0; i < count; i++)
            {
                stringDictionary.Add("Key_" + i, "Value_" + i);
            }

            return stringDictionary;
        }
    }
}
