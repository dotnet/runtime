#if IVT
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo ("missing")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo ("test-with-key, PublicKey=00240000")]
#endif

namespace Mono.Linker.Tests.Cases.Attributes.Dependencies;

public class IVTUnusedLib;
