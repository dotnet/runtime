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
