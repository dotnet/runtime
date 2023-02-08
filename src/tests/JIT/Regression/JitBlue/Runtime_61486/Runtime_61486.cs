// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_61486
{
    [Fact]
    public static int TestEntryPoint()
    {
        var my = new My(new My(null));
        var m = my.GetType().GetMethod("M");
        try
        {
            m.Invoke(my, null);
            return -1;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException)
        {
            return 100;
        }
    }

    public interface IFace
    {
        void M();
    }

    public class My : IFace
    {
        private IFace _face;

        public My(IFace face)
        {
            _face = face;
        }

        // We cannot handle a null ref inside a VSD if the caller is not
        // managed frame. This test is verifying that JIT null checks ahead of
        // time in this case.
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void M() => _face.M();
    }
}
