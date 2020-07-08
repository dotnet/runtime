// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public string CallAbstractFinal()
        {
            return AbstractFinal();
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
        public string CallAbstractOverrideNil()
        {
            return AbstractOverrideNil();
        }
        public sealed override string VirtualFinal()
        {
            return "Child.VirtualFinal";
        }
        public string CallVirtualFinal()
        {
            return VirtualFinal();
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
        public string CallVirtualOverrideNil()
        {
            return VirtualOverrideNil();
        }
    }

    public class GrandChild : Child
    {
        public sealed override string AbstractOverrideFinal()
        {
            return "GrandChild.AbstractOverrideFinal";
        }

        public string CallAbstractOverrideFinal()
        {
            return AbstractOverrideFinal();
        }

        public override string AbstractOverrideOverride()
        {
            return "GrandChild.AbstractOverrideOverride";
        }

        public string CallAbstractOverrideOverride()
        {
            return AbstractOverrideOverride();
        }

        public sealed override string VirtualNilFinal()
        {
            return "GrandChild.VirtualNilFinal";
        }

        public string CallVirtualNilFinal()
        {
            return VirtualNilFinal();
        }

        public sealed override string VirtualOverrideFinal()
        {
            return "GrandChild.VirtualOverrideFinal";
        }

        public string CallVirtualOverrideFinal()
        {
            return VirtualOverrideFinal();
        }

        public override string VirtualOverrideOverride()
        {
            return "GrandChild.VirtualOverrideOverride";
        }

        public string CallVirtualOverrideOverride()
        {
            return VirtualOverrideOverride();
        }

        public override string VirtualNilOverride()
        {
            return "GrandChild.VirtualNilOverride";
        }

        public string CallVirtualNilOverride()
        {
            return VirtualNilOverride();
        }

        public void TestGrandChild()
        {
            Console.WriteLine("Call from inside GrandChild");
            Assert.AreEqual("Child.AbstractFinal", CallAbstractFinal());
            Assert.AreEqual("GrandChild.AbstractOverrideFinal", CallAbstractOverrideFinal());
            Assert.AreEqual("GrandChild.AbstractOverrideOverride", CallAbstractOverrideOverride());
            Assert.AreEqual("Child.AbstractOverrideNil", CallAbstractOverrideNil());
            Assert.AreEqual("Child.VirtualFinal", CallVirtualFinal());
            Assert.AreEqual("GrandChild.VirtualOverrideFinal", CallVirtualOverrideFinal());
            Assert.AreEqual("GrandChild.VirtualOverrideOverride", CallVirtualOverrideOverride());
            Assert.AreEqual("Child.VirtualOverrideNil", CallVirtualOverrideNil());
        }
    }

    public static class Program
    {
        public static void CallFromInsideGrandChild()
        {
            GrandChild child = new GrandChild();
            child.TestGrandChild();
        }

        public static int Main(string[] args)
        {
            try
            {
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
