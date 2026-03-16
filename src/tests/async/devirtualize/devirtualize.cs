// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Devirtualize
{
    class Base
    {
        public virtual async Task<int> GetValue()
        {
            await Task.Yield();
            return 1;
        }
    }

    // Non-sealed derived class: devirtualization must go through resolveVirtualMethod
    // because the JIT cannot use the sealed-type shortcut.
    class OpenDerived : Base
    {
        public override async Task<int> GetValue()
        {
            await Task.Yield();
            return 42;
        }
    }

    sealed class SealedDerived : Base
    {
        public override async Task<int> GetValue()
        {
            await Task.Yield();
            return 2;
        }
    }

    sealed class SealedDerivedNoYield : Base
    {
        [RuntimeAsyncMethodGeneration(false)]
        public override async Task<int> GetValue()
        {
            await Task.Yield();
            return 3;
        }
    }

    interface IAsyncInterface
    {
        Task<int> GetValue();
    }

    // Non-sealed interface implementation: devirtualization of the interface
    // call must go through resolveVirtualMethod.
    class OpenInterfaceImpl : IAsyncInterface
    {
        public virtual async Task<int> GetValue()
        {
            await Task.Yield();
            return 43;
        }
    }

    sealed class SealedInterfaceImpl : IAsyncInterface
    {
        public async Task<int> GetValue()
        {
            await Task.Yield();
            return 10;
        }
    }

    sealed class SealedInterfaceImplNoYield : IAsyncInterface
    {
        [RuntimeAsyncMethodGeneration(false)]
        public async Task<int> GetValue()
        {
            await Task.Yield();
            return 11;
        }
    }

    // Uses newobj to give the JIT exact type info on a non-sealed type.
    // The JIT must call resolveVirtualMethod to devirtualize.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnNewOpenDerived()
    {
        Base obj = new OpenDerived();
        return await obj.GetValue();
    }

    // Non-sealed interface impl with exact type from newobj.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnNewOpenInterfaceImpl()
    {
        IAsyncInterface obj = new OpenInterfaceImpl();
        return await obj.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnSealed(SealedDerived obj)
    {
        return await obj.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnSealedNoYield(SealedDerivedNoYield obj)
    {
        return await obj.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnSealedInterface(SealedInterfaceImpl obj)
    {
        return await obj.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnSealedInterfaceNoYield(SealedInterfaceImplNoYield obj)
    {
        return await obj.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static async Task<int> CallOnInterface<T>(T obj) where T : IAsyncInterface
    {
        return await obj.GetValue();
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(42, CallOnNewOpenDerived().Result);
        Assert.Equal(43, CallOnNewOpenInterfaceImpl().Result);
        Assert.Equal(2, CallOnSealed(new SealedDerived()).Result);
        Assert.Equal(3, CallOnSealedNoYield(new SealedDerivedNoYield()).Result);
        Assert.Equal(10, CallOnSealedInterface(new SealedInterfaceImpl()).Result);
        Assert.Equal(11, CallOnSealedInterfaceNoYield(new SealedInterfaceImplNoYield()).Result);
        Assert.Equal(10, CallOnInterface(new SealedInterfaceImpl()).Result);
        Assert.Equal(11, CallOnInterface(new SealedInterfaceImplNoYield()).Result);
    }
}
