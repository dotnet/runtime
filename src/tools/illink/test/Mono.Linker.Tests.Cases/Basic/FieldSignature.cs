using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    class FieldSignature
    {
        [Kept]
        static FieldType field;

        [Kept]
        static void Main()
        {
            field = null;
            _ = field;
        }

        [Kept]
        class FieldType
        {
        }
    }
}
