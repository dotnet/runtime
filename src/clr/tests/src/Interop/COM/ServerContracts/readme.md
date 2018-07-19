## Server Contracts

This directory contains the API contracts for the testing of .NET COM interop. The contract is defined in C# to the degree that the [TlbExp.exe](https://docs.microsoft.com/en-us/dotnet/framework/tools/tlbexp-exe-type-library-exporter) tool can be used to generate a TLB that can then be used by the Microsoft VC++ compiler to generate the `tlh` and `tli` files. This is a manual process at the moment, but as more support is added to CoreCLR, this may change.

The process to create a TLB and update the native contracts are as follows:

1) Take the `Primitives.cs` file and create a DLL using a SDK project.
1) Use the .NET Framework [TlbExp.exe](https://docs.microsoft.com/en-us/dotnet/framework/tools/tlbexp-exe-type-library-exporter) to create a `tlb` based on the DLL previously compiled.
1) Using the Microsoft VC++ compiler consume the `tlb` using the [`#import`](https://msdn.microsoft.com/en-us/library/8etzzkb6.aspx) directive (See commented out line in `Servers.h`). The compiler will generate two files (`tlh` and `tli`). The files in this directory can then be updated.