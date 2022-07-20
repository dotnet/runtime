// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_valuetype
{
class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
}

namespace Test
{
    public struct Dummy
    {
        public string Virtual()
        {
            return "Dummy.Virtual";
        }
    }

    public delegate string TestMethod();

    public static class Program
    {
        public static void CallDummy()
        {
            Dummy dummy = new Dummy();
            Assert.AreEqual("Dummy.Virtual", dummy.Virtual());
            Assert.AreEqual("Dummy.Virtual", new TestMethod(dummy.Virtual));
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                CallDummy();
                Console.WriteLine("Test SUCCESS");
                return 100;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Test FAILED");
                return 101;
            }
        }
    }

    public static class Assert
    {
        public static void AreEqual(string left, TestMethod right)
        {
            AreEqual(left, right());
        }
        public static void AreEqual(string left, string right)
        {
            if (String.IsNullOrEmpty(left))
                throw new ArgumentNullException("left");
            if (string.IsNullOrEmpty(right))
                throw new ArgumentNullException("right");
            if (left != right)
            {
                string message = String.Format("[[{0}]] != [[{1}]]", left, right);
                throw new ApplicationException(message);
            }
        }
    }
}
}
