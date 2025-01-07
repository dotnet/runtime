// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

//
// Implements the single finalizer thread for a Redhawk instance. Essentially waits for an event to fire
// indicating finalization is necessary then drains the queue of pending finalizable objects, calling the
// finalize method for each one.
//

namespace System.Runtime
{
    // We choose this name to avoid clashing with any future public class with the name Finalizer.
    internal static class __Finalizer
    {
        [UnmanagedCallersOnly(EntryPoint = "ProcessFinalizers")]
        public static void ProcessFinalizers()
        {
#if INPLACE_RUNTIME
            System.Runtime.FinalizerInitRunner.DoInitialize();
#endif

            while (true)
            {
                // Wait until there's some work to be done. If true is returned we should finalize objects,
                // otherwise memory is low and we should initiate a collection.
                if (InternalCalls.RhpWaitForFinalizerRequest() != 0)
                {
                    int observedFullGcCount = RuntimeImports.RhGetGcCollectionCount(RuntimeImports.RhGetMaxGcGeneration(), false);
                    uint finalizerCount = DrainQueue();

                    // Anyone waiting to drain the Q can now wake up.  Note that there is a
                    // race in that another thread starting a drain, as we leave a drain, may
                    // consider itself satisfied by the drain that just completed.
                    // Thus we include the Full GC count that we have certaily observed.
                    InternalCalls.RhpSignalFinalizationComplete(finalizerCount, observedFullGcCount);
                }
                else
                {
                    // RhpWaitForFinalizerRequest() returned false and indicated that memory is low. We help
                    // out by initiating a garbage collection and then go back to waiting for another request.
                    InternalCalls.RhCollect(0, InternalGCCollectionMode.Blocking, lowMemoryP: true);
                }
            }
        }

        // Do not inline this method -- we do not want to accidentally have any temps in ProcessFinalizers which contain
        // objects that came off of the finalizer queue.  If such temps were reported across the duration of the
        // finalizer thread wait operation, it could cause unpredictable behavior with weak handles.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe uint DrainQueue()
        {
            uint finalizerCount = 0;
            // Drain the queue of finalizable objects.
            while (true)
            {
                object target = InternalCalls.RhpGetNextFinalizableObject();
                if (target == null)
                    return finalizerCount;

                finalizerCount++;

                try
                {
                    // Call the finalizer on the current target object.
                    ((delegate*<object, void>)target.GetMethodTable()->FinalizerCode)(target);
                }
                catch (Exception ex) when (ExceptionHandling.IsHandledByGlobalHandler(ex))
                {
                    // the handler returned "true" means the exception is now "handled" and we should continue.
                }
            }
        }
    }
}
