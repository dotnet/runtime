// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Casting
{
    class Base { }

    interface IFoo { }

    interface IContravariant<in T> { }

    class ClassImplementingIFoo : IFoo { }

    class ClassImplementingIFooIndirectly : ClassImplementingIFoo { }

    enum IntBasedEnum : int { }

    enum UIntBasedEnum : uint { }

    enum ShortBasedEnum : short { }

    class ClassWithNoConstraint<T> { }

    class ClassWithValueTypeConstraint<T> where T : struct { }

    class ClassWithBaseClassConstraint<T> where T : Base { }

    class ClassWithInterfaceConstraint<T> where T : IFoo { }

    class ClassWithRecursiveImplementation : IContravariant<IContravariant<ClassWithRecursiveImplementation>> { }
}
