using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.LinkXml;

[assembly: TypeForwardedTo (typeof (UsedNonRequiredExportedTypeIsKept_Used1))]
[assembly: TypeForwardedTo (typeof (UsedNonRequiredExportedTypeIsKept_Used2))]
[assembly: TypeForwardedTo (typeof (UsedNonRequiredExportedTypeIsKept_Used3))]
