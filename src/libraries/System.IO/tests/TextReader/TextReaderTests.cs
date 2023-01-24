// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class TextReaderTests
    {
        protected (char[] chArr, CharArrayTextReader textReader) GetCharArray()
        {
            CharArrayTextReader tr = new CharArrayTextReader(TestDataProvider.CharData);
            return (TestDataProvider.CharData, tr);
        }

        [Fact]
        public void EndOfStream()
        {
            using (CharArrayTextReader tr = new CharArrayTextReader(TestDataProvider.SmallData))
            {
                var result = tr.ReadToEnd();

                Assert.Equal("HELLO", result);

                Assert.True(tr.EndOfStream, "End of TextReader was not true after ReadToEnd");
            }
        }

        [Fact]
        public void NotEndOfStream()
        {
            using (CharArrayTextReader tr = new CharArrayTextReader(TestDataProvider.SmallData))
            {
                char[] charBuff = new char[3];
                var result = tr.Read(charBuff, 0, 3);

                Assert.Equal(3, result);

                Assert.Equal("HEL", new string(charBuff));

                Assert.False(tr.EndOfStream, "End of TextReader was true after ReadToEnd");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadToEndAsync()
        {
            using (CharArrayTextReader tr = new CharArrayTextReader(TestDataProvider.LargeData))
            {
                var result = await tr.ReadToEndAsync();

                Assert.Equal(5000, result.Length);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadToEndAsync_WithCancellationToken()
        {
            using var tr = new CharArrayTextReader(TestDataProvider.LargeData);
            var result = await tr.ReadToEndAsync(default);
            Assert.Equal(5000, result.Length);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadToEndAsync_WithCanceledCancellationToken()
        {
            using var tr = new CharArrayTextReader(TestDataProvider.LargeData);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;

            var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await tr.ReadToEndAsync(token));
            Assert.Equal(token, ex.CancellationToken);
        }

        [Fact]
        public void TestRead()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                for (int count = 0; count < baseInfo.chArr.Length; ++count)
                {
                    int tmp = tr.Read();
                    Assert.Equal((int)baseInfo.chArr[count], tmp);
                }
            }
        }

        [Fact]
        public void ReadZeroCharacters()
        {
            using (CharArrayTextReader tr = GetCharArray().textReader)
            {
                Assert.Equal(0, tr.Read(new char[0], 0, 0));
            }
        }

        [Fact]
        public void ArgumentNullOnNullArray()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                Assert.Throws<ArgumentNullException>(() => tr.Read(null, 0, 0));
            }
        }

        [Fact]
        public void ArgumentOutOfRangeOnInvalidOffset()
        {
            using (CharArrayTextReader tr = GetCharArray().textReader)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => tr.Read(new char[0], -1, 0));
            }
        }

        [Fact]
        public void ArgumentOutOfRangeOnNegativeCount()
        {
            using (CharArrayTextReader tr = GetCharArray().textReader)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => tr.Read(new char[0], 0, 1));
            }
        }

        [Fact]
        public void ArgumentExceptionOffsetAndCount()
        {
            using (CharArrayTextReader tr = GetCharArray().textReader)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => tr.Read(new char[0], 2, 0));
            }
        }

        [Fact]
        public void EmptyInput()
        {
            using (CharArrayTextReader tr = new CharArrayTextReader(new char[] { }))
            {
                char[] buffer = new char[10];
                int read = tr.Read(buffer, 0, 1);
                Assert.Equal(0, read);
            }
        }

        [Fact]
        public void ReadCharArr()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];

                var read = tr.Read(chArr, 0, chArr.Length);
                Assert.Equal(chArr.Length, read);

                for (int count = 0; count < baseInfo.chArr.Length; ++count)
                {
                    Assert.Equal(baseInfo.chArr[count], chArr[count]);
                }
            }
        }

        [Fact]
        public void ReadBlockCharArr()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];

                var read = tr.ReadBlock(chArr, 0, chArr.Length);
                Assert.Equal(chArr.Length, read);

                for (int count = 0; count < baseInfo.chArr.Length; ++count)
                {
                    Assert.Equal(baseInfo.chArr[count], chArr[count]);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadBlockAsyncCharArr()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];

                var read = await tr.ReadBlockAsync(chArr, 0, chArr.Length);
                Assert.Equal(chArr.Length, read);

                for (int count = 0; count < baseInfo.chArr.Length; ++count)
                {
                    Assert.Equal(baseInfo.chArr[count], chArr[count]);
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadAsync()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];

                var read = await tr.ReadAsync(chArr, 4, 3);
                Assert.Equal(3, read);

                for (int count = 0; count < 3; ++count)
                {
                    Assert.Equal(baseInfo.chArr[count], chArr[count + 4]);
                }
            }
        }

        [Fact]
        public void ReadLines()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                string valueString = new string(baseInfo.chArr);

                var data = tr.ReadLine();
                Assert.Equal(valueString.Substring(0, valueString.IndexOf('\r')), data);

                data = tr.ReadLine();
                Assert.Equal(valueString.Substring(valueString.IndexOf('\r') + 1, 3), data);

                data = tr.ReadLine();
                Assert.Equal(valueString.Substring(valueString.IndexOf('\n') + 1, 2), data);

                data = tr.ReadLine();
                Assert.Equal((valueString.Substring(valueString.LastIndexOf('\n') + 1)), data);
            }
        }

        [Fact]
        public void ReadLines2()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                string valueString = new string(baseInfo.chArr);

                char[] temp = new char[10];
                tr.Read(temp, 0, 1);
                var data = tr.ReadLine();

                Assert.Equal(valueString.Substring(1, valueString.IndexOf('\r') - 1), data);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ReadLineAsyncContinuousNewLinesAndTabs()
        {
            char[] newLineTabData = new char[] { '\n', '\n', '\r', '\r', '\n' };
            using (CharArrayTextReader tr = new CharArrayTextReader(newLineTabData))
            {
                for (int count = 0; count < 4; ++count)
                {
                    var data = await tr.ReadLineAsync();
                    Assert.Equal(string.Empty, data);
                }

                var eol = await tr.ReadLineAsync();
                Assert.Null(eol);
            }
        }

        [Fact]
        public void ReadSpan()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];
                var chSpan = new Span<char>(chArr, 0, baseInfo.chArr.Length);

                var read = tr.Read(chSpan);
                Assert.Equal(chArr.Length, read);

                for (int i = 0; i < baseInfo.chArr.Length; i++)
                {
                    Assert.Equal(baseInfo.chArr[i], chArr[i]);
                }
            }
        }

        [Fact]
        public void ReadBlockSpan()
        {
            (char[] chArr, CharArrayTextReader textReader) baseInfo = GetCharArray();
            using (CharArrayTextReader tr = baseInfo.textReader)
            {
                char[] chArr = new char[baseInfo.chArr.Length];
                var chSpan = new Span<char>(chArr, 0, baseInfo.chArr.Length);

                var read = tr.ReadBlock(chSpan);
                Assert.Equal(chArr.Length, read);

                for (int i = 0; i < baseInfo.chArr.Length; i++)
                {
                    Assert.Equal(baseInfo.chArr[i], chArr[i]);
                }
            }
        }
    }
}
