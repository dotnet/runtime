// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

// This test comes from https://github.com/dotnet/runtime/issues/79603
namespace RecursiveConstraintOnDefaultImplementationOfStaticAbstractMember
{
    public class Tests
    {
        [Fact]
        public static void AbstractClass()
        {
            _ = typeof(Buggyclass).TypeHandle;
        }

        [Fact]
        public static void ImplClass()
        {
            _ = new C2();
        }
    }

    public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
    {
        static abstract TSelf Create();
    }
    public interface IFactoryimpl<TSelf> : IFactory<TSelf> where TSelf : class, IFactory<TSelf>
    {
        static TSelf IFactory<TSelf>.Create() => default;
    }
    public abstract class Buggyclass : IFactoryimpl<Buggyclass> { }
    class C2 : Buggyclass { }
}
