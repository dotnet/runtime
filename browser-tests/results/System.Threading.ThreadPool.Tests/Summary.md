# System.Threading.ThreadPool.Tests - Test Summary

## Test Results

| Metric | CoreCLR | Mono Baseline |
|--------|---------|---------------|
| Tests run | 47 | 47 |
| Passed | 0 | 0 |
| Failed | 0 | 0 |
| Skipped | 47 | 47 |

## Status: ✅ PASSED

All tests match the Mono baseline (all skipped due to no threading support on Browser).

## Comparison

- Extra in CoreCLR: 1 (SetMinMaxThreadsTest - extra metadata)
- Missing in CoreCLR: 0

## Notes

- ThreadPool tests are skipped on Browser/WASM because threading is not supported
- This is expected behavior matching the Mono baseline
