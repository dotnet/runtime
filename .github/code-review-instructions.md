---
excludeAgent: coding-agent
---

# Code Review Instructions for dotnet/runtime

These instructions guide code reviews for the dotnet/runtime repository. A compiler runs on every PR (as do a wealth of static analyzers for C# changes), so focus on higher-level concerns that require expert judgment rather than stylistic or syntactic issues.

## Review Priorities

### 1. Security

**Critical Security Concerns:**
- **Input Validation & Sanitization**: Ensure all external inputs (user data, file I/O, network data) are properly validated and sanitized before use
- **Injection Vulnerabilities**: Check for potential SQL injection, command injection, path traversal, or code injection risks
- **Cryptographic Operations**: Verify proper use of cryptographic APIs, secure random number generation, and correct handling of keys/certificates
- **Buffer Overflows**: In native code, check for proper bounds checking and safe memory operations
- **Authentication & Authorization**: Ensure proper access control checks are in place and cannot be bypassed
- **Information Disclosure**: Watch for accidental logging or exposure of sensitive data (credentials, keys, PII)
- **Denial of Service**: Check for potential infinite loops, resource exhaustion, or algorithmic complexity attacks
- **Deserialization**: Ensure safe deserialization practices, especially with untrusted data
- **Race Conditions**: Identify potential TOCTOU (time-of-check-time-of-use) vulnerabilities in security-sensitive operations

### 2. Performance

**Performance Considerations:**
- **Algorithmic Complexity**: Identify inefficient algorithms (O(n²) where O(n) is possible, unnecessary allocations)
- **Memory Allocations**: Watch for excessive allocations in hot paths, consider stack allocation (stackalloc) or object pooling where appropriate
- **Boxing**: Identify unnecessary boxing of value types
- **String Operations**: Check for string concatenation in loops (use StringBuilder), excessive string allocations
- **LINQ Performance**: Evaluate LINQ usage in performance-critical paths; consider more direct alternatives
- **Async/Await Overhead**: Ensure async/await is used appropriately (not for trivial synchronous operations)
- **Collection Choices**: Verify appropriate collection types for access patterns (List vs. HashSet vs. Dictionary)
- **Lazy Initialization**: Check for opportunities to defer expensive operations
- **Caching**: Identify repeated expensive computations that could be cached
- **Span<T> and Memory<T>**: Encourage use of modern memory-efficient types for buffer manipulation
- **Native Interop**: Ensure P/Invoke calls are efficient and properly marshaled

### 3. Backwards Compatibility

**Compatibility Requirements:**
- **Public API Changes**: Any change to public APIs requires careful scrutiny
  - Breaking changes are generally not acceptable
  - New optional parameters, overloads, and interface implementations need careful consideration
  - Verify that API additions follow existing patterns and naming conventions
- **Serialization Compatibility**: Ensure changes don't break serialization/deserialization of persisted data
- **Configuration Changes**: Changes to configuration format or behavior must maintain backwards compatibility
- **Binary Compatibility**: IL-level changes must preserve binary compatibility for existing assemblies
- **Behavioral Changes**: Even non-breaking API changes can break consumers if behavior changes unexpectedly
  - Document behavioral changes clearly
  - Consider feature flags or opt-in mechanisms for significant behavior changes
- **Obsolete APIs**: Check that proper obsolescence process is followed (attributes, documentation, migration path)

### 4. Cross-Component Interactions

**Integration Points:**
- **Runtime/Library Boundaries**: Changes affecting CoreCLR, Mono, or NativeAOT must work across all runtimes
- **Platform Differences**: Ensure changes work correctly across Windows, Linux, macOS, and other supported platforms
- **Architecture Considerations**: Verify behavior is correct on x86, x64, ARM32, and ARM64
- **Dependencies**: Changes to core libraries may impact higher-level libraries; consider cascading effects
- **Threading Models**: Ensure thread-safety is maintained and synchronization primitives are used correctly
- **Lifecycle Management**: Verify proper initialization, disposal patterns, and cleanup across component boundaries
- **Shared State**: Carefully review any shared mutable state for thread-safety and consistency
- **Error Handling**: Ensure exceptions and error codes are properly propagated across component boundaries

### 5. Correctness and Edge Cases

**Code Correctness:**
- **Null Handling**: While the compiler enforces nullable reference types, verify runtime null checks are appropriate
- **Boundary Conditions**: Test for off-by-one errors, empty collections, null inputs, maximum values
- **Error Paths**: Ensure error handling is correct and complete; resources are properly cleaned up
- **Concurrency**: Identify race conditions, deadlocks, or improper synchronization
- **Exception Safety**: Verify operations maintain invariants even when exceptions occur
- **Resource Management**: Ensure IDisposable is implemented correctly and resources are not leaked
- **Numeric Overflow**: Check for potential integer overflow/underflow in calculations
- **Culture/Locale Issues**: Verify culture-invariant operations where appropriate, proper localization otherwise
- **Time Handling**: Check for timezone, daylight saving, and leap second handling issues

### 6. Design and Architecture

**Design Quality:**
- **API Design**: Ensure new APIs are intuitive, follow framework design guidelines, and are hard to misuse
- **Abstraction Level**: Verify abstractions are at the appropriate level and don't leak implementation details
- **Separation of Concerns**: Check that responsibilities are properly separated
- **Extensibility**: Consider whether the design allows for future extension without breaking changes
- **SOLID Principles**: Evaluate adherence to single responsibility, open/closed, and other design principles
- **Code Duplication**: Identify opportunities to reduce duplication while maintaining clarity
- **Testability**: Ensure the code is designed to be testable (proper dependency injection, separation of concerns)

### 7. Testing

**Test Quality:**
- **Coverage**: Ensure new functionality has appropriate test coverage
- **Test Scenarios**: Verify tests cover happy paths, error paths, and edge cases
- **Test Reliability**: Watch for flaky tests (timing dependencies, environmental assumptions)
- **Test Performance**: Ensure tests run efficiently and don't unnecessarily slow down CI
- **Platform-Specific Tests**: Verify platform-specific tests are properly conditioned
- **Regression Tests**: Check that bugs being fixed have corresponding regression tests

### 8. Documentation and Code Clarity

**Documentation:**
- **XML Documentation**: New public APIs must have clear XML documentation explaining purpose, parameters, return values, and exceptions. Do not comment on existing APIs that lack documentation.
- **Complex Logic**: Comments should explain the "why" behind non-obvious decisions, not restate what the code does
- **TODOs and FIXMEs**: Ensure they are tracked with issues and are appropriate for the change
- **Breaking Changes**: Must be clearly documented with migration guidance

## What NOT to Focus On

The following are handled by automated tooling and don't need review comments:

- Code formatting and style (handled by `.editorconfig` and analyzers)
- Naming convention violations (handled by analyzers)
- Missing using directives (handled by compiler)
- Most syntax errors (handled by compiler)
- Simple code style preferences without technical merit

## Review Approach

1. **Understand the Context**: Read the PR description and linked issues to understand the goal. Consider as much relevant code from the containing project as possible. For public APIs, review any code in the repo that consumes the method.
2. **Assess the Scope**: Verify the change is focused and not mixing unrelated concerns
3. **Evaluate Risk**: Consider the risk level based on what components are affected and how widely used they are
4. **Think Like an Attacker**: For security-sensitive code, consider how it might be exploited
5. **Think Like a Consumer**: Consider how the API will be used and potentially misused
6. **Consider Maintenance**: Think about long-term maintenance burden and technical debt

## Severity Guidelines

- **Critical**: Security vulnerabilities, data corruption, crashes, breaking changes
- **High**: Performance regressions, resource leaks, incorrect behavior in common scenarios
- **Medium**: Edge case bugs, suboptimal design, missing documentation
- **Low**: Code clarity issues, minor inefficiencies, nice-to-have improvements
