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


        public static object GetJSOwnedObjectByGCHandle(int gcHandle)
        {
            GCHandle h = (GCHandle)(IntPtr)gcHandle;
            return h.Target!;
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public static IntPtr GetJSOwnedObjectGCHandle(object obj)
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
            return GetJSOwnedObjectGCHandle(tcs);
        }

        public static void SetTaskSourceResult(int tcsGCHandle, object result)
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

        public static object GetTaskSourceTask(int tcsGCHandle)
        {
            GCHandle handle = (GCHandle)(IntPtr)tcsGCHandle;
            // this is JS owned Normal handle. We always have a Target
            TaskCompletionSource<object> tcs = (TaskCompletionSource<object>)handle.Target!;
            return tcs.Task;
        }

        public static object TaskFromResult(object? obj)
        {
            return Task.FromResult(obj);
        }

        public static void SetupJSContinuation(Task task, JSObject continuationObj)
        {
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
