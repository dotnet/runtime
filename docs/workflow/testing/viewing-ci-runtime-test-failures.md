# Finding the failed Runtime Tests in CI

The process for finding out which tests actually failed when a runtime test leg
fails is not straight forward.

Suppose for example you see on github that this leg failed:

```
runtime (Mono Pri0 Runtime Tests Run Linux arm release)
```

To find the actual test failure click `details`

Then `View more details on Azure Pipelines`.

Near the top, you will see something like

`dnceng / public / Pipelines / runtime / <build id>`

Click on the build id, then click `Tests`.

This will show a list of test failures, broken down by leg and scenario (for example, 'interpreter').

You may also see failures like "JIT.Methodical Work Item"; this is not a test itself, 
but just means at least one test in the JIT.Methodical group failed. The specific test(s)
should be listed separately.

