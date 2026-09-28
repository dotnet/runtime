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
| HttpClient response parsing (HTTP/1.1, h2c, decompression) | `HttpClientResponseFuzzer` | 0.3M on a Debug build (only finding 19), Release build run in progress |

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
