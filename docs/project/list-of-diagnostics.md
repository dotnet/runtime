# List of Diagnostics Produced by .NET Libraries APIs

## Obsoletions

Per https://github.com/dotnet/designs/blob/master/accepted/2020/better-obsoletion/better-obsoletion.md, we now have a strategy for marking existing APIs as `[Obsolete]`. This takes advantage of the new diagnostic id and URL template mechanisms introduced to `ObsoleteAttribute` in .NET 5.

The diagnostic id values reserved for obsoletions are `SYSLIB0001` through `SYSLIB0999`. When obsoleting an API, claim the next three-digit identifier in the `SYSLIB0###` sequence and add it to the list below. The URL template for all obsoletions is `https://aka.ms/dotnet-warnings/{0}`. The `{0}` placeholder is replaced by the compiler with the `SYSLIB0###` identifier.

The acceptance criteria for adding an obsoletion includes:

* Add the obsoletion to the table below, claiming the next diagnostic id
    * Ensure the description is meaningful within the context of this table, and without requiring the context of the calling code
* Add new constants to `src\libraries\Common\src\System\Obsoletions.cs`, following the existing conventions
    * A `...Message` const using the same description added to the table below
    * A `...DiagId` const for the `SYSLIB0###` id
* Annotate `src` files by referring to the constants defined from `Obsoletions.cs`
    * Specify the `UrlFormat = Obsoletions.SharedUrlFormat`
    * Example: `[Obsolete(Obsoletions.CodeAccessSecurityMessage, DiagnosticId = Obsoletions.CodeAccessSecurityDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]`
    * If the `Obsoletions` type is not available in the project, link it into the project
        * `<Compile Include="$(CommonPath)System\Obsoletions.cs" Link="Common\System\Obsoletions.cs" />`
* Annotate `ref` files using the hard-coded strings copied from `Obsoletions.cs`
    * This matches our general pattern of `ref` files using hard-coded attribute strings
    * Example: `[System.ObsoleteAttribute("The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.", DiagnosticId = "SYSLIB0001", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]`
* If the library builds against downlevel targets earlier than .NET 5.0, then add an internal copy of `ObsoleteAttribute`
    * The compiler recognizes internal implementations of `ObsoleteAttribute` to enable the `DiagnosticId` and `UrlFormat` properties to light up downlevel
    * An MSBuild property can be added to the project's first `<PropertyGroup>` to achieve this easily
    * Example: `<IncludeInternalObsoleteAttribute>true</IncludeInternalObsoleteAttribute>`
    * This will need to be specified in both the `src` and `ref` projects
* If the library contains types that are forwarded within a generated shim
    * Errors will be received when running `build libs`, with obsoletion errors in `src/libraries/shims/generated` files
    * This is resolved by adding the obsoletion's diagnostic id to the `<NoWarn>` property for partial facade assemblies
    * That property is found in `src/libraries/Directory.Build.targets`
    * Search for the "Ignore Obsolete errors within the generated shims that type-forward types" comment and add the appropriate diagnostic id to the comment and the `<NoWarn>` property (other SYSLIB diagnostics already exist there)
* Apply the `breaking-change` label to the PR that introduces the obsoletion
    * A bot will automatically apply the `needs-breaking-change-doc-created` label when the `breaking-change` label is detected
* Follow up with the breaking change process to communicate and document the breaking change
    * In the breaking-change issue filed in [dotnet/docs](https://github.com/dotnet/docs), specifically mention that this breaking change is an obsoletion with a `SYSLIB` diagnostic id
    * The documentation team will produce a PR that adds the obsoletion to the [SYSLIB warnings](https://docs.microsoft.com/en-us/dotnet/core/compatibility/syslib-obsoletions) page
    * That PR will also add a new URL specific to this diagnostic ID; e.g. [SYSLIB0001](https://docs.microsoft.com/en-us/dotnet/core/compatibility/syslib-warnings/syslib0001)
    * Connect with `@gewarren` or `@BillWagner` with any questions
* Register the `SYSLIB0###` URL in `aka.ms`
    * The vanity name will be `dotnet-warnings/syslib0###`
    * Ensure the link's group owner matches the group owner of `dotnet-warnings/syslib0001`
    * Connect with `@jeffhandley`, `@GrabYourPitchforks`, or `@gewarren` with any questions

An example obsoletion PR that can be referenced where each of the above criteria was met is:

* [Implement new GetContextAPI overloads (#49186)](https://github.com/dotnet/runtime/pull/49186/files)

The PR that reveals the implementation of the `<IncludeInternalObsoleteAttribute>` property was:

* [Mark DirectoryServices CAS APIs as Obsolete (#40756)](https://github.com/dotnet/runtime/pull/40756/files)

### Obsoletion Diagnostics (`SYSLIB0001` - `SYSLIB0999`)

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
|  __`SYSLIB0014`__ | WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead. |
|  __`SYSLIB0015`__ | DisablePrivateReflectionAttribute has no effect in .NET 6.0+. |
|  __`SYSLIB0016`__ | Use the Graphics.GetContextInfo overloads that accept arguments for better performance and fewer allocations. |
|  __`SYSLIB0017`__ | Strong name signing is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0018`__ | ReflectionOnly loading is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0019`__ | RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are no longer supported and throw PlatformNotSupportedException. |
|  __`SYSLIB0020`__ | JsonSerializerOptions.IgnoreNullValues is obsolete. To ignore null values when serializing, set DefaultIgnoreCondition to JsonIgnoreCondition.WhenWritingNull. |
|  __`SYSLIB0021`__ | Derived cryptographic types are obsolete. Use the Create method on the base type instead. |
|  __`SYSLIB0022`__ | The Rijndael and RijndaelManaged types are obsolete. Use Aes instead. |
|  __`SYSLIB0023`__ | RNGCryptoServiceProvider is obsolete. To generate a random number, use one of the RandomNumberGenerator static methods instead. |
|  __`SYSLIB0024`__ | Creating and unloading AppDomains is not supported and throws an exception. |
|  __`SYSLIB0025`__ | SuppressIldasmAttribute has no effect in .NET 6.0+. |

## Analyzer Warnings

The diagnostic id values reserved for .NET Libraries analyzer warnings are `SYSLIB1001` through `SYSLIB1999`. When creating a new analyzer that ships as part of the Libraries (and not part of the SDK), claim the next three-digit identifier in the `SYSLIB1###` sequence and add it to the list below.

### Analyzer Diagnostics (`SYSLIB1001` - `SYSLIB1999`)

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
|  __`SYSLIB1024`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1025`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1026`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1027`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1028`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1029`__ | *_`SYSLIB1024`-`SYSLIB1029` reserved for logging._* |
|  __`SYSLIB1030`__ | [System.Text.Json.SourceGeneration] Did not generate serialization metadata for type. |
|  __`SYSLIB1031`__ | [System.Text.Json.SourceGeneration] Duplicate type name. |
|  __`SYSLIB1032`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1033`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1034`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1035`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1036`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1037`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1038`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
|  __`SYSLIB1039`__ | *_`SYSLIB1032`-`SYSLIB1039` reserved for System.Text.Json.SourceGeneration._* |
