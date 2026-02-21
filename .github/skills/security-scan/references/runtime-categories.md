# Security Categories for dotnet/runtime

## Contents

- [Unsafe code and memory safety](#unsafe-code--memory-safety)
- [Serialization](#serialization--deserialization)
- [Cryptography](#cryptography)
- [Input validation and injection](#input-validation--injection)
- [Authentication and authorization](#authentication--authorization)
- [Networking](#networking--web)
- [Native interop](#native-interop)
- [General categories](#general-categories)
- [DOS and resource exhaustion](#dos--resource-exhaustion)
- [API contract abuse](#api-contract-abuse)

## dotnet/runtime-Specific Concerns

### Unsafe Code & Memory Safety

- `unsafe` blocks with pointer arithmetic, `Span<T>`/`Memory<T>` misuse
- Buffer overflows in native interop (`DllImport`, `LibraryImport`, `Marshal.*`)
- Use-after-free in `SafeHandle`/`CriticalHandle` implementations
- Stack buffer overflows via `stackalloc` without bounds checking
- `Unsafe.As<T>` type punning that violates type safety

### Serialization & Deserialization

- `BinaryFormatter`, `SoapFormatter`, or other insecure deserializers
- `TypeNameHandling` in JSON serialization allowing type injection
- Custom `TypeConverter` or `SerializationBinder` that don't restrict types
- XML deserialization without restricting allowed types (XXE, type injection)
- `System.Text.Json` custom converters that trust input type discriminators

### Cryptography

- Obsolete algorithms (MD5, SHA1 for security, DES, RC2, 3DES)
- Hardcoded keys, IVs, or salts
- Missing or incorrect padding modes
- Non-constant-time comparisons of secrets/MACs (use `CryptographicOperations.FixedTimeEquals`)
- `Random` instead of `RandomNumberGenerator` for security purposes
- Certificate validation bypass (`ServerCertificateCustomValidationCallback` returning `true`)

### Input Validation & Injection

- Path traversal via unsanitized `Path.Combine` or `Path.GetFullPath`
- Command injection via `Process.Start` with unsanitized arguments
- LDAP injection, XPath injection in query construction
- ReDoS with untrusted patterns — **only if the regex processes external input**
- Format string vulnerabilities in native code (`sprintf` without bounds)

### Authentication & Authorization

- Bypassing security checks via reflection or `BindingFlags.NonPublic`
- `AllowPartiallyTrustedCallers` misuse
- Elevation of privilege through assembly loading or code generation
- Missing permission demands on security-critical operations

### Networking & Web

- SSRF via user-controlled URLs (only if attacker can control host/protocol)
- TLS downgrade or missing certificate validation
- Cookie handling without `Secure`/`HttpOnly` flags
- HTTP header injection via unsanitized values

### Native Interop

- Buffer size mismatches between managed and native code
- Missing null checks on pointers returned from native calls
- Incorrect `MarshalAs` attributes leading to memory corruption
- Missing `SetLastError = true` on P/Invoke that checks `GetLastError`

## General Categories

### Injection Attacks

- SQL injection via string concatenation in queries
- Command injection in system calls or subprocesses
- XXE injection in XML parsing (missing `DtdProcessing.Prohibit`)
- Template injection in string formatting with untrusted input

### Data Exposure

- Sensitive data (keys, tokens, PII) in log output
- Debug information leaking in release builds
- Exception messages exposing internal paths or state
- Timing side channels leaking secret-dependent information

## DOS & Resource Exhaustion

Applicable when scanning public API surface reachable by external callers:

- Unbounded stream reads (`ReadToEnd`, `CopyTo`) on untrusted input
- Allocations sized by external input (`new byte[userSize]`) without upper bounds
- `JsonDocument.Parse` / `JsonSerializer.Deserialize` on unbounded streams
- `StringBuilder` / `MemoryStream` created without capacity on untrusted input
- Collection growth in loops where iteration count is attacker-controlled
- XML parsing without `MaxCharactersInDocument` / `MaxCharactersFromEntities`
- Regular expression evaluation on attacker-controlled input without timeout
- Recursive data structures causing stack overflow (deeply nested JSON/XML)

For detailed anti-patterns, see [anti-patterns/README.md](anti-patterns/README.md) (AP-001, AP-002, AP-005, AP-015).

## API Contract Abuse

Vulnerabilities arising from misunderstood or violated API contracts:

- Public APIs that assume pre-validated input but don't enforce it
- Inconsistent validation across overloads of the same method
- Silent truncation of oversized input (bypasses upstream length checks)
- State-dependent APIs callable in wrong order (missing initialization checks)
- Cross-component contract mismatches (A's output violates B's preconditions)
- Implicit encoding/format assumptions not enforced at the boundary

For the full contract analysis methodology, see [contract-analysis.md](contract-analysis.md).
