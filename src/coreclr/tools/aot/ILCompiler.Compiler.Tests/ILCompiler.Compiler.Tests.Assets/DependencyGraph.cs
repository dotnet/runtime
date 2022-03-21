// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace ILCompiler.Compiler.Tests.Assets
{
    //
    // Classes nested under this class gets automatically discovered by the unit test.
    // The unit test will locate the Entrypoint method, run the IL scanner on it,
    // and validate the invariants declared by the Entrypoint method with custom attributes.
    //

    class DependencyGraph
    {
        /// <summary>
        /// Validates a cast doesn't force a constructed EEType.
        /// </summary>
        class PInvokeCctorDependencyTest
        {
            class TypeThatWasNeverAllocated
            {
                public static object O = null;
            }

            [NoConstructedEEType(typeof(TypeThatWasNeverAllocated))]
            public static void Entrypoint()
            {
                ((TypeThatWasNeverAllocated)TypeThatWasNeverAllocated.O).GetHashCode();
            }
        }

        class GenericVirtualMethodDirectCallDependencyTest
        {
            class NeverAllocated { }

            class Base
            {
                public virtual object GenericVirtualCalledDirectly<T>()
                {
                    return null;
                }
            }

            class Derived : Base
            {
                public override object GenericVirtualCalledDirectly<T>()
                {
                    return new NeverAllocated();
                }

                public object CallBaseGenericVirtualDirectly<T>()
                {
                    // This is a call in IL, not callvirt
                    return base.GenericVirtualCalledDirectly<T>();
                }
            }

            [NoConstructedEEType(typeof(NeverAllocated))]
            public static void Entrypoint()
            {
                new Base();
                new Derived().CallBaseGenericVirtualDirectly<object>();
            }
        }
    }

    #region Custom attributes that define invariants to check
    public class GeneratesConstructedEETypeAttribute : Attribute
    {
        public GeneratesConstructedEETypeAttribute(Type type) { }
    }

    public class NoConstructedEETypeAttribute : Attribute
    {
        public NoConstructedEETypeAttribute(Type type) { }
    }

    public class GeneratesMethodBodyAttribute : Attribute
    {
        public GeneratesMethodBodyAttribute(Type owningType, string methodName) { }

        public Type[] GenericArguments;
        public Type[] Signature;
    }

    public class NoMethodBodyAttribute : Attribute
    {
        public NoMethodBodyAttribute(Type owningType, string methodName) { }

        public Type[] GenericArguments;
        public Type[] Signature;
    }
    #endregion
}
