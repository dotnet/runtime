// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Interface:  IDisposable
**
**
** Purpose: Interface for assisting with deterministic finalization.
**
** 
===========================================================*/

namespace System
{
    // IDisposable is an attempt at helping to solve problems with deterministic
    // finalization.  The GC of course doesn't leave any way to deterministically
    // know when a finalizer will run.  This forces classes that hold onto OS
    // resources or some sort of important state (such as a FileStream or a 
    // network connection) to provide a Close or Dispose method so users can 
    // run clean up code deterministically.  We have formalized this into an 
    // interface with one method.  Classes may privately implement IDisposable and
    // provide a Close method instead, if that name is by far the expected name
    // for objects in that domain (ie, you don't Dispose of a FileStream, you Close
    // it).
    //
    // This interface could be theoretically used as a marker by a compiler to 
    // ensure a disposable object has been cleaned up along all code paths if it's 
    // been allocated in that method, though in practice any compiler that 
    // draconian may tick off any number of people.  Perhaps an external tool (like
    // like Purify or BoundsChecker) could do this.  Instead, C# has added a using 
    // clause, which will generate a try/finally statement where the resource 
    // passed into the using clause will always have it's Dispose method called.  
    // Syntax is using(FileStream fs = ...) { .. };
    //
    // Dispose should meet the following conditions:
    // 1) Be safely callable multiple times
    // 2) Release any resources associated with the instance
    // 3) Call the base class's Dispose method, if necessary
    // 4) Suppress finalization of this class to help the GC by reducing the
    //    number of objects on the finalization queue.
    // 5) Dispose shouldn't generally throw exceptions, except for very serious 
    //    errors that are particularly unexpected. (ie, OutOfMemoryException)  
    //    Ideally, nothing should go wrong with your object by calling Dispose.
    //
    // If possible, a class should define a finalizer that calls Dispose.
    // However, in many situations, this is impractical.  For instance, take the
    // classic example of a Stream and a StreamWriter (which has an internal 
    // buffer of data to write to the Stream).  If both objects are collected 
    // before Close or Dispose has been called on either, then the GC may run the
    // finalizer for the Stream first, before the StreamWriter.  At that point, any
    // data buffered by the StreamWriter cannot be written to the Stream.  In this
    // case, it doesn't make much sense to provide a finalizer on the StreamWriter
    // since you cannot solve this problem correctly.  
    public interface IDisposable
    {
        void Dispose();
    }
}
