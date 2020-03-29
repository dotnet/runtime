using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Container for storing numbers with efficient range queries and insertions.
    /// </summary>
    internal class RangeSet
    {
        // TODO-RZ: Replace sorted set by efficient implementation
        private SortedSet<ulong> items;

        internal RangeSet()
        {
            items = new SortedSet<ulong>();
        }

        internal void Add(ulong value, ulong count = 1)
        {
            for (ulong i = 0; i < count; i++)
            {
                items.Add(value + i);
            }
        }

        internal ulong GetMin()
        {
            return items.Min;
        }

        internal ulong GetMax()
        {
            return items.Max;
        }

        internal void RemoveUntil(ulong value)
        {
            items = items.GetViewBetween(value, ulong.MaxValue);
        }

        internal ulong? NextTo(ulong value)
        {
            var view = items.GetViewBetween(value + 1, ulong.MaxValue);
            return view.Count > 0 ? view.Min : (ulong?) null;
        }

        internal ulong? PreviousTo(ulong value)
        {
            var view = items.GetViewBetween(0, value - 1);
            return view.Count > 0 ? view.Max: (ulong?) null;
        }

        internal int Count => items.Count;
    }
}
