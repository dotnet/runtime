// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ML.Tokenizers
{
    /// <summary>
    /// Represent the rapid Byte Pair Encoding tokenizer.
    /// </summary>
    public sealed partial class TiktokenTokenizer : Tokenizer
    {
        private readonly Dictionary<ReadOnlyMemory<byte>, int> _encoder;
        private readonly Dictionary<int, ReadOnlyMemory<byte>> _decoder;
        private readonly LruCache<(int Id, int TokenIndex, int TokenLength)[]> _cache;
        private readonly Dictionary<StringSpanOrdinalKey, (int Id, string Token)> _vocab;
        private IReadOnlyDictionary<string, int>? _vocabOriginal;
        private const int MaxWordLengthToCache = 15;
        private readonly PreTokenizer? _preTokenizer;
        private readonly Normalizer? _normalizer;

        /// <summary>
        /// Create a new Tiktoken tokenizer's object.
        /// </summary>
        /// <param name="vocabFilePath">The path to the BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="vocabFilePath"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when failed to load the BPE vocab file.</exception>
        internal TiktokenTokenizer(string vocabFilePath, PreTokenizer? preTokenizer, IReadOnlyDictionary<string, int>? specialTokens = null, Normalizer? normalizer = null, int cacheSize = LruCache<int[]>.DefaultCacheSize) :
            this(string.IsNullOrEmpty(vocabFilePath) ? throw new ArgumentNullException(nameof(vocabFilePath)) : File.OpenRead(vocabFilePath), preTokenizer, specialTokens, normalizer, cacheSize, disposeStream: true)
        {
        }

        /// <summary>
        /// Create a new Tiktoken tokenizer's object.
        /// </summary>
        /// <param name="vocabStream">The stream to the BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="vocabStream"/> is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when failed to load the BPE vocab file.</exception>
        internal TiktokenTokenizer(Stream vocabStream, PreTokenizer? preTokenizer, IReadOnlyDictionary<string, int>? specialTokens = null, Normalizer? normalizer = null, int cacheSize = LruCache<int[]>.DefaultCacheSize) :
            this(vocabStream ?? throw new ArgumentNullException(nameof(vocabStream)), preTokenizer, specialTokens, normalizer, cacheSize, disposeStream: false)
        {
        }

        /// <summary>
        /// Create a new Tiktoken tokenizer's object.
        /// </summary>
        /// <param name="encoder">The dictionary mapping token utf-8 bytes to Ids.</param>
        /// <param name="decoder">The dictionary mapping Ids to token utf-8 bytes.</param>
        /// <param name="vocab">The dictionary mapping string tokens to Ids.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="cacheSize">The max size of the cache to use.</param>
        internal TiktokenTokenizer(
            Dictionary<ReadOnlyMemory<byte>, int> encoder,
            Dictionary<int, ReadOnlyMemory<byte>> decoder,
            Dictionary<StringSpanOrdinalKey, (int Id, string Token)> vocab,
            PreTokenizer? preTokenizer,
            IReadOnlyDictionary<string, int>? specialTokens,
            Normalizer? normalizer = null,
            int cacheSize = LruCache<int[]>.DefaultCacheSize)
        {
            _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
            _decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
            _vocab = vocab ?? throw new ArgumentNullException(nameof(vocab));

            _encoder = encoder!;
            _decoder = decoder!;
            _vocab = vocab!;

            _preTokenizer = preTokenizer;
            _normalizer = normalizer;

            _cache = new LruCache<(int Id, int TokenIndex, int TokenLength)[]>(cacheSize);

            SpecialTokens = specialTokens;
            CacheSpecialTokensEncoding(specialTokens);
        }

        private TiktokenTokenizer(Stream vocabStream, PreTokenizer? preTokenizer, IReadOnlyDictionary<string, int>? specialTokens, Normalizer? normalizer, int cacheSize, bool disposeStream)
        {
            try
            {
                _cache = new LruCache<(int Id, int TokenIndex, int TokenLength)[]>(cacheSize);
                (_encoder, _vocab, _decoder) = LoadTiktokenBpeAsync(vocabStream, useAsync: false).GetAwaiter().GetResult();

                _preTokenizer = preTokenizer;
                _normalizer = normalizer;

                SpecialTokens = specialTokens;
                CacheSpecialTokensEncoding(specialTokens);
            }
            finally
            {
                if (disposeStream)
                {
                    vocabStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Gets the PreTokenizer used by the Tokenizer.
        /// </summary>
        public override PreTokenizer? PreTokenizer => _preTokenizer;

        /// <summary>
        /// Gets the Normalizer in use by the Tokenizer.
        /// </summary>
        public override Normalizer? Normalizer => _normalizer;

        private void CacheSpecialTokensEncoding(IReadOnlyDictionary<string, int>? specialTokens)
        {
            Debug.Assert(_cache is not null);
            Debug.Assert(_decoder is not null);

            if (specialTokens is not null)
            {
                foreach (KeyValuePair<string, int> specialToken in specialTokens)
                {
                    _decoder![specialToken.Value] = Encoding.UTF8.GetBytes(specialToken.Key);
                    _cache!.Add(specialToken.Key, new[] { (Id: specialToken.Value, TokenIndex0: 0, TokenLength: specialToken.Key.Length) });
                }
            }
        }

        /// <summary>
        /// Load BPE vocab dictionary from a stream.
        /// </summary>
        /// <param name="vocabStream">Stream to the BPE vocab file</param>
        /// <param name="useAsync">Whether to perform I/O synchronously or asynchronously.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> used to request cancellation of the operation.</param>
        /// <returns>Map of byte[] to integer token id</returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static async ValueTask<(Dictionary<ReadOnlyMemory<byte>, int>, Dictionary<StringSpanOrdinalKey, (int Id, string Token)>, Dictionary<int, ReadOnlyMemory<byte>>)> LoadTiktokenBpeAsync(
            Stream vocabStream, bool useAsync, CancellationToken cancellationToken = default)
        {
            Dictionary<ReadOnlyMemory<byte>, int> encoder;
            Dictionary<StringSpanOrdinalKey, (int Id, string Token)> vocab;
            Dictionary<int, ReadOnlyMemory<byte>> decoder;

            try
            {
                // Don't dispose the reader as it will dispose the underlying stream vocabStream. The caller is responsible for disposing the stream.
                StreamReader reader = new StreamReader(vocabStream);
                string? line = useAsync ? await Helpers.ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) : reader.ReadLine();

                const string capacity = "Capacity: ";
                int suggestedCapacity = 0; // default capacity
                if (line is not null && line.StartsWith(capacity, StringComparison.Ordinal))
                {
                    if (!Helpers.TryParseInt32(line, capacity.Length, out suggestedCapacity))
                    {
                        throw new FormatException($"Invalid format in the BPE vocab file stream");
                    }

                    line = useAsync ? await Helpers.ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) : reader.ReadLine();
                }

                encoder = new Dictionary<ReadOnlyMemory<byte>, int>(suggestedCapacity, ReadOnlyMemoryByteComparer.Instance);
                vocab = new Dictionary<StringSpanOrdinalKey, (int Id, string Token)>(suggestedCapacity);
                decoder = new Dictionary<int, ReadOnlyMemory<byte>>(suggestedCapacity);

                // skip empty lines
                while (line is not null && line.Length == 0)
                {
                    line = useAsync ? await Helpers.ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) : reader.ReadLine();
                }

                if (line is not null && line.IndexOf(' ') < 0)
                {
                    // We generate the ranking using the line number
                    int lineNumber = 0;
                    do
                    {
                        if (line.Length > 0)
                        {
                            AddData(Convert.FromBase64String(line), lineNumber);
                        }
                        lineNumber++;
                    } while ((line = useAsync ? await Helpers.ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) : reader.ReadLine()) is not null);
                }

                while (line is not null)
                {
                    if (line.Length > 0)
                    {
                        int spaceIndex = line.IndexOf(' ');
                        if (spaceIndex <= 0 || spaceIndex >= line.Length - 1 || line.IndexOf(' ', spaceIndex + 1) >= 0)
                        {
                            throw new FormatException($"Invalid format in the BPE vocab file stream");
                        }

                        if (Helpers.TryParseInt32(line, spaceIndex + 1, out int rank))
                        {
                            AddData(Helpers.FromBase64String(line, 0, spaceIndex), rank);
                        }
                        else
                        {
                            throw new FormatException($"Can't parse {line.Substring(spaceIndex)} to integer");
                        }

                        line = useAsync ?
                            await Helpers.ReadLineAsync(reader, cancellationToken).ConfigureAwait(false) :
                            reader.ReadLine();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load from BPE vocab file stream: {ex.Message}", ex);
            }

            return (encoder, vocab, decoder);

            void AddData(byte[] tokenBytes, int rank)
            {
                encoder[tokenBytes] = rank;
                decoder[rank] = tokenBytes;

                string decodedToken = Encoding.UTF8.GetString(tokenBytes);

                if (decodedToken.IndexOf('\uFFFD') < 0)
                {
                    vocab[new StringSpanOrdinalKey(decodedToken)] = (rank, decodedToken);
                }
            }
        }

        /// <summary>
        /// Encodes input text to a list of <see cref="EncodedToken" />s.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="textSpan">The span of the text to encode which will be used if the <paramref name="text"/> is <see langword="null"/>.</param>
        /// <param name="settings">The settings used to encode the text.</param>
        protected override EncodeResults<EncodedToken> EncodeToTokens(string? text, ReadOnlySpan<char> textSpan, EncodeSettings settings)
        {
            if (string.IsNullOrEmpty(text) && textSpan.IsEmpty)
            {
                return new EncodeResults<EncodedToken> { NormalizedText = null, Tokens = [], CharsConsumed = 0 };
            }

            IEnumerable<(int Offset, int Length)>? splits = InitializeForEncoding(
                                                                text,
                                                                textSpan,
                                                                settings.ConsiderPreTokenization,
                                                                settings.ConsiderNormalization,
                                                                _normalizer,
                                                                _preTokenizer,
                                                                out string? normalizedText,
                                                                out ReadOnlySpan<char> textSpanToEncode,
                                                                out int charsConsumed);

            List<EncodedToken> tokens = new();

            if (splits is not null)
            {
                foreach ((int Offset, int Length) split in splits)
                {
                    EncodeToTokens(textSpanToEncode.Slice(split.Offset, split.Length), tokens, split.Offset);
                }
            }
            else
            {
                EncodeToTokens(textSpanToEncode, tokens, 0);
            }

            return new EncodeResults<EncodedToken> { NormalizedText = normalizedText, Tokens = tokens, CharsConsumed = charsConsumed };
        }

        /// <summary>
        /// Encode text to a list of tokens.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="tokens">The list of tokens to populate.</param>
        /// <param name="offset">The offset to start encoding from.</param>
        private void EncodeToTokens(ReadOnlySpan<char> text, List<EncodedToken> tokens, int offset)
        {
            Debug.Assert(!text.IsEmpty);

            if (_cache.TryGetValue(text, out (int Id, int TokenIndex, int TokenLength)[] value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    tokens.Add(new EncodedToken(
                                        value[i].Id,
                                        value[i].TokenLength == 0 ? string.Empty : text.Slice(value[i].TokenIndex, value[i].TokenLength).ToString(),
                                        new Range(value[i].TokenIndex + offset, value[i].TokenIndex + offset + value[i].TokenLength)));
                }

                return;
            }

            // cache miss
            if (_vocab.TryGetValue(text, out (int Id, string Token) mappedId))
            {
                tokens.Add(new EncodedToken(mappedId.Id, mappedId.Token, new Range(offset, offset + mappedId.Token.Length)));
                return;
            }

            int utf8Length = Encoding.UTF8.GetMaxByteCount(text.Length);
            byte[] arrayPoolArray = arrayPoolArray = ArrayPool<byte>.Shared.Rent(utf8Length);
            int[]? indexMappingArray = null;
            Span<int> indexMappingSpan = utf8Length + 1 <= 128 ? stackalloc int[128] : (indexMappingArray = ArrayPool<int>.Shared.Rent(utf8Length + 1));
            int encodedLength = Helpers.EncodeToUtf8(text, arrayPoolArray, indexMappingSpan);
            Debug.Assert(encodedLength < indexMappingSpan.Length);
            indexMappingSpan[encodedLength] = text.Length;

            (int Id, int TokenIndex, int TokenLength)[] encodedTokens = BytePairEncoder.BytePairEncode(arrayPoolArray.AsMemory(0, encodedLength), _encoder, indexMappingSpan.Slice(0, encodedLength + 1));
            ArrayPool<byte>.Shared.Return(arrayPoolArray);
            if (indexMappingArray is not null)
            {
                ArrayPool<int>.Shared.Return(indexMappingArray);
            }

            Debug.Assert(encodedTokens.Length > 0);
            string textAsString = text.ToString();

            if (text.Length <= MaxWordLengthToCache)
            {
                _cache.Add(textAsString, encodedTokens);
            }

            for (int i = 0; i < encodedTokens.Length; i++)
            {
                tokens.Add(new EncodedToken(
                                encodedTokens[i].Id,
                                encodedTokens[i].TokenLength == 0 ? string.Empty : text.Slice(encodedTokens[i].TokenIndex, encodedTokens[i].TokenLength).ToString(),
                                new Range(encodedTokens[i].TokenIndex + offset, encodedTokens[i].TokenIndex + offset + encodedTokens[i].TokenLength)));
            }
        }

        /// <summary>
        /// Encodes input text to token Ids.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="textSpan">The span of the text to encode which will be used if the <paramref name="text"/> is <see langword="null"/>.</param>
        /// <param name="settings">The settings used to encode the text.</param>
        /// <returns>The encoded results containing the list of encoded Ids.</returns>
        protected override EncodeResults<int> EncodeToIds(string? text, ReadOnlySpan<char> textSpan, EncodeSettings settings)
        {
            int maxTokenCount = settings.MaxTokenCount;
            if (maxTokenCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(settings.MaxTokenCount), "The maximum number of tokens must be greater than zero.");
            }

            if (string.IsNullOrEmpty(text) && textSpan.IsEmpty)
            {
                return new EncodeResults<int> { NormalizedText = null, Tokens = [], CharsConsumed = 0 };
            }

            IEnumerable<(int Offset, int Length)>? splits = InitializeForEncoding(
                                                                text,
                                                                textSpan,
                                                                settings.ConsiderPreTokenization,
                                                                settings.ConsiderNormalization,
                                                                _normalizer,
                                                                _preTokenizer,
                                                                out string? normalizedText,
                                                                out ReadOnlySpan<char> textSpanToEncode,
                                                                out int charsConsumed);

            List<int> ids = new();

            if (splits is not null)
            {
                charsConsumed = 0;
                foreach ((int Offset, int Length) split in splits)
                {
                    EncodeToIds(textSpanToEncode.Slice(split.Offset, split.Length), ids, out int length, maxTokenCount - ids.Count);
                    charsConsumed = split.Offset + length;

                    if (length < split.Length || ids.Count >= maxTokenCount)
                    {
                        break;
                    }
                }
            }
            else
            {
                EncodeToIds(textSpanToEncode, ids, out charsConsumed);
            }

            return new EncodeResults<int> { NormalizedText = normalizedText, Tokens = ids, CharsConsumed = charsConsumed };
        }

        /// <summary>
        /// Encode text to a list of Ids.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="accumulatedIds">The list of accumulated Ids.</param>
        /// <param name="charsConsumed">The length of the text that encompasses the maximum encoded tokens.</param>
        /// <param name="maxTokenCount">The maximum number of tokens to encode.</param>
        /// <returns>The number of tokens that the input text will be encoded to.</returns>
        private int EncodeToIds(ReadOnlySpan<char> text, IList<int> accumulatedIds, out int charsConsumed, int maxTokenCount = int.MaxValue)
        {
            Debug.Assert(maxTokenCount > 0);

            if (text.IsEmpty)
            {
                charsConsumed = 0;
                return 0;
            }

            if (_cache.TryGetValue(text, out (int Id, int TokenIndex, int TokenLength)[] value))
            {
                return EncodeToIdsResult(value, accumulatedIds, maxTokenCount, text.Length, out charsConsumed);
            }

            if (_vocab.TryGetValue(text, out (int Id, string Token) mappedId))
            {
                charsConsumed = text.Length;
                accumulatedIds.Add(mappedId.Id);
                return 1;
            }

            int utf8Length = Encoding.UTF8.GetMaxByteCount(text.Length);
            byte[] arrayPoolArray = arrayPoolArray = ArrayPool<byte>.Shared.Rent(utf8Length);
            int[]? indexMappingArray = null;
            Span<int> indexMappingSpan = utf8Length + 1 <= 128 ? stackalloc int[128] : (indexMappingArray = ArrayPool<int>.Shared.Rent(utf8Length + 1));
            int encodedLength = Helpers.EncodeToUtf8(text, arrayPoolArray, indexMappingSpan);
            Debug.Assert(encodedLength < indexMappingSpan.Length);
            indexMappingSpan[encodedLength] = text.Length;

            (int Id, int TokenIndex, int TokenLength)[] encodedTokens = BytePairEncoder.BytePairEncode(arrayPoolArray.AsMemory(0, encodedLength), _encoder, indexMappingSpan.Slice(0, encodedLength + 1));
            ArrayPool<byte>.Shared.Return(arrayPoolArray);
            if (indexMappingArray is not null)
            {
                ArrayPool<int>.Shared.Return(indexMappingArray);
            }

            if (text.Length <= MaxWordLengthToCache)
            {
                string textAsString = text.ToString();
                _cache.Add(textAsString, encodedTokens);
            }

            return EncodeToIdsResult(encodedTokens, accumulatedIds, maxTokenCount, text.Length, out charsConsumed);
        }

        private int EncodeToIdsResult((int Id, int TokenIndex, int TokenLength)[] tokens, IList<int>? accumulatedIds, int maxTokens, int fullTextLength, out int charsConsumed)
        {
            charsConsumed = 0;

            if (tokens.Length <= maxTokens)
            {
                if (accumulatedIds is not null)
                {
                    foreach (var t in tokens)
                    {
                        accumulatedIds.Add(t.Id);
                    }
                }

                charsConsumed = fullTextLength;
                return tokens.Length;
            }

            int tokenCount;
            for (tokenCount = 0; tokenCount < maxTokens; tokenCount++)
            {
                int overlapIndex = tokens[tokenCount].TokenIndex + tokens[tokenCount].TokenLength;
                // maxTokens is less than tokens.Count, so it is safe to index maxTokens.
                if (tokens[tokenCount + 1].TokenIndex < overlapIndex)
                {
                    // Ensure we'll not break the text in the middle of a code-point
                    int j = tokenCount + 2;
                    while (j < tokens.Length && tokens[j].TokenIndex < overlapIndex)
                    {
                        j++;
                    }

                    if (j <= maxTokens)
                    {
                        // append encountered tokens to the accumulatedIds
                        for (int k = tokenCount; k < j; k++)
                        {
                            accumulatedIds?.Add(tokens[k].Id);
                        }
                        tokenCount = j - 1;
                        charsConsumed = tokens[tokenCount].TokenIndex + tokens[tokenCount].TokenLength;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    accumulatedIds?.Add(tokens[tokenCount].Id);
                    charsConsumed = tokens[tokenCount].TokenIndex + tokens[tokenCount].TokenLength;
                }
            }

            return tokenCount;
        }

        /// <summary>
        /// Get the number of tokens that the input text will be encoded to.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="textSpan">The span of the text to encode which will be used if the <paramref name="text"/> is <see langword="null"/>.</param>
        /// <param name="settings">The settings used to encode the text.</param>
        /// <returns>The number of token Ids that the input text will be encoded to.</returns>
        protected override int CountTokens(string? text, ReadOnlySpan<char> textSpan, EncodeSettings settings)
            => CountTokens(text, textSpan, settings.ConsiderPreTokenization, settings.ConsiderNormalization, out _, out _, settings.MaxTokenCount);

        private int CountTokens(string? text, ReadOnlySpan<char> textSpan, bool considerPreTokenization, bool considerNormalization, out string? normalizedText, out int charsConsumed, int maxTokenCount = int.MaxValue)
        {
            if (maxTokenCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTokenCount), "The maximum number of tokens must be greater than zero.");
            }

            charsConsumed = 0;
            if (string.IsNullOrEmpty(text) && textSpan.IsEmpty)
            {
                normalizedText = null;
                return 0;
            }

            IEnumerable<(int Offset, int Length)>? splits = InitializeForEncoding(
                                                                text,
                                                                textSpan,
                                                                considerPreTokenization,
                                                                considerNormalization,
                                                                _normalizer, _preTokenizer,
                                                                out normalizedText,
                                                                out ReadOnlySpan<char> textSpanToEncode,
                                                                out _);

            int count = 0;
            if (splits is not null)
            {
                foreach ((int Offset, int Length) split in splits)
                {
                    count += CountTokens(textSpanToEncode.Slice(split.Offset, split.Length), out int length, maxTokenCount - count);
                    charsConsumed = split.Offset + length;

                    if (length < split.Length || count >= maxTokenCount)
                    {
                        break;
                    }
                }
            }
            else
            {
                count = CountTokens(textSpanToEncode, out charsConsumed, maxTokenCount);
            }

            return count;
        }

        /// <summary>
        /// Get the number of tokens that the input text will be encoded to.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="charsConsumed">The length of the text that encompasses the maximum encoded tokens.</param>
        /// <param name="maxTokens">The maximum number of tokens to encode.</param>
        /// <returns>The number of tokens that the input text will be encoded to.</returns>
        private int CountTokens(ReadOnlySpan<char> text, out int charsConsumed, int maxTokens = int.MaxValue)
        {
            Debug.Assert(maxTokens > 0);

            if (text.IsEmpty)
            {
                charsConsumed = 0;
                return 0;
            }

            if (_cache.TryGetValue(text, out (int Id, int TokenIndex, int TokenLength)[] value))
            {
                return EncodeToIdsResult(value, accumulatedIds: null, maxTokens, text.Length, out charsConsumed);
            }

            if (_vocab.TryGetValue(text, out _))
            {
                charsConsumed = text.Length;
                return 1;
            }

            int utf8Length = Encoding.UTF8.GetMaxByteCount(text.Length);
            byte[] arrayPoolArray = arrayPoolArray = ArrayPool<byte>.Shared.Rent(utf8Length);
            int[]? indexMappingArray = null;
            Span<int> indexMappingSpan = utf8Length + 1 <= 128 ? stackalloc int[128] : (indexMappingArray = ArrayPool<int>.Shared.Rent(utf8Length + 1));
            int encodedLength = Helpers.EncodeToUtf8(text, arrayPoolArray, indexMappingSpan);
            Debug.Assert(encodedLength < indexMappingSpan.Length);
            indexMappingSpan[encodedLength] = text.Length;

            (int Id, int TokenIndex, int TokenLength)[] encodedTokens = BytePairEncoder.BytePairEncode(arrayPoolArray.AsMemory(0, encodedLength), _encoder, indexMappingSpan.Slice(0, encodedLength + 1));
            ArrayPool<byte>.Shared.Return(arrayPoolArray);
            if (indexMappingArray is not null)
            {
                ArrayPool<int>.Shared.Return(indexMappingArray);
            }

            if (text.Length <= MaxWordLengthToCache)
            {
                string textAsString = text.ToString();
                _cache.Add(textAsString, encodedTokens);
            }

            return EncodeToIdsResult(encodedTokens, accumulatedIds: null, maxTokens, text.Length, out charsConsumed);
        }


        /// <summary>
        /// Find the index of the maximum encoding capacity without surpassing the token limit.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="textSpan">The span of the text to encode which will be used if the <paramref name="text"/> is <see langword="null"/>.</param>
        /// <param name="settings">The settings used to encode the text.</param>
        /// <param name="fromEnd">Indicate whether to find the index from the end of the text.</param>
        /// <param name="normalizedText">If the tokenizer's normalization is enabled or <paramRef name="settings" /> has <see cref="EncodeSettings.ConsiderNormalization"/> is <see langword="false"/>, this will be set to <paramRef name="text" /> in its normalized form; otherwise, this value will be set to <see langword="null"/>.</param>
        /// <param name="tokenCount">The token count can be generated which should be smaller than the maximum token count.</param>
        /// <returns>
        /// The index of the maximum encoding capacity within the processed text without surpassing the token limit.
        /// If <paramRef name="fromEnd" /> is <see langword="false"/>, it represents the index immediately following the last character to be included. In cases where no tokens fit, the result will be 0; conversely,
        /// if all tokens fit, the result will be length of the input text or the <paramref name="normalizedText"/> if the normalization is enabled.
        /// If <paramRef name="fromEnd" /> is <see langword="true"/>, it represents the index of the first character to be included. In cases where no tokens fit, the result will be the text length; conversely,
        /// if all tokens fit, the result will be zero.
        /// </returns>
        protected override int GetIndexByTokenCount(string? text, ReadOnlySpan<char> textSpan, EncodeSettings settings, bool fromEnd, out string? normalizedText, out int tokenCount)
        {
            if (fromEnd)
            {
                return LastIndexOf(text, textSpan, settings.MaxTokenCount, settings.ConsiderNormalization, settings.ConsiderNormalization, out normalizedText, out tokenCount);
            }

            tokenCount = CountTokens(text, textSpan, settings.ConsiderPreTokenization, settings.ConsiderNormalization, out normalizedText, out int charsConsumed, settings.MaxTokenCount);
            return charsConsumed;
        }

        private int LastIndexOf(string? text, ReadOnlySpan<char> textSpan, int maxTokenCount, bool considerPreTokenization, bool considerNormalization, out string? normalizedText, out int tokenCount)
        {
            if (maxTokenCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTokenCount), "The max token count must be greater than 0.");
            }

            if (string.IsNullOrEmpty(text) && textSpan.IsEmpty)
            {
                normalizedText = null;
                tokenCount = 0;
                return 0;
            }

            IEnumerable<(int Offset, int Length)>? splits = InitializeForEncoding(
                                                                text,
                                                                textSpan,
                                                                considerPreTokenization,
                                                                considerNormalization,
                                                                _normalizer,
                                                                _preTokenizer,
                                                                out normalizedText,
                                                                out ReadOnlySpan<char> textSpanToEncode,
                                                                out _);

            if (splits is not null)
            {
                tokenCount = 0;
                foreach ((int Offset, int Length) split in splits.Reverse())
                {
                    tokenCount += CountTokensFromEnd(textSpanToEncode.Slice(split.Offset, split.Length), out int textIndex, maxTokenCount - tokenCount);
                    if (textIndex > 0 || tokenCount >= maxTokenCount)
                    {
                        return split.Offset + textIndex;
                    }
                }

                return 0;
            }
            else
            {
                tokenCount = CountTokensFromEnd(textSpanToEncode, out int charsConsumed, maxTokenCount);
                return charsConsumed;
            }
        }

        /// <summary>
        /// Get the number of tokens that the input text will be encoded to.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <param name="textIndex">Starting from this index to the end of the text will encompasses the maximum encoded tokens.</param>
        /// <param name="maxTokens">The maximum number of tokens to encode.</param>
        /// <returns>The number of tokens that the input text will be encoded to.</returns>
        private int CountTokensFromEnd(ReadOnlySpan<char> text, out int textIndex, int maxTokens = int.MaxValue)
        {
            Debug.Assert(maxTokens > 0);

            if (text.IsEmpty)
            {
                textIndex = 0;
                return 0;
            }

            if (_cache.TryGetValue(text, out (int Id, int TokenIndex, int TokenLength)[] value))
            {
                return EncodeToIdsFromEndResult(value, accumulatedIds: null, maxTokens, text.Length, out textIndex);
            }

            if (_vocab.TryGetValue(text, out _))
            {
                textIndex = 0;
                return 1;
            }

            int utf8Length = Encoding.UTF8.GetMaxByteCount(text.Length);
            byte[] arrayPoolArray = arrayPoolArray = ArrayPool<byte>.Shared.Rent(utf8Length);
            int[]? indexMappingArray = null;
            Span<int> indexMappingSpan = utf8Length + 1 <= 128 ? stackalloc int[128] : (indexMappingArray = ArrayPool<int>.Shared.Rent(utf8Length + 1));
            int encodedLength = Helpers.EncodeToUtf8(text, arrayPoolArray, indexMappingSpan);
            Debug.Assert(encodedLength < indexMappingSpan.Length);
            indexMappingSpan[encodedLength] = text.Length;

            (int Id, int TokenIndex, int TokenLength)[] encodedTokens = BytePairEncoder.BytePairEncode(arrayPoolArray.AsMemory(0, encodedLength), _encoder, indexMappingSpan.Slice(0, encodedLength + 1));
            ArrayPool<byte>.Shared.Return(arrayPoolArray);
            if (indexMappingArray is not null)
            {
                ArrayPool<int>.Shared.Return(indexMappingArray);
            }

            if (text.Length <= MaxWordLengthToCache)
            {
                string textAsString = text.ToString();
                _cache.Add(textAsString, encodedTokens);
            }

            return EncodeToIdsFromEndResult(encodedTokens, accumulatedIds: null, maxTokens, text.Length, out textIndex);
        }

        private int EncodeToIdsFromEndResult((int Id, int TokenIndex, int TokenLength)[] tokens, IList<int>? accumulatedIds, int maxTokens, int fullTextLength, out int textIndex)
        {
            textIndex = fullTextLength;

            if (tokens.Length <= maxTokens)
            {
                if (accumulatedIds is not null)
                {
                    foreach (var t in tokens)
                    {
                        accumulatedIds.Add(t.Id);
                    }
                }

                textIndex = 0;
                return tokens.Length;
            }

            int index = tokens.Length - maxTokens;

            // avoid breaking the text in the middle of a code-point
            while (index < tokens.Length && tokens[index].TokenIndex < tokens[index - 1].TokenIndex + tokens[index - 1].TokenLength)
            {
                index++;
            }

            for (int i = index; i < tokens.Length; i++)
            {
                accumulatedIds?.Add(tokens[i].Id);
            }

            textIndex = index >= tokens.Length ? fullTextLength : tokens[index].TokenIndex;
            return tokens.Length - index;
        }

        /// <summary>
        /// Decode the given ids, back to a String.
        /// </summary>
        /// <param name="ids">The list of ids that we want to decode.</param>
        /// <returns>The decoded string.</returns>
        public override string Decode(IEnumerable<int> ids)
        {
            // Tiktoken doesn't guarantee a one-to-one correspondence between IDs and UTF-16 words.
            // Consequently, decoding individual IDs into UTF-16 string is not supported; instead, decoding all IDs must be performed collectively.
            // Here's an example case that maps one character to multiple IDs:
            // '⭐' U-2B50 is mapped to IDs [2928, 99834] in the Tiktoken model.
            // In other words, the character '⭐' with UTF-8 code point 0xE2, 0xAD, 0x90 will be mapped by Tiktoken as follows: 0xE2 to [2928]
            // and 0xAD, 0x90 to [99834]. Decoding 2928 and 99834 individually won't reconstruct the original UTF-16 string '⭐' U-2B50;
            // decoding all IDs together is required to get the expected result.
            if (ids is null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            byte[]? arrayPoolArray = null;
            try
            {
                Span<byte> utf8Bytes = stackalloc byte[256];
                int utf8ByteCount = 0;

                foreach (int id in ids)
                {
                    if (_decoder.TryGetValue(id, out ReadOnlyMemory<byte> tokenBytes))
                    {
                        if ((uint)utf8ByteCount + (uint)tokenBytes.Length > (uint)utf8Bytes.Length)
                        {
                            ArrayPoolGrow(ref utf8Bytes, ref arrayPoolArray, utf8ByteCount + tokenBytes.Length);
                        }

                        tokenBytes.Span.CopyTo(utf8Bytes.Slice(utf8ByteCount));
                        utf8ByteCount += tokenBytes.Length;
                    }
                }

                return Helpers.GetString(utf8Bytes.Slice(0, utf8ByteCount));
            }
            finally
            {
                if (arrayPoolArray is not null)
                {
                    ArrayPool<byte>.Shared.Return(arrayPoolArray);
                }
            }

            static void ArrayPoolGrow(ref Span<byte> utf8Bytes, ref byte[]? arrayPoolArray, int requiredCapacity)
            {
                byte[] tmp = ArrayPool<byte>.Shared.Rent(Math.Max(utf8Bytes.Length * 2, requiredCapacity));
                utf8Bytes.CopyTo(tmp.AsSpan());
                byte[]? toReturn = arrayPoolArray;
                utf8Bytes = arrayPoolArray = tmp;
                if (toReturn is not null)
                {
                    ArrayPool<byte>.Shared.Return(toReturn);
                }
            }
        }

        /// <summary>
        /// Decode the given ids back to text and store the result in the <paramref name="destination"/> span.
        /// </summary>
        /// <param name="ids">The list of ids that we want to decode.</param>
        /// <param name="destination">The span to store the decoded text.</param>
        /// <param name="idsConsumed">The number of ids consumed during the decoding.</param>
        /// <param name="charsWritten">The number of characters written to the destination span.</param>
        /// <returns>The operation status indicates whether all IDs were successfully decoded or if the <paramref name="destination"/> is too small to contain the entire decoded result.</returns>
        public override OperationStatus Decode(IEnumerable<int> ids, Span<char> destination, out int idsConsumed, out int charsWritten)
        {
            idsConsumed = 0;
            charsWritten = 0;

            // Tiktoken doesn't guarantee a one-to-one correspondence between IDs and UTF-16 words.
            // Consequently, decoding individual IDs into UTF-16 string is not supported; instead, decoding all IDs must be performed collectively.
            // Here's an example case that maps one character to multiple IDs:
            // '⭐' U-2B50 is mapped to IDs [2928, 99834] in the Tiktoken model.
            // In other words, the character '⭐' with UTF-8 code point 0xE2, 0xAD, 0x90 will be mapped by Tiktoken as follows: 0xE2 to [2928]
            // and 0xAD, 0x90 to [99834]. Decoding 2928 and 99834 individually won't reconstruct the original UTF-16 string '⭐' U-2B50;
            // decoding all IDs together is required to get the expected result.
            if (ids is null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            // should be far enough to store incomplete utf-8 sequences
            Span<byte> utf8Bytes = stackalloc byte[256];
            // should be far enough to store one vocabulary token at time
            Span<char> tempBuffer = stackalloc char[256];

            int utf8BytesIncompleteCount = 0;
            int utf8BytesIncompleteIndex = 0;
            int incompleteCharsWritten = 0;
            int hangingIdsCount = 0;

            Span<char> buffer = destination;

            foreach (int id in ids)
            {
                if (_decoder.TryGetValue(id, out ReadOnlyMemory<byte> tokenBytes))
                {
                    if (utf8BytesIncompleteCount + tokenBytes.Length > utf8Bytes.Length)
                    {
                        // Its unexpected to have a token that is larger than the utf8Bytes buffer.
                        return OperationStatus.InvalidData;
                    }

                    if (Encoding.UTF8.GetMaxCharCount(utf8BytesIncompleteCount + tokenBytes.Length) > tempBuffer.Length)
                    {
                        return OperationStatus.DestinationTooSmall;
                    }

                    if (utf8BytesIncompleteCount == 0)
                    {
                        // No incomplete utf-8 sequence currently recorded. Try to decode tokenBytes directly.

                        Debug.Assert(incompleteCharsWritten == 0);
                        Debug.Assert(hangingIdsCount == 0);
                        Debug.Assert(utf8BytesIncompleteIndex == 0);

                        if (!Helpers.ConvertUtf8ToUtf16(tokenBytes.Span, tempBuffer, out int bytesConsumed, out incompleteCharsWritten))
                        {
                            return OperationStatus.InvalidData;
                        }

                        if (incompleteCharsWritten > buffer.Length)
                        {
                            return OperationStatus.DestinationTooSmall;
                        }

                        tempBuffer.Slice(0, incompleteCharsWritten).CopyTo(buffer);
                        buffer = buffer.Slice(incompleteCharsWritten);

                        if (bytesConsumed == tokenBytes.Length)
                        {
                            // Encoding is complete
                            charsWritten += incompleteCharsWritten;
                            idsConsumed++;
                            incompleteCharsWritten = 0;
                        }
                        else
                        {
                            // Encoding is incomplete
                            utf8BytesIncompleteCount = tokenBytes.Length - bytesConsumed;
                            tokenBytes.Span.Slice(bytesConsumed).CopyTo(utf8Bytes);
                            hangingIdsCount = 1;
                        }
                    }
                    else
                    {
                        // Previously, we had an incomplete utf-8 sequence. Try to complete it first.
                        tokenBytes.Span.CopyTo(utf8Bytes.Slice(utf8BytesIncompleteIndex + utf8BytesIncompleteCount));

                        if (!Helpers.ConvertUtf8ToUtf16(utf8Bytes.Slice(utf8BytesIncompleteIndex, utf8BytesIncompleteCount + tokenBytes.Length), tempBuffer, out int bytesConsumed, out int charsConsumed))
                        {
                            return OperationStatus.InvalidData;
                        }

                        if (charsConsumed > buffer.Length)
                        {
                            return OperationStatus.DestinationTooSmall;
                        }

                        tempBuffer.Slice(0, charsConsumed).CopyTo(buffer);
                        buffer = buffer.Slice(charsConsumed);

                        if (bytesConsumed == utf8BytesIncompleteCount + tokenBytes.Length)
                        {
                            // Encoding is complete
                            charsWritten += charsConsumed + incompleteCharsWritten;
                            idsConsumed += hangingIdsCount + 1;
                            hangingIdsCount = 0;
                            utf8BytesIncompleteCount = 0;
                            utf8BytesIncompleteIndex = 0;
                            incompleteCharsWritten = 0;
                        }
                        else
                        {
                            // Encoding is incomplete
                            utf8BytesIncompleteIndex += bytesConsumed;
                            utf8BytesIncompleteCount = utf8BytesIncompleteCount + tokenBytes.Length - bytesConsumed;
                            hangingIdsCount++;
                            incompleteCharsWritten += charsConsumed;
                        }
                    }
                }
                else
                {
                    return OperationStatus.InvalidData;
                }
            }

            return utf8BytesIncompleteCount != 0 ? OperationStatus.NeedMoreData : OperationStatus.Done;
        }

        /// <summary>
        /// Gets the dictionary mapping tokens to Ids.
        /// </summary>
        /// <remarks>This may not contain the full set of vocabulary tokens, use Encoder to get the full set of vocabulary.</remarks>
        internal IReadOnlyDictionary<string, int> Vocabulary => _vocabOriginal ??= _vocab.ToDictionary(kvp => kvp.Key.Data!, kvp => kvp.Value.Id);

        /// <summary>
        /// Gets the dictionary mapping special tokens to Ids.
        /// </summary>
        public IReadOnlyDictionary<string, int>? SpecialTokens { get; }

        /// <summary>
        /// Gets the dictionary mapping token bytes to Ids.
        /// </summary>
        internal IReadOnlyDictionary<ReadOnlyMemory<byte>, int> Encoder => _encoder;

        /// <summary>
        /// Gets the dictionary mapping Ids to token utf-8 bytes.
        /// </summary>
        internal IReadOnlyDictionary<int, ReadOnlyMemory<byte>> Decoder => _decoder;

        private const string EndOfText = "<|endoftext|>";
        private const string FimPrefix = "<|fim_prefix|>";
        private const string FimMiddle = "<|fim_middle|>";
        private const string FimSuffix = "<|fim_suffix|>";
        private const string EndOfPrompt = "<|endofprompt|>";
        private const string IMStart = "<|im_start|>";
        private const string IMEnd = "<|im_end|>";
        private const string IMSep = "<|im_sep|>";
        private const string StartOfText = "<|startoftext|>";
        private const string Return = "<|return|>";
        private const string Constrain = "<|constrain|>";
        private const string Channel = "<|channel|>";
        private const string Start = "<|start|>";
        private const string End = "<|end|>";
        private const string Message = "<|message|>";
        private const string Call = "<|call|>";
        private const string ReservedPrefix = "<|reserved_";

        private enum ModelEncoding
        {
            None,
            Cl100kBase,
            P50kBase,
            P50kEdit,
            R50kBase,
            GPT2,
            O200kBase,
            O200kHarmony
        }

        private const string Phi4ModelName = "phi-4";

        private static readonly (string Prefix, ModelEncoding Encoding)[] _modelPrefixToEncoding =
                                                            [
                                                                ( "o1-", ModelEncoding.O200kBase ),       // e.g. o1-mini
                                                                ( "o3-", ModelEncoding.O200kBase ),       // e.g. o3-mini
                                                                ( "o4-mini-", ModelEncoding.O200kBase ),  // e.g. o4-mini

                                                                // chat
                                                                ( "gpt-5.3-", ModelEncoding.O200kBase ),
                                                                ( "gpt-5.2-", ModelEncoding.O200kBase ),
                                                                ( "gpt-5.1-", ModelEncoding.O200kBase ),
                                                                ( "gpt-5-", ModelEncoding.O200kBase ),
                                                                ( "gpt-4.1-", ModelEncoding.O200kBase ),   // e.g., gpt-4.1-mini
                                                                ( "gpt-4.5-", ModelEncoding.O200kBase ),   // e.g., gpt-4.5
                                                                ( "gpt-4o-", ModelEncoding.O200kBase ),    // e.g., gpt-4o-2024-05-13
                                                                ( "chatgpt-4o-", ModelEncoding.O200kBase ),
                                                                ( "gpt-4-", ModelEncoding.Cl100kBase ),    // e.g., gpt-4-0314, etc., plus gpt-4-32k
                                                                ( "gpt-3.5-", ModelEncoding.Cl100kBase ),  // e.g, gpt-3.5-turbo-0301, -0401, etc.
                                                                ( "gpt-35-", ModelEncoding.Cl100kBase ),  // Azure deployment name
                                                                ( "gpt-oss-", ModelEncoding.O200kHarmony ),

                                                                // fine-tuned
                                                                ( "ft:gpt-4o", ModelEncoding.O200kBase ),
                                                                ( "ft:gpt-4", ModelEncoding.Cl100kBase ),
                                                                ( "ft:gpt-3.5-turbo", ModelEncoding.Cl100kBase ),
                                                                ( "ft:davinci-002", ModelEncoding.Cl100kBase ),
                                                                ( "ft:babbage-002", ModelEncoding.Cl100kBase ),
                                                            ];

        private static readonly Dictionary<string, ModelEncoding> _modelToEncoding =
                                                            new Dictionary<string, ModelEncoding>(StringComparer.OrdinalIgnoreCase)
                                                            {
                                                                // reasoning
                                                                { "o1", ModelEncoding.O200kBase },
                                                                { "o3", ModelEncoding.O200kBase },
                                                                { "o4-mini", ModelEncoding.O200kBase },

                                                                // chat
                                                                { "gpt-5.3", ModelEncoding.O200kBase },
                                                                { "gpt-5.2", ModelEncoding.O200kBase },
                                                                { "gpt-5.1", ModelEncoding.O200kBase },
                                                                { "gpt-5", ModelEncoding.O200kBase },
                                                                { "gpt-4.1", ModelEncoding.O200kBase },
                                                                { "gpt-4o", ModelEncoding.O200kBase },
                                                                { "gpt-4", ModelEncoding.Cl100kBase },
                                                                { "gpt-3.5-turbo", ModelEncoding.Cl100kBase },
                                                                { "gpt-3.5", ModelEncoding.Cl100kBase },
                                                                { "gpt-3.5-turbo-16k", ModelEncoding.Cl100kBase },
                                                                { "gpt-35", ModelEncoding.Cl100kBase },           // Azure deployment name
                                                                { "gpt-35-turbo", ModelEncoding.Cl100kBase },     // Azure deployment name
                                                                { "gpt-35-turbo-16k", ModelEncoding.Cl100kBase }, // Azure deployment name

                                                                // Base
                                                                { "davinci-002", ModelEncoding.Cl100kBase },
                                                                { "babbage-002", ModelEncoding.Cl100kBase },

                                                                // embeddings
                                                                // https://platform.openai.com/docs/guides/embeddings/what-are-embeddings
                                                                { "text-embedding-ada-002", ModelEncoding.Cl100kBase },
                                                                { "text-embedding-3-small", ModelEncoding.Cl100kBase },
                                                                { "text-embedding-3-large", ModelEncoding.Cl100kBase },

                                                                // DEPRECATED MODELS
                                                                // text (DEPRECATED)
                                                                { "text-davinci-003", ModelEncoding.P50kBase },
                                                                { "text-davinci-002", ModelEncoding.P50kBase },
                                                                { "text-davinci-001", ModelEncoding.R50kBase },
                                                                { "text-curie-001", ModelEncoding.R50kBase },
                                                                { "text-babbage-001", ModelEncoding.R50kBase },
                                                                { "text-ada-001", ModelEncoding.R50kBase },
                                                                { "davinci", ModelEncoding.R50kBase },
                                                                { "curie", ModelEncoding.R50kBase },
                                                                { "babbage", ModelEncoding.R50kBase },
                                                                { "ada", ModelEncoding.R50kBase },

                                                                // code (DEPRECATED)
                                                                { "code-davinci-002", ModelEncoding.P50kBase },
                                                                { "code-davinci-001", ModelEncoding.P50kBase },
                                                                { "code-cushman-002", ModelEncoding.P50kBase },
                                                                { "code-cushman-001", ModelEncoding.P50kBase },
                                                                { "davinci-codex", ModelEncoding.P50kBase },
                                                                { "cushman-codex", ModelEncoding.P50kBase },

                                                                // edit (DEPRECATED)
                                                                { "text-davinci-edit-001", ModelEncoding.P50kEdit },
                                                                { "code-davinci-edit-001", ModelEncoding.P50kEdit },


                                                                // old embeddings (DEPRECATED)
                                                                { "text-similarity-davinci-001", ModelEncoding.R50kBase },
                                                                { "text-similarity-curie-001", ModelEncoding.R50kBase },
                                                                { "text-similarity-babbage-001", ModelEncoding.R50kBase },
                                                                { "text-similarity-ada-001", ModelEncoding.R50kBase },
                                                                { "text-search-davinci-doc-001", ModelEncoding.R50kBase },
                                                                { "text-search-curie-doc-001", ModelEncoding.R50kBase },
                                                                { "text-search-babbage-doc-001", ModelEncoding.R50kBase },
                                                                { "text-search-ada-doc-001", ModelEncoding.R50kBase },
                                                                { "code-search-babbage-code-001", ModelEncoding.R50kBase },
                                                                { "code-search-ada-code-001", ModelEncoding.R50kBase },

                                                                // open source
                                                                { "gpt2", ModelEncoding.GPT2 },
                                                                { "gpt-2", ModelEncoding.GPT2 },

                                                                // phi-4
                                                                { Phi4ModelName, ModelEncoding.Cl100kBase },
                                                            };

        private static ModelEncoding GetModelEncoding(string modelName)
        {
            if (!_modelToEncoding.TryGetValue(modelName, out ModelEncoding encoder))
            {
                foreach ((string Prefix, ModelEncoding Encoding) in _modelPrefixToEncoding)
                {
                    if (modelName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        encoder = Encoding;
                        break;
                    }
                }
            }

            if (encoder == ModelEncoding.None)
            {
                throw new NotSupportedException($"The model '{modelName}' is not supported.");
            }

            return encoder;
        }

        private static Dictionary<string, int> CreateHarmonyEncodingSpecialTokens() =>
            new Dictionary<string, int>
            {
                { StartOfText,                  199998 },
                { EndOfText,                    199999 },
                { $"{ReservedPrefix}200000|>",  200000 },
                { $"{ReservedPrefix}200001|>",  200001 },
                { Return,                       200002 },
                { Constrain,                    200003 },
                { $"{ReservedPrefix}200004|>",  200004 },
                { Channel,                      200005 },
                { Start,                        200006 },
                { End,                          200007 },
                { Message,                      200008 },
                { $"{ReservedPrefix}200009|>",  200009 },
                { $"{ReservedPrefix}200010|>",  200010 },
                { $"{ReservedPrefix}200011|>",  200011 },
                { Call,                         200012 },
                { $"{ReservedPrefix}200013|>",  200013 },
                { $"{ReservedPrefix}200014|>",  200014 },
                { $"{ReservedPrefix}200015|>",  200015 },
                { $"{ReservedPrefix}200016|>",  200016 },
                { $"{ReservedPrefix}200017|>",  200017 },
                { EndOfPrompt,                  200018 },
            };

        private static (Dictionary<string, int> SpecialTokens, Regex Regex, string VocabFile, Type? DataType, string PackageName) GetTiktokenConfigurations(string modelName) => GetTiktokenConfigurations(GetModelEncoding(modelName), modelName);

        private static (Dictionary<string, int> SpecialTokens, Regex Regex, string VocabFile, Type? DataType, string PackageName) GetTiktokenConfigurations(ModelEncoding modelEncoding, string? modelName = null)
        {
            switch (modelEncoding)
            {
                case ModelEncoding.Cl100kBase:
                    return (
                        Phi4ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase) ?
                        new Dictionary<string, int> { { EndOfText, 100257 }, { FimPrefix, 100258 }, { FimMiddle, 100259 }, { FimSuffix, 100260 }, { EndOfPrompt, 100276 }, { IMStart, 100264 },
                                                      { IMEnd, 100265 }, { IMSep, 100266 }, { "<|dummy_85|>", 100349}, // <|dummy_85|> is used for padding according to the phi-4 special token mapping.
                        } :
                        new Dictionary<string, int> { { EndOfText, 100257 }, { FimPrefix, 100258 }, { FimMiddle, 100259 }, { FimSuffix, 100260 }, { EndOfPrompt, 100276 } },
                        Cl100kBaseRegex(), Cl100kBaseVocabFile, Type.GetType(Cl100kBaseTypeName), Cl100kBasePackageName);

                case ModelEncoding.GPT2:
                    return (new Dictionary<string, int> { { EndOfText, 50256 }, }, P50kBaseRegex(), GPT2File, Type.GetType(Gpt2TypeName), Gpt2PackageName);

                case ModelEncoding.O200kBase:
                    return (new Dictionary<string, int> { { EndOfText, 199999 }, { EndOfPrompt, 200018 } }, O200kBaseRegex(), O200kBaseFile, Type.GetType(O200kBaseTypeName), O200kBasePackageName);

                case ModelEncoding.P50kBase:
                    return (new Dictionary<string, int> { { EndOfText, 50256 } }, P50kBaseRegex(), P50RanksFile, Type.GetType(P50kBaseTypeName), P50kBasePackageName);

                case ModelEncoding.P50kEdit:
                    return (new Dictionary<string, int>
                        { { EndOfText, 50256 }, { FimPrefix, 50281 }, { FimMiddle, 50282 }, { FimSuffix, 50283 } }, P50kBaseRegex(), P50RanksFile, Type.GetType(P50kBaseTypeName), P50kBasePackageName);

                case ModelEncoding.R50kBase:
                    return (new Dictionary<string, int> { { EndOfText, 50256 } }, P50kBaseRegex(), R50RanksFile, Type.GetType(R50kBaseTypeName), R50kBasePackageName);

                case ModelEncoding.O200kHarmony:
                    return (CreateHarmonyEncodingSpecialTokens(), O200kBaseRegex(), O200kBaseFile, Type.GetType(O200kBaseTypeName), O200kBasePackageName);

                default:
                    throw new NotSupportedException($"The model '{modelName ?? modelEncoding.ToString()}' is not supported.");
            }
        }

        // Regex patterns based on https://github.com/openai/tiktoken/blob/main/tiktoken_ext/openai_public.py

        private const string Cl100kBaseRegexPattern = /*lang=regex*/ @"'(?i:[sdmt]|ll|ve|re)|(?>[^\r\n\p{L}\p{N}]?)(?>\p{L}+)|(?>\p{N}{1,3})| ?(?>[^\s\p{L}\p{N}]+)(?>[\r\n]*)|(?>\s+)$|\s*[\r\n]|\s+(?!\S)|\s";
        private const string P50kBaseRegexPattern = /*lang=regex*/ @"'(?:[sdmt]|ll|ve|re)| ?(?>\p{L}+)| ?(?>\p{N}+)| ?(?>[^\s\p{L}\p{N}]+)|(?>\s+)$|\s+(?!\S)|\s";
        private const string O200kBaseRegexPattern = /*lang=regex*/ @"[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]*[\p{Ll}\p{Lm}\p{Lo}\p{M}]+(?i:'s|'t|'re|'ve|'m|'ll|'d)?|[^\r\n\p{L}\p{N}]?[\p{Lu}\p{Lt}\p{Lm}\p{Lo}\p{M}]+[\p{Ll}\p{Lm}\p{Lo}\p{M}]*(?i:'s|'t|'re|'ve|'m|'ll|'d)?|\p{N}{1,3}| ?[^\s\p{L}\p{N}]+[\r\n/]*|\s*[\r\n]+|\s+(?!\S)|\s+";

        private const string Cl100kBaseVocabFile = "cl100k_base.tiktoken.deflate";  // "https://openaipublic.blob.core.windows.net/encodings/cl100k_base.tiktoken"
        private const string P50RanksFile = "p50k_base.tiktoken.deflate";           // "https://openaipublic.blob.core.windows.net/encodings/p50k_base.tiktoken"
        private const string R50RanksFile = "r50k_base.tiktoken.deflate";           // "https://openaipublic.blob.core.windows.net/encodings/r50k_base.tiktoken"
        private const string GPT2File = "gpt2.tiktoken.deflate";                    // "https://openaipublic.blob.core.windows.net/encodings/r50k_base.tiktoken". Gpt2 is using the same encoding as R50kBase
        private const string O200kBaseFile = "o200k_base.tiktoken.deflate";         // "https://openaipublic.blob.core.windows.net/encodings/o200k_base.tiktoken"

        internal const string Cl100kBaseEncodingName = "cl100k_base";
        internal const string P50kBaseEncodingName = "p50k_base";
        internal const string P50kEditEncodingName = "p50k_edit";
        internal const string R50kBaseEncodingName = "r50k_base";
        internal const string O200kBaseEncodingName = "o200k_base";
        internal const string O200kHarmonyEncodingName = "o200k_harmony";

        internal const string Cl100kBasePackageName = "Microsoft.ML.Tokenizers.Data.Cl100kBase";
        internal const string Gpt2PackageName = "Microsoft.ML.Tokenizers.Data.Gpt2";
        internal const string P50kBasePackageName = "Microsoft.ML.Tokenizers.Data.P50kBase";
        internal const string R50kBasePackageName = "Microsoft.ML.Tokenizers.Data.R50kBase";
        internal const string O200kBasePackageName = "Microsoft.ML.Tokenizers.Data.O200kBase";

        internal const string Cl100kBaseTypeName = "Microsoft.ML.Tokenizers.Cl100kBaseTokenizerData, Microsoft.ML.Tokenizers.Data.Cl100kBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string Gpt2TypeName = "Microsoft.ML.Tokenizers.Gpt2TokenizerData, Microsoft.ML.Tokenizers.Data.Gpt2, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string O200kBaseTypeName = "Microsoft.ML.Tokenizers.O200kBaseTokenizerData, Microsoft.ML.Tokenizers.Data.O200kBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string P50kBaseTypeName = "Microsoft.ML.Tokenizers.P50kBaseTokenizerData, Microsoft.ML.Tokenizers.Data.P50kBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        internal const string R50kBaseTypeName = "Microsoft.ML.Tokenizers.R50kBaseTokenizerData, Microsoft.ML.Tokenizers.Data.R50kBase, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";

#if NET7_0_OR_GREATER
        [GeneratedRegex(Cl100kBaseRegexPattern, RegexOptions.None, PreTokenizer.DefaultTimeOutInMilliseconds)]
        private static partial Regex Cl100kBaseRegex();

        [GeneratedRegex(P50kBaseRegexPattern, RegexOptions.None, PreTokenizer.DefaultTimeOutInMilliseconds)]
        internal static partial Regex P50kBaseRegex();

        [GeneratedRegex(O200kBaseRegexPattern, RegexOptions.None, PreTokenizer.DefaultTimeOutInMilliseconds)]
        internal static partial Regex O200kBaseRegex();
#else
        private static Regex? _cl100kBaseRegex;
        private static Regex Cl100kBaseRegex() => _cl100kBaseRegex ??= new Regex(Cl100kBaseRegexPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(PreTokenizer.DefaultTimeOutInMilliseconds));

        private static Regex? _p50kBaseRegex;
        internal static Regex P50kBaseRegex() => _p50kBaseRegex ??= new Regex(P50kBaseRegexPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(PreTokenizer.DefaultTimeOutInMilliseconds));

        private static Regex? _o200kBaseRegex;
        internal static Regex O200kBaseRegex() => _o200kBaseRegex ??= new Regex(O200kBaseRegexPattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(PreTokenizer.DefaultTimeOutInMilliseconds));
#endif

        private static readonly ConcurrentDictionary<string, (Dictionary<ReadOnlyMemory<byte>, int> encoder, Dictionary<StringSpanOrdinalKey, (int Id, string Token)> vocab, Dictionary<int, ReadOnlyMemory<byte>> decoder)> _tiktokenCache = new(StringComparer.OrdinalIgnoreCase);

        //
        // Creation Factory Methods
        //

        private static TiktokenTokenizer CreateForModel(
                                    ModelEncoding modelEncoding,
                                    string? modelName = null,
                                    IReadOnlyDictionary<string, int>? extraSpecialTokens = null,
                                    Normalizer? normalizer = null)
        {
            (Dictionary<string, int> SpecialTokens, Regex Regex, string VocabFile, Type? DataType, string PackageName) tiktokenConfiguration = GetTiktokenConfigurations(modelEncoding, modelName);

            if (extraSpecialTokens is not null)
            {
                foreach (var extraSpecialToken in extraSpecialTokens)
                {
                    tiktokenConfiguration.SpecialTokens.Add(extraSpecialToken.Key, extraSpecialToken.Value);
                }
            }

            if (!_tiktokenCache.TryGetValue(
                    tiktokenConfiguration.VocabFile,
                    out (Dictionary<ReadOnlyMemory<byte>, int> encoder, Dictionary<StringSpanOrdinalKey, (int Id, string Token)> vocab, Dictionary<int, ReadOnlyMemory<byte>> decoder) cache))
            {
                if (tiktokenConfiguration.DataType is null)
                {
                    throw new InvalidOperationException($"The tokenizer data file {tiktokenConfiguration.PackageName}.dll could not be loaded. Please reference the package {tiktokenConfiguration.PackageName} in your project.");
                }

                using Stream compressedStream = tiktokenConfiguration.DataType.Assembly!.GetManifestResourceStream(tiktokenConfiguration.VocabFile)!;
                using Stream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);

                cache = LoadTiktokenBpeAsync(deflateStream, useAsync: false).GetAwaiter().GetResult();

                _tiktokenCache.TryAdd(tiktokenConfiguration.VocabFile, cache);
            }

            return new TiktokenTokenizer(
                        cache.encoder,
                        cache.decoder,
                        cache.vocab,
                        new RegexPreTokenizer(tiktokenConfiguration.Regex, tiktokenConfiguration.SpecialTokens),
                        tiktokenConfiguration.SpecialTokens,
                        normalizer,
                        LruCache<int[]>.DefaultCacheSize);
        }

        /// <summary>
        /// Create a new Tiktoken tokenizer's object.
        /// </summary>
        /// <param name="vocabFilePath">The BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <returns>The tokenizer's object.</returns>
        /// <remarks>
        /// When creating the tokenizer, ensure that the vocabulary file is sourced from a trusted provider.
        /// </remarks>
        public static TiktokenTokenizer Create(
                                string vocabFilePath,
                                PreTokenizer? preTokenizer,
                                Normalizer? normalizer,
                                IReadOnlyDictionary<string, int>? specialTokens = null,
                                int cacheSize = LruCache<int[]>.DefaultCacheSize)
            => new TiktokenTokenizer(vocabFilePath, preTokenizer, specialTokens, normalizer, cacheSize);

        /// <summary>
        /// Create a new Tiktoken tokenizer's object.
        /// </summary>
        /// <param name="vocabStream">The stream to the BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <returns>The tokenizer's object.</returns>
        /// <remarks>
        /// When creating the tokenizer, ensure that the vocabulary stream is sourced from a trusted provider.
        /// </remarks>
        public static TiktokenTokenizer Create(
                                Stream vocabStream,
                                PreTokenizer? preTokenizer,
                                Normalizer? normalizer,
                                IReadOnlyDictionary<string, int>? specialTokens = null,
                                int cacheSize = LruCache<int[]>.DefaultCacheSize)
            => new TiktokenTokenizer(vocabStream, preTokenizer, specialTokens, normalizer, cacheSize);

        /// <summary>
        /// Create a new Tiktoken tokenizer's object asynchronously.
        /// </summary>
        /// <param name="vocabStream">The stream to the BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> used to request cancellation of the operation.</param>
        /// <returns>The tokenizer's object.</returns>
        /// <remarks>
        /// When creating the tokenizer, ensure that the vocabulary stream is sourced from a trusted provider.
        /// </remarks>
        public static async Task<TiktokenTokenizer> CreateAsync(
                            Stream vocabStream,
                            PreTokenizer? preTokenizer,
                            Normalizer? normalizer,
                            IReadOnlyDictionary<string, int>? specialTokens = null,
                            int cacheSize = LruCache<int[]>.DefaultCacheSize,
                            CancellationToken cancellationToken = default)
        {
            if (vocabStream is null)
            {
                throw new ArgumentNullException(nameof(vocabStream));
            }

            (Dictionary<ReadOnlyMemory<byte>, int> encoder, Dictionary<StringSpanOrdinalKey, (int Id, string Token)> vocab, Dictionary<int, ReadOnlyMemory<byte>> decoder) =
                        await LoadTiktokenBpeAsync(vocabStream, useAsync: true, cancellationToken).ConfigureAwait(false);

            return new TiktokenTokenizer(encoder, decoder, vocab, preTokenizer, specialTokens, normalizer, cacheSize);
        }

        /// <summary>
        /// Create a new Tiktoken tokenizer's object asynchronously.
        /// </summary>
        /// <param name="vocabFilePath">The BPE vocab file.</param>
        /// <param name="preTokenizer">The pre-tokenizer to use.</param>
        /// <param name="normalizer">The normalizer to use.</param>
        /// <param name="specialTokens">The dictionary mapping special tokens to Ids.</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> used to request cancellation of the operation.</param>
        /// <returns>The tokenizer's object.</returns>
        /// <remarks>
        /// When creating the tokenizer, ensure that the vocabulary file is sourced from a trusted provider.
        /// </remarks>
        public static async Task<TiktokenTokenizer> CreateAsync(
                                string vocabFilePath,
                                PreTokenizer? preTokenizer,
                                Normalizer? normalizer,
                                IReadOnlyDictionary<string, int>? specialTokens = null,
                                int cacheSize = LruCache<int[]>.DefaultCacheSize,
                                CancellationToken cancellationToken = default)
        {
            if (vocabFilePath is null)
            {
                throw new ArgumentNullException(nameof(vocabFilePath));
            }

            using Stream vocabStream = File.OpenRead(vocabFilePath);
            return await CreateAsync(vocabStream, preTokenizer, normalizer, specialTokens, cacheSize, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create a Tiktoken tokenizer based on model name and vocab file.
        /// </summary>
        /// <param name="modelName">Model name</param>
        /// <param name="vocabStream">The stream to the BPE vocab file.</param>
        /// <param name="extraSpecialTokens">Extra special tokens other than the built-in ones for the model</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <param name="normalizer">To normalize the text before tokenization</param>
        /// <returns>The tokenizer</returns>
        public static TiktokenTokenizer CreateForModel(
                                    string modelName,
                                    Stream vocabStream,
                                    IReadOnlyDictionary<string, int>? extraSpecialTokens = null,
                                    int cacheSize = LruCache<int[]>.DefaultCacheSize,
                                    Normalizer? normalizer = null)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            (Dictionary<string, int> SpecialTokens, Regex Regex, string _, Type? __, string ___) tiktokenConfiguration = GetTiktokenConfigurations(modelName);

            if (extraSpecialTokens is not null)
            {
                foreach (var extraSpecialToken in extraSpecialTokens)
                {
                    tiktokenConfiguration.SpecialTokens.Add(extraSpecialToken.Key, extraSpecialToken.Value);
                }
            }

            return new TiktokenTokenizer(vocabStream,
                            new RegexPreTokenizer(tiktokenConfiguration.Regex, tiktokenConfiguration.SpecialTokens),
                            tiktokenConfiguration.SpecialTokens,
                            normalizer,
                            cacheSize);
        }

        /// <summary>
        /// Create a Tiktoken tokenizer based on model name and vocab file.
        /// </summary>
        /// <param name="modelName">Model name</param>
        /// <param name="vocabStream">The stream to the BPE vocab file.</param>
        /// <param name="extraSpecialTokens">Extra special tokens other than the built-in ones for the model</param>
        /// <param name="cacheSize">The size of the cache to use.</param>
        /// <param name="normalizer">To normalize the text before tokenization</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> used to request cancellation of the operation.</param>
        /// <returns>The tokenizer</returns>
        public static async Task<TiktokenTokenizer> CreateForModelAsync(
                                    string modelName,
                                    Stream vocabStream,
                                    IReadOnlyDictionary<string, int>? extraSpecialTokens = null,
                                    int cacheSize = LruCache<int[]>.DefaultCacheSize,
                                    Normalizer? normalizer = null,
                                    CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(modelName))
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            (Dictionary<string, int> SpecialTokens, Regex Regex, string _, Type? __, string ___) tiktokenConfiguration = GetTiktokenConfigurations(modelName);

            if (extraSpecialTokens is not null)
            {
                foreach (var extraSpecialToken in extraSpecialTokens)
                {
                    tiktokenConfiguration.SpecialTokens.Add(extraSpecialToken.Key, extraSpecialToken.Value);
                }
            }

            return await CreateAsync(vocabStream,
                                new RegexPreTokenizer(tiktokenConfiguration.Regex, tiktokenConfiguration.SpecialTokens),
                                normalizer,
                                tiktokenConfiguration.SpecialTokens,
                                cacheSize, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create tokenizer based on model name
        /// </summary>
        /// <param name="modelName">Model name</param>
        /// <param name="extraSpecialTokens">Extra special tokens other than the built-in ones for the model</param>
        /// <param name="normalizer">To normalize the text before tokenization</param>
        /// <returns>The tokenizer</returns>
        public static TiktokenTokenizer CreateForModel(string modelName, IReadOnlyDictionary<string, int>? extraSpecialTokens = null, Normalizer? normalizer = null)
                        => CreateForModel(GetModelEncoding(modelName), modelName, extraSpecialTokens, normalizer);

        /// <summary>
        /// Create tokenizer based on encoding name
        /// </summary>
        /// <param name="encodingName">Encoding name</param>
        /// <param name="extraSpecialTokens">Extra special tokens other than the built-in ones for the encoding</param>
        /// <param name="normalizer">To normalize the text before tokenization</param>
        /// <returns>The tokenizer</returns>
        public static TiktokenTokenizer CreateForEncoding(string encodingName, IReadOnlyDictionary<string, int>? extraSpecialTokens = null, Normalizer? normalizer = null)
        {
            if (string.IsNullOrEmpty(encodingName))
            {
                throw new ArgumentNullException(nameof(encodingName));
            }

            ModelEncoding modelEncoding;
            if (encodingName.Equals(Cl100kBaseEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.Cl100kBase;
            }
            else if (encodingName.Equals(O200kBaseEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.O200kBase;
            }
            else if (encodingName.Equals(O200kHarmonyEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.O200kHarmony;
            }
            else if (encodingName.Equals(P50kBaseEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.P50kBase;
            }
            else if (encodingName.Equals(P50kEditEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.P50kEdit;
            }
            else if (encodingName.Equals(R50kBaseEncodingName, StringComparison.OrdinalIgnoreCase))
            {
                modelEncoding = ModelEncoding.R50kBase;
            }
            else
            {
                throw new ArgumentException($"The encoding name '{encodingName}' is not supported. The only supported encoding names are: {TiktokenTokenizer.Cl100kBaseEncodingName}, {TiktokenTokenizer.P50kBaseEncodingName}, {TiktokenTokenizer.P50kEditEncodingName}, and {TiktokenTokenizer.R50kBaseEncodingName}.", nameof(encodingName));
            }

            return CreateForModel(modelEncoding, modelName: null, extraSpecialTokens, normalizer);
        }
    }
}
