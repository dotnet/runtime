// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

// This test comes from https://github.com/dotnet/runtime/issues/107754

namespace VariantVirtualStaticDefaultDispatch
{
    interface IStaticConstraint<in T>
    {
        public abstract static void M();
    }

    interface IStaticConstraintDefaultImpl<in T> : IStaticConstraint<T>
    {
        static void IStaticConstraint<T>.M() { }
    }

    interface IConstraintCheck<U, W> where U : IStaticConstraint<W>
    {
    }

    struct StructThatImplementsConstraint : IStaticConstraintDefaultImpl<object>
    {
    }

    public class Tests
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void M<U>() where U: IStaticConstraint<string>
        {
            U.M();
        }

        [Fact]
        public static void RunTest()
        {
            System.Console.WriteLine(typeof(IConstraintCheck<StructThatImplementsConstraint, string>));
            M<StructThatImplementsConstraint>();
        }
    }
}
