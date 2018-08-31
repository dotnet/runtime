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

namespace System
{
    using System;
    using System.Diagnostics;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using CultureInfo = System.Globalization.CultureInfo;
    using FieldInfo = System.Reflection.FieldInfo;
    using BindingFlags = System.Reflection.BindingFlags;

    // The Object is the root class for all object in the CLR System. Object 
    // is the super class for all other CLR objects and provide a set of methods and low level
    // services to subclasses.  These services include object synchronization and support for clone
    // operations.
    // 
    //This class contains no data and does not need to be serializable 
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class Object
    {
        // Creates a new instance of an Object.
        [System.Runtime.Versioning.NonVersionable]
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

        [System.Runtime.Versioning.NonVersionable]
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
        [System.Runtime.Versioning.NonVersionable]
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


        // Sets the value specified in the variant on the field
        // 
        private void FieldSetter(string typeName, string fieldName, object val)
        {
            Debug.Assert(typeName != null);
            Debug.Assert(fieldName != null);

            // Extract the field info object
            FieldInfo fldInfo = GetFieldInfo(typeName, fieldName);

            if (fldInfo.IsInitOnly)
                throw new FieldAccessException(SR.FieldAccess_InitOnly);

            // Make sure that the value is compatible with the type
            // of field
            Type pt = fldInfo.FieldType;
            if (pt.IsByRef)
            {
                pt = pt.GetElementType();
            }

            if (!pt.IsInstanceOfType(val))
            {
                val = Convert.ChangeType(val, pt, CultureInfo.InvariantCulture);
            }

            // Set the value
            fldInfo.SetValue(this, val);
        }

        // Gets the value specified in the variant on the field
        // 
        private void FieldGetter(string typeName, string fieldName, ref object val)
        {
            Debug.Assert(typeName != null);
            Debug.Assert(fieldName != null);

            // Extract the field info object
            FieldInfo fldInfo = GetFieldInfo(typeName, fieldName);

            // Get the value
            val = fldInfo.GetValue(this);
        }

        // Gets the field info object given the type name and field name.
        // 
        private FieldInfo GetFieldInfo(string typeName, string fieldName)
        {
            Debug.Assert(typeName != null);
            Debug.Assert(fieldName != null);

            Type t = GetType();
            while (null != t)
            {
                if (t.FullName.Equals(typeName))
                {
                    break;
                }

                t = t.BaseType;
            }

            if (null == t)
            {
                throw new ArgumentException();
            }

            FieldInfo fldInfo = t.GetField(fieldName, BindingFlags.Public |
                                                      BindingFlags.Instance |
                                                      BindingFlags.IgnoreCase);
            if (null == fldInfo)
            {
                throw new ArgumentException();
            }

            return fldInfo;
        }
    }


    // Internal methodtable used to instantiate the "canonical" methodtable for generic instantiations.
    // The name "__Canon" will never been seen by users but it will appear a lot in debugger stack traces
    // involving generics so it is kept deliberately short as to avoid being a nuisance.

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [System.Runtime.InteropServices.ComVisible(true)]
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
