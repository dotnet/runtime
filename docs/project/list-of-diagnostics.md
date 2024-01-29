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
|  __`SYSLIB0009`__ | AuthenticationManager is not supported. Methods will no-op or throw PlatformNotSupportedException. |
|  __`SYSLIB0010`__ | This Remoting API is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0011`__ | `BinaryFormatter` serialization is obsolete and should not be used. See https://aka.ms/binaryformatter for recommended alternatives. |
|  __`SYSLIB0012`__ | Assembly.CodeBase and Assembly.EscapedCodeBase are only included for .NET Framework compatibility. Use Assembly.Location instead. |
|  __`SYSLIB0013`__ | Uri.EscapeUriString can corrupt the Uri string in some cases. Consider using Uri.EscapeDataString for query string components instead. |
|  __`SYSLIB0014`__ | WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead. |
|  __`SYSLIB0015`__ | DisablePrivateReflectionAttribute has no effect in .NET 6.0+. |
|  __`SYSLIB0016`__ | Use the Graphics.GetContextInfo overloads that accept arguments for better performance and fewer allocations. |
|  __`SYSLIB0017`__ | Strong name signing is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0018`__ | ReflectionOnly loading is not supported and throws PlatformNotSupportedException. |
|  __`SYSLIB0019`__ | RuntimeEnvironment members SystemConfigurationFile, GetRuntimeInterfaceAsIntPtr, and GetRuntimeInterfaceAsObject are not supported and throw PlatformNotSupportedException. |
|  __`SYSLIB0020`__ | JsonSerializerOptions.IgnoreNullValues is obsolete. To ignore null values when serializing, set DefaultIgnoreCondition to JsonIgnoreCondition.WhenWritingNull. |
|  __`SYSLIB0021`__ | Derived cryptographic types are obsolete. Use the Create method on the base type instead. |
|  __`SYSLIB0022`__ | The Rijndael and RijndaelManaged types are obsolete. Use Aes instead. |
|  __`SYSLIB0023`__ | RNGCryptoServiceProvider is obsolete. To generate a random number, use one of the RandomNumberGenerator static methods instead. |
|  __`SYSLIB0024`__ | Creating and unloading AppDomains is not supported and throws an exception. |
|  __`SYSLIB0025`__ | SuppressIldasmAttribute has no effect in .NET 6.0+. |
|  __`SYSLIB0026`__ | X509Certificate and X509Certificate2 are immutable. Use the appropriate constructor to create a new certificate. |
|  __`SYSLIB0027`__ | PublicKey.Key is obsolete. Use the appropriate method to get the public key, such as GetRSAPublicKey. |
|  __`SYSLIB0028`__ | X509Certificate2.PrivateKey is obsolete. Use the appropriate method to get the private key, such as GetRSAPrivateKey, or use the CopyWithPrivateKey method to create a new instance with a private key. |
|  __`SYSLIB0029`__ | ProduceLegacyHmacValues is obsolete. Producing legacy HMAC values is not supported. |
|  __`SYSLIB0030`__ | HMACSHA1 always uses the algorithm implementation provided by the platform. Use a constructor without the useManagedSha1 parameter. |
|  __`SYSLIB0031`__ | EncodeOID is obsolete. Use the ASN.1 functionality provided in System.Formats.Asn1. |
|  __`SYSLIB0032`__ | Recovery from corrupted process state exceptions is not supported; HandleProcessCorruptedStateExceptionsAttribute is ignored. |
|  __`SYSLIB0033`__ | Rfc2898DeriveBytes.CryptDeriveKey is obsolete and is not supported. Use PasswordDeriveBytes.CryptDeriveKey instead. |
|  __`SYSLIB0034`__ | CmsSigner(CspParameters) is obsolete and is not supported. Use an alternative constructor instead. |
|  __`SYSLIB0035`__ | ComputeCounterSignature without specifying a CmsSigner is obsolete and is not supported. Use the overload that accepts a CmsSigner. |
|  __`SYSLIB0036`__ | Regex.CompileToAssembly is obsolete and not supported. Use GeneratedRegexAttribute with the regular expression source generator instead. |
|  __`SYSLIB0037`__ | AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported. |
|  __`SYSLIB0038`__ | SerializationFormat.Binary is obsolete and should not be used. See https://aka.ms/serializationformat-binary-obsolete for more information. |
|  __`SYSLIB0039`__ | TLS versions 1.0 and 1.1 have known vulnerabilities and are not recommended. Use a newer TLS version instead, or use SslProtocols.None to defer to OS defaults. |
|  __`SYSLIB0040`__ | EncryptionPolicy.NoEncryption and AllowEncryption significantly reduce security and should not be used in production code. |
|  __`SYSLIB0041`__ | The default hash algorithm and iteration counts in Rfc2898DeriveBytes constructors are outdated and insecure. Use a constructor that accepts the hash algorithm and the number of iterations. |
|  __`SYSLIB0042`__ | ToXmlString and FromXmlString have no implementation for ECC types, and are obsolete. Use a standard import and export format such as ExportSubjectPublicKeyInfo or ImportSubjectPublicKeyInfo for public keys and ExportPkcs8PrivateKey or ImportPkcs8PrivateKey for private keys. |
|  __`SYSLIB0043`__ | ECDiffieHellmanPublicKey.ToByteArray() and the associated constructor do not have a consistent and interoperable implementation on all platforms. Use ECDiffieHellmanPublicKey.ExportSubjectPublicKeyInfo() instead. |
|  __`SYSLIB0044`__ | AssemblyName.CodeBase and AssemblyName.EscapedCodeBase are obsolete. Using them for loading an assembly is not supported. |
|  __`SYSLIB0045`__ | Cryptographic factory methods accepting an algorithm name are obsolete. Use the parameterless Create factory method on the algorithm type instead. |
|  __`SYSLIB0046`__ | ControlledExecution.Run method may corrupt the process and should not be used in production code. |
|  __`SYSLIB0047`__ | XmlSecureResolver is obsolete. Use XmlResolver.ThrowingResolver instead when attempting to forbid XML external entity resolution. |
|  __`SYSLIB0048`__ | RSA.EncryptValue and DecryptValue are not supported and throw NotSupportedException. Use RSA.Encrypt and RSA.Decrypt instead. |
|  __`SYSLIB0049`__ | JsonSerializerOptions.AddContext is obsolete. To register a JsonSerializerContext, use either the TypeInfoResolver or TypeInfoResolverChain properties. |
|  __`SYSLIB0050`__ | Formatter-based serialization is obsolete and should not be used. |
|  __`SYSLIB0051`__ | This API supports obsolete formatter-based serialization. It should not be called or extended by application code. |
|  __`SYSLIB0052`__ | This API supports obsolete mechanisms for Regex extensibility. It is not supported. |
|  __`SYSLIB0053`__ | AesGcm should indicate the required tag size for encryption and decryption. Use a constructor that accepts the tag size. |

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
|  __`SYSLIB1022`__ | Logging method contains malformed format strings |
|  __`SYSLIB1023`__ | Generating more than 6 arguments is not supported |
|  __`SYSLIB1024`__ | Argument is using the unsupported out parameter modifier |
|  __`SYSLIB1025`__ | Multiple logging methods cannot use the same event name within a class |
|  __`SYSLIB1026`__ | C# language version not supported by the logging source generator. |
|  __`SYSLIB1027`__ | _`SYSLIB1001`-`SYSLIB1029` reserved for logging._ |
|  __`SYSLIB1028`__ | _`SYSLIB1001`-`SYSLIB1029` reserved for logging._ |
|  __`SYSLIB1029`__ | _`SYSLIB1001`-`SYSLIB1029` reserved for logging._ |
|  __`SYSLIB1030`__ | JsonSourceGenerator did not generate serialization metadata for type |
|  __`SYSLIB1031`__ | JsonSourceGenerator encountered a duplicate JsonTypeInfo property name |
|  __`SYSLIB1032`__ | JsonSourceGenerator encountered a context class that is not partial |
|  __`SYSLIB1033`__ | JsonSourceGenerator encountered a type that has multiple [JsonConstructor] annotations|
|  __`SYSLIB1034`__ | JsonSourceGenerator encountered a [JsonStringEnumConverter] annotation |
|  __`SYSLIB1035`__ | JsonSourceGenerator encountered a type that has multiple [JsonExtensionData] annotations |
|  __`SYSLIB1036`__ | JsonSourceGenerator encountered an invalid [JsonExtensionData] annotation |
|  __`SYSLIB1037`__ | JsonSourceGenerator encountered a type with init-only properties for which deserialization is not supported |
|  __`SYSLIB1038`__ | JsonSourceGenerator encountered a property annotated with [JsonInclude] that has inaccessible accessors |
|  __`SYSLIB1039`__ | JsonSourceGenerator encountered a [JsonDerivedTypeAttribute] annotation with [JsonSourceGenerationMode.Serialization] enabled |
|  __`SYSLIB1040`__ | Invalid GeneratedRegex attribute |
|  __`SYSLIB1041`__ | Multiple GeneratedRegex attribute |
|  __`SYSLIB1042`__ | Invalid GeneratedRegex arguments |
|  __`SYSLIB1043`__ | GeneratedRegex method must have a valid signature |
|  __`SYSLIB1044`__ | GeneratedRegex only supports C# 11 and newer |
|  __`SYSLIB1045`__ | Use 'GeneratedRegexAttribute' to generate the regular expression implementation at compile-time |
|  __`SYSLIB1046`__ | _`SYSLIB1045`-`SYSLIB1049` reserved for System.Text.RegularExpressions.Generator._ |
|  __`SYSLIB1047`__ | _`SYSLIB1045`-`SYSLIB1049` reserved for System.Text.RegularExpressions.Generator._ |
|  __`SYSLIB1048`__ | _`SYSLIB1045`-`SYSLIB1049` reserved for System.Text.RegularExpressions.Generator._ |
|  __`SYSLIB1049`__ | _`SYSLIB1045`-`SYSLIB1049` reserved for System.Text.RegularExpressions.Generator._ |
|  __`SYSLIB1050`__ | Invalid LibraryImportAttribute usage |
|  __`SYSLIB1051`__ | Specified type is not supported by source-generated P/Invokes |
|  __`SYSLIB1052`__ | Specified configuration is not supported by source-generated P/Invokes |
|  __`SYSLIB1053`__ | Specified LibraryImportAttribute arguments cannot be forwarded to DllImportAttribute |
|  __`SYSLIB1054`__ | Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time |
|  __`SYSLIB1055`__ | Invalid CustomMarshallerAttribute usage |
|  __`SYSLIB1056`__ | Specified native type is invalid |
|  __`SYSLIB1057`__ | Marshaller type does not have the required shape |
|  __`SYSLIB1058`__ | Invalid NativeMarshallingAttribute usage |
|  __`SYSLIB1059`__ | Marshaller type does not support allocating constructor |
|  __`SYSLIB1060`__ | Specified marshaller type is invalid |
|  __`SYSLIB1061`__ | Marshaller type has incompatible method signatures |
|  __`SYSLIB1062`__ | Project must be updated with '<AllowUnsafeBlocks>true</AllowUnsafeBlocks>' |
|  __`SYSLIB1063`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1064`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1065`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1066`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1067`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1068`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1069`__ | _`SYSLIB1063`-`SYSLIB1069` reserved for Microsoft.Interop.LibraryImportGenerator._ |
|  __`SYSLIB1070`__ | Invalid 'JSImportAttribute' usage |
|  __`SYSLIB1071`__ | Invalid 'JSExportAttribute' usage |
|  __`SYSLIB1072`__ | Specified type is not supported by source-generated JavaScript interop |
|  __`SYSLIB1073`__ | Specified configuration is not supported by source-generated JavaScript interop |
|  __`SYSLIB1074`__ | JSImportAttribute requires unsafe code |
|  __`SYSLIB1075`__ | JSExportAttribute requires unsafe code |
|  __`SYSLIB1076`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1077`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1078`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1079`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1080`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1081`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1082`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1083`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1084`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1085`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1086`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1087`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1088`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1089`__ | _`SYSLIB1070`-`SYSLIB1089` reserved for System.Runtime.InteropServices.JavaScript.JSImportGenerator._ |
|  __`SYSLIB1090`__ | Invalid 'GeneratedComInterfaceAttribute' usage |
|  __`SYSLIB1091`__ | Method is declared in different partial declaration than the 'GeneratedComInterface' attribute. |
|  __`SYSLIB1092`__ | Usage of '[LibraryImport|GeneratedComInterface]' does not follow recommendation. See aka.ms/[LibraryImport|GeneratedComInterface]Usage for best practices. |
|  __`SYSLIB1093`__ | Analysis for COM interface generation has failed |
|  __`SYSLIB1094`__ | The base COM interface failed to generate source. Code will not be generated for this interface. |
|  __`SYSLIB1095`__ | Invalid 'GeneratedComClassAttribute' usage |
|  __`SYSLIB1096`__ | Use 'GeneratedComInterfaceAttribute' instead of 'ComImportAttribute' to generate COM marshalling code at compile time |
|  __`SYSLIB1097`__ | This type implements at least one type with the 'GeneratedComInterfaceAttribute' attribute. Add the 'GeneratedComClassAttribute' to enable passing this type to COM and exposing the COM interfaces for the types with the 'GeneratedComInterfaceAttribute' from objects of this type. |
|  __`SYSLIB1098`__ | .NET COM hosting with 'EnableComHosting' only supports built-in COM interop. It does not support source-generated COM interop with 'GeneratedComInterfaceAttribute'. |
|  __`SYSLIB1099`__ | COM Interop APIs on 'System.Runtime.InteropServices.Marshal' do not support source-generated COM and will fail at runtime |
|  __`SYSLIB1100`__ | Configuration binding generator: type is not supported. |
|  __`SYSLIB1101`__ | Configuration binding generator: property on type is not supported. |
|  __`SYSLIB1102`__ | Configuration binding generator: project's language version must be at least C# 12.|
|  __`SYSLIB1103`__ | Configuration binding generator: value types are invalid inputs to configuration 'Bind' methods.* |
|  __`SYSLIB1104`__ | Configuration binding generator: Generator cannot determine the target configuration type.* |
|  __`SYSLIB1105`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1106`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1107`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1108`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1109`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1110`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1111`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1112`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1113`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1114`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1115`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1116`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1117`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1118`__ | *_`SYSLIB1100`-`SYSLIB1118` reserved for Microsoft.Extensions.Configuration.Binder.SourceGeneration.* |
|  __`SYSLIB1201`__ | Options validation generator: Can't use 'ValidateObjectMembersAttribute' or `ValidateEnumeratedItemsAttribute` on fields or properties with open generic types. |
|  __`SYSLIB1202`__ | Options validation generator: A member type has no fields or properties to validate. |
|  __`SYSLIB1203`__ | Options validation generator: A type has no fields or properties to validate. |
|  __`SYSLIB1204`__ | Options validation generator: A type annotated with `OptionsValidatorAttribute` doesn't implement the necessary interface. |
|  __`SYSLIB1205`__ | Options validation generator: A type already includes an implementation of the 'Validate' method. |
|  __`SYSLIB1206`__ | Options validation generator: Can't validate private fields or properties. |
|  __`SYSLIB1207`__ | Options validation generator: Member type is not enumerable. |
|  __`SYSLIB1208`__ | Options validation generator: Validators used for transitive or enumerable validation must have a constructor with no parameters. |
|  __`SYSLIB1209`__ | Options validation generator: `OptionsValidatorAttribute` can't be applied to a static class. |
|  __`SYSLIB1210`__ | Options validation generator: Null validator type specified for the `ValidateObjectMembersAttribute` or 'ValidateEnumeratedItemsAttribute' attributes. |
|  __`SYSLIB1211`__ | Options validation generator: Unsupported circular references in model types. |
|  __`SYSLIB1212`__ | Options validation generator: Member potentially missing transitive validation. |
|  __`SYSLIB1213`__ | Options validation generator: Member potentially missing enumerable validation. |
|  __`SYSLIB1214`__ | Options validation generator: Can't validate constants, static fields or properties. |
|  __`SYSLIB1215`__ | Options validation generator: Validation attribute on the member is inaccessible from the validator type. |
|  __`SYSLIB1216`__ | C# language version not supported by the options validation source generator. |
|  __`SYSLIB1217`__ | The validation attribute is only applicable to properties of type string, array, or ICollection; it cannot be used with other types. |
|  __`SYSLIB1218`__ | *_`SYSLIB1201`-`SYSLIB1219` reserved for Microsoft.Extensions.Options.SourceGeneration.* |
|  __`SYSLIB1219`__ | *_`SYSLIB1201`-`SYSLIB1219` reserved for Microsoft.Extensions.Options.SourceGeneration.* |
|  __`SYSLIB1220`__ | JsonSourceGenerator encountered a [JsonConverterAttribute] with an invalid type argument. |
|  __`SYSLIB1221`__ | JsonSourceGenerator does not support this C# language version. |
|  __`SYSLIB1222`__ | Constructor annotated with JsonConstructorAttribute is inaccessible. |
|  __`SYSLIB1223`__ | Attributes deriving from JsonConverterAttribute are not supported by the source generator. |
|  __`SYSLIB1224`__ | Types annotated with JsonSerializableAttribute must be classes deriving from JsonSerializerContext. |
|  __`SYSLIB1225`__ | *`SYSLIB1220`-`SYSLIB229` reserved for System.Text.Json.SourceGeneration.* |
|  __`SYSLIB1226`__ | *`SYSLIB1220`-`SYSLIB229` reserved for System.Text.Json.SourceGeneration.* |
|  __`SYSLIB1227`__ | *`SYSLIB1220`-`SYSLIB229` reserved for System.Text.Json.SourceGeneration.* |
|  __`SYSLIB1228`__ | *`SYSLIB1220`-`SYSLIB229` reserved for System.Text.Json.SourceGeneration.* |
|  __`SYSLIB1229`__ | *`SYSLIB1220`-`SYSLIB229` reserved for System.Text.Json.SourceGeneration.* |

### Diagnostic Suppressions (`SYSLIBSUPPRESS****`)

| Suppression ID           | Suppressed Diagnostic ID | Description |
| :----------------------- | :----------------------- | :---------- |
| __`SYSLIBSUPPRESS0001`__ | CA1822                   | Do not offer to make methods static when the methods need to be instance methods for a custom marshaller shape. |
| __`SYSLIBSUPPRESS0002`__ | IL2026                   | ConfigurationBindingGenerator: suppress RequiresUnreferencedCode diagnostic for binding call that has been intercepted by a generated static variant. |
| __`SYSLIBSUPPRESS0003`__ | IL3050                   | ConfigurationBindingGenerator: suppress RequiresDynamicCode diagnostic for binding call that has been intercepted by a generated static variant. |
