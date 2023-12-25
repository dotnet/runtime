// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace StaticVirtualsAndMethodConstraintsAndDefaultImplementation
{
    public interface ITestItem<T>
    {
    }

    public interface IStaticInterfaceBase<T, in TRequest>
        where T : class
        where TRequest : IStaticInterfaceBase<T, TRequest>
    {
        static abstract int TryInvoke<TItem>(TItem item, TRequest request) where TItem : ITestItem<T>;
    }

    public interface IStaticInterface<T, in TRequest> : IStaticInterfaceBase<T, TRequest>
        where T : class
        where TRequest : IStaticInterface<T, TRequest>
    {
        static int IStaticInterfaceBase<T, TRequest>.TryInvoke<TItem>(TItem item, TRequest request)
        {
            return 100;
        }
    }

    public class Request : IStaticInterface<object, Request>
    {
    }

    public class Program
    {
        public static int Invoke<T, TRequest>(TRequest request)
            where T : class
            where TRequest : IStaticInterfaceBase<T, TRequest> =>
            TRequest.TryInvoke((ITestItem<T>) null!, request);

        [Fact]
        public static int TestEntryPoint() => Invoke<object, Request>(new Request());
    }
}
