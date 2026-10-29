// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using SignatureDependency;
using Xunit;

public class ColdSignatureDispatch
{
    private static bool s_isGCStress;
    private static int s_gcCountBeforeDispatch;

    public interface ITransform<T>
    {
        SignatureValue Transform(SignatureValue value);
    }

    public sealed class GenericTransform<T> : ITransform<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public SignatureValue Transform(SignatureValue value)
        {
            if (s_isGCStress)
            {
                Assert.True(GC.CollectionCount(0) > s_gcCountBeforeDispatch,
                    "GC stress must collect during first-call resolution, before entering the target.");
            }
            Assert.Equal(typeof(string), typeof(T));

            return value;
        }
    }

    public abstract class GenericVirtualTransform
    {
        public abstract GenericVirtualSignatureValue Transform<T>(GenericVirtualSignatureValue value);
    }

    public sealed class GenericVirtualTransformImpl : GenericVirtualTransform
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override GenericVirtualSignatureValue Transform<T>(GenericVirtualSignatureValue value)
        {
            Assert.Equal(typeof(string), typeof(T));

            return value;
        }
    }

    public sealed class Caller<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public SignatureValue Invoke(ITransform<T> target, SignatureValue value)
        {
            s_gcCountBeforeDispatch = GC.CollectionCount(0);
            SignatureValue result = target.Transform(value);
            GC.KeepAlive(target);

            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public GenericVirtualSignatureValue InvokeGenericVirtual(GenericVirtualTransform target, GenericVirtualSignatureValue value)
        {
            return target.Transform<T>(value);
        }
    }

    // A cold shared-generic interface call must prepare signature types before GC scans its
    // dispatch frame. Composite R2R with GC stress exercises this with a signature-only struct
    // in a separate assembly, not an interface type argument, and GC before target entry.
    [Fact]
    public static void TestEntryPoint()
    {
        s_isGCStress = TestLibrary.Utilities.IsGCStress;
        object reference = new object();
        SignatureValue result = new Caller<string>().Invoke(
            new GenericTransform<string>(), new SignatureValue { Reference = reference });
        Assert.Same(reference, result.Reference);

        // Shared code resolves a generic virtual target at run time. The resolved instantiation
        // must load its signature types before GC can scan its prestub frame.
        GenericVirtualSignatureValue genericVirtualResult = new Caller<string>().InvokeGenericVirtual(
            new GenericVirtualTransformImpl(), new GenericVirtualSignatureValue { Reference = reference });
        Assert.Same(reference, genericVirtualResult.Reference);
    }
}
