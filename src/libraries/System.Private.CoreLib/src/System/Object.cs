// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System
{
    // The Object is the root class for all object in the CLR System. Object
    // is the super class for all other CLR objects and provide a set of methods and low level
    // services to subclasses.  These services include object synchronization and support for clone
    // operations.
    //
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ComVisible(true)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class Object
    {
        // Creates a new instance of an Object.
        [NonVersionable]
        public Object()
        {
        }

        // Allow an object to free resources before the object is reclaimed by the GC.
        // This method's virtual slot number is hardcoded in runtimes. Do not add any virtual methods ahead of this.
        [NonVersionable]
#pragma warning disable CA1821 // Remove empty Finalizers
        ~Object()
        {
        }
#pragma warning restore CA1821

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public virtual string? ToString()
        {
            // The default for an object is to return the fully qualified name of the class.
            return GetType().ToString();
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public virtual bool Equals(object? obj)
        {
            return this == obj;
        }

        public static bool Equals(object? objA, object? objB)
        {
            if (objA == objB)
            {
                return true;
            }
            if (objA == null || objB == null)
            {
                return false;
            }
            return objA.Equals(objB);
        }

        [NonVersionable]
        public static bool ReferenceEquals(object? objA, object? objB)
        {
            return objA == objB;
        }

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>A hash code for the current object.</returns>
        public virtual int GetHashCode()
        {
            // GetHashCode is intended to serve as a hash function for this object.
            // Based on the contents of the object, the hash function will return a suitable
            // value with a relatively random distribution over the various inputs.
            //
            // The default implementation returns the sync block index for this instance.
            // Calling it on the same object multiple times will return the same value, so
            // it will technically meet the needs of a hash function, but it's less than ideal.
            // Objects (& especially value classes) should override this method.

            return RuntimeHelpers.GetHashCode(this);
        }
    }
}
