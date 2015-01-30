// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
** This interface is implemented by classes that support cloning.
**
===========================================================*/
namespace System {
    
    using System;
    // Defines an interface indicating that an object may be cloned.  Only objects 
    // that implement ICloneable may be cloned. The interface defines a single 
    // method which is called to create a clone of the object.   Object defines a method
    // MemberwiseClone to support default clone operations.
    // 
    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICloneable
    {
        // Interface does not need to be marked with the serializable attribute
        // Make a new object which is a copy of the object instanced.  This object may be either
        // deep copy or a shallow copy depending on the implementation of clone.  The default
        // Object support for clone does a shallow copy.
        // 
        Object Clone();
    }
}
