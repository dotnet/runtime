// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace System.Management
{

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Retrieves a collection of management objects based
    ///       on a specified query.</para>
    ///    <para>This class is one of the more commonly used entry points to retrieving
    ///       management information. For example, it can be used to enumerate all disk
    ///       drives, network adapters, processes and many more management objects on a
    ///       system, or to query for all network connections that are up, services that are
    ///       paused etc. </para>
    ///    <para>When instantiated, an instance of this class takes as input a WMI
    ///       query represented in an <see cref='System.Management.ObjectQuery'/> or it's derivatives, and optionally a <see cref='System.Management.ManagementScope'/> representing the WMI namespace
    ///       to execute the query in. It can also take additional advanced
    ///       options in an <see cref='System.Management.EnumerationOptions'/> object. When the Get() method on this object
    ///       is invoked, the ManagementObjectSearcher executes the given query in the
    ///       specified scope and returns a collection of management objects that match the
    ///       query in a <see cref='System.Management.ManagementObjectCollection'/>.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates perform a query using
    /// // ManagementObjectSearcher object.
    /// class Sample_ManagementObjectSearcher
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementObjectSearcher searcher = new
    ///             ManagementObjectSearcher("select * from win32_share");
    ///         foreach (ManagementObject share in searcher.Get()) {
    ///             Console.WriteLine("Share = " + share["Name"]);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates perform a query using
    /// ' ManagementObjectSearcher object.
    /// Class Sample_ManagementObjectSearcher
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim searcher As New ManagementObjectSearcher("SELECT * FROM Win32_Share")
    ///         Dim share As ManagementObject
    ///         For Each share In searcher.Get()
    ///             Console.WriteLine("Share = " &amp; share("Name").ToString())
    ///         Next share
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    [ToolboxItem(false)]
    public class ManagementObjectSearcher : Component
    {
        //fields
        private ManagementScope scope;
        private ObjectQuery query;
        private EnumerationOptions options;

        //default constructor
        /// <overload>
        ///    Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class. After some properties on
        ///    this object are set, the object can be used to invoke a query for management information. This is the default
        ///    constructor.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>ManagementObjectSearcher s = new ManagementObjectSearcher();
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementObjectSearcher()
        ///    </code>
        /// </example>
        public ManagementObjectSearcher() : this((ManagementScope)null, null, null) { }

        //parameterized constructors
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class used
        ///    to invoke the specified query for management information.</para>
        /// </summary>
        /// <param name='queryString'>The WMI query to be invoked by the object.</param>
        /// <example>
        ///    <code lang='C#'>ManagementObjectSearcher s =
        ///     new ManagementObjectSearcher("SELECT * FROM Win32_Service");
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementObjectSearcher("SELECT * FROM Win32_Service")
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(string queryString) : this(null, new ObjectQuery(queryString), null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class used to invoke the
        ///    specified query for management information.</para>
        /// </summary>
        /// <param name='query'>An <see cref='System.Management.ObjectQuery'/> representing the query to be invoked by the searcher.</param>
        /// <example>
        ///    <code lang='C#'>SelectQuery q = new SelectQuery("Win32_Service", "State='Running'");
        /// ManagementObjectSearcher s = new ManagementObjectSearcher(q);
        ///    </code>
        ///    <code lang='VB'>Dim q As New SelectQuery("Win32_Service", "State=""Running""")
        /// Dim s As New ManagementObjectSearcher(q)
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(ObjectQuery query) : this(null, query, null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class used to invoke the
        ///    specified query in the specified scope.</para>
        /// </summary>
        /// <param name='scope'>The scope in which to query.</param>
        /// <param name=' queryString'>The query to be invoked.</param>
        /// <remarks>
        /// <para>If no scope is specified, the default scope (<see cref='System.Management.ManagementPath.DefaultPath'/>) is used.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObjectSearcher s = new ManagementObjectSearcher(
        ///                                "root\\MyApp",
        ///                                "SELECT * FROM MyClass WHERE MyProp=5");
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementObjectSearcher( _
        ///                                "root\MyApp", _
        ///                                "SELECT * FROM MyClass WHERE MyProp=5")
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(string scope, string queryString) :
            this(new ManagementScope(scope), new ObjectQuery(queryString), null)
        { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class used to invoke the
        ///    specified query in the specified scope.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> representing the scope in which to invoke the query.</param>
        /// <param name=' query'>An <see cref='System.Management.ObjectQuery'/> representing the query to be invoked.</param>
        /// <remarks>
        /// <para>If no scope is specified, the default scope (<see cref='System.Management.ManagementPath.DefaultPath'/>) is
        ///    used.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementScope myScope = new ManagementScope("root\\MyApp");
        /// SelectQuery q = new SelectQuery("Win32_Environment", "User=&lt;system&gt;");
        /// ManagementObjectSearcher s = new ManagementObjectSearcher(myScope,q);
        ///    </code>
        ///    <code lang='VB'>Dim myScope As New ManagementScope("root\MyApp")
        /// Dim q As New SelectQuery("Win32_Environment", "User=&lt;system&gt;")
        /// Dim s As New ManagementObjectSearcher(myScope,q)
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(ManagementScope scope, ObjectQuery query) : this(scope, query, null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class used to invoke the specified
        ///    query, in the specified scope, and with the specified options.</para>
        /// </summary>
        /// <param name='scope'>The scope in which the query should be invoked.</param>
        /// <param name=' queryString'>The query to be invoked.</param>
        /// <param name=' options'>An <see cref='System.Management.EnumerationOptions'/> specifying additional options for the query.</param>
        /// <example>
        ///    <code lang='C#'>ManagementObjectSearcher s = new ManagementObjectSearcher(
        ///     "root\\MyApp",
        ///     "SELECT * FROM MyClass",
        ///     new EnumerationOptions(null, InfiniteTimeout, 1, true, false, true);
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementObjectSearcher( _
        ///     "root\MyApp", _
        ///     "SELECT * FROM MyClass", _
        ///     New EnumerationOptions(Null, InfiniteTimeout, 1, True, False, True)
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(string scope, string queryString, EnumerationOptions options) :
            this(new ManagementScope(scope), new ObjectQuery(queryString), options)
        { }
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObjectSearcher'/> class to be
        ///    used to invoke the specified query in the specified scope, with the specified
        ///    options.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> specifying the scope of the query</param>
        /// <param name=' query'>An <see cref='System.Management.ObjectQuery'/> specifying the query to be invoked</param>
        /// <param name=' options'>An <see cref='System.Management.EnumerationOptions'/> specifying additional options to be used for the query.</param>
        /// <example>
        ///    <code lang='C#'>ManagementScope scope = new ManagementScope("root\\MyApp");
        /// SelectQuery q = new SelectQuery("SELECT * FROM MyClass");
        /// EnumerationOptions o = new EnumerationOptions(null, InfiniteTimeout, 1, true, false, true);
        /// ManagementObjectSearcher s = new ManagementObjectSearcher(scope, q, o);
        ///    </code>
        ///    <code lang='VB'>Dim scope As New ManagementScope("root\MyApp")
        /// Dim q As New SelectQuery("SELECT * FROM MyClass")
        /// Dim o As New EnumerationOptions(Null, InfiniteTimeout, 1, True, False, True)
        /// Dim s As New ManagementObjectSearcher(scope, q, o)
        ///    </code>
        /// </example>
        public ManagementObjectSearcher(ManagementScope scope, ObjectQuery query, EnumerationOptions options)
        {
            this.scope = ManagementScope._Clone(scope);

            if (null != query)
                this.query = (ObjectQuery)query.Clone();
            else
                this.query = new ObjectQuery();

            if (null != options)
                this.options = (EnumerationOptions)options.Clone();
            else
                this.options = new EnumerationOptions();
        }


        //
        //Public Properties
        //

        /// <summary>
        ///    <para>Gets or sets the scope in which to look for objects (the scope represents a WMI namespace).</para>
        /// </summary>
        /// <value>
        ///    <para> The scope (namespace) in which to look for objects.</para>
        /// </value>
        /// <remarks>
        ///    <para>When the value of this property is changed,
        ///       the <see cref='System.Management.ManagementObjectSearcher'/>
        ///       is re-bound to the new scope.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObjectSearcher s = new ManagementObjectSearcher();
        /// s.Scope = new ManagementScope("root\\MyApp");
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementObjectSearcher()
        /// Dim ms As New ManagementScope ("root\MyApp")
        /// s.Scope = ms
        ///    </code>
        /// </example>
        public ManagementScope Scope
        {
            get
            {
                return scope;
            }
            set
            {
                if (null != value)
                    scope = (ManagementScope)value.Clone();
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        ///    <para> Gets or sets the query to be invoked in the
        ///       searcher (that is, the criteria to be applied to the search for management objects).</para>
        /// </summary>
        /// <value>
        ///    <para> The criteria to apply to the query.</para>
        /// </value>
        /// <remarks>
        /// <para>When the value of this property is changed, the <see cref='System.Management.ManagementObjectSearcher'/>
        /// is reset to use the new query.</para>
        /// </remarks>
        public ObjectQuery Query
        {
            get
            {
                return query;
            }
            set
            {
                if (null != value)
                    query = (ObjectQuery)value.Clone();
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        ///    <para>Gets or sets the options for how to search for objects.</para>
        /// </summary>
        /// <value>
        ///    <para>The options for how to search for objects.</para>
        /// </value>
        public EnumerationOptions Options
        {
            get
            {
                return options;
            }
            set
            {
                if (null != value)
                    options = (EnumerationOptions)value.Clone();
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        //********************************************
        //Get()
        //********************************************
        /// <overload>
        ///    Invokes the specified WMI query and returns the resulting collection.
        /// </overload>
        /// <summary>
        ///    <para>Invokes the specified WMI query and returns the
        ///       resulting collection.</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementObjectCollection'/> containing the objects that match the
        ///    specified query.</para>
        /// </returns>
        public ManagementObjectCollection Get()
        {
            Initialize();
            IEnumWbemClassObject ew = null;
            SecurityHandler securityHandler = scope.GetSecurityHandler();
            EnumerationOptions enumOptions = (EnumerationOptions)options.Clone();

            int status = (int)ManagementStatus.NoError;

            try
            {
                //If this is a simple SelectQuery (className only), and the enumerateDeep is set, we have
                //to find out whether this is a class enumeration or instance enumeration and call CreateInstanceEnum/
                //CreateClassEnum appropriately, because with ExecQuery we can't do a deep enumeration.
                if ((query.GetType() == typeof(SelectQuery)) &&
                    (((SelectQuery)query).Condition == null) &&
                    (((SelectQuery)query).SelectedProperties == null) &&
                    options.EnumerateDeep)
                {
                    //Need to make sure that we're not passing invalid flags to enumeration APIs.
                    //The only flags not valid for enumerations are EnsureLocatable & PrototypeOnly.
                    enumOptions.EnsureLocatable = false; enumOptions.PrototypeOnly = false;

                    if (((SelectQuery)query).IsSchemaQuery == false) //deep instance enumeration
                    {
                        status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).CreateInstanceEnum_(
                            ((SelectQuery)query).ClassName,
                            enumOptions.Flags,
                            enumOptions.GetContext(),
                            ref ew);
                    }
                    else //deep class enumeration
                    {
                        status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).CreateClassEnum_(((SelectQuery)query).ClassName,
                            enumOptions.Flags,
                            enumOptions.GetContext(),
                            ref ew);
                    }
                }
                else //we can use ExecQuery
                {
                    //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
                    enumOptions.EnumerateDeep = true;
                    status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecQuery_(
                        query.QueryLanguage,
                        query.QueryString,
                        enumOptions.Flags,
                        enumOptions.GetContext(),
                        ref ew);
                }
            }
            catch (COMException e)
            {
                ManagementException.ThrowWithExtendedInfo(e);
            }
            finally
            {
                securityHandler.Reset();
            }

            if ((status & 0xfffff000) == 0x80041000)
            {
                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
            }
            else if ((status & 0x80000000) != 0)
            {
                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            //Create a new collection object for the results

            return new ManagementObjectCollection(scope, options, ew);
        }//Get()


        //********************************************
        //Get() asynchronous
        //********************************************
        /// <summary>
        ///    <para>Invokes the WMI query, asynchronously, and binds to a watcher to deliver the results.</para>
        /// </summary>
        /// <param name='watcher'>The watcher that raises events triggered by the operation. </param>
        public void Get(ManagementOperationObserver watcher)
        {
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));

            Initialize();
            IWbemServices wbemServices = scope.GetIWbemServices();

            EnumerationOptions enumOptions = (EnumerationOptions)options.Clone();
            // Ensure we switch off ReturnImmediately as this is invalid for async calls
            enumOptions.ReturnImmediately = false;
            // If someone has registered for progress, make sure we flag it
            if (watcher.HaveListenersForProgress)
                enumOptions.SendStatus = true;

            WmiEventSink sink = watcher.GetNewSink(scope, enumOptions.Context);
            SecurityHandler securityHandler = scope.GetSecurityHandler();

            int status = (int)ManagementStatus.NoError;

            try
            {
                //If this is a simple SelectQuery (className only), and the enumerateDeep is set, we have
                //to find out whether this is a class enumeration or instance enumeration and call CreateInstanceEnum/
                //CreateClassEnum appropriately, because with ExecQuery we can't do a deep enumeration.
                if ((query.GetType() == typeof(SelectQuery)) &&
                    (((SelectQuery)query).Condition == null) &&
                    (((SelectQuery)query).SelectedProperties == null) &&
                    options.EnumerateDeep)
                {
                    //Need to make sure that we're not passing invalid flags to enumeration APIs.
                    //The only flags not valid for enumerations are EnsureLocatable & PrototypeOnly.
                    enumOptions.EnsureLocatable = false; enumOptions.PrototypeOnly = false;

                    if (((SelectQuery)query).IsSchemaQuery == false) //deep instance enumeration
                    {
                        status = scope.GetSecuredIWbemServicesHandler(wbemServices).CreateInstanceEnumAsync_(((SelectQuery)query).ClassName,
                            enumOptions.Flags,
                            enumOptions.GetContext(),
                            sink.Stub);
                    }
                    else
                    {
                        status = scope.GetSecuredIWbemServicesHandler(wbemServices).CreateClassEnumAsync_(((SelectQuery)query).ClassName,
                            enumOptions.Flags,
                            enumOptions.GetContext(),
                            sink.Stub);
                    }
                }
                else //we can use ExecQuery
                {
                    //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
                    enumOptions.EnumerateDeep = true;
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).ExecQueryAsync_(
                        query.QueryLanguage,
                        query.QueryString,
                        enumOptions.Flags,
                        enumOptions.GetContext(),
                        sink.Stub);
                }

            }
            catch (COMException e)
            {
                watcher.RemoveSink(sink);
                ManagementException.ThrowWithExtendedInfo(e);
            }
            finally
            {
                securityHandler.Reset();
            }

            if ((status & 0xfffff000) == 0x80041000)
            {
                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
            }
            else if ((status & 0x80000000) != 0)
            {
                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }


        private void Initialize()
        {
            //If the query is not set yet we can't do it
            if (null == query)
                throw new InvalidOperationException();

            //If we're not connected yet, this is the time to do it...
            lock (this)
            {
                if (null == scope)
                    scope = ManagementScope._Clone(null);
            }

            lock (scope)
            {
                if (!scope.IsConnected)
                    scope.Initialize();
            }
        }
    }
}
