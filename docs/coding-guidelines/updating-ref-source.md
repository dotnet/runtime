This document provides the steps you need to take to update the reference assembly when adding new **public** APIs to an implementation assembly (post [API Review](adding-api-guidelines.md)).

## For most assemblies within libraries

1. Implement the API in the source assembly and [build it](../workflow/building/libraries/README.md#building-individual-libraries). Note that when adding new public types, this might fail with a `TypeMustExist` error. The deadlock can be worked around by disabling the `RunApiCompat` property: `dotnet build /p:RunApiCompat=false`.
2. Run the following command (from the src directory) `dotnet msbuild /t:GenerateReferenceAssemblySource` to update the reference assembly**.
3. Navigate to the ref directory and build the reference assembly.
4. Add, build, and run tests.

** **Note:** If you already added the new API to the reference source, re-generating it (after building the source assembly) will update it to be fully qualified and placed in the correct order. This can be done by running the `GenerateReferenceAssemblySource` command from the ref directory.

## For System.Runtime

These steps can also be applied to some unique assemblies which depend on changes in System.Private.Corelib. (partial facades like System.Memory, for example).

1) Run `dotnet build --no-incremental /t:GenerateReferenceAssemblySource` from the System.Runtime/src directory.
2) Filter out all unrelated changes and extract the changes you care about (ignore certain attributes being removed). Generally, this step is not required for other reference assemblies.

## For Full Facade Assemblies implementation assemblies

For implementation assemblies that are "full facades" over another assembly but define types in the reference assembly (ex. System.Runtime.Serialization.Json or System.Xml.XDocument), use the following command to generate the reference source code instead:

```
dotnet msbuild /t:GenerateReferenceAssemblySource /p:GenAPIFollowTypeForwards=true
```

## For .NETFramework Facade Assemblies

Some assemblies define types in .NETStandard and .NETCore but require facades on .NETFramework to forward types to their existing location in .NETFramework. In these cases we need to add type forwards manually to the .NETFramework build of the reference assembly. TypeForwards must be added for every type in the compatible .NETStandard reference assembly that exists in the .NETFramework, types which are defined in the .NETFramework reference should be factored into a shared source file.
