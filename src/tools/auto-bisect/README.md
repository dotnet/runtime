# auto-bisect

A tool for automatically finding the first commit that introduced a test failure in Azure DevOps builds using binary search. Given a known good build and a known bad build, it will automatically queue builds (or use existing ones) to test commits in between, narrowing down to the exact commit that caused the regression.

**Requirements:**

1. A personal AzDO PAT with read & execute permission for the "Build" tasks, and read for "Test Management".
2. A "good" build and a "bad" build, where a test pass in the good build and fail in the bad build.
3. The name of a test to track. `auto-bisect diff` can help you find tests that newly fail in between two builds.

**Usage:**
Inside the src directory, run

```bash
export AZDO_PAT=<token>
dotnet run -- bisect \
  -o <org> -p <project> \
  --good <build-id> --bad <build-id> \
  --test <test-name>
```

The public dotnet testing org is "dnceng-public" and the project is "public".