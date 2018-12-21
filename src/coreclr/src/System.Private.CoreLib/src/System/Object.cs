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
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ComVisible(true)]
    public partial class Object
    {
        // Returns a Type object which represent this object instance.
        // 
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern Type GetType();

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
