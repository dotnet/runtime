# Link Checker Test

This is a test file to validate the markdown link checker configuration works as expected.

## Valid Links (should be checked)
- [Valid Microsoft Docs](https://learn.microsoft.com/dotnet)
- [Valid GitHub Repo](https://github.com/dotnet/runtime)
- [Valid NuGet](https://www.nuget.org/)

## Links That Should Be Ignored
- [Local Development](http://localhost:3000) - should be ignored by config
- [Example Link](https://example.com/test) - should be ignored by config  
- [Private Link](https://github.com/dotnet/runtime/pull/99999) - should be ignored by config
- [Mail Link](mailto:test@example.com) - should be ignored by config

This file will be deleted after testing the configuration.