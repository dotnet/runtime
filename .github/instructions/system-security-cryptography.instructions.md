---
applyTo: "src/libraries/System.Security.Cryptography/**"
---

# System.Security.Cryptography — Folder-Specific Guidance

## Code Style

- Prefer scoped `using (...) { ... }` statements over `using` declarations (`using var ...`) so resource lifetimes and disposal scopes are explicit. This reinforces `csharp_prefer_simple_using_statement = false:none` in `.editorconfig`.
- When an `if` statement follows another statement in the same block, insert a blank line before the `if`. Do not add an artificial leading blank line when the `if` is the first statement in a block.

## Correctness

- Check the success result of `Try*` methods and any bytes-written value, whether returned directly or through an `out` parameter. When control flow guarantees an expected result or byte count, use `Debug.Assert`, a parameterless `CryptographicException`, or both, as appropriate.

## Security

- Clear owned writable buffers containing keys or other secret material with `CryptographicOperations.ZeroMemory` as soon as they are no longer needed. For rented buffers, return them with `ArrayPool<T>.Return(..., clearArray: true)`.
- Use `CryptographicOperations.FixedTimeEquals` for secret-dependent comparisons; do not implement ad hoc comparison loops or use ordinary sequence equality.

## Tests

- Avoid throwing `SkipTestException`, which creates noisy test output. Prefer `[ConditionalFact]` or `[ConditionalTheory]` with a condition; when that is not possible, return early from the test.
