# Libraries Feature Switches

Starting with .NET 5 there are several [feature-switches](https://github.com/dotnet/designs/blob/master/accepted/2020/feature-switch.md) available which
can be used to control the size of the final binary. They are available in all
configurations but their defaults might vary as any SDK can set the defaults differently.

## Available Feature Switches

| MSBuild Property Name | AppContext Setting | Description |
|-|-|-|
| DebuggerSupport | System.Diagnostics.Debugger.IsSupported | Any dependency that enables better debugging experience to be trimmed when set to false |
| EnableUnsafeUTF7Encoding | System.Text.Encoding.EnableUnsafeUTF7Encoding | Insecure UTF-7 encoding is trimmed when set to false |
| EnableUnsafeBinaryFormatterSerialization | System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization | BinaryFormatter serialization support is trimmed when set to false |
| EventSourceSupport | System.Diagnostics.Tracing.EventSource.IsSupported | Any EventSource related code or logic is trimmed when set to false |
| InvariantGlobalization | System.Globalization.Invariant | All globalization specific code and data is trimmed when set to true |
| UseSystemResourceKeys | System.Resources.UseSystemResourceKeys |  Any localizable resources for system assemblies is trimmed when set to true |
| HttpActivityPropagationSupport | System.Net.Http.EnableActivityPropagation | Any dependency related to diagnostics support for System.Net.Http is trimmed when set to false |
| UseNativeHttpHandler | System.Net.Http.UseNativeHttpHandler | HttpClient uses by default platform native implementation of HttpMessageHandler if set to true. |
| StartupHookSupport | System.StartupHookProvider.IsSupported | Startup hooks are disabled when set to false. Startup hook related functionality can be trimmed. |
| AutoreleasePoolSupport | System.Threading.Thread.EnableAutoreleasePool | When set to true, creates an NSAutoreleasePool for each thread and thread pool work item on applicable platforms. |
| CustomResourceTypesSupport | System.Resources.ResourceManager.AllowCustomResourceTypes | Use of custom resource types is disabled when set to false. ResourceManager code paths that use reflection for custom types can be trimmed. |
| EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization | System.ComponentModel.TypeConverter.EnableUnsafeBinaryFormatterInDesigntimeLicenseContextSerialization | BinaryFormatter serialization support is trimmed when set to false. |
| BuiltInComInteropSupport | System.Runtime.InteropServices.BuiltInComInterop.IsSupported | Built-in COM support is trimmed when set to false. |
| EnableCppCLIHostActivation | System.Runtime.InteropServices.EnableCppCLIHostActivation | C++/CLI host activation code is disabled when set to false and related functionality can be trimmed. |
| MetadataUpdaterSupport | System.Reflection.Metadata.MetadataUpdater.IsSupported | Metadata update related code to be trimmed when set to false |
| _EnableConsumingManagedCodeFromNativeHosting | System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting | Getting a managed function from native hosting is disabled when set to false and related functionality can be trimmed. |

Any feature-switch which defines property can be set in csproj file or
on the command line as any other MSBuild property. Those without predefined property name
the value can be set with following XML tag in csproj file.

```xml
<RuntimeHostConfigurationOption Include="<AppContext-Setting>"
                                Value="false"
                                Trim="true" />
```

## Adding New Feature Switch

The primary goal of features switches is to produce smaller output by removing code which is
unreachable under feature condition. The typical approach is to introduce static bool like
property which is used to guard the dependencies which can be trimmed when the value is flipped.
Ideally, the static property should be located in type which does not have any static constructor
logic. Once you are done with the code changes following steps connects the code with trimming
settings.

Add XML settings for the features switch to assembly substitution. It's usually located in
`src/ILLink/ILLink.Substitutions.xml` file for each library. The example of the syntax used to control
`EnableUnsafeUTF7Encoding` property is following.

```xml
<method signature="System.Boolean get_EnableUnsafeUTF7Encoding()" body="stub" value="false" feature="System.Text.Encoding.EnableUnsafeUTF7Encoding" featurevalue="false" />
```

Add MSBuild integration by adding new RuntimeHostConfigurationOption entry. The file is located in
[Microsoft.NET.Sdk.targets](https://github.com/dotnet/sdk/blob/33ce6234e6bf45bce16f610c441679252d309189/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.Sdk.targets#L348-L401) file and includes all
other public feature-switches. You can add a new one by simply adding a new XML tag

```xml
<RuntimeHostConfigurationOption Include="<AppContext-Setting>"
            Condition="'$(<msbuild-property-name>)' != ''"
            Value="$(<msbuild-property-name>)"
            Trim="true" />
```

Please don't forget to update the table with available features-switches when you are done.
