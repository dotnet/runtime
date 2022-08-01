// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_delegate
{
class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
}

namespace Test
{
    public abstract class Base
    {
        public abstract string AbstractFinal();
        public abstract string AbstractOverrideFinal();
        public abstract string AbstractOverrideOverride();
        public abstract string AbstractOverrideNil();

        public virtual string VirtualFinal()
        {
            return "Base.VirtualFinal";
        }

        public virtual string VirtualNilFinal()
        {
            return "Base.VirtualNilFinal";
        }

        public virtual string VirtualOverrideFinal()
        {
            return "Base.VirtualOverrideFinal";
        }
        public virtual string VirtualNilOverride()
        {
            return "Base.VirtualNilOverride";
        }
        public virtual string VirtualNilNil()
        {
            return "Base.VirtualNilNil";
        }
        public virtual string VirtualOverrideOverride()
        {
            return "Base.VirtualOverrideOverride";
        }
        public virtual string VirtualOverrideNil()
        {
            return "Base.VirtualOverrideNil";
        }
    }

    public class Child : Base
    {
        public sealed override string AbstractFinal()
        {
            return "Child.AbstractFinal";
        }
        public override string AbstractOverrideFinal()
        {
            return "Child.AbstractOverrideFinal";
        }
        public override string AbstractOverrideOverride()
        {
            return "Child.AbstractOverrideOverride";
        }
        public override string AbstractOverrideNil()
        {
            return "Child.AbstractOverrideNil";
        }
        public sealed override string VirtualFinal()
        {
            return "Child.VirtualFinal";
        }
        public override string VirtualOverrideOverride()
        {
            return "Child.VirtualOverrideOverride";
        }
        public override string VirtualOverrideNil()
        {
            return "Child.VirtualOverrideNil";
        }
    }

    public class GrandChild : Child
    {
        public sealed override string AbstractOverrideFinal()
        {
            return "GrandChild.AbstractOverrideFinal";
        }
        public override string AbstractOverrideOverride()
        {
            return "GrandChild.AbstractOverrideOverride";
        }
        public sealed override string VirtualNilFinal()
        {
            return "GrandChild.VirtualNilFinal";
        }
        public sealed override string VirtualOverrideFinal()
        {
            return "GrandChild.VirtualOverrideFinal";
        }
        public override string VirtualOverrideOverride()
        {
            return "GrandChild.VirtualOverrideOverride";
        }
        public override string VirtualNilOverride()
        {
            return "GrandChild.VirtualNilOverride";
        }
    }

    public sealed class SealedGrandChild : GrandChild
    { }

    public delegate string TestMethod();

    public static class Program
    {
        public static void CallDelegateFromSealedGrandChild()
        {
            SealedGrandChild child = new SealedGrandChild();

            Assert.AreEqual("Child.AbstractFinal", new TestMethod(child.AbstractFinal));
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", new TestMethod(child.AbstractOverrideFinal));
            Assert.AreEqual("Child.AbstractOverrideNil", new TestMethod(child.AbstractOverrideNil));
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", new TestMethod(child.AbstractOverrideOverride));
            Assert.AreEqual("Child.VirtualFinal", new TestMethod(child.VirtualFinal));
            Assert.AreEqual("GrandChild.VirtualNilFinal", new TestMethod(child.VirtualNilFinal));
            Assert.AreEqual("Base.VirtualNilNil", new TestMethod(child.VirtualNilNil));
            Assert.AreEqual("GrandChild.VirtualNilOverride", new TestMethod(child.VirtualNilOverride));
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", new TestMethod(child.VirtualOverrideFinal));
            Assert.AreEqual("Child.VirtualOverrideNil", new TestMethod(child.VirtualOverrideNil));
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", new TestMethod(child.VirtualOverrideOverride));
        }

        public static void CallDelegateFromGrandChild()
        {
            GrandChild child = new GrandChild();

            Assert.AreEqual("Child.AbstractFinal", new TestMethod(child.AbstractFinal));
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", new TestMethod(child.AbstractOverrideFinal));
            Assert.AreEqual("Child.VirtualFinal", new TestMethod(child.VirtualFinal));
            Assert.AreEqual("GrandChild.VirtualNilFinal", new TestMethod(child.VirtualNilFinal));
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", new TestMethod(child.VirtualOverrideFinal));
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                CallDelegateFromGrandChild();
                CallDelegateFromSealedGrandChild();

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
