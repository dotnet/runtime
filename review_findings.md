## Review of PR #125438 (RegexGenerator Incremental Refactor)

### Findings

| Category | Status | Details |
| :--- | :---: | :--- |
| **Re-parse-on-emission** | ✅ | Logic in `Emitter.cs` correctly reconstructs the `RegexTree` using `methodSpec.Tree.CultureName`, matching `Parser.cs` logic. |
| **Incremental Equality** | ✅ | `RegexMethodSpec` and `RegexTreeSpec` are `record` types including `CultureName`, ensuring correct cache invalidation when culture changes. |
| **Culture Handling** | ✅ | Explicit culture + `IgnoreCase` correctly propagates. Inline `(?i)` defaults to Invariant. Culture without `IgnoreCase` is ignored. `CultureInvariant` + explicit culture raises diagnostic. |
| **Deduplication** | ✅ | Dictionary key in `Emitter.cs` includes `CultureName`, preventing collision of identical patterns with different cultures. |
| **Limited Support** | ⚠️ | Fallback code (`EmitRegexLimitedBoilerplate`) ignores the provided `CultureName`. Additionally, `RegexMethodSpec` loses `CultureName` when `Tree` is null, making limited-support specs equal across different cultures. |
| **Test Coverage** | ❌ | No tests found in `RegexGeneratorParserTests.cs` or `RegexGeneratorIncrementalTests.cs` that verify `CultureName` handling or incremental updates when culture changes. |

### Recommendations
1.  **Add Test Coverage**: Add a test case in `RegexGeneratorIncrementalTests.cs` that toggles `CultureName` (e.g., "es-ES" vs "tr-TR") to ensure it triggers a source update.
2.  **Verify Limited Support (Pre-existing)**: Acknowledge that `[GeneratedRegex(..., "es-ES")]` on a limited-support pattern falls back to `CurrentCulture` behavior, which may differ from the requested culture.

### Evidence
*   **Parser.cs**: `effectiveCultureName` logic handles `IgnoreCase` dependency correctly.
*   **Emitter.cs**: `CultureInfo.GetCultureInfo` calls match `Tree.CultureName`.
*   **Limited Support**: `EmitRegexLimitedBoilerplate` only emits `new Regex(pattern, options)`, dropping culture.
