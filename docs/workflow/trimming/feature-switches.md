# Libraries Feature Switches

Starting with .NET 5 there are several [feature-switches](https://github.com/dotnet/designs/blob/master/accepted/2020/feature-switch.md) available which
can be used to control the size of the final binary. They are available in all
configurations but their defaults might vary as any SDK can set the defaults differently. Publicly documented feature switches can be found on the [official docs](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options#trimming-framework-library-features). Non-public feature switches that impact the runtime libraries can be found in the following table.

## Available Feature Switches

| MSBuild Property Name | AppContext Setting | Description |
|-|-|-|
| _AggressiveAttributeTrimming | System.AggressiveAttributeTrimming | When set to true, aggressively trims attributes to allow for the most size savings possible, even if it could result in runtime behavior changes |
| _ComObjectDescriptorSupport | System.ComponentModel.TypeDescriptor.IsComObjectDescriptorSupported | When set to true, supports creating a TypeDescriptor based view of COM objects. |
| _DefaultValueAttributeSupport | System.ComponentModel.DefaultValueAttribute.IsSupported | When set to true, supports creating a DefaultValueAttribute at runtime. |
| _DesignerHostSupport | System.ComponentModel.Design.IDesignerHost.IsSupported | When set to true, supports creating design components at runtime. |
| _EnableConsumingManagedCodeFromNativeHosting | System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting | Getting a managed function from native hosting is disabled when set to false and related functionality can be trimmed. |
| _UseManagedNtlm | System.Net.Security.UseManagedNtlm | When set to true, uses built-in managed implementation of NTLM and SPNEGO algorithm for HTTP, SMTP authentication, and NegotiateAuthentication API instead of system provided GSSAPI implementation. |

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
