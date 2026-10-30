---
applyTo: "src/libraries/Microsoft.Extensions.Options*/**"
---

# Microsoft.Extensions.Options — Folder-Specific Guidance

## Options Pattern Usage (D8)

- `IOptions<T>` provides a singleton snapshot — use `IOptionsMonitor<T>` when configuration may change at runtime
- `IOptionsSnapshot<T>` is scoped — do not inject it into singleton services (captive dependency)
- Named options must be resolved correctly — validate that the name parameter flows through the entire options pipeline
- Null option names must resolve to `Options.DefaultName`

## Validation (D8, D13)

- `ValidateOnStart()` calls must not accumulate duplicate validation registrations — each call should be idempotent
- Use `IValidateOptions<T>` for complex cross-property validation that data annotations cannot express
- Validation source generator output must match the behavior of runtime validation for all supported attributes
- `[Range]`, `[Required]`, and custom validation attributes must be tested with boundary values
- Generated validation code must handle concurrent usage without race conditions (avoid shared mutable state in generated validators)

## Options Monitor & Change Tracking

- `IOptionsMonitor<T>.OnChange` notifications must propagate correctly to all subscribers during configuration reload
- Change tracking must not race with concurrent reads — use appropriate synchronization

## Source Generator Parity (D13)

- Options validation source generator must produce identical results to the runtime reflection-based validator
- Generated code must handle all edge cases: nullable types, default values, constructor `params` parameters
- Test both generated and runtime validation paths — parity failures are critical bugs

## Architecture & Layering (D17)

- Options abstractions (`IOptions<T>`, `IOptionsMonitor<T>`) belong in `Microsoft.Extensions.Options`
- Do not introduce unnecessary new packages — prefer extending existing ones unless layering requires separation
- Package references must target correct and aligned versions
- Changes to options defaults or validation behavior are breaking changes requiring migration guidance
