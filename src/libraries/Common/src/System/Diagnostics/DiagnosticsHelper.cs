// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal static class DiagnosticsHelper
    {
        internal static bool CompareTags(IEnumerable<KeyValuePair<string, object?>>? tags1, IEnumerable<KeyValuePair<string, object?>>? tags2)
        {
            if (tags1 == tags2)
            {
                return true;
            }

            if (tags1 is null || tags2 is null)
            {
                return false;
            }

            if (tags1 is ICollection<KeyValuePair<string, object?>> firstCol && tags2 is ICollection<KeyValuePair<string, object?>> secondCol)
            {
                int count = firstCol.Count;
                if (count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<KeyValuePair<string, object?>> firstList && secondCol is IList<KeyValuePair<string, object?>> secondList)
                {
                    for (int i = 0; i < count; i++)
                    {
                        KeyValuePair<string, object?> pair1 = firstList[i];
                        KeyValuePair<string, object?> pair2 = secondList[i];
                        if (pair1.Key != pair2.Key || !object.Equals(pair1.Value, pair2.Value))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using (IEnumerator<KeyValuePair<string, object?>> e1 = tags1.GetEnumerator())
            using (IEnumerator<KeyValuePair<string, object?>> e2 = tags2.GetEnumerator())
            {
                while (e1.MoveNext())
                {
                    KeyValuePair<string, object?> pair1 = e1.Current;
                    if (!e2.MoveNext())
                    {
                        return false;
                    }

                    KeyValuePair<string, object?> pair2 = e2.Current;
                    if (pair1.Key != pair2.Key || !object.Equals(pair1.Value, pair2.Value))
                    {
                        return false;
                    }
                }

                return !e2.MoveNext();
            }
        }
    }
}
