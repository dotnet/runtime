# PlatformDocAnalyzer

A Roslyn analyzer that enforces documentation placement conventions for platform-specific
libraries with `UseCompilerGeneratedDocXmlFile=true`.

## Running tests

```
dotnet test eng/analyzers/PlatformDocAnalyzer.Tests/PlatformDocAnalyzer.Tests.csproj
```

These tests are not part of the main CI test pipeline (consistent with other infrastructure
analyzer tests like `IntrinsicsInSystemPrivateCoreLibAnalyzer.Tests`). Run them locally when
modifying the analyzer.
