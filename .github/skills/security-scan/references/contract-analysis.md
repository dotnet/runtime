# Contract Analysis

How to systematically analyze API contracts and find where they can be abused.

## Contents

- [What is a contract?](#what-is-a-contract)
- [Analysis workflow](#analysis-workflow)
- [Violation patterns](#contract-violation-patterns)
- [Cross-component contracts](#cross-component-contracts)

## What Is a Contract?

The set of assumptions a public API makes about its inputs, the guarantees it provides to callers, and the invariants it must maintain. Security bugs emerge when callers (internal or external) violate these contracts — intentionally or accidentally.

Contracts exist at three levels:
1. **Explicit** — documented via XML docs, `<exception>` tags, `ArgumentException` throws
2. **Implicit** — assumed but not enforced (e.g., "this stream is seekable", "this length was already bounds-checked")
3. **Emergent** — arise from how multiple components interact (A's output becomes B's input)

Implicit and emergent contracts are where most security bugs hide.

## Analysis Workflow

### Step 1: Identify the Public Surface

Find all `public` and `protected` methods on public types in the target library:

```bash
grep -rn "public\s\+\(static\s\+\)\?\(async\s\+\)\?\S\+\s\+\w\+\s*(" src/libraries/<Lib>/src/ --include="*.cs" | grep -v "/ref/" | grep -v "/tests/"
```

Focus on methods that accept potentially untrusted input: `string`, `byte[]`, `Stream`, `ReadOnlySpan<byte>`, `ReadOnlyMemory<byte>`, `ReadOnlySequence<byte>`.

### Step 2: Map Trust Boundaries

For each public entry point, determine:

| Question | How to check |
|---|---|
| Can an attacker control this parameter? | Trace callers — does input originate from network, file, or user input? |
| Is the input pre-validated by the caller? | Check if callers consistently validate before calling. If inconsistent → contract gap |
| Does this cross a process/serialization boundary? | Look for `[DllImport]`, serializer calls, HTTP handlers |
| What privilege level does this code run at? | Check for `[SecurityCritical]`, `SafeHandle` usage, native resource access |

### Step 3: Document Implicit Contracts

Read the method body and identify assumptions that aren't validated:

- **Size assumptions**: "This buffer is at most 4KB" — but is that enforced?
- **State assumptions**: "Caller already called Initialize()" — but what if they didn't?
- **Encoding assumptions**: "This string is valid UTF-8" — but what if it contains malformed sequences?
- **Concurrency assumptions**: "Only one thread calls this" — but is that enforced?
- **Ownership assumptions**: "Caller won't modify the array after passing it" — but arrays are mutable

### Step 4: Find Contract Gaps

Look for these patterns:

1. **Missing validation on public APIs**: The method assumes valid input but doesn't check. Internal callers validate, but nothing stops external callers from passing garbage.

2. **Inconsistent overloads**: One overload validates a parameter, another accepts the same logical input without validation.
   ```csharp
   // This overload validates:
   public void Write(string text) { ArgumentNullException.ThrowIfNull(text); ... }
   // This overload doesn't:
   public void Write(ReadOnlySpan<char> text) { /* no null/length check, just uses it */ }
   ```

3. **Validation-then-use gap**: Input is validated, then used later in a different context where the validation no longer applies (e.g., validated as a filename, then used in a SQL query).

4. **Error path leaks**: Exception messages or error responses that expose internal state (file paths, stack traces, connection strings).

### Step 5: Trace Cross-Component Contracts

When component A calls component B's public API:
- Does A satisfy B's documented preconditions?
- Does A rely on guarantees B doesn't actually provide?
- If B changes its behavior (within its documented contract), does A break?

Search for cross-component calls:
```bash
# Find where System.Text.Json is called from System.Net.Http
grep -rn "JsonSerializer\|JsonDocument\|Utf8JsonReader" src/libraries/System.Net.Http/src/
```

## Contract Violation Patterns

### Assumed-Valid Input

**What**: API skips validation because "callers should have checked."

**Why it's dangerous**: Internal callers may validate consistently, but public API consumers don't know the implicit contract. New internal callers may also forget.

**Detection**: Look for public methods that use parameters directly in `unsafe` blocks, native interop calls, or array indexing without bounds checks.

### Inconsistent Overloads

**What**: Multiple overloads of the same logical operation apply different validation rules.

**Detection**: Compare all overloads of a public method. Do they all validate the same constraints?

### Silent Truncation

**What**: API truncates oversized input instead of rejecting it.

**Why it's dangerous**: Caller may rely on the full input being processed. Truncation can bypass length-based security checks upstream.

**Detection**: Look for `Math.Min(input.Length, maxSize)` patterns without throwing when `input.Length > maxSize`.

### Type Confusion in Generics

**What**: Generic API behaves differently based on runtime type, and caller doesn't expect type-dependent behavior.

**Detection**: Look for `typeof(T)` checks, `is` patterns, or `Unsafe.As<T>` inside generic methods.

### State-Dependent Safety

**What**: API is safe only when called in a specific order, but nothing enforces the ordering.

**Detection**: Look for methods that check `_isInitialized`, `_disposed`, or similar state flags. Are these checks in ALL public entry points?

## Downstream Impact Assessment

For each contract gap found, assess:

1. **Who calls this API?** — Is it used by ASP.NET Core, EF Core, SDK, or other framework components?
2. **Can an external attacker reach this through a downstream caller?** — Trace the call chain from HTTP endpoint to the vulnerable API
3. **What would happen if a consumer misuses this?** — Document the concrete failure mode (crash, memory corruption, data leak, etc.)

Record findings in the anti-pattern catalog (see [anti-patterns/README.md](anti-patterns/README.md)) with the `Downstream impact` field.
