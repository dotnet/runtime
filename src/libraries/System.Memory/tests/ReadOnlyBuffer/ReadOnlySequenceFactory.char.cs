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
            static string s_stringData = InitalizeStringData();

            static string InitalizeStringData()
            {
                IEnumerable<int> ascii = Enumerable.Range(' ', (char)0x7f - ' ');

                return new string(ascii.Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Concat(ascii)
                    .Select(c => (char)c)
                    .ToArray());
            }

            public override ReadOnlySequence<char> CreateOfSize(int size)
            {
                return new ReadOnlySequence<char>(s_stringData.AsMemory(10, size));
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
