// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Hashcode
{
    class NonNestedType
    {
        class NestedType
        {

        }

        void GenericMethod<T>()
        { }
    }

    class GenericType<X,Y>
    {
        void GenericMethod<T>()
        {
        }
    }
}
