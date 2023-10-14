# Timezone Invariant Mode

Author: [Pavel Savara](https://github.com/pavelsavara)

It's currently only available for Browser OS.
The timezone database is not part of the browser environment (as opposed to other operating systems).
Therefore dotnet bundles the timezone database as binary as part of the runtime.
That makes download size larger and application startup slower.
If your application doesn't need to work with time zone information, you could use this feature to make the runtime about 200KB smaller.

You enable it in project file:
    ```xml
    <PropertyGroup>
        <InvariantTimezone>true</InvariantTimezone>
    </PropertyGroup>
    ```
