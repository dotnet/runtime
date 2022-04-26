// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Test_generics
{
class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
}

namespace Test
{
    public abstract class Base
    {
        public abstract string AbstractFinal<T>();
        public abstract string AbstractOverrideFinal<T>();
        public abstract string AbstractOverrideOverride<T>();
        public abstract string AbstractOverrideNil<T>();

        public virtual string VirtualFinal<T>()
        {
            return "Base.VirtualFinal";
        }
        public virtual string VirtualNilFinal<T>()
        {
            return "Base.VirtualNilFinal";
        }
        public virtual string VirtualOverrideFinal<T>()
        {
            return "Base.VirtualOverrideFinal";
        }
        public virtual string VirtualNilOverride<T>()
        {
            return "Base.VirtualNilOverride";
        }
        public virtual string VirtualNilNil<T>()
        {
            return "Base.VirtualNilNil";
        }
        public virtual string VirtualOverrideOverride<T>()
        {
            return "Base.VirtualOverrideOverride";
        }
        public virtual string VirtualOverrideNil<T>()
        {
            return "Base.VirtualOverrideNil";
        }
    }

    public class Child : Base
    {
        public sealed override string AbstractFinal<T>()
        {
            return "Child.AbstractFinal";
        }
        public override string AbstractOverrideFinal<T>()
        {
            return "Child.AbstractOverrideFinal";
        }
        public override string AbstractOverrideOverride<T>()
        {
            return "Child.AbstractOverrideOverride";
        }
        public override string AbstractOverrideNil<T>()
        {
            return "Child.AbstractOverrideNil";
        }
        public sealed override string VirtualFinal<T>()
        {
            return "Child.VirtualFinal";
        }
        public override string VirtualOverrideFinal<T>()
        {
            return "Child.VirtualOverrideFinal";
        }
        public override string VirtualOverrideOverride<T>()
        {
            return "Child.VirtualOverrideOverride";
        }
        public override string VirtualOverrideNil<T>()
        {
            return "Child.VirtualOverrideNil";
        }

        public void TestChild()
        {
            Console.WriteLine("Call from inside Child");
            Assert.AreEqual("Child.AbstractFinal", AbstractFinal<object>());
            Assert.AreEqual("Child.AbstractOverrideFinal", AbstractOverrideFinal<object>());
            Assert.AreEqual("Child.AbstractOverrideOverride", AbstractOverrideOverride<object>());
            Assert.AreEqual("Child.AbstractOverrideNil", AbstractOverrideNil<object>());
            Assert.AreEqual("Base.VirtualFinal", base.VirtualFinal<object>());
            Assert.AreEqual("Child.VirtualFinal", VirtualFinal<object>());
            Assert.AreEqual("Base.VirtualOverrideFinal", base.VirtualOverrideFinal<object>());
            Assert.AreEqual("Child.VirtualOverrideFinal", VirtualOverrideFinal<object>());
            Assert.AreEqual("Base.VirtualOverrideOverride", base.VirtualOverrideOverride<object>());
            Assert.AreEqual("Child.VirtualOverrideOverride", VirtualOverrideOverride<object>());
            Assert.AreEqual("Base.VirtualOverrideNil", base.VirtualOverrideNil<object>());
            Assert.AreEqual("Child.VirtualOverrideNil", VirtualOverrideNil<object>());
        }
    }

    public class GrandChild : Child
    {
        public sealed override string AbstractOverrideFinal<T>()
        {
            return "GrandChild.AbstractOverrideFinal";
        }

        public override string AbstractOverrideOverride<T>()
        {
            return "GrandChild.AbstractOverrideOverride";
        }
        public sealed override string VirtualNilFinal<T>()
        {
            return "GrandChild.VirtualNilFinal";
        }
        public sealed override string VirtualOverrideFinal<T>()
        {
            return "GrandChild.VirtualOverrideFinal";
        }
        public override string VirtualOverrideOverride<T>()
        {
            return "GrandChild.VirtualOverrideOverride";
        }
        public override string VirtualNilOverride<T>()
        {
            return "GrandChild.VirtualNilOverride";
        }

        public void TestGrandChild()
        {
            Console.WriteLine("Call from inside GrandChild");
            Assert.AreEqual("Child.AbstractFinal", AbstractFinal<object>());
            Assert.AreEqual("Child.AbstractOverrideFinal", base.AbstractOverrideFinal<object>());
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", AbstractOverrideFinal<object>());
            Assert.AreEqual("Child.AbstractOverrideOverride", base.AbstractOverrideOverride<object>());
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", AbstractOverrideOverride<object>());
            Assert.AreEqual("Child.AbstractOverrideNil", base.AbstractOverrideNil<object>());
            Assert.AreEqual("Child.AbstractOverrideNil", AbstractOverrideNil<object>());
            Assert.AreEqual("Child.VirtualFinal", base.VirtualFinal<object>());
            Assert.AreEqual("Child.VirtualFinal", VirtualFinal<object>());
            Assert.AreEqual("Child.VirtualOverrideFinal", base.VirtualOverrideFinal<object>());
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", VirtualOverrideFinal<object>());
            Assert.AreEqual("Child.VirtualOverrideOverride", base.VirtualOverrideOverride<object>());
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", VirtualOverrideOverride<object>());
            Assert.AreEqual("Child.VirtualOverrideNil", base.VirtualOverrideNil<object>());
            Assert.AreEqual("Child.VirtualOverrideNil", VirtualOverrideNil<object>());
        }
    }

    public sealed class SealedGrandChild : GrandChild
    { }

    public static class Program
    {
        public static void CallSealedGrandChild()
        {
            Console.WriteLine("Call SealedGrandChild from outside");
            // Calling methods of a sealed class from outside
            SealedGrandChild o = new SealedGrandChild();
            Assert.AreEqual("Child.AbstractFinal", o.AbstractFinal<object>());
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", o.AbstractOverrideFinal<object>());
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", o.AbstractOverrideOverride<object>());
            Assert.AreEqual("Child.AbstractOverrideNil", o.AbstractOverrideNil<object>());
            Assert.AreEqual("Child.VirtualFinal", o.VirtualFinal<object>());
            Assert.AreEqual("GrandChild.VirtualNilFinal", o.VirtualNilFinal<object>());
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", o.VirtualOverrideFinal<object>());
            Assert.AreEqual("GrandChild.VirtualNilOverride", o.VirtualNilOverride<object>());
            Assert.AreEqual("Base.VirtualNilNil", o.VirtualNilNil<object>());
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", o.VirtualOverrideOverride<object>());
            Assert.AreEqual("Child.VirtualOverrideNil", o.VirtualOverrideNil<object>());
        }

        public static void CallFromInsideChild()
        {
            Child child = new Child();
            child.TestChild();
        }

        public static void CallFromInsideGrandChild()
        {
            GrandChild child = new GrandChild();
            child.TestGrandChild();
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                CallSealedGrandChild();
                CallFromInsideChild();
                CallFromInsideGrandChild();

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
