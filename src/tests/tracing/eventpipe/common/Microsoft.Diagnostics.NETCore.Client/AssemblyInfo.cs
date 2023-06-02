#if DIAGNOSTICS_RUNTIME
// As of https://github.com/dotnet/runtime/pull/64358, InternalsVisibleTo MSBuild Item does not seem to work in Dotnet/Runtime like it does on Dotnet/Diagnostics
using System;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("common")]
[assembly: InternalsVisibleTo("enabledisable")]
#endif