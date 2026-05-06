// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class NonNamespaceQualifiedType
{

}

namespace TypeNameParsing
{
    public class Generic<T>
    {
        public class NestedNongeneric
        {
        }

        public class NestedGeneric<U>
        {
        }
    }

    public class VeryGeneric<T, U, V>
    {
    }

    public class Simple
    {
        public class Nested
        {
            public class NestedTwice
            {
            }
        }
    }

    public struct Struct
    {
    }
}
