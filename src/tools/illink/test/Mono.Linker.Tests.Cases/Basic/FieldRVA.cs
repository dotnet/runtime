using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
	class FieldRVA
	{
        [Kept]
        [KeptInitializerData]
        static int Main() => new byte[] { 1, 2, 3, 4, 5 }.Length;
	}
}
