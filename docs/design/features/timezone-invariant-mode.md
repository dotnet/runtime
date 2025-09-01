# Timezone Invariant Mode

Author: [Pavel Savara](https://github.com/pavelsavara)

It's currently only available for Browser OS.
The timezone database is not part of the browser environment (as opposed to other operating systems).
Therefore dotnet bundles the timezone database as binary as part of the runtime.
That makes download size larger and application startup slower.
If your application doesn't need to work with time zone information, you could use this feature to make the runtime about 200KB smaller.

## Enabling the invariant mode

Applications can enable the invariant mode by either of the following:

1. in project file:

    ```xml
    <PropertyGroup>
        <InvariantTimezone>true</InvariantTimezone>
    </PropertyGroup>
    ```

2. in `runtimeconfig.json` file:

    ```json
    {
        "runtimeOptions": {
            "configProperties": {
                "System.TimeZoneInfo.Invariant": true
            }
        }
    }
    ```

3. setting environment variable value `DOTNET_SYSTEM_TIMEZONE_INVARIANT` to `true` or `1`.
