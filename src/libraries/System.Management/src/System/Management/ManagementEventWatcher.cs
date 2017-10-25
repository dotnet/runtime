// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace System.Management
{
    /// <summary>
    /// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementEventWatcher.EventArrived'/> event.</para>
    /// </summary>
    public delegate void EventArrivedEventHandler(object sender, EventArrivedEventArgs e);

    /// <summary>
    /// <para>Represents the method that will handle the <see cref='E:System.Management.ManagementEventWatcher.Stopped'/> event.</para>
    /// </summary>
    public delegate void StoppedEventHandler (object sender, StoppedEventArgs e);

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC// 
    /// <summary>
    ///    <para> Subscribes to temporary event notifications
    ///       based on a specified event query.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System; 
    /// using System.Management; 
    /// 
    /// // This example demonstrates how to subscribe to an event using the ManagementEventWatcher object. 
    /// class Sample_ManagementEventWatcher 
    /// { 
    ///     public static int Main(string[] args) { 
    /// 
    ///         //For the example, we'll put a class into the repository, and watch
    ///         //for class deletion events when the class is deleted.
    ///         ManagementClass newClass = new ManagementClass(); 
    ///         newClass["__CLASS"] = "TestDeletionClass";
    ///         newClass.Put();
    /// 
    ///         //Set up an event watcher and a handler for the event
    ///         ManagementEventWatcher watcher = new ManagementEventWatcher(
    ///             new WqlEventQuery("__ClassDeletionEvent"));
    ///         MyHandler handler = new MyHandler();
    ///         watcher.EventArrived += new EventArrivedEventHandler(handler.Arrived);
    /// 
    ///         //Start watching for events
    ///         watcher.Start();
    ///       
    ///         // For the purpose of this sample, we delete the class to trigger the event
    ///         // and wait for two seconds before terminating the consumer
    ///         newClass.Delete();
    ///        
    ///         System.Threading.Thread.Sleep(2000);
    ///  
    ///         //Stop watching
    ///         watcher.Stop();
    ///        
    ///         return 0;
    ///     }
    ///  
    ///     public class MyHandler {
    ///         public void Arrived(object sender, EventArrivedEventArgs e) {
    ///             Console.WriteLine("Class Deleted = " +
    ///                ((ManagementBaseObject)e.NewEvent["TargetClass"])["__CLASS"]);
    ///         }
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    /// 
    /// ' This example demonstrates how to subscribe an event using the ManagementEventWatcher object.
    /// Class Sample_ManagementEventWatcher
    ///     Public Shared Sub Main()
    /// 
    ///         ' For the example, we'll put a class into the repository, and watch
    ///         ' for class deletion events when the class is deleted.
    ///         Dim newClass As New ManagementClass()
    ///         newClass("__CLASS") = "TestDeletionClass"
    ///         newClass.Put()
    ///         
    ///         ' Set up an event watcher and a handler for the event
    ///         Dim watcher As _
    ///             New ManagementEventWatcher(New WqlEventQuery("__ClassDeletionEvent"))
    ///         Dim handler As New MyHandler()
    ///         AddHandler watcher.EventArrived, AddressOf handler.Arrived
    ///  
    ///         ' Start watching for events
    ///         watcher.Start()
    /// 
    ///         ' For the purpose of this sample, we delete the class to trigger the event
    ///         ' and wait for two seconds before terminating the consumer      
    ///         newClass.Delete()
    /// 
    ///         System.Threading.Thread.Sleep(2000)
    ///   
    ///         ' Stop watching
    ///         watcher.Stop()
    /// 
    ///     End Sub
    /// 
    ///     Public Class MyHandler
    ///         Public Sub Arrived(sender As Object, e As EventArrivedEventArgs)
    ///             Console.WriteLine("Class Deleted = " &amp; _
    ///                 CType(e.NewEvent("TargetClass"), ManagementBaseObject)("__CLASS"))
    ///         End Sub
    ///     End Class
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    [ToolboxItem(false)]
    public class ManagementEventWatcher : Component
    {
        //fields
        private ManagementScope         scope;
        private EventQuery              query;
        private EventWatcherOptions     options;
        private IEnumWbemClassObject    enumWbem;
        private IWbemClassObjectFreeThreaded[]      cachedObjects; //points to objects currently available in cache
        private uint                    cachedCount; //says how many objects are in the cache (when using BlockSize option)
        private uint                    cacheIndex; //used to walk the cache
        private SinkForEventQuery       sink; // the sink implementation for event queries
        private WmiDelegateInvoker      delegateInvoker; 
        
        //Called when IdentifierChanged() event fires
        private void HandleIdentifierChange(object sender, 
            IdentifierChangedEventArgs e)
        {
            // Invalidate any sync or async call in progress
            Stop();
        }

        //default constructor
        /// <overload>
        ///    Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class.
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class. For further
        ///    initialization, set the properties on the object. This is the default constructor.</para>
        /// </summary>
        public ManagementEventWatcher() : this((ManagementScope)null, null, null) {}

        //parameterized constructors
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class when given a WMI event query.</para>
        /// </summary>
        /// <param name='query'>An <see cref='System.Management.EventQuery'/> object representing a WMI event query, which determines the events for which the watcher will listen.</param>
        /// <remarks>
        ///    <para>The namespace in which the watcher will be listening for
        ///       events is the default namespace that is currently set.</para>
        /// </remarks>
        public ManagementEventWatcher (
            EventQuery query) : this(null, query, null) {}

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class when given a WMI event query in the 
        ///    form of a string.</para>
        /// </summary>
        /// <param name='query'> A WMI event query, which defines the events for which the watcher will listen.</param>
        /// <remarks>
        ///    <para>The namespace in which the watcher will be listening for
        ///       events is the default namespace that is currently set.</para>
        /// </remarks>
        public ManagementEventWatcher (
            string query) : this(null, new EventQuery(query), null) {}

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> 
        /// class that listens for events conforming to the given WMI event query.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> object representing the scope (namespace) in which the watcher will listen for events.</param>
        /// <param name=' query'>An <see cref='System.Management.EventQuery'/> object representing a WMI event query, which determines the events for which the watcher will listen.</param>
        public ManagementEventWatcher(
            ManagementScope scope, 
            EventQuery query) : this(scope, query, null) {}

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> 
        /// class that listens for events conforming to the given WMI event query. For this
        /// variant, the query and the scope are specified as strings.</para>
        /// </summary>
        /// <param name='scope'> The management scope (namespace) in which the watcher will listen for events.</param>
        /// <param name=' query'> The query that defines the events for which the watcher will listen.</param>
        public ManagementEventWatcher(
            string scope, 
            string query) : this(new ManagementScope(scope), new EventQuery(query), null) {}

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class that listens for 
        ///    events conforming to the given WMI event query, according to the specified options. For
        ///    this variant, the query and the scope are specified as strings. The options
        ///    object can specify options such as a timeout and context information.</para>
        /// </summary>
        /// <param name='scope'>The management scope (namespace) in which the watcher will listen for events.</param>
        /// <param name=' query'>The query that defines the events for which the watcher will listen.</param>
        /// <param name='options'>An <see cref='System.Management.EventWatcherOptions'/> object representing additional options used to watch for events. </param>
        public ManagementEventWatcher(
            string scope,
            string query,
            EventWatcherOptions options) : this(new ManagementScope(scope), new EventQuery(query), options) {}

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementEventWatcher'/> class 
        ///    that listens for events conforming to the given WMI event query, according to the specified
        ///    options. For this variant, the query and the scope are specified objects. The
        ///    options object can specify options such as timeout and context information.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> object representing the scope (namespace) in which the watcher will listen for events.</param>
        /// <param name=' query'>An <see cref='System.Management.EventQuery'/> object representing a WMI event query, which determines the events for which the watcher will listen.</param>
        /// <param name='options'>An <see cref='System.Management.EventWatcherOptions'/> object representing additional options used to watch for events. </param>
        public ManagementEventWatcher(
            ManagementScope scope, 
            EventQuery query, 
            EventWatcherOptions options)
        {
            if (null != scope)
                this.scope = ManagementScope._Clone(scope, new IdentifierChangedEventHandler(HandleIdentifierChange));
            else
                this.scope = ManagementScope._Clone(null, new IdentifierChangedEventHandler(HandleIdentifierChange));

            if (null != query)
                this.query = (EventQuery)query.Clone();
            else
                this.query = new EventQuery();
            this.query.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);

            if (null != options)
                this.options = (EventWatcherOptions)options.Clone();
            else
                this.options = new EventWatcherOptions();
            this.options.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);

            enumWbem = null;
            cachedCount = 0; 
            cacheIndex = 0;
            sink = null;
            delegateInvoker = new WmiDelegateInvoker (this);
        }
        
        /// <summary>
        ///    <para>Ensures that outstanding calls are cleared. This is the destructor for the object.</para>
        /// </summary>
        ~ManagementEventWatcher ()
        {
            // Ensure any outstanding calls are cleared
            Stop ();

            if (null != scope)
                scope.IdentifierChanged -= new IdentifierChangedEventHandler (HandleIdentifierChange);

            if (null != options)
                options.IdentifierChanged -= new IdentifierChangedEventHandler (HandleIdentifierChange);

            if (null != query)
                query.IdentifierChanged -= new IdentifierChangedEventHandler (HandleIdentifierChange);
        }

        // 
        // Events
        //

        /// <summary>
        ///    <para> Occurs when a new event arrives.</para>
        /// </summary>
        public event EventArrivedEventHandler       EventArrived;

        /// <summary>
        ///    <para> Occurs when a subscription is canceled.</para>
        /// </summary>
        public event StoppedEventHandler            Stopped;

        //
        //Public Properties
        //

        /// <summary>
        ///    <para>Gets or sets the scope in which to watch for events (namespace or scope).</para>
        /// </summary>
        /// <value>
        ///    <para> The scope in which to watch for events (namespace or scope).</para>
        /// </value>
        public ManagementScope Scope 
        {
            get 
            { 
                return scope; 
            } 
            set 
            {
                if (null != value)
                {
                    ManagementScope oldScope = scope;
                    scope = (ManagementScope)value.Clone ();

                    // Unregister ourselves from the previous scope object
                    if (null != oldScope)
                        oldScope.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    //register for change events in this object
                    scope.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                    //the scope property has changed so act like we fired the event
                    HandleIdentifierChange(this, null);
                }
                else
                    throw new ArgumentNullException("value");
            }
        }

        /// <summary>
        ///    <para>Gets or sets the criteria to apply to events.</para>
        /// </summary>
        /// <value>
        ///    <para> The criteria to apply to the events, which is equal to the event query.</para>
        /// </value>
        public EventQuery Query 
        {
            get 
            { 
                return query; 
            } 
            set 
            { 
                if (null != value)
                {
                    ManagementQuery oldQuery = query;
                    query = (EventQuery)value.Clone ();

                    // Unregister ourselves from the previous query object
                    if (null != oldQuery)
                        oldQuery.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    //register for change events in this object
                    query.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                    //the query property has changed so act like we fired the event
                    HandleIdentifierChange(this, null);
                }
                else
                    throw new ArgumentNullException("value");
            }
        }

        /// <summary>
        ///    <para>Gets or sets the options used to watch for events.</para>
        /// </summary>
        /// <value>
        ///    <para>The options used to watch for events.</para>
        /// </value>
        public EventWatcherOptions Options 
        { 
            get 
            { 
                return options; 
            } 
            set 
            { 
                if (null != value)
                {
                    EventWatcherOptions oldOptions = options;
                    options = (EventWatcherOptions)value.Clone ();

                    // Unregister ourselves from the previous scope object
                    if (null != oldOptions)
                        oldOptions.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    cachedObjects = new IWbemClassObjectFreeThreaded[options.BlockSize];
                    //register for change events in this object
                    options.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                    //the options property has changed so act like we fired the event
                    HandleIdentifierChange(this, null);
                }
                else
                    throw new ArgumentNullException("value");
            } 
        }

        /// <summary>
        ///    <para>Waits for the next event that matches the specified query to arrive, and
        ///       then returns it.</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> representing the 
        ///    newly arrived event.</para>
        /// </returns>
        /// <remarks>
        ///    <para>If the event watcher object contains options with
        ///       a specified timeout, the API will wait for the next event only for the specified
        ///       amount of time; otherwise, the API will be blocked until the next event occurs.</para>
        /// </remarks>
        public ManagementBaseObject WaitForNextEvent()
        {
            ManagementBaseObject obj = null;

            Initialize ();
            
#pragma warning disable CA2002
            lock(this)
#pragma warning restore CA2002
            {
                SecurityHandler securityHandler = Scope.GetSecurityHandler();

                int status = (int)ManagementStatus.NoError;

                try 
                {
                    if (null == enumWbem)   //don't have an enumerator yet - get it
                    {
                        //Execute the query 
                        status = scope.GetSecuredIWbemServicesHandler( Scope.GetIWbemServices() ).ExecNotificationQuery_(
                            query.QueryLanguage,
                            query.QueryString, 
                            options.Flags,
                            options.GetContext (),
                            ref enumWbem);
                    }

                    if (status >= 0)
                    {
                        if ((cachedCount - cacheIndex) == 0) //cache is empty - need to get more objects
                        {
                            //Because Interop doesn't support custom marshalling for arrays, we have to use
                            //the "DoNotMarshal" objects in the interop and then convert to the "FreeThreaded"
                            //counterparts afterwards.
                            IWbemClassObject_DoNotMarshal[] tempArray = new IWbemClassObject_DoNotMarshal[options.BlockSize];

                            int timeout = (ManagementOptions.InfiniteTimeout == options.Timeout)
                                ? (int) tag_WBEM_TIMEOUT_TYPE.WBEM_INFINITE :
                                (int) options.Timeout.TotalMilliseconds;
                            
                            status = scope.GetSecuredIEnumWbemClassObjectHandler(enumWbem).Next_(timeout, (uint)options.BlockSize, tempArray, ref cachedCount);
                            cacheIndex = 0;

                            if (status >= 0)
                            {
                                //Convert results and put them in cache. Note that we may have timed out
                                //in which case we might not have all the objects. If no object can be returned
                                //we throw a timeout exception.
                                if (cachedCount == 0)
                                    ManagementException.ThrowWithExtendedInfo(ManagementStatus.Timedout);

                                for (int i = 0; i < cachedCount; i++)
                                    cachedObjects[i] = new IWbemClassObjectFreeThreaded(Marshal.GetIUnknownForObject(tempArray[i]));
                            }
                        }

                        if (status >= 0)
                        {
                            obj = new ManagementBaseObject(cachedObjects[cacheIndex]);
                            cacheIndex++;
                        }
                    }
                } 
                finally 
                {
                    securityHandler.Reset();
                }

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }

            return obj;
        }


        //********************************************
        //Start
        //********************************************
        /// <summary>
        ///    <para>Subscribes to events with the given query and delivers 
        ///       them, asynchronously, through the <see cref='System.Management.ManagementEventWatcher.EventArrived'/> event.</para>
        /// </summary>
        public void Start()
        {
            Initialize ();

            // Cancel any current event query
            Stop ();
            
            // Submit a new query
            SecurityHandler securityHandler = Scope.GetSecurityHandler();
            IWbemServices wbemServices = scope.GetIWbemServices();

            try
            {
                sink = new SinkForEventQuery(this, options.Context, wbemServices);
                if (sink.Status < 0) 
                {
                    Marshal.ThrowExceptionForHR(sink.Status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                // For async event queries we should ensure 0 flags as this is
                // the only legal value
                int status = scope.GetSecuredIWbemServicesHandler(wbemServices).ExecNotificationQueryAsync_(
                    query.QueryLanguage,
                    query.QueryString,
                    0,
                    options.GetContext(),
                    sink.Stub);


                if (status < 0)
                {
                    if (sink != null)
                    {
                        sink.ReleaseStub();
                        sink = null;
                    }

                    if ((status & 0xfffff000) == 0x80041000)

                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
            finally
            {
                securityHandler.Reset();
            }
        }
        
        //********************************************
        //Stop
        //********************************************
        /// <summary>
        ///    <para>Cancels the subscription whether it is synchronous or asynchronous.</para>
        /// </summary>
        public void Stop()
        {
            //For semi-synchronous, release the WMI enumerator to cancel the subscription
            if (null != enumWbem)
            {
                Marshal.ReleaseComObject(enumWbem);
                enumWbem = null;
                FireStopped (new StoppedEventArgs (options.Context, (int)ManagementStatus.OperationCanceled));
            }

            // In async mode cancel the call to the sink - this will
            // unwind the operation and cause a Stopped message
            if (null != sink)
            {
                sink.Cancel ();
                sink = null;
            }
        }

        private void Initialize ()
        {
            //If the query is not set yet we can't do it
            if (null == query)
                throw new InvalidOperationException();

            if (null == options)
                Options = new EventWatcherOptions ();

            //If we're not connected yet, this is the time to do it...
#pragma warning disable CA2002
            lock (this)
#pragma warning restore CA2002
            {
                if (null == scope)
                    Scope = new ManagementScope ();

                if (null == cachedObjects)
                    cachedObjects = new IWbemClassObjectFreeThreaded[options.BlockSize];
            }

            lock (scope)
            {
                scope.Initialize ();
            }
        }


        internal void FireStopped (StoppedEventArgs args)
        {
            try 
            {
                delegateInvoker.FireEventToDelegates (Stopped, args);
            } 
            catch
            {
            }
        }

        internal void FireEventArrived (EventArrivedEventArgs args)
        {
            try 
            {
                delegateInvoker.FireEventToDelegates (EventArrived, args);
            } 
            catch
            {
            }
        }



    }

    internal class SinkForEventQuery : IWmiEventSource
    {
        private ManagementEventWatcher          eventWatcher;
        private object                          context;
        private IWbemServices                   services;
        private IWbemObjectSink stub;           // The secured IWbemObjectSink
        private int status;
        private bool isLocal;

        public int Status {get {return status;} set {status=value;}}

        public SinkForEventQuery (ManagementEventWatcher eventWatcher,
            object context, 
            IWbemServices services)
        {
            this.services = services;
            this.context = context;
            this.eventWatcher = eventWatcher;
            this.status = 0;
            this.isLocal = false;

            // determine if the server is local, and if so don't create a real stub using unsecap
            if((0==String.Compare(eventWatcher.Scope.Path.Server, ".", StringComparison.OrdinalIgnoreCase)) ||
                (0==String.Compare(eventWatcher.Scope.Path.Server, System.Environment.MachineName, StringComparison.OrdinalIgnoreCase)))
            {
                this.isLocal = true;
            }
            
            if(MTAHelper.IsNoContextMTA())
                HackToCreateStubInMTA(this);
            else
            {
                //
                // Ensure we are able to trap exceptions from worker thread.
                //
                ThreadDispatch disp = new ThreadDispatch ( new ThreadDispatch.ThreadWorkerMethodWithParam ( HackToCreateStubInMTA ) ) ;
                disp.Parameter = this ;
                disp.Start ( ) ;
            }

        }

        void HackToCreateStubInMTA(object param)
        {
            SinkForEventQuery obj = (SinkForEventQuery) param ;
            object dmuxStub = null;
            obj.Status = WmiNetUtilsHelper.GetDemultiplexedStub_f (obj, obj.isLocal, out dmuxStub);
            obj.stub = (IWbemObjectSink) dmuxStub;
        }

        internal IWbemObjectSink Stub 
        { 
            get { return stub; }
        }

        public void Indicate(IntPtr pWbemClassObject)
        {
            Marshal.AddRef(pWbemClassObject);
            IWbemClassObjectFreeThreaded obj = new IWbemClassObjectFreeThreaded(pWbemClassObject);
            try

            {
                EventArrivedEventArgs args = new EventArrivedEventArgs(context, new ManagementBaseObject(obj));

                eventWatcher.FireEventArrived(args);
            }
            catch
            {
            }
        }
    
        public void SetStatus (
            int flags, 
            int hResult, 
            String message, 
            IntPtr pErrObj)
        {
            try 
            {
                // Fire Stopped event
                eventWatcher.FireStopped(new StoppedEventArgs(context, hResult));

                //This handles cases in which WMI calls SetStatus to indicate a problem, for example
                //a queue overflow due to slow client processing.
                //Currently we just cancel the subscription in this case.
                if (hResult != (int)tag_WBEMSTATUS.WBEM_E_CALL_CANCELLED
                    && hResult != (int)tag_WBEMSTATUS.WBEM_S_OPERATION_CANCELLED)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Cancel2));
            }
            catch
            {
            }
        }

        // On Win2k, we get a deadlock if we do a Cancel within a SetStatus
        // Instead of calling it from SetStatus, we use ThreadPool.QueueUserWorkItem
        void Cancel2(object o)
        {
            //
            // Try catch the call to cancel. In this case the cancel is being done without the client
            // knowing about it so catching all exceptions is not a bad thing to do. If a client calls
            // Stop (which calls Cancel), they will still recieve any exceptions that may have occured.
            //
            try
            {
                Cancel();
            }
            catch
            {
            }
        }

        internal void Cancel () 
        {
            if (null != stub)
            {
#pragma warning disable CA2002
                lock(this)
#pragma warning restore CA2002
                {
                    if (null != stub)
                    {

                        int status = services.CancelAsyncCall_(stub);

                        // Release prior to throwing an exception.
                        ReleaseStub();

                        if (status < 0)
                        {
                            if ((status & 0xfffff000) == 0x80041000)
                                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                            else
                                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                        }
                    }
                }
            }
        }

        internal void ReleaseStub ()
        {
            if (null != stub)
            {
#pragma warning disable CA2002
                lock(this)
#pragma warning restore CA2002
                {
                    /*
                     * We force a release of the stub here so as to allow
                     * unsecapp.exe to die as soon as possible.
                     * however if it is local, unsecap won't be started
                     */
                    if (null != stub)
                    {
                        try 
                        {
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(stub);
                            stub = null;
                        } 
                        catch
                        {
                        }
                    }
                }
            }
        }
    }

}
