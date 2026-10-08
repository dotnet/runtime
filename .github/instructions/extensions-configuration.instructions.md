---
applyTo: "src/libraries/Microsoft.Extensions.Configuration*/**"
---

# Microsoft.Extensions.Configuration — Folder-Specific Guidance

## Configuration Binding (D8)

- Configuration key lookups are case-insensitive — all switch statements and dictionary lookups over config keys must use `StringComparison.OrdinalIgnoreCase`
- The default binder suppresses binding errors and silently skips unrecognized properties — document this when code depends on strict binding
- Type conversion failures during binding must produce clear error messages identifying the configuration key and expected type
- `IOptionsMonitor<T>` change notifications must propagate correctly to all subscribers during reload without race conditions
- `ValidateOnStart()` calls must be idempotent — must not accumulate duplicate validation registrations

## Source Generator Parity (D13)

- Source-generated binding code must produce identical behavior to the runtime reflection-based binder for all supported types
- Generated switch blocks over configuration keys must use case-insensitive comparison
- Generated code must handle nullable types, default values, collections, and nested objects correctly
- Incremental generators must handle cancellation and produce correct output on incremental changes
- Test both source-generated and runtime code paths — parity failures are critical bugs

## Resource Ownership & Change Tokens (D4)

- File-based configuration providers must handle concurrent file replacement — use `FileShare.Delete` when reading
- `IConfigurationRoot.Dispose` must dispose owned providers; document ownership for custom providers
- Reload tokens must not leak event handler registrations — unsubscribe on disposal
- Configuration providers own their data — `IConfigurationRoot` does not own `IFileProvider` instances

## Error Handling (D9)

- Exceptions must be the most specific applicable type with actionable context (key name, expected vs actual type)
- Exceptions from inner operations must be wrapped or propagated — never swallowed silently
- Configuration reload failures must be observable through logging or change notification callbacks

## Null Safety (D3)

- Input from configuration values and deserialized data must be validated for both null and semantic correctness

## Trim & AOT (D14)

- Annotate reflection-using binding APIs with `[DynamicallyAccessedMembers]` to preserve required metadata
- Provide feature switches so the linker can trim optional binding functionality
- No new IL2xxx trim warnings without explicit justification
