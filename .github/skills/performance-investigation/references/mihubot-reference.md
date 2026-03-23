# MihuBot Reference

[MihuBot](https://github.com/MihuBot/runtime-utils) provides several
performance-related services for dotnet/runtime: JIT diff generation, benchmark
execution from the [dotnet/performance](https://github.com/dotnet/performance)
repo, library fuzzing, and regex source generator diffs. It also has a
[web interface](https://mihubot.xyz/runtime-utils) for submitting jobs.

For full and up-to-date option details, see the
[MihuBot runtime-utils](https://github.com/MihuBot/runtime-utils) repository.

## JIT Diff Generation

Generate JIT diffs between a PR and its base branch to see how a change affects
the generated machine code across the BCL.

```
@MihuBot
@MihuBot -arm -tier0
```

## Running Benchmarks from dotnet/performance

Run existing benchmarks from the
[dotnet/performance](https://github.com/dotnet/performance) repository without
writing custom benchmark code.

```
@MihuBot benchmark Regex
@MihuBot benchmark GetUnicodeCategory https://github.com/dotnet/runtime/compare/4bb0bcd...c74440f
```

## Library Fuzzer

Run fuzz testing on a library:

```
@MihuBot fuzz SearchValues
@MihuBot fuzz SearchValues -dependsOn #107206
```

## Regex Source Generator Diffs

Generate diffs for regex source generator output and JIT diffs for the
generated code:

```
@MihuBot regexdiff
@MihuBot regexdiff -arm
```

## Common Options

Most MihuBot job types support options like `-arm`, `-intel`, `-fast`,
`-dependsOn <prs>`, and `-combineWith <prs>`. For example:

```
@MihuBot -arm -hetzner -combineWith #1000,#1001
```

## Links

- [MihuBot runtime-utils](https://github.com/MihuBot/runtime-utils) — full
  documentation and option reference
- [Web interface](https://mihubot.xyz/runtime-utils) for submitting jobs
  directly
