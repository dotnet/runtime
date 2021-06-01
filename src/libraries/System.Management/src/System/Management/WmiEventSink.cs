// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Management
{

    internal class WmiEventSink : IWmiEventSource
    {
        private static int s_hash;
        private readonly int hash;
        private readonly ManagementOperationObserver watcher;
        private readonly object context;
        private readonly ManagementScope scope;
        private object stub;           // The secured IWbemObjectSink

        // Used for Put's only
        internal event InternalObjectPutEventHandler InternalObjectPut;
        private readonly ManagementPath path;
        private readonly string className;
        private readonly bool isLocal;


        private static ManagementOperationObserver watcherParameter;
        private static object contextParameter;
        private static ManagementScope scopeParameter;
        private static string pathParameter;
        private static string classNameParameter;
        private static WmiEventSink wmiEventSinkNew;

        internal static WmiEventSink GetWmiEventSink(
            ManagementOperationObserver watcher,
            object context,
            ManagementScope scope,
            string path,
            string className)
        {
            if (MTAHelper.IsNoContextMTA())
                return new WmiEventSink(watcher, context, scope, path, className);

            watcherParameter = watcher;
            contextParameter = context;
            scopeParameter = scope;
            pathParameter = path;
            classNameParameter = className;

            //
            // Ensure we are able to trap exceptions from worker thread.
            //
            ThreadDispatch disp = new ThreadDispatch(new ThreadDispatch.ThreadWorkerMethod(HackToCreateWmiEventSink));
            disp.Start();

            return wmiEventSinkNew;
        }

        private static void HackToCreateWmiEventSink()
        {
            wmiEventSinkNew = new WmiEventSink(watcherParameter, contextParameter, scopeParameter, pathParameter, classNameParameter);
        }

        protected WmiEventSink(ManagementOperationObserver watcher,
                             object context,
                             ManagementScope scope,
                             string path,
                             string className)
        {
            try
            {
                this.context = context;
                this.watcher = watcher;
                this.className = className;
                this.isLocal = false;

                if (null != path)
                {
                    this.path = new ManagementPath(path);
                    if ((0 == string.Compare(this.path.Server, ".", StringComparison.OrdinalIgnoreCase)) ||
                        (0 == string.Compare(this.path.Server, System.Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
                    {
                        this.isLocal = true;
                    }
                }

                if (null != scope)
                {
                    this.scope = (ManagementScope)scope.Clone();
                    if (null == path) // use scope to see if sink is local
                    {
                        if ((0 == string.Compare(this.scope.Path.Server, ".", StringComparison.OrdinalIgnoreCase)) ||
                            (0 == string.Compare(this.scope.Path.Server, System.Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
                        {
                            this.isLocal = true;
                        }
                    }
                }

                WmiNetUtilsHelper.GetDemultiplexedStub_f(this, this.isLocal, out stub);
                hash = Threading.Interlocked.Increment(ref s_hash);
            }
            catch { }
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public IWbemObjectSink Stub
        {
            get
            {
                try
                {
                    return (null != stub) ? (IWbemObjectSink)stub : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        public virtual void Indicate(IntPtr pIWbemClassObject)
        {
            Marshal.AddRef(pIWbemClassObject);
            IWbemClassObjectFreeThreaded obj = new IWbemClassObjectFreeThreaded(pIWbemClassObject);
            try
            {
                ObjectReadyEventArgs args = new ObjectReadyEventArgs(context,
                                            ManagementBaseObject.GetBaseObject(obj, scope));
                watcher.FireObjectReady(args);
            }
            catch { }
        }

        public void SetStatus(
                        int flags,
                        int hResult,
                        string message,
                        IntPtr pErrorObj)
        {
            IWbemClassObjectFreeThreaded errObj = null;
            if (pErrorObj != IntPtr.Zero)
            {
                Marshal.AddRef(pErrorObj);
                errObj = new IWbemClassObjectFreeThreaded(pErrorObj);
            }

            try
            {
                if (flags == (int)tag_WBEM_STATUS_TYPE.WBEM_STATUS_COMPLETE)
                {
                    // Is this a Put? If so fire the ObjectPut event
                    if (null != path)
                    {
                        if (null == className)
                            path.RelativePath = message;
                        else
                            path.RelativePath = className;

                        // Fire the internal event (if anyone is interested)
                        if (null != InternalObjectPut)
                        {
                            try
                            {
                                InternalObjectPutEventArgs iargs = new InternalObjectPutEventArgs(path);
                                InternalObjectPut(this, iargs);
                            }
                            catch { }
                        }

                        ObjectPutEventArgs args = new ObjectPutEventArgs(context, path);
                        watcher.FireObjectPut(args);
                    }

                    // Fire Completed event
                    CompletedEventArgs args2 = null;
                    if (errObj != null)
                    {
                        args2 = new CompletedEventArgs(context, hResult,
                                                new ManagementBaseObject(errObj)
                                                );
                    }
                    else
                    {
                        args2 = new CompletedEventArgs(context, hResult,
                                                null
                                                );
                    }
                    watcher.FireCompleted(args2);

                    // Unhook and tidy up
                    watcher.RemoveSink(this);
                }
                else if (0 != (flags & (int)tag_WBEM_STATUS_TYPE.WBEM_STATUS_PROGRESS))
                {
                    // Fire Progress event
                    ProgressEventArgs args = new ProgressEventArgs(context,
                        (int)(((uint)hResult & 0xFFFF0000) >> 16), hResult & 0xFFFF, message);

                    watcher.FireProgress(args);
                }
            }
            catch { }
        }

        internal void Cancel()
        {
            try
            {
                scope.GetIWbemServices().CancelAsyncCall_((IWbemObjectSink)stub);
            }
            catch { }
        }

        internal void ReleaseStub()
        {
            try
            {
                /*
                 * We force a release of the stub here so as to allow
                 * unsecapp.exe to die as soon as possible.
                 */
                if (null != stub)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(stub);
                    stub = null;
                }
            }
            catch { }
        }

    }

    // Special sink implementation for ManagementObject.Get
    // Doesn't issue ObjectReady events
    internal sealed class WmiGetEventSink : WmiEventSink
    {
        private readonly ManagementObject managementObject;

        private static ManagementOperationObserver watcherParameter;
        private static object contextParameter;
        private static ManagementScope scopeParameter;
        private static ManagementObject managementObjectParameter;

        private static WmiGetEventSink wmiGetEventSinkNew;

        internal static WmiGetEventSink GetWmiGetEventSink(
            ManagementOperationObserver watcher,
            object context,
            ManagementScope scope,
            ManagementObject managementObject)
        {
            if (MTAHelper.IsNoContextMTA())
                return new WmiGetEventSink(watcher, context, scope, managementObject);

            watcherParameter = watcher;
            contextParameter = context;
            scopeParameter = scope;
            managementObjectParameter = managementObject;

            //
            // Ensure we are able to trap exceptions from worker thread.
            //
            ThreadDispatch disp = new ThreadDispatch(new ThreadDispatch.ThreadWorkerMethod(HackToCreateWmiGetEventSink));
            disp.Start();

            return wmiGetEventSinkNew;
        }

        private static void HackToCreateWmiGetEventSink()
        {
            wmiGetEventSinkNew = new WmiGetEventSink(watcherParameter, contextParameter, scopeParameter, managementObjectParameter);
        }


        private WmiGetEventSink(ManagementOperationObserver watcher,
                             object context,
                             ManagementScope scope,
                             ManagementObject managementObject) :
            base(watcher, context, scope, null, null)
        {
            this.managementObject = managementObject;
        }

        public override void Indicate(IntPtr pIWbemClassObject)
        {
            Marshal.AddRef(pIWbemClassObject);
            IWbemClassObjectFreeThreaded obj = new IWbemClassObjectFreeThreaded(pIWbemClassObject);
            if (null != managementObject)
            {
                try
                {
                    managementObject.wbemObject = obj;
                }
                catch { }
            }
        }
    }



}
