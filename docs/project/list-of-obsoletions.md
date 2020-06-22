List of Obsoletions
==================

Per https://github.com/dotnet/designs/blob/master/accepted/2020/better-obsoletion/better-obsoletion.md, we now have a strategy in place for marking existing APIs as `[Obsolete]`. This takes advantage of the new diagnostic id and URL template mechanisms introduced to `ObsoleteAttribute` in .NET 5.

When obsoleting an API, use the diagnostic ID `MSLIB####`, where _\#\#\#\#_ is the next four-digit identifier in the sequence, and add it to the list below. This helps us maintain a centralized location of all APIs that were obsoleted using this mechanism.

The URL template we use for obsoletions is `https://aka.ms/dotnet-warnings/{0}`.

Currently the identifiers `MSLIB0001` through `MSLIB0999` are carved out for obsoletions. If we wish to introduce analyzer warnings not related to obsoletion in the future, we should begin at a different range, such as `MSLIB2000`.

## Current obsoletions (`MSLIB0001` - `MSLIB0999`)

| Diagnostic ID    | Description |
| :--------------- | :---------- |
|  __`MSLIB0001`__ | (Reserved for `Encoding.UTF7`.) |
|  __`MSLIB0002`__ | `PrincipalPermissionAttribute` is not honored by the runtime and must not be used. |
