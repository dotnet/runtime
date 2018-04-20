// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.MemoryTests;
using System.Text;

namespace System.Memory.Tests
{
    public abstract class ReadOnlySequenceFactoryChar
    {
        public static ReadOnlySequenceFactory<char> StringFactory { get; } = new StringTestSequenceFactory();

        internal class StringTestSequenceFactory : ReadOnlySequenceFactory<char>
        {
            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                IEnumerable<int> ascii = Enumerable.Range(' ', (char)0x7f - ' ');
                IEnumerable<int> items = ascii;
                for(int i = (size + 20) / ascii.Count(); i > 0; i--)
                    items = items.Concat(ascii);

                var str = new string(items.Select(c => (char)c).ToArray());
                return new ReadOnlySequence<char>(str.AsMemory(10, size));
            }

            public override ReadOnlySequence<char> CreateWithContent(char[] data)
            {
                var startSegment = new char[data.Length + 20];
                Array.Copy(data, 0, startSegment, 10, data.Length);
                var text = new string(startSegment);
                return new ReadOnlySequence<char>(text.AsMemory(10, data.Length));
            }
        }
    }
}
