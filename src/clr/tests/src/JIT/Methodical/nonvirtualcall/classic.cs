// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

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
        public override string VirtualOverrideFinal()
        {
            return "Child.VirtualOverrideFinal";
        }
        public override string VirtualOverrideOverride()
        {
            return "Child.VirtualOverrideOverride";
        }
        public override string VirtualOverrideNil()
        {
            return "Child.VirtualOverrideNil";
        }

        public void TestChild()
        {
            Console.WriteLine("Call from inside Child");
            Assert.AreEqual("Child.AbstractFinal", AbstractFinal());
            Assert.AreEqual("Child.AbstractOverrideFinal", AbstractOverrideFinal());
            Assert.AreEqual("Child.AbstractOverrideOverride", AbstractOverrideOverride());
            Assert.AreEqual("Child.AbstractOverrideNil", AbstractOverrideNil());
            Assert.AreEqual("Base.VirtualFinal", base.VirtualFinal());
            Assert.AreEqual("Child.VirtualFinal", VirtualFinal());
            Assert.AreEqual("Base.VirtualOverrideFinal", base.VirtualOverrideFinal());
            Assert.AreEqual("Child.VirtualOverrideFinal", VirtualOverrideFinal());
            Assert.AreEqual("Base.VirtualOverrideOverride", base.VirtualOverrideOverride());
            Assert.AreEqual("Child.VirtualOverrideOverride", VirtualOverrideOverride());
            Assert.AreEqual("Base.VirtualOverrideNil", base.VirtualOverrideNil());
            Assert.AreEqual("Child.VirtualOverrideNil", VirtualOverrideNil());
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

        public void TestGrandChild()
        {
            Console.WriteLine("Call from inside GrandChild");
            Assert.AreEqual("Child.AbstractFinal", base.AbstractFinal());
            Assert.AreEqual("Child.AbstractFinal", AbstractFinal());
            Assert.AreEqual("Child.AbstractOverrideFinal", base.AbstractOverrideFinal());
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", AbstractOverrideFinal());
            Assert.AreEqual("Child.AbstractOverrideOverride", base.AbstractOverrideOverride());
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", AbstractOverrideOverride());
            Assert.AreEqual("Child.AbstractOverrideNil", base.AbstractOverrideNil());
            Assert.AreEqual("Child.AbstractOverrideNil", AbstractOverrideNil());
            Assert.AreEqual("Child.VirtualFinal", base.VirtualFinal());
            Assert.AreEqual("Child.VirtualFinal", VirtualFinal());
            Assert.AreEqual("Child.VirtualOverrideFinal", base.VirtualOverrideFinal());
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", VirtualOverrideFinal());
            Assert.AreEqual("Child.VirtualOverrideOverride", base.VirtualOverrideOverride());
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", VirtualOverrideOverride());
            Assert.AreEqual("Child.VirtualOverrideNil", base.VirtualOverrideNil());
            Assert.AreEqual("Child.VirtualOverrideNil", VirtualOverrideNil());
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
            Assert.AreEqual("Child.AbstractFinal", o.AbstractFinal());
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", o.AbstractOverrideFinal());
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", o.AbstractOverrideOverride());
            Assert.AreEqual("Child.AbstractOverrideNil", o.AbstractOverrideNil());
            Assert.AreEqual("Child.VirtualFinal", o.VirtualFinal());
            Assert.AreEqual("GrandChild.VirtualNilFinal", o.VirtualNilFinal());
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", o.VirtualOverrideFinal());
            Assert.AreEqual("GrandChild.VirtualNilOverride", o.VirtualNilOverride());
            Assert.AreEqual("Base.VirtualNilNil", o.VirtualNilNil());
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", o.VirtualOverrideOverride());
            Assert.AreEqual("Child.VirtualOverrideNil", o.VirtualOverrideNil());
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

        public static int Main(string[] args)
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
