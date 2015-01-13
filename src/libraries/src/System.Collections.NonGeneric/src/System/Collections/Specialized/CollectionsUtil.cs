// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
** Class: CollectionsUtil
**
** Purpose: Creates collections that ignore the case in strings.
**
=============================================================================*/

using System.Collections;

namespace System.Collections.Specialized
{
    public class CollectionsUtil
    {
        public static Hashtable CreateCaseInsensitiveHashtable()
        {
            return new Hashtable(StringComparer.CurrentCultureIgnoreCase);
        }

        public static Hashtable CreateCaseInsensitiveHashtable(int capacity)
        {
            return new Hashtable(capacity, StringComparer.CurrentCultureIgnoreCase);
        }

        public static Hashtable CreateCaseInsensitiveHashtable(IDictionary d)
        {
            return new Hashtable(d, StringComparer.CurrentCultureIgnoreCase);
        }

        public static SortedList CreateCaseInsensitiveSortedList()
        {
            return new SortedList(CaseInsensitiveComparer.Default);
        }
    }
}
