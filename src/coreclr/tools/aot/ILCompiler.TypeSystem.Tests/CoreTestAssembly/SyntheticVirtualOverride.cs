// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SyntheticVirtualOverride
{
    struct StructWithNoEqualsAndGetHashCode
    {
    }

    class ClassWithInjectedEqualsAndGetHashCode
    {
    }

    class ClassOverridingEqualsAndGetHashCode : ClassWithInjectedEqualsAndGetHashCode
    {
        public override bool Equals(object other)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return 0;
        }
    }

    class ClassNotOverridingEqualsAndGetHashCode : ClassWithInjectedEqualsAndGetHashCode
    {
    }
}
