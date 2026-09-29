# SharpFuzz findings

Results from fuzzing dotnet/runtime `main` (commit `48f53a10e`) with [SharpFuzz](https://github.com/Metalnem/sharpfuzz) and libFuzzer on Linux x64. The runtime was built from source (`clr+libs`, Release runtime, Debug libraries) and the fuzz targets live in `src/libraries/Fuzzing/DotnetFuzzing/Fuzzers`.

Every finding links to a standalone repro. They're [file-based apps](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps), so all you need is a .NET 10 or newer SDK:

```sh
cd src/libraries/Fuzzing/Findings/repros
dotnet run 03-Tensor-ToString-OutOfBoundsRead.cs
```

Each repro prints what it observed next to what was expected and ends with `REPRODUCED` or `NOT REPRODUCED`. The ones that need a NuGet package pin the 11.0 RC1 version with `#:package`, so they show the bug in shipped bits. This folder has its own `global.json`, `NuGet.config` and empty `Directory.Build.*` files so the repros don't pick up the runtime repo's build setup.

Findings 48 to 54 are in the ICU-based globalization code, so their repros only show the bug on Linux and macOS. On Windows, .NET uses NLS and the repros say so.

## Summary

| # | Area | Finding | Severity | Shipped in 11.0 RC1? | Repro |
|---|---|---|---|---|---|
| 1 | SslStream | `IndexOutOfRangeException` on a 5-byte zero-length handshake record | Medium | No, regression on main | [01](repros/01-SslStream-ZeroLengthRecord.cs) |
| 2 | SslStream | 5-byte SSLv2 hello skips `ServerOptionsSelectionCallback`, throws `NotSupportedException` | Low | No, regression on main | [02](repros/02-SslStream-Sslv2HelloSkipsCallback.cs) |
| 3 | Tensors | `Tensor.ToString` reads out of bounds for a zero innermost stride and prints wrong elements for strides above 1 | High (memory disclosure) | Yes | [03](repros/03-Tensor-ToString-OutOfBoundsRead.cs) |
| 4 | TensorPrimitives | `MaxNumber`/`MinNumber`/`MaxMagnitudeNumber` return NaN when any input is NaN | Medium | Yes, since 9.0.0 | [04](repros/04-TensorPrimitives-MaxNumber-NaN.cs) |
| 5 | TensorPrimitives | `HammingDistance<float/double>` treats NaN differently in the vector and scalar paths | Low | Yes | [05](repros/05-TensorPrimitives-HammingDistance-NaN.cs) |
| 6 | Tensors | Pairwise comparisons iterate the wrong shape when broadcasting | Medium | Yes | [06](repros/06-Tensor-PairwiseComparisons-WrongShape.cs) |
| 7 | Tensors | `*Any` comparisons return true for empty tensors | Low | Yes | [07](repros/07-Tensor-AnyComparisons-EmptyIsTrue.cs) |
| 8 | Tensors | `Squeeze()` of an all-ones shape drops the element | Low | Yes | [08](repros/08-Tensor-Squeeze-LosesElement.cs) |
| 9 | Tensors | `ResizeTo` doesn't zero-fill as documented | Low | Yes | [09](repros/09-Tensor-ResizeTo-NoZeroFill.cs) |
| 10 | TensorPrimitives | `Remainder<float/double/Half>` isn't IEEE fmod (`x % ∞` is NaN, overflow gives ∞) | Medium | Yes | [10](repros/10-TensorPrimitives-Remainder-Float.cs) |
| 11 | TensorPrimitives | `CopySign<signed int>` wraps where the scalar throws `OverflowException` | Low | Yes | [11](repros/11-TensorPrimitives-CopySign-Overflow.cs) |
| 12 | TensorPrimitives | Vectorized `CosPi`/`SinPi` compute `Cos(x * Pi)` and lose accuracy | Low | Yes | [12](repros/12-TensorPrimitives-CosPi-Accuracy.cs) |
| 13 | Tensors | Constructors throw `OverflowException` and `Slice` throws `IndexOutOfRangeException` instead of the documented `ArgumentOutOfRangeException` | Low | Yes | [13](repros/13-TensorSpan-ExceptionContracts.cs) |
| 14 | CBOR | Reserved simple values `0xFC`-`0xFE` surface as `InvalidOperationException` | Low | Yes | [14](repros/14-Cbor-ReservedSimpleValue.cs) |
| 15 | CBOR | Dangling root tag with multiple roots reports `Finished`, `SkipValue` throws `InvalidOperationException` | Low | Yes | [15](repros/15-Cbor-DanglingRootTag.cs) |
| 16 | CBOR | Canonical reader and writer disagree on float encodings | Observation | Yes | [16](repros/16-Cbor-CanonicalFloats.cs) |
| 17 | ServerSentEvents | Event type (and per-item id/retry) leaks across a blank line without data | Medium | Yes | [17](repros/17-Sse-EventTypeLeak.cs) |
| 18 | ServerSentEvents | `retry` with trailing NULs is accepted | Low | Yes | [18](repros/18-Sse-RetryTrailingNul.cs) |
| 19 | HttpClient | Conflicting `Content-Length` headers are accepted (first wins); Debug builds assert | Low | Yes | [19](repros/19-HttpClient-ConflictingContentLength.cs) |
| 20 | Brotli | `BrotliStream` throws `InvalidOperationException` for corrupt data instead of the documented `InvalidDataException` | Low | Yes | [20](repros/20-BrotliStream-InvalidDataException.cs) |
| 21 | Brotli | Wrong `Debug.Assert` in `BrotliDecoder.TryDecompress` aborts Debug/Checked builds on invalid input | Low | Yes | [21](repros/21-BrotliDecoder-TryDecompress-Assert.cs) |
| 22 | Mail | `MailAddress` display names with backslashes change on every round trip or stop parsing | Low | Yes | [22](repros/22-MailAddress-DisplayNameRoundTrip.cs) |
| 23 | MIME | `ContentDisposition`/`ContentType` throw `IndexOutOfRangeException`/`ArgumentException` instead of `FormatException` | Low | Yes | [23](repros/23-ContentDisposition-ParseExceptions.cs) |
| 24 | MIME | Non-ASCII `ContentDisposition` parameters don't round-trip (encoded-words aren't decoded) | Observation | Yes | [24](repros/24-ContentDisposition-EncodedWordRoundTrip.cs) |
| 25 | Hashing | Reflected `Crc32ParameterSet`/`Crc64ParameterSet` take the initial value in the reflected domain, unlike the Rocksoft/reveng model | Low (API semantics) | Yes | [25](repros/25-Crc32ParameterSet-ReflectedInitialValue.cs) |
| 26 | HttpListener | Managed `HttpListener` accepts invalid request targets (`#frag`, `?x`) and asserts on them in Debug builds | Low | Yes | [26](repros/26-HttpListener-InvalidRequestTarget.cs) |
| 27 | NTLM | Managed NTLM client reports `IsAuthenticated` after a rejected challenge; `ComputeIntegrityCheck` then throws `NullReferenceException` | Medium | Yes | [27](repros/27-ManagedNtlm-IsAuthenticatedAfterFailure.cs) |
| 28 | NTLM | A server's out-of-range `MsvAvTimestamp` makes the managed NTLM client, and `HttpClient`, throw `ArgumentOutOfRangeException` | Medium | Yes | [28](repros/28-ManagedNtlm-BadTimestamp.cs) |
| 29 | Complex | `Exp`, `Sinh`, `Cosh`, `Sin`, `Cos` and `Pow` return NaN parts (`Exp(710) = (∞, NaN)`) and spurious infinities once `e^x` overflows | Medium | Yes | [29](repros/29-Complex-OverflowNaN.cs) |
| 30 | Complex | `Asin`, `Acos` and `Atan` pick the wrong side of their branch cuts for signed-zero inputs | Medium | Yes | [30](repros/30-Complex-BranchCuts.cs) |
| 31 | Complex | `Tan(π/2)` is `(∞, NaN)` and `Tan(1e308)` is NaN, though `Math.Tan` is finite for both; same for `Tanh` | Medium | Yes | [31](repros/31-Complex-TanPoles.cs) |
| 32 | Complex | Division by a value near `MaxValue` returns 0 (`(1e308 + i) / (1e308 + 1e308i)` should be `0.5 - 0.5i`) | Medium | Yes | [32](repros/32-Complex-DivisionOverflow.cs) |
| 33 | Complex | `Complex<float>`/`Complex<Half>` `Log`, `Log10` and `Atan` overflow for `\|z\| > MaxValue`; double is fine | Low | Yes | [33](repros/33-Complex-FloatLogOverflow.cs) |
| 34 | Frozen collections | `ToFrozenSet()` of up to 10 enum values aborts Debug/Checked builds (`Debug.Assert(default(T) is IComparable<T>)`) | Low | Debug builds only | [34](repros/34-FrozenSet-EnumAssert.cs) |
| 35 | Reflection.Metadata | `GetAssemblyName()` throws `CultureNotFoundException` for every satellite assembly in invariant globalization mode, and for malformed cultures in any mode | Medium | Yes | [35](repros/35-Metadata-GetAssemblyName-Culture.cs) |
| 36 | Frozen collections | A non-ASCII lookup in an `OrdinalIgnoreCase` frozen collection of ASCII keys hits `Debug.Assert(Ascii.IsValid(s))` | Low | Debug builds only | [36](repros/36-Frozen-AsciiHashAssert.cs) |
| 37 | Reflection.Metadata | A metadata stream count of `0x8000` or more makes `MetadataReader` (and `PEReader.GetMetadataReader`) throw `OverflowException` | Low | Yes | [37](repros/37-Metadata-NegativeStreamCount.cs) |
| 38 | Complex | Annex G violations: huge finite / infinite gives `(0, NaN)` instead of zero; `Sqrt(-9.27e307 - 5e-324i)` lands on the wrong side of the cut | Low | Yes | [38](repros/38-Complex-AnnexG-Violations.cs) |
| 39 | Reflection.Metadata | `BlobReader.ReadTypeHandle` lets big row numbers spill into the table byte: rows alias (`0x02000005` → TypeDef 5), and `SignatureDecoder` hits an impossible `Debug.Assert` | Low | Yes (aliasing); Debug builds (assert) | [39](repros/39-Metadata-ReadTypeHandle-RowOverflow.cs) |
| 40 | BitArray | Growing `Length` past the storage brings back bits cut off by an earlier shrink (778 of 1000 set instead of 10) | Medium | Yes | [40](repros/40-BitArray-StaleBitsOnGrow.cs) |
| 41 | Convert | `FromHexString` OperationStatus overloads: `charsConsumed` can be odd or point past an invalid char, and a trailing non-hex char gives `NeedMoreData` | Observation | Yes | [41](repros/41-Convert-FromHexString-Consumed.cs) |
| 42 | Reflection.Metadata | `PEReader.ReadDebugDirectory()` throws `NullReferenceException` for COFF-only images (Debug builds assert) | Low | Yes | [42](repros/42-PEReader-CoffDebugDirectory.cs) |
| 43 | Complex | `Sqrt(0 + εi)` with `ε = T.Epsilon` is `(0, ∞)` for double, float and Half | Low | Yes | [43](repros/43-Complex-SqrtSubnormal.cs) |
| 44 | Number parsing | UTF-8 `TryParse`/`Parse` reads past the end of the span when matching a 3-byte NaN/Infinity symbol or sign: `AccessViolationException` at a page boundary, and a UTF-8/UTF-16 mismatch | High (memory safety) | Yes | [44](repros/44-NumberParsing-Utf8-OutOfBoundsRead.cs) |
| 45 | IPNetwork | `IPNetwork` silently masks host bits after the prefix although the constructor, `Parse` and `TryParse` all document that they reject them | Low | Yes | [45](repros/45-IPNetwork-SilentHostBitMasking.cs) |
| 46 | HttpHeaders | `StringWithQualityHeaderValue` keeps a q-value with more than 3 decimals but `ToString` rounds to 3, so it doesn't round-trip | Low | Yes | [46](repros/46-StringWithQualityHeaderValue-QualityRoundTrip.cs) |
| 47 | CompositeFormat | `CompositeFormat.Parse` doesn't bound the hole index or the alignment, so it accepts values `string.Format` rejects: `MinimumArgumentCount` wraps, formatting throws `IndexOutOfRangeException`, and `{0,2147483647}` runs out of memory | Low-Medium | Yes | [47](repros/47-CompositeFormat-IndexOverflow.cs) |
| 48 | IdnMapping (ICU) | `GetAscii(string)`/`GetUnicode(string)` return the caller's casing when ICU's answer differs only by case, while `TryGetAscii`/`TryGetUnicode` and the index overloads return ICU's lowercased output | Low | Yes | [48](repros/48-IdnMapping-GetAscii-CasePreservation.cs) |
| 49 | CompareInfo (ICU) | `IgnoreNonSpace` without `IgnoreCase` is case-sensitive in `Compare` but case-insensitive in `IndexOf`/`LastIndexOf`/`IsPrefix`/`IsSuffix`, except when the ASCII fast path runs | Medium | Yes | [49](repros/49-CompareInfo-IgnoreNonSpace-SearchIgnoresCase.cs) |
| 50 | CompareInfo (ICU) | `IsSuffix(..., out matchLength)` with an ignorable suffix (`"\0"`, ZWJ, soft hyphen) sets `matchLength` to the whole source length instead of 0 | Medium | Yes | [50](repros/50-CompareInfo-IsSuffix-IgnorableMatchLength.cs) |
| 51 | CompareInfo (ICU) | In shifted collation (`IgnoreSymbols`, or any th-TH search) a value of only combining marks is treated as ignorable by `IndexOf`/`LastIndexOf`/`IsPrefix`, so with CurrentCulture th-TH `"abc".Contains("\u0E48")` (a Thai tone mark) is true | Medium | Yes | [51](repros/51-CompareInfo-IgnoreSymbols-CombiningMarksIgnorable.cs) |
| 52 | IdnMapping (ICU) | `GetUnicode` throws for ASCII names with `--` in label positions 3-4 (`r3---sn-abcd.googlevideo.com`) that `GetAscii` accepts unchanged | Low | Yes | [52](repros/52-IdnMapping-GetUnicode-Hyphen34.cs) |
| 53 | CompareInfo (ICU) | Culture-aware `StartsWith`/`EndsWith` accept an affix that splits a grapheme or an expansion, so `"कि".StartsWith("क")` and `"เก".StartsWith("ก")` are true while `IndexOf` is -1 (Indic vowel signs, Hangul jamo, Thai prevowels, ligatures with IgnoreCase; backwards even `"e\u0301".EndsWith("\u0301")`) | Medium | Yes | [53](repros/53-CompareInfo-AffixSplitsGrapheme.cs) |
| 54 | CompareInfo (ICU) | Repeated culture-aware backward searches reuse a cached ICU search object and return stale matches: `LastIndexOf(..., out matchLength)` gives matchLength -1 (or -5, -11) or an earlier index on the second identical call, and `IsSuffix` flips from true to false | Medium | Yes | [54](repros/54-CompareInfo-LastIndexOf-StaleSearchState.cs) |
| 55 | Compression | The new span decoders `DeflateDecoder`/`ZLibDecoder`/`GZipDecoder` can't decode a valid stream of empty data into an empty destination (`TryDecompress` is false; Brotli and Zstandard accept it), and `ZstandardDecoder.Decompress` returns `Done` instead of throwing after `Dispose` | Low | Yes | [55](repros/55-CompressionDecoders-SpanEdgeCases.cs) |
| 56 | Compression (zstd) | `ZstandardDecoder.TryDecompress` accepts frames with a raw/RLE block larger than the window, which the format forbids and `Decompress`/`ZstandardStream` reject, so the same bytes decode or fail depending on the API | Low | Yes | [56](repros/56-Zstandard-OneShotAcceptsOversizedBlock.cs) |

"Shipped in 11.0 RC1" was checked against the `11.0.0-rc.1` NuGet packages and the 11.0 RC1 shared framework.

## Details

### 1. SslStream: IndexOutOfRangeException on a zero-length handshake record

[Repro](repros/01-SslStream-ZeroLengthRecord.cs). A TLS server that receives `16 03 01 00 00` (a handshake record with no payload) throws `IndexOutOfRangeException` from `SslStream.ReceiveHandshakeFrameAsync`, whether the certificate is set directly or chosen by a callback, on both the new and the legacy handshake path.

"Fix SslStream detection of exactly-5-byte TLS frames" (dotnet/runtime#132694, Aug 25) made a 5-byte record count as a complete frame. The server's first-frame checks in `SslStream.IO.cs` still read `EncryptedReadOnlySpan[HandshakeTypeOffsetTls]`, index 5, of a 5-byte span. That PR's test only covers a 5-byte record after the handshake. On 11.0 RC1 the same input ends in an `IOException`.

Any unauthenticated client can trigger this, and callers of `AuthenticateAsServerAsync` don't expect `IndexOutOfRangeException`.

### 2. SslStream: SSLv2 hello skips the options callback

[Repro](repros/02-SslStream-Sslv2HelloSkipsCallback.cs). With the `ServerOptionsSelectionCallback` overload, the 5-byte SSLv2-framed hello `00 01 01 03 03` never reaches the callback and fails with `NotSupportedException: The server mode SSL must use a certificate with the associated private key`.

`TlsFrameHelper.TryGetFrameInfo` always reads the handshake type at the TLS offset (5) and needs more than 5 bytes, even for SSLv2 frames where the type is at offset 2. The first-frame check added in dotnet/runtime#126352 uses the SSLv2 offset and lets the frame through, so the handshake continues with no certificate. That check was meant to prevent exactly this `NotSupportedException`; #132694 made this 5-byte input reachable. RC1 throws `IOException`.

### 3. Tensor.ToString reads out of bounds

[Repro](repros/03-Tensor-ToString-OutOfBoundsRead.cs). The innermost-dimension printer in `Tensor.ToString` does `Unsafe.Add(ref tensor._reference, i)`, so it assumes the last dimension is contiguous.

With a zero stride (a broadcast view, which the constructors accept) it reads past the end of the storage. `new ReadOnlyTensorSpan<int>(new[] { 1, 2 }, [8], [0]).ToString([8])` prints heap memory after the array, which looks like the next object's method table pointer and length. Over guard-paged memory the fuzzer got an `AccessViolationException`. With a stride above 1 it prints the wrong elements (`[0, 1, 2]` instead of `[0, 4, 8]`). The indexer, enumerator and `FlattenTo` all handle these views correctly.

### 4. MaxNumber, MinNumber and MaxMagnitudeNumber propagate NaN

[Repro](repros/04-TensorPrimitives-MaxNumber-NaN.cs). These are documented to match IEEE 754 `maximumNumber`/`minimumNumber`, which ignore NaN, but `TensorPrimitives.MaxNumber([NaN, 2, 1])` returns NaN. They go through `MinMaxCore`, which returns the first NaN it sees. That's right for `Max`/`Min` and wrong for the `*Number` operators. Same result on NuGet 9.0.0, 10.0.0, 10.0.12 and 11.0 RC1.

### 5. HammingDistance and NaN

[Repro](repros/05-TensorPrimitives-HammingDistance-NaN.cs). Documented as counting `!EqualityComparer<T>.Default.Equals(x[i], y[i])`, where NaN equals NaN. The scalar path does that, but the vector path uses `Vector.Equals`, where NaN doesn't equal NaN. Three NaN pairs give 0, sixty-four give 64.

### 6. Pairwise comparisons use the wrong shape

[Repro](repros/06-Tensor-PairwiseComparisons-WrongShape.cs). `EqualsAll/Any`, `GreaterThanAll/Any`, `LessThanAll/Any` and friends broadcast `x` and `y` against each other. However, `TensorOperation.Invoke(x, y)` iterates over the operand with the larger `FlattenedLength` instead of the broadcast shape.

A `[3,1]` column against a `[1,4]` row only visits 4 of the 12 pairs, so `EqualsAny` misses a match and `LessThanAll` misses a failure. When the chosen operand has the lower rank, Debug builds hit `Debug.Assert(indexes.Length >= Rank)` in `TensorShape.AdjustToNextIndex`.

### 7. `*Any` on empty tensors

[Repro](repros/07-Tensor-AnyComparisons-EmptyIsTrue.cs). `EqualsAny` and `GreaterThanAny` of two empty tensors return true, which looks like the `All` result negated rather than a real "any".

### 8. Squeeze drops the only element

[Repro](repros/08-Tensor-Squeeze-LosesElement.cs). Squeezing a `[1,1]` tensor removes every dimension, and a rank-0 view reports `FlattenedLength == 0`, so the value is gone. NumPy gives a 0-d array holding one element.

### 9. ResizeTo doesn't zero-fill

[Repro](repros/09-Tensor-ResizeTo-NoZeroFill.cs). The docs say "If the final shape is bigger it is filled with 0s", but neither code path clears the tail, so `[1, 2, 3]` resized into a destination of nines gives `[1, 2, 3, 9, 9, 9]`.

### 10. Remainder for floating point

[Repro](repros/10-TensorPrimitives-Remainder-Float.cs). The vectorized `Remainder` behaves like `x - trunc(x / y) * y`. `1 % ∞` returns NaN instead of 1, and `-2147483648f % 1e-30f` returns +∞ because `x / y` overflows. The scalar `%` operator gets both right.

### 11. CopySign for signed integers

[Repro](repros/11-TensorPrimitives-CopySign-Overflow.cs). `int.CopySign(int.MinValue, 1)` throws `OverflowException`, while `TensorPrimitives.CopySign` over the same values silently returns `int.MinValue`. That's either a missing check or a missing doc note.

### 12. CosPi and SinPi accuracy

[Repro](repros/12-TensorPrimitives-CosPi-Accuracy.cs). The vector path computes `Cos(x * Pi)`, and rounding `x * Pi` to float defeats the point of the `Pi` variants. `CosPi(0.5f)` gives `-4.4e-8` instead of 0, `CosPi(1e6f)` gives `0.9954` instead of 1, and `CosPi(100000.5f)` gives `-0.0076` instead of 0.

### 13. Tensor exception contracts

[Repro](repros/13-TensorSpan-ExceptionContracts.cs). Lengths like `[2^32, 2^32]` make every constructor (array, span, pointer, `System.Array`) throw `OverflowException` from checked arithmetic in `TensorShape`. An out-of-range `Slice(nint[])` start throws `IndexOutOfRangeException`. Both are documented as `ArgumentOutOfRangeException`. Nothing unsafe happens.

### 14. CBOR reserved simple values

[Repro](repros/14-Cbor-ReservedSimpleValue.cs). A major type 7 item with additional info 28-30 (`0xFC`-`0xFE`) is malformed. `PeekState()` still reports `SimpleValue`, and `ReadSimpleValue()` or a `SkipValue()` over it throws `InvalidOperationException` instead of `CborContentException`. Code that only catches `CborContentException` for untrusted input will crash.

### 15. CBOR dangling root tag

[Repro](repros/15-Cbor-DanglingRootTag.cs). With `AllowMultipleRootLevelValues`, input that ends in a tag with no content (`C1`) reports `Finished` after `ReadTag()`, and `SkipValue()` throws `InvalidOperationException: Reader state 'Finished' is not at the start of a data item`. Single-root readers correctly throw `CborContentException`. The recent incremental-reading commit (55b3dadef0) mentions this as a known pre-existing issue.

### 16. CBOR canonical floats (observation)

[Repro](repros/16-Cbor-CanonicalFloats.cs). In the canonical modes the reader accepts floats that aren't in their shortest form, but the writer shortens them, so `FA7F800000` re-encodes as `F97C00`. It also means a map with two NaN keys that differ only in payload is accepted by the reader, and re-writing it canonically fails with a duplicate-key error. RFC 7049 doesn't strictly require shortest floats, so this is more of a consistency question than a bug.

### 17. SSE event type leaks across an empty dispatch

[Repro](repros/17-Sse-EventTypeLeak.cs). Per the WHATWG spec, a blank line with an empty data buffer resets the event type ("set the data buffer and the event type buffer to the empty string and return"). `SseParser.ProcessLine` returns without resetting anything, so `event: foo\n\n` followed by `data: x\n\n` gives an event of type `foo` instead of `message`. The per-item `EventId` and `ReconnectionInterval` leak the same way. `LastEventId` is sticky by spec and behaves correctly.

### 18. SSE retry with trailing NULs

[Repro](repros/18-Sse-RetryTrailingNul.cs). The spec only accepts `retry` values made of ASCII digits. `long.TryParse` with `NumberStyles.None` still skips trailing `\0`, so `retry: 7\0` sets the interval to 7 ms.

### 19. HttpClient accepts conflicting Content-Length

[Repro](repros/19-HttpClient-ConflictingContentLength.cs). A response with `Content-Length: 3` and `Content-Length: 10` is accepted and framed with 3. RFC 9112 section 6.3 says to treat that as an unrecoverable error. The handler does refuse to reuse a connection with leftover buffered bytes, so I couldn't get the leftover bytes parsed as a second response (no desync). In Debug/Checked builds of System.Net.Http, the handler's own `ContentLength` read trips `Debug.Assert("Only a single parsed value should be stored for this parser")`. Duplicate `Content-Type` or `Cache-Control` headers hit the same assert from the typed header properties.

### 20. BrotliStream throws InvalidOperationException for corrupt data

[Repro](repros/20-BrotliStream-InvalidDataException.cs). `BrotliStream.Read` documents `InvalidDataException` for data in an invalid format, and the end-of-stream checks do throw that. Corrupt data in the middle of the stream goes through `BrotliStream.TryDecompress`, which throws `InvalidOperationException("Decoder ran into invalid data")`. `DeflateStream` and `GZipStream` use `InvalidDataException`, so code that follows the docs and catches it for untrusted input crashes on Brotli.

### 21. Wrong Debug.Assert in BrotliDecoder.TryDecompress

[Repro](repros/21-BrotliDecoder-TryDecompress-Assert.cs). `TryDecompress` asserts `success ? availableOutput <= destination.Length : availableOutput == 0`, but the native decoder reports partial output when it fails, which the method's own remarks allow. Release builds return `false` with `bytesWritten > 0` as documented. Debug/Checked builds of System.IO.Compression.Brotli abort the process on such input, which is why the Brotli campaign ran against a Release build.

### 22. MailAddress display names don't round-trip

[Repro](repros/22-MailAddress-DisplayNameRoundTrip.cs). The parser keeps quoted-pair backslashes in `DisplayName` (`"\"quoted\""` gives `\"quoted\"`), and `ToString()` escapes them again, so the backslashes double on every parse/format cycle. When the escaped character is a control character (a backslash followed by NUL), doubling the backslash leaves the NUL unescaped, and `MailAddressCollection.ToString()` produces text that no longer parses at all.

### 23. ContentDisposition and ContentType parse exceptions

[Repro](repros/23-ContentDisposition-ParseExceptions.cs). Both constructors document `FormatException` for input they can't parse. `new ContentDisposition("0000000000;Y")`, with a trailing parameter that has no value, throws `IndexOutOfRangeException` from `ContentDisposition.ParseValue`. A repeated parameter (`attachment; size=100; size=100` or `text/html; charset=utf-8; charset=utf-8`) throws `ArgumentException` from the parameter dictionary.

### 24. ContentDisposition encoded-words (observation)

[Repro](repros/24-ContentDisposition-EncodedWordRoundTrip.cs). A non-ASCII `FileName` is written as an RFC 2047 encoded-word (`filename="=?utf-8?B?bmHDr3ZlLnR4dA==?="`), but the parser doesn't decode encoded-words, so parsing the formatted header gives the encoded text back as the file name. `creation-date` style parameters also get normalized when formatted, so they don't round-trip either.

### 25. Reflected CRC parameter sets and the initial value

[Repro](repros/25-Crc32ParameterSet-ReflectedInitialValue.cs). `Crc32ParameterSet.Create(poly, init, xor, reflectValues: true)` (and the CRC-64 version) loads `init` straight into the reflected, LSB-first register. The Rocksoft/reveng parameter model, which the property names follow and which the CRC catalogue, `crcmod` and `reveng` use, defines the initial value in the unreflected domain. The two only agree for bit-palindromic values like 0 and all ones, which is why all the catalogue check values pass. A reflected CRC with any other seed, created from its published parameters, doesn't match other tools. Passing `ReverseBits(init)` gives the standard result. The API is new in 11.0, so now is the time to either reflect the value or document the convention.

### 26. HttpListener accepts invalid request targets

[Repro](repros/26-HttpListener-InvalidRequestTarget.cs). The managed `HttpListener` used on Linux and macOS delivers requests like `GET #frag HTTP/1.1` or `GET ?x=1 HTTP/1.1` to the application, with `RawUrl` `#frag` and a synthesized `Url` of `http://127.0.0.1:port/#frag`. RFC 9112 only allows origin-form, absolute-form, authority-form and `*`. In Debug/Checked builds of System.Net.HttpListener these requests, and a valid absolute-form target without a path (`GET http://host:port HTTP/1.1`), trip `Debug.Assert` in `HttpListenerRequestUriBuilder`, so any client can abort such a server.

### 27. Managed NTLM stays "authenticated" after a rejected challenge

[Repro](repros/27-ManagedNtlm-IsAuthenticatedAfterFailure.cs). `ManagedNtlmNegotiateAuthenticationPal.GetOutgoingBlob` sets `_isAuthenticated = true` before it parses the server's CHALLENGE message and never resets it when parsing fails. After `GetOutgoingBlob` returns `InvalidToken`, `IsAuthenticated` and `IsSigned` are true, `RemoteIdentity` returns the target name, and `ComputeIntegrityCheck` throws `NullReferenceException` because there's no signing key. `Wrap`/`Unwrap` do refuse. HttpClient goes by the status codes, but other users of the public API can be misled. This client is the default on macOS, iOS, Android and OpenBSD. Separately, an empty second server token hits `Debug.Assert(!incomingBlob.IsEmpty)` in Debug builds.

### 28. Managed NTLM throws on a server-controlled timestamp

[Repro](repros/28-ManagedNtlm-BadTimestamp.cs). `ProcessTargetInfo` passes the `MsvAvTimestamp` AV pair from the server's challenge to `DateTime.FromFileTimeUtc` without validating it. An out-of-range value makes `GetOutgoingBlob` throw `ArgumentOutOfRangeException: Not a valid Win32 FileTime` instead of returning `InvalidToken`, on both the NTLM and the SPNEGO path. Through `HttpClient` with credentials, a server that asks for NTLM and sends such a challenge makes `SendAsync` throw that `ArgumentOutOfRangeException`, which callers don't expect from HTTP requests. The repro shows both.

### 29. Complex overflow turns zeros into NaN

[Repro](repros/29-Complex-OverflowNaN.cs). `Complex<T>.Exp` is `FromPolarCoordinates(T.Exp(x), y)` for finite input, and `Sinh`, `Cosh`, `Sin` and `Cos` are built the same way from `e^x` (or `cosh`/`sinh` of one part) times the trig functions of the other. Once that factor overflows, infinity times zero makes a zero component NaN: `Exp(710 + 0i)` is `(∞, NaN)` instead of `(∞, 0)`, and likewise `Sinh(711)`, `Cosh(711)`, `Sin(711i)`, `Cos(711i)`. `Pow` hands overflowing inputs to `Exp(power * Log(value))`, so `Pow(1e200, 2)` is `(∞, NaN)` too. The same split also overflows early: `Exp(710 + 1.5707963267948966i)` has a real part of about `1.4e292`, but it comes back as `∞`, and scaling `e^(x/2)` twice would avoid that. `Complex<float>` and `Complex<Half>` hit this at `Exp(89)` and `Exp(12)`. The non-generic `Complex` delegates to `Complex<double>`, so it has the same results. The Annex G change (#131132) handled infinite and NaN inputs but left this finite overflow case alone.

### 30. Complex inverse trig on the branch cuts

[Repro](repros/30-Complex-BranchCuts.cs). Annex G (G.6.2) wants `casin`, `cacos` and `catan` to be continuous onto their cuts from the side the sign of zero picks, with `casin(conj z) = conj(casin z)` and so on. `Complex<T>` gets this wrong on every cut:

- `Asin(x - 0i)` and `Acos(x + 0i)` with `|x| > 1` ignore the zero's sign, so the imaginary part has the wrong sign (`Asin(1.5 - 0i)` gives `+0.962i`, it should be `-0.962i`).
- `Atan(±0 + iy)` with `|y| > 1` takes the real part's sign from `y` instead of from the zero, so `Atan(-0 + 1.5i)` is `+π/2 + 0.805i` instead of `-π/2 + 0.805i`.
- Several results lose the sign of a zero component (`Asin(0 - 0i)`, `Atan(0.5 - 0i)`, `Acos(0.5 + 0i)`).

The expected values match C99 `casin`/`cacos`/`catan` and CPython's `cmath`. `Sqrt` and `Log` get their cuts right. The non-generic `Complex` delegates and has the same bug.

### 31. Complex Tan and Tanh at poles and for huge arguments

[Repro](repros/31-Complex-TanPoles.cs). `Tan` computes `sin(2x) / (cos(2x) + cosh(2y))` and `Tanh` the mirror image. At the double nearest `π/2`, `cos(2x)` rounds to exactly `-1`, the denominator is zero, and `Tan(π/2 + 0i)` is `(∞, NaN)`, while `Math.Tan(π/2)` is `1.633e16`. For `|x|` above `MaxValue/2`, `2x` overflows and `Tan(1e308)` is `(NaN, NaN)`, though `Math.Tan(1e308)` is `-0.509`. `Tanh` has the same problems on the imaginary axis, and `Complex<float>.Tan(-1.5707964f)` is `(∞, NaN)` against `MathF.Tan` = `2.29e7`. Computing from `tan(x)` and `tanh(y)` (as glibc's `ctan` does) avoids both.

### 32. Complex division overflows

[Repro](repros/32-Complex-DivisionOverflow.cs). `operator /` is Smith's formula without scaling. For `(a + bi) / (c + di)` with `|d| >= |c|` it computes `c * (c/d) + d`. With `c = d = 1e308` that is `2e308`, which overflows to infinity, so the quotient becomes `(0, -0)` instead of `0.5 - 0.5i`. `Complex<float>` does the same near `float.MaxValue`, and `Reciprocal` goes through the same code. The commit that added the Annex G handling says division "stays accurate for large-magnitude dividends", but large divisors aren't covered. Scaling by a power of two first (Priest; Baudin and Smith) fixes it.

### 33. Complex<float> and Complex<Half> Log/Atan overflow

[Repro](repros/33-Complex-FloatLogOverflow.cs). `Complex<double>.Log(1e308 + 1e308i)` is `709.54 + 0.785i`, so the double path scales before taking the magnitude. The float and Half instantiations don't. `Complex<float>.Log(2.5e38 + 2.5e38i)` and `Log10(MaxValue + MaxValue·i)` have an infinite real part, `Complex<Half>.Log(60000 + 60000i)` too, and `Complex<float>.Atan(MaxValue + MaxValue·i)` is `(NaN, NaN)` instead of `(π/2, 0)`. The scaling threshold is probably tuned for double.

### 34. Small enum FrozenSets assert in Debug builds

[Repro](repros/34-FrozenSet-EnumAssert.cs). `FrozenSet.ToFrozenSet` sends small sets (10 items or fewer) of value types that `Constants.IsKnownComparable<T>()` accepts to `SmallValueTypeComparableFrozenSet<T>`. That list ends with `typeof(T).IsEnum`, but the class's constructor asserts `default(T) is IComparable<T>`, and enums only implement the non-generic `IComparable`. Lookups go through `Comparer<T>.Default` and are fine, so Release builds work. Debug/Checked builds of System.Collections.Immutable abort on something as ordinary as `new[] { DayOfWeek.Monday }.ToFrozenSet()`. `SmallValueTypeComparableFrozenDictionary` doesn't have the assert, and the tests only freeze enum-keyed dictionaries, which is probably why nobody hit it. Run the repro with `run-on-local-runtime.sh` to see the abort.

### 35. GetAssemblyName and culture strings

[Repro](repros/35-Metadata-GetAssemblyName-Culture.cs). `AssemblyDefinition.GetAssemblyName()` and `AssemblyReference.GetAssemblyName()` build an `AssemblyName` and set `CultureName` from the metadata string, which constructs a `CultureInfo`. With `InvariantGlobalization=true`, which many container images use, that throws `CultureNotFoundException` for every culture except the neutral one, so reading any satellite assembly (`Culture=de`) or any reference to one fails. In any mode, a malformed culture string in a crafted image (`.`, `<Module>`) throws `CultureNotFoundException` rather than `BadImageFormatException`, the exception every other malformed-input path in `MetadataReader` uses. `GetAssemblyNameInfo()` keeps the culture as a string and handles both, so callers have a workaround, but `GetAssemblyName()` is the older and more widely used API. Found by `MetadataReaderFuzzer`.

### 36. Non-ASCII lookups in ASCII-only case-insensitive frozen collections

[Repro](repros/36-Frozen-AsciiHashAssert.cs). The frozen string collections pick `*CaseInsensitiveAscii*` strategies when the hashed part of every key is ASCII, and then use `Hashing.GetHashCodeOrdinalIgnoreCaseAscii` on the lookup key as well. That method starts with `Debug.Assert(Ascii.IsValid(s))`, and the lookup key is whatever the caller passes, so `ContainsKey("é1")` on a dictionary of `"a1"`…`"g1"` aborts Debug/Checked builds. Release builds give the right answer, since no non-ASCII character is `OrdinalIgnoreCase`-equal to an ASCII one. It's the same kind of problem as 34, but reachable from user input in any app that keeps, say, header names in an `OrdinalIgnoreCase` frozen dictionary.

### 37. Negative metadata stream count

[Repro](repros/37-Metadata-NegativeStreamCount.cs). `MetadataReader.ReadStreamHeaders` reads the stream count with `ReadInt16()` and allocates `new StreamHeader[streamCount]` right away. A count of `0x8000` or more is negative, so the constructor throws `OverflowException` instead of `BadImageFormatException`. That's 24 bytes of metadata, or any PE file with that field patched, and it escapes code that opens untrusted assemblies and catches `BadImageFormatException`. Reading the count as `ushort`, or checking it against the remaining bytes, fixes it.

### 38. Two more Annex G violations in Complex

[Repro](repros/38-Complex-AnnexG-Violations.cs). G.5.2 requires a finite value divided by an infinity to be zero. `operator /` only applies the recovery step when both parts of the Smith's-formula result are NaN, and for a large finite dividend only one is: `(MaxValue + MaxValue·i) / (∞ - ∞i)` is `(0, NaN)` for both double and float, while `(1 + i) / (∞ - ∞i)` is correctly `(0, 0)`. Separately, `Sqrt(-9.27e307 - 5e-324i)` returns `(-0, +9.63e153)`: the imaginary part should take the sign of `y` (`-9.63e153`, as C99 `csqrt` and CPython give), so the result is on the wrong side of the branch cut. It only happens when a huge negative real part meets a subnormal imaginary part, so it's probably lost in the scaling step.

### 39. ReadTypeHandle row overflow

[Repro](repros/39-Metadata-ReadTypeHandle-RowOverflow.cs). `BlobReader.ReadTypeHandle` turns a TypeDefOrRefOrSpec coded index into `tokenType | (value >> 2)`. Compressed integers go up to `0x1FFFFFFF`, so `value >> 2` can be 27 bits wide and overwrite the table byte. An encoded TypeDef row `0x02000005` comes back as TypeDef row 5, so a malformed signature silently points at another type. A row like `0x01000005` produces a handle of kind 3, which `SignatureDecoder.DecodeTypeHandle` doesn't expect. Release builds throw `BadImageFormatException` there, but the `default` branch starts with `Debug.Assert(handle.IsNil)` inside `if (!handle.IsNil)`, so Debug/Checked builds abort. The MetadataReader campaign was switched to a Release build of System.Reflection.Metadata after this.

### 40. BitArray keeps stale bits when it grows

[Repro](repros/40-BitArray-StaleBitsOnGrow.cs). The `Length` setter documents that new elements are `false`. When the new length still fits in the current `byte[]` storage it clears the bytes past the old length, but when it doesn't, it only calls `Array.Resize`, which copies every old byte, including the ones past the current `Length` that a previous shrink left behind (shrinking by less than 1024 bytes keeps the storage). So `new BitArray(100 × 0xFF) { Length = 10 }` followed by `Length = 1000` has 778 bits set instead of 10, while growing to 500 (still within the storage) correctly has 10. Bits a caller truncated away come back. Clearing `_array.AsSpan(currentByteLength)` before the resize fixes it. Found by `UnsafeBuffersFuzzer`.

### 41. FromHexString and charsConsumed (observation)

[Repro](repros/41-Convert-FromHexString-Consumed.cs). For the `OperationStatus` overloads of `Convert.FromHexString`, `charsConsumed` on `InvalidData` is the index of the first invalid char, so `"0A1g"` reports 3 chars consumed with 1 byte written, and a caller resuming at `charsConsumed` loses the `1`. When both chars of a pair are invalid non-ASCII chars (`"\uFF10\uFF10"`) it reports 1 even though char 0 is the invalid one. A trailing single char is `NeedMoreData` even when it isn't a hex digit (`"0Ag"`), although the `OperationStatus` contract reserves `NeedMoreData` for input that more data could complete. None of this is covered by the tests, which only check the status.

### 42. ReadDebugDirectory on COFF images

[Repro](repros/42-PEReader-CoffDebugDirectory.cs). `PEReader.ReadDebugDirectory` starts with `Debug.Assert(PEHeaders.PEHeader != null)` and then reads `PEHeaders.PEHeader.DebugTableDirectory`. `PEHeaders` accepts COFF-only images (object files, or any input without an `MZ` header that parses as COFF), where `PEHeader` is null, so Release builds throw `NullReferenceException` for a 20-byte empty AMD64 COFF header and Debug builds abort. A COFF file has no debug directory, so returning an empty array would be the natural answer. Found by `MetadataReaderFuzzer` on the Release build.

### 43. Complex Sqrt of the smallest subnormal

[Repro](repros/43-Complex-SqrtSubnormal.cs). `Sqrt(0 + yi)` with `|y| == T.Epsilon` returns `(0, ∞)`: `|y| / 2` underflows to zero, the real part comes out as 0, and the imaginary part is computed as `y / (2 · real)`. The right answer is about `1.57e-162 · (1 + i)` for double. `2 · Epsilon` already works, and float and Half fail the same way at their own `Epsilon`.

### 44. Out-of-bounds read parsing numbers from UTF-8

[Repro](repros/44-NumberParsing-Utf8-OutOfBoundsRead.cs). This is the most serious finding here: a memory-safety over-read reachable from public `double.TryParse(ReadOnlySpan<byte>, ...)` (and every other numeric UTF-8 `TryParse`/`Parse`). Matching the NaN, `PositiveInfinity` or `NegativeInfinity` symbol or the negative sign goes through `Ordinal.EqualsIgnoreCaseUtf8_Scalar` / `StartsWithIgnoreCaseUtf8_Scalar`. The tail that handles exactly 3 leftover bytes reads a `ushort` and one more byte, advancing `byteOffset` by 2, but doesn't subtract that from `range`. When those bytes are non-ASCII it jumps to the non-ASCII fallback with the pointer moved forward 2 while the length still counts the full 3 bytes, so `EqualsStringIgnoreCaseNonAsciiUtf8` reads 2 bytes past the end of the span. It fires whenever the symbol's UTF-8 length is 3 mod 4 and the input ends with it.

Two symptoms:

- Memory safety: `en-US` `PositiveInfinitySymbol` is `∞` (`E2 88 9E`, 3 bytes). Parsing a span that holds just that symbol and ends at an unmapped page throws `AccessViolationException` and crashes the process. Against ordinary heap buffers the over-read usually lands on readable memory and only perturbs the result.
- Correctness: with a format whose `NegativeSign` is `−` (U+2212, 3 bytes), `"−nan"` parses to `NaN` from a string but fails from UTF-8, because the mis-lengthed compare rejects the sign.

The tail should subtract the 2 it read from `range` (or set `range` from `byteOffset`) before the non-ASCII fallback, in both `EqualsIgnoreCaseUtf8_Scalar` and `StartsWithIgnoreCaseUtf8_Scalar`. Found by `NumberParsingUtf8Fuzzer`.

### 45. IPNetwork silently masks host bits

[Repro](repros/45-IPNetwork-SilentHostBitMasking.cs). The `IPNetwork` class summary says "The constructor and the parsing methods will throw in case there are non-zero bits after the prefix", the `IPNetwork(IPAddress, int)` constructor documents `ArgumentException` for "non-zero bits after the network prefix", and `Parse`/`TryParse` document `FormatException` for the same. The implementation does the opposite: `ClearNonZeroBitsAfterNetworkPrefix` silently zeroes those bits. `new IPNetwork(192.168.1.5, 24)` and `IPNetwork.Parse("192.168.1.5/24")` both return `192.168.1.0/24`, and `TryParse` returns true. Code that trusts the documented rejection to catch a malformed CIDR (an ACL or allowlist entry, say) instead accepts it as a broader network than intended. Either the masking or the docs should change; masking is the more surprising choice for a type used in access control. Found by `IPNetworkEndPointFuzzer`.

### 46. StringWithQualityHeaderValue quality precision

[Repro](repros/46-StringWithQualityHeaderValue-QualityRoundTrip.cs). `StringWithQualityHeaderValue.ToString` formats the quality with `$"{_value}; q={_quality:0.0##}"`, so at most three decimals, but the parser and the `(string, double)` constructor accept and keep more. `new StringWithQualityHeaderValue("gzip", 0.1234)` and `Parse("gzip; q=0.1234")` both report `Quality = 0.1234`, while `ToString()` is `"gzip; q=0.123"`, which parses back to `0.123`, so the value doesn't round-trip. RFC 9110 defines the quality value as at most three digits after the decimal point, so rejecting or rounding on input (as `MediaTypeWithQualityHeaderValue`, which stores the q-value as a parameter string, effectively does) would match `ToString`. As it stands an `Accept-Encoding` or `TE` header rebuilt from these objects carries a different weight than the object reports. Found by `HttpHeaderValuesFuzzer`.

### 47. CompositeFormat argument index overflow

[Repro](repros/47-CompositeFormat-IndexOverflow.cs). The digit loop in `CompositeFormat.TryParseLiterals` does `index = index * 10 + ch - '0'` with no overflow or upper-bound check, unlike `string.Format`, whose parser rejects an argument index above 1,000,000 with `FormatException`. So `CompositeFormat.Parse("{2147483648}")` succeeds with a wrapped `ArgIndex`; the resulting object reports `MinimumArgumentCount = 0` (the wrapped index is negative and skipped by the `ArgIndex >= 0` check), and `"{9999999999}"` reports a wrapped positive `1410065408`. Two consequences: `string.Format(compositeFormat, args)` throws `IndexOutOfRangeException` rather than the documented `FormatException`, and `MinimumArgumentCount` misreports how many arguments the format needs, which a caller may use to size an argument array. `string.Format(string, args)` rejects all of these with `FormatException`. The alignment loop right after it has the same gap. `string.Format` stops at a width of 1,000,000 and throws `FormatException`, but `CompositeFormat.Parse("{0,99999999}")` succeeds and formatting pads to 100 million chars. `{0,2147483647}` throws `OutOfMemoryException`, and `{0,4294967295}` wraps to -1 and silently becomes a left-aligned width of 1. A service that accepts a user-supplied format and pre-parses it with `CompositeFormat` can be made to allocate gigabytes, where `string.Format` would have refused the string. Capping both values the way the `StringBuilder.AppendFormat` parser does (`IndexLimit`/`WidthLimit`) fixes it. Found by `CompositeFormatFuzzer`; the alignment half showed up as OOMs and timeouts in a later campaign.

### 48. IdnMapping string overloads keep the input's casing

[Repro](repros/48-IdnMapping-GetAscii-CasePreservation.cs). ICU's UTS #46 mapping lowercases, so `uidna_nameToASCII("Example.COM")` gives `example.com`. `IcuGetAsciiCore` then calls `GetStringForOutput`, which returns the *original* string whenever ICU's output matches it under `Ordinal.EqualsIgnoreCase`. That was meant to save an allocation, but it changes the answer, and only when the whole string is converted (the check needs `originalString.Length == input.Length`). The result on Linux and macOS:

- `GetAscii("Example.COM")` returns `"Example.COM"`.
- `GetAscii("xExample.COM", 1)` returns `"example.com"`.
- `TryGetAscii("Example.COM", ...)` writes `"example.com"`.
- `GetAscii("xn--BCHER-KVA.de")` returns the uppercase punycode unchanged.

`GetUnicode`/`TryGetUnicode` split the same way. Code that compares the result ordinally, like a host allow-list or a cache key, gets different answers depending on the overload. Found by `GlobalizationIcuFuzzer`, which compares the string and span overloads.

### 49. IgnoreNonSpace searches ignore case on ICU

[Repro](repros/49-CompareInfo-IgnoreNonSpace-SearchIgnoresCase.cs). For `IgnoreNonSpace` without `IgnoreCase`, `pal_collation.c` runs the collator at primary strength with `UCOL_CASE_LEVEL` on. `ucol_strcoll` honours the case level, so `Compare("a", "A", IgnoreNonSpace)` is -1. The search APIs use ICU `usearch`, which only compares collation elements masked to the primary weight and never looks at the case level. So `IndexOf("ä", "A", IgnoreNonSpace)` returns 0, and the text it matched doesn't compare equal to the value under the same options. For all-ASCII input the invariant and `en-*` cultures take the managed ordinal fast path in `CompareInfo.Icu.cs`, which *is* case-sensitive. That gives `IndexOf("a", "A", IgnoreNonSpace)` = -1 in en-US but 0 in de-DE and ja-JP. Found by `GlobalizationIcuFuzzer`'s "the matched slice compares equal to the value" check.

### 50. IsSuffix match length for an ignorable suffix

[Repro](repros/50-CompareInfo-IsSuffix-IgnorableMatchLength.cs). `CompareInfo.IsSuffix("Strasse", "\0", CompareOptions.None, out int matchLength)` correctly returns true, since an ignorable suffix matches anything, but sets `matchLength` to 7 instead of 0. `IsPrefix` reports 0 for the same value. The cause is `SimpleAffix_Iterators` in `pal_collation.c`, the path for `None` and `IgnoreCase`. Before each step it saves `ucol_getOffset(pSourceIterator)` and then calls `ucol_previous`. On a freshly opened iterator `ucol_getOffset` returns 0, although the first `ucol_previous` starts at the end of the text. When every element of the pattern is ignorable, the pattern runs out before the source iterator has moved, so the saved offset is still that initial 0. `SimpleAffix` then returns `textLength - 0`. Callers that trim with the match length (`source[..^matchLength]`) delete the whole string. Starting `capturedOffset` at `textLength` for backward searches, or setting the offset explicitly, would fix it. It accounted for almost all of the crashes in the first `GlobalizationIcuFuzzer` campaign.

### 51. IgnoreSymbols treats combining marks as ignorable in searches

[Repro](repros/51-CompareInfo-IgnoreSymbols-CombiningMarksIgnorable.cs). With `IgnoreSymbols` on ICU, `IndexOf("abc", "\u0308", IgnoreSymbols)` returns 0 with a match length of 0, `LastIndexOf` returns 3 and `IsPrefix` returns true. So a value made only of combining marks (optionally with punctuation) "matches" in every string. Meanwhile `IsSuffix` returns false and `Compare("", "\u0308", IgnoreSymbols)` is -1, meaning the value isn't ignorable. `IgnoreSymbols` turns on `alternate=shifted`. ICU `usearch`'s `getCE()` discards any collation element numerically below `variableTop` when shifting is on. A combining mark's element has primary weight 0, so its 32-bit value is below `variableTop` and gets dropped as if it were a symbol, leaving an empty pattern. `ucol_strcoll` handles shifted mode correctly. The root cause is in ICU, but .NET exposes it through `CompareInfo`; the native shim could fall back to a `Compare`-based ignorable check before trusting a zero-length match.

Thai makes this user-visible without any special options. CLDR's Thai collation turns on `alternate=shifted` by default, so with `CurrentCulture` set to th-TH, `"abc".Contains("\u0E48", StringComparison.CurrentCulture)` is true and `IndexOf` returns 0. U+0E48 is MAI EK, a Thai tone mark, so searching Thai text for a tone mark always "finds" it at position 0. The dropped marks also make forward and backward search disagree: in zh-Hans-CN with `IgnoreCase | IgnoreSymbols`, `IndexOf("]\u0F73A\u304C]\u0F73\u0001\u0001", "A\u304C]")` is 2 while `LastIndexOf` is -1. Found by `GlobalizationIcuFuzzer`.

### 52. IdnMapping.GetUnicode rejects names GetAscii accepts

[Repro](repros/52-IdnMapping-GetUnicode-Hyphen34.cs). `GlobalizationNative_ToAscii` in `pal_idna.c` masks out `UIDNA_ERROR_HYPHEN_3_4` "to have a consistent behavior with Windows". `GlobalizationNative_ToUnicode` doesn't. So `GetAscii("ab--cd.example")` returns the name unchanged while `GetUnicode` of that result throws `ArgumentException` ("Decoded string is not a valid IDN name"). The same happens for real host names shaped like YouTube's CDN hosts (`r3---sn-abcd.googlevideo.com`). `TryGetUnicode` behaves the same way. Masking the same bit in `ToUnicode` would make the two directions agree. Found by `GlobalizationIcuFuzzer`'s round-trip check.

### 53. StartsWith/EndsWith accept an affix that splits a grapheme

[Repro](repros/53-CompareInfo-AffixSplitsGrapheme.cs). On ICU, in every culture, with `CompareOptions.None` or `IgnoreCase`:

- `"कि".StartsWith("क", CurrentCulture)` is true, but `IndexOf` is -1 and `Contains` is false (Hindi KA + vowel sign I). Tamil, Bengali, conjuncts with a virama and Tibetan behave the same.
- `"가나".StartsWith("\u1100")` is true, but `IndexOf("\u1100")` is -1 (the syllable's leading conjoining jamo).
- `"เก".StartsWith("ก")` is true, but `IndexOf("ก")` is -1, and `"เก".EndsWith("เ")` is true while `LastIndexOf` is -1. Thai collation reorders a prevowel with the following consonant, so this hits ordinary Thai text.
- `"ﬁx".StartsWith("f", CurrentCultureIgnoreCase)` is true, but `IndexOf` is -1. `IgnoreCase` drops the tertiary difference, so a prefix can end inside a ligature's expansion; `Ⅸ`/`I` and `ǆ`/`d` do the same.
- `"e\u0301".EndsWith("\u0301")` is true, but `LastIndexOf("\u0301")` is -1. This one is plain Latin, going backwards.

`StartsWith`/`EndsWith` go through `SimpleAffix` in `pal_collation.c`, which walks raw collation elements. Going forward it refuses a match that is followed by a *nonspacing* mark: it looks for an element with primary weight 0 and a secondary weight, which is why `"e\u0301".StartsWith("e")` is false. Indic vowel signs, viramas and Hangul vowel/final jamo have primary weights, so that check doesn't fire. Going backward there is no such check at all. `IndexOf`/`LastIndexOf` use ICU `usearch`, which only accepts matches on grapheme boundaries. The two disagree, and `IsPrefix(..., out matchLength)` returns a length that cuts the cluster. With `IgnoreWidth` or `IgnoreKanaType`, the usearch-based `ComplexStartsWith`/`ComplexEndsWith` path runs and the results agree with `IndexOf`. A boundary check on the source after the match (or before it, for suffixes) would fix it. Found by `GlobalizationIcuFuzzer`'s "IsPrefix implies IndexOf == 0" check.

### 54. Stale state in cached ICU search objects

[Repro](repros/54-CompareInfo-LastIndexOf-StaleSearchState.cs). In a fresh process, calling `CompareInfo.LastIndexOf("\t\u0301q", "\u0301q", CompareOptions.None, out int matchLength)` twice gives index 1 and matchLength 2 the first time, then index 1 and matchLength **-1**. A negative match length breaks the API contract, and `Substring(index, matchLength)` or `AsSpan(index, matchLength)` throws on it. Other inputs return stale values like -5 or -11. The stale state can also change the index: for source `"unu\0\u0345\0\0\0\0\0\u0010\0\0\u0345"` and value `"\u0345\0\0"` with `IgnoreCase`, successive identical calls return 13, 4 and 13. `IsSuffix` with `IgnoreKanaType` or `IgnoreWidth` goes through `ComplexEndsWith`, which adds `usearch_getMatchedLength` to the match index. For `("\v\u0301q", "\u0301q")` it returns true and then false.

The trigger is a value that starts with a combining mark, where the source has a control or whitespace character (`\t`, `\n`, `\r`, `\v`, `\f`, U+0085, U+2028) right before the match, so the mark starts a new grapheme. Forward searches aren't affected, and a forward search in between clears the problem, so the results depend on call history. The cache lives in `GetSearchIteratorUsingCollator` (`pal_collation.c`). The first search for each options value opens a fresh `UStringSearch`. Later searches borrow it and only call `usearch_setText`/`usearch_setPattern`, and `usearch_last` on the reused object then reports a stale match. Calling `usearch_reset` on a borrowed iterator, or refusing a negative `usearch_getMatchedLength`, would be the place to fix it.

`ComplexEndsWith` passes `pText + matchEnd` with length `textLength - matchEnd` to `CanIgnoreAllCollationElements`, so a stale match end outside `[0, textLength]` would read out of bounds natively. Out of 200,000 random backward searches, 112 returned a negative match length, but the match end always stayed inside the string, and the guard-paged fuzzer never faulted. So I have no evidence of a memory-safety impact. Found by `GlobalizationIcuFuzzer`, which compares the span and string overloads.

### 55. Edge cases in the new span decoders

[Repro](repros/55-CompressionDecoders-SpanEdgeCases.cs). `DeflateDecoder.Decompress` starts with `if (destination.IsEmpty && source.Length > 0) return OperationStatus.DestinationTooSmall;`. So zlib never gets to see the end-of-stream marker of a stream whose payload is empty. `DeflateDecoder.TryDecompress(stream, Span<byte>.Empty, out _)` returns false for the 2-byte deflate encoding of nothing, and so do `ZLibDecoder` and `GZipDecoder`, which wrap it. `BrotliDecoder.TryDecompress` and `ZstandardDecoder.TryDecompress` return true for the same case. A caller that sizes the output from a stored length, like a length-prefixed record of length 0, gets a failure for valid data. `ZstandardDecoder.Decompress` has the same early `DestinationTooSmall` return. Separately, `ZstandardDecoder.Decompress` checks its `_finished` flag before `EnsureNotDisposed`. Once a frame has been decoded, calling it after `Dispose` returns `Done` instead of throwing `ObjectDisposedException`; `DeflateDecoder` throws as documented. Both are small, but these APIs are new in .NET 11, so now is a cheap time to fix them. Found by `CompressionCodecsFuzzer`'s exact-size destination check.

### 56. Zstandard one-shot and streaming decoders disagree

[Repro](repros/56-Zstandard-OneShotAcceptsOversizedBlock.cs). The frame in the repro declares a 1 KB window and then carries an 8254-byte RLE block. The Zstandard format caps a block at `Block_Maximum_Size = min(Window_Size, 128 KB)` (RFC 8878, section 3.1.1.2), so the frame is invalid. `ZstandardDecoder.Decompress` returns `InvalidData` and `ZstandardStream` throws `InvalidDataException`, because zstd's streaming path checks `cBlockSize > blockSizeMax` ("Block Size Exceeds Maximum"). `ZstandardDecoder.TryDecompress` returns true with 8255 bytes. It calls `ZSTD_decompress`, and in the bundled zstd 1.5.7 the one-shot `ZSTD_decompressFrame` never compares raw or RLE block sizes with `blockSizeMax`. The leniency is upstream's, but .NET offers both paths as interchangeable APIs, so a component that validates with one and decodes with the other sees different data. The one-shot writes stay within the destination, so there's no memory-safety impact. Relatedly, and by design, the one-shot path ignores `maxWindowLog2`, since it needs no window buffer. A frame declaring a 1 GB window decodes one-shot while the streaming decoder throws `IOException`. Found by `CompressionCodecsFuzzer`, which compares the one-shot and streaming decoders.

## Things that looked like bugs but aren't

- `NrbfDecoderFuzzer` OOM on the repo's own seed `largeArrayOfNulls.nrbf`: the input asks for an `Array.MaxLength` array, and the `ArrayRecord.GetArray` docs tell callers to check `Lengths` first. It only fails on machines that can't allocate 16 GB.
- NaN-propagating reductions (`Max`, `Min`, ...) don't always return the *first* NaN as documented, and `Half` can return a quieted signalling NaN. Minor doc mismatch, no repro file.
- `Sigmoid` throws for an empty span. That's documented, unlike the other element-wise ops.
- The incremental `CborReader` fails a few tokens later than a one-shot reader on a declared length that exceeds the buffer. That's inherent to streaming; the outcome is the same.
- The web encoders always escape the U+FFFD that replaces invalid input (lone surrogates, bad UTF-8), even when U+FFFD itself is allowed. That's deliberate in `OptimizedInboxTextEncoder`.
- `Matcher.AddInclude` throws `ArgumentException` for `..` anywhere but at the start of a pattern. Documented.
- `Encoding.Latin1` best-fits some characters above U+00FF (`Ā` becomes `A`) instead of writing `?`, and replaces each half of a surrogate pair separately. Long-standing behavior.
- `OrdinalIgnoreCase` doesn't treat `ſ`, `ı` or the Kelvin sign as equal to ASCII letters, so the ASCII-only frozen collection strategies stay correct in Release builds.
- `UnmanagedMemoryAccessor.ReadArray`/`WriteArray` throw when `position == Capacity`. That's intentional (`PositionLessThanCapacityRequired`), although the docs only mention `position > Capacity`.
- `Ascii.ToUpper` and `Convert.FromHexString` return `DestinationTooSmall` rather than `InvalidData` when the destination fills up before the bad input is reached. Either answer is reasonable.
- A failed `TryFormat` (destination too small) may leave partial data in the destination buffer for DateTime and the other formatters. `charsWritten` is 0 and the return is false, which is all the contract promises; the buffer contents are undefined on failure.
- `IPAddress.TryParse` accepts forms `IPNetwork` callers might not expect (a bare `10` as `0.0.0.10`, IPv4 with fewer than four parts), so `10/8` parses as the network `0.0.0.0/8`. That is `IPAddress`'s long-standing inet_aton-style behavior, not an `IPNetwork` bug.
- The SIMD `Vector`/`Matrix4x4`/`Quaternion` dot products and matrix multiply reassociate their sums, so they differ from a left-to-right scalar sum by a rounding step, and with infinities of opposite sign one path gives `Inf` where the other gives `NaN`. That's allowed floating-point non-associativity, so the vector fuzzer compares those within a conditioning-aware tolerance.
- `ZstandardDecoder.Decompress` throws `InvalidDataException` for a frame that names a dictionary it wasn't given, and `IOException` for one that needs a bigger window than `maxWindowLog2`, instead of returning `OperationStatus.InvalidData`. That's deliberate (the code calls these errors "actionable by the caller"). But untrusted input can trigger both, so callers need a try/catch as well as the status check, and `InvalidDataException` isn't in the method's documented exceptions. The one-shot `TryDecompress` just returns false.
- `CompareInfo.IndexOf("", c)` returns 0 when `c` is ignorable (`\0`, `\u200D`). An ignorable value matches at the start of any string, including the empty one. My oracle wrongly required the index to be less than the source length.
- `BigInteger`: about 290K executions over the new kernels (repeated limbs, factors of 3/5/7, `B^k - 1` divisors, Toom-sized operands) turned up nothing.

## Coverage

Clean runs, with the known issues above tolerated so the fuzzers could get past them:

| Target | Fuzzer | Executions |
|---|---|---|
| Existing 19 DotnetFuzzing targets (15 min each) | existing | about 170M |
| TensorPrimitives on `argmin-blocks` and `tensorprimitives-block-reductions`, 128/256/512-bit | `TensorPrimitivesFuzzer` | 1.9M, no findings |
| TensorPrimitives on main | `TensorPrimitivesFuzzer` | 0.45M |
| Tensor/TensorSpan constructors and views | `TensorSpanFuzzer` | 3.7M |
| Tensor operations over strided/broadcast views | `TensorOperationsFuzzer` | 25M |
| TensorPrimitives math, integer and conversion ops | `TensorPrimitivesMathFuzzer` | 15M |
| CborReader, all modes plus incremental reading | `CborReaderFuzzer` | 17M |
| SseParser, sync and async, small buffer limits | `SseParserFuzzer` | 22M |
| HttpClient response parsing (HTTP/1.1, h2c, decompression) | `HttpClientResponseFuzzer` | 0.3M on a Debug build (only finding 19), then 3.7M on a Release build with no findings |
| Brotli decode/encode, three APIs each | `BrotliFuzzer` | 0.23M on a Release build (quality 11 round-trips are slow) |
| MailAddress, MailAddressCollection, ContentType, ContentDisposition, CookieContainer.SetCookies | `NetHeaderParsersFuzzer` | 1.6M |
| System.IO.Hashing: CRC-32/CRC-64 parameter sets, XxHash32/64/3/128, Adler32, vectorized and scalar | `HashingFuzzer` | 0.4M |
| Managed `HttpListener` request parsing over loopback | `HttpListenerFuzzer` | 7.1M |
| Managed NTLM and SPNEGO client | `ManagedNtlmFuzzer` | 8.6M |
| `Matcher` (FileSystemGlobbing) against a reference glob matcher | `GlobbingFuzzer` | 11.8M, no findings |
| HTML, JavaScript and URL encoders, built-in and custom settings | `TextEncodingsWebFuzzer` | 0.8M, no findings |
| `BigInteger` against a word-based reference, operands aimed at the special-case kernels | `BigIntegerFuzzer` | 0.29M (large operands, about 35 exec/s), no findings |
| `Complex<T>` for double, float and Half | `ComplexFuzzer` | 64M |
| `FrozenDictionary`/`FrozenSet`, every string strategy plus integer and enum keys | `FrozenCollectionsFuzzer` | 6.3M |
| `PEReader`/`MetadataReader` over guard-paged images, PDBs included | `MetadataReaderFuzzer` | 0.1M (Debug build, then Release) |
| BitArray, Ascii, hex, UnmanagedMemoryAccessor, MemoryMarshal, OrdinalIgnoreCase, Latin-1 | `UnsafeBuffersFuzzer` | 2.8M |
| MemoryExtensions search and comparison (SpanHelpers), eight element types | `SpanHelpersFuzzer` | 0.3M |
| Date/time formatting and parsing, 8 cultures incl. non-Gregorian calendars | `DateTimeFuzzer` | 41M |
| Utf8Parser/Utf8Formatter round-trips over guard-paged input | `Utf8ParserFormatterFuzzer` | 72M |
| Number parsing from UTF-8 vs UTF-16, custom NumberFormatInfo symbols | `NumberParsingUtf8Fuzzer` | 4.4M (finding 44) |
| Vector2/3/4, Matrix4x4, Quaternion, Plane vs scalar references | `NumericsVectorsFuzzer` | 2.4M (SIMD mostly inlined, low coverage) |
| IPNetwork and IPEndPoint parsing, string/span/UTF-8 | `IPNetworkEndPointFuzzer` | 0.7M (finding 45) |
| HTTP header value parsers (MediaType, CacheControl, ...), round-trip | `HttpHeaderValuesFuzzer` | 0.9M |
| ICU globalization interop: sort keys, culture-aware search, normalization, IDN, casing, guard-paged buffers | `GlobalizationIcuFuzzer` | in progress (findings 48 to 54) |
| Span compression codecs over native zlib-ng and zstd: Deflate/ZLib/GZip/Zstandard encoders and decoders vs the Stream classes | `CompressionCodecsFuzzer` | in progress (findings 55 and 56) |

A planted tie-breaking bug in `argmin-blocks` was caught by the saved corpus in under a second, so the clean result on those branches means something.

## Running the fuzzers

The new fuzzers are in `src/libraries/Fuzzing/DotnetFuzzing/Fuzzers` next to the existing ones. They compare against a scalar or spec reference implementation, not just "doesn't crash", and they skip the known issues above unless you set `TENSOR_FUZZ_STRICT=1`.

The upstream harness is Windows-only. On Linux I built libfuzzer-dotnet with `clang -fsanitize=fuzzer`, instrumented the target assembly with the SharpFuzz CLI built from source, and ran:

```sh
libfuzzer-dotnet --target_path=<publish dir>/DotnetFuzzing --target_arg=CborReaderFuzzer -fork=1 -ignore_crashes=1 corpus/
```

`libfuzzer-dotnet` leaks one SysV shared memory segment per crashing child. After a few thousand crashes `shmget()` fails and every job dies at startup, which shows up as thousands of zero-length "crashes". `ipcrm -m` on segments with no attached processes fixes it; the campaigns here ran a small cleaner loop for that.

For vector-width-specific code, `DOTNET_PreferredVectorBitWidth=512`, `DOTNET_EnableAVX512=0` and `DOTNET_EnableAVX=0` select the 512, 256 and 128-bit paths.

## Findings on main only

Findings 1 and 2 are regressions after 11.0 RC1, so `dotnet run` prints `NOT REPRODUCED` with a released SDK. Build the runtime from this repo (`./build.sh clr+libs`) and use the helper, which builds the repro and runs it on the local testhost:

```sh
./run-on-local-runtime.sh repros/01-SslStream-ZeroLengthRecord.cs
```
