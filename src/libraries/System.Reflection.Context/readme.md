# System.Reflection.Context
This is used by `System.ComponentModel` to support [`CustomReflectionContext`](https://learn.microsoft.com/dotnet/api/system.reflection.context.customreflectioncontext).

## Status: [Legacy](..\system.reflection\overview.md#status)
Although it is used for key scenarios by the community, it has remained relatively unchanged. Internally it uses [Emit](..\System.Reflection.Emit\readme.md) to generate the proxies so it doesn't work on all platforms.

## Deployment
https://www.nuget.org/packages/System.Reflection.Context
