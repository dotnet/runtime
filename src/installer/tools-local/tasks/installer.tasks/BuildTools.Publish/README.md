The files in this directory are the closure of C# code from the BuildTools repo
that's necessary for the Core-Setup publish tasks. There are no changes except
automatically removing and sorting the using statements.

Source: https://github.com/dotnet/buildtools/tree/55d43483866c7caeeace96355add3a9b12fa5795

Using the existing BuildTools code reduces the risk of behavior differences vs.
trying to find equivalents in Arcade. The upcoming new Arcade-powered publish
functionality makes short-term effort to deduplicate these tasks throwaway work.

See [core-setup/#8285 "Migrate to Arcade's blob publish infrastructure"](https://github.com/dotnet/core-setup/issues/8285)
