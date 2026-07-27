# Libraries

Covers `src/libraries/`, including Browser/WASM and WASI targets. Confirm the component's
[baseline sentinel](../SKILL.md#baseline-sentinels) before running tests.

## Libraries (most common)

**Build and test a specific library:**

```bash
cd src/libraries/<LibraryName>
dotnet build
dotnet build /t:test ./tests/<TestProject>.csproj
```

Test projects are typically at `tests/<LibraryName>.Tests.csproj` or
`tests/<LibraryName>.Tests/<LibraryName>.Tests.csproj`, or under `tests/FunctionalTests/`,
`tests/UnitTests/`, etc. Use `find tests -name '*.Tests.csproj'` to discover them.

Before completing, ensure ALL tests for the affected libraries pass — not just the one project
you touched.

**Test all libraries:** `./build.sh libs.tests -test -rc release`

**System.Private.CoreLib:** rebuild with `./build.sh clr.corelib+clr.nativecorelib+libs.pretest -rc checked`

## WASM / WASI Libraries

**Build:** `./build.sh libs -os browser`

**Test:** `./build.sh libs.tests -test -os browser`

## Reference

- [Build Libraries](/docs/workflow/building/libraries/README.md) · [Test Libraries](/docs/workflow/testing/libraries/testing.md)
- [WASM Build](/docs/workflow/building/libraries/webassembly-instructions.md) · [WASM Test](/docs/workflow/testing/libraries/testing-wasm.md)
