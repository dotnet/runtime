// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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
        [UnmanagedCallersOnly(EntryPoint = "ProcessFinalizers", CallConvs = new Type[] { typeof(CallConvCdecl) })]
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
                    DrainQueue();

                    // Tell anybody that's interested that the finalization pass is complete (there is a race condition here
                    // where we might immediately signal a new request as complete, but this is acceptable).
                    InternalCalls.RhpSignalFinalizationComplete();
                }
                else
                {
                    // RhpWaitForFinalizerRequest() returned false and indicated that memory is low. We help
                    // out by initiating a garbage collection and then go back to waiting for another request.
                    InternalCalls.RhCollect(-1, InternalGCCollectionMode.Blocking);
                }
            }
        }

        // Do not inline this method -- we do not want to accidentally have any temps in ProcessFinalizers which contain
        // objects that came off of the finalizer queue.  If such temps were reported across the duration of the
        // finalizer thread wait operation, it could cause unpredictable behavior with weak handles.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void DrainQueue()
        {
            // Drain the queue of finalizable objects.
            while (true)
            {
                object target = InternalCalls.RhpGetNextFinalizableObject();
                if (target == null)
                    return;

                // Call the finalizer on the current target object. If the finalizer throws we'll fail
                // fast via normal Redhawk exception semantics (since we don't attempt to catch
                // anything).
                ((delegate*<object, void>)target.GetMethodTable()->FinalizerCode)(target);
            }
        }
    }
}
