# Security Anti-Pattern Catalog

Structured catalog of security anti-patterns for .NET runtime APIs. Each entry is designed to be reusable when scanning downstream repos (ASP.NET Core, SDK, etc.).

## Contents

- [Format specification](#format)
- [AP-001: Unbounded stream read](#ap-001-unbounded-stream-read-from-untrusted-source)
- [AP-002: Unbounded allocation from external size](#ap-002-unbounded-allocation-from-external-size)
- [AP-003: BinaryFormatter usage](#ap-003-binaryformatter-usage)
- [AP-004: TypeNameHandling without binder](#ap-004-typenamehandling-without-restricted-binder)
- [AP-005: JsonDocument parse without limits](#ap-005-jsondocument-parse-without-size-limits)
- [AP-006: XmlReader without DTD prohibition](#ap-006-xmlreader-without-dtd-prohibition)
- [AP-007: Unsanitized path combination](#ap-007-unsanitized-path-combination)
- [AP-008: Process start with external arguments](#ap-008-process-start-with-external-arguments)
- [AP-009: Non-constant-time secret comparison](#ap-009-non-constant-time-secret-comparison)
- [AP-010: Marshal buffer size mismatch](#ap-010-marshal-buffer-size-mismatch)
- [AP-011: Public API missing input validation](#ap-011-public-api-missing-input-validation)
- [AP-012: Inconsistent overload validation](#ap-012-inconsistent-overload-validation)
- [AP-013: Certificate validation bypass](#ap-013-certificate-validation-bypass)
- [AP-014: ISerializable trusting SerializationInfo](#ap-014-iserializable-trusting-serializationinfo)
- [AP-015: Collection growth from untrusted input](#ap-015-collection-growth-from-untrusted-input)

## Format

Each anti-pattern follows this structure:

```
### AP-NNN: <Title>
- **Component**: Which library/namespace this applies to
- **API**: Specific APIs involved
- **Risk**: What happens when this pattern is present (DOS, RCE, data leak, etc.)
- **Contract violated**: What assumption is broken
- **Detection**: How to find this pattern (grep, code inspection)
- **Fix**: How to remediate
- **Downstream impact**: How this affects consumer repos (ASP.NET Core, SDK, etc.)
```

---

### AP-001: Unbounded stream read from untrusted source

- **Component**: System.IO, System.Net.Http
- **API**: `StreamReader.ReadToEnd()`, `StreamReader.ReadToEndAsync()`, `HttpContent.ReadAsStringAsync()`, `HttpContent.ReadAsByteArrayAsync()`
- **Risk**: DOS — attacker sends multi-GB response/request body, causing OOM
- **Contract violated**: Caller assumes the source is bounded; the API imposes no limit
- **Detection**: `grep -rn "ReadToEnd\|ReadAsStringAsync\|ReadAsByteArrayAsync"` — flag when the stream source is network, file upload, or other external input
- **Fix**: Use `ReadAsync` with a bounded buffer and byte count limit. For HTTP, set `HttpClient.MaxResponseContentBufferSize`. For request bodies, enforce `Content-Length` limits at the middleware level.
- **Downstream impact**: ASP.NET Core middleware reading request bodies (`Request.Body`), `HttpClient` consumers, file upload handlers

### AP-002: Unbounded allocation from external size

- **Component**: System.Runtime, System.Buffers
- **API**: `new byte[size]`, `new char[size]`, `ArrayPool.Rent(size)`, `Marshal.AllocHGlobal(size)`
- **Risk**: DOS — attacker controls `size` parameter, causes OOM or excessive allocation
- **Contract violated**: Caller assumes `size` is reasonable; no upper bound enforced
- **Detection**: `grep -rn "new byte\[.*\]\|new char\[.*\]\|ArrayPool.*Rent\|AllocHGlobal"` — flag when the size value traces to external input (HTTP header, content-length, deserialized field)
- **Fix**: Validate `size` against a maximum before allocating. Use `ArgumentOutOfRangeException.ThrowIfGreaterThan(size, maxAllowed)`.
- **Downstream impact**: Any consumer that allocates buffers based on protocol headers or deserialized fields

### AP-003: BinaryFormatter usage

- **Component**: System.Runtime.Serialization
- **API**: `BinaryFormatter.Deserialize()`, `BinaryFormatter.Serialize()`
- **Risk**: RCE — arbitrary type instantiation via known gadget chains
- **Contract violated**: Fundamental — no safe configuration exists
- **Detection**: `grep -rn "BinaryFormatter\|SoapFormatter"`
- **Fix**: Replace with `System.Text.Json`, `MessagePack`, or `protobuf-net`. For legacy compat, use a restricted `SerializationBinder` as a stopgap only.
- **Downstream impact**: Any assembly that deserializes data from untrusted sources using BinaryFormatter

### AP-004: TypeNameHandling without restricted binder

- **Component**: Newtonsoft.Json
- **API**: `JsonSerializerSettings.TypeNameHandling` set to anything other than `None`
- **Risk**: RCE — attacker injects `$type` in JSON to instantiate arbitrary types
- **Contract violated**: Serializer assumes type metadata is trusted
- **Detection**: `grep -rn "TypeNameHandling"` — flag if value is not `None` or if `SerializationBinder` is absent/unrestricted
- **Fix**: Set `TypeNameHandling = None`. If polymorphism needed, use `System.Text.Json` with explicit `[JsonDerivedType]` attributes.
- **Downstream impact**: ASP.NET Core JSON endpoints, SignalR message handling, any API accepting JSON with type metadata

### AP-005: JsonDocument parse without size limits

- **Component**: System.Text.Json
- **API**: `JsonDocument.Parse(stream)`, `JsonDocument.ParseAsync(stream)`, `JsonSerializer.Deserialize(stream)`
- **Risk**: DOS — multi-GB JSON payload exhausts memory. Deeply nested JSON (>64 levels default) can also cause stack overflow.
- **Contract violated**: Caller assumes input is well-formed and reasonably sized
- **Detection**: `grep -rn "JsonDocument.Parse\|JsonSerializer.Deserialize"` — flag when input stream is from network/file without prior size validation
- **Fix**: Set `JsonDocumentOptions.MaxDepth` / `JsonSerializerOptions.MaxDepth`. Limit stream size before parsing (e.g., `Content-Length` check or wrapping with a length-limited stream).
- **Downstream impact**: ASP.NET Core model binding, minimal API endpoints, any HTTP handler parsing JSON request bodies

### AP-006: XmlReader without DTD prohibition

- **Component**: System.Xml
- **API**: `XmlReader.Create(stream)` without `XmlReaderSettings.DtdProcessing = DtdProcessing.Prohibit`
- **Risk**: XXE — external entity injection can read local files or trigger SSRF
- **Contract violated**: Caller assumes XML is data-only; DTD processing enables active content
- **Detection**: `grep -rn "XmlReader.Create"` — flag if `DtdProcessing` is not set to `Prohibit`
- **Fix**: Always pass `new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }`.
- **Downstream impact**: ASP.NET Core XML formatters, SOAP service consumers, config file parsers

### AP-007: Unsanitized path combination

- **Component**: System.IO
- **API**: `Path.Combine(basePath, userInput)`, `Path.GetFullPath(userInput)`
- **Risk**: Path traversal — attacker uses `../` to escape intended directory
- **Contract violated**: Caller assumes `userInput` is a relative filename; API allows absolute paths and traversal
- **Detection**: `grep -rn "Path.Combine\|Path.GetFullPath"` — flag when second argument traces to user input
- **Fix**: After combining, verify the result starts with the expected base directory: `Path.GetFullPath(combined).StartsWith(Path.GetFullPath(basePath))`.
- **Downstream impact**: File upload handlers, static file middleware, template engines

### AP-008: Process start with external arguments

- **Component**: System.Diagnostics
- **API**: `Process.Start(filename, arguments)`, `ProcessStartInfo` with user-controlled properties
- **Risk**: Command injection — attacker injects shell metacharacters in arguments
- **Contract violated**: Caller assumes arguments are data; shell interpretation treats them as commands
- **Detection**: `grep -rn "Process.Start\|ProcessStartInfo"` — flag when arguments trace to user input
- **Fix**: Use argument arrays instead of shell strings. Set `UseShellExecute = false`. Validate/escape all arguments.
- **Downstream impact**: Build tools, code generators, any tool that invokes external processes

### AP-009: Non-constant-time secret comparison

- **Component**: System.Security.Cryptography
- **API**: `==` or `SequenceEqual` on secrets, MACs, tokens, or hashes
- **Risk**: Timing side channel — attacker can determine secret value byte-by-byte
- **Contract violated**: Caller assumes equality comparison doesn't leak information
- **Detection**: `grep -rn "SequenceEqual\|== .*token\|== .*hash\|== .*mac\|== .*secret"` — flag when comparing security-sensitive values
- **Fix**: Use `CryptographicOperations.FixedTimeEquals()`.
- **Downstream impact**: Authentication middleware, token validation, HMAC verification

### AP-010: Marshal buffer size mismatch

- **Component**: System.Runtime.InteropServices
- **API**: `Marshal.Copy`, `Marshal.PtrToStructure`, P/Invoke calls with buffer parameters
- **Risk**: Buffer overflow — managed buffer size doesn't match native expectation, causing memory corruption
- **Contract violated**: Native function assumes buffer is at least N bytes; managed caller allocates fewer
- **Detection**: Review all `[DllImport]`/`[LibraryImport]` declarations where a buffer + length are passed. Compare managed allocation size with native function's documented requirements.
- **Fix**: Always derive buffer size from the same source the native function uses. Add assertions in debug builds.
- **Downstream impact**: Any consumer calling the managed wrapper for native APIs

### AP-011: Public API missing input validation

- **Component**: Any
- **API**: Any `public` method accepting potentially untrusted parameters
- **Risk**: Varies — depends on what the unvalidated input is used for
- **Contract violated**: Implicit assumption that callers pre-validate
- **Detection**: Find `public` methods that use `string`, `byte[]`, `Stream`, or `Span<T>` parameters in unsafe operations, native interop, or array indexing without prior validation
- **Fix**: Add explicit parameter validation (`ArgumentNullException.ThrowIfNull`, bounds checks) at the public API boundary.
- **Downstream impact**: Every consumer of the API inherits the vulnerability

### AP-012: Inconsistent overload validation

- **Component**: Any
- **API**: Multiple overloads of the same method with different validation behavior
- **Risk**: Bypass — attacker uses the unvalidated overload to skip security checks
- **Contract violated**: Caller expects all overloads to enforce the same preconditions
- **Detection**: Compare overloads of public methods. If one validates a parameter and another doesn't, flag the inconsistency.
- **Fix**: Extract validation into a shared helper. Call it from all overloads.
- **Downstream impact**: Consumers using the "wrong" overload unknowingly bypass validation

### AP-013: Certificate validation bypass

- **Component**: System.Net.Security, System.Net.Http
- **API**: `ServerCertificateCustomValidationCallback` returning `true`, `ServicePointManager.ServerCertificateValidationCallback`
- **Risk**: MITM — TLS certificate validation disabled, attacker can intercept traffic
- **Contract violated**: TLS assumes certificate chain is verified
- **Detection**: `grep -rn "CertificateValidationCallback\|ServerCertificateCustomValidation"` — flag if the callback returns `true` unconditionally
- **Fix**: Implement proper certificate validation or remove the callback.
- **Downstream impact**: Any HTTP client or TLS connection in the application

### AP-014: ISerializable trusting SerializationInfo

- **Component**: System.Runtime.Serialization
- **API**: `ISerializable` deserialization constructor `(SerializationInfo info, StreamingContext context)`
- **Risk**: Object injection — attacker controls the values in `SerializationInfo`, enabling construction of objects in unexpected states
- **Contract violated**: Constructor assumes `SerializationInfo` contains valid, well-typed data
- **Detection**: Find types implementing `ISerializable`. Check if the deserialization constructor validates types and ranges of values from `info.GetValue()`.
- **Fix**: Validate every value extracted from `SerializationInfo`. Reject unexpected types or out-of-range values.
- **Downstream impact**: Any code path that deserializes `ISerializable` types from untrusted data

### AP-015: Collection growth from untrusted input

- **Component**: System.Collections, System.Linq
- **API**: `List<T>.Add()` in a loop, `Dictionary.Add()`, `HashSet.Add()`, `Concat()`, `Append()` — when count is attacker-controlled
- **Risk**: DOS — attacker sends payload that causes unbounded collection growth, exhausting memory
- **Contract violated**: Code assumes input count is reasonable; no maximum enforced
- **Detection**: Look for loops adding to collections where the iteration count comes from external input (deserialized array length, HTTP header, protocol field)
- **Fix**: Enforce a maximum count before the loop. Reject input exceeding the limit.
- **Downstream impact**: Request processing pipelines, batch endpoints, any handler that accumulates items from a request
