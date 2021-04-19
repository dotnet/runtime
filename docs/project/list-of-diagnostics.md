List of Obsoletions
==================

Per https://github.com/dotnet/designs/blob/master/accepted/2020/better-obsoletion/better-obsoletion.md, we now have a strategy in place for marking existing APIs as `[Obsolete]`. This takes advantage of the new diagnostic id and URL template mechanisms introduced to `ObsoleteAttribute` in .NET 5.

When obsoleting an API, use the diagnostic ID `SYSLIB####`, where _\#\#\#\#_ is the next four-digit identifier in the sequence, and add it to the list below. This helps us maintain a centralized location of all APIs that were obsoleted using this mechanism.

The URL template we use for obsoletions is `https://aka.ms/dotnet-warnings/{0}`.

Currently the identifiers `SYSLIB0001` through `SYSLIB0999` are carved out for obsoletions. If we wish to introduce analyzer warnings not related to obsoletion in the future, we should begin at a different range, such as `SYSLIB2000`.

## Current obsoletions (`SYSLIB0001` - `SYSLIB0999`)

| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`SYSLIB0001`__ | The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead. |
|  __`SYSLIB0002`__ | PrincipalPermissionAttribute is not honored by the runtime and must not be used. |
|  __`SYSLIB0003`__ | Code Access Security is not supported or honored by the runtime. |
|  __`SYSLIB0004`__ | The Constrained Execution Region (CER) feature is not supported. |
|  __`SYSLIB0005`__ | The Global Assembly Cache is not supported. |
|  __`SYSLIB0006`__ | Thread.Abort is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0007`__ | The default implementation of this cryptography algorithm is not supported. |
|  __`SYSLIB0008`__ | The CreatePdbGenerator API is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0009`__ | The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException. |
|  __`SYSLIB0010`__ | This Remoting API is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0011`__ | `BinaryFormatter` serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for recommended alternatives. |
|  __`SYSLIB0012`__ | Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead. |
|  __`SYSLIB0013`__ | Uri.EscapeUriString can corrupt the Uri string in some cases. Consider using Uri.EscapeDataString for query string components instead. |
|  __`SYSLIB0015`__ | DisablePrivateReflectionAttribute has no effect in .NET 6.0+ applications. |
|  __`SYSLIB0016`__ | Use the Graphics.GetContextInfo overloads that accept arguments for better performance and fewer allocations. |

### Analyzer warnings (`SYSLIB1001` - `SYSLIB1999`)
| Diagnostic ID     | Description |
| :---------------- | :---------- |
|  __`SYSLIB1001`__ | Logging method names cannot start with _ |
|  __`SYSLIB1002`__ | Don't include log level parameters as templates in the logging message |
|  __`SYSLIB1003`__ | InvalidLoggingMethodParameterNameTitle |
|  __`SYSLIB1004`__ | Logging class cannot be in nested types |
|  __`SYSLIB1005`__ | Could not find a required type definition |
|  __`SYSLIB1006`__ | Multiple logging methods cannot use the same event id within a class |
|  __`SYSLIB1007`__ | Logging methods must return void |
|  __`SYSLIB1008`__ | One of the arguments to a logging method must implement the Microsoft.Extensions.Logging.ILogger interface |
|  __`SYSLIB1009`__ | Logging methods must be static |
|  __`SYSLIB1010`__ | Logging methods must be partial |
|  __`SYSLIB1011`__ | Logging methods cannot be generic |
|  __`SYSLIB1012`__ | Redundant qualifier in logging message |
|  __`SYSLIB1013`__ | Don't include exception parameters as templates in the logging message |
|  __`SYSLIB1014`__ | Logging template has no corresponding method argument |
|  __`SYSLIB1015`__ | Argument is not referenced from the logging message |
|  __`SYSLIB1016`__ | Logging methods cannot have a body |
|  __`SYSLIB1017`__ | A LogLevel value must be supplied in the LoggerMessage attribute or as a parameter to the logging method |
|  __`SYSLIB1018`__ | Don't include logger parameters as templates in the logging message |
|  __`SYSLIB1019`__ | Couldn't find a field of type Microsoft.Extensions.Logging.ILogger |
|  __`SYSLIB1020`__ | Found multiple fields of type Microsoft.Extensions.Logging.ILogger |
|  __`SYSLIB1021`__ | Can't have the same template with different casing |
|  __`SYSLIB1022`__ | Can't have malformed format strings (like dangling {, etc)  |
|  __`SYSLIB1023`__ | Generating more than 6 arguments is not supported |
|  __`SYSLIB1029`__ | *_Blocked range `SYSLIB1024`-`SYSLIB1029` for logging._* |
