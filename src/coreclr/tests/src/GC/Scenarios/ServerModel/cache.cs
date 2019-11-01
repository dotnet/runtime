// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ServerSimulator
{
    /// <summary>
    /// This class simulates a cache of user-defined size filled with 
    /// arrays of objects of user-defined size, replacing them using either a FIFO or random algorithm
    /// </summary>
    internal sealed class Cache
    {
        private Object[] cache_list;
        private int cache_length;
        private int cache_item_size;
        private int cache_item_count;
        private bool fifo;
        private int fifoIndex;

        public Cache(bool fifo)
        {
            cache_item_size = (int)(ServerSimulator.Params.CacheSize * ServerSimulator.Params.CacheReplacementRate);
            cache_length = (int)(1 / ServerSimulator.Params.CacheReplacementRate);
            cache_list = new Object[cache_length];
            cache_item_count = 0;
            fifoIndex = 0;
            this.fifo = fifo;
        }

        // fills an entry in the cache with an array of objects
        public void Encache()
        {
            Object[] survivors = new Object[1 + cache_item_size / 100];

            int volume = 0;
            for (int i = 0; volume < cache_item_size; i++)
            {
                int alloc_surv = ServerSimulator.Rand.Next(100, 2000 + 2 * i);
                survivors[i] = new byte[alloc_surv];
                volume += alloc_surv;
            }

            int index;
            if (fifo)
            {
                // use fifo cache replacement
                index = fifoIndex;
                fifoIndex++;
                if (fifoIndex == cache_list.Length)
                {
                    fifoIndex = 0;
                }
            }
            else
            {
                // use random cache replacement
                index = ServerSimulator.Rand.Next(0, cache_length);
            }

            if (cache_list[index] == null)
            {
                cache_item_count++;
            }
            cache_list[index] = survivors;
        }

        // empties the cache
        public void Clear()
        {
            for (int i = 0; i < cache_list.Length; i++)
            {
                cache_list[i] = null;
            }
        }

        // returns true if the cache is full
        public bool IsFull
        {
            get
            {
                return (cache_item_count == cache_length);
            }
        }
    }
}
