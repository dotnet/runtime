// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

namespace GetInterfaceMapWithStaticVirtualsAndConstraints
{
    public static class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            Type i, s;
            InterfaceMapping imap;
            Console.WriteLine("Inner");
            s = typeof(Program).Assembly.GetType("GetInterfaceMapWithStaticVirtualsAndConstraints.Outer`1+Inner");
            s = s.MakeGenericType(typeof(int));
            i = s.GetInterface("IStatics");
            imap = s.GetInterfaceMap(i);
            foreach (var iMethod in imap.InterfaceMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
            }

            Assert.Single(imap.TargetMethods);
            foreach (var iMethod in imap.TargetMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
                Assert.Equal(typeof(Outer<int>.Inner), iMethod.DeclaringType);
            }

            i = s.GetInterface("IInstance");
            imap = s.GetInterfaceMap(i);
            foreach (var iMethod in imap.InterfaceMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
            }

            Assert.Single(imap.TargetMethods);
            foreach (var iMethod in imap.TargetMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
                Assert.Equal(typeof(Outer<int>.Inner), iMethod.DeclaringType);
            }

            Console.WriteLine("Inner2");
            s = typeof(Program).Assembly.GetType("GetInterfaceMapWithStaticVirtualsAndConstraints.Outer`1+Inner2");
            s = s.MakeGenericType(typeof(int));
            i = s.GetInterface("IStatics");
            imap = s.GetInterfaceMap(i);
            foreach (var iMethod in imap.InterfaceMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
            }
            Assert.Single(imap.TargetMethods);
            foreach (var iMethod in imap.TargetMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
                Assert.Equal(typeof(Outer<int>.IStaticsImpl), iMethod.DeclaringType);
            }

            i = s.GetInterface("IInstance");
            imap = s.GetInterfaceMap(i);
            foreach (var iMethod in imap.InterfaceMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
            }
            Assert.Single(imap.TargetMethods);
            foreach (var iMethod in imap.TargetMethods)
            {
                Console.WriteLine($"{iMethod.DeclaringType } {iMethod}");
                Assert.Equal(typeof(Outer<int>.IInstanceImpl), iMethod.DeclaringType);
            }
        }
    }

    class Outer<TOuter>
    {
        public struct Inner : IStatics, IInstance
        {
            public void M<TInner>() where TInner : IConstraint { }
            public static void MStatic<TInner>() where TInner : IConstraint { }
        }

        public struct Inner2 : IStaticsImpl, IInstanceImpl
        {

        }

        public interface IStaticsImpl : IStatics
        {
            static void IStatics.MStatic<TInner>() { }
        }

        public interface IInstanceImpl : IInstance
        {
            void IInstance.M<TInner>() { }
        }

        public interface IStatics
        {
            static abstract void MStatic<TInner2>() where TInner2 : IConstraint;
        }

        public interface IInstance
        {
            abstract void M<TInner2>() where TInner2 : IConstraint;
        }

        public interface IConstraint { }
    }
}
