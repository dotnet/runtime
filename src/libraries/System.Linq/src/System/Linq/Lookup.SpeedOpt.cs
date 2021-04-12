// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    /// <summary>Represents a collection of keys each mapped to one or more values.</summary>
    /// <remarks>A <see cref="System.Linq.Lookup{T1,T2}" /> resembles a <see cref="System.Collections.Generic.Dictionary{T1,T2}" />. The difference is that a <see cref="System.Collections.Generic.Dictionary{T1,T2}" /> maps keys to single values, whereas a <see cref="System.Linq.Lookup{T1,T2}" /> maps keys to collections of values.
    /// You can create an instance of a <see cref="System.Linq.Lookup{T1,T2}" /> by calling <see cref="O:System.Linq.Enumerable.ToLookup" /> on an object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// <format type="text/markdown"><![CDATA[
    /// > [!NOTE]
    /// >  There is no public constructor to create a new instance of a <xref:System.Linq.Lookup%602>. Additionally, <xref:System.Linq.Lookup%602> objects are immutable, that is, you cannot add or remove elements or keys from a <xref:System.Linq.Lookup%602> object after it has been created.
    /// ]]></format></remarks>
    /// <example>The following example creates a <see cref="System.Linq.Lookup{T1,T2}" /> from a collection of objects. It then enumerates the <see cref="System.Linq.Lookup{T1,T2}" /> and outputs each key and each value in the key's associated collection of values. It also demonstrates how to use the properties <see cref="O:System.Linq.Lookup{T1,T2}.Count" /> and <see cref="O:System.Linq.Lookup{T1,T2}.Item" /> and the methods <see cref="O:System.Linq.Lookup{T1,T2}.Contains" /> and <see cref="O:System.Linq.Lookup{T1,T2}.GetEnumerator" />.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Lookup/CS/lookup.cs" id="Snippet1":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Lookup/VB/Lookup.vb" id="Snippet1":::</example>
    public partial class Lookup<TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        IGrouping<TKey, TElement>[] IIListProvider<IGrouping<TKey, TElement>>.ToArray()
        {
            IGrouping<TKey, TElement>[] array = new IGrouping<TKey, TElement>[_count];
            int index = 0;
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    array[index] = g;
                    ++index;
                }
                while (g != _lastGrouping);
            }

            return array;
        }

        internal TResult[] ToArray<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            TResult[] array = new TResult[_count];
            int index = 0;
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    g.Trim();
                    array[index] = resultSelector(g._key, g._elements);
                    ++index;
                }
                while (g != _lastGrouping);
            }

            return array;
        }

        List<IGrouping<TKey, TElement>> IIListProvider<IGrouping<TKey, TElement>>.ToList()
        {
            List<IGrouping<TKey, TElement>> list = new List<IGrouping<TKey, TElement>>(_count);
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    list.Add(g);
                }
                while (g != _lastGrouping);
            }

            return list;
        }

        int IIListProvider<IGrouping<TKey, TElement>>.GetCount(bool onlyIfCheap) => _count;
    }
}
