// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.RemoteExecutor;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.ML.Tokenizers.Tests
{
    public class TiktokenTests
    {
        const string IMStart = "<|im_start|>";
        const string IMEnd = "<|im_end|>";
        const string IMSep = "<|im_sep|>";

        private static readonly Dictionary<string, int> _specialTokens = new Dictionary<string, int>
                                                {
                                                    { IMStart, 100264},
                                                    { IMEnd, 100265},
                                                };

        public static Tokenizer GPT4 { get; } = TiktokenTokenizer.CreateForModel("gpt-4", _specialTokens);
        public static Tokenizer GPT2 { get; } = TiktokenTokenizer.CreateForModel("gpt2");
        public static Tokenizer P50kBase { get; } = TiktokenTokenizer.CreateForModel("text-davinci-003");
        public static Tokenizer R50kBase { get; } = TiktokenTokenizer.CreateForModel("ada");
        public static Tokenizer P50kEdit { get; } = TiktokenTokenizer.CreateForModel("text-davinci-edit-001");
        public static Tokenizer GPT4o { get; } = TiktokenTokenizer.CreateForModel("gpt-4o");
        public static Tokenizer GPT5 { get; } = TiktokenTokenizer.CreateForModel("gpt-5");
        public static Tokenizer GPT5_1 { get; } = TiktokenTokenizer.CreateForModel("gpt-5.1");
        public static Tokenizer GPT5_2 { get; } = TiktokenTokenizer.CreateForModel("gpt-5.2");
        public static Tokenizer GPT5_3 { get; } = TiktokenTokenizer.CreateForModel("gpt-5.3");
        public static Tokenizer Phi4 { get; } = TiktokenTokenizer.CreateForModel("phi-4");
        public static TiktokenTokenizer GptOss { get; } = TiktokenTokenizer.CreateForModel("gpt-oss-20b");

        [Fact]
        public async Task TestTokenizerCreation()
        {
            TestGPT4TokenizationEncoding(GPT4);
            TestGPT4TokenizationEncoding(Phi4);

            Assert.True(GPT4 is TiktokenTokenizer);
            IReadOnlyDictionary<string, int>? specialTokens = (GPT4 as TiktokenTokenizer)!.SpecialTokens;

            string tokenizerDataFileName = Utils.CreateTemporaryFile("tiktoken");

            string assemblyName = typeof(TiktokenTokenizer).Assembly.FullName!;
            using Stream compressedStream = Assembly.Load($"Microsoft.ML.Tokenizers.Data.Cl100kBase{assemblyName.Substring(assemblyName.IndexOf(','))}").GetManifestResourceStream("cl100k_base.tiktoken.deflate")!;
            using Stream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);

            using (Stream fileStream = File.OpenWrite(tokenizerDataFileName))
            {
                deflateStream.CopyTo(fileStream);
            }

            try
            {
                Tokenizer tokenizer = TiktokenTokenizer.Create(tokenizerDataFileName, GPT4.PreTokenizer, null, specialTokens);
                TestGPT4TokenizationEncoding(tokenizer);

                using (Stream stream = File.OpenRead(tokenizerDataFileName))
                {
                    tokenizer = TiktokenTokenizer.Create(stream, GPT4.PreTokenizer, null, specialTokens);
                }
                TestGPT4TokenizationEncoding(tokenizer);

                tokenizer = await TiktokenTokenizer.CreateAsync(tokenizerDataFileName, GPT4.PreTokenizer, normalizer: null, specialTokens);
                TestGPT4TokenizationEncoding(tokenizer);

                using (Stream stream = File.OpenRead(tokenizerDataFileName))
                {
                    tokenizer = await TiktokenTokenizer.CreateAsync(stream, GPT4.PreTokenizer, normalizer: null, specialTokens);
                }
                TestGPT4TokenizationEncoding(tokenizer);

                using (Stream stream = File.OpenRead(tokenizerDataFileName))
                {
                    tokenizer = TiktokenTokenizer.CreateForModel("gpt-4", stream);
                }
                TestGPT4TokenizationEncoding(tokenizer);

                using (Stream stream = File.OpenRead(tokenizerDataFileName))
                {
                    tokenizer = await TiktokenTokenizer.CreateForModelAsync("gpt-3.5-turbo", stream);
                }
                TestGPT4TokenizationEncoding(tokenizer);

                tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
                TestGPT4TokenizationEncoding(tokenizer);
            }
            finally
            {
                Utils.DeleteFile(tokenizerDataFileName);
            }
        }

        public static IEnumerable<object[]> ModelUrlData()
        {
            yield return new object[] { GPT4, @"https://openaipublic.blob.core.windows.net/encodings/cl100k_base.tiktoken" };
            yield return new object[] { GPT2, @"https://openaipublic.blob.core.windows.net/encodings/r50k_base.tiktoken" }; // GPT2 uses the same encoding as R50kBase
            yield return new object[] { P50kBase, @"https://openaipublic.blob.core.windows.net/encodings/p50k_base.tiktoken" };
            yield return new object[] { R50kBase, @"https://openaipublic.blob.core.windows.net/encodings/r50k_base.tiktoken" };
            yield return new object[] { GPT4o, @"https://openaipublic.blob.core.windows.net/encodings/o200k_base.tiktoken" };
        }

        [Theory]
        [MemberData(nameof(ModelUrlData))]
        public async Task TestTokenizerUsingExternalVocab(Tokenizer tokenizer, string url)
        {
            string tokenizerDataFileName = Utils.CreateTemporaryFile("tiktoken");
            await Utils.DownloadFile(url, tokenizerDataFileName);

            try
            {
                TiktokenTokenizer tiktoken = (tokenizer as TiktokenTokenizer)!;
                TiktokenTokenizer externalTokenizer = TiktokenTokenizer.Create(tokenizerDataFileName, tokenizer.PreTokenizer, null, tiktoken.SpecialTokens);

                IReadOnlyDictionary<ReadOnlyMemory<byte>, int> encoder = GetEncoder(tiktoken)!;
                IReadOnlyDictionary<ReadOnlyMemory<byte>, int> externalEncoder = GetEncoder(externalTokenizer)!;

                Assert.Equal(externalEncoder.Count, encoder.Count);
                foreach (KeyValuePair<ReadOnlyMemory<byte>, int> kvp in encoder)
                {
                    Assert.True(externalEncoder.TryGetValue(kvp.Key, out int value));
                    Assert.Equal(kvp.Value, value);
                }
            }
            finally
            {
                Utils.DeleteFile(tokenizerDataFileName);
            }
        }

        private void TestGPT4TokenizationEncoding(Tokenizer tokenizer)
        {
            string text = "Hello World";
            IReadOnlyList<int> encoded = tokenizer.EncodeToIds(text);
            Assert.Equal(new List<int>() { 9906, 4435 }, encoded);
            Assert.Equal(text, tokenizer.Decode(encoded)!);
            TestDecodingWithSpan((tokenizer as TiktokenTokenizer)!, encoded.ToArray(), text);

            IReadOnlyList<EncodedToken> result = tokenizer.EncodeToTokens(text, out string? normalizedText);
            int idsCount = tokenizer.CountTokens(text);

            int[] ids = result.Select(token => token.Id).ToArray();
            string[] tokens = result.Select(token => token.Value).ToArray();
            Range[] offsets = result.Select(token => token.Offset).ToArray();
            Assert.Equal(encoded, ids);
            Assert.Equal(new string[] { "Hello", " World" }, tokens);
            Assert.Equal(new List<Range> { new Range(0, 5), new Range(5, 11) }, offsets);
            Assert.Equal(encoded.Count, idsCount);
            Assert.Equal(encoded, ids);

            TestGPT4Tokenizer(tokenizer);
        }

        private void TestDecodingWithSpan(TiktokenTokenizer tokenizer, int[] ids, string expectedDecoded)
        {
            char[] destinationBuffer = new char[expectedDecoded.Length];

            OperationStatus status;
            int lastIdsConsumed = 0;
            int lastCharactersWritten = 0;
            int idsConsumed;
            int charactersWritten;

            for (int i = 1; i < destinationBuffer.Length - 1; i += Math.Max(1, destinationBuffer.Length - 3)) // enough to test length 1, and destinationBuffer.Length - 2 only.
            {
                status = tokenizer.Decode(ids, destinationBuffer.AsSpan().Slice(0, i), out idsConsumed, out charactersWritten);
                Assert.Equal(OperationStatus.DestinationTooSmall, status);
                Assert.True(idsConsumed < ids.Length);
                Assert.True(idsConsumed >= lastIdsConsumed);
                Assert.True(charactersWritten < expectedDecoded.Length);
                Assert.True(charactersWritten >= lastCharactersWritten);
                lastIdsConsumed = idsConsumed;
                lastCharactersWritten = charactersWritten;
            }

            status = tokenizer.Decode(ids, destinationBuffer.AsSpan(), out idsConsumed, out charactersWritten);
            Assert.Equal(OperationStatus.Done, status);
            Assert.Equal(ids.Length, idsConsumed);
            Assert.Equal(expectedDecoded.Length, charactersWritten);
            Assert.Equal(expectedDecoded, destinationBuffer.AsSpan().ToString());
        }

        [Fact]
        public void TestEncode1()
        {
            var text = "<|im_start|>Hello World<|im_end|>";
            IReadOnlyList<int> encoded = GPT4.EncodeToIds(text);
            Assert.Equal(new List<int>() { 100264, 9906, 4435, 100265 }, encoded);
            Assert.Equal(text, GPT4.Decode(encoded));
            TestDecodingWithSpan((GPT4 as TiktokenTokenizer)!, encoded.ToArray(), text);

            IReadOnlyList<EncodedToken> result = GPT4.EncodeToTokens(text, out string? normalizedText);
            int idsCount = GPT4.CountTokens(text);

            int[] ids = result.Select(token => token.Id).ToArray();
            string[] tokens = result.Select(token => token.Value).ToArray();
            (int, int)[] offsets = result.Select(token => (token.Offset.Start.Value, token.Offset.End.Value - token.Offset.Start.Value)).ToArray();

            Assert.Equal(encoded, ids);
            Assert.Equal(new string[] { "<|im_start|>", "Hello", " World", "<|im_end|>" }, tokens);
            Assert.Equal(new List<(int, int)> { (0, 12), (12, 5), (17, 6), (23, 10) }, offsets);
            Assert.Equal(encoded.Count, idsCount);
            Assert.Equal(encoded, ids);
        }

        private void TestGPT4Tokenizer(Tokenizer gpt4Tokenizer)
        {
            string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
            IReadOnlyList<int> encoded = gpt4Tokenizer.EncodeToIds(text);
            Assert.Equal(5584, encoded.Count);
            int idsCount = gpt4Tokenizer.CountTokens(text);
            Assert.Equal(encoded.Count, idsCount);

            using (Stream stream = File.OpenRead("./Data/tokens.json"))
            {
                int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                Assert.Equal(expected!, encoded);
            }

            Assert.Equal(text, gpt4Tokenizer.Decode(encoded));
            TestDecodingWithSpan((gpt4Tokenizer as TiktokenTokenizer)!, encoded.ToArray(), text);

            TokenizerTests.TestTokenLimits(gpt4Tokenizer);
        }

        [Fact]
        public void TestEncode3()
        {
            string text = "<|im_start|>Hello<|im_end|> World";
            IReadOnlyList<int> encoded = GPT4.EncodeToIds(text);
            Assert.Equal(new List<int>() { 100264, 9906, 100265, 4435 }, encoded);
            Assert.Equal(text, GPT4.Decode(encoded));
            TestDecodingWithSpan((GPT4 as TiktokenTokenizer)!, encoded.ToArray(), text);

            IReadOnlyList<EncodedToken> result = GPT4.EncodeToTokens(text, out string? normalizedText);
            int[] ids = result.Select(token => token.Id).ToArray();
            string[] tokens = result.Select(token => token.Value).ToArray();
            (int, int)[] offsets = result.Select(token => (token.Offset.Start.Value, token.Offset.End.Value - token.Offset.Start.Value)).ToArray();

            int idsCount = GPT4.CountTokens(text);
            Assert.Equal(encoded, ids);
            Assert.Equal(encoded.Count, idsCount);
            Assert.Equal(new string[] { "<|im_start|>", "Hello", "<|im_end|>", " World" }, tokens);
            Assert.Equal(new List<(int, int)> { (0, 12), (12, 5), (17, 10), (27, 6) }, offsets);
        }

        [Fact]
        public void TestEncode4()
        {
            string text = "";
            IReadOnlyList<int> encoded = GPT4.EncodeToIds(text);
            Assert.Empty(encoded);

            IReadOnlyList<EncodedToken> result = GPT4.EncodeToTokens(text, out string? normalizedText);
            int idsCount = GPT4.CountTokens(text);
            Assert.Empty(result);
            Assert.Equal(0, idsCount);
        }

        [Fact]
        public void TestEncode5()
        {
            string text = "<|im_start|>Hello ⭐ World<|im_end|>";
            IReadOnlyList<int> encoded = GPT4.EncodeToIds(text);
            int idsCount = GPT4.CountTokens(text);
            Assert.Equal(new List<int>() { 100264, 9906, 2928, 99834, 4435, 100265 }, encoded);
            Assert.Equal(text, GPT4.Decode(encoded));
            TestDecodingWithSpan((GPT4 as TiktokenTokenizer)!, encoded.ToArray(), text);

            IReadOnlyList<EncodedToken> result = GPT4.EncodeToTokens(text, out string? normalizedText);
            Assert.Equal(encoded, result.Select(token => token.Id).ToArray());
            Assert.Equal(encoded.Count, idsCount);
            Assert.Equal(new string[] { "<|im_start|>", "Hello", " ⭐", "⭐", " World", "<|im_end|>" }, result.Select(token => token.Value).ToArray());
            Assert.Equal(new List<(int, int)> { (0, 12), (12, 5), (17, 2), (18, 1), (19, 6), (25, 10) }, result.Select(token => (token.Offset.Start.Value, token.Offset.End.Value - token.Offset.Start.Value)).ToArray());
        }

        [Fact]
        public void TestEncodeO200kBaseEncoding()
        {
            foreach (TiktokenTokenizer tokenizer in new[] { GPT4o, GptOss, GPT5, GPT5_1, GPT5_2, GPT5_3 })
            {
                string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
                IReadOnlyList<int> encoded = tokenizer.EncodeToIds(text);
                int idsCount = tokenizer.CountTokens(text);

                Assert.Equal(5609, encoded.Count);
                Assert.Equal(encoded.Count, idsCount);

                using (Stream stream = File.OpenRead("./Data/tokens_gpt4o.json"))
                {
                    int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                    Assert.Equal(expected!, encoded);
                }

                Assert.Equal(text, tokenizer.Decode(encoded));
                TestDecodingWithSpan(tokenizer, encoded.ToArray(), text);

                text = "<|endoftext|>Hello ⭐ World<|endofprompt|>";

                encoded = tokenizer.EncodeToIds(text);
                idsCount = tokenizer.CountTokens(text);
                Assert.Equal(new List<int>() { 199999, 13225, 161181, 5922, 200018 }, encoded);
                Assert.Equal(text, tokenizer.Decode(encoded));
                TestDecodingWithSpan(tokenizer, encoded.ToArray(), text);

                IReadOnlyList<EncodedToken> result = tokenizer.EncodeToTokens(text, out string? normalizedText);

                Assert.Equal(encoded, result.Select(token => token.Id).ToArray());
                Assert.Equal(encoded.Count, idsCount);
                Assert.Equal(new string[] { "<|endoftext|>", "Hello", " ⭐", " World", "<|endofprompt|>" }, result.Select(token => token.Value).ToArray());
                Assert.Equal(new List<(int, int)> { (0, 13), (13, 5), (18, 2), (20, 6), (26, 15) }, result.Select(token => (token.Offset.Start.Value, token.Offset.End.Value - token.Offset.Start.Value)).ToArray());

                TokenizerTests.TestTokenLimits(tokenizer);
            }
        }

        [Fact]
        public void TestEncodeGpt2()
        {
            string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
            IReadOnlyList<int> encoded = GPT2.EncodeToIds(text);
            int idsCount = GPT2.CountTokens(text);
            Assert.Equal(11378, encoded.Count);
            Assert.Equal(encoded.Count, idsCount);

            using (Stream stream = File.OpenRead("./Data/tokens_gpt2.json"))
            {
                int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                Assert.Equal(expected!, encoded);
            }

            Assert.Equal(text, GPT2.Decode(encoded));
            TestDecodingWithSpan((GPT2 as TiktokenTokenizer)!, encoded.ToArray(), text);
        }

        [Fact]
        public void TestEncodeP50kBase()
        {
            string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
            IReadOnlyList<int> encoded = P50kBase.EncodeToIds(text);
            int idsCount = P50kBase.CountTokens(text);
            Assert.Equal(7230, encoded.Count);
            Assert.Equal(encoded.Count, idsCount);

            using (Stream stream = File.OpenRead("./Data/tokens_p50k_base.json"))
            {
                int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                Assert.Equal(expected!, encoded);
            }

            Assert.Equal(text, P50kBase.Decode(encoded));
            TestDecodingWithSpan((P50kBase as TiktokenTokenizer)!, encoded.ToArray(), text);
        }

        [Fact]
        public void TestEncodeP50kEdit()
        {
            string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
            IReadOnlyList<int> encoded = P50kEdit.EncodeToIds(text);
            int idsCount = P50kEdit.CountTokens(text);
            Assert.Equal(7230, encoded.Count);
            Assert.Equal(encoded.Count, idsCount);

            using (Stream stream = File.OpenRead("./Data/tokens_p50k_edit.json"))
            {
                int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                Assert.Equal(expected!, encoded);
            }

            Assert.Equal(text, P50kEdit.Decode(encoded));
            TestDecodingWithSpan((P50kEdit as TiktokenTokenizer)!, encoded.ToArray(), text);
        }

        [Fact]
        public void TestEncodeR50kBase()
        {
            string text = ReadAndSanitizeFile("./Data/lib.rs.txt");
            IReadOnlyList<int> encoded = R50kBase.EncodeToIds(text);
            int idsCount = R50kBase.CountTokens(text);
            Assert.Equal(11378, encoded.Count);
            Assert.Equal(encoded.Count, idsCount);

            using (Stream stream = File.OpenRead("./Data/tokens_r50k_base.json"))
            {
                int[]? expected = JsonSerializer.Deserialize<int[]>(stream) as int[];
                Assert.Equal(expected!, encoded);
            }

            Assert.Equal(text, R50kBase.Decode(encoded));
            TestDecodingWithSpan((R50kBase as TiktokenTokenizer)!, encoded.ToArray(), text);
        }

        [Theory]
        [InlineData("o1")]
        [InlineData("o1-")]
        [InlineData("o1-mini")]
        [InlineData("o4-mini-")]
        [InlineData("o3")]
        [InlineData("o3-")]
        [InlineData("o3-mini")]
        [InlineData("o4-mini")]
        [InlineData("gpt-4.1")]
        [InlineData("gpt-4.1-mini")]
        [InlineData("gpt-4.5-")]
        [InlineData("gpt-4o")]
        [InlineData("gpt-4o-")]
        [InlineData("gpt-5")]
        [InlineData("gpt-5-chat")]
        [InlineData("gpt-5.1")]
        [InlineData("gpt-5.1-mini")]
        [InlineData("gpt-5.2")]
        [InlineData("gpt-5.2-mini")]
        [InlineData("gpt-5.3")]
        [InlineData("gpt-5.3-mini")]
        [InlineData("chatgpt-4o-")]
        [InlineData("gpt-4")]
        [InlineData("gpt-4-")]
        [InlineData("gpt-3.5")]
        [InlineData("gpt-3.5-")]
        [InlineData("gpt-3.5-turbo")]
        [InlineData("gpt-3.5-turbo-")]
        [InlineData("gpt-3.5-turbo-16k")]
        [InlineData("gpt-35")]
        [InlineData("gpt-35-")]
        [InlineData("gpt-35-turbo")]
        [InlineData("gpt-35-turbo-16k")]
        [InlineData("gpt-35-turbo-")]
        [InlineData("text-davinci-003")]
        [InlineData("text-davinci-002")]
        [InlineData("text-davinci-001")]
        [InlineData("text-curie-001")]
        [InlineData("text-babbage-001")]
        [InlineData("text-ada-001")]
        [InlineData("davinci")]
        [InlineData("davinci-002")]
        [InlineData("curie")]
        [InlineData("babbage")]
        [InlineData("babbage-002")]
        [InlineData("ada")]
        [InlineData("code-davinci-002")]
        [InlineData("code-davinci-001")]
        [InlineData("code-cushman-002")]
        [InlineData("code-cushman-001")]
        [InlineData("davinci-codex")]
        [InlineData("cushman-codex")]
        [InlineData("text-davinci-edit-001")]
        [InlineData("code-davinci-edit-001")]
        [InlineData("text-embedding-ada-002")]
        [InlineData("text-embedding-3-small")]
        [InlineData("text-embedding-3-large")]
        [InlineData("text-similarity-davinci-001")]
        [InlineData("text-similarity-curie-001")]
        [InlineData("text-similarity-babbage-001")]
        [InlineData("text-similarity-ada-001")]
        [InlineData("text-search-davinci-doc-001")]
        [InlineData("text-search-curie-doc-001")]
        [InlineData("text-search-babbage-doc-001")]
        [InlineData("text-search-ada-doc-001")]
        [InlineData("code-search-babbage-code-001")]
        [InlineData("code-search-ada-code-001")]
        [InlineData("gpt2")]
        [InlineData("gpt-2")]
        [InlineData("phi-4")]
        [InlineData("gpt-oss-")]
        [InlineData("gpt-oss-120b")]
        [InlineData("gpt-oss-20b")]
        [InlineData("ft:gpt-4o")]
        [InlineData("ft:gpt-4")]
        [InlineData("ft:gpt-3.5-turbo")]
        [InlineData("ft:davinci-002")]
        [InlineData("ft:babbage-002")]
        public void TestAllSupportedModelNames(string modelName)
        {
            Tokenizer tokenizer = TiktokenTokenizer.CreateForModel(modelName);
            Assert.True(tokenizer is TiktokenTokenizer);
            Assert.NotNull(tokenizer.PreTokenizer);
        }

        [Theory]
        [InlineData("r50k_base")]
        [InlineData("p50k_base")]
        [InlineData("p50k_edit")]
        [InlineData("cl100k_base")]
        [InlineData("o200k_base")]
        [InlineData("o200k_harmony")]
        public void TestAllSupportedEncodingNames(string encodingName)
        {
            Tokenizer tokenizer = TiktokenTokenizer.CreateForEncoding(encodingName);
            Assert.True(tokenizer is TiktokenTokenizer);
            Assert.NotNull(tokenizer.PreTokenizer);

            string modelName = encodingName.ToLowerInvariant() switch
            {
                "r50k_base" => "text-davinci-001",
                "p50k_base" => "text-davinci-003",
                "p50k_edit" => "text-davinci-edit-001",
                "cl100k_base" => "gpt-4",
                "o200k_base" => "gpt-4o",
                "o200k_harmony" => "gpt-oss-120b",
                _ => throw new ArgumentException("Invalid encoding name"),
            };

            Tokenizer tokenizer1 = TiktokenTokenizer.CreateForModel(modelName);

            Assert.True(tokenizer is TiktokenTokenizer);
            Assert.True(tokenizer1 is TiktokenTokenizer);

            TiktokenTokenizer tiktoken = (tokenizer as TiktokenTokenizer)!;
            TiktokenTokenizer tiktoken1 = (tokenizer1 as TiktokenTokenizer)!;

            Assert.Equal(GetEncoder(tiktoken1), GetEncoder(tiktoken));
            Assert.Equal(GetDecoder(tiktoken1), GetDecoder(tiktoken));
            Assert.Equal(tiktoken1.SpecialTokens, tiktoken.SpecialTokens);
            Assert.Equal(GetVocabulary(tiktoken1), GetVocabulary(tiktoken));
        }

        [Fact]
        public void TestEncodingNamesNegativeCases()
        {
            Assert.Throws<ArgumentNullException>(() => TiktokenTokenizer.CreateForEncoding(null!));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("r50k_base_"));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("p50k_base_"));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("p50k_edit_"));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("cl100k_base_"));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("o200k_base_"));
            Assert.Throws<ArgumentException>(() => TiktokenTokenizer.CreateForEncoding("o200k_harmony_"));
        }

        [InlineData("gpt-4")]
        [InlineData("gpt-4.1")]
        [InlineData("gpt-4o")]
        [InlineData("gpt-5")]
        [InlineData("gpt-5.1")]
        [InlineData("gpt-5.2")]
        [InlineData("gpt-5.3")]
        [InlineData("o1")]
        [InlineData("o3")]
        [InlineData("o4-mini")]
        [InlineData("text-davinci-003")]
        [InlineData("text-curie-001")]
        [InlineData("text-davinci-edit-001")]
        [InlineData("phi-4")]
        [InlineData("gpt-oss-20b")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestCreationUsingModel(string modelName)
        {
            RemoteExecutor.Invoke(static (name) =>
            {
#if NET8_0_OR_GREATER || NETFRAMEWORK_4_8_OR_GREATER
                long allocation = GC.GetAllocatedBytesForCurrentThread();
#endif // NET8_0_OR_GREATER || NETFRAMEWORK_4_8_OR_GREATER

                Tokenizer tokenizer = TiktokenTokenizer.CreateForModel(name);
                Assert.True(tokenizer is TiktokenTokenizer);
                Assert.NotNull(tokenizer.PreTokenizer);

#if NET8_0_OR_GREATER || NETFRAMEWORK_4_8_OR_GREATER
                int entriesCount = GetEncoder((tokenizer as TiktokenTokenizer)!)!.Count;
                allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;

                // entriesCount * 260 is average memory allocation during the initialization for the the models we carry data files for.
                // this allocation is not the size of the cache but it include all temporary allocations during the initialization.
                Assert.True((entriesCount * 260) > allocation, $"Memory allocation of {entriesCount} entries for {name}: {allocation} bytes");
#endif // NET8_0_OR_GREATER || NETFRAMEWORK_4_8_OR_GREATER
            }, modelName).Dispose();
        }

        public static IEnumerable<object?[]> TokenizerTestData
        {
            get
            {
                // string to tokenize, produced tokens, the token offsets
                yield return new object?[]
                {
                    "the brown fox jumped over the lazy dog!",
                    new string[] { "the", " brown", " fox", " jumped", " over", " the", " lazy", " dog", "!" },
                    new (int Index, int Length)[] { (0, 3), (3, 6), (9, 4), (13, 7), (20, 5), (25, 4), (29, 5), (34, 4), (38, 1) },
                    new int[] { 1820, 14198, 39935, 27096, 927, 279, 16053, 5679, 0 }
                };
                yield return new object?[]
                {
                    "he traveled to Egypt during the summer, the weather was hot and ammunition." ,
                    new string[] { "he", " traveled", " to", " Egypt", " during", " the", " summer", ",", " the", " weather", " was", " hot", " and", " ammunition", "." },
                    new (int Index, int Length)[] { (0, 2), (2, 9), (11, 3), (14, 6), (20, 7), (27, 4), (31, 7), (38, 1), (39, 4), (43, 8), (51, 4), (55, 4), (59, 4), (63, 11), (74, 1) },
                    new int[] { 383, 31796, 311, 15212, 2391, 279, 7474, 11, 279, 9282, 574, 4106, 323, 37768, 13 }
                };
                yield return new object?[]
                {
                    "She played many games and she felt exhausted afterward",
                    new string[] { "She", " played", " many", " games", " and", " she", " felt", " exhausted", " afterward" },
                    new (int Index, int Length)[] { (0, 3), (3, 7), (10, 5), (15, 6), (21, 4), (25, 4), (29, 5), (34, 10), (44, 10) },
                    new int[] { 8100, 6476, 1690, 3953, 323, 1364, 6612, 39019, 49043 }
                };
                yield return new object?[]
                {
                    "Hello, y'all! How are you 😁 ?",
                    new string[] { "Hello", ",", " y", "'all", "!", " How", " are", " you", " 😁", "😁", " ?" },
                    new (int Index, int Length)[] { (0, 5), (5, 1), (6, 2), (8, 4), (12, 1), (13, 4), (17, 4), (21, 4), (25, 3), (26, 2), (28, 2) },
                    new int[] { 9906, 11, 379, 65948, 0, 2650, 527, 499, 27623, 223, 949 }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TokenizerTestData))]
        public void TestTokenizerEncoding(string text, string[] expectedTokens, (int Index, int Length)[] expectedOffsets, int[] expectedIds)
        {
            TestTokenizerEncodingForTokenizer(GPT4, text, expectedTokens, expectedOffsets, expectedIds);
            TestTokenizerEncodingForTokenizer(Phi4, text, expectedTokens, expectedOffsets, expectedIds);
        }

        private void TestTokenizerEncodingForTokenizer(Tokenizer tokenizer, string text, string[] expectedTokens, (int Index, int Length)[] expectedOffsets, int[] expectedIds)
        {
            IReadOnlyList<EncodedToken> encoding = tokenizer.EncodeToTokens(text, out _);
            IReadOnlyList<EncodedToken> encoding1 = tokenizer.EncodeToTokens(text.AsSpan(), out _);

            Assert.Equal(expectedTokens, encoding.Select(t => t.Value).ToArray());
            Assert.Equal(expectedOffsets, encoding.Select(t => (t.Offset.Start.Value, t.Offset.End.Value - t.Offset.Start.Value)).ToArray());
            Assert.Equal(expectedIds, encoding.Select(t => t.Id).ToArray());

            Assert.Equal(expectedTokens, encoding1.Select(t => t.Value).ToArray());
            Assert.Equal(expectedOffsets, encoding1.Select(t => (t.Offset.Start.Value, t.Offset.End.Value - t.Offset.Start.Value)).ToArray());
            Assert.Equal(expectedIds, encoding1.Select(t => t.Id).ToArray());

            Assert.Equal(expectedIds, tokenizer.EncodeToIds(text));
            Assert.Equal(expectedIds, tokenizer.EncodeToIds(text.AsSpan()));
            Assert.Equal(expectedIds, tokenizer.EncodeToIds(text, expectedIds.Length, out string? normalizedText, out int length));
            Assert.Null(normalizedText);
            Assert.Equal(text.Length, length);
            Assert.Equal(expectedIds, tokenizer.EncodeToIds(text.AsSpan(), expectedIds.Length, out normalizedText, out length));
            Assert.Null(normalizedText);
            Assert.Equal(text.Length, length);

            Assert.Equal(expectedIds.Take(expectedIds.Length - 4), tokenizer.EncodeToIds(text, expectedIds.Length - 4, out normalizedText, out length));
            Assert.Null(normalizedText);
            int expectedLength = expectedOffsets[expectedOffsets.Length - 5].Index + expectedOffsets[expectedOffsets.Length - 5].Length;
            Assert.Equal(expectedLength, length);
            Assert.Equal(expectedIds.Take(expectedIds.Length - 4), tokenizer.EncodeToIds(text.AsSpan(), expectedIds.Length - 4, out normalizedText, out length));
            Assert.Null(normalizedText);
            Assert.Equal(expectedLength, length);

            Assert.Equal(expectedIds.Length, tokenizer.CountTokens(text));
            Assert.Equal(expectedIds.Length, tokenizer.CountTokens(text.AsSpan()));

            Assert.Equal(expectedOffsets[expectedOffsets.Length - 4].Index + expectedOffsets[expectedOffsets.Length - 4].Length, tokenizer.GetIndexByTokenCount(text, expectedIds.Length - 3, out normalizedText, out int tokenCount));
            Assert.Null(normalizedText);
            Assert.Equal(expectedIds.Length - 3, tokenCount);
            Assert.Equal(expectedOffsets[expectedOffsets.Length - 4].Index + expectedOffsets[expectedOffsets.Length - 4].Length, tokenizer.GetIndexByTokenCount(text.AsSpan(), expectedIds.Length - 3, out normalizedText, out tokenCount));
            Assert.Null(normalizedText);
            Assert.Equal(expectedIds.Length - 3, tokenCount);

            Assert.Equal(expectedOffsets[expectedOffsets.Length - 3].Index, tokenizer.GetIndexByTokenCountFromEnd(text, 3, out normalizedText, out tokenCount));
            Assert.Null(normalizedText);
            Assert.Equal(3, tokenCount);
            Assert.Equal(expectedOffsets[expectedOffsets.Length - 3].Index, tokenizer.GetIndexByTokenCountFromEnd(text.AsSpan(), 3, out normalizedText, out tokenCount));
            Assert.Null(normalizedText);
            Assert.Equal(3, tokenCount);
        }

        // Test running copy the test data files to the output folder but sometimes the file content is mutated replacing '\n' with '\r\n'.
        // This method reads the file and removes the extra inserted '\r' characters. Having '\r' in the file content will cause the tests to fail.
        private string ReadAndSanitizeFile(string path)
        {
            // Didn't use String.Replace because the version accept stringComparison parameter is not supported on NETFX.
            string text = File.ReadAllText(path);
            StringBuilder sb = new StringBuilder();

            foreach (char c in text)
            {
                if (c != '\r')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static IEnumerable<object?[]> TokenizerLimitsTestData
        {
            get
            {
                // string to tokenize, produced tokens, the token offsets, the token ids
                yield return new object?[]
                {
                    "Hello ⭐ World",
                    new string[] { "Hello", " ⭐", "⭐", " World" },
                    new (int Index, int Length)[] { (0, 5), (5, 2), (6, 1), (7, 6) },
                    new int[] { 9906, 2928, 99834, 4435 }
                };

                yield return new object?[]
                {
                    "⭐", // encoded to multiple tokens
                    new string[] { "⭐", "⭐" },
                    new (int Index, int Length)[] { (0, 1), (0, 1) },
                    new int[] { 158, 99834 }
                };

                yield return new object?[]
                {
                    "Hi 😀", // Surrogates
                    new string[] { "Hi", " 😀" },
                    new (int Index, int Length)[] { (0, 2), (2, 3) },
                    new int[] { 13347, 91416 }
                };

                yield return new object?[]
                {
                    "⭐😀", // character encoded to multiple tokens and surrogates
                    new string[] { "⭐", "⭐", "😀", "😀" },
                    new (int Index, int Length)[] { (0, 1), (0, 1), (1, 2), (1, 2) },
                    new int[] { 158, 99834, 76460, 222 }
                };

                yield return new object?[]
                {
                    "From: Adele Vance\nSubject: TestSubject\nTestBodyContent",
                    new string[] { "From", ":", " Ade", "le", " Vance", "\n", "Subject", ":", " Test", "Subject", "\n", "Test", "Body", "Content" },
                    new (int Index, int Length)[] { (0, 4), (4, 1), (5, 4), (9, 2), (11, 6), (17, 1), (18, 7), (25, 1), (26, 5), (31, 7), (38, 1), (39, 4), (43, 4), (47, 7)},
                    new int[] { 3915, 25, 63140, 273, 92368, 198, 13317, 25, 3475, 13317, 198, 2323, 5561, 2831 }
                };
            }
        }

        [Theory]
        [MemberData(nameof(TokenizerLimitsTestData))]
        public void TestPreciseTokenLimits(string text, string[] expectedTokens, (int Index, int Length)[] expectedOffsets, int[] expectedIds)
        {
            IReadOnlyList<EncodedToken> result = GPT4.EncodeToTokens(text, out _);
            int[] ids = result.Select(r => r.Id).ToArray();
            (int Index, int Length)[] offsets = result.Select(r => (r.Offset.Start.Value, r.Offset.End.Value - r.Offset.Start.Value)).ToArray();
            Assert.Equal(expectedTokens, result.Select(r => r.Value));
            Assert.Equal(expectedIds, ids);
            Assert.Equal(expectedOffsets, offsets);
            Assert.Equal(expectedIds, GPT4.EncodeToIds(text));
            Assert.Equal(expectedIds.Length, GPT4.CountTokens(text));

            for (int tokenCount = 1; tokenCount <= ids.Length; tokenCount++)
            {
                int length = GPT4.GetIndexByTokenCount(text, tokenCount, out _, out int count);
                Assert.True(count <= ids.Length);

                if (count < tokenCount)
                {
                    Assert.True(count < ids.Length - 1);
                    Assert.True(offsets[count + 1].Index < offsets[count].Index + offsets[count].Length);
                }

                if (count > 0)
                {
                    Assert.Equal(offsets[count - 1].Index + offsets[count - 1].Length, length);
                }
                else
                {
                    Assert.Equal(0, length);
                }

                int index = GPT4.GetIndexByTokenCountFromEnd(text, tokenCount, out _, out count);
                Assert.True(count <= ids.Length);

                if (count < tokenCount)
                {
                    Assert.True(ids.Length - tokenCount > 0);
                    Assert.True(offsets[offsets.Length - tokenCount].Index < offsets[offsets.Length - tokenCount - 1].Index + offsets[offsets.Length - tokenCount - 1].Length);
                }

                if (count > 0)
                {
                    Assert.Equal(offsets[offsets.Length - count].Index, index);
                }
                else
                {
                    Assert.Equal(text.Length, index);
                }
            }
        }

        [Fact]
        public void TestPhi4SpecialCases()
        {
            string text = $"{IMStart}Hello{IMSep} World{IMEnd}<|dummy_85|>";
            IReadOnlyList<int> encoded = Phi4.EncodeToIds(text);
            Assert.Equal(new List<int>() { 100264, 9906, 100266, 4435, 100265, 100349 }, encoded);
            Assert.Equal(text, Phi4.Decode(encoded));
        }

        [Fact]
        public void TestOss()
        {
            Assert.Equal(
                new Dictionary<string, int>
                {
                    { "<|startoftext|>",     199998 },
                    { "<|endoftext|>",       199999 },
                    { "<|reserved_200000|>", 200000 },
                    { "<|reserved_200001|>", 200001 },
                    { "<|return|>",          200002 },
                    { "<|constrain|>",       200003 },
                    { "<|reserved_200004|>", 200004 },
                    { "<|channel|>",         200005 },
                    { "<|start|>",           200006 },
                    { "<|end|>",             200007 },
                    { "<|message|>",         200008 },
                    { "<|reserved_200009|>", 200009 },
                    { "<|reserved_200010|>", 200010 },
                    { "<|reserved_200011|>", 200011 },
                    { "<|call|>",            200012 },
                    { "<|reserved_200013|>", 200013 },
                    { "<|reserved_200014|>", 200014 },
                    { "<|reserved_200015|>", 200015 },
                    { "<|reserved_200016|>", 200016 },
                    { "<|reserved_200017|>", 200017 },
                    { "<|endofprompt|>",     200018 },
                }, GptOss.SpecialTokens);

            string text = "<|startoftext|><|start|><|message|>Hello World<|end|><|endoftext|>";

            IReadOnlyList<int> ids = GptOss.EncodeToIds(text);

            Assert.Equal(
                new List<int> { 199998, 200006, 200008, 13225, 5922, 200007, 199999 },
                ids);
            Assert.Equal(text, GptOss.Decode(ids));

            Assert.Equal(new string[] { "<|startoftext|>", "<|start|>", "<|message|>", "Hello", " World", "<|end|>", "<|endoftext|>" },
                GptOss.EncodeToTokens(text, out _).Select(t => t.Value).ToArray());

            Assert.Equal(new List<(int, int)> { (0, 15), (15, 24), (24, 35), (35, 40), (40, 46), (46, 53), (53, 66) },
                GptOss.EncodeToTokens(text, out _).Select(t => (t.Offset.Start.Value, t.Offset.End.Value)).ToList());

            Assert.Equal(ids, GptOss.EncodeToTokens(text, out _).Select(t => t.Id).ToList());
        }

        // We are not exposing the Encoder, Decoder, or Vocabulary so far. For now, use reflection to test it.
        private static IReadOnlyDictionary<ReadOnlyMemory<byte>, int>? GetEncoder(TiktokenTokenizer tiktoken)
            => typeof(TiktokenTokenizer).GetProperty("Encoder", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tiktoken) as IReadOnlyDictionary<ReadOnlyMemory<byte>, int>;

        private static IReadOnlyDictionary<int, ReadOnlyMemory<byte>>? GetDecoder(TiktokenTokenizer tiktoken)
            => typeof(TiktokenTokenizer).GetProperty("Decoder", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tiktoken) as IReadOnlyDictionary<int, ReadOnlyMemory<byte>>;

        private static IReadOnlyDictionary<string, int>? GetVocabulary(TiktokenTokenizer tiktoken)
            => typeof(TiktokenTokenizer).GetProperty("Vocabulary", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(tiktoken) as IReadOnlyDictionary<string, int>;
    }
}

