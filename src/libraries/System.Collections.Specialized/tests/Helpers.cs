// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized.Tests
{
    public static class Helpers
    {
        public static MyNameObjectCollection CreateNameObjectCollection(int count)
        {
            MyNameObjectCollection nameObjectCollection = new MyNameObjectCollection();

            for (int i = 0; i < count; i++)
            {
                nameObjectCollection.Add("Name_" + i, new Foo("Value_" + i));
            }

            return nameObjectCollection;
        }

        public static NameValueCollection CreateNameValueCollection(int count, int start = 0)
        {
            NameValueCollection nameValueCollection = new NameValueCollection();

            for (int i = start; i < start + count; i++)
            {
                nameValueCollection.Add("Name_" + i, "Value_" + i);
            }

            return nameValueCollection;
        }

        public static StringDictionary CreateStringDictionary(int count)
        {
            StringDictionary stringDictionary = new StringDictionary();

            for (int i = 0; i < count; i++)
            {
                stringDictionary.Add("Key_" + i, "Value_" + i);
            }

            return stringDictionary;
        }

        public static HybridDictionary CreateHybridDictionary(int count, bool caseInsensitive = false)
        {
            HybridDictionary hybridDictionary = new HybridDictionary(caseInsensitive);
            
            for (int i = 0; i < count; i++)
            {
                hybridDictionary.Add("Key_" + i, "Value_" + i);
            }

            return hybridDictionary;
        }
    }
}
