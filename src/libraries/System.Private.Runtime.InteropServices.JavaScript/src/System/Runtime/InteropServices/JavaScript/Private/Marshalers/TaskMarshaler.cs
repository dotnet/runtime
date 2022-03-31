// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class TaskMarshaler : JavaScriptMarshalerBase<Task>
    {
        protected override string JavaScriptCode => null;
        protected override MarshalToManagedDelegate<Task> ToManaged => JavaScriptMarshal.MarshalToManagedTask;
        protected override MarshalToJavaScriptDelegate<Task> ToJavaScript => JavaScriptMarshal.MarshalTaskToJs;
        protected override MarshalToJavaScriptDelegate<Task> AfterToJavaScript => JavaScriptMarshal.MarshalTaskToJs;
    }
}

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Task MarshalToManagedTask(JavaScriptMarshalerArg arg)
        {
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            /*if (arg.TypeHandle == JavaScriptMarshalImpl.taskType)
            {
                // this is managed Task round-trip
                var t=((GCHandle)arg.GCHandle);
                return (Task)t.Target;
            }
            */
            Debug.Assert(arg.TypeHandle == JavaScriptMarshalImpl.ijsObjectType);

            IntPtr jSHandle = arg.JSHandle;
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>(jSHandle);
            lock (JavaScriptMarshalImpl._jsHandleToTaskCompletionSource)
            {
                JavaScriptMarshalImpl._jsHandleToTaskCompletionSource.Add(jSHandle, tcs);
            }
            return tcs.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalTaskToJs(ref Task value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
                return;
            }
            /*if (value.AsyncState is IntPtr jsHandle)
            {
                // This is Promise round-trip
                arg.TypeHandle = JavaScriptMarshalImpl.ijsObjectType;
                arg.JSHandle = jsHandle;
            }
            else
            {*/
            var gcHandle = (IntPtr)GCHandle.Alloc(value, GCHandleType.Normal);
            arg.TypeHandle = JavaScriptMarshalImpl.taskType;
            arg.GCHandle = gcHandle;
            //}
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AfterMarshalTaskToJs(ref Task value, JavaScriptMarshalerArg arg)
        {
            var task = value;
            if (arg.TypeHandle != JavaScriptMarshalImpl.taskType)
            {
                return;
            }
            // we know that task instance is still alive
            // we also know that gcHandle is valid, until Complete() was called
            // because we need to make sure that GCHandle is not dealocated before Complete()
            // un-completed Tasks leak resources
            var gcHandle = arg.GCHandle;

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
                        object result;
                        Type task_type = task.GetType();
                        if (task_type == typeof(Task) || task == Task.CompletedTask)
                        {
                            result = null;
                        }
                        else
                        {
                            // TODO, is this reflection slow ?
                            result = Runtime.GetTaskResultMethodInfo(task_type)?.Invoke(task, null);
                        }

                        JavaScriptMarshalImpl._ResolveTask(gcHandle, result);
                    }
                    else
                    {
                        if (task.Exception is AggregateException ae && ae.InnerExceptions.Count == 1)
                        {
                            JavaScriptMarshalImpl._RejectTask(gcHandle, ae.InnerExceptions[0]);
                        }
                        else
                        {
                            JavaScriptMarshalImpl._RejectTask(gcHandle, task.Exception);
                        }
                    }
                    GCHandle gch = (GCHandle)gcHandle;
                    gch.Free();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Should not happen", ex);
                }
            }
        }
    }
}
