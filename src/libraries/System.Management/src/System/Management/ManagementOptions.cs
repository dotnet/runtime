// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Security;

namespace System.Management
{
    /// <summary>
    ///    <para>Describes the authentication level to be used to connect to WMI. This is used for the COM connection to WMI.</para>
    /// </summary>
    public enum AuthenticationLevel
    {
        /// <summary>
        ///    <para>The default COM authentication level. WMI uses the default Windows Authentication setting.</para>
        /// </summary>
        Default = 0,
        /// <summary>
        ///    <para> No COM authentication.</para>
        /// </summary>
        None = 1,
        /// <summary>
        ///    <para> Connect-level COM authentication.</para>
        /// </summary>
        Connect = 2,
        /// <summary>
        ///    <para> Call-level COM authentication.</para>
        /// </summary>
        Call = 3,
        /// <summary>
        ///    <para> Packet-level COM authentication.</para>
        /// </summary>
        Packet = 4,
        /// <summary>
        ///    <para>Packet Integrity-level COM authentication.</para>
        /// </summary>
        PacketIntegrity = 5,
        /// <summary>
        ///    <para>Packet Privacy-level COM authentication.</para>
        /// </summary>
        PacketPrivacy = 6,
        /// <summary>
        ///    <para>The default COM authentication level. WMI uses the default Windows Authentication setting.</para>
        /// </summary>
        Unchanged = -1
    }

    /// <summary>
    ///    <para>Describes the impersonation level to be used to connect to WMI.</para>
    /// </summary>
    public enum ImpersonationLevel
    {
        /// <summary>
        ///    <para>Default impersonation.</para>
        /// </summary>
        Default = 0,
        /// <summary>
        ///    <para> Anonymous COM impersonation level that hides the
        ///       identity of the caller. Calls to WMI may fail
        ///       with this impersonation level.</para>
        /// </summary>
        Anonymous = 1,
        /// <summary>
        ///    <para> Identify-level COM impersonation level that allows objects
        ///       to query the credentials of the caller. Calls to
        ///       WMI may fail with this impersonation level.</para>
        /// </summary>
        Identify = 2,
        /// <summary>
        ///    <para> Impersonate-level COM impersonation level that allows
        ///       objects to use the credentials of the caller. This is the recommended impersonation level for WMI calls.</para>
        /// </summary>
        Impersonate = 3,
        /// <summary>
        ///    <para> Delegate-level COM impersonation level that allows objects
        ///       to permit other objects to use the credentials of the caller. This
        ///       level, which will work with WMI calls but may constitute an unnecessary
        ///       security risk, is supported only under Windows 2000.</para>
        /// </summary>
        Delegate = 4
    }

    /// <summary>
    ///    <para>Describes the possible effects of saving an object to WMI when
    ///       using <see cref='System.Management.ManagementObject.Put()'/>.</para>
    /// </summary>
    public enum PutType
    {
        /// <summary>
        ///    <para> Invalid Type </para>
        /// </summary>
        None = 0,
        /// <summary>
        ///    <para> Updates an existing object
        ///       only; does not create a new object.</para>
        /// </summary>
        UpdateOnly = 1,
        /// <summary>
        ///    <para> Creates an object only;
        ///       does not update an existing object.</para>
        /// </summary>
        CreateOnly = 2,
        /// <summary>
        ///    <para> Saves the object, whether
        ///       updating an existing object or creating a new object.</para>
        /// </summary>
        UpdateOrCreate = 3
    }

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para>
    ///       Provides an abstract base class for all Options objects.</para>
    ///    <para>Options objects are used to customize different management operations. </para>
    ///    <para>Use one of the Options classes derived from this class, as
    ///       indicated by the signature of the operation being performed.</para>
    /// </summary>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class ManagementOptions : ICloneable
    {
        /// <summary>
        ///    <para> Specifies an infinite timeout.</para>
        /// </summary>
        public static readonly TimeSpan InfiniteTimeout = TimeSpan.MaxValue;

        internal int flags;
        internal ManagementNamedValueCollection context;
        internal TimeSpan timeout;

        //Used when any public property on this object is changed, to signal
        //to the containing object that it needs to be refreshed.
        internal event IdentifierChangedEventHandler IdentifierChanged;

        //Fires IdentifierChanged event
        internal void FireIdentifierChanged()
        {
            IdentifierChanged?.Invoke(this, null);
        }

        //Called when IdentifierChanged() event fires
        internal void HandleIdentifierChange(object sender,
                            IdentifierChangedEventArgs args)
        {
            //Something inside ManagementOptions changed, we need to fire an event
            //to the parent object
            FireIdentifierChanged();
        }

        internal int Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        /// <summary>
        ///    <para> Gets or sets a WMI context object. This is a
        ///       name-value pairs list to be passed through to a WMI provider that supports
        ///       context information for customized operation.</para>
        /// </summary>
        /// <value>
        ///    <para>A name-value pairs list to be passed through to a WMI provider that
        ///       supports context information for customized operation.</para>
        /// </value>
        public ManagementNamedValueCollection Context
        {
            get
            {
                return context ??= new ManagementNamedValueCollection();
            }
            set
            {
                ManagementNamedValueCollection oldContext = context;

                if (null != value)
                    context = (ManagementNamedValueCollection)value.Clone();
                else
                    context = new ManagementNamedValueCollection();

                if (null != oldContext)
                    oldContext.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                //register for change events in this object
                context.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);

                //the context property has changed so act like we fired the event
                HandleIdentifierChange(this, null);
            }
        }

        /// <summary>
        ///    <para>Gets or sets the timeout to apply to the operation.
        ///       Note that for operations that return collections, this timeout applies to the
        ///       enumeration through the resulting collection, not the operation itself
        ///       (the <see cref='System.Management.EnumerationOptions.ReturnImmediately'/>
        ///       property is used for the latter).</para>
        ///    This property is used to indicate that the operation should be performed semisynchronously.
        /// </summary>
        /// <value>
        /// <para>The default value for this property is <see cref='System.Management.ManagementOptions.InfiniteTimeout'/>
        /// , which means the operation will block.
        /// The value specified must be positive.</para>
        /// </value>
        public TimeSpan Timeout
        {
            get
            { return timeout; }
            set
            {
                //Timespan allows for negative values, but we want to make sure it's positive here...
                if (value.Ticks < 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                timeout = value;
                FireIdentifierChanged();
            }
        }


        internal ManagementOptions() : this(null, InfiniteTimeout) { }
        internal ManagementOptions(ManagementNamedValueCollection context, TimeSpan timeout) : this(context, timeout, 0) { }
        internal ManagementOptions(ManagementNamedValueCollection context, TimeSpan timeout, int flags)
        {
            this.flags = flags;
            if (context != null)
                this.Context = context;
            else
                this.context = null;
            this.Timeout = timeout;
        }


        internal IWbemContext GetContext()
        {
            if (context != null)
                return context.GetContext();
            else
                return null;
        }

        // We do not expose this publicly; instead the flag is set automatically
        // when making an async call if we detect that someone has requested to
        // listen for status messages.
        internal bool SendStatus
        {
            get
            { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_SEND_STATUS) != 0) ? true : false); }
            set
            {
                Flags = (value == false) ? (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_SEND_STATUS) :
                    (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_SEND_STATUS);
            }
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public abstract object Clone();
    }


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para>Provides a base class for query and enumeration-related options
    ///       objects.</para>
    ///    <para>Use this class to customize enumeration of management
    ///       objects, traverse management object relationships, or query for
    ///       management objects.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to enumerate all top-level WMI classes
    /// // and subclasses in root/cimv2 namespace.
    /// class Sample_EnumerationOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass newClass = new ManagementClass();
    ///         EnumerationOptions options = new EnumerationOptions();
    ///         options.EnumerateDeep = false;
    ///         foreach (ManagementObject o in newClass.GetSubclasses(options)) {
    ///             Console.WriteLine(o["__Class"]);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates how to enumerate all top-level WMI classes
    /// ' and subclasses in root/cimv2 namespace.
    /// Class Sample_EnumerationOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim newClass As New ManagementClass()
    ///         Dim options As New EnumerationOptions()
    ///         options.EnumerateDeep = False
    ///         Dim o As ManagementObject
    ///         For Each o In newClass.GetSubclasses(options)
    ///             Console.WriteLine(o("__Class"))
    ///         Next o
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class EnumerationOptions : ManagementOptions
    {
        private int blockSize;

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the invoked operation should be
        ///       performed in a synchronous or semisynchronous fashion. If this property is set
        ///       to <see langword='true'/>, the enumeration is invoked and the call returns immediately. The actual
        ///       retrieval of the results will occur when the resulting collection is walked.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the invoked operation should
        ///    be performed in a synchronous or semisynchronous fashion; otherwise,
        /// <see langword='false'/>. The default value is <see langword='true'/>.</para>
        /// </value>
        public bool ReturnImmediately
        {
            get { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY) != 0) ? true : false); }
            set
            {
                Flags = (value == false) ? (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY) :
                            (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY);
            }
        }

        /// <summary>
        ///    <para> Gets or sets the block size
        ///       for block operations. When enumerating through a collection, WMI will return results in
        ///       groups of the specified size.</para>
        /// </summary>
        /// <value>
        ///    <para>The default value is 1.</para>
        /// </value>
        public int BlockSize
        {
            get { return blockSize; }
            set
            {
                //Unfortunately BlockSize was defined as int, but valid values are only  > 0
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value));

                blockSize = value;
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the collection is assumed to be
        ///       rewindable. If <see langword='true'/>, the objects in the
        ///       collection will be kept available for multiple enumerations. If
        ///    <see langword='false'/>, the collection
        ///       can only be enumerated one time.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the collection is assumed to
        ///    be rewindable; otherwise, <see langword='false'/>. The default value is
        /// <see langword='true'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para>A rewindable collection is more costly in memory
        ///       consumption as all the objects need to be kept available at the same time.
        ///       In a collection defined as non-rewindable, the objects are discarded after being returned
        ///       in the enumeration.</para>
        /// </remarks>
        public bool Rewindable
        {
            get { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY) != 0) ? false : true); }
            set
            {
                Flags = value ? (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY) :
                                (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY);
            }
        }

        /// <summary>
        ///    <para> Gets or sets a value indicating whether the objects returned from
        ///       WMI should contain amended information. Typically, amended information is localizable
        ///       information attached to the WMI object, such as object and property
        ///       descriptions.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the objects returned from WMI
        ///    should contain amended information; otherwise, <see langword='false'/>. The
        ///    default value is <see langword='false'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para>If descriptions and other amended information are not of
        ///       interest, setting this property to <see langword='false'/>
        ///       is more
        ///       efficient.</para>
        /// </remarks>
        public bool UseAmendedQualifiers
        {
            get { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) :
                                (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS);
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether to the objects returned should have
        ///       locatable information in them. This ensures that the system properties, such as
        ///    <see langword='__PATH'/>, <see langword='__RELPATH'/>, and
        ///    <see langword='__SERVER'/>, are non-NULL. This flag can only be used in queries,
        ///       and is ignored in enumerations.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if WMI
        ///    should ensure all returned objects have valid paths; otherwise,
        /// <see langword='false'/>. The default value is <see langword='false'/>.</para>
        /// </value>
        public bool EnsureLocatable
        {
            get
            { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_ENSURE_LOCATABLE) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_ENSURE_LOCATABLE) :
                                (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_ENSURE_LOCATABLE);
            }
        }


        /// <summary>
        ///    <para>Gets or sets a value indicating whether the query should return a
        ///       prototype of the result set instead of the actual results. This flag is used for
        ///       prototyping.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the
        ///    query should return a prototype of the result set instead of the actual results;
        ///    otherwise, <see langword='false'/>. The default value is
        /// <see langword='false'/>.</para>
        /// </value>
        public bool PrototypeOnly
        {
            get
            { return (((Flags & (int)tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_PROTOTYPE) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_PROTOTYPE) :
                                (Flags & (int)~tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_PROTOTYPE);
            }
        }

        /// <summary>
        ///    <para> Gets or sets a value indicating whether direct access to the WMI provider is requested for the specified class,
        ///       without any regard to its base class or derived classes.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if only
        ///    objects of the specified class should be received, without regard to derivation
        ///    or inheritance; otherwise, <see langword='false'/>. The default value is
        /// <see langword='false'/>. </para>
        /// </value>
        public bool DirectRead
        {
            get
            { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_DIRECT_READ) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_DIRECT_READ) :
                                (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_DIRECT_READ);
            }
        }


        /// <summary>
        ///    <para> Gets or sets a value indicating whether recursive enumeration is requested
        ///       into all classes derived from the specified base class. If
        ///    <see langword='false'/>, only immediate derived
        ///       class members are returned.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if recursive enumeration is requested
        ///    into all classes derived from the specified base class; otherwise,
        /// <see langword='false'/>. The default value is <see langword='false'/>.</para>
        /// </value>
        public bool EnumerateDeep
        {
            get
            { return (((Flags & (int)tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_SHALLOW) != 0) ? false : true); }
            set
            {
                Flags = (value == false) ? (Flags | (int)tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_SHALLOW) :
                        (Flags & (int)~tag_WBEM_QUERY_FLAG_TYPE.WBEM_FLAG_SHALLOW);
            }
        }


        //default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.EnumerationOptions'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.EnumerationOptions'/>
        /// class with default values (see the individual property descriptions
        /// for what the default values are). This is the default constructor. </para>
        /// </summary>
        public EnumerationOptions() : this(null, InfiniteTimeout, 1, true, true, false, false, false, false, false) { }



        //Constructor that specifies flags as individual values - we need to set the flags accordingly !
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.EnumerationOptions'/> class to be used for queries or enumerations,
        ///    allowing the user to specify values for the different options.</para>
        /// </summary>
        /// <param name='context'>The options context object containing provider-specific information that can be passed through to the provider.</param>
        /// <param name=' timeout'>The timeout value for enumerating through the results.</param>
        /// <param name=' blockSize'>The number of items to retrieve at one time from WMI.</param>
        /// <param name=' rewindable'><see langword='true'/> to specify whether the result set is rewindable (=allows multiple traversal or one-time); otherwise, <see langword='false'/>.</param>
        /// <param name=' returnImmediatley'><see langword='true'/> to specify whether the operation should return immediately (semi-sync) or block until all results are available; otherwise, <see langword='false'/> .</param>
        /// <param name=' useAmendedQualifiers'><see langword='true'/> to specify whether the returned objects should contain amended (locale-aware) qualifiers; otherwise, <see langword='false'/> .</param>
        /// <param name=' ensureLocatable'><see langword='true'/> to specify to WMI that it should ensure all returned objects have valid paths; otherwise, <see langword='false'/> .</param>
        /// <param name=' prototypeOnly'><see langword='true'/> to return a prototype of the result set instead of the actual results; otherwise, <see langword='false'/> .</param>
        /// <param name=' directRead'><see langword='true'/> to retrieve objects of only the specified class only or from derived classes as well; otherwise, <see langword='false'/> .</param>
        /// <param name=' enumerateDeep'><see langword='true'/> to specify recursive enumeration in subclasses; otherwise, <see langword='false'/> .</param>
        public EnumerationOptions(
            ManagementNamedValueCollection context,
            TimeSpan timeout,
            int blockSize,
            bool rewindable,
            bool returnImmediatley,
            bool useAmendedQualifiers,
            bool ensureLocatable,
            bool prototypeOnly,
            bool directRead,
            bool enumerateDeep) : base(context, timeout)
        {
            BlockSize = blockSize;
            Rewindable = rewindable;
            ReturnImmediately = returnImmediatley;
            UseAmendedQualifiers = useAmendedQualifiers;
            EnsureLocatable = ensureLocatable;
            PrototypeOnly = prototypeOnly;
            DirectRead = directRead;
            EnumerateDeep = enumerateDeep;
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new EnumerationOptions(newContext, Timeout, blockSize, Rewindable,
                            ReturnImmediately, UseAmendedQualifiers, EnsureLocatable, PrototypeOnly, DirectRead, EnumerateDeep);
        }

    }//EnumerationOptions



    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies options for management event watching.</para>
    ///    <para>Use this class to customize subscriptions for watching management events. </para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to listen to an event using ManagementEventWatcher object.
    /// class Sample_EventWatcherOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass newClass = new ManagementClass();
    ///         newClass["__CLASS"] = "TestDeletionClass";
    ///         newClass.Put();
    ///
    ///         EventWatcherOptions options = new EventWatcherOptions();
    ///         ManagementEventWatcher watcher = new ManagementEventWatcher(null,
    ///                                                                     new WqlEventQuery("__classdeletionevent"),
    ///                                                                     options);
    ///         MyHandler handler = new MyHandler();
    ///         watcher.EventArrived += new EventArrivedEventHandler(handler.Arrived);
    ///         watcher.Start();
    ///
    ///         // Delete class to trigger event
    ///         newClass.Delete();
    ///
    ///         //For the purpose of this example, we will wait
    ///         // two seconds before main thread terminates.
    ///         System.Threading.Thread.Sleep(2000);
    ///
    ///         watcher.Stop();
    ///
    ///         return 0;
    ///     }
    ///
    ///     public class MyHandler
    ///     {
    ///        public void Arrived(object sender, EventArrivedEventArgs e) {
    ///            Console.WriteLine("Class Deleted= " +
    ///                ((ManagementBaseObject)e.NewEvent["TargetClass"])["__CLASS"]);
    ///        }
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates how to listen to an event using the ManagementEventWatcher object.
    /// Class Sample_EventWatcherOptions
    ///     Public Shared Sub Main()
    ///         Dim newClass As New ManagementClass()
    ///         newClass("__CLASS") = "TestDeletionClass"
    ///         newClass.Put()
    ///
    ///         Dim options As _
    ///             New EventWatcherOptions()
    ///         Dim watcher As New ManagementEventWatcher( _
    ///             Nothing, _
    ///             New WqlEventQuery("__classdeletionevent"), _
    ///             options)
    ///         Dim handler As New MyHandler()
    ///         AddHandler watcher.EventArrived, AddressOf handler.Arrived
    ///         watcher.Start()
    ///
    ///         ' Delete class to trigger event
    ///         newClass.Delete()
    ///
    ///         ' For the purpose of this example, we will wait
    ///         ' two seconds before main thread terminates.
    ///         System.Threading.Thread.Sleep(2000)
    ///         watcher.Stop()
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
    public class EventWatcherOptions : ManagementOptions
    {
        private int blockSize = 1;

        /// <summary>
        ///    <para>Gets or sets the block size for block operations. When waiting for events, this
        ///       value specifies how many events to wait for before returning.</para>
        /// </summary>
        /// <value>
        ///    <para>The default value is 1.</para>
        /// </value>
        public int BlockSize
        {
            get { return blockSize; }
            set
            {
                blockSize = value;
                FireIdentifierChanged();
            }

        }

        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.EventWatcherOptions'/> class. </para>
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.EventWatcherOptions'/> class for event watching, using default values.
        ///    This is the default constructor.</para>
        /// </summary>
        public EventWatcherOptions()
            : this(null, InfiniteTimeout, 1) { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.EventWatcherOptions'/> class with the given
        ///    values.</para>
        /// </summary>
        /// <param name='context'>The options context object containing provider-specific information to be passed through to the provider. </param>
        /// <param name=' timeout'>The timeout to wait for the next events.</param>
        /// <param name=' blockSize'>The number of events to wait for in each block.</param>
        public EventWatcherOptions(ManagementNamedValueCollection context, TimeSpan timeout, int blockSize)
            : base(context, timeout)
        {
            Flags = (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_FORWARD_ONLY;
            BlockSize = blockSize;
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new EventWatcherOptions(newContext, Timeout, blockSize);
        }
    }//EventWatcherOptions



    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies options for getting a management object.</para>
    ///    Use this class to customize retrieval of a management object.
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to set a timeout value and list
    /// // all amended qualifiers in a ManagementClass object.
    /// class Sample_ObjectGetOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         // Request amended qualifiers
    ///         ObjectGetOptions options =
    ///             new ObjectGetOptions(null, new TimeSpan(0,0,0,5), true);
    ///         ManagementClass diskClass =
    ///             new ManagementClass("root/cimv2", "Win32_Process", options);
    ///         foreach (QualifierData qualifier in diskClass.Qualifiers) {
    ///             Console.WriteLine(qualifier.Name + ":" + qualifier.Value);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates how to set a timeout value and list
    /// ' all amended qualifiers in a ManagementClass object.
    /// Class Sample_ObjectGetOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         ' Request amended qualifiers
    ///         Dim options As _
    ///             New ObjectGetOptions(Nothing, New TimeSpan(0, 0, 0, 5), True)
    ///         Dim diskClass As New ManagementClass( _
    ///             "root/cimv2", _
    ///             "Win32_Process", _
    ///             options)
    ///         Dim qualifier As QualifierData
    ///         For Each qualifier In diskClass.Qualifiers
    ///             Console.WriteLine(qualifier.Name &amp; ":" &amp; qualifier.Value)
    ///         Next qualifier
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ObjectGetOptions : ManagementOptions
    {
        internal static ObjectGetOptions _Clone(ObjectGetOptions options)
        {
            return ObjectGetOptions._Clone(options, null);
        }

        internal static ObjectGetOptions _Clone(ObjectGetOptions options, IdentifierChangedEventHandler handler)
        {
            ObjectGetOptions optionsTmp;

            if (options != null)
                optionsTmp = new ObjectGetOptions(options.context, options.timeout, options.UseAmendedQualifiers);
            else
                optionsTmp = new ObjectGetOptions();

            // Wire up change handler chain. Use supplied handler, if specified;
            // otherwise, default to that of the path argument.
            if (handler != null)
                optionsTmp.IdentifierChanged += handler;
            else if (options != null)
                optionsTmp.IdentifierChanged += new IdentifierChangedEventHandler(options.HandleIdentifierChange);

            return optionsTmp;
        }

        /// <summary>
        ///    <para> Gets or sets a value indicating whether the objects returned from WMI should
        ///       contain amended information. Typically, amended information is localizable information
        ///       attached to the WMI object, such as object and property descriptions.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the objects returned from WMI
        ///    should contain amended information; otherwise, <see langword='false'/>. The
        ///    default value is <see langword='false'/>.</para>
        /// </value>
        public bool UseAmendedQualifiers
        {
            get { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) :
                                (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS);
                FireIdentifierChanged();
            }
        }

        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.ObjectGetOptions'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ObjectGetOptions'/> class for getting a WMI object, using
        ///    default values. This is the default constructor.</para>
        /// </summary>
        public ObjectGetOptions() : this(null, InfiniteTimeout, false) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ObjectGetOptions'/> class for getting a WMI object, using the
        ///    specified provider-specific context.</para>
        /// </summary>
        /// <param name='context'>A provider-specific, named-value pairs context object to be passed through to the provider.</param>
        public ObjectGetOptions(ManagementNamedValueCollection context) : this(context, InfiniteTimeout, false) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ObjectGetOptions'/> class for getting a WMI object,
        ///    using the given options values.</para>
        /// </summary>
        /// <param name='context'>A provider-specific, named-value pairs context object to be passed through to the provider.</param>
        /// <param name=' timeout'>The length of time to let the operation perform before it times out. The default is <see cref='System.Management.ManagementOptions.InfiniteTimeout'/> .</param>
        /// <param name=' useAmendedQualifiers'><see langword='true'/> if the returned objects should contain amended (locale-aware) qualifiers; otherwise, <see langword='false'/>. </param>
        public ObjectGetOptions(ManagementNamedValueCollection context, TimeSpan timeout, bool useAmendedQualifiers) : base(context, timeout)
        {
            UseAmendedQualifiers = useAmendedQualifiers;
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new ObjectGetOptions(newContext, Timeout, UseAmendedQualifiers);
        }
    }

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies options for committing management
    ///       object changes.</para>
    ///    <para>Use this class to customize how values are saved to a management object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to specify a PutOptions using
    /// // PutOptions object when saving a ManagementClass object to
    /// // the WMI respository.
    /// class Sample_PutOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass newClass = new ManagementClass("root/default",
    ///                                                        String.Empty,
    ///                                                        null);
    ///         newClass["__Class"] = "class999xc";
    ///
    ///         PutOptions options = new PutOptions();
    ///         options.Type = PutType.UpdateOnly;
    ///
    ///         try
    ///         {
    ///             newClass.Put(options); //will fail if the class doesn't already exist
    ///         }
    ///         catch (ManagementException e)
    ///         {
    ///             Console.WriteLine("Couldn't update class: " + e.ErrorCode);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates how to specify a PutOptions using
    /// ' PutOptions object when saving a ManagementClass object to
    /// ' WMI respository.
    /// Class Sample_PutOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim newClass As New ManagementClass( _
    ///            "root/default", _
    ///            String.Empty, _
    ///            Nothing)
    ///         newClass("__Class") = "class999xc"
    ///
    ///         Dim options As New PutOptions()
    ///         options.Type = PutType.UpdateOnly 'will fail if the class doesn't already exist
    ///
    ///         Try
    ///             newClass.Put(options)
    ///         Catch e As ManagementException
    ///             Console.WriteLine("Couldn't update class: " &amp; e.ErrorCode)
    ///         End Try
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class PutOptions : ManagementOptions
    {

        /// <summary>
        ///    <para> Gets or sets a value indicating whether the objects returned from WMI should
        ///       contain amended information. Typically, amended information is localizable information
        ///       attached to the WMI object, such as object and property descriptions.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the objects returned from WMI
        ///    should contain amended information; otherwise, <see langword='false'/>. The
        ///    default value is <see langword='false'/>.</para>
        /// </value>
        public bool UseAmendedQualifiers
        {
            get { return (((Flags & (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) != 0) ? true : false); }
            set
            {
                Flags = value ? (Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS) :
                                (Flags & (int)~tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_USE_AMENDED_QUALIFIERS);
            }
        }

        /// <summary>
        ///    <para>Gets or sets the type of commit to be performed for the object.</para>
        /// </summary>
        /// <value>
        /// <para>The default value is <see cref='System.Management.PutType.UpdateOrCreate'/>.</para>
        /// </value>
        public PutType Type
        {
            get
            {
                return (((Flags & (int)tag_WBEM_CHANGE_FLAG_TYPE.WBEM_FLAG_UPDATE_ONLY) != 0) ? PutType.UpdateOnly :
                        ((Flags & (int)tag_WBEM_CHANGE_FLAG_TYPE.WBEM_FLAG_CREATE_ONLY) != 0) ? PutType.CreateOnly :
                                                                              PutType.UpdateOrCreate);
            }
            set
            {
                Flags |= value switch
                {
                    PutType.UpdateOnly => (int)tag_WBEM_CHANGE_FLAG_TYPE.WBEM_FLAG_UPDATE_ONLY,
                    PutType.CreateOnly => (int)tag_WBEM_CHANGE_FLAG_TYPE.WBEM_FLAG_CREATE_ONLY,
                    PutType.UpdateOrCreate => (int)tag_WBEM_CHANGE_FLAG_TYPE.WBEM_FLAG_CREATE_OR_UPDATE,
                    _ => throw new ArgumentException(null, nameof(Type)),
                };
            }
        }

        /// <overload>
        /// <para> Initializes a new instance of the <see cref='System.Management.PutOptions'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.PutOptions'/> class for put operations, using default values.
        ///    This is the default constructor.</para>
        /// </summary>
        public PutOptions() : this(null, InfiniteTimeout, false, PutType.UpdateOrCreate) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.PutOptions'/> class for committing a WMI object, using the
        ///    specified provider-specific context.</para>
        /// </summary>
        /// <param name='context'>A provider-specific, named-value pairs context object to be passed through to the provider.</param>
        public PutOptions(ManagementNamedValueCollection context) : this(context, InfiniteTimeout, false, PutType.UpdateOrCreate) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.PutOptions'/> class for committing a WMI object, using
        ///    the specified option values.</para>
        /// </summary>
        /// <param name='context'>A provider-specific, named-value pairs object to be passed through to the provider. </param>
        /// <param name=' timeout'>The length of time to let the operation perform before it times out. The default is <see cref='System.Management.ManagementOptions.InfiniteTimeout'/> .</param>
        /// <param name=' useAmendedQualifiers'><see langword='true'/> if the returned objects should contain amended (locale-aware) qualifiers; otherwise, <see langword='false'/>. </param>
        /// <param name=' putType'> The type of commit to be performed (update or create).</param>
        public PutOptions(ManagementNamedValueCollection context, TimeSpan timeout, bool useAmendedQualifiers, PutType putType) : base(context, timeout)
        {
            UseAmendedQualifiers = useAmendedQualifiers;
            Type = putType;
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new PutOptions(newContext, Timeout, UseAmendedQualifiers, Type);
        }
    }

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies options for deleting a management
    ///       object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to specify a timeout value
    /// // when deleting a ManagementClass object.
    /// class Sample_DeleteOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass newClass = new ManagementClass();
    ///         newClass["__CLASS"] = "ClassToDelete";
    ///         newClass.Put();
    ///
    ///         // Set deletion options: delete operation timeout value
    ///         DeleteOptions opt = new DeleteOptions(null, new TimeSpan(0,0,0,5));
    ///
    ///         ManagementClass dummyClassToDelete =
    ///             new ManagementClass("ClassToDelete");
    ///         dummyClassToDelete.Delete(opt);
    ///
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to specify a timeout value
    /// ' when deleting a ManagementClass object.
    /// Class Sample_DeleteOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim newClass As New ManagementClass()
    ///         newClass("__CLASS") = "ClassToDelete"
    ///         newClass.Put()
    ///
    ///         ' Set deletion options: delete operation timeout value
    ///         Dim opt As New DeleteOptions(Nothing, New TimeSpan(0, 0, 0, 5))
    ///
    ///         Dim dummyClassToDelete As New ManagementClass("ClassToDelete")
    ///         dummyClassToDelete.Delete(opt)
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class DeleteOptions : ManagementOptions
    {
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.DeleteOptions'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.DeleteOptions'/> class for the delete operation, using default values.
        ///    This is the default constructor.</para>
        /// </summary>
        public DeleteOptions() : base() { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.DeleteOptions'/> class for a delete operation, using
        ///    the specified values.</para>
        /// </summary>
        /// <param name='context'>A provider-specific, named-value pairs object to be passed through to the provider. </param>
        /// <param name='timeout'>The length of time to let the operation perform before it times out. The default value is <see cref='System.Management.ManagementOptions.InfiniteTimeout'/> . Setting this parameter will invoke the operation semisynchronously.</param>
        public DeleteOptions(ManagementNamedValueCollection context, TimeSpan timeout) : base(context, timeout) { }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>A cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new DeleteOptions(newContext, Timeout);
        }
    }

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies options for invoking a management method.</para>
    ///    <para>Use this class to customize the execution of a method on a management
    ///       object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to stop a system service.
    /// class Sample_InvokeMethodOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementObject service =
    ///             new ManagementObject("win32_service=\"winmgmt\"");
    ///         InvokeMethodOptions options = new InvokeMethodOptions();
    ///         options.Timeout = new TimeSpan(0,0,0,5);
    ///
    ///         ManagementBaseObject outParams = service.InvokeMethod("StopService", null, options);
    ///
    ///         Console.WriteLine("Return Status = " + outParams["ReturnValue"]);
    ///
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to stop a system service.
    /// Class Sample_InvokeMethodOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim service As New ManagementObject("win32_service=""winmgmt""")
    ///         Dim options As New InvokeMethodOptions()
    ///         options.Timeout = New TimeSpan(0, 0, 0, 5)
    ///
    ///         Dim outParams As ManagementBaseObject = service.InvokeMethod( _
    ///             "StopService", _
    ///             Nothing, _
    ///             options)
    ///
    ///         Console.WriteLine("Return Status = " &amp; _
    ///             outParams("ReturnValue").ToString())
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class InvokeMethodOptions : ManagementOptions
    {
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.InvokeMethodOptions'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.InvokeMethodOptions'/> class for the <see cref='System.Management.ManagementObject.InvokeMethod(string, ManagementBaseObject, InvokeMethodOptions) '/> operation, using default values.
        ///    This is the default constructor.</para>
        /// </summary>
        public InvokeMethodOptions() : base() { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.InvokeMethodOptions'/> class for an invoke operation using
        ///    the specified values.</para>
        /// </summary>
        /// <param name=' context'>A provider-specific, named-value pairs object to be passed through to the provider. </param>
        /// <param name='timeout'>The length of time to let the operation perform before it times out. The default value is <see cref='System.Management.ManagementOptions.InfiniteTimeout'/> . Setting this parameter will invoke the operation semisynchronously.</param>
        public InvokeMethodOptions(ManagementNamedValueCollection context, TimeSpan timeout) : base(context, timeout) { }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();

            return new InvokeMethodOptions(newContext, Timeout);
        }
    }


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Specifies all settings required to make a WMI connection.</para>
    ///    <para>Use this class to customize a connection to WMI made via a
    ///       ManagementScope object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates how to connect to remote machine
    /// // using supplied credentials.
    /// class Sample_ConnectionOptions
    /// {
    ///     public static int Main(string[] args) {
    ///         ConnectionOptions options = new ConnectionOptions();
    ///         options.Username = "domain\\username";
    ///         options.Password = "password";
    ///         ManagementScope scope = new ManagementScope(
    ///             "\\\\servername\\root\\cimv2",
    ///             options);
    ///         try {
    ///             scope.Connect();
    ///             ManagementObject disk = new ManagementObject(
    ///                 scope,
    ///                 new ManagementPath("Win32_logicaldisk='c:'"),
    ///                 null);
    ///             disk.Get();
    ///         }
    ///         catch (Exception e) {
    ///             Console.WriteLine("Failed to connect: " + e.Message);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates how to connect to remote machine
    /// ' using supplied credentials.
    /// Class Sample_ConnectionOptions
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim options As New ConnectionOptions()
    ///         options.Username = "domain\username"
    ///         options.Password = "password"
    ///         Dim scope As New ManagementScope("\\servername\root\cimv2", options)
    ///         Try
    ///             scope.Connect()
    ///             Dim disk As New ManagementObject(scope, _
    ///                 New ManagementPath("Win32_logicaldisk='c:'"), Nothing)
    ///             disk.Get()
    ///         Catch e As UnauthorizedAccessException
    ///             Console.WriteLine(("Failed to connect: " + e.Message))
    ///         End Try
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ConnectionOptions : ManagementOptions
    {

        internal const string DEFAULTLOCALE = null;
        internal const string DEFAULTAUTHORITY = null;
        internal const ImpersonationLevel DEFAULTIMPERSONATION = ImpersonationLevel.Impersonate;
        internal const AuthenticationLevel DEFAULTAUTHENTICATION = AuthenticationLevel.Unchanged;
        internal const bool DEFAULTENABLEPRIVILEGES = false;

        //Fields
        private string locale;
        private string username;
        private SecureString securePassword;
        private string authority;
        private ImpersonationLevel impersonation;
        private AuthenticationLevel authentication;
        private bool enablePrivileges;


        //
        //Properties
        //

        /// <summary>
        ///    <para>Gets or sets the locale to be used for the connection operation.</para>
        /// </summary>
        /// <value>
        ///    <para>The default value is DEFAULTLOCALE.</para>
        /// </value>
        public string Locale
        {
            get { return locale ?? string.Empty; }
            set
            {
                if (locale != value)
                {
                    locale = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para>Gets or sets the user name to be used for the connection operation.</para>
        /// </summary>
        /// <value>
        ///    <para>Null if the connection will use the currently logged-on user; otherwise, a string representing the user name. The default value is null.</para>
        /// </value>
        /// <remarks>
        ///    <para>If the user name is from a domain other than the current
        ///       domain, the string may contain the domain name and user name, separated by a backslash:</para>
        ///    <c>
        ///       <para>string username = "EnterDomainHere\\EnterUsernameHere";</para>
        ///    </c>
        /// </remarks>
        public string Username
        {
            get { return username; }
            set
            {
                if (username != value)
                {
                    username = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para>Sets the password for the specified user. The value can be set, but not retrieved.</para>
        /// </summary>
        /// <value>
        ///    <para> The default value is null. If the user name is also
        ///       null, the credentials used will be those of the currently logged-on user.</para>
        /// </value>
        /// <remarks>
        ///    <para> A blank string ("") specifies a valid
        ///       zero-length password.</para>
        /// </remarks>
        public string Password
        {
            set
            {
                if (value != null)
                {
                    if (securePassword == null)
                    {
                        securePassword = new SecureString();
                        for (int i = 0; i < value.Length; i++)
                        {
                            securePassword.AppendChar(value[i]);
                        }
                    }
                    else
                    {
                        SecureString tempStr = new SecureString();
                        for (int i = 0; i < value.Length; i++)
                        {
                            tempStr.AppendChar(value[i]);
                        }
                        securePassword.Clear();
                        securePassword = tempStr.Copy();
                        FireIdentifierChanged();
                        tempStr.Dispose();
                    }
                }
                else
                {
                    if (securePassword != null)
                    {
                        securePassword.Dispose();
                        securePassword = null;
                        FireIdentifierChanged();
                    }
                }
            }
        }
        /// <summary>
        ///    <para>Sets the secure password for the specified user. The value can be set, but not retrieved.</para>
        /// </summary>
        /// <value>
        ///    <para> The default value is null. If the user name is also
        ///       null, the credentials used will be those of the currently logged-on user.</para>
        /// </value>
        /// <remarks>
        ///    <para> A blank securestring ("") specifies a valid
        ///       zero-length password.</para>
        /// </remarks>
        public SecureString SecurePassword
        {
            set
            {
                if (value != null)
                {
                    if (securePassword == null)
                    {
                        securePassword = value.Copy();
                    }
                    else
                    {
                        securePassword.Clear();
                        securePassword = value.Copy();
                        FireIdentifierChanged();
                    }
                }
                else
                {
                    if (securePassword != null)
                    {
                        securePassword.Dispose();
                        securePassword = null;
                        FireIdentifierChanged();
                    }
                }
            }
        }

        /// <summary>
        ///    <para>Gets or sets the authority to be used to authenticate the specified user.</para>
        /// </summary>
        /// <value>
        ///    <para>If not null, this property can contain the name of the
        ///       Windows NT/Windows 2000 domain in which to obtain the user to
        ///       authenticate.</para>
        /// </value>
        /// <remarks>
        ///    <para>
        ///       The property must be passed
        ///       as follows: If it begins with the string "Kerberos:", Kerberos
        ///       authentication will be used and this property should contain a Kerberos principal name. For
        ///       example, Kerberos:&lt;principal name&gt;.</para>
        ///    <para>If the property value begins with the string "NTLMDOMAIN:", NTLM
        ///       authentication will be used and the property should contain a NTLM domain name.
        ///       For example, NTLMDOMAIN:&lt;domain name&gt;. </para>
        ///    <para>If the property is null, NTLM authentication will be used and the NTLM domain
        ///       of the current user will be used.</para>
        /// </remarks>
        public string Authority
        {
            get { return authority ?? string.Empty; }
            set
            {
                if (authority != value)
                {
                    authority = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para>Gets or sets the COM impersonation level to be used for operations in this connection.</para>
        /// </summary>
        /// <value>
        ///    <para>The COM impersonation level to be used for operations in
        ///       this connection. The default value is <see cref='System.Management.ImpersonationLevel.Impersonate' qualify='true'/>, which indicates that the WMI provider can
        ///       impersonate the client when performing the requested operations in this connection.</para>
        /// </value>
        /// <remarks>
        /// <para>The <see cref='System.Management.ImpersonationLevel.Impersonate' qualify='true'/> setting is advantageous when the provider is
        ///    a trusted application or service. It eliminates the need for the provider to
        ///    perform client identity and access checks for the requested operations. However,
        ///    note that if for some reason the provider cannot be trusted, allowing it to
        ///    impersonate the client may constitute a security threat. In such cases, it is
        ///    recommended that this property be set by the client to a lower value, such as
        /// <see cref='System.Management.ImpersonationLevel. Identify' qualify='true'/>. Note that this may cause failure of the
        ///    provider to perform the requested operations, for lack of sufficient permissions
        ///    or inability to perform access checks.</para>
        /// </remarks>
        public ImpersonationLevel Impersonation
        {
            get { return impersonation; }
            set
            {
                if (impersonation != value)
                {
                    impersonation = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para>Gets or sets the COM authentication level to be used for operations in this connection.</para>
        /// </summary>
        /// <value>
        ///    <para>The COM authentication level to be used for operations
        ///       in this connection. The default value is <see cref='System.Management.AuthenticationLevel.Unchanged' qualify='true'/>, which indicates that the
        ///       client will use the authentication level requested by the server, according to
        ///       the standard DCOM negotiation process.</para>
        /// </value>
        /// <remarks>
        ///    <para>On Windows 2000 and below, the WMI service will request
        ///       Connect level authentication, while on Windows XP and higher it will request
        ///       Packet level authentication. If the client requires a specific authentication
        ///       setting, this property can be used to control the authentication level on this
        ///       particular connection. For example, the property can be set to <see cref='System.Management.AuthenticationLevel.PacketPrivacy' qualify='true'/>
        ///       if the
        ///       client requires all communication to be encrypted.</para>
        /// </remarks>
        public AuthenticationLevel Authentication
        {
            get { return authentication; }
            set
            {
                if (authentication != value)
                {
                    authentication = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether user privileges need to be enabled for
        ///       the connection operation. This property should only be used when the operation
        ///       performed requires a certain user privilege to be enabled
        ///       (for example, a machine reboot).</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if user privileges need to be
        ///    enabled for the connection operation; otherwise, <see langword='false'/>. The
        ///    default value is <see langword='false'/>.</para>
        /// </value>
        public bool EnablePrivileges
        {
            get { return enablePrivileges; }
            set
            {
                if (enablePrivileges != value)
                {
                    enablePrivileges = value;
                    FireIdentifierChanged();
                }
            }
        }

        //
        //Constructors
        //

        //default
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.ConnectionOptions'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ConnectionOptions'/> class for the connection operation, using default values. This is the
        ///    default constructor.</para>
        /// </summary>
        public ConnectionOptions() :
            this(DEFAULTLOCALE, null, (string)null, DEFAULTAUTHORITY,
                    DEFAULTIMPERSONATION, DEFAULTAUTHENTICATION,
                    DEFAULTENABLEPRIVILEGES, null, InfiniteTimeout)
        { }


        //parameterized
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ConnectionOptions'/> class to be used for a WMI
        ///    connection, using the specified values.</para>
        /// </summary>
        /// <param name='locale'>The locale to be used for the connection.</param>
        /// <param name=' username'>The user name to be used for the connection. If null, the credentials of the currently logged-on user are used.</param>
        /// <param name=' password'>The password for the given user name. If the user name is also null, the credentials used will be those of the currently logged-on user.</param>
        /// <param name=' authority'><para>The authority to be used to authenticate the specified user.</para></param>
        /// <param name=' impersonation'>The COM impersonation level to be used for the connection.</param>
        /// <param name=' authentication'>The COM authentication level to be used for the connection.</param>
        /// <param name=' enablePrivileges'><see langword='true'/>to enable special user privileges; otherwise, <see langword='false'/> . This parameter should only be used when performing an operation that requires special Windows NT user privileges.</param>
        /// <param name=' context'>A provider-specific, named value pairs object to be passed through to the provider.</param>
        /// <param name=' timeout'>Reserved for future use.</param>
        public ConnectionOptions(string locale,
                string username, string password, string authority,
                ImpersonationLevel impersonation, AuthenticationLevel authentication,
                bool enablePrivileges,
                ManagementNamedValueCollection context, TimeSpan timeout) : base(context, timeout)
        {
            if (locale != null)
                this.locale = locale;

            this.username = username;
            this.enablePrivileges = enablePrivileges;

            if (password != null)
            {
                this.securePassword = new SecureString();
                for (int i = 0; i < password.Length; i++)
                {
                    securePassword.AppendChar(password[i]);
                }
            }

            if (authority != null)
                this.authority = authority;

            if (impersonation != 0)
                this.impersonation = impersonation;

            if (authentication != 0)
                this.authentication = authentication;
        }
        //parameterized
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ConnectionOptions'/> class to be used for a WMI
        ///    connection, using the specified values.</para>
        /// </summary>
        /// <param name='locale'>The locale to be used for the connection.</param>
        /// <param name='username'>The user name to be used for the connection. If null, the credentials of the currently logged-on user are used.</param>
        /// <param name='password'>The secure password for the given user name. If the user name is also null, the credentials used will be those of the currently logged-on user.</param>
        /// <param name='authority'><para>The authority to be used to authenticate the specified user.</para></param>
        /// <param name='impersonation'>The COM impersonation level to be used for the connection.</param>
        /// <param name='authentication'>The COM authentication level to be used for the connection.</param>
        /// <param name='enablePrivileges'><see langword='true'/>to enable special user privileges; otherwise, <see langword='false'/> . This parameter should only be used when performing an operation that requires special Windows NT user privileges.</param>
        /// <param name='context'>A provider-specific, named value pairs object to be passed through to the provider.</param>
        /// <param name='timeout'>Reserved for future use.</param>
        public ConnectionOptions(string locale,
                string username, SecureString password, string authority,
                ImpersonationLevel impersonation, AuthenticationLevel authentication,
                bool enablePrivileges,
                ManagementNamedValueCollection context, TimeSpan timeout) : base(context, timeout)
        {
            if (locale != null)
                this.locale = locale;

            this.username = username;
            this.enablePrivileges = enablePrivileges;

            if (password != null)
            {
                this.securePassword = password.Copy();
            }

            if (authority != null)
                this.authority = authority;

            if (impersonation != 0)
                this.impersonation = impersonation;

            if (authentication != 0)
                this.authentication = authentication;
        }

        /// <summary>
        ///    <para> Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The cloned object.</para>
        /// </returns>
        public override object Clone()
        {
            ManagementNamedValueCollection newContext = null;

            if (null != Context)
                newContext = (ManagementNamedValueCollection)Context.Clone();
            return new ConnectionOptions(locale, username, GetSecurePassword(),
                    authority, impersonation, authentication, enablePrivileges, newContext, Timeout);
        }

        //
        //Methods
        //

        internal IntPtr GetPassword()
        {
            if (securePassword != null)
            {
                try
                {
                    return System.Runtime.InteropServices.Marshal.SecureStringToBSTR(securePassword);
                }
                catch (OutOfMemoryException)
                {
                    return IntPtr.Zero;
                }
            }
            else
                return IntPtr.Zero;
        }
        internal SecureString GetSecurePassword()
        {
            if (securePassword != null)
                return securePassword.Copy();
            else
                return null;
        }

        internal ConnectionOptions(ManagementNamedValueCollection context, TimeSpan timeout, int flags) : base(context, timeout, flags) { }

        internal ConnectionOptions(ManagementNamedValueCollection context) : base(context, InfiniteTimeout) { }

        internal static ConnectionOptions _Clone(ConnectionOptions options)
        {
            return ConnectionOptions._Clone(options, null);
        }

        internal static ConnectionOptions _Clone(ConnectionOptions options, IdentifierChangedEventHandler handler)
        {
            ConnectionOptions optionsTmp;

            if (options != null)
            {
                optionsTmp = new ConnectionOptions(options.Context, options.Timeout, options.Flags);

                optionsTmp.locale = options.locale;

                optionsTmp.username = options.username;
                optionsTmp.enablePrivileges = options.enablePrivileges;

                if (options.securePassword != null)
                {
                    optionsTmp.securePassword = options.securePassword.Copy();
                }
                else
                    optionsTmp.securePassword = null;

                if (options.authority != null)
                    optionsTmp.authority = options.authority;

                if (options.impersonation != 0)
                    optionsTmp.impersonation = options.impersonation;

                if (options.authentication != 0)
                    optionsTmp.authentication = options.authentication;
            }
            else
                optionsTmp = new ConnectionOptions();

            // Wire up change handler chain. Use supplied handler, if specified;
            // otherwise, default to that of the path argument.
            if (handler != null)
                optionsTmp.IdentifierChanged += handler;
            else if (options != null)
                optionsTmp.IdentifierChanged += new IdentifierChangedEventHandler(options.HandleIdentifierChange);

            return optionsTmp;
        }

    }//ConnectionOptions
}
