// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    public sealed partial class StringBuilder
    {
        private void ShowChunks(int maxChunksToShow = 10)
        {
            int count = 0;
            StringBuilder head = this;
            StringBuilder? current = this;

            while (current != null)
            {
                if (count < maxChunksToShow)
                {
                    count++;
                }
                else
                {
                    Debug.Assert(head.m_ChunkPrevious != null);
                    head = head.m_ChunkPrevious;
                }
                current = current.m_ChunkPrevious;
            }

            current = head;
            string[] chunks = new string[count];
            for (int i = count; i > 0; i--)
            {
                chunks[i - 1] = new string(current.m_ChunkChars).Replace('\0', '.');
                Debug.Assert(current.m_ChunkPrevious != null);
                current = current.m_ChunkPrevious;
            }

            Debug.WriteLine('|' + string.Join('|', chunks) + '|');
        }
    }
}
