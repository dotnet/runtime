// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Canonicalization
{
    class ReferenceType
    {
        void Method()
        {
        }

        void GenericMethod<U>()
        {
        }
    }

    class OtherReferenceType
    {
    }

    struct StructType
    {
        void Method()
        {
        }

        void GenericMethod<U>()
        {
        }
    }

    struct OtherStructType
    {
    }

    class GenericReferenceType<T>
    {
        void Method()
        {
        }

        void GenericMethod<U>()
        {
        }
    }

    struct GenericStructType<T>
    {
        void Method()
        {
        }

        void GenericMethod<U>()
        {
        }
    }

    class GenericReferenceTypeWithThreeParams<T, U, V>
    {
    }

    class GenericStructTypeWithThreeParams<T, U, V>
    {
    }
}
