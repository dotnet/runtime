// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Object is the root class for all CLR objects.  This class
** defines only the basics.
**
** 
===========================================================*/

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
    public class Object
    {
        // Creates a new instance of an Object.
        [NonVersionable]
        public Object()
        {
        }

        // Returns a String which represents the object instance.  The default
        // for an object is to return the fully qualified name of the class.
        // 
        public virtual string ToString()
        {
            return GetType().ToString();
        }

        // Returns a boolean indicating if the passed in object obj is 
        // Equal to this.  Equality is defined as object equality for reference
        // types and bitwise equality for value types using a loader trick to
        // replace Equals with EqualsValue for value types).
        // 

        public virtual bool Equals(object obj)
        {
            return RuntimeHelpers.Equals(this, obj);
        }

        public static bool Equals(object objA, object objB)
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
        public static bool ReferenceEquals(object objA, object objB)
        {
            return objA == objB;
        }

        // GetHashCode is intended to serve as a hash function for this object.
        // Based on the contents of the object, the hash function will return a suitable
        // value with a relatively random distribution over the various inputs.
        //
        // The default implementation returns the sync block index for this instance.
        // Calling it on the same object multiple times will return the same value, so
        // it will technically meet the needs of a hash function, but it's less than ideal.
        // Objects (& especially value classes) should override this method.
        // 
        public virtual int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        // Returns a Type object which represent this object instance.
        // 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern Type GetType();

        // Allow an object to free resources before the object is reclaimed by the GC.
        // 
        [NonVersionable]
        ~Object()
        {
        }

        // Returns a new object instance that is a memberwise copy of this 
        // object.  This is always a shallow copy of the instance. The method is protected
        // so that other object may only call this method on themselves.  It is entended to
        // support the ICloneable interface.
        // 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        protected extern object MemberwiseClone();
    }


    // Internal methodtable used to instantiate the "canonical" methodtable for generic instantiations.
    // The name "__Canon" will never been seen by users but it will appear a lot in debugger stack traces
    // involving generics so it is kept deliberately short as to avoid being a nuisance.

    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    internal class __Canon
    {
    }

    // This class is used to define the name of the base class library
    internal class CoreLib
    {
        public const string Name = "System.Private.CoreLib";

        public static string FixupCoreLibName(string strToFixup)
        {
            if (!string.IsNullOrEmpty(strToFixup))
            {
                strToFixup = strToFixup.Replace("mscorlib", System.CoreLib.Name);
            }

            return strToFixup;
        }
    }
}
