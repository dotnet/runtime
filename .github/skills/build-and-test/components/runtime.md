# CoreCLR and Mono

Covers `src/coreclr/` and `src/mono/`. Confirm the component's
[baseline sentinel](../SKILL.md#baseline-sentinels) first.

## CoreCLR

**Test:** `cd src/tests && ./build.sh && ./run.sh`

## Mono

**Test:**

```bash
./build.sh clr.host
cd src/tests
./build.sh mono debug /p:LibrariesConfiguration=debug
./run.sh
```

For building or running an individual runtime test, see [`runtime-tests.md`](runtime-tests.md).

## Reference

- [Build CoreCLR](/docs/workflow/building/coreclr/README.md) · [Test CoreCLR](/docs/workflow/testing/coreclr/testing.md)
- [Build Mono](/docs/workflow/building/mono/README.md) · [Test Mono](/docs/workflow/testing/mono/testing.md)
