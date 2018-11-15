This document provides the steps you need to take to update the reference assembly when adding new **public** APIs to an implementation assembly (post [API Review](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/adding-api-guidelines.md)).

## For most assemblies within corefx
1) Implement the API in the source assembly and [build it](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#building-individual-libraries).
2) Run the following command (from the src directory) `msbuild /t:GenerateReferenceSource` to update the reference assembly**.
3) Navigate to the ref directory and build the reference assembly.
4) Add, build, and run tests.

** **Note:** If you already added the new API to the reference source, re-generating it (after building the source assembly) will update it to be fully qualified  and placed in the correct order. This can be done by running the `GenerateReferenceSource` command from the ref directory.

## For System.Runtime
These steps can also be applied to some unique assemblies which depend on changes in System.Private.Corelib coming from [coreclr](https://github.com/dotnet/coreclr) (partial facades like [System.Memory](https://github.com/dotnet/corefx/blob/83711167ee74d2e87cf2d5ed3508c94044bb7edc/src/System.Memory/src/System.Memory.csproj#L6), for example).
1) Build coreclr release.
2) Build corefx release with coreclr bits (see [Testing with private CoreCLR bits](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits) for more details).
3) Run `msbuild /t:GenerateReferenceSource /p:ConfigurationGroup=Release` from the System.Runtime/ref directory.
4) Filter out all unrelated changes and extract the changes you care about (ignore certain attributes being removed). Generally, this step is not required for other reference assemblies.