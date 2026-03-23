# EgorBot Reference

[EgorBot](https://github.com/EgorBo/EgorBot) is a benchmark-as-a-service bot for
[dotnet/runtime](https://github.com/dotnet/runtime). It runs BenchmarkDotNet
microbenchmarks on dedicated hardware and posts results back as GitHub comments.
Its primary use case is comparing performance before and after a change — either
across a PR or between specific commits.

For the full and up-to-date command reference (targets, options, defaults),
see the [EgorBot manual](https://github.com/EgorBo/EgorBot).

## Command Format

Mention `@EgorBot` in a PR or issue comment. The benchmark source goes in a
fenced C# code block (a code fence that begins with three backticks followed
by `cs`) in the same comment.

```
@EgorBot [targets...] [options...] [BDN arguments...]
```

> **Formatting rules:**
> - The `@EgorBot` command must be **outside** the code block.
> - Only benchmark source code belongs inside the code block.
> - Do not place text between the `@EgorBot` line and the code block — EgorBot
>   treats it as additional command arguments.

## Examples

Compare a PR against its base branch on AMD and Apple Silicon:

```
@EgorBot -amd -arm
```

Compare two specific commits:

```
@EgorBot -amd -commits abc1234,def5678
```

Compare a commit against its parent:

```
@EgorBot -arm -commits abc1234,abc1234~1
```

Compare a range of commits for a specific benchmark filter:

```
@EgorBot -arm -commits abc1234...def5678 --filter "*MyBench*"
```

## Practical Notes

- **Default target:** If no target is specified, runs on Apple Silicon via Helix.
- **PR mode:** When posting in a PR without `-commits`, EgorBot automatically
  compares the PR branch against the base branch.
- **No code block:** If no code block is provided, EgorBot runs benchmarks from
  the [dotnet/performance](https://github.com/dotnet/performance) repo instead.
- **Response time:** EgorBot uses polling and may take up to 30 seconds to
  acknowledge the request.
- **Supported repositories:** `dotnet/runtime` and `EgorBot/runtime-utils`.
- **Result variability:** Results can vary between runs due to VM differences.
  Do not compare results across different architectures or cloud providers.

## Links

- [EgorBot manual](https://github.com/EgorBo/EgorBot) — full target list,
  options, and usage documentation
- [BenchmarkDotNet CLI arguments](https://benchmarkdotnet.org/articles/guides/console-args.html)
