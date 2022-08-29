// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
  File:      GC.cs

  Summary:   Demonstrates how the garbage collector works.

---------------------------------------------------------------------
  This file is part of the Microsoft COM+ 2.0 SDK Code Samples.

  Copyright (C) 2000 Microsoft Corporation.  All rights reserved.

This source code is intended only as a supplement to Microsoft
Development Tools and/or on-line documentation.  See these other
materials for detailed information regarding Microsoft code samples.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.
=====================================================================*/


// Add the classes in the following namespaces to our namespace
using System;
using System.Threading;


///////////////////////////////////////////////////////////////////////////////


// Note that deriving from Object is optional since it is always implied
public class BaseObj : Object {
    private String name;    // Each object has a name to help identify it

    // Note that explicitly calling the base class's constructor is
    // optional since the compiler ensures it anyway.
    // Also note that there is no concept of a destructor.
    public BaseObj(String name) : base() {
        this.name = name;
        Display("BaseObj Constructor");
    }

    public void Display(String status) {
        Application.Display(String.Format("Obj({0}): {1}", name, status));
    }

    // A Finalize method is the closest thing to a destructor but many of the
    // semantics are different. The demos in this application demonstrate this.
    //protected override void Finalize() {
    ~BaseObj() {
        Display("BaseObj Finalize");

        // If possible, do not have a Finalize method for your class. Finalize
        // methods usually run when the heap is low on available storage
        // and needs to be garbage collected. This can hurt application
        // performance significantly.

        // If you must implement a Finalize method, make it run fast, avoid
        // synchronizing on other threads, do not block, and
        // avoid raising any exceptions (although the Finalizer thread
        // automatically recovers from any unhandled exceptions).

        // NOTE: In the future, exceptions may be caught using an
        // AppDomain-registered unhandled Finalize Exception Handler

        // While discouraged, you may call methods on object's referred
        // to by this object. However, you must be aware that the other
        // objects may have already had their Finalize method called
        // causing these objects to be in an unpredictable state.
        // This is because the system does not guarantees that
        // Finalizers will be called in any particular order.
    }
}


///////////////////////////////////////////////////////////////////////////////


// This class shows how to derive a class from another class and how base class
// Finalize methods are NOT automatically called. By contrast, base class
// destructors (in unmanaged code) are automatically called.
// This is one example of how destructors and Finalize methods differ.
public class DerivedObj : BaseObj {
    public DerivedObj(String s) : base(s) {
        Display("DerivedObj Constructor");
    }

    //protected override void Finalize() {
      ~DerivedObj() {
        Display("DerivedObj Finalize");

        // The GC has a special thread dedicated to executing Finalize
        // methods. You can tell that this thread is different from the
        // application's main thread by comparing the thread's hash codes.
        Display("Finalize thread's hash code: "
            + Thread.CurrentThread.GetHashCode());

        // BaseObj's Finalize is NOT called unless you execute the line below
        // base.Finalize();        //commented by vaishak due to breaking change
    }
}


///////////////////////////////////////////////////////////////////////////////


// This class shows how an object can resurrect itself
public class ResurrectObj : BaseObj {

    // Indicates if object should resurrect itself when collected
    private Boolean allowResurrection = true;   // Assume resurrection

    public ResurrectObj(String s) : base(s) {
        Display("ResurrectObj Constructor");
    }

    public void SetResurrection(Boolean allowResurrection) {
        this.allowResurrection = allowResurrection;
    }

    //protected override void Finalize() {
      ~ResurrectObj() {
        Display("ResurrectObj Finalize");
        if (allowResurrection) {
            Display("This object is being resurrected");
            // Resurrect this object by making something refer to it
            Application.ResObjHolder = this;

            // When the GC calls an object's Finalize method, it assumes that
            // there is no need to ever call it again. However, we've now
            // resurrected this object and the line below forces the GC to call
            // this object's Finalize again when the object is destroyed again.
            // BEWARE: If ReRegisterForFinalize is called multiple times, the
            // object's Finalize method will be called multiple times.
            GC.ReRegisterForFinalize(this);

            // If this object contains a member referencing another object,
            // The other object may have been finalized before this object
            // gets resurrected. Note that resurrecting this object forces
            // the referenced object to be resurrected as well. This object
            // can continue to use the referenced object even though it was
            // finalized.

        } else {
            Display("This object is NOT being resurrected");
        }
    }
}


///////////////////////////////////////////////////////////////////////////////


// This class shows how the GC improves performance using generations
public class GenObj : BaseObj {
    public GenObj(String s) : base(s) {
        Display("GenObj Constructor");
    }

    public void DisplayGeneration() {
        Display(String.Format("Generation: {0}", GC.GetGeneration(this)));
    }
}


///////////////////////////////////////////////////////////////////////////////


// This class shows the proper way to implement explicit cleanup.
public class DisposeObj : BaseObj {
    public DisposeObj(String s) : base(s) {
        Display("DisposeObj Constructor");
    }

    // When an object of this type wants to be explicitly cleaned-up, the user
    // of this object should call Dispose at the desired code location.
    public void Dispose() {
        Display("DisposeObj Dispose");
        // Usually Dispose() calls Finalize so that you can
        // implement all the cleanup code in one place.
        // Finalize();    //commented by vaishak due to breaking change

        // Tell the garbage collector that the object doesn't require any
        // cleanup when collected since Dispose was called explicitly.
        GC.SuppressFinalize(this);
    }

    // Put the object cleanup code in the Finalize method
    //protected override void Finalize() {
    ~DisposeObj(){
        Display("DisposeObj Finalize");
        // This function can be called by Dispose() or by the GC
        // If called by Dispose, the application's thread executes this code
        // If called by the GC, then a special GC thread executes this code
    }
}


///////////////////////////////////////////////////////////////////////////////


// This class represents the application itself
class Application {
    static private int indent = 0;

    static public void Display(String s) {
        for (int x = 0; x < indent * 3; x++)
            Console.Write(" ");
        Console.WriteLine(s);
    }

    static public void Display(int preIndent, String s, int postIndent) {
        indent += preIndent;
        Display(s);
        indent += postIndent;
    }

    static public void Collect() {
        Display(0, "Forcing a garbage collection", 0);
        GC.Collect();
    }

    static public void Collect(int generation) {
        Display(0, "Forcing a garbage collection of generation " + generation, 0);
        GC.Collect(generation);
    }

    static public void WaitForFinalizers() {
        Display(0, "Waiting for Finalizers to complete", +1);
        GC.WaitForPendingFinalizers();
        Display(-1, "Finalizers are complete", 0);
    }

    // This method demonstrates how the GC works.
    private static void Introduction() {
        Display(0, "\n\nDemo start: Introduction to Garbage Collection.", +1);

        // Create a new DerivedObj in the managed heap
        // Note: Both BaseObj and DerivedObj constructors are called
        DerivedObj obj = new DerivedObj("Introduction");

        obj = null; // We no longer need this object

        // The object is unreachable so forcing a GC causes it to be finalized.
        Collect();

        // Wait for the GC's Finalize thread to finish
        // executing all queued Finalize methods.
        WaitForFinalizers();
        // NOTE: The GC calls the most-derived (farthest away from
        // the Object base class) Finalize only.
        // Base class Finalize functions are called only if the most-derived
        // Finalize method explicitly calls its base class's Finalize method.

        // This is the same test as above with one slight variation
        obj = new DerivedObj("Introduction");
        // obj = null; // Variation: this line is commented out
        Collect();
        WaitForFinalizers();
        // Notice that we get identical results as above: the Finalize method
        // runs because the jitter's optimizer knows that obj is not
        // referenced later in this function.

        Display(-1, "Demo stop: Introduction to Garbage Collection.", 0);
    }


    // This reference is accessed in the ResurrectObj.Finalize method and
    // is used to create a strong reference to an object (resurrecting it).
    static public ResurrectObj ResObjHolder;    // Defaults to null


    // These methods demonstrate how the GC supports resurrection.
    // NOTE: Resurrection is discouraged.
    private static void ResurrectionInit() {
        // Create a ResurrectionObj
        ResurrectObj obj = new ResurrectObj("Resurrection");

        // Destroy all strong references to the new ResurrectionObj
        obj = null;
    }

    private static void ResurrectionDemo() {
        Display(0, "\n\nDemo start: Object Resurrection.", +1);

        // Create a ResurrectionObj and drop it on the floor.
        ResurrectionInit();

        // Force the GC to determine that the object is unreachable.
        Collect();
        WaitForFinalizers(); // You should see the Finalize method called.

        // However, the ResurrectionObj's Finalize method
        // resurrects the object keeping it alive. It does this by placing a
        // reference to the dying-object in Application.ResObjHolder

        // You can see that ResurrectionObj still exists because
        // the following line doesn't raise an exception.
        ResObjHolder.Display("Still alive after Finalize called");

        // Prevent the ResurrectionObj object from resurrecting itself again,
        ResObjHolder.SetResurrection(false);

        // Now, let's destroy this last reference to the ResurrectionObj
        ResObjHolder = null;

        // Force the GC to determine that the object is unreachable.
        Collect();
        WaitForFinalizers(); // You should see the Finalize method called.
        Display(-1, "Demo stop: Object Resurrection.", 0);
    }


    // This method demonstrates how to implement a type that allows its users
    // to explicitly dispose/close the object. For many object's this paradigm
    // is strongly encouranged.
    private static void DisposeDemo() {
        Display(0, "\n\nDemo start: Disposing an object versus Finalize.", +1);
        DisposeObj obj = new DisposeObj("Explicitly disposed");
        obj.Dispose();  // Explicitly cleanup this object, Finalize should run
        obj = null;
        Collect();
        WaitForFinalizers(); // Finalize should NOT run (it was suppressed)

        obj = new DisposeObj("Implicitly disposed");
        obj = null;
        Collect();
        WaitForFinalizers(); // No explicit cleanup, Finalize SHOULD run
        Display(-1, "Demo stop: Disposing an object versus Finalize.", 0);
    }


    // This method demonstrates the unbalanced nature of ReRegisterForFinalize
    // and SuppressFinalize. The main point is if your code makes multiple
    // calls to ReRegisterForFinalize (without intervening calls to
    // SuppressFinalize) the Finalize method may get called multiple times.
    private static void FinalizationQDemo() {
        Display(0, "\n\nDemo start: Suppressing and ReRegistering for Finalize.", +1);
        // Since this object has a Finalize method, a reference to the object
        // will be added to the finalization queue.
        BaseObj obj = new BaseObj("Finalization Queue");

        // Add another 2 references onto the finalization queue
        // NOTE: Don't do this in a normal app. This is only for demo purposes.
        GC.ReRegisterForFinalize(obj);
        GC.ReRegisterForFinalize(obj);

        // There are now 3 references to this object on the finalization queue.

        // Set a bit flag on this object indicating that it should NOT be finalized.
        GC.SuppressFinalize(obj);

        // There are now 3 references to this object on the finalization queue.
        // If the object were unreachable, the 1st call to this object's Finalize
        // method will be discarded but the 2nd & 3rd calls to Finalize will execute.

        // Sets the same bit effectively doing nothing!
        GC.SuppressFinalize(obj);

        obj = null;   // Remove the strong reference to the object.

        // Force a GC so that the object gets finalized
        Collect();
        // NOTE: Finalize is called twice because only the 1st call is suppressed!
        WaitForFinalizers();
        Display(-1, "Demo stop: Suppressing and ReRegistering for Finalize.", 0);
    }


    // This method demonstrates how objects are promoted between generations.
    // Applications could take advantage of this info to improve performance
    // but most applications will ignore this information.
    private static void GenerationDemo() {
        Display(0, "\n\nDemo start: Understanding Generations.", +1);

        // Let's see how many generations the managed heap supports (we know it's 2)
        Display("Maximum GC generations: " + GC.MaxGeneration);

        // Create a new BaseObj in the heap
        GenObj obj = new GenObj("Generation");

        // Since this object is newly created, it should be in generation 0
        obj.DisplayGeneration();    // Displays 0

        // Performing a GC promotes the object's generation
        Collect();
        obj.DisplayGeneration();    // Displays 1

        Collect();
        obj.DisplayGeneration();    // Displays 2

        Collect();
        obj.DisplayGeneration();    // Displays 2   (max generation)

        obj = null;             // Destroy the strong reference to this object

        Collect(0);             // Collect objects in generation 0
        WaitForFinalizers();    // We should see nothing

        Collect(1);             // Collect objects in generation 1
        WaitForFinalizers();    // We should see nothing

        Collect(2);             // Same as Collect()
        WaitForFinalizers();    // Now, we should see the Finalize method run

        Display(-1, "Demo stop: Understanding Generations.", 0);
    }


    // This method demonstrates how weak references (WR) work. A WR allows
    // the GC to collect objects if the managed heap is low on memory.
    // WRs are useful to apps that have large amounts of easily-reconstructed
    // data that they want to keep around to improve performance. But, if the
    // system is low on memory, the objects can be destroyed and replaced when
    // the app knows that it needs it again.
    private static void WeakRefDemo(Boolean trackResurrection) {
        Display(0, String.Format(
            "\n\nDemo start: WeakReferences that {0}track resurrections.",
            trackResurrection ? "" : "do not "), +1);

        // Create an object
        BaseObj obj = new BaseObj("WeakRef");

        // Create a WeakReference object that refers to the new object
        WeakReference wr = new WeakReference(obj, trackResurrection);

        // The object is still reachable, so it is not finalized.
        Collect();
        WaitForFinalizers(); // The Finalize method should NOT execute
        obj.Display("Still exists");

        // Let's remove the strong reference to the object
        obj = null;     // Destroy strong reference to this object

        // The following line creates a strong reference to the object
        obj = (BaseObj) wr.Target;
        Display("Strong reference to object obtained: " + (obj != null));

        obj = null;     // Destroy strong reference to this object again.

        // The GC considers the object to be unreachable and collects it.
        Collect();
        WaitForFinalizers();    // Finalize should run.

        // This object resurrects itself when its Finalize method is called.
        // If wr is NOT tracking resurrection, wr thinks the object is dead
        // If wr is tracking resurrection, wr thinks the object is still alive

        // NOTE: If the object referred to by wr doesn't have a Finalize method,
        // then wr would think that the object is dead regardless of whether
        // wr is tracking resurrection or not. For example:
        //    Object obj = new Object();   // Object doesn't have a Finalize method
        //    WeakReference wr = new WeakReference(obj, true);
        //    obj = null;
        //    Collect();
        //    WaitForFinalizers();       // Does nothing
        //    obj = (Object) wr.Target;  // returns null

        // The following line attempts to create a strong reference to the object
        obj = (BaseObj) wr.Target;
        Display("Strong reference to object obtained: " + (obj != null));

        if (obj != null) {
            // The strong reference was obtained so this wr must be
            // tracking resurrection. At this point we have a strong
            // reference to an object that has been finalized but its memory
            // has not yet been reclaimed by the collector.
            obj.Display("See, I'm still alive");

            obj = null; // Destroy the strong reference to the object

            // Collect reclaims the object's memory since this object
            // has no Finalize method registered for it anymore.
            Collect();
            WaitForFinalizers();    // We should see nothing here

            obj = (BaseObj) wr.Target;  // This now returns null
            Display("Strong reference to object obtained: " + (obj != null));
        }

        // Cleanup everything about this demo so there is no affect on the next demo
        obj = null;           // Destroy strong reference (if it exists)
        wr = null;            // Destroy the WeakReference object (optional)
        Collect();
        WaitForFinalizers();

        // NOTE: You are dicouraged from using the WeakReference.IsAlive property
        // because the object may be killed immediately after IsAlive returns
        // making the return value incorrect. If the Target property returns
        // a non-null value, then the object is alive and will stay alive
        // since you have a reference to it. If Target returns null, then the
        // object is dead.
        Display(-1, String.Format("Demo stop: WeakReferences that {0}track resurrections.",
            trackResurrection ? "" : "do not "), 0);
    }


    public static int Main(String[] args) {
    // Environment.ExitCode = 1;
        Display("To fully understand this sample, you should step through the");
        Display("code in the debugger while monitoring the output generated.\n");
        Display("NOTE: The demos in this application assume that no garbage");
        Display("      collections occur naturally. To ensure this, the sample");
        Display("      objects are small in size and few are allocated.\n");
        Display("Main thread's hash code: " + Thread.CurrentThread.GetHashCode());

        Introduction();      // GC introduction
        ResurrectionDemo();  // Demos object resurrection
        DisposeDemo();       // Demos the use of Dispose & Finalize
        FinalizationQDemo(); // Demos the use of SuppressFinalize & ReRegisterForFinalize
        GenerationDemo();    // Demos GC generations
        WeakRefDemo(false);  // Demos WeakReferences without resurrection tracking
        WeakRefDemo(true);   // Demos WeakReferences with resurrection tracking

        // Demos Finalize on Shutdown semantics (this demo is inline)
        Display(0, "\n\nDemo start: Finalize on shutdown.", +1);

        // When Main returns, obj will have its Finalize method called.
        BaseObj obj = new BaseObj("Shutdown");

        // This is the last line of code executed before the application terminates.
        Display(-1, "Demo stop: Finalize on shutdown (application is now terminating)", 0);

    return 100;
    }
}


///////////////////////////////// End of File /////////////////////////////////
