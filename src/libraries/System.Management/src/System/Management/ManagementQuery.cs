// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.ComponentModel.Design.Serialization;
using System.ComponentModel;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Provides an abstract base class for all management query objects.</para>
    /// </summary>
    /// <remarks>
    ///    <para> This class is abstract; only
    ///       derivatives of it are actually used in the API.</para>
    /// </remarks>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    [TypeConverter(typeof(ManagementQueryConverter))]
    public abstract class ManagementQuery : ICloneable
    {
        internal const string DEFAULTQUERYLANGUAGE = "WQL";
        internal const string TokenSelect = "select ";    // Keep trailing space char.

        //Used when any public property on this object is changed, to signal
        //to the containing object that it needs to be refreshed.
        internal event IdentifierChangedEventHandler IdentifierChanged;

        //Fires IdentifierChanged event
        internal void FireIdentifierChanged()
        {
            IdentifierChanged?.Invoke(this, null);
        }

        private string queryLanguage;
        private string queryString;

        internal void SetQueryString(string qString)
        {
            queryString = qString;
        }

        //default constructor
        internal ManagementQuery() : this(DEFAULTQUERYLANGUAGE, null) { }

        //parameterized constructors
        internal ManagementQuery(string query) : this(DEFAULTQUERYLANGUAGE, query) { }
        internal ManagementQuery(string language, string query)
        {
            QueryLanguage = language;
            QueryString = query;
        }

        /// <summary>
        ///  Parses the query string and sets the property values accordingly.
        /// </summary>
        /// <param name="query">The query string to be parsed.</param>
        protected internal virtual void ParseQuery(string query) { }

        //
        //properties
        //
        /// <summary>
        ///    <para>Gets or sets the query in text format.</para>
        /// </summary>
        /// <value>
        ///    <para> If the query object is
        ///       constructed with no parameters, the property is null until specifically set. If the
        ///       object was constructed with a specified query, the property returns the specified
        ///       query string.</para>
        /// </value>
        public virtual string QueryString
        {
            get { return queryString ?? string.Empty; }
            set
            {
                if (queryString != value)
                {
                    ParseQuery(value); // this may throw
                    queryString = value;
                    FireIdentifierChanged();
                }
            }
        }

        /// <summary>
        ///    <para> Gets or sets the query language used in the query
        ///       string, defining the format of the query string.</para>
        /// </summary>
        /// <value>
        ///    <para>Can be set to any supported query
        ///       language. "WQL" is the only value supported intrinsically by WMI.</para>
        /// </value>
        public virtual string QueryLanguage
        {
            get { return queryLanguage ?? string.Empty; }
            set
            {
                if (queryLanguage != value)
                {
                    queryLanguage = value;
                    FireIdentifierChanged();
                }
            }
        }

        //ICloneable
        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        public abstract object Clone();

        internal static void ParseToken(ref string q, string token, string op, ref bool bTokenFound, ref string tokenValue)
        {
            if (bTokenFound)
                throw new ArgumentException(SR.InvalidQueryDuplicatedToken);    // Invalid query - duplicate token

            bTokenFound = true;
            q = q.Remove(0, token.Length).TrimStart(null);

            // Next character should be the operator if any
            if (op != null)
            {
                if (0 != q.IndexOf(op, StringComparison.Ordinal))
                    throw new ArgumentException(SR.InvalidQuery);    // Invalid query

                // Strip off the op and any leading WS
                q = q.Remove(0, op.Length).TrimStart(null);
            }

            if (0 == q.Length)
                throw new ArgumentException(SR.InvalidQueryNullToken);        // Invalid query - token has no value

            // Next token should be the token value - look for terminating WS
            // or end of string
            int i;
            if (-1 == (i = q.IndexOf(' ')))
                i = q.Length;            // No WS => consume entire string

            tokenValue = q.Substring(0, i);
            q = q.Remove(0, tokenValue.Length).TrimStart(null);
        }

        internal static void ParseToken(ref string q, string token, ref bool bTokenFound)
        {
            if (bTokenFound)
                throw new ArgumentException(SR.InvalidQueryDuplicatedToken);    // Invalid query - duplicate token

            bTokenFound = true;
            q = q.Remove(0, token.Length).TrimStart(null);
        }

    }//ManagementQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a management query that returns instances or classes.</para>
    /// </summary>
    /// <remarks>
    ///    <para>This class or its derivatives are used to specify a
    ///       query in the <see cref='System.Management.ManagementObjectSearcher'/>. Use
    ///       a more specific query class whenever possible.</para>
    /// </remarks>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates creating a query.
    ///
    /// class Sample_ObjectQuery
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         ObjectQuery objectQuery = new ObjectQuery("select * from Win32_Share");
    ///         ManagementObjectSearcher searcher =
    ///             new ManagementObjectSearcher(objectQuery);
    ///         foreach (ManagementObject share in searcher.Get())
    ///         {
    ///             Console.WriteLine("Share = " + share["Name"]);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates creating a query.
    ///
    /// Class Sample_ObjectQuery
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim objectQuery As New ObjectQuery("select * from Win32_Share")
    ///         Dim searcher As New ManagementObjectSearcher(objectQuery)
    ///         Dim share As ManagementObject
    ///         For Each share In searcher.Get()
    ///             Console.WriteLine("Share = " &amp; share("Name"))
    ///         Next share
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ObjectQuery : ManagementQuery
    {
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.ObjectQuery'/>
        /// class.</para>
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ObjectQuery'/>
        /// class with no initialized values. This
        /// is the default constructor.</para>
        /// </summary>
        public ObjectQuery() : base() { }
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ObjectQuery'/>
        /// class
        /// for a specific query string.</para>
        /// </summary>
        /// <param name='query'>The string representation of the query.</param>
        public ObjectQuery(string query) : base(query) { }
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ObjectQuery'/>
        /// class for a specific
        /// query string and language.</para>
        /// </summary>
        /// <param name='language'>The query language in which this query is specified.</param>
        /// <param name=' query'>The string representation of the query.</param>
        public ObjectQuery(string language, string query) : base(language, query) { }

        //ICloneable
        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        public override object Clone()
        {
            return new ObjectQuery(QueryLanguage, QueryString);
        }

    }//ObjectQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WMI event query.</para>
    /// </summary>
    /// <remarks>
    ///    <para> Objects of this class or its derivatives are used in
    ///    <see cref='System.Management.ManagementEventWatcher'/> to subscribe to
    ///       WMI events. Use more specific derivatives of this class whenever possible.</para>
    /// </remarks>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to subscribe to an event
    /// // using the EventQuery object.
    ///
    /// class Sample_EventQuery
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         //For this example, we make sure we have an arbitrary class on root\default
    ///         ManagementClass newClass = new ManagementClass(
    ///             "root\\default",
    ///             String.Empty,
    ///             null);
    ///         newClass["__Class"] = "TestWql";
    ///         newClass.Put();
    ///
    ///         //Create a query object for watching for class deletion events
    ///         EventQuery eventQuery = new EventQuery("select * from __classdeletionevent");
    ///
    ///         //Initialize an event watcher object with this query
    ///         ManagementEventWatcher watcher = new ManagementEventWatcher(
    ///             new ManagementScope("root/default"),
    ///             eventQuery);
    ///
    ///         //Set up a handler for incoming events
    ///         MyHandler handler = new MyHandler();
    ///         watcher.EventArrived += new EventArrivedEventHandler(handler.Arrived);
    ///
    ///         //Start watching for events
    ///         watcher.Start();
    ///
    ///         //For this example, we delete the class to trigger an event
    ///         newClass.Delete();
    ///
    ///         //Nothing better to do - we loop to wait for an event to arrive.
    ///         while (!handler.IsArrived) {
    ///              System.Threading.Thread.Sleep(1000);
    ///         }
    ///
    ///         //In this example we only want to wait for one event, so we can stop watching
    ///         watcher.Stop();
    ///
    ///         //Get some values from the event.
    ///         //Note: this can also be done in the event handler.
    ///         ManagementBaseObject eventArg =
    ///             (ManagementBaseObject)(handler.ReturnedArgs.NewEvent["TargetClass"]);
    ///         Console.WriteLine("Class Deleted = " + eventArg["__CLASS"]);
    ///
    ///         return 0;
    ///     }
    ///
    ///     public class MyHandler
    ///     {
    ///         private bool isArrived = false;
    ///         private EventArrivedEventArgs args;
    ///
    ///         //Handles the event when it arrives
    ///         public void Arrived(object sender, EventArrivedEventArgs e) {
    ///             args = e;
    ///             isArrived = true;
    ///         }
    ///
    ///         //Public property to get at the event information stored in the handler
    ///         public EventArrivedEventArgs ReturnedArgs {
    ///             get {
    ///                 return args;
    ///             }
    ///         }
    ///
    ///         //Used to determine whether the event has arrived or not.
    ///         public bool IsArrived {
    ///             get {
    ///                 return isArrived;
    ///             }
    ///         }
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to subscribe an event
    /// ' using the EventQuery object.
    ///
    /// Class Sample_EventQuery
    ///     Public Shared Sub Main()
    ///
    ///         'For this example, we make sure we have an arbitrary class on root\default
    ///         Dim newClass As New ManagementClass( _
    ///             "root\default", _
    ///             String.Empty, Nothing)
    ///             newClass("__Class") = "TestWql"
    ///             newClass.Put()
    ///
    ///         'Create a query object for watching for class deletion events
    ///         Dim eventQuery As New EventQuery("select * from __classdeletionevent")
    ///
    ///         'Initialize an event watcher object with this query
    ///         Dim watcher As New ManagementEventWatcher( _
    ///             New ManagementScope("root/default"), _
    ///             eventQuery)
    ///
    ///         'Set up a handler for incoming events
    ///         Dim handler As New MyHandler()
    ///         AddHandler watcher.EventArrived, AddressOf handler.Arrived
    ///
    ///         'Start watching for events
    ///         watcher.Start()
    ///
    ///         'For this example, we delete the class to trigger an event
    ///         newClass.Delete()
    ///
    ///         'Nothing better to do - we loop to wait for an event to arrive.
    ///         While Not handler.IsArrived
    ///             Console.Write("0")
    ///             System.Threading.Thread.Sleep(1000)
    ///         End While
    ///
    ///         'In this example we only want to wait for one event, so we can stop watching
    ///         watcher.Stop()
    ///
    ///         'Get some values from the event
    ///         'Note: this can also be done in the event handler.
    ///         Dim eventArg As ManagementBaseObject = CType( _
    ///             handler.ReturnedArgs.NewEvent("TargetClass"), _
    ///             ManagementBaseObject)
    ///         Console.WriteLine(("Class Deleted = " + eventArg("__CLASS")))
    ///
    ///     End Sub
    ///
    ///     Public Class MyHandler
    ///         Private _isArrived As Boolean = False
    ///         Private args As EventArrivedEventArgs
    ///
    ///         'Handles the event when it arrives
    ///         Public Sub Arrived(sender As Object, e As EventArrivedEventArgs)
    ///             args = e
    ///             _isArrived = True
    ///         End Sub
    ///
    ///         'Public property to get at the event information stored in the handler
    ///         Public ReadOnly Property ReturnedArgs() As EventArrivedEventArgs
    ///             Get
    ///                 Return args
    ///             End Get
    ///         End Property
    ///
    ///         'Used to determine whether the event has arrived or not.
    ///         Public ReadOnly Property IsArrived() As Boolean
    ///             Get
    ///                 Return _isArrived
    ///             End Get
    ///         End Property
    ///     End Class
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class EventQuery : ManagementQuery
    {
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.EventQuery'/>
        /// class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.EventQuery'/>
        /// class. This is the
        /// default constructor.</para>
        /// </summary>
        public EventQuery() : base() { }
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.EventQuery'/>
        /// class for the specified query.</para>
        /// </summary>
        /// <param name='query'>A textual representation of the event query.</param>
        public EventQuery(string query) : base(query) { }
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.EventQuery'/>
        /// class for the specified
        /// language and query.</para>
        /// </summary>
        /// <param name='language'>The language in which the query string is specified. </param>
        /// <param name=' query'>The string representation of the query.</param>
        public EventQuery(string language, string query) : base(language, query) { }

        //ICloneable
        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        public override object Clone()
        {
            return new EventQuery(QueryLanguage, QueryString);
        }
    }//EventQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WMI data query in WQL format.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to use a WqlObjectQuery class to
    /// // perform an object query.
    ///
    /// class Sample_WqlObjectQuery
    /// {
    ///     public static int Main(string[] args) {
    ///         WqlObjectQuery objectQuery = new WqlObjectQuery("select * from Win32_Share");
    ///         ManagementObjectSearcher searcher =
    ///             new ManagementObjectSearcher(objectQuery);
    ///
    ///         foreach (ManagementObject share in searcher.Get()) {
    ///             Console.WriteLine("Share = " + share["Name"]);
    ///         }
    ///
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrate how to use a WqlObjectQuery class to
    /// ' perform an object query.
    ///
    /// Class Sample_WqlObjectQuery
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim objectQuery As New WqlObjectQuery("select * from Win32_Share")
    ///         Dim searcher As New ManagementObjectSearcher(objectQuery)
    ///
    ///         Dim share As ManagementObject
    ///         For Each share In searcher.Get()
    ///             Console.WriteLine("Share = " &amp; share("Name"))
    ///         Next share
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class WqlObjectQuery : ObjectQuery
    {
        //constructors
        //Here we don't take a language argument but hard-code it to WQL in the base class
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.WqlObjectQuery'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.WqlObjectQuery'/> class. This is the
        ///    default constructor.</para>
        /// </summary>
        public WqlObjectQuery() : base(null) { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlObjectQuery'/> class initialized to the
        ///    specified query.</para>
        /// </summary>
        /// <param name='query'><para> The representation of the data query.</para></param>
        public WqlObjectQuery(string query) : base(query) { }

        //QueryLanguage property is read-only in this class (does this work ??)
        /// <summary>
        ///    <para>Gets or sets the language of the query.</para>
        /// </summary>
        /// <value>
        ///    <para> The value of this
        ///       property is always "WQL".</para>
        /// </value>
        public override string QueryLanguage
        {
            get
            { return base.QueryLanguage; }
        }

        //ICloneable
        /// <summary>
        ///    <para>Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The copied object.
        /// </returns>
        public override object Clone()
        {
            return new WqlObjectQuery(QueryString);
        }


    }//WqlObjectQuery




    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WQL SELECT data query.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to perform a WQL select query.
    ///
    /// class Sample_SelectQuery
    /// {
    ///     public static int Main(string[] args) {
    ///         SelectQuery selectQuery = new SelectQuery("win32_logicaldisk");
    ///         ManagementObjectSearcher searcher =
    ///             new ManagementObjectSearcher(selectQuery);
    ///
    ///         foreach (ManagementObject disk in searcher.Get()) {
    ///             Console.WriteLine(disk.ToString());
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to perform a WQL select query.
    ///
    /// Class Sample_SelectQuery
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim selectQuery As New SelectQuery("win32_logicaldisk")
    ///         Dim searcher As New ManagementObjectSearcher(selectQuery)
    ///
    ///         Dim disk As ManagementObject
    ///         For Each disk In  searcher.Get()
    ///             Console.WriteLine(disk.ToString())
    ///         Next disk
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class SelectQuery : WqlObjectQuery
    {
        private bool isSchemaQuery;
        private string className;
        private string condition;
        private StringCollection selectedProperties;

        //default constructor
        /// <overload>
        /// <para>Initializes a new instance of the <see cref='System.Management.SelectQuery'/>
        /// class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.SelectQuery'/>
        /// class. This is the
        /// default constructor.</para>
        /// </summary>
        public SelectQuery() : this(null) { }

        //parameterized constructors
        //ISSUE : We have 2 possible constructors that take a single string :
        //  one that takes the full query string and the other that takes the class name.
        //  We resolve this by trying to parse the string, if it succeeds we assume it's the query, if
        //  not we assume it's the class name.
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.SelectQuery'/> class for the specified
        ///    query or the specified class name.</para>
        /// </summary>
        /// <param name='queryOrClassName'>The entire query or the class name to use in the query. The parser in this class attempts to parse the string as a valid WQL SELECT query. If the parser is unsuccessful, it assumes the string is a class name.</param>
        /// <example>
        ///    <code lang='C#'>SelectQuery s = new SelectQuery("SELECT * FROM Win32_Service WHERE State='Stopped');
        ///
        /// or
        ///
        /// //This is equivalent to "SELECT * FROM Win32_Service"
        /// SelectQuery s = new SelectQuery("Win32_Service");
        ///    </code>
        ///    <code lang='VB'>Dim s As New SelectQuery("SELECT * FROM Win32_Service WHERE State='Stopped')
        ///
        /// or
        ///
        /// //This is equivalent to "SELECT * FROM Win32_Service"
        /// Dim s As New SelectQuery("Win32_Service")
        ///    </code>
        /// </example>
        public SelectQuery(string queryOrClassName)
        {
            selectedProperties = new StringCollection();

            if (null != queryOrClassName)
            {
                // Minimally determine if the string is a query or class name.
                //
                if (queryOrClassName.TrimStart().StartsWith(TokenSelect, StringComparison.OrdinalIgnoreCase))
                {
                    // Looks to be a query - do further checking.
                    //
                    QueryString = queryOrClassName;        // Parse/validate; may throw.
                }
                else
                {
                    // Do some basic sanity checking on whether it's a class name
                    //

                    ManagementPath p = new ManagementPath(queryOrClassName);

                    if (p.IsClass && (p.NamespacePath.Length == 0))
                        ClassName = queryOrClassName;
                    else
                        throw new ArgumentException(SR.InvalidQuery, nameof(queryOrClassName));

                }
            }
        }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.SelectQuery'/>
        /// class with the specified
        /// class name and condition.</para>
        /// </summary>
        /// <param name='className'>The name of the class to select in the query.</param>
        /// <param name=' condition'>The condition to be applied in the query.</param>
        /// <example>
        ///    <code lang='C#'>SelectQuery s = new SelectQuery("Win32_Process", "HandleID=1234");
        ///    </code>
        ///    <code lang='VB'>Dim s As New SelectQuery("Win32_Process", "HandleID=1234")
        ///    </code>
        /// </example>
        public SelectQuery(string className, string condition) : this(className, condition, null) { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.SelectQuery'/>
        /// class with the specified
        /// class name and condition, selecting only the specified properties.</para>
        /// </summary>
        /// <param name='className'>The name of the class from which to select.</param>
        /// <param name='condition'>The condition to be applied to instances of the selected class.</param>
        /// <param name='selectedProperties'>An array of property names to be returned in the query results.</param>
        /// <example>
        ///    <code lang='C#'>String[] properties = {"VariableName", "VariableValue"};
        ///
        /// SelectQuery s = new SelectQuery("Win32_Environment",
        ///                                 "User='&lt;system&gt;'",
        ///                                 properties);
        ///    </code>
        ///    <code lang='VB'>Dim properties As String[] = {"VariableName", "VariableValue"}
        ///
        /// Dim s As New SelectQuery("Win32_Environment", _
        ///                          "User=""&lt;system&gt;""", _
        ///                          properties)
        ///    </code>
        /// </example>
        public SelectQuery(string className, string condition, string[] selectedProperties) : base()
        {
            this.isSchemaQuery = false;
            this.className = className;
            this.condition = condition;
            this.selectedProperties = new StringCollection();

            if (null != selectedProperties)
                this.selectedProperties.AddRange(selectedProperties);

            BuildQuery();
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.SelectQuery'/>
        /// class for a schema query, optionally specifying a condition. For schema queries,
        /// only the <paramref name="condition"/> parameter is valid.</para>
        /// </summary>
        /// <param name='isSchemaQuery'><see langword='true'/>to indicate that this is a schema query; otherwise, <see langword='false'/>. A <see langword='false'/> value is invalid in this constructor.</param>
        /// <param name='condition'>The condition to be applied to form the result set of classes.</param>
        /// <example>
        ///    <code lang='C#'>SelectQuery s = new SelectQuery(true, "__CLASS = 'Win32_Service'");
        ///    </code>
        ///    <code lang='VB'>Dim s As New SelectQuery(true, "__CLASS = ""Win32_Service""")
        ///    </code>
        /// </example>
        public SelectQuery(bool isSchemaQuery, string condition) : base()
        {
            if (isSchemaQuery == false)
                throw new ArgumentException(SR.InvalidQuery, nameof(isSchemaQuery));

            this.isSchemaQuery = true;
            this.className = null;
            this.condition = condition;
            this.selectedProperties = null;

            BuildQuery();
        }


        /// <summary>
        /// <para>Gets or sets the query in the <see cref='System.Management.SelectQuery'/>, in string form.</para>
        /// </summary>
        /// <value>
        ///    <para>A string representing the query.</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this
        ///       property value overrides any previous value stored in the object. In addition, setting this
        ///       property causes the other members of the object to be updated when the string
        ///       is reparsed.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>SelectQuery s = new SelectQuery();
        /// s.QueryString = "SELECT * FROM Win32_LogicalDisk";
        ///    </code>
        ///    <code lang='VB'>Dim s As New SelectQuery()
        /// s.QueryString = "SELECT * FROM Win32_LogicalDisk"
        ///    </code>
        /// </example>
        public override string QueryString
        {
            get
            {
                // We need to force a rebuild as we may not have detected
                // a change to selected properties
                BuildQuery();
                return base.QueryString;
            }
            set
            {
                base.QueryString = value;
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether this query is a schema query or an instances query.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this query
        ///    should be evaluated over the schema; <see langword='false'/> if the query should
        ///    be evaluated over instances.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new query type.</para>
        /// </remarks>
        public bool IsSchemaQuery
        {
            get
            { return isSchemaQuery; }
            set
            { isSchemaQuery = value; BuildQuery(); FireIdentifierChanged(); }
        }


        /// <summary>
        ///    <para>Gets or sets the class name to be selected from in the query.</para>
        /// </summary>
        /// <value>
        ///    <para>A string representing the name of the
        ///       class.</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value
        ///       overrides any previous value stored in the object. The query string is
        ///       rebuilt to reflect the new class name.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>SelectQuery s = new SelectQuery("SELECT * FROM Win32_LogicalDisk");
        /// Console.WriteLine(s.QueryString); //output is : SELECT * FROM Win32_LogicalDisk
        ///
        /// s.ClassName = "Win32_Process";
        /// Console.WriteLine(s.QueryString); //output is : SELECT * FROM Win32_Process
        ///    </code>
        ///    <code lang='VB'>Dim s As New SelectQuery("SELECT * FROM Win32_LogicalDisk")
        /// Console.WriteLine(s.QueryString)  'output is : SELECT * FROM Win32_LogicalDisk
        ///
        /// s.ClassName = "Win32_Process"
        /// Console.WriteLine(s.QueryString)  'output is : SELECT * FROM Win32_Process
        ///    </code>
        /// </example>
        public string ClassName
        {
            get { return className ?? string.Empty; }
            set { className = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the condition to be applied in the SELECT
        ///       query.</para>
        /// </summary>
        /// <value>
        ///    A string containing the condition to
        ///    be applied in the SELECT query.
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value
        ///       stored in the object. The query string is rebuilt to reflect the new
        ///       condition.</para>
        /// </remarks>
        public string Condition
        {
            get { return condition ?? string.Empty; }
            set { condition = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para> Gets or sets an array of property names to be
        ///       selected in the query.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Collections.Specialized.StringCollection'/> containing the names of the
        ///    properties to be selected in the query.</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value stored
        ///       in the object. The query string is rebuilt to reflect the new
        ///       properties.</para>
        /// </remarks>
        public StringCollection SelectedProperties
        {
            get { return selectedProperties; }
            set
            {
                if (null != value)
                {
                    // A tad painful since StringCollection doesn't support ICloneable
                    StringCollection src = (StringCollection)value;
                    StringCollection dst = new StringCollection();

                    foreach (string s in src)
                        dst.Add(s);

                    selectedProperties = dst;
                }
                else
                    selectedProperties = new StringCollection();

                BuildQuery();
                FireIdentifierChanged();
            }
        }

        /// <summary>
        ///  Builds the query string according to the current property values.
        /// </summary>
        protected internal void BuildQuery()
        {
            string s;

            if (isSchemaQuery == false) //this is an instances query
            {
                //If the class name is not set we can't build a query
                //Shouldn't throw here because the user may be in the process of filling in the properties...
                if (className == null)
                    SetQueryString(string.Empty);

                if ((className == null) || (className.Length == 0))
                    return;

                //Select clause
                s = TokenSelect;

                //If properties are specified list them
                if ((null != selectedProperties) && (0 < selectedProperties.Count))
                {
                    int count = selectedProperties.Count;

                    for (int i = 0; i < count; i++)
                        s = s + selectedProperties[i] + ((i == (count - 1)) ? " " : ",");
                }
                else
                    s += "* ";

                //From clause
                s = s + "from " + className;

            }
            else //this is a schema query, ignore className or selectedProperties.
            {
                //Select clause
                s = "select * from meta_class";
            }

            //Where clause
            if ((Condition != null) && (Condition.Length != 0))
                s = s + " where " + condition;

            //Set the queryString member to the built query (NB: note we set
            //by accessing the internal helper function rather than the property,
            //since we do not want to force a parse of a query we just built).
            SetQueryString(s);
        }


        /// <summary>
        ///  Parses the query string and sets the property values accordingly.
        /// </summary>
        /// <param name="query">The query string to be parsed.</param>
        protected internal override void ParseQuery(string query)
        {
            //Clear out previous property values
            className = null;
            condition = null;
            selectedProperties?.Clear();

            //Trim whitespaces
            string q = query.Trim();
            bool bFound = false; string tempProp; int i;

            if (isSchemaQuery == false) //instances query
            {
                //Find "select" clause and get the property list if exists
                string keyword = TokenSelect;
                if ((q.Length >= keyword.Length) && (string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0)) //select clause found
                {
                    ParseToken(ref q, keyword, ref bFound);
                    if (q[0] != '*') //we have properties
                    {
                        if (null != selectedProperties)
                            selectedProperties.Clear();
                        else
                            selectedProperties = new StringCollection();

                        //get the property list
                        while (true)
                        {
                            if ((i = q.IndexOf(',')) > 0)
                            {
                                tempProp = q.Substring(0, i);
                                q = q.Remove(0, i + 1).TrimStart(null);
                                tempProp = tempProp.Trim();
                                if (tempProp.Length > 0)
                                    selectedProperties.Add(tempProp);
                            }
                            else
                            { //last property in the list
                                if ((i = q.IndexOf(' ')) > 0)
                                {
                                    tempProp = q.Substring(0, i);
                                    q = q.Remove(0, i).TrimStart(null);
                                    selectedProperties.Add(tempProp);
                                    break;
                                }
                                else //bad query
                                    throw new ArgumentException(SR.InvalidQuery);
                            }
                        } //while
                    }
                    else
                        q = q.Remove(0, 1).TrimStart(null);
                }
                else //select clause has to be there, otherwise the parsing fails
                    throw new ArgumentException(SR.InvalidQuery);

                //Find "from" clause, get the class name and remove the clause
                keyword = "from "; bFound = false;
                if ((q.Length >= keyword.Length) && (string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0)) //from clause found
                    ParseToken(ref q, keyword, null, ref bFound, ref className);
                else //from clause has to be there, otherwise the parsing fails
                    throw new ArgumentException(SR.InvalidQuery);

                //Find "where" clause, get the condition out and remove the clause
                keyword = "where ";
                if ((q.Length >= keyword.Length) && (string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) == 0)) //where clause exists
                {
                    condition = q.Substring(keyword.Length).Trim();
                }
            } //if isSchemaQuery == false
            else //this is a schema query
            {
                //Find "select" clause and make sure it's the right syntax
                string keyword = "select";

                // Should start with "select"
                if ((q.Length < keyword.Length) ||
                    (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));

                q = q.Remove(0, keyword.Length).TrimStart(null);

                // Next should be a '*'
                if (0 != q.IndexOf('*', 0))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, "*"), nameof(query));

                q = q.Remove(0, 1).TrimStart(null);

                // Next should be "from"
                keyword = "from";

                if ((q.Length < keyword.Length) ||
                    (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));

                q = q.Remove(0, keyword.Length).TrimStart(null);

                // Next should be "meta_class"
                keyword = "meta_class";

                if ((q.Length < keyword.Length) ||
                    (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));

                q = q.Remove(0, keyword.Length).TrimStart(null);

                // There may be a where clause
                if (0 < q.Length)
                {
                    //Find "where" clause, and get the condition out
                    keyword = "where";

                    if ((q.Length < keyword.Length) ||
                        (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                        throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));

                    q = q.Remove(0, keyword.Length);

                    // Must be some white space next
                    if ((0 == q.Length) || !char.IsWhiteSpace(q[0]))
                        throw new ArgumentException(SR.InvalidQuery);    // Invalid query

                    q = q.TrimStart(null);    // Remove the leading whitespace

                    condition = q;
                }
                else
                    condition = string.Empty;

                //Empty not-applicable properties
                className = null;
                selectedProperties = null;
            }//schema query
        }

        /// <summary>
        ///    <para> Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The copied object.
        /// </returns>
        public override object Clone()
        {
            string[] strArray = null;

            if (null != selectedProperties)
            {
                int count = selectedProperties.Count;

                if (0 < count)
                {
                    strArray = new string[count];
                    selectedProperties.CopyTo(strArray, 0);
                }
            }

            if (isSchemaQuery == false)
                return new SelectQuery(className, condition, strArray);
            else
                return new SelectQuery(true, condition);
        }

    }//SelectQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WQL ASSOCIATORS OF data query.
    ///       It can be used for both instances and schema queries.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to query all instances associated
    /// // with Win32_LogicalDisk='C:'.
    ///
    /// class Sample_RelatedObjectQuery
    /// {
    ///     public static int Main(string[] args) {
    ///
    ///         //This query requests all objects related to the 'C:' drive.
    ///         RelatedObjectQuery relatedQuery =
    ///             new RelatedObjectQuery("win32_logicaldisk='c:'");
    ///         ManagementObjectSearcher searcher =
    ///             new ManagementObjectSearcher(relatedQuery);
    ///
    ///         foreach (ManagementObject relatedObject in searcher.Get()) {
    ///             Console.WriteLine(relatedObject.ToString());
    ///         }
    ///
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to query all instances associated
    /// ' with Win32_LogicalDisk='C:'.
    ///
    /// Class Sample_RelatedObjectQuery
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///
    ///         'This query requests all objects related to the 'C:' drive.
    ///         Dim relatedQuery As New RelatedObjectQuery("win32_logicaldisk='c:'")
    ///         Dim searcher As New ManagementObjectSearcher(relatedQuery)
    ///
    ///         Dim relatedObject As ManagementObject
    ///         For Each relatedObject In  searcher.Get()
    ///             Console.WriteLine(relatedObject.ToString())
    ///         Next relatedObject
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class RelatedObjectQuery : WqlObjectQuery
    {
        private const string TokenAssociators = "associators";
        private const string TokenOf = "of";
        private const string TokenWhere = "where";
        private const string TokenResultClass = "resultclass";
        private const string TokenAssocClass = "assocclass";
        private const string TokenResultRole = "resultrole";
        private const string TokenRole = "role";
        private const string TokenRequiredQualifier = "requiredqualifier";
        private const string TokenRequiredAssocQualifier = "requiredassocqualifier";
        private const string TokenClassDefsOnly = "classdefsonly";
        private const string TokenSchemaOnly = "schemaonly";

        private bool isSchemaQuery;
        private string sourceObject;
        private string relatedClass;
        private string relationshipClass;
        private string relatedQualifier;
        private string relationshipQualifier;
        private string relatedRole;
        private string thisRole;
        private bool classDefinitionsOnly;


        //default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.RelatedObjectQuery'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelatedObjectQuery'/> class. This is the
        ///    default constructor.</para>
        /// </summary>
        public RelatedObjectQuery() : this(null) { }

        //parameterized constructor
        //ISSUE : We have 2 possible constructors that take a single string :
        //  one that takes the full query string and the other that takes the source object path.
        //  We resolve this by trying to parse the string, if it succeeds we assume it's the query, if
        //  not we assume it's the source object.
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelatedObjectQuery'/>class. If the specified string can be successfully parsed as
        ///    a WQL query, it is considered to be the query string; otherwise, it is assumed to be the path of the source
        ///    object for the query. In this case, the query is assumed to be an instance query. </para>
        /// </summary>
        /// <param name='queryOrSourceObject'>The query string or the path of the source object.</param>
        /// <example>
        ///    <code lang='C#'>//This query retrieves all objects related to the 'mymachine' computer system
        /// //It specifies the full query string in the constructor
        /// RelatedObjectQuery q =
        ///     new RelatedObjectQuery("associators of {Win32_ComputerSystem.Name='mymachine'}");
        ///
        /// //or
        ///
        /// //This query retrieves all objects related to the 'Alerter' service
        /// //It specifies only the object of interest in the constructor
        /// RelatedObjectQuery q =
        ///     new RelatedObjectQuery("Win32_Service.Name='Alerter'");
        ///    </code>
        ///    <code lang='VB'>'This query retrieves all objects related to the 'mymachine' computer system
        /// 'It specifies the full query string in the constructor
        /// Dim q As New RelatedObjectQuery("associators of {Win32_ComputerSystem.Name='mymachine'}")
        ///
        /// 'or
        ///
        /// 'This query retrieves all objects related to the 'Alerter' service
        /// 'It specifies only the object of interest in the constructor
        /// Dim q As New RelatedObjectQuery("Win32_Service.Name='Alerter'")
        ///    </code>
        /// </example>
        public RelatedObjectQuery(string queryOrSourceObject)
        {
            if (null != queryOrSourceObject)
            {
                // Minimally determine if the string is a query or instance name.
                //
                if (queryOrSourceObject.TrimStart().StartsWith(TokenAssociators, StringComparison.OrdinalIgnoreCase))
                {
                    // Looks to be a query - do further checking.
                    //
                    QueryString = queryOrSourceObject;    // Parse/validate; may throw.
                }
                else
                {
                    // We'd like to treat it as the source object. Is it a valid
                    // class or instance?
                    //
                    // Do some basic sanity checking on whether it's a class/instance name
                    //

                    ManagementPath p = new ManagementPath(queryOrSourceObject);

                    if ((p.IsClass || p.IsInstance) && (p.NamespacePath.Length == 0))
                    {
                        SourceObject = queryOrSourceObject;
                        isSchemaQuery = false;
                    }
                    else
                        throw new ArgumentException(SR.InvalidQuery, nameof(queryOrSourceObject));
                }
            }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelatedObjectQuery'/> class for the given source object and related class.
        ///    The query is assumed to be an instance query (as opposed to a schema query).</para>
        /// </summary>
        /// <param name='sourceObject'>The path of the source object for this query.</param>
        /// <param name='relatedClass'>The related objects class.</param>
        public RelatedObjectQuery(string sourceObject, string relatedClass) : this(sourceObject, relatedClass,
                                                                                    null, null, null, null, null, false)
        { }

        //Do we need additional variants of constructors here ??
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelatedObjectQuery'/> class for the given set of parameters.
        ///    The query is assumed to be an instance query (as opposed to a schema query).</para>
        /// </summary>
        /// <param name='sourceObject'>The path of the source object.</param>
        /// <param name='relatedClass'>The related objects required class.</param>
        /// <param name='relationshipClass'>The relationship type.</param>
        /// <param name='relatedQualifier'>The qualifier required to be present on the related objects.</param>
        /// <param name='relationshipQualifier'>The qualifier required to be present on the relationships.</param>
        /// <param name='relatedRole'>The role that the related objects are required to play in the relationship.</param>
        /// <param name='thisRole'>The role that the source object is required to play in the relationship.</param>
        /// <param name='classDefinitionsOnly'><see langword='true'/>to return only the class definitions of the related objects; otherwise, <see langword='false'/> .</param>
        public RelatedObjectQuery(string sourceObject,
                                   string relatedClass,
                                   string relationshipClass,
                                   string relatedQualifier,
                                   string relationshipQualifier,
                                   string relatedRole,
                                   string thisRole,
                                   bool classDefinitionsOnly)
        {
            this.isSchemaQuery = false;
            this.sourceObject = sourceObject;
            this.relatedClass = relatedClass;
            this.relationshipClass = relationshipClass;
            this.relatedQualifier = relatedQualifier;
            this.relationshipQualifier = relationshipQualifier;
            this.relatedRole = relatedRole;
            this.thisRole = thisRole;
            this.classDefinitionsOnly = classDefinitionsOnly;
            BuildQuery();

        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelatedObjectQuery'/> class for a schema query using the given set
        ///    of parameters. This constructor is used for schema queries only: the first
        ///    parameter must be set to <see langword='true'/>
        ///    .</para>
        /// </summary>
        /// <param name='isSchemaQuery'><see langword='true'/>to indicate that this is a schema query; otherwise, <see langword='false'/> .</param>
        /// <param name='sourceObject'>The path of the source class.</param>
        /// <param name='relatedClass'>The related objects' required base class.</param>
        /// <param name='relationshipClass'>The relationship type.</param>
        /// <param name='relatedQualifier'>The qualifier required to be present on the related objects.</param>
        /// <param name='relationshipQualifier'>The qualifier required to be present on the relationships.</param>
        /// <param name='relatedRole'>The role that the related objects are required to play in the relationship.</param>
        /// <param name='thisRole'>The role that the source class is required to play in the relationship.</param>
        public RelatedObjectQuery(bool isSchemaQuery,
            string sourceObject,
            string relatedClass,
            string relationshipClass,
            string relatedQualifier,
            string relationshipQualifier,
            string relatedRole,
            string thisRole)
        {
            if (isSchemaQuery == false)
                throw new ArgumentException(SR.InvalidQuery, nameof(isSchemaQuery));

            this.isSchemaQuery = true;
            this.sourceObject = sourceObject;
            this.relatedClass = relatedClass;
            this.relationshipClass = relationshipClass;
            this.relatedQualifier = relatedQualifier;
            this.relationshipQualifier = relationshipQualifier;
            this.relatedRole = relatedRole;
            this.thisRole = thisRole;
            this.classDefinitionsOnly = false; //this parameter is not relevant for schema queries.
            BuildQuery();

        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether this is a schema query or an instance query.</para>
        /// </summary>
        /// <value>
        /// <see langword='true'/> if this query
        ///    should be evaluated over the schema; <see langword='false'/> if the query should
        ///    be evaluated over instances.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new query type.</para>
        /// </remarks>
        public bool IsSchemaQuery
        {
            get
            { return isSchemaQuery; }
            set
            { isSchemaQuery = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para> Gets or sets the source object to be used for the query. For instance
        ///       queries, this is typically an instance path. For schema queries, this is typically a class name.</para>
        /// </summary>
        /// <value>
        ///    A string representing the path of the
        ///    object to be used for the query.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new source object.</para>
        /// </remarks>
        public string SourceObject
        {
            get { return sourceObject ?? string.Empty; }
            set { sourceObject = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the class of the endpoint objects.</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the related class
        ///       name.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new related class.</para>
        /// </remarks>
        /// <example>
        ///    <para>To find all the Win32 services available on a computer, this property is set
        ///       to "Win32_Service" : </para>
        ///    <code lang='C#'>RelatedObjectQuery q = new RelatedObjectQuery("Win32_ComputerSystem='MySystem'");
        /// q.RelatedClass = "Win32_Service";
        ///    </code>
        ///    <code lang='VB'>Dim q As New RelatedObjectQuery("Win32_ComputerSystem=""MySystem""")
        /// q.RelatedClass = "Win32_Service"
        ///    </code>
        /// </example>
        public string RelatedClass
        {
            get { return relatedClass ?? string.Empty; }
            set { relatedClass = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the type of relationship (association).</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the relationship
        ///       class name.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new relationship class.</para>
        /// </remarks>
        /// <example>
        ///    <para>For example, for finding all the Win32 services dependent on
        ///       a service, this property should be set to the "Win32_DependentService" association class: </para>
        ///    <code lang='C#'>RelatedObjectQuery q = new RelatedObjectQuery("Win32_Service='TCP/IP'");
        /// q.RelationshipClass = "Win32_DependentService";
        ///    </code>
        ///    <code lang='VB'>Dim q As New RelatedObjectQuery("Win32_Service=""TCP/IP""")
        /// q.RelationshipClass = "Win32_DependentService"
        ///    </code>
        /// </example>
        public string RelationshipClass
        {
            get { return relationshipClass ?? string.Empty; }
            set { relationshipClass = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets a qualifier required to be defined on the related objects.</para>
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    qualifier required on the related objects.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new qualifier.</para>
        /// </remarks>
        public string RelatedQualifier
        {
            get { return relatedQualifier ?? string.Empty; }
            set { relatedQualifier = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets a qualifier required to be defined on the relationship objects.</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the name of the qualifier required
        ///       on the relationship objects.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new qualifier.</para>
        /// </remarks>
        public string RelationshipQualifier
        {
            get { return relationshipQualifier ?? string.Empty; }
            set { relationshipQualifier = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the role that the related objects returned should be playing in the relationship.</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the role of the
        ///       related objects.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new role.</para>
        /// </remarks>
        public string RelatedRole
        {
            get { return relatedRole ?? string.Empty; }
            set { relatedRole = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the role that the source object should be playing in the relationship.</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the role of this object.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new role.</para>
        /// </remarks>
        public string ThisRole
        {
            get { return thisRole ?? string.Empty; }
            set { thisRole = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating that for all instances that adhere to the query, only their class definitions be returned.
        ///       This parameter is only valid for instance queries.</para>
        /// </summary>
        /// <value>
        /// <see langword='true'/> if the query
        ///    requests only class definitions of the result set; otherwise,
        /// <see langword='false'/>.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new flag.</para>
        /// </remarks>
        public bool ClassDefinitionsOnly
        {
            get { return classDefinitionsOnly; }
            set { classDefinitionsOnly = value; BuildQuery(); FireIdentifierChanged(); }
        }


        /// <summary>
        ///  Builds the query string according to the current property values.
        /// </summary>
        protected internal void BuildQuery()
        {
            //If the source object is not set we can't build a query
            //Shouldn't throw here because the user may be in the process of filling in the properties...
            if (sourceObject == null)
                SetQueryString(string.Empty);

            if ((sourceObject == null) || (sourceObject.Length == 0))
                return;

            //"associators" clause
            string s = TokenAssociators + " " + TokenOf + " {" + sourceObject + "}";

            //If any of the other parameters are set we need a "where" clause
            if (!(RelatedClass.Length == 0) ||
                !(RelationshipClass.Length == 0) ||
                !(RelatedQualifier.Length == 0) ||
                !(RelationshipQualifier.Length == 0) ||
                !(RelatedRole.Length == 0) ||
                !(ThisRole.Length == 0) ||
                classDefinitionsOnly ||
                isSchemaQuery)
            {
                s = s + " " + TokenWhere;

                //"ResultClass"
                if (!(RelatedClass.Length == 0))
                    s = s + " " + TokenResultClass + " = " + relatedClass;

                //"AssocClass"
                if (!(RelationshipClass.Length == 0))
                    s = s + " " + TokenAssocClass + " = " + relationshipClass;

                //"ResultRole"
                if (!(RelatedRole.Length == 0))
                    s = s + " " + TokenResultRole + " = " + relatedRole;

                //"Role"
                if (!(ThisRole.Length == 0))
                    s = s + " " + TokenRole + " = " + thisRole;

                //"RequiredQualifier"
                if (!(RelatedQualifier.Length == 0))
                    s = s + " " + TokenRequiredQualifier + " = " + relatedQualifier;

                //"RequiredAssocQualifier"
                if (!(RelationshipQualifier.Length == 0))
                    s = s + " " + TokenRequiredAssocQualifier + " = " + relationshipQualifier;

                //"SchemaOnly" and "ClassDefsOnly"
                if (!isSchemaQuery) //this is an instance query - classDefs allowed
                {
                    if (classDefinitionsOnly)
                        s = s + " " + TokenClassDefsOnly;
                }
                else //this is a schema query, schemaonly required
                    s = s + " " + TokenSchemaOnly;
            }

            //Set the queryString member to the built query (NB: note we set
            //by accessing the internal helper function rather than the property,
            //since we do not want to force a parse of a query we just built).
            SetQueryString(s);

        }//BuildQuery()


        /// <summary>
        ///  Parses the query string and sets the property values accordingly.
        /// </summary>
        /// <param name="query">The query string to be parsed.</param>
        protected internal override void ParseQuery(string query)
        {
            // Temporary variables to hold token values until we are sure query is valid
            string tempSourceObject = null;
            string tempRelatedClass = null;
            string tempRelationshipClass = null;
            string tempRelatedRole = null;
            string tempThisRole = null;
            string tempRelatedQualifier = null;
            string tempRelationshipQualifier = null;
            bool tempClassDefsOnly = false;
            bool tempIsSchemaQuery = false;

            //Trim whitespaces
            string q = query.Trim();
            int i;

            //Find "associators" clause
            if (0 != string.Compare(q, 0, TokenAssociators, 0, TokenAssociators.Length, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenAssociators), nameof(query));    // Invalid query

            // Strip off the clause
            q = q.Remove(0, TokenAssociators.Length);

            // Must be some white space next
            if ((0 == q.Length) || !char.IsWhiteSpace(q[0]))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            q = q.TrimStart(null);    // Remove the leading whitespace

            // Next token should be "of"
            if (0 != string.Compare(q, 0, TokenOf, 0, TokenOf.Length, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenOf), nameof(query));    // Invalid query

            // Strip off the clause and leading WS
            q = q.Remove(0, TokenOf.Length).TrimStart(null);

            // Next character should be "{"
            if (0 != q.IndexOf('{'))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            // Strip off the "{" and any leading WS
            q = q.Remove(0, 1).TrimStart(null);

            // Next item should be the source object
            if (-1 == (i = q.IndexOf('}')))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            tempSourceObject = q.Substring(0, i).TrimEnd(null);
            q = q.Remove(0, i + 1).TrimStart(null);

            // At this point we may or may not have a "where" clause
            if (0 < q.Length)
            {
                // Next should be the "where" clause
                if (0 != string.Compare(q, 0, TokenWhere, 0, TokenWhere.Length, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenWhere), nameof(query));    // Invalid query

                q = q.Remove(0, TokenWhere.Length);

                // Must be some white space next
                if ((0 == q.Length) || !char.IsWhiteSpace(q[0]))
                    throw new ArgumentException(SR.InvalidQuery);    // Invalid query

                q = q.TrimStart(null);    // Remove the leading whitespace

                // Remaining tokens can appear in any order
                bool bResultClassFound = false;
                bool bAssocClassFound = false;
                bool bResultRoleFound = false;
                bool bRoleFound = false;
                bool bRequiredQualifierFound = false;
                bool bRequiredAssocQualifierFound = false;
                bool bClassDefsOnlyFound = false;
                bool bSchemaOnlyFound = false;

                // Keep looking for tokens until we are done
                while (true)
                {
                    if ((q.Length >= TokenResultClass.Length) && (0 == string.Compare(q, 0, TokenResultClass, 0, TokenResultClass.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenResultClass, "=", ref bResultClassFound, ref tempRelatedClass);
                    else if ((q.Length >= TokenAssocClass.Length) && (0 == string.Compare(q, 0, TokenAssocClass, 0, TokenAssocClass.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenAssocClass, "=", ref bAssocClassFound, ref tempRelationshipClass);
                    else if ((q.Length >= TokenResultRole.Length) && (0 == string.Compare(q, 0, TokenResultRole, 0, TokenResultRole.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenResultRole, "=", ref bResultRoleFound, ref tempRelatedRole);
                    else if ((q.Length >= TokenRole.Length) && (0 == string.Compare(q, 0, TokenRole, 0, TokenRole.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenRole, "=", ref bRoleFound, ref tempThisRole);
                    else if ((q.Length >= TokenRequiredQualifier.Length) && (0 == string.Compare(q, 0, TokenRequiredQualifier, 0, TokenRequiredQualifier.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenRequiredQualifier, "=", ref bRequiredQualifierFound, ref tempRelatedQualifier);
                    else if ((q.Length >= TokenRequiredAssocQualifier.Length) && (0 == string.Compare(q, 0, TokenRequiredAssocQualifier, 0, TokenRequiredAssocQualifier.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenRequiredAssocQualifier, "=", ref bRequiredAssocQualifierFound, ref tempRelationshipQualifier);
                    else if ((q.Length >= TokenSchemaOnly.Length) && (0 == string.Compare(q, 0, TokenSchemaOnly, 0, TokenSchemaOnly.Length, StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseToken(ref q, TokenSchemaOnly, ref bSchemaOnlyFound);
                        tempIsSchemaQuery = true;
                    }
                    else if ((q.Length >= TokenClassDefsOnly.Length) && (0 == string.Compare(q, 0, TokenClassDefsOnly, 0, TokenClassDefsOnly.Length, StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseToken(ref q, TokenClassDefsOnly, ref bClassDefsOnlyFound);
                        tempClassDefsOnly = true;
                    }
                    else if (0 == q.Length)
                        break;        // done
                    else
                        throw new ArgumentException(SR.InvalidQuery);        // Unrecognized token
                }

                //Can't have both classDefsOnly and schemaOnly
                if (bSchemaOnlyFound && bClassDefsOnlyFound)
                    throw new ArgumentException(SR.InvalidQuery);
            }

            // Getting here means we parsed successfully. Assign the values.
            sourceObject = tempSourceObject;
            relatedClass = tempRelatedClass;
            relationshipClass = tempRelationshipClass;
            relatedRole = tempRelatedRole;
            thisRole = tempThisRole;
            relatedQualifier = tempRelatedQualifier;
            relationshipQualifier = tempRelationshipQualifier;
            classDefinitionsOnly = tempClassDefsOnly;
            isSchemaQuery = tempIsSchemaQuery;

        }//ParseQuery()


        //ICloneable
        /// <summary>
        ///    <para>Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The copied object.
        /// </returns>
        public override object Clone()
        {
            if (isSchemaQuery == false)
                return new RelatedObjectQuery(sourceObject, relatedClass, relationshipClass,
                                            relatedQualifier, relationshipQualifier, relatedRole,
                                            thisRole, classDefinitionsOnly);
            else
                return new RelatedObjectQuery(true, sourceObject, relatedClass, relationshipClass,
                                            relatedQualifier, relationshipQualifier, relatedRole,
                                            thisRole);

        }

    }//RelatedObjectQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WQL REFERENCES OF data query.</para>
    /// </summary>
    /// <example>
    ///    <para>The following example searches for all objects related to the
    ///       'C:' drive object:</para>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// class Sample_RelationshipQuery
    /// {
    ///     public static int Main(string[] args) {
    ///         RelationshipQuery query =
    ///             new RelationshipQuery("references of {Win32_LogicalDisk.DeviceID='C:'}");
    ///         ManagementObjectSearcher searcher =
    ///             new ManagementObjectSearcher(query);
    ///
    ///         foreach (ManagementObject assoc in searcher.Get()) {
    ///             Console.WriteLine("Association class = " + assoc["__CLASS"]);
    ///         }
    ///
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// Class Sample_RelatedObjectQuery
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim query As New RelationshipQuery("references of {Win32_LogicalDisk.DeviceID='C:'}")
    ///         Dim searcher As New ManagementObjectSearcher(query)
    ///         Dim assoc As ManagementObject
    ///
    ///         For Each assoc In searcher.Get()
    ///             Console.WriteLine("Association class = " &amp; assoc("__CLASS"))
    ///         Next assoc
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>

    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class RelationshipQuery : WqlObjectQuery
    {
        private const string TokenReferences = "references";
        private const string TokenOf = "of";
        private const string TokenWhere = "where";
        private const string TokenResultClass = "resultclass";
        private const string TokenRole = "role";
        private const string TokenRequiredQualifier = "requiredqualifier";
        private const string TokenClassDefsOnly = "classdefsonly";
        private const string TokenSchemaOnly = "schemaonly";

        private string sourceObject;
        private string relationshipClass;
        private string relationshipQualifier;
        private string thisRole;
        private bool classDefinitionsOnly;
        private bool isSchemaQuery;

        //default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.RelationshipQuery'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelationshipQuery'/> class. This is the default constructor.</para>
        /// </summary>
        public RelationshipQuery() : this(null) { }

        //parameterized constructor
        //ISSUE : We have 2 possible constructors that take a single string :
        //  one that takes the full query string and the other that takes the source object path.
        //  We resolve this by trying to parse the string, if it succeeds we assume it's the query, if
        //  not we assume it's the source object.
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelationshipQuery'/>class. If the specified string can be successfully parsed as
        ///    a WQL query, it is considered to be the query string; otherwise, it is assumed to be the path of the source
        ///    object for the query. In this case, the query is assumed to be an instances query. </para>
        /// </summary>
        /// <param name='queryOrSourceObject'>The query string or the class name for this query.</param>
        /// <example>
        ///    <para>This example shows the two different ways to use this constructor:</para>
        ///    <code lang='C#'>//Full query string is specified to the constructor
        /// RelationshipQuery q = new RelationshipQuery("references of {Win32_ComputerSystem.Name='mymachine'}");
        ///
        /// //Only the object of interest is specified to the constructor
        /// RelationshipQuery q = new RelationshipQuery("Win32_Service.Name='Alerter'");
        ///    </code>
        ///    <code lang='VB'>'Full query string is specified to the constructor
        /// Dim q As New RelationshipQuery("references of {Win32_ComputerSystem.Name='mymachine'}")
        ///
        /// 'Only the object of interest is specified to the constructor
        /// Dim q As New RelationshipQuery("Win32_Service.Name='Alerter'")
        ///    </code>
        /// </example>
        public RelationshipQuery(string queryOrSourceObject)
        {
            if (null != queryOrSourceObject)
            {
                // Minimally determine if the string is a query or instance name.
                //
                if (queryOrSourceObject.TrimStart().StartsWith(TokenReferences, StringComparison.OrdinalIgnoreCase))
                {
                    // Looks to be a query - do further checking.
                    //
                    QueryString = queryOrSourceObject;    // Parse/validate; may throw.
                }
                else
                {
                    // We'd like to treat it as the source object. Is it a valid
                    // class or instance?

                    // Do some basic sanity checking on whether it's a class/instance name
                    //
                    ManagementPath p = new ManagementPath(queryOrSourceObject);

                    if ((p.IsClass || p.IsInstance) && (p.NamespacePath.Length == 0))
                    {
                        SourceObject = queryOrSourceObject;
                        isSchemaQuery = false;
                    }
                    else
                        throw new ArgumentException(SR.InvalidQuery, nameof(queryOrSourceObject));

                }
            }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelationshipQuery'/> class for the given source object and relationship class.
        ///    The query is assumed to be an instance query (as opposed to a schema query).</para>
        /// </summary>
        /// <param name='sourceObject'> The path of the source object for this query.</param>
        /// <param name='relationshipClass'> The type of relationship for which to query.</param>
        public RelationshipQuery(string sourceObject, string relationshipClass) : this(sourceObject, relationshipClass,
                                                                                        null, null, false)
        { }

        //Do we need additional variants of constructors here ??
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelationshipQuery'/> class for the given set of parameters.
        ///    The query is assumed to be an instance query (as opposed to a schema query).</para>
        /// </summary>
        /// <param name='sourceObject'> The path of the source object for this query.</param>
        /// <param name='relationshipClass'> The type of relationship for which to query.</param>
        /// <param name='relationshipQualifier'> A qualifier required to be present on the relationship object.</param>
        /// <param name='thisRole'> The role that the source object is required to play in the relationship.</param>
        /// <param name='classDefinitionsOnly'>When this method returns, it contains a boolean that indicates that only class definitions for the resulting objects are returned.</param>
        public RelationshipQuery(string sourceObject,
                                  string relationshipClass,
                                  string relationshipQualifier,
                                  string thisRole,
                                  bool classDefinitionsOnly)
        {
            this.isSchemaQuery = false;
            this.sourceObject = sourceObject;
            this.relationshipClass = relationshipClass;
            this.relationshipQualifier = relationshipQualifier;
            this.thisRole = thisRole;
            this.classDefinitionsOnly = classDefinitionsOnly;
            BuildQuery();
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.RelationshipQuery'/> class for a schema query using the given set
        ///    of parameters. This constructor is used for schema queries only, so the first
        ///    parameter must be <see langword='true'/>
        ///    .</para>
        /// </summary>
        /// <param name='isSchemaQuery'><see langword='true'/>to indicate that this is a schema query; otherwise, <see langword='false'/> .</param>
        /// <param name='sourceObject'> The path of the source class for this query.</param>
        /// <param name='relationshipClass'> The type of relationship for which to query.</param>
        /// <param name='relationshipQualifier'> A qualifier required to be present on the relationship class.</param>
        /// <param name='thisRole'> The role that the source class is required to play in the relationship.</param>
        public RelationshipQuery(bool isSchemaQuery,
            string sourceObject,
            string relationshipClass,
            string relationshipQualifier,
            string thisRole)
        {
            if (isSchemaQuery == false)
                throw new ArgumentException(SR.InvalidQuery, nameof(isSchemaQuery));

            this.isSchemaQuery = true;
            this.sourceObject = sourceObject;
            this.relationshipClass = relationshipClass;
            this.relationshipQualifier = relationshipQualifier;
            this.thisRole = thisRole;
            this.classDefinitionsOnly = false; //this parameter is not relevant for schema queries.
            BuildQuery();

        }


        /// <summary>
        ///    <para>Gets or sets a value indicating whether this query is a schema query or an instance query.</para>
        /// </summary>
        /// <value>
        /// <see langword='true'/> if this query
        ///    should be evaluated over the schema; <see langword='false'/> if the query should
        ///    be evaluated over instances.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new query type.</para>
        /// </remarks>
        public bool IsSchemaQuery
        {
            get
            { return isSchemaQuery; }
            set
            { isSchemaQuery = value; BuildQuery(); FireIdentifierChanged(); }
        }


        /// <summary>
        ///    <para>Gets or sets the source object for this query.</para>
        /// </summary>
        /// <value>
        ///    A string representing the path of
        ///    the object to be used for the query.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new source object.</para>
        /// </remarks>
        public string SourceObject
        {
            get { return sourceObject ?? string.Empty; }
            set { sourceObject = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the class of the relationship objects wanted in the query.</para>
        /// </summary>
        /// <value>
        ///    A string containing the relationship
        ///    class name.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new class.</para>
        /// </remarks>
        public string RelationshipClass
        {
            get { return relationshipClass ?? string.Empty; }
            set { relationshipClass = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets a qualifier required on the relationship objects.</para>
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    qualifier required on the relationship objects.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new qualifier.</para>
        /// </remarks>
        public string RelationshipQualifier
        {
            get { return relationshipQualifier ?? string.Empty; }
            set { relationshipQualifier = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets the role of the source object in the relationship.</para>
        /// </summary>
        /// <value>
        ///    A string containing the role of this
        ///    object.
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any
        ///       previous value stored in the object. The query string is
        ///       rebuilt to reflect the new role.</para>
        /// </remarks>
        public string ThisRole
        {
            get { return thisRole ?? string.Empty; }
            set { thisRole = value; BuildQuery(); FireIdentifierChanged(); }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating that only the class definitions of the relevant relationship objects be returned.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the query requests only class definitions of the
        ///    result set; otherwise, <see langword='false'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any previous
        ///       value stored in the object. As a side-effect, the query string is
        ///       rebuilt to reflect the new flag.</para>
        /// </remarks>
        public bool ClassDefinitionsOnly
        {
            get { return classDefinitionsOnly; }
            set { classDefinitionsOnly = value; BuildQuery(); FireIdentifierChanged(); }
        }


        /// <summary>
        ///  Builds the query string according to the current property values.
        /// </summary>
        protected internal void BuildQuery()
        {
            //If the source object is not set we can't build a query
            //Shouldn't throw here because the user may be in the process of filling in the properties...
            if (sourceObject == null)
                SetQueryString(string.Empty);

            if ((sourceObject == null) || (sourceObject.Length == 0))
                return;

            //"references" clause
            string s = TokenReferences + " " + TokenOf + " {" + sourceObject + "}";

            //If any of the other parameters are set we need a "where" clause
            if (!(RelationshipClass.Length == 0) ||
                !(RelationshipQualifier.Length == 0) ||
                !(ThisRole.Length == 0) ||
                classDefinitionsOnly ||
                isSchemaQuery)
            {
                s = s + " " + TokenWhere;

                //"ResultClass"
                if (!(RelationshipClass.Length == 0))
                    s = s + " " + TokenResultClass + " = " + relationshipClass;

                //"Role"
                if (!(ThisRole.Length == 0))
                    s = s + " " + TokenRole + " = " + thisRole;

                //"RequiredQualifier"
                if (!(RelationshipQualifier.Length == 0))
                    s = s + " " + TokenRequiredQualifier + " = " + relationshipQualifier;

                //"SchemaOnly" and "ClassDefsOnly"
                if (!isSchemaQuery) //this is an instance query - classDefs allowed
                {
                    if (classDefinitionsOnly)
                        s = s + " " + TokenClassDefsOnly;
                }
                else //this is a schema query, schemaonly required
                    s = s + " " + TokenSchemaOnly;

            }

            //Set the queryString member to the built query (NB: note we set
            //by accessing the internal helper function rather than the property,
            //since we do not want to force a parse of a query we just built).
            SetQueryString(s);
        } //BuildQuery()


        /// <summary>
        ///  Parses the query string and sets the property values accordingly.
        /// </summary>
        /// <param name="query">The query string to be parsed.</param>
        protected internal override void ParseQuery(string query)
        {
            // Temporary variables to hold token values until we are sure query is valid
            string tempSourceObject = null;
            string tempRelationshipClass = null;
            string tempThisRole = null;
            string tempRelationshipQualifier = null;
            bool tempClassDefsOnly = false;
            bool tempSchemaOnly = false;

            //Trim whitespaces
            string q = query.Trim();
            int i;

            //Find "references" clause
            if (0 != string.Compare(q, 0, TokenReferences, 0, TokenReferences.Length, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenReferences), nameof(query));    // Invalid query

            // Strip off the clause
            q = q.Remove(0, TokenReferences.Length);

            // Must be some white space next
            if ((0 == q.Length) || !char.IsWhiteSpace(q[0]))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            q = q.TrimStart(null);    // Remove the leading whitespace

            // Next token should be "of"
            if (0 != string.Compare(q, 0, TokenOf, 0, TokenOf.Length, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenOf), nameof(query));    // Invalid query

            // Strip off the clause and leading WS
            q = q.Remove(0, TokenOf.Length).TrimStart(null);

            // Next character should be "{"
            if (0 != q.IndexOf('{'))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            // Strip off the "{" and any leading WS
            q = q.Remove(0, 1).TrimStart(null);

            // Next item should be the source object
            if (-1 == (i = q.IndexOf('}')))
                throw new ArgumentException(SR.InvalidQuery);    // Invalid query

            tempSourceObject = q.Substring(0, i).TrimEnd(null);
            q = q.Remove(0, i + 1).TrimStart(null);

            // At this point we may or may not have a "where" clause
            if (0 < q.Length)
            {
                // Next should be the "where" clause
                if (0 != string.Compare(q, 0, TokenWhere, 0, TokenWhere.Length, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, TokenWhere), nameof(query));    // Invalid query

                q = q.Remove(0, TokenWhere.Length);

                // Must be some white space next
                if ((0 == q.Length) || !char.IsWhiteSpace(q[0]))
                    throw new ArgumentException(SR.InvalidQuery);    // Invalid query

                q = q.TrimStart(null);    // Remove the leading whitespace

                // Remaining tokens can appear in any order
                bool bResultClassFound = false;
                bool bRoleFound = false;
                bool bRequiredQualifierFound = false;
                bool bClassDefsOnlyFound = false;
                bool bSchemaOnlyFound = false;

                // Keep looking for tokens until we are done
                while (true)
                {
                    if ((q.Length >= TokenResultClass.Length) && (0 == string.Compare(q, 0, TokenResultClass, 0, TokenResultClass.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenResultClass, "=", ref bResultClassFound, ref tempRelationshipClass);
                    else if ((q.Length >= TokenRole.Length) && (0 == string.Compare(q, 0, TokenRole, 0, TokenRole.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenRole, "=", ref bRoleFound, ref tempThisRole);
                    else if ((q.Length >= TokenRequiredQualifier.Length) && (0 == string.Compare(q, 0, TokenRequiredQualifier, 0, TokenRequiredQualifier.Length, StringComparison.OrdinalIgnoreCase)))
                        ParseToken(ref q, TokenRequiredQualifier, "=", ref bRequiredQualifierFound, ref tempRelationshipQualifier);
                    else if ((q.Length >= TokenClassDefsOnly.Length) && (0 == string.Compare(q, 0, TokenClassDefsOnly, 0, TokenClassDefsOnly.Length, StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseToken(ref q, TokenClassDefsOnly, ref bClassDefsOnlyFound);
                        tempClassDefsOnly = true;
                    }
                    else if ((q.Length >= TokenSchemaOnly.Length) && (0 == string.Compare(q, 0, TokenSchemaOnly, 0, TokenSchemaOnly.Length, StringComparison.OrdinalIgnoreCase)))
                    {
                        ParseToken(ref q, TokenSchemaOnly, ref bSchemaOnlyFound);
                        tempSchemaOnly = true;
                    }
                    else if (0 == q.Length)
                        break;        // done
                    else
                        throw new ArgumentException(SR.InvalidQuery);        // Unrecognized token
                }

                //Can't have both classDefsOnly and schemaOnly
                if (tempClassDefsOnly && tempSchemaOnly)
                    throw new ArgumentException(SR.InvalidQuery);

            }

            // Getting here means we parsed successfully. Assign the values.
            sourceObject = tempSourceObject;
            relationshipClass = tempRelationshipClass;
            thisRole = tempThisRole;
            relationshipQualifier = tempRelationshipQualifier;
            classDefinitionsOnly = tempClassDefsOnly;
            isSchemaQuery = tempSchemaOnly;

        }//ParseQuery()


        //ICloneable
        /// <summary>
        ///    <para>Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The copied object.
        /// </returns>
        public override object Clone()
        {
            if (isSchemaQuery == false)
                return new RelationshipQuery(sourceObject, relationshipClass,
                                            relationshipQualifier, thisRole, classDefinitionsOnly);
            else
                return new RelationshipQuery(true, sourceObject, relationshipClass, relationshipQualifier,
                                            thisRole);
        }

    }//RelationshipQuery


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a WMI event query in WQL format.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to subscribe to an event
    /// // using a WQL event query.
    ///
    /// class Sample_EventQuery
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         //For this example, we make sure we have an arbitrary class on root\default
    ///         ManagementClass newClass = new ManagementClass(
    ///             "root\\default",
    ///             String.Empty,
    ///             null);
    ///         newClass["__Class"] = "TestWql";
    ///         newClass.Put();
    ///
    ///         //Create a query object for watching for class deletion events
    ///         WqlEventQuery eventQuery = new WqlEventQuery("select * from __classdeletionevent");
    ///
    ///         //Initialize an event watcher object with this query
    ///         ManagementEventWatcher watcher = new ManagementEventWatcher(
    ///             new ManagementScope("root/default"),
    ///             eventQuery);
    ///
    ///         //Set up a handler for incoming events
    ///         MyHandler handler = new MyHandler();
    ///         watcher.EventArrived += new EventArrivedEventHandler(handler.Arrived);
    ///
    ///         //Start watching for events
    ///         watcher.Start();
    ///
    ///         //For this example, we delete the class to trigger an event
    ///         newClass.Delete();
    ///
    ///         //Nothing better to do - we loop to wait for an event to arrive.
    ///         while (!handler.IsArrived) {
    ///              System.Threading.Thread.Sleep(1000);
    ///         }
    ///
    ///         //In this example we only want to wait for one event, so we can stop watching
    ///         watcher.Stop();
    ///
    ///         return 0;
    ///     }
    ///
    ///     public class MyHandler
    ///     {
    ///         private bool isArrived = false;
    ///
    ///         //Handles the event when it arrives
    ///         public void Arrived(object sender, EventArrivedEventArgs e) {
    ///             ManagementBaseObject eventArg = (ManagementBaseObject)(e.NewEvent["TargetClass"]);
    ///             Console.WriteLine("Class Deleted = " + eventArg["__CLASS"]);
    ///             isArrived = true;
    ///         }
    ///
    ///          //Used to determine whether the event has arrived or not.
    ///         public bool IsArrived {
    ///             get {
    ///                 return isArrived;
    ///             }
    ///         }
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates how to subscribe an event
    /// ' using a WQL event query.
    ///
    /// Class Sample_EventQuery
    ///     Public Shared Sub Main()
    ///
    ///         'For this example, we make sure we have an arbitrary class on root\default
    ///         Dim newClass As New ManagementClass( _
    ///             "root\default", _
    ///             String.Empty, Nothing)
    ///             newClass("__Class") = "TestWql"
    ///             newClass.Put()
    ///
    ///         'Create a query object for watching for class deletion events
    ///         Dim eventQuery As New WqlEventQuery("select * from __classdeletionevent")
    ///
    ///         'Initialize an event watcher object with this query
    ///         Dim watcher As New ManagementEventWatcher( _
    ///             New ManagementScope("root/default"), _
    ///             eventQuery)
    ///
    ///         'Set up a handler for incoming events
    ///         Dim handler As New MyHandler()
    ///         AddHandler watcher.EventArrived, AddressOf handler.Arrived
    ///
    ///         'Start watching for events
    ///         watcher.Start()
    ///
    ///         'For this example, we delete the class to trigger an event
    ///         newClass.Delete()
    ///
    ///         'Nothing better to do - we loop to wait for an event to arrive.
    ///         While Not handler.IsArrived
    ///             Console.Write("0")
    ///             System.Threading.Thread.Sleep(1000)
    ///         End While
    ///
    ///         'In this example we only want to wait for one event, so we can stop watching
    ///         watcher.Stop()
    ///
    ///     End Sub
    ///
    ///     Public Class MyHandler
    ///         Private _isArrived As Boolean = False
    ///
    ///         'Handles the event when it arrives
    ///         Public Sub Arrived(sender As Object, e As EventArrivedEventArgs)
    ///             Dim eventArg As ManagementBaseObject = CType( _
    ///                 e.NewEvent("TargetClass"), _
    ///                 ManagementBaseObject)
    ///             Console.WriteLine(("Class Deleted = " + eventArg("__CLASS")))
    ///             _isArrived = True
    ///         End Sub
    ///
    ///         'Used to determine whether the event has arrived or not.
    ///         Public ReadOnly Property IsArrived() As Boolean
    ///             Get
    ///                 Return _isArrived
    ///             End Get
    ///         End Property
    ///     End Class
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class WqlEventQuery : EventQuery
    {
        private const string TokenSelectAll = "select * ";

        private string eventClassName;
        private TimeSpan withinInterval;
        private string condition;
        private TimeSpan groupWithinInterval;
        private StringCollection groupByPropertyList;
        private string havingCondition;

        //default constructor
        /// <overload>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/> class.</para>
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class. This is the default
        /// constructor.</para>
        /// </summary>
        public WqlEventQuery() : this(null, TimeSpan.Zero, null, TimeSpan.Zero, null, null) { }

        //parameterized constructors
        //ISSUE : We have 2 possible constructors that take a single string :
        //  one that takes the full query string and the other that takes the class name.
        //  We resolve this by trying to parse the string, if it succeeds we assume it's the query, if
        //  not we assume it's the class name.
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class based on the given
        /// query string or event class name.</para>
        /// </summary>
        /// <param name='queryOrEventClassName'>The string representing either the entire event query or the name of the event class to query. The object will try to parse the string as a valid event query. If unsuccessful, the parser will assume that the parameter represents an event class name.</param>
        /// <example>
        ///    <para>The two options below are equivalent :</para>
        ///    <code lang='C#'>//Full query string specified to the constructor
        /// WqlEventQuery q = new WqlEventQuery("SELECT * FROM MyEvent");
        ///
        /// //Only relevant event class name specified to the constructor
        /// WqlEventQuery q = new WqlEventQuery("MyEvent"); //results in the same query as above.
        ///    </code>
        ///    <code lang='VB'>'Full query string specified to the constructor
        /// Dim q As New WqlEventQuery("SELECT * FROM MyEvent")
        ///
        /// 'Only relevant event class name specified to the constructor
        /// Dim q As New WqlEventQuery("MyEvent") 'results in the same query as above
        ///    </code>
        /// </example>
        public WqlEventQuery(string queryOrEventClassName)
        {
            groupByPropertyList = new StringCollection();

            if (null != queryOrEventClassName)
            {
                // Minimally determine if the string is a query or event class name.
                //
                if (queryOrEventClassName.TrimStart().StartsWith(TokenSelectAll, StringComparison.OrdinalIgnoreCase))
                {
                    QueryString = queryOrEventClassName;    // Parse/validate; may throw.
                }
                else
                {
                    // Do some basic sanity checking on whether it's a class name
                    //
                    ManagementPath p = new ManagementPath(queryOrEventClassName);

                    if (p.IsClass && (p.NamespacePath.Length == 0))
                    {
                        EventClassName = queryOrEventClassName;
                    }
                    else
                        throw new ArgumentException(SR.InvalidQuery, nameof(queryOrEventClassName));
                }
            }
        }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class for the
        /// specified event class name, with the specified condition.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class to query.</param>
        /// <param name=' condition'>The condition to apply to events of the specified class.</param>
        /// <example>
        ///    <para>This example shows how to create an event query that contains a condition in
        ///       addition to the event class :</para>
        ///    <code lang='C#'>//Requests all "MyEvent" events where the event's properties
        /// //match the specified condition
        /// WqlEventQuery q = new WqlEventQuery("MyEvent", "FirstProp &lt; 20 and SecondProp = 'red'");
        ///    </code>
        ///    <code lang='VB'>'Requests all "MyEvent" events where the event's properties
        /// 'match the specified condition
        /// Dim q As New WqlEventQuery("MyEvent", "FirstProp &lt; 20 and SecondProp = 'red'")
        ///    </code>
        /// </example>
        public WqlEventQuery(string eventClassName, string condition) : this(eventClassName, TimeSpan.Zero, condition, TimeSpan.Zero, null, null) { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class for the specified
        /// event class, with the specified latency time.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class to query.</param>
        /// <param name=' withinInterval'>A timespan value specifying the latency acceptable for receiving this event. This value is used in cases where there is no explicit event provider for the query requested, and WMI is required to poll for the condition. This interval is the maximum amount of time that can pass before notification of an event must be delivered. </param>
        /// <example>
        ///    <para>This example shows creating an event query that contains
        ///       a
        ///       time interval.</para>
        ///    <code lang='C#'>//Requests all instance creation events, with a specified latency of
        /// //10 seconds. The query created is "SELECT * FROM __InstanceCreationEvent WITHIN 10"
        /// WqlEventQuery q = new WqlEventQuery("__InstanceCreationEvent",
        ///                                     new TimeSpan(0,0,10));
        ///    </code>
        ///    <code lang='VB'>'Requests all instance creation events, with a specified latency of
        /// '10 seconds. The query created is "SELECT * FROM __InstanceCreationEvent WITHIN 10"
        /// Dim t As New TimeSpan(0,0,10)
        /// Dim q As New WqlEventQuery("__InstanceCreationEvent", t)
        ///    </code>
        /// </example>
        public WqlEventQuery(string eventClassName, TimeSpan withinInterval) :
                                        this(eventClassName, withinInterval, null, TimeSpan.Zero, null, null)
        { }
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class with the specified
        /// event class name, polling interval, and condition.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class to query. </param>
        /// <param name=' withinInterval'>A timespan value specifying the latency acceptable for receiving this event. This value is used in cases where there is no explicit event provider for the query requested and WMI is required to poll for the condition. This interval is the maximum amount of time that can pass before notification of an event must be delivered. </param>
        /// <param name=' condition'>The condition to apply to events of the specified class.</param>
        /// <example>
        ///    <para> This example creates the event query: "SELECT * FROM
        ///    <see langword='__InstanceCreationEvent '/>WITHIN 10 WHERE
        ///    <see langword='TargetInstance'/> ISA <see langword='Win32_Service'/>", which means
        ///       "send notification of the creation of <see langword='Win32_Service '/>
        ///       instances,
        ///       with a 10-second polling interval."</para>
        ///    <code lang='C#'>//Requests notification of the creation of Win32_Service instances with a 10 second
        /// //allowed latency.
        /// WqlEventQuery q = new WqlEventQuery("__InstanceCreationEvent",
        ///                                     new TimeSpan(0,0,10),
        ///                                     "TargetInstance isa 'Win32_Service'");
        ///    </code>
        ///    <code lang='VB'>'Requests notification of the creation of Win32_Service instances with a 10 second
        /// 'allowed latency.
        /// Dim t As New TimeSpan(0,0,10)
        /// Dim q As New WqlEventQuery("__InstanceCreationEvent", _
        ///                            t, _
        ///                            "TargetInstance isa ""Win32_Service""")
        ///    </code>
        /// </example>
        public WqlEventQuery(string eventClassName, TimeSpan withinInterval, string condition) :
                                        this(eventClassName, withinInterval, condition, TimeSpan.Zero, null, null)
        { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class with the specified
        /// event class name, condition, and grouping interval.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class to query. </param>
        /// <param name='condition'>The condition to apply to events of the specified class.</param>
        /// <param name=' groupWithinInterval'>The specified interval at which WMI sends one aggregate event, rather than many events.</param>
        /// <example>
        ///    <para>This example creates the event query: "SELECT * FROM
        ///    <see langword='FrequentEvent'/> WHERE <see langword='InterestingProperty'/>= 5
        ///       GROUP WITHIN 10", which means "send notification of events of type
        ///    <see langword='FrequentEvent'/>, in which the
        ///    <see langword='InterestingProperty'/> is equal to 5, but send an aggregate event in
        ///       a
        ///       10-second interval."</para>
        ///    <code lang='C#'>//Sends an aggregate of the requested events every 10 seconds
        /// WqlEventQuery q = new WqlEventQuery("FrequentEvent",
        ///                                     "InterestingProperty = 5",
        ///                                     new TimeSpan(0,0,10));
        ///    </code>
        ///    <code lang='VB'>'Sends an aggregate of the requested events every 10 seconds
        /// Dim t As New TimeSpan(0,0,10)
        /// Dim q As New WqlEventQuery("FrequentEvent", _
        ///                            "InterestingProperty = 5", _
        ///                            t)
        ///    </code>
        /// </example>
        public WqlEventQuery(string eventClassName, string condition, TimeSpan groupWithinInterval) :
                                        this(eventClassName, TimeSpan.Zero, condition, groupWithinInterval, null, null)
        { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class with the specified event class
        /// name, condition, grouping interval, and grouping properties.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class to query. </param>
        /// <param name='condition'>The condition to apply to events of the specified class.</param>
        /// <param name=' groupWithinInterval'>The specified interval at which WMI sends one aggregate event, rather than many events. </param>
        /// <param name=' groupByPropertyList'>The properties in the event class by which the events should be grouped.</param>
        /// <example>
        ///    <para>This example creates the event query: "SELECT * FROM
        ///    <see langword='EmailEvent'/> WHERE <see langword='Sender'/> = 'MyBoss' GROUP
        ///       WITHIN 300 BY <see langword='Importance'/>", which means "send notification when
        ///       new email from a particular sender has arrived within the last 10 minutes,
        ///       combined with other events that have the same value in the
        ///    <see langword='Importance'/>
        ///    property."</para>
        /// <code lang='C#'>//Requests "EmailEvent" events where the Sender property is "MyBoss", and
        /// //groups them based on importance
        /// String[] props = {"Importance"};
        /// WqlEventQuery q = new WqlEventQuery("EmailEvent",
        ///                                     "Sender = 'MyBoss'",
        ///                                     new TimeSpan(0,10,0),
        ///                                     props);
        /// </code>
        /// <code lang='VB'>'Requests "EmailEvent" events where the Sender property is "MyBoss", and
        /// 'groups them based on importance
        /// Dim props() As String = {"Importance"}
        /// Dim t As New TimeSpan(0,10,0)
        /// Dim q As New WqlEventQuery("EmailEvent", _
        ///                            "Sender = ""MyBoss""", _
        ///                            t, _
        ///                            props)
        /// </code>
        /// </example>
        public WqlEventQuery(string eventClassName, string condition, TimeSpan groupWithinInterval, string[] groupByPropertyList) :
            this(eventClassName, TimeSpan.Zero, condition, groupWithinInterval, groupByPropertyList, null)
        { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.WqlEventQuery'/>
        /// class with the specified event class
        /// name, condition, grouping interval, grouping properties, and specified number of events.</para>
        /// </summary>
        /// <param name='eventClassName'>The name of the event class on which to be queried.</param>
        /// <param name='withinInterval'>A timespan value specifying the latency acceptable for receiving this event. This value is used in cases where there is no explicit event provider for the query requested, and WMI is required to poll for the condition. This interval is the maximum amount of time that can pass before notification of an event must be delivered.</param>
        /// <param name=' condition'>The condition to apply to events of the specified class.</param>
        /// <param name=' groupWithinInterval'>The specified interval at which WMI sends one aggregate event, rather than many events. </param>
        /// <param name=' groupByPropertyList'>The properties in the event class by which the events should be grouped.</param>
        /// <param name=' havingCondition'>The condition to apply to the number of events.</param>
        /// <example>
        ///    <para>This example creates the event query: "SELECT * FROM
        ///    <see langword='__InstanceCreationEvent '/>WHERE <see langword='TargetInstance'/>
        ///    ISA <see langword='Win32_NTLogEvent '/>GROUP WITHIN 300 BY
        /// <see langword='TargetInstance.SourceName'/> HAVING
        /// <see langword='NumberOfEvents'/> &gt; 15" which means "deliver aggregate events
        ///    only if the number of <see langword='Win32_NTLogEvent '/>events received from the
        ///    same source exceeds 15."</para>
        /// <code lang='C#'>//Requests sending aggregated events if the number of events exceeds 15.
        /// String[] props = {"TargetInstance.SourceName"};
        /// WqlEventQuery q = new WqlEventQuery("__InstanceCreationEvent",
        ///                                     "TargetInstance isa 'Win32_NTLogEvent'",
        ///                                     new TimeSpan(0,10,0),
        ///                                     props,
        ///                                     "NumberOfEvents &gt;15");
        /// </code>
        /// <code lang='VB'>'Requests sending aggregated events if the number of events exceeds 15.
        /// Dim props() As String = {"TargetInstance.SourceName"};
        /// Dim t As New TimeSpan(0,10,0)
        /// Dim q As WqlEventQuery("__InstanceCreationEvent", _
        ///                        "TargetInstance isa ""Win32_NTLogEvent""", _
        ///                        t, _
        ///                        props, _
        ///                        "NumberOfEvents &gt;15")
        /// </code>
        /// </example>
        public WqlEventQuery(string eventClassName, TimeSpan withinInterval, string condition, TimeSpan groupWithinInterval,
                          string[] groupByPropertyList, string havingCondition)
        {
            this.eventClassName = eventClassName;
            this.withinInterval = withinInterval;
            this.condition = condition;
            this.groupWithinInterval = groupWithinInterval;
            this.groupByPropertyList = new StringCollection();

            if (null != groupByPropertyList)
                this.groupByPropertyList.AddRange(groupByPropertyList);

            this.havingCondition = havingCondition;
            BuildQuery();
        }


        //QueryLanguage property is read-only in this class (does this work ??)
        /// <summary>
        ///    <para>Gets or sets the language of the query.</para>
        /// </summary>
        /// <value>
        ///    <para>The value of this property in this
        ///       object is always "WQL".</para>
        /// </value>
        public override string QueryLanguage
        {
            get
            { return base.QueryLanguage; }
        }

        /// <summary>
        ///    <para>Gets or sets the string representing the query.</para>
        /// </summary>
        /// <value>
        ///    A string representing the query.
        /// </value>
        public override string QueryString
        {
            get
            {
                // We need to force a rebuild as we may not have detected
                // a change to selected properties
                BuildQuery();
                return base.QueryString;
            }
            set
            {
                base.QueryString = value;
            }
        }

        /// <summary>
        ///    <para> Gets or sets the event class to query.</para>
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    event class to query.
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value
        ///       stored
        ///       in the object. The query string is rebuilt to
        ///       reflect the new class name.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='MyEvent'/> ".</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "MyEvent";
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "MyEvent"
        /// </code>
        /// </example>
        public string EventClassName
        {
            get { return eventClassName ?? string.Empty; }
            set { eventClassName = value; BuildQuery(); }
        }

        /// <summary>
        ///    <para>Gets or sets the condition to be applied to events of the
        ///       specified class.</para>
        /// </summary>
        /// <value>
        ///    <para>The condition is represented as a
        ///       string, containing one or more clauses of the form: &lt;propName&gt;
        ///       &lt;operator&gt; &lt;value&gt; combined with and/or operators. &lt;propName&gt;
        ///       must represent a property defined on the event class specified in this query.</para>
        /// </value>
        /// <remarks>
        ///    <para>Setting this property value overrides any previous value
        ///       stored in the object. The query string is rebuilt to
        ///       reflect the new condition.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='MyEvent'/> WHERE
        /// <see langword='PropVal'/> &gt; 8".</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "MyEvent";
        /// q.Condition = "PropVal &gt; 8";
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "MyEvent"
        /// q.Condition = "PropVal &gt; 8"
        /// </code>
        /// </example>
        public string Condition
        {
            get { return condition ?? string.Empty; }
            set { condition = value; BuildQuery(); }
        }

        /// <summary>
        ///    <para>Gets or sets the polling interval to be used in this query.</para>
        /// </summary>
        /// <value>
        ///    <para>Null, if there is no polling involved; otherwise, a
        ///       valid <see cref='System.TimeSpan'/>
        ///       value if polling is required.</para>
        /// </value>
        /// <remarks>
        ///    <para>This property should only be set in cases
        ///       where there is no event provider for the event requested, and WMI is required to
        ///       poll for the requested condition.</para>
        ///    <para>Setting this property value overrides any previous value
        ///       stored in
        ///       the object. The query string is rebuilt to reflect the new interval.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='__InstanceModificationEvent '/>WITHIN 10 WHERE <see langword='PropVal'/> &gt; 8".</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "__InstanceModificationEvent";
        /// q.Condition = "PropVal &gt; 8";
        /// q.WithinInterval = new TimeSpan(0,0,10);
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "__InstanceModificationEvent"
        /// q.Condition = "PropVal &gt; 8"
        /// q.WithinInterval = New TimeSpan(0,0,10)
        /// </code>
        /// </example>
        public TimeSpan WithinInterval
        {
            get { return withinInterval; }
            set { withinInterval = value; BuildQuery(); }
        }

        /// <summary>
        ///    <para>Gets or sets the interval to be used for grouping events of
        ///       the same type.</para>
        /// </summary>
        /// <value>
        ///    <para> Null, if there is no
        ///       grouping involved; otherwise, the interval in which WMI should group events of
        ///       the same type.</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value stored in
        ///       the object. The query string is rebuilt to reflect the new interval.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='MyEvent'/> WHERE
        /// <see langword='PropVal'/> &gt; 8 GROUP WITHIN 10", which means "send notification
        /// of all <see langword='MyEvent'/> events where the <see langword='PropVal'/>
        /// property is greater than 8, and aggregate these events within 10-second intervals."</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "MyEvent";
        /// q.Condition = "PropVal &gt; 8";
        /// q.GroupWithinInterval = new TimeSpan(0,0,10);
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "MyEvent"
        /// q.Condition = "PropVal &gt; 8"
        /// q.GroupWithinInterval = New TimeSpan(0,0,10)
        /// </code>
        /// </example>
        public TimeSpan GroupWithinInterval
        {
            get { return groupWithinInterval; }
            set { groupWithinInterval = value; BuildQuery(); }
        }

        /// <summary>
        ///    <para>Gets or sets properties in the event to be used for
        ///       grouping events of the same type.</para>
        /// </summary>
        /// <value>
        ///    <para>
        ///       Null, if no grouping is required; otherwise, a collection of event
        ///       property names.</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value stored in
        ///       the object. The query string is rebuilt to reflect the new grouping.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='EmailEvent'/> GROUP
        /// WITHIN 300 BY <see langword='Sender'/>", which means "send notification of all
        /// <see langword='EmailEvent'/> events, aggregated by the <see langword='Sender'/>property, within 10-minute intervals."</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "EmailEvent";
        /// q.GroupWithinInterval = new TimeSpan(0,10,0);
        /// q.GroupByPropertyList = new StringCollection();
        /// q.GroupByPropertyList.Add("Sender");
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "EmailEvent"
        /// q.GroupWithinInterval = New TimeSpan(0,10,0)
        /// q.GroupByPropertyList = New StringCollection()
        /// q.GroupByPropertyList.Add("Sender")
        /// </code>
        /// </example>
        public StringCollection GroupByPropertyList
        {
            get { return groupByPropertyList; }
            set
            {
                // A tad painful since StringCollection doesn't support ICloneable
                StringCollection src = (StringCollection)value;
                StringCollection dst = new StringCollection();

                foreach (string s in src)
                    dst.Add(s);

                groupByPropertyList = dst;
                BuildQuery();
            }
        }

        /// <summary>
        ///    <para>Gets or sets the condition to be applied to the aggregation of
        ///       events, based on the number of events received.</para>
        /// </summary>
        /// <value>
        ///    <para>
        ///       Null, if no aggregation or no condition should be applied;
        ///       otherwise, a condition of the form "NumberOfEvents &lt;operator&gt;
        ///       &lt;value&gt;".</para>
        /// </value>
        /// <remarks>
        ///    <para> Setting this property value overrides any previous value stored in
        ///       the object. The query string is rebuilt to reflect the new grouping condition.</para>
        /// </remarks>
        /// <example>
        /// <para>This example creates a new <see cref='System.Management.WqlEventQuery'/>
        /// that represents the query: "SELECT * FROM <see langword='EmailEvent'/> GROUP
        /// WITHIN 300 HAVING <see langword='NumberOfEvents'/> &gt; 5", which means "send
        /// notification of all <see langword='EmailEvent'/> events, aggregated within
        /// 10-minute intervals, if there are more than 5 occurrences."</para>
        /// <code lang='C#'>WqlEventQuery q = new WqlEventQuery();
        /// q.EventClassName = "EmailEvent";
        /// q.GroupWithinInterval = new TimeSpan(0,10,0);
        /// q.HavingCondition = "NumberOfEvents &gt; 5";
        /// </code>
        /// <code lang='VB'>Dim q As New WqlEventQuery()
        /// q.EventClassName = "EmailEvent"
        /// q.GroupWithinInterval = new TimeSpan(0,10,0)
        /// q.HavingCondition = "NumberOfEvents &gt; 5"
        /// </code>
        /// </example>
        public string HavingCondition
        {
            get { return havingCondition ?? string.Empty; }
            set { havingCondition = value; BuildQuery(); }
        }


        /// <summary>
        ///  Builds the query string according to the current property values.
        /// </summary>
        protected internal void BuildQuery()
        {
            //If the event class name is not set we can't build a query
            //This shouldn't throw because the user may be in the process of setting properties...
            if ((eventClassName == null) || (eventClassName.Length == 0))
            {
                SetQueryString(string.Empty);
                return;
            }

            //Select clause
            string s = TokenSelectAll;    //no property list allowed here...

            //From clause
            s = s + "from " + eventClassName;

            //Within clause
            if (withinInterval != TimeSpan.Zero)
                s = s + " within " + withinInterval.TotalSeconds.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(double)));

            //Where clause
            if (!(Condition.Length == 0))
                s = s + " where " + condition;

            //Group within clause
            if (groupWithinInterval != TimeSpan.Zero)
            {
                s = s + " group within " + groupWithinInterval.TotalSeconds.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(double)));

                //Group By clause
                if ((null != groupByPropertyList) && (0 < groupByPropertyList.Count))
                {
                    int count = groupByPropertyList.Count;
                    s += " by ";

                    for (int i = 0; i < count; i++)
                        s = s + groupByPropertyList[i] + (i == (count - 1) ? "" : ",");
                }

                //Having clause
                if (!(HavingCondition.Length == 0))
                {
                    s = s + " having " + havingCondition;
                }
            }

            //Set the queryString member to the built query (NB: note we set
            //by accessing the internal helper function rather than the property,
            //since we do not want to force a parse of a query we just built).
            SetQueryString(s);

        }//BuildQuery

        /// <summary>
        ///  Parses the query string and sets the property values accordingly.
        /// </summary>
        /// <param name="query">The query string to be parsed.</param>
        protected internal override void ParseQuery(string query)
        {
            //Clear out previous property values
            eventClassName = null;
            withinInterval = TimeSpan.Zero;
            condition = null;
            groupWithinInterval = TimeSpan.Zero;
            groupByPropertyList?.Clear();
            havingCondition = null;

            //Trim whitespaces
            string q = query.Trim();
            int i;
            string w, tempProp;
            bool bFound = false;

            //Find "select" clause and make sure it's a select *
            string keyword = TokenSelect;
            if ((q.Length < keyword.Length) || (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException(SR.InvalidQuery);
            q = q.Remove(0, keyword.Length).TrimStart(null);

            if (!q.StartsWith("*", StringComparison.Ordinal))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, "*"), nameof(query));
            q = q.Remove(0, 1).TrimStart(null);

            //Find "from" clause
            keyword = "from ";
            if ((q.Length < keyword.Length) || (0 != string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));
            ParseToken(ref q, keyword, null, ref bFound, ref eventClassName);

            //Find "within" clause
            keyword = "within ";
            if ((q.Length >= keyword.Length) && (0 == string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
            {
                string intervalString = null; bFound = false;
                ParseToken(ref q, keyword, null, ref bFound, ref intervalString);
                withinInterval = TimeSpan.FromSeconds(((IConvertible)intervalString).ToDouble(null));
            }

            //Find "group within" clause
            keyword = "group within ";
            if ((q.Length >= keyword.Length) && ((i = q.ToLowerInvariant().IndexOf(keyword, StringComparison.Ordinal)) != -1)) //found
            {
                //Separate the part of the string before this - that should be the "where" clause
                w = q.Substring(0, i).Trim();
                q = q.Remove(0, i);

                string intervalString = null; bFound = false;
                ParseToken(ref q, keyword, null, ref bFound, ref intervalString);
                groupWithinInterval = TimeSpan.FromSeconds(((IConvertible)intervalString).ToDouble(null));

                //Find "By" subclause
                keyword = "by ";
                if ((q.Length >= keyword.Length) && (0 == string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                {
                    q = q.Remove(0, keyword.Length);
                    if (null != groupByPropertyList)
                        groupByPropertyList.Clear();
                    else
                        groupByPropertyList = new StringCollection();

                    //get the property list
                    while (true)
                    {
                        if ((i = q.IndexOf(',')) > 0)
                        {
                            tempProp = q.Substring(0, i);
                            q = q.Remove(0, i + 1).TrimStart(null);
                            tempProp = tempProp.Trim();
                            if (tempProp.Length > 0)
                                groupByPropertyList.Add(tempProp);
                        }
                        else
                        { //last property in the list
                            if ((i = q.IndexOf(' ')) > 0)
                            {
                                tempProp = q.Substring(0, i);
                                q = q.Remove(0, i).TrimStart(null);
                                groupByPropertyList.Add(tempProp);
                                break;
                            }
                            else //end of the query
                            {
                                groupByPropertyList.Add(q);
                                return;
                            }
                        }
                    } //while
                } //by

                //Find "Having" subclause
                keyword = "having "; bFound = false;
                if ((q.Length >= keyword.Length) && (0 == string.Compare(q, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase)))
                {   //the rest until the end is assumed to be the having condition
                    q = q.Remove(0, keyword.Length);

                    if (q.Length == 0) //bad query
                        throw new ArgumentException(SR.Format(SR.InvalidQueryTokenExpected, keyword), nameof(query));

                    havingCondition = q;
                }
            }
            else
                //No "group within" then everything should be the "where" clause
                w = q.Trim();

            //Find "where" clause
            keyword = "where ";
            if ((w.Length >= keyword.Length) && (0 == string.Compare(w, 0, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase))) //where clause exists
            {
                condition = w.Substring(keyword.Length);
            }

        }//ParseQuery()


        //ICloneable
        /// <summary>
        ///    <para>Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    The copied object.
        /// </returns>
        public override object Clone()
        {
            string[] strArray = null;

            if (null != groupByPropertyList)
            {
                int count = groupByPropertyList.Count;

                if (0 < count)
                {
                    strArray = new string[count];
                    groupByPropertyList.CopyTo(strArray, 0);
                }
            }

            return new WqlEventQuery(eventClassName, withinInterval, condition, groupWithinInterval,
                                                                            strArray, havingCondition);
        }

    }//WqlEventQuery


    /// <summary>
    /// Converts a String to a ManagementQuery
    /// </summary>
    internal sealed class ManagementQueryConverter : ExpandableObjectConverter
    {

        /// <summary>
        /// Determines if this converter can convert an object in the given source type to the native type of the converter.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='sourceType'>A Type that represents the type you wish to convert from.</param>
        /// <returns>
        ///    <para>true if this converter can perform the conversion; otherwise, false.</para>
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if ((sourceType == typeof(ManagementQuery)))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the given destination type using the context.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='destinationType'>A Type that represents the type you wish to convert to.</param>
        /// <returns>
        ///    <para>true if this converter can perform the conversion; otherwise, false.</para>
        /// </returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if ((destinationType == typeof(InstanceDescriptor)))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the destination type, this will
        ///      throw a NotSupportedException.
        /// </summary>
        /// <param name='context'>An ITypeDescriptorContext that provides a format context.</param>
        /// <param name='culture'>A CultureInfo object. If a null reference (Nothing in Visual Basic) is passed, the current culture is assumed.</param>
        /// <param name='value'>The Object to convert.</param>
        /// <param name='destinationType'>The Type to convert the value parameter to.</param>
        /// <returns>An Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {

            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (value is EventQuery && destinationType == typeof(InstanceDescriptor))
            {
                EventQuery obj = ((EventQuery)(value));
                ConstructorInfo ctor = typeof(EventQuery).GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { obj.QueryString });
                }
            }

            if (value is ObjectQuery && destinationType == typeof(InstanceDescriptor))
            {
                ObjectQuery obj = ((ObjectQuery)(value));
                ConstructorInfo ctor = typeof(ObjectQuery).GetConstructor(new Type[] { typeof(string) });
                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { obj.QueryString });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
