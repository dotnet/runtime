// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.Compiler.Tests.Assets
{
    public class JitInterface
    {
        public struct Struct
        {
            public object Reference;
        }

        public struct BlittableStruct
        {
            public int Value;
        }

        public int Instance;
        public static int Primitive;
        public static object Reference;
        public static BlittableStruct StructWithoutReference;
        public static Struct StructWithReference;
    }
}
