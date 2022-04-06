// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class Runtime
    {
        private static object JSOwnedObjectLock = new object();
        // we use this to maintain identity of GCHandle for a managed object
        private static Dictionary<object, IntPtr> GCHandleFromJSOwnedObject = new Dictionary<object, IntPtr>(ReferenceEqualityComparer.Instance);


        public static void GetJSOwnedObjectByGCHandleRef(int gcHandle, out object result)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            result = h.Target!;
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public static IntPtr GetJSOwnedObjectGCHandleRef(in object obj)
        {
            if (obj == null)
                return IntPtr.Zero;

            IntPtr result;
            lock (JSOwnedObjectLock)
            {
                if (GCHandleFromJSOwnedObject.TryGetValue(obj, out result))
                    return result;

                result = (IntPtr)GCHandle.Alloc(obj, GCHandleType.Normal);
                GCHandleFromJSOwnedObject[obj] = result;
                return result;
            }
        }

        // The JS layer invokes this method when the JS wrapper for a JS owned object
        //  has been collected by the JS garbage collector
        public static void ReleaseJSOwnedObjectByGCHandle(int gcHandle)
        {
            GCHandle handle = (GCHandle)(IntPtr)gcHandle;
            lock (JSOwnedObjectLock)
            {
                GCHandleFromJSOwnedObject.Remove(handle.Target!);
                handle.Free();
            }
        }

        public static IntPtr CreateTaskSource()
        {
            var tcs = new TaskCompletionSource<object>();
            return GetJSOwnedObjectGCHandleRef(tcs);
        }

        public static void SetTaskSourceResultRef(int tcsGCHandle, in object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetResult(result);
        }

        public static void SetTaskSourceFailure(int tcsGCHandle, string reason)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            tcs.SetException(new JSException(reason));
        }

        public static void GetTaskSourceTaskRef(int tcsGCHandle, out object result)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            result = tcs.Task;
        }

        public static void TaskFromResultRef(in object? obj, out object result)
        {
            result = Task.FromResult(obj);
        }

        public static void SetupJSContinuationRef(in Task _task, JSObject continuationObj)
        {
            // HACK: Attempting to use the in-param will produce CS1628, so we make a temporary copy
            //  on the stack that can be captured by our local functions below
            var task = _task;

            if (task.IsCompleted)
                Complete();
            else
                task.GetAwaiter().OnCompleted(Complete);

            void Complete()
            {
                try
                {
                    if (task.Exception == null)
                    {
                        object? result;
                        Type task_type = task.GetType();
                        if (task_type == typeof(Task))
                        {
                            result = System.Array.Empty<object>();
                        }
                        else
                        {
                            result = GetTaskResultMethodInfo(task_type)?.Invoke(task, null);
                        }

                        continuationObj.Invoke("resolve", result);
                    }
                    else
                    {
                        continuationObj.Invoke("reject", task.Exception.ToString());
                    }
                }
                catch (Exception e)
                {
                    continuationObj.Invoke("reject", e.ToString());
                }
                finally
                {
                    continuationObj.Dispose();
                }
            }
        }
    }
}
