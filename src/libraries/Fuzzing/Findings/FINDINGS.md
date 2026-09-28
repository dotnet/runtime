# SharpFuzz findings

Results from fuzzing dotnet/runtime `main` (commit `48f53a10e`) with [SharpFuzz](https://github.com/Metalnem/sharpfuzz) and libFuzzer on Linux x64. The runtime was built from source (`clr+libs`, Release runtime, Debug libraries) and the fuzz targets live in `src/libraries/Fuzzing/DotnetFuzzing/Fuzzers`.

Every finding links to a standalone repro. They're [file-based apps](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps), so all you need is a .NET 10 or newer SDK:

```sh
cd src/libraries/Fuzzing/Findings/repros
dotnet run 03-Tensor-ToString-OutOfBoundsRead.cs
```

Each repro prints what it observed next to what was expected and ends with `REPRODUCED` or `NOT REPRODUCED`. The ones that need a NuGet package pin the 11.0 RC1 version with `#:package`, so they show the bug in shipped bits. This folder has its own `global.json`, `NuGet.config` and empty `Directory.Build.*` files so the repros don't pick up the runtime repo's build setup.

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

## Things that looked like bugs but aren't

- `NrbfDecoderFuzzer` OOM on the repo's own seed `largeArrayOfNulls.nrbf`: the input asks for an `Array.MaxLength` array, and the `ArrayRecord.GetArray` docs tell callers to check `Lengths` first. It only fails on machines that can't allocate 16 GB.
- NaN-propagating reductions (`Max`, `Min`, ...) don't always return the *first* NaN as documented, and `Half` can return a quieted signalling NaN. Minor doc mismatch, no repro file.
- `Sigmoid` throws for an empty span. That's documented, unlike the other element-wise ops.
- The incremental `CborReader` fails a few tokens later than a one-shot reader on a declared length that exceeds the buffer. That's inherent to streaming; the outcome is the same.

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

A planted tie-breaking bug in `argmin-blocks` was caught by the saved corpus in under a second, so the clean result on those branches means something.

## Running the fuzzers

The new fuzzers are in `src/libraries/Fuzzing/DotnetFuzzing/Fuzzers` next to the existing ones. They compare against a scalar or spec reference implementation, not just "doesn't crash", and they skip the known issues above unless you set `TENSOR_FUZZ_STRICT=1`.

The upstream harness is Windows-only. On Linux I built libfuzzer-dotnet with `clang -fsanitize=fuzzer`, instrumented the target assembly with the SharpFuzz CLI built from source, and ran:

```sh
libfuzzer-dotnet --target_path=<publish dir>/DotnetFuzzing --target_arg=CborReaderFuzzer -fork=1 -ignore_crashes=1 corpus/
```

For vector-width-specific code, `DOTNET_PreferredVectorBitWidth=512`, `DOTNET_EnableAVX512=0` and `DOTNET_EnableAVX=0` select the 512, 256 and 128-bit paths.

## Findings on main only

Findings 1 and 2 are regressions after 11.0 RC1, so `dotnet run` prints `NOT REPRODUCED` with a released SDK. Build the runtime from this repo (`./build.sh clr+libs`) and use the helper, which builds the repro and runs it on the local testhost:

```sh
./run-on-local-runtime.sh repros/01-SslStream-ZeroLengthRecord.cs
```
