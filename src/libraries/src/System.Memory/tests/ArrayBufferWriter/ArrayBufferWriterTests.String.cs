// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Buffers.Tests
{
    public class ArrayBufferWriterTests_String : ArrayBufferWriterTests<string>
    {
        public override void WriteData(IBufferWriter<string> bufferWriter, int numStrings)
        {
            Span<string> outputSpan = bufferWriter.GetSpan(numStrings);
            Debug.Assert(outputSpan.Length >= numStrings);
            var random = new Random(42);

            var data = new string[numStrings];

            for (int i = 0; i < numStrings; i++)
            {
                int length = random.Next(5, 10);
                data[i] = GetRandomString(random, length, 32, 127);
            }

            data.CopyTo(outputSpan);

            bufferWriter.Advance(numStrings);
        }

        private static string GetRandomString(Random r, int length, int minCodePoint, int maxCodePoint)
        {
            StringBuilder sb = new StringBuilder(length);
            while (length-- != 0)
            {
                sb.Append((char)r.Next(minCodePoint, maxCodePoint));
            }
            return sb.ToString();
        }
    }
}
