// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_94467
{
    public interface ITypeChecker
    {
        static abstract bool Test<T>(T value);
    }

    public interface IHandler
    {
        bool Test<T>(T value);
    }

    public struct TypeChecker : ITypeChecker
    {
        public static bool Test<T>(T value) => true;
    }

    public class Handler<TChecker> : IHandler where TChecker : ITypeChecker
    {
        public bool Test<T>(T value) => TChecker.Test(value);
    }

    public static IHandler GetHandler() => new Handler<TypeChecker>();

    [Fact]
    public static int Test()
    {
        try {
            var handler = GetHandler();
            if (handler.Test<bool>(true) && handler.Test<bool?>(true))
                return 100;
            else
                return 101;
        } catch (Exception) {
            return -1;
        }
    }
}
