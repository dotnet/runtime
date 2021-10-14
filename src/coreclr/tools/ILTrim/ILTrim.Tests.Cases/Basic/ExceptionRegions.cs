// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    [Kept]
    public class ExceptionRegions
    {
        [Kept]
        static void Main()
        {
            try
            {
                A();
            }
            catch (CustomException ce)
            {
                Console.WriteLine(ce.Message);
                try
                {
                    B();
                }
                catch (Exception e) when (e.InnerException != null)
                {
                    Console.WriteLine(e.Message);
                }
            }
            finally
            {
                C();
            }
        }

        [Kept]
        static void A() { }

        [Kept]
        static void B() { }

        [Kept]
        static void C() { }
    }

    [Kept]
    [KeptBaseType(typeof(Exception))]
    class CustomException : Exception
    {
    }
}
