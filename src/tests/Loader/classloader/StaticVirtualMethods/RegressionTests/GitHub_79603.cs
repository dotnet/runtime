// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace GitHub_79603
{
    internal class Program
    {
        static int Main(string[] args)
        {
           //both calls leads to crash BEFORE "Main" even got called.
            _ = typeof(Buggyclass).TypeHandle;
            _ = new C2();
            return 100;
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
