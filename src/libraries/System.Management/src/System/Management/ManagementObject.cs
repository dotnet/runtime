// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Management
{
    /// <summary>
    /// Delegate definition for the IdentifierChanged event.
    /// This event is used to signal the ManagementObject that an identifying property
    /// has been changed. Identifying properties are the ones that identify the object,
    /// namely the scope, path and options.
    /// </summary>
    internal delegate void IdentifierChangedEventHandler(object sender,
                    IdentifierChangedEventArgs e);

    /// <summary>
    /// Delegate definition for InternalObjectPut event. This is used so that
    /// the WmiEventSink can signal to this object that the async Put call has
    /// completed.
    /// </summary>
    internal delegate void InternalObjectPutEventHandler(object sender,
        InternalObjectPutEventArgs e);


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a data management object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates reading a property of a ManagementObject.
    /// class Sample_ManagementObject
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementObject disk = new ManagementObject(
    ///             "win32_logicaldisk.deviceid=\"c:\"");
    ///         disk.Get();
    ///         Console.WriteLine("Logical Disk Size = " + disk["Size"] + " bytes");
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates reading a property of a ManagementObject.
    /// Class Sample_ManagementObject
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim disk As New ManagementObject("win32_logicaldisk.deviceid=""c:""")
    ///         disk.Get()
    ///         Console.WriteLine(("Logical Disk Size = " &amp; disk("Size").ToString() _
    ///             &amp; " bytes"))
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ManagementObject : ManagementBaseObject, ICloneable
    {
        // constants
        internal const string ID = "ID";
        internal const string RETURNVALUE = "RETURNVALUE";

        //Fields

        private IWbemClassObjectFreeThreaded wmiClass;
        internal ManagementScope scope;
        internal ManagementPath path;
        internal ObjectGetOptions options;

        //Used to represent whether this managementObject is currently bound to a wbemObject
        //or not - whenever an "identifying" property is changed (Path, Scope...) the object
        //is "detached" (isBound becomes false) so that we refresh the wbemObject next time
        //it's used, in conformance with the new property values.
        //
        // ***
        // *    Changed isBound flag to wbemObject==null check.
        // *    private bool isBound;
        // ***

        //This is used to identify a state in which a Put() operation was performed, but the
        //object was not retrieved again, so the WMI object that's available at this point
        //cannot be used for certain operations, namely CreateInstance, GetSubclasses, Derive,
        //Clone & ClassPath.
        //When these operations are invoked, if we are in this state we need to implicitly
        //get the object...
        private bool putButNotGot;

        //Event fired when any "identifying" property changes.
        internal event IdentifierChangedEventHandler IdentifierChanged;

        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public new void Dispose()
        {
            if (wmiClass != null)
            {
                wmiClass.Dispose();
                wmiClass = null;
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        //Fires IdentifierChanged event

        internal void FireIdentifierChanged()
        {
            IdentifierChanged?.Invoke(this, null);
        }

        internal bool PutButNotGot
        {
            get
            { return putButNotGot; }
            set
            { putButNotGot = value; }
        }

        //Called when IdentifierChanged() event fires
        private void HandleIdentifierChange(object sender,
            IdentifierChangedEventArgs e)
        {
            // Detach the object from the WMI object underneath
            //
            // ***
            // *    Changed isBound flag to wbemObject==null check.
            // *    isBound = false;
            // ***
            wbemObject = null;
        }

        internal bool IsBound
        {
            get
            { return _wbemObject != null; }
        }

        //internal constructor
        internal static ManagementObject GetManagementObject(
            IWbemClassObjectFreeThreaded wbemObject,
            ManagementObject mgObj)
        {
            ManagementObject newObject = new ManagementObject();
            newObject.wbemObject = wbemObject;

            if (null != mgObj)
            {
                newObject.scope = ManagementScope._Clone(mgObj.scope);

                if (null != mgObj.path)
                    newObject.path = ManagementPath._Clone(mgObj.path);

                if (null != mgObj.options)
                    newObject.options = ObjectGetOptions._Clone(mgObj.options);

                // We set isBound last since assigning to Scope, Path
                // or Options can trigger isBound to be set false.
                //
                // ***
                // *    Changed isBound flag to wbemObject==null check.
                // *    newObject.isBound = mgObj.isBound;
                // ***
            }

            return newObject;
        }

        internal static ManagementObject GetManagementObject(
            IWbemClassObjectFreeThreaded wbemObject,
            ManagementScope scope)
        {
            ManagementObject newObject = new ManagementObject();
            newObject.wbemObject = wbemObject;

            newObject.path = new ManagementPath(ManagementPath.GetManagementPath(wbemObject));
            newObject.path.IdentifierChanged += new IdentifierChangedEventHandler(newObject.HandleIdentifierChange);

            newObject.scope = ManagementScope._Clone(scope, new IdentifierChangedEventHandler(newObject.HandleIdentifierChange));

            // Since we have an object, we should mark it as bound. Note
            // that we do this AFTER setting Scope and Path, since those
            // have side-effects of setting isBound=false.
            //
            // ***
            // *    Changed isBound flag to wbemObject==null check.
            // *    newObject.isBound = true;
            // ***

            return newObject;
        }

        //default constructor
        /// <overload>
        ///    Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class.
        /// </overload>
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class. This is the
        ///    default constructor.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject();
        ///
        /// //Now set the path on this object to bind it to a 'real' manageable entity
        /// o.Path = new ManagementPath("Win32_LogicalDisk='c:'");
        ///
        /// //Now it can be used
        /// Console.WriteLine(o["FreeSpace"]);
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject()
        /// Dim mp As New ManagementPath("Win32_LogicalDisk='c:'")
        ///
        /// 'Now set the path on this object to bind it to a 'real' manageable entity
        /// o.Path = mp
        ///
        /// 'Now it can be used
        /// Console.WriteLine(o("FreeSpace"))
        ///    </code>
        /// </example>
        public ManagementObject() : this((ManagementScope)null, null, null) { }

        //parameterized constructors
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class for the specified WMI
        ///    object path. The path is provided as a <see cref='System.Management.ManagementPath'/>.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> that contains a path to a WMI object.</param>
        /// <example>
        ///    <code lang='C#'>ManagementPath p = new ManagementPath("Win32_Service.Name='Alerter'");
        /// ManagementObject o = new ManagementObject(p);
        ///    </code>
        ///    <code lang='VB'>Dim p As New ManagementPath("Win32_Service.Name=""Alerter""")
        /// Dim o As New ManagementObject(p)
        ///    </code>
        /// </example>
        public ManagementObject(ManagementPath path) : this(null, path, null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class for the specified WMI object path. The path
        ///    is provided as a string.</para>
        /// </summary>
        /// <param name='path'>A WMI path.</param>
        /// <remarks>
        ///    <para>If the specified path is a relative path only (a server
        ///       or namespace is not specified), the default path is the local machine, and the
        ///       default namespace is the <see cref='System.Management.ManagementPath.DefaultPath'/>
        ///       path (by default, root\cimv2). If the user specifies a
        ///       full path, the default settings are overridden.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("Win32_Service.Name='Alerter'");
        ///
        /// //or with a full path :
        ///
        /// ManagementObject o = new ManagementObject("\\\\MyServer\\root\\MyApp:MyClass.Key='abc'");
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("Win32_Service.Name=""Alerter""")
        ///
        /// //or with a full path :
        ///
        /// Dim o As New ManagementObject("\\\\MyServer\\root\\MyApp:MyClass.Key=""abc""");
        ///    </code>
        /// </example>
        public ManagementObject(string path) : this(null, new ManagementPath(path), null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class bound to the specified
        ///    WMI path, including the specified additional options.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> containing the WMI path.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> containing additional options for binding to the WMI object. This parameter could be null if default options are to be used.</param>
        /// <example>
        ///    <code lang='C#'>ManagementPath p = new ManagementPath("Win32_ComputerSystem.Name='MyMachine'");
        ///
        /// //Set options for no context info, but requests amended qualifiers
        /// //to be contained in the object
        /// ObjectGetOptions opt = new ObjectGetOptions(null, true);
        ///
        /// ManagementObject o = new ManagementObject(p, opt);
        ///
        /// Console.WriteLine(o.GetQualifierValue("Description"));
        ///    </code>
        ///    <code lang='VB'>Dim p As New ManagementPath("Win32_ComputerSystem.Name=""MyMachine""")
        ///
        /// 'Set options for no context info, but requests amended qualifiers
        /// 'to be contained in the object
        /// Dim opt As New ObjectGetOptions(null, true)
        ///
        /// Dim o As New ManagementObject(p, opt)
        ///
        /// Console.WriteLine(o.GetQualifierValue("Description"));
        ///    </code>
        /// </example>
        public ManagementObject(ManagementPath path, ObjectGetOptions options) : this(null, path, options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class bound to the specified WMI path, including the
        ///    specified additional options. In this variant, the path can be specified as a
        ///    string.</para>
        /// </summary>
        /// <param name='path'>The WMI path to the object.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> representing options to get the specified WMI object.</param>
        /// <example>
        ///    <code lang='C#'>//Set options for no context info,
        /// //but requests amended qualifiers to be contained in the object
        /// ObjectGetOptions opt = new ObjectGetOptions(null, true);
        ///
        /// ManagementObject o = new ManagementObject("Win32_ComputerSystem.Name='MyMachine'", opt);
        ///
        /// Console.WriteLine(o.GetQualifierValue("Description"));
        ///    </code>
        ///    <code lang='VB'>'Set options for no context info,
        /// 'but requests amended qualifiers to be contained in the object
        /// Dim opt As New ObjectGetOptions(null, true)
        ///
        /// Dim o As New ManagementObject("Win32_ComputerSystem.Name=""MyMachine""", opt);
        ///
        /// Console.WriteLine(o.GetQualifierValue("Description"))
        ///    </code>
        /// </example>
        public ManagementObject(string path, ObjectGetOptions options) :
            this(new ManagementPath(path), options)
        { }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementObject'/>
        /// class bound to the specified WMI path that includes the specified options.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> representing the scope in which the WMI object resides. In this version, scopes can only be WMI namespaces.</param>
        /// <param name=' path'>A <see cref='System.Management.ManagementPath'/> representing the WMI path to the manageable object.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> specifying additional options for getting the object.</param>
        /// <remarks>
        ///    <para> Because WMI paths can be relative or full, a conflict between the scope and the path
        ///       specified may arise. However, if a scope is specified and
        ///       a relative WMI path is specified, then there is no conflict. The
        ///       following are some possible conflicts: </para>
        ///    <para> If a scope is not specified and a relative WMI
        ///       path is specified, then the scope will default to the local machine's <see cref='System.Management.ManagementPath.DefaultPath'/>. </para>
        ///    <para> If a scope is not specified and a full WMI path is
        ///       specified, then the scope will be inferred from the scope portion of the full
        ///       path. For example, the full WMI path: <c>\\MyMachine\root\MyNamespace:MyClass.Name='abc'</c> will
        ///    represent the WMI object 'MyClass.Name='abc'" in the scope
        ///    '\\MyMachine\root\MyNamespace'. </para>
        /// If a scope is specified and a full WMI path is specified, then the scope
        /// will override the scope portion of the full path. For example, if the following
        /// scope was specified: \\MyMachine\root\MyScope, and the following full path was
        /// specified: \\MyMachine\root\MyNamespace:MyClass.Name='abc', then look for the
        /// following <c>object:
        /// \\MyMachine\root\MyScope:MyClass.Name=
        /// 'abc'</c>
        /// (the scope part of the full path is ignored).
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope("\\\\MyMachine\\root\\cimv2");
        /// ManagementPath p = new ManagementPath("Win32_LogicalDisk.Name='c:'");
        /// ManagementObject o = new ManagementObject(s,p);
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope("\\MyMachine\root\cimv2");
        /// Dim p As New ManagementPath("Win32_LogicalDisk.Name=""c:""");
        /// Dim o As New ManagementObject(s,p);
        ///    </code>
        /// </example>
        public ManagementObject(ManagementScope scope, ManagementPath path, ObjectGetOptions options)
            : base(null)
        {
            ManagementObjectCTOR(scope, path, options);
        }

        private void ManagementObjectCTOR(ManagementScope scope, ManagementPath path, ObjectGetOptions options)
        {
            // We may use this to set the scope path
            string nsPath = string.Empty;

            if ((null != path) && !path.IsEmpty)
            {
                //If this is a ManagementObject then the path has to be an instance,
                // and if this is a ManagementClass the path has to be a class.
                if (GetType() == typeof(ManagementObject) && path.IsClass)
                    throw new ArgumentOutOfRangeException(nameof(path));
                else if (GetType() == typeof(ManagementClass) && path.IsInstance)
                    throw new ArgumentOutOfRangeException(nameof(path));

                // Save the namespace path portion of the path (if any) in case
                // we don't have a scope
                nsPath = path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);

                if ((null != scope) && (scope.Path.NamespacePath.Length > 0))
                {
                    // If the scope has a path too, the namespace portion of
                    // scope.path takes precedence over what is specified in path
                    path = new ManagementPath(path.RelativePath);
                    path.NamespacePath = scope.Path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);
                }

                // If the supplied path is a class or instance use it, otherwise
                // leave it empty
                if (path.IsClass || path.IsInstance)
                    this.path = ManagementPath._Clone(path, new IdentifierChangedEventHandler(HandleIdentifierChange));

                else
                    this.path = ManagementPath._Clone(null, new IdentifierChangedEventHandler(HandleIdentifierChange));
            }

            if (null != options)
                this.options = ObjectGetOptions._Clone(options, new IdentifierChangedEventHandler(HandleIdentifierChange));

            if (null != scope)
                this.scope = ManagementScope._Clone(scope, new IdentifierChangedEventHandler(HandleIdentifierChange));
            else
            {
                // Use the path if possible, otherwise let it default
                if (nsPath.Length > 0)
                {
                    this.scope = new ManagementScope(nsPath);
                    this.scope.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                }
            }

            //register for identifier change events
            IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
            // ***
            // *    Changed isBound flag to wbemObject==null check.
            // *    isBound = false;
            // ***
            putButNotGot = false;

        }

        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class
        ///    bound to the specified WMI path, and includes the specified options. The scope and
        ///    the path are specified as strings.</para>
        /// </summary>
        /// <param name='scopeString'>The scope for the WMI object.</param>
        /// <param name=' pathString'>The WMI object path.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> representing additional options for getting the WMI object.</param>
        /// <remarks>
        ///    <para>See the equivalent overload for details.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>GetObjectOptions opt = new GetObjectOptions(null, true);
        /// ManagementObject o = new ManagementObject("root\\MyNamespace", "MyClass.Name='abc'", opt);
        ///    </code>
        ///    <code lang='VB'>Dim opt As New GetObjectOptions(null, true)
        /// Dim o As New ManagementObject("root\MyNamespace", "MyClass.Name=""abc""", opt);
        ///    </code>
        /// </example>
        public ManagementObject(string scopeString, string pathString, ObjectGetOptions options)
            : this(new ManagementScope(scopeString), new ManagementPath(pathString), options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementObject'/> class that is serializable.</para>
        /// </summary>
        /// <param name='info'>The <see cref='System.Runtime.Serialization.SerializationInfo'/> to populate with data.</param>
        /// <param name='context'>The destination (see <see cref='System.Runtime.Serialization.StreamingContext'/> ) for this serialization.</param>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ManagementObject(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ManagementObjectCTOR(null, null, null);
        }

        /// <summary>
        ///    <para> Gets or sets the scope in which this object resides.</para>
        /// </summary>
        /// <value>
        /// <para> A <see cref='System.Management.ManagementScope'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para>
        ///       Changing
        ///       this property after the management object has been bound to a WMI object in
        ///       a particular namespace results in releasing the original WMI object. This causes the management object to
        ///       be rebound to the new object specified by the new path properties and scope
        ///       values. </para>
        ///    <para>The rebinding is performed in a "lazy" manner, that is, only when a requested
        ///       value requires the management object to be bound to the WMI object. Changes can
        ///       be made to more than just this property before attempting to rebind (for example, modifying the scope
        ///       and path properties simultaneously).</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>//Create the object with the default namespace (root\cimv2)
        /// ManagementObject o = new ManagementObject();
        ///
        /// //Change the scope (=namespace) of this object to the one specified.
        /// o.Scope = new ManagementScope("root\\MyAppNamespace");
        ///    </code>
        ///    <code lang='VB'>'Create the object with the default namespace (root\cimv2)
        /// Dim o As New ManagementObject()
        ///
        /// 'Change the scope (=namespace) of this object to the one specified.
        /// o.Scope = New ManagementScope("root\MyAppNamespace")
        ///    </code>
        /// </example>
        public ManagementScope Scope
        {
            get
            {
                return scope ??= ManagementScope._Clone(null);
            }
            set
            {
                if (null != value)
                {
                    if (null != scope)
                        scope.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    scope = ManagementScope._Clone((ManagementScope)value, new IdentifierChangedEventHandler(HandleIdentifierChange));

                    //the scope property has changed so fire event
                    FireIdentifierChanged();
                }
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        ///    <para> Gets or sets the object's WMI path.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementPath'/> representing the object's path.</para>
        /// </value>
        /// <remarks>
        ///    <para>
        ///       Changing the property after the management
        ///       object has been bound to a WMI object in a particular namespace results in releasing
        ///       the original WMI object. This causes the management object to be rebound to
        ///       the new object specified by the new path properties and scope values.</para>
        ///    <para>The rebinding is performed in a "lazy" manner, that is, only when a requested
        ///       value requires the management object to be bound to the WMI object. Changes can
        ///       be made to more than just the property before attempting to rebind (for example,
        ///       modifying the scope and path properties simultaneously).</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject();
        ///
        /// //Specify the WMI path to which this object should be bound to
        /// o.Path = new ManagementPath("MyClass.Name='MyName'");
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject()
        ///
        /// 'Specify the WMI path to which this object should be bound to
        /// o.Path = New ManagementPath("MyClass.Name=""MyName""");
        ///    </code>
        /// </example>
        public virtual ManagementPath Path
        {
            get
            {
                return path ??= ManagementPath._Clone(null);
            }
            set
            {
                ManagementPath newPath = value ?? new ManagementPath();

                //If the new path contains a namespace path and the scope is currently defaulted,
                //we want to set the scope to the new namespace path provided
                string nsPath = newPath.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);
                if ((nsPath.Length > 0) && (scope != null) && (scope.IsDefaulted))
                    Scope = new ManagementScope(nsPath);

                // This must be a class for a ManagementClass object or an instance for a ManagementObject, or empty
                if ((GetType() == typeof(ManagementObject) && newPath.IsInstance) ||
                    (GetType() == typeof(ManagementClass) && newPath.IsClass) ||
                    newPath.IsEmpty)
                {
                    if (null != path)
                        path.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    path = ManagementPath._Clone((ManagementPath)value, new IdentifierChangedEventHandler(HandleIdentifierChange));

                    //the path property has changed so fire event
                    FireIdentifierChanged();
                }
                else
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets or
        ///       sets additional information to use when retrieving the object.</para>
        /// </summary>
        /// <value>
        /// <para>An <see cref='System.Management.ObjectGetOptions'/> to use when retrieving the object.</para>
        /// </value>
        /// <remarks>
        ///    <para> When the property is
        ///       changed after the management object has been bound to a WMI object, the management object
        ///       is disconnected from the original WMI object and later rebound using the new
        ///       options.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>//Contains default options
        /// ManagementObject o = new ManagementObject("MyClass.Name='abc'");
        ///
        /// //Replace default options, in this case requesting retrieval of
        /// //amended qualifiers along with the WMI object.
        /// o.Options = new ObjectGetOptions(null, true);
        ///    </code>
        ///    <code lang='VB'>'Contains default options
        /// Dim o As New ManagementObject("MyClass.Name=""abc""")
        ///
        /// 'Replace default options, in this case requesting retrieval of
        /// 'amended qualifiers along with the WMI object.
        /// o.Options = New ObjectGetOptions(null, true)
        ///    </code>
        /// </example>
        public ObjectGetOptions Options
        {
            get
            {
                return options ??= ObjectGetOptions._Clone(null);
            }
            set
            {
                if (null != value)
                {
                    if (null != options)
                        options.IdentifierChanged -= new IdentifierChangedEventHandler(HandleIdentifierChange);

                    options = ObjectGetOptions._Clone((ObjectGetOptions)value, new IdentifierChangedEventHandler(HandleIdentifierChange));

                    //the options property has changed so fire event
                    FireIdentifierChanged();
                }
                else
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        ///    <para>Gets or sets the path to the object's class.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementPath'/> representing the path to the object's
        ///    class.</para>
        /// </value>
        /// <remarks>
        ///    <para>This property is read-only.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("MyClass.Name='abc'");
        ///
        /// //Get the class definition for the object above.
        /// ManagementClass c = new ManagementClass(o.ClassPath);
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("MyClass.Name=""abc""")
        ///
        /// 'Get the class definition for the object above.
        /// Dim c As New ManagementClass(o.ClassPath);
        ///    </code>
        /// </example>
        public override ManagementPath ClassPath
        {
            get
            {
                object serverName = null;
                object scopeName = null;
                object className = null;
                int propertyType = 0;
                int propertyFlavor = 0;

                if (PutButNotGot)
                {
                    Get();
                    PutButNotGot = false;
                }

                int status = wbemObject.Get_("__SERVER", 0, ref serverName, ref propertyType, ref propertyFlavor);

                if (status >= 0)
                {
                    status = wbemObject.Get_("__NAMESPACE", 0, ref scopeName, ref propertyType, ref propertyFlavor);

                    if (status >= 0)
                    {
                        status = wbemObject.Get_("__CLASS", 0, ref className, ref propertyType, ref propertyFlavor);
                    }
                }

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                ManagementPath classPath = new ManagementPath();

                // initialize in case of throw
                classPath.Server = string.Empty;
                classPath.NamespacePath = string.Empty;
                classPath.ClassName = string.Empty;

                // Some of these may throw if they are NULL
                try
                {
                    classPath.Server = (string)(serverName is System.DBNull ? "" : serverName);
                    classPath.NamespacePath = (string)(scopeName is System.DBNull ? "" : scopeName);
                    classPath.ClassName = (string)(className is System.DBNull ? "" : className);
                }
                catch
                {
                }

                return classPath;
            }
        }

        //
        //Methods
        //

        //******************************************************
        //Get
        //******************************************************
        /// <overload>
        ///    Binds to the management object.
        /// </overload>
        /// <summary>
        ///    <para> Binds to the management object.</para>
        /// </summary>
        /// <remarks>
        ///    <para> The method is implicitly
        ///       invoked at the first attempt to get or set information to the WMI object. It
        ///       can also be explicitly invoked at the user's discretion, to better control the
        ///       timing and manner of retrieval.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("MyClass.Name='abc'");
        /// string s = o["SomeProperty"]; //this causes an implicit Get().
        ///
        /// //or :
        ///
        /// ManagementObject o= new ManagementObject("MyClass.Name= 'abc'");
        /// o.Get(); //explicitly
        /// //Now it's faster because the object has already been retrieved.
        /// string s = o["SomeProperty"];
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("MyClass.Name=""abc""")
        /// string s = o("SomeProperty") 'this causes an implicit Get().
        ///
        /// 'or :
        ///
        /// Dim o As New ManagementObject("MyClass.Name= ""abc""")
        /// o.Get()  'explicitly
        /// 'Now it's faster because the object has already been retrieved.
        /// string s = o("SomeProperty");
        ///    </code>
        /// </example>
        public void Get()
        {
            IWbemClassObjectFreeThreaded tempObj = null;

            Initialize(false); // this may throw

            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else
            {
                ObjectGetOptions gOptions = options ?? new ObjectGetOptions();

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                try
                {
                    securityHandler = scope.GetSecurityHandler();

                    status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).GetObject_(path.RelativePath,
                                                            gOptions.Flags,
                                                            gOptions.GetContext(),
                                                            ref tempObj,
                                                            IntPtr.Zero);

                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }

                    wbemObject = tempObj;
                }
                finally
                {
                    securityHandler?.Reset();
                }
            }
        }

        //******************************************************
        //Get
        //******************************************************
        /// <summary>
        ///    <para> Binds to the management object asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to receive the results of the operation as events.</param>
        /// <remarks>
        ///    <para>The method will issue the request to get the object
        ///       and then will immediately return. The results of the operation will then be
        ///       delivered through events being fired on the watcher object provided.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("MyClass.Name='abc'");
        ///
        /// //Set up handlers for asynchronous get
        /// MyHandler h = new MyHandler();
        /// ManagementOperationObserver ob = new ManagementOperationObserver();
        /// ob.Completed += new CompletedEventHandler(h.Done);
        ///
        /// //Get the object asynchronously
        /// o.Get(ob);
        ///
        /// //Wait until operation is completed
        /// while (!h.Completed)
        ///     System.Threading.Thread.Sleep (1000);
        ///
        /// //Here we can use the object
        /// Console.WriteLine(o["SomeProperty"]);
        ///
        /// public class MyHandler
        /// {
        ///     private bool completed = false;
        ///
        ///     public void Done(object sender, CompletedEventArgs e) {
        ///         Console.WriteLine("async Get completed !");
        ///         completed = true;
        ///     }
        ///
        ///     public bool Completed {
        ///         get {
        ///             return completed;
        ///         }
        ///     }
        /// }
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("MyClass.Name=""abc""")
        ///
        /// 'Set up handlers for asynchronous get
        /// Dim h As New MyHandler()
        /// Dim ob As New ManagementOperationObserver()
        /// ob.Completed += New CompletedEventHandler(h.Done)
        ///
        /// 'Get the object asynchronously
        /// o.Get(ob)
        ///
        /// 'Wait until operation is completed
        /// While Not h.Completed
        ///     System.Threading.Thread.Sleep(1000)
        /// End While
        ///
        /// 'Here we can use the object
        /// Console.WriteLine(o("SomeProperty"))
        ///
        /// Public Class MyHandler
        ///     Private _completed As Boolean = false;
        ///
        ///     Public Sub Done(sender As Object, e As EventArrivedEventArgs)
        ///         Console.WriteLine("async Get completed !")
        ///         _completed = True
        ///     End Sub
        ///
        ///     Public ReadOnly Property Completed() As Boolean
        ///        Get
        ///            Return _completed
        ///        End Get
        ///     End Property
        /// End Class
        ///    </code>
        /// </example>
        public void Get(ManagementOperationObserver watcher)
        {
            Initialize(false);

            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                IWbemServices wbemServices = scope.GetIWbemServices();

                ObjectGetOptions o = ObjectGetOptions._Clone(options);

                WmiGetEventSink sink = watcher.GetNewGetSink(
                    scope,
                    o.Context,
                    this);

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(wbemServices).GetObjectAsync_(path.RelativePath,
                                            o.Flags,
                                            o.GetContext(),
                                            sink.Stub);


                securityHandler?.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //******************************************************
        //GetRelated
        //****************************************************
        /// <overload>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </overload>
        /// <summary>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementObjectCollection'/> containing the
        ///    related objects.</para>
        /// </returns>
        /// <remarks>
        ///    <para> The operation is equivalent to an ASSOCIATORS OF query where ResultClass = relatedClass.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("Win32_Service='Alerter'");
        /// foreach (ManagementBaseObject b in o.GetRelated())
        ///     Console.WriteLine("Object related to Alerter service : ", b.Path);
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("Win32_Service=""Alerter""")
        /// Dim b As ManagementBaseObject
        /// For Each b In o.GetRelated()
        ///     Console.WriteLine("Object related to Alerter service : ", b.Path)
        /// Next b
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetRelated()
        {
            return GetRelated((string)null);
        }

        //******************************************************
        //GetRelated
        //****************************************************
        /// <summary>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </summary>
        /// <param name='relatedClass'>A class of related objects. </param>
        /// <returns>
        ///    A <see cref='System.Management.ManagementObjectCollection'/> containing the related objects.
        /// </returns>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("Win32_Service='Alerter'");
        /// foreach (ManagementBaseObject b in o.GetRelated("Win32_Service")
        ///     Console.WriteLine("Service related to the Alerter service {0} is {1}", b["Name"], b["State"]);
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("Win32_Service=""Alerter""");
        /// Dim b As ManagementBaseObject
        /// For Each b in o.GetRelated("Win32_Service")
        ///     Console.WriteLine("Service related to the Alerter service {0} is {1}", b("Name"), b("State"))
        /// Next b
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetRelated(
            string relatedClass)
        {
            return GetRelated(relatedClass, null, null, null, null, null, false, null);
        }


        //******************************************************
        //GetRelated
        //****************************************************
        /// <summary>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </summary>
        /// <param name='relatedClass'>The class of the related objects. </param>
        /// <param name='relationshipClass'>The relationship class of interest. </param>
        /// <param name='relationshipQualifier'>The qualifier required to be present on the relationship class. </param>
        /// <param name='relatedQualifier'>The qualifier required to be present on the related class. </param>
        /// <param name='relatedRole'>The role that the related class is playing in the relationship. </param>
        /// <param name='thisRole'>The role that this class is playing in the relationship. </param>
        /// <param name='classDefinitionsOnly'>When this method returns, it contains only class definitions for the instances that match the query. </param>
        /// <param name='options'>Extended options for how to execute the query. </param>
        /// <returns>
        ///    A <see cref='System.Management.ManagementObjectCollection'/> containing the related objects.
        /// </returns>
        /// <remarks>
        ///    <para>This operation is equivalent to an ASSOCIATORS OF query where ResultClass = &lt;relatedClass&gt;.</para>
        /// </remarks>
        public ManagementObjectCollection GetRelated(
            string relatedClass,
            string relationshipClass,
            string relationshipQualifier,
            string relatedQualifier,
            string relatedRole,
            string thisRole,
            bool classDefinitionsOnly,
            EnumerationOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();

            Initialize(false);

            IEnumWbemClassObject enumWbem = null;
            EnumerationOptions o = options ?? new EnumerationOptions();
            RelatedObjectQuery q = new RelatedObjectQuery(
                path.Path,
                relatedClass,
                relationshipClass,
                relationshipQualifier,
                relatedQualifier, relatedRole,
                thisRole, classDefinitionsOnly);


            //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
            o.EnumerateDeep = true; //note this turns the FLAG to 0 !!

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecQuery_(
                                                        q.QueryLanguage,
                                                        q.QueryString,
                                                        o.Flags,
                                                        o.GetContext(),
                                                        ref enumWbem);


                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

            }
            finally
            {
                securityHandler?.Reset();
            }

            //Create collection object
            return new ManagementObjectCollection(scope, o, enumWbem);
        }


        //******************************************************
        //GetRelated
        //****************************************************
        /// <summary>
        ///    <para> Gets a collection of objects
        ///       related to the object (associators) asynchronously. This call returns immediately, and a
        ///       delegate is called when the results are available.</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        public void GetRelated(
            ManagementOperationObserver watcher)
        {
            GetRelated(watcher, (string)null);
        }

        //******************************************************
        //GetRelated
        //****************************************************
        /// <summary>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        /// <param name='relatedClass'>The class of related objects. </param>
        /// <remarks>
        ///    <para>This operation is equivalent to an ASSOCIATORS OF query where ResultClass = &lt;relatedClass&gt;.</para>
        /// </remarks>
        public void GetRelated(
            ManagementOperationObserver watcher,
            string relatedClass)
        {
            GetRelated(watcher, relatedClass, null, null, null, null, null, false, null);
        }


        //******************************************************
        //GetRelated
        //****************************************************
        /// <summary>
        ///    <para>Gets a collection of objects related to the object (associators).</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        /// <param name='relatedClass'>The class of the related objects. </param>
        /// <param name='relationshipClass'>The relationship class of interest. </param>
        /// <param name='relationshipQualifier'>The qualifier required to be present on the relationship class. </param>
        /// <param name='relatedQualifier'>The qualifier required to be present on the related class. </param>
        /// <param name='relatedRole'>The role that the related class is playing in the relationship. </param>
        /// <param name='thisRole'>The role that this class is playing in the relationship. </param>
        /// <param name='classDefinitionsOnly'>Return only class definitions for the instances that match the query. </param>
        /// <param name='options'>Extended options for how to execute the query.</param>
        /// <remarks>
        ///    <para>This operation is equivalent to an ASSOCIATORS OF query where ResultClass = &lt;relatedClass&gt;.</para>
        /// </remarks>
        public void GetRelated(
            ManagementOperationObserver watcher,
            string relatedClass,
            string relationshipClass,
            string relationshipQualifier,
            string relatedQualifier,
            string relatedRole,
            string thisRole,
            bool classDefinitionsOnly,
            EnumerationOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();

            Initialize(true);

            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                // Ensure we switch off ReturnImmediately as this is invalid for async calls
                EnumerationOptions o = (null != options)
                    ? (EnumerationOptions)options.Clone() : new EnumerationOptions();
                o.ReturnImmediately = false;

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewSink(
                    scope,
                    o.Context);

                RelatedObjectQuery q = new RelatedObjectQuery(path.Path, relatedClass,
                    relationshipClass, relationshipQualifier,
                    relatedQualifier, relatedRole,
                    thisRole, classDefinitionsOnly);


                //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
                o.EnumerateDeep = true; //note this turns the FLAG to 0 !!

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecQueryAsync_(
                                                        q.QueryLanguage,
                                                        q.QueryString,
                                                        o.Flags,
                                                        o.GetContext(),
                                                        sink.Stub);


                securityHandler.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <overload>
        ///    Gets a collection of associations to the object.
        /// </overload>
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementObjectCollection'/> containing the association objects.</para>
        /// </returns>
        /// <remarks>
        ///    <para> The operation is equivalent to a REFERENCES OF query.</para>
        /// </remarks>
        public ManagementObjectCollection GetRelationships()
        {
            return GetRelationships((string)null);
        }

        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <param name='relationshipClass'>The associations to include. </param>
        /// <returns>
        ///    A <see cref='System.Management.ManagementObjectCollection'/> containing the association objects.
        /// </returns>
        /// <remarks>
        ///    <para>This operation is equivalent to a REFERENCES OF query where the AssocClass = &lt;relationshipClass&gt;.</para>
        /// </remarks>
        public ManagementObjectCollection GetRelationships(
            string relationshipClass)
        {
            return GetRelationships(relationshipClass, null, null, false, null);
        }


        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <param name='relationshipClass'>The type of relationship of interest. </param>
        /// <param name='relationshipQualifier'>The qualifier to be present on the relationship. </param>
        /// <param name='thisRole'>The role of this object in the relationship. </param>
        /// <param name='classDefinitionsOnly'>When this method returns, it contains only the class definitions for the result set. </param>
        /// <param name='options'>The extended options for the query execution. </param>
        /// <returns>
        ///    A <see cref='System.Management.ManagementObjectCollection'/> containing the association objects.
        /// </returns>
        /// <remarks>
        ///    <para>This operation is equivalent to a REFERENCES OF query with possibly all the extensions.</para>
        /// </remarks>
        public ManagementObjectCollection GetRelationships(
            string relationshipClass,
            string relationshipQualifier,
            string thisRole,
            bool classDefinitionsOnly,
            EnumerationOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();

            Initialize(false);

            IEnumWbemClassObject enumWbem = null;
            EnumerationOptions o = options ?? new EnumerationOptions();
            RelationshipQuery q = new RelationshipQuery(path.Path, relationshipClass,
                relationshipQualifier, thisRole, classDefinitionsOnly);


            //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
            o.EnumerateDeep = true; //note this turns the FLAG to 0 !!

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecQuery_(
                                                    q.QueryLanguage,
                                                    q.QueryString,
                                                    o.Flags,
                                                    o.GetContext(),
                                                    ref enumWbem);


                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

            }
            finally
            {
                securityHandler?.Reset();
            }

            //Create collection object
            return new ManagementObjectCollection(scope, o, enumWbem);
        }


        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        /// <remarks>
        /// This operation is equivalent to a REFERENCES OF query
        /// </remarks>
        public void GetRelationships(
            ManagementOperationObserver watcher)
        {
            GetRelationships(watcher, (string)null);
        }

        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        /// <param name='relationshipClass'>The associations to include. </param>
        /// <remarks>
        ///    <para>This operation is equivalent to a REFERENCES OF query where the AssocClass = &lt;relationshipClass&gt;.</para>
        /// </remarks>
        public void GetRelationships(
            ManagementOperationObserver watcher,
            string relationshipClass)
        {
            GetRelationships(watcher, relationshipClass, null, null, false, null);
        }


        //*******************************************************************
        //GetRelationships
        //*******************************************************************
        /// <summary>
        ///    <para>Gets a collection of associations to the object.</para>
        /// </summary>
        /// <param name='watcher'>The object to use to return results. </param>
        /// <param name='relationshipClass'>The type of relationship of interest. </param>
        /// <param name='relationshipQualifier'>The qualifier to be present on the relationship. </param>
        /// <param name='thisRole'>The role of this object in the relationship. </param>
        /// <param name='classDefinitionsOnly'>When this method returns, it contains only the class definitions for the result set. </param>
        /// <param name='options'>The extended options for the query execution. </param>
        /// <remarks>
        ///    <para>This operation is equivalent to a REFERENCES OF query with possibly all the extensions.</para>
        /// </remarks>
        public void GetRelationships(
            ManagementOperationObserver watcher,
            string relationshipClass,
            string relationshipQualifier,
            string thisRole,
            bool classDefinitionsOnly,
            EnumerationOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                Initialize(false);

                // Ensure we switch off ReturnImmediately as this is invalid for async calls
                EnumerationOptions o =
                    (null != options) ? (EnumerationOptions)options.Clone() :
                    new EnumerationOptions();
                o.ReturnImmediately = false;

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewSink(scope, o.Context);

                RelationshipQuery q = new RelationshipQuery(path.Path, relationshipClass,
                    relationshipQualifier, thisRole, classDefinitionsOnly);


                //Make sure the EnumerateDeep flag bit is turned off because it's invalid for queries
                o.EnumerateDeep = true; //note this turns the FLAG to 0 !!

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecQueryAsync_(
                                                        q.QueryLanguage,
                                                        q.QueryString,
                                                        o.Flags,
                                                        o.GetContext(),
                                                        sink.Stub);


                securityHandler?.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //******************************************************
        //Put
        //******************************************************
        /// <overload>
        ///    Commits the changes to the object.
        /// </overload>
        /// <summary>
        ///    <para>Commits the changes to the object.</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementPath'/> containing the path to the committed
        ///    object.</para>
        /// </returns>
        public ManagementPath Put()
        {
            return Put((PutOptions)null);
        }


        //******************************************************
        //Put
        //******************************************************
        /// <summary>
        ///    <para>Commits the changes to the object.</para>
        /// </summary>
        /// <param name='options'>The options for how to commit the changes. </param>
        /// <returns>
        ///    A <see cref='System.Management.ManagementPath'/> containing the path to the committed object.
        /// </returns>
        public ManagementPath Put(PutOptions options)
        {
            ManagementPath newPath = null;
            Initialize(true);
            PutOptions o = options ?? new PutOptions();

            IWbemServices wbemServices = scope.GetIWbemServices();

            //
            // Must do this convoluted allocation since the IWbemServices ref IWbemCallResult
            // has been redefined to be an IntPtr.  Due to the fact that it wasn't possible to
            // pass NULL for the optional argument.
            //
            IntPtr ppwbemCallResult = IntPtr.Zero;
            IntPtr pwbemCallResult = IntPtr.Zero;
            IWbemCallResult wbemCallResult = null;
            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = scope.GetSecurityHandler();

                ppwbemCallResult = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(ppwbemCallResult, IntPtr.Zero);        // Init to NULL.

                if (IsClass)
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutClass_(wbemObject,
                        o.Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY,
                        o.GetContext(),
                        ppwbemCallResult);
                }
                else
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutInstance_(wbemObject,
                        o.Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY,
                        o.GetContext(),
                        ppwbemCallResult);
                }


                // Keep this statement here; otherwise, there'll be a leak in error cases.
                pwbemCallResult = Marshal.ReadIntPtr(ppwbemCallResult);

                wbemCallResult = (IWbemCallResult)Marshal.GetObjectForIUnknown(pwbemCallResult);

                int hr;
                status = wbemCallResult.GetCallStatus_((int)tag_WBEM_TIMEOUT_TYPE.WBEM_INFINITE, out hr);

                if (status >= 0)
                    status = hr;

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                newPath = GetPath(wbemCallResult);
            }
            finally
            {
                securityHandler?.Reset();

                if (ppwbemCallResult != IntPtr.Zero)                    // Cleanup from allocations above.
                    Marshal.FreeHGlobal(ppwbemCallResult);

                if (pwbemCallResult != IntPtr.Zero)
                    Marshal.Release(pwbemCallResult);

                if (wbemCallResult != null)
                    Marshal.ReleaseComObject(wbemCallResult);
            }

            //Set the flag that tells the object that we've put it, so that a refresh is
            //triggered when an operation that needs this is invoked (CreateInstance, Derive).
            putButNotGot = true;

            // Update our path to address the object just put. Note that
            // we do this in such a way as to NOT trigger the setting of this
            // ManagementObject into an unbound state
            path.SetRelativePath(newPath.RelativePath);

            return newPath;
        }

        private ManagementPath GetPath(IWbemCallResult callResult)
        {
            ManagementPath newPath = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                //
                // Obtain the path from the call result.
                // Note this will return the relative path at best.
                //
                string resultPath = null;

                status = callResult.GetResultString_(
                    (int)tag_WBEM_TIMEOUT_TYPE.WBEM_INFINITE,
                    out resultPath);

                if (status >= 0)
                {
                    newPath = new ManagementPath(scope.Path.Path);
                    newPath.RelativePath = resultPath;
                }
                else
                {
                    //
                    // That didn't work. Use the path in the object instead.
                    //
                    object pathValue = GetPropertyValue("__PATH");

                    // No path? Try Relpath?
                    if (pathValue != null)
                        newPath = new ManagementPath((string)pathValue);
                    else
                    {
                        pathValue = GetPropertyValue("__RELPATH");

                        if (pathValue != null)
                        {
                            newPath = new ManagementPath(scope.Path.Path);
                            newPath.RelativePath = (string)pathValue;
                        }
                    }
                }

            }
            catch
            {
            }

            return newPath ?? new ManagementPath();
        }

        /// <summary>
        ///    <para>Commits the changes to the object, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>A <see cref='System.Management.ManagementOperationObserver'/> used to handle the progress and results of the asynchronous operation.</param>
        public void Put(ManagementOperationObserver watcher)
        {
            Put(watcher, null);
        }

        /// <summary>
        ///    <para>Commits the changes to the object asynchronously and
        ///       using the specified options.</para>
        /// </summary>
        /// <param name='watcher'>A <see cref='System.Management.ManagementOperationObserver'/> used to handle the progress and results of the asynchronous operation.</param>
        /// <param name=' options'>A <see cref='System.Management.PutOptions'/> used to specify additional options for the commit operation.</param>
        public void Put(ManagementOperationObserver watcher, PutOptions options)
        {
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                Initialize(false);

                PutOptions o = (null == options) ?
                    new PutOptions() : (PutOptions)options.Clone();

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                IWbemServices wbemServices = scope.GetIWbemServices();
                WmiEventSink sink = watcher.GetNewPutSink(scope,
                    o.Context, scope.Path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY), ClassName);

                // Add ourselves to the watcher so we can update our state
                sink.InternalObjectPut +=
                    new InternalObjectPutEventHandler(this.HandleObjectPut);

                SecurityHandler securityHandler = null;
                // Assign to error initially to insure internal event handler cleanup
                // on non-management exception.
                int status = (int)ManagementStatus.Failed;

                securityHandler = scope.GetSecurityHandler();

                if (IsClass)
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutClassAsync_(
                        wbemObject,
                        o.Flags,
                        o.GetContext(),
                        sink.Stub);
                }
                else
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutInstanceAsync_(
                        wbemObject,
                        o.Flags,
                        o.GetContext(),
                        sink.Stub);
                }


                securityHandler?.Reset();

                if (status < 0)
                {
                    sink.InternalObjectPut -= new InternalObjectPutEventHandler(this.HandleObjectPut);
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        internal void HandleObjectPut(object sender, InternalObjectPutEventArgs e)
        {
            try
            {
                if (sender is WmiEventSink)
                {
                    ((WmiEventSink)sender).InternalObjectPut -= new InternalObjectPutEventHandler(this.HandleObjectPut);
                    putButNotGot = true;
                    path.SetRelativePath(e.Path.RelativePath);
                }
            }
            catch
            {
            }
        }

        //******************************************************
        //CopyTo
        //******************************************************
        /// <overload>
        ///    Copies the object to a different location.
        /// </overload>
        /// <summary>
        ///    <para>Copies the object to a different location.</para>
        /// </summary>
        /// <param name='path'>The <see cref='System.Management.ManagementPath'/> to which the object should be copied. </param>
        /// <returns>
        ///    <para>The new path of the copied object.</para>
        /// </returns>
        public ManagementPath CopyTo(ManagementPath path)
        {
            return CopyTo(path, (PutOptions)null);
        }

        /// <summary>
        ///    <para>Copies the object to a different location.</para>
        /// </summary>
        /// <param name='path'>The path to which the object should be copied. </param>
        /// <returns>
        ///    The new path of the copied object.
        /// </returns>
        public ManagementPath CopyTo(string path)
        {
            return CopyTo(new ManagementPath(path), (PutOptions)null);
        }

        /// <summary>
        ///    <para>Copies the object to a different location.</para>
        /// </summary>
        /// <param name='path'>The path to which the object should be copied.</param>
        /// <param name='options'>The options for how the object should be put.</param>
        /// <returns>
        ///    The new path of the copied object.
        /// </returns>
        public ManagementPath CopyTo(string path, PutOptions options)
        {
            return CopyTo(new ManagementPath(path), options);
        }

        /// <summary>
        ///    <para>Copies the object to a different location.</para>
        /// </summary>
        /// <param name='path'>The <see cref='System.Management.ManagementPath'/> to which the object should be copied.</param>
        /// <param name='options'>The options for how the object should be put.</param>
        /// <returns>
        ///    The new path of the copied object.
        /// </returns>
        public ManagementPath CopyTo(ManagementPath path, PutOptions options)
        {
            Initialize(false);

            ManagementScope destinationScope = null;

            // Build a scope for our target destination
            destinationScope = new ManagementScope(path, scope);
            destinationScope.Initialize();

            PutOptions o = options ?? new PutOptions();
            IWbemServices wbemServices = destinationScope.GetIWbemServices();
            ManagementPath newPath = null;

            //
            // TO-DO : This code is almost identical to Put - should consolidate.
            //
            // Must do this convoluted allocation since the IWbemServices ref IWbemCallResult
            // has been redefined to be an IntPtr.  Due to the fact that it wasn't possible to
            // pass NULL for the optional argument.
            //
            IntPtr ppwbemCallResult = IntPtr.Zero;
            IntPtr pwbemCallResult = IntPtr.Zero;
            IWbemCallResult wbemCallResult = null;
            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = destinationScope.GetSecurityHandler();

                ppwbemCallResult = Marshal.AllocHGlobal(IntPtr.Size);
                Marshal.WriteIntPtr(ppwbemCallResult, IntPtr.Zero);        // Init to NULL.

                if (IsClass)
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutClass_(
                        wbemObject,
                        o.Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY,
                        o.GetContext(),
                        ppwbemCallResult);
                }
                else
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).PutInstance_(
                        wbemObject,
                        o.Flags | (int)tag_WBEM_GENERIC_FLAG_TYPE.WBEM_FLAG_RETURN_IMMEDIATELY,
                        o.GetContext(),
                        ppwbemCallResult);

                }


                // Keep this statement here; otherwise, there'll be a leak in error cases.
                pwbemCallResult = Marshal.ReadIntPtr(ppwbemCallResult);

                //Use the CallResult to retrieve the resulting object path
                wbemCallResult = (IWbemCallResult)Marshal.GetObjectForIUnknown(pwbemCallResult);

                int hr;
                status = wbemCallResult.GetCallStatus_((int)tag_WBEM_TIMEOUT_TYPE.WBEM_INFINITE, out hr);

                if (status >= 0)
                    status = hr;

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                newPath = GetPath(wbemCallResult);
                newPath.NamespacePath = path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);
            }
            finally
            {
                securityHandler?.Reset();

                if (ppwbemCallResult != IntPtr.Zero)                    // Cleanup from allocations above.
                    Marshal.FreeHGlobal(ppwbemCallResult);

                if (pwbemCallResult != IntPtr.Zero)
                    Marshal.Release(pwbemCallResult);

                if (wbemCallResult != null)
                    Marshal.ReleaseComObject(wbemCallResult);
            }

            return newPath;
        }

        /// <summary>
        ///    <para>Copies the object to a different location, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> specifying the path to which the object should be copied.</param>
        public void CopyTo(ManagementOperationObserver watcher, ManagementPath path)
        {
            CopyTo(watcher, path, null);
        }

        /// <summary>
        ///    <para>Copies the object to a different location, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        /// <param name='path'> The path to which the object should be copied.</param>
        public void CopyTo(ManagementOperationObserver watcher, string path)
        {
            CopyTo(watcher, new ManagementPath(path), null);
        }

        /// <summary>
        ///    <para>Copies the object to a different location, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        /// <param name='path'>The path to which the object should be copied.</param>
        /// <param name='options'>The options for how the object should be put.</param>
        public void CopyTo(ManagementOperationObserver watcher, string path, PutOptions options)
        {
            CopyTo(watcher, new ManagementPath(path), options);
        }

        /// <summary>
        ///    <para>Copies the object to a different location, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        /// <param name='path'>The path to which the object should be copied.</param>
        /// <param name='options'>The options for how the object should be put.</param>
        public void CopyTo(ManagementOperationObserver watcher, ManagementPath path, PutOptions options)
        {
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                Initialize(false);
                ManagementScope destinationScope = null;

                destinationScope = new ManagementScope(path, scope);
                destinationScope.Initialize();

                PutOptions o = (null != options) ? (PutOptions)options.Clone() : new PutOptions();

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewPutSink(destinationScope, o.Context,
                    path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY), ClassName);
                IWbemServices destWbemServices = destinationScope.GetIWbemServices();

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = destinationScope.GetSecurityHandler();

                if (IsClass)
                {
                    status = destinationScope.GetSecuredIWbemServicesHandler(destWbemServices).PutClassAsync_(
                                                    wbemObject,
                                                    o.Flags,
                                                    o.GetContext(),
                                                    sink.Stub);

                }
                else
                {
                    status = destinationScope.GetSecuredIWbemServicesHandler(destWbemServices).PutInstanceAsync_(
                                                    wbemObject,
                                                    o.Flags,
                                                    o.GetContext(),
                                                    sink.Stub);
                }


                securityHandler?.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //******************************************************
        //Delete
        //******************************************************
        /// <overload>
        ///    Deletes the object.
        /// </overload>
        /// <summary>
        ///    <para>Deletes the object.</para>
        /// </summary>
        public void Delete()
        {
            Delete((DeleteOptions)null);
        }

        /// <summary>
        ///    <para>Deletes the object.</para>
        /// </summary>
        /// <param name='options'>The options for how to delete the object. </param>
        public void Delete(DeleteOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();

            Initialize(false);
            DeleteOptions o = options ?? new DeleteOptions();
            IWbemServices wbemServices = scope.GetIWbemServices();

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = scope.GetSecurityHandler();

                if (IsClass)
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).DeleteClass_(
                        path.RelativePath,
                        o.Flags,
                        o.GetContext(),
                        IntPtr.Zero);
                }
                else
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).DeleteInstance_(
                        path.RelativePath,
                        o.Flags,
                        o.GetContext(),
                        IntPtr.Zero);
                }


                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
            finally
            {
                securityHandler?.Reset();
            }
        }


        /// <summary>
        ///    <para>Deletes the object.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        public void Delete(ManagementOperationObserver watcher)
        {
            Delete(watcher, null);
        }

        /// <summary>
        ///    <para>Deletes the object.</para>
        /// </summary>
        /// <param name='watcher'>The object that will receive the results of the operation.</param>
        /// <param name='options'>The options for how to delete the object.</param>
        public void Delete(ManagementOperationObserver watcher, DeleteOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                Initialize(false);
                DeleteOptions o = (null != options) ? (DeleteOptions)options.Clone() : new DeleteOptions();

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                IWbemServices wbemServices = scope.GetIWbemServices();
                WmiEventSink sink = watcher.GetNewSink(scope, o.Context);

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = scope.GetSecurityHandler();

                if (IsClass)
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).DeleteClassAsync_(path.RelativePath,
                        o.Flags,
                        o.GetContext(),
                        sink.Stub);
                }
                else
                {
                    status = scope.GetSecuredIWbemServicesHandler(wbemServices).DeleteInstanceAsync_(path.RelativePath,
                        o.Flags,
                        o.GetContext(),
                        sink.Stub);
                }


                securityHandler?.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //******************************************************
        //InvokeMethod
        //******************************************************
        /// <overload>
        ///    <para>Invokes a method on the object.</para>
        /// </overload>
        /// <summary>
        ///    <para>
        ///       Invokes a method on the object.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method to execute. </param>
        /// <param name='args'>An array containing parameter values. </param>
        /// <returns>
        ///    <para>The value returned by the method.</para>
        /// </returns>
        /// <remarks>
        ///    <para>If the method is static, the execution
        ///       should still succeed.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        ///
        /// // This sample demonstrates invoking a WMI method using an array of arguments.
        /// public class InvokeMethod
        /// {
        ///     public static void Main()
        ///     {
        ///
        ///         //Get the object on which the method will be invoked
        ///         ManagementClass processClass = new ManagementClass("Win32_Process");
        ///
        ///         //Create an array containing all arguments for the method
        ///         object[] methodArgs = {"notepad.exe", null, null, 0};
        ///
        ///         //Execute the method
        ///         object result = processClass.InvokeMethod ("Create", methodArgs);
        ///
        ///         //Display results
        ///         Console.WriteLine ("Creation of process returned: " + result);
        ///         Console.WriteLine ("Process id: " + methodArgs[3]);
        ///     }
        ///
        /// }
        ///    </code>
        ///    <code lang='VB'>Imports System
        /// Imports System.Management
        ///
        /// ' This sample demonstrates invoking a WMI method using an array of arguments.
        /// Class InvokeMethod
        ///     Public Overloads Shared Function Main(ByVal args() As String) As Integer
        ///
        ///         ' Get the object on which the method will be invoked
        ///         Dim processClass As New ManagementClass("Win32_Process")
        ///
        ///         ' Create an array containing all arguments for the method
        ///         Dim methodArgs() As Object = {"notepad.exe", Nothing, Nothing, 0}
        ///
        ///         ' Execute the method
        ///         Dim result As Object = processClass.InvokeMethod("Create", methodArgs)
        ///
        ///         'Display results
        ///         Console.WriteLine("Creation of process returned: {0}", result)
        ///         Console.WriteLine("Process id: {0}", methodArgs(3))
        ///         Return 0
        ///     End Function
        /// End Class
        ///    </code>
        /// </example>
        public object InvokeMethod(string methodName, object[] args)
        {
            object result = null;

            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == methodName)
                throw new ArgumentNullException(nameof(methodName));
            else
            {
                Initialize(false);

                // Map args into a inparams structure
                ManagementBaseObject inParameters;
                IWbemClassObjectFreeThreaded inParametersClass, outParametersClass;
                GetMethodParameters(methodName, out inParameters,
                    out inParametersClass, out outParametersClass);

                MapInParameters(args, inParameters, inParametersClass);

                // Call ExecMethod
                ManagementBaseObject outParameters =
                    InvokeMethod(methodName, inParameters, null);

                // Map outparams to args
                result = MapOutParameters(args, outParameters, outParametersClass);
            }

            return result;
        }

        //******************************************************
        //InvokeMethod
        //******************************************************
        /// <summary>
        ///    <para>Invokes a method on the object, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to receive the results of the operation.</param>
        /// <param name='methodName'>The name of the method to execute. </param>
        /// <param name='args'>An array containing parameter values. </param>
        /// <remarks>
        ///    <para>If the method is static, the execution
        ///       should still succeed.</para>
        /// </remarks>
        public void InvokeMethod(
            ManagementOperationObserver watcher,
            string methodName,
            object[] args)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else if (null == methodName)
                throw new ArgumentNullException(nameof(methodName));
            else
            {
                Initialize(false);

                // Map args into a inparams structure
                ManagementBaseObject inParameters;
                IWbemClassObjectFreeThreaded inParametersClass, outParametersClass;
                GetMethodParameters(methodName, out inParameters,
                    out inParametersClass, out outParametersClass);

                MapInParameters(args, inParameters, inParametersClass);

                // Call the method
                InvokeMethod(watcher, methodName, inParameters, null);
            }
        }

        /// <summary>
        ///    <para>Invokes a method on the WMI object. The input and output
        ///       parameters are represented as <see cref='System.Management.ManagementBaseObject'/>
        ///       objects.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method to execute.</param>
        /// <param name=' inParameters'>A <see cref='System.Management.ManagementBaseObject'/> holding the input parameters to the method.</param>
        /// <param name=' options'>An <see cref='System.Management.InvokeMethodOptions'/> containing additional options for the execution of the method.</param>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> containing the
        ///    output parameters and return value of the executed method.</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        ///
        /// // This sample demonstrates invoking a WMI method using parameter objects
        /// public class InvokeMethod
        /// {
        ///     public static void Main()
        ///     {
        ///
        ///         //Get the object on which the method will be invoked
        ///         ManagementClass processClass = new ManagementClass("Win32_Process");
        ///
        ///         //Get an input parameters object for this method
        ///         ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
        ///
        ///         //Fill in input parameter values
        ///         inParams["CommandLine"] = "calc.exe";
        ///
        ///         //Execute the method
        ///         ManagementBaseObject outParams = processClass.InvokeMethod ("Create", inParams, null);
        ///
        ///         //Display results
        ///         //Note: The return code of the method is provided in the "returnValue" property of the outParams object
        ///         Console.WriteLine("Creation of calculator process returned: " + outParams["returnValue"]);
        ///         Console.WriteLine("Process ID: " + outParams["processId"]);
        ///    }
        /// }
        ///    </code>
        ///    <code lang='VB'>
        /// Imports System
        /// Imports System.Management
        ///
        /// ' This sample demonstrates invoking a WMI method using parameter objects
        /// Class InvokeMethod
        ///     Public Overloads Shared Function Main(ByVal args() As String) As Integer
        ///
        ///         ' Get the object on which the method will be invoked
        ///         Dim processClass As New ManagementClass("Win32_Process")
        ///
        ///          ' Get an input parameters object for this method
        ///         Dim inParams As ManagementBaseObject = processClass.GetMethodParameters("Create")
        ///
        ///         ' Fill in input parameter values
        ///         inParams("CommandLine") = "calc.exe"
        ///
        ///         ' Execute the method
        ///         Dim outParams As ManagementBaseObject = processClass.InvokeMethod("Create", inParams, Nothing)
        ///
        ///         ' Display results
        ///         ' Note: The return code of the method is provided in the "returnValue" property of the outParams object
        ///         Console.WriteLine("Creation of calculator process returned: {0}", outParams("returnValue"))
        ///         Console.WriteLine("Process ID: {0}", outParams("processId"))
        ///
        ///         Return 0
        ///     End Function
        /// End Class
        ///    </code>
        /// </example>
        public ManagementBaseObject InvokeMethod(
            string methodName,
            ManagementBaseObject inParameters,
            InvokeMethodOptions options)
        {
            ManagementBaseObject outParameters = null;

            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == methodName)
                throw new ArgumentNullException(nameof(methodName));
            else
            {
                Initialize(false);
                InvokeMethodOptions o = options ?? new InvokeMethodOptions();
                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                try
                {
                    securityHandler = scope.GetSecurityHandler();

                    IWbemClassObjectFreeThreaded inParams = inParameters?.wbemObject;
                    IWbemClassObjectFreeThreaded outParams = null;

                    status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecMethod_(
                        path.RelativePath,
                        methodName,
                        o.Flags,
                        o.GetContext(),
                        inParams,
                        ref outParams,
                        IntPtr.Zero);


                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }

                    if (outParams != null)
                        outParameters = new ManagementBaseObject(outParams);
                }
                finally
                {
                    securityHandler?.Reset();
                }
            }

            return outParameters;
        }

        /// <summary>
        ///    <para>Invokes a method on the object, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>A <see cref='System.Management.ManagementOperationObserver'/> used to handle the asynchronous execution's progress and results.</param>
        /// <param name=' methodName'>The name of the method to be executed.</param>
        /// <param name=' inParameters'><para>A <see cref='System.Management.ManagementBaseObject'/> containing the input parameters for the method.</para></param>
        /// <param name=' options'>An <see cref='System.Management.InvokeMethodOptions'/> containing additional options used to execute the method.</param>
        /// <remarks>
        ///    <para>The method invokes the specified method execution and then
        ///       returns. Progress and results are reported through events on the <see cref='System.Management.ManagementOperationObserver'/>.</para>
        /// </remarks>
        public void InvokeMethod(
            ManagementOperationObserver watcher,
            string methodName,
            ManagementBaseObject inParameters,
            InvokeMethodOptions options)
        {
            if ((null == path) || (path.Path.Length == 0))
                throw new InvalidOperationException();
            else if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else if (null == methodName)
                throw new ArgumentNullException(nameof(methodName));
            else
            {
                Initialize(false);
                InvokeMethodOptions o = (null != options) ?
                    (InvokeMethodOptions)options.Clone() : new InvokeMethodOptions();

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewSink(scope, o.Context);

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = scope.GetSecurityHandler();

                IWbemClassObjectFreeThreaded inParams = null;

                if (null != inParameters)
                    inParams = inParameters.wbemObject;

                status = scope.GetSecuredIWbemServicesHandler(scope.GetIWbemServices()).ExecMethodAsync_(
                    path.RelativePath,
                    methodName,
                    o.Flags,
                    o.GetContext(),
                    inParams,
                    sink.Stub);

                securityHandler?.Reset();

                if (status < 0)
                {
                    watcher.RemoveSink(sink);
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        //******************************************************
        //GetMethodParameters
        //******************************************************
        /// <summary>
        /// <para>Returns a <see cref='System.Management.ManagementBaseObject'/> representing the list of input parameters for a method.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method. </param>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> containing the
        ///    input parameters to the method.</para>
        /// </returns>
        /// <remarks>
        ///    <para> Gets the object containing the input parameters to a
        ///       method, and then fills in the values and passes the object to the <see cref='System.Management.ManagementObject.InvokeMethod(string, ManagementBaseObject, InvokeMethodOptions)'/> call.</para>
        /// </remarks>
        public ManagementBaseObject GetMethodParameters(
            string methodName)
        {
            ManagementBaseObject inParameters;
            IWbemClassObjectFreeThreaded dummy1, dummy2;

            GetMethodParameters(methodName, out inParameters, out dummy1, out dummy2);

            return inParameters;
        }

        private void GetMethodParameters(
            string methodName,
            out ManagementBaseObject inParameters,
            out IWbemClassObjectFreeThreaded inParametersClass,
            out IWbemClassObjectFreeThreaded outParametersClass)
        {
            inParameters = null;
            inParametersClass = null;
            outParametersClass = null;

            if (null == methodName)
                throw new ArgumentNullException(nameof(methodName));
            else
            {
                Initialize(false);

                // Do we have the class?
                if (null == wmiClass)
                {
                    ManagementPath classPath = ClassPath;

                    if ((null == classPath) || !(classPath.IsClass))
                        throw new InvalidOperationException();
                    else
                    {
                        ManagementClass classObject =
                            new ManagementClass(scope, classPath, null);
                        classObject.Get();
                        wmiClass = classObject.wbemObject;
                    }
                }

                int status = (int)ManagementStatus.NoError;

                // Ask it for the method parameters
                status = wmiClass.GetMethod_(methodName, 0, out inParametersClass, out outParametersClass);

                // To ensure that all forms of invoke return the same error codes when
                // the method does not exist, we will map WBEM_E_NOT_FOUND to WBEM_E_METHOD_NOT_IMPLEMENTED.
                if (status == (int)tag_WBEMSTATUS.WBEM_E_NOT_FOUND)
                    status = (int)tag_WBEMSTATUS.WBEM_E_METHOD_NOT_IMPLEMENTED;

                if (status >= 0)
                {
                    // Hand out instances
                    if (inParametersClass != null)
                    {
                        IWbemClassObjectFreeThreaded inParamsInstance = null;
                        status = inParametersClass.SpawnInstance_(0, out inParamsInstance);

                        if (status >= 0)
                        {
                            inParameters = new ManagementBaseObject(inParamsInstance);
                        }
                    }
                }

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        /// <summary>
        ///    <para>Creates a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The copied object.</para>
        /// </returns>
        public override object Clone()
        {
            if (PutButNotGot)
            {
                Get();
                PutButNotGot = false;
            }

            IWbemClassObjectFreeThreaded theClone = null;

            int status = wbemObject.Clone_(out theClone);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return ManagementObject.GetManagementObject(theClone, this);
        }

        //******************************************************
        //ToString
        //******************************************************
        /// <summary>
        ///    <para>Returns the full path of the object. This is an override of the
        ///       default object implementation.</para>
        /// </summary>
        /// <returns>
        ///    <para> The full path of
        ///       the object.</para>
        /// </returns>
        public override string ToString()
        {
            if (null != path)
                return path.Path;
            else
                return "";
        }

        //
        // The prototype of Initialize has been changed to accept a bool, indicating whether or not
        // the caller wants to bind to the underlying WMI object in the Initialize call or not.
        //
        internal override void Initialize(bool getObject)
        {
            bool needToGetObject = false;

            //If we're not connected yet, this is the time to do it... We lock
            //the state to prevent 2 threads simultaneously doing the same
            //connection
            lock (this)
            {
                // Make sure we have some kind of path if we get here. Note that
                // we don't use a set to the Path property since that would trigger
                // an IdentifierChanged event
                if (null == path)
                {
                    path = new ManagementPath();
                    path.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                }

                //Have we already got this object
                if (!IsBound && getObject)
                    needToGetObject = true;

                if (null == scope)
                {
                    // If our object has a valid namespace path, use that
                    string nsPath = path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);

                    // Set the scope - note that we do not set through
                    // the Scope property since that would trigger an IdentifierChanged
                    // event and reset isBound to false.
                    if (0 < nsPath.Length)
                        scope = new ManagementScope(nsPath);
                    else
                    {
                        // Use the default constructor
                        scope = new ManagementScope();
                    }

                    // Hook ourselves up to this scope for future change notifications
                    scope.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                }
                else if ((null == scope.Path) || scope.Path.IsEmpty)
                {
                    // We have a scope but an empty path - use the object's path or the default
                    string nsPath = path.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);

                    if (0 < nsPath.Length)
                        scope.Path = new ManagementPath(nsPath);
                    else
                        scope.Path = ManagementPath.DefaultPath;
                }

                lock (scope)
                {
                    if (!scope.IsConnected)
                    {
                        scope.Initialize();

                        // If we have just connected, make sure we get the object
                        if (getObject)
                        {
                            needToGetObject = true;
                        }
                    }

                    if (needToGetObject)
                    {
                        // If we haven't set up any options yet, now is the time.
                        // Again we don't use the set to the Options property
                        // since that would trigger an IdentifierChangedEvent and
                        // force isBound=false.
                        if (null == options)
                        {
                            options = new ObjectGetOptions();
                            options.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                        }

                        IWbemClassObjectFreeThreaded tempObj = null;
                        IWbemServices wbemServices = scope.GetIWbemServices();

                        SecurityHandler securityHandler = null;
                        int status = (int)ManagementStatus.NoError;

                        try
                        {
                            securityHandler = scope.GetSecurityHandler();

                            string objectPath = null;
                            string curPath = path.RelativePath;

                            if (curPath.Length > 0)
                                objectPath = curPath;
                            status = scope.GetSecuredIWbemServicesHandler(wbemServices).GetObject_(objectPath, options.Flags, options.GetContext(), ref tempObj, IntPtr.Zero);

                            if (status >= 0)
                            {
                                wbemObject = tempObj;

                                // Getting the object succeeded, we are bound
                                //
                                // ***
                                // *    Changed isBound flag to wbemObject==null check.
                                // *    isBound = true;
                                // ***

                                // now set the path from the "real" object
                                object val = null;
                                int dummy1 = 0, dummy2 = 0;

                                status = wbemObject.Get_("__PATH", 0, ref val, ref dummy1, ref dummy2);

                                if (status >= 0)
                                {
                                    path = (System.DBNull.Value != val) ? (new ManagementPath((string)val)) : (new ManagementPath());
                                    path.IdentifierChanged += new IdentifierChangedEventHandler(HandleIdentifierChange);
                                }
                            }

                            if (status < 0)
                            {
                                if ((status & 0xfffff000) == 0x80041000)
                                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                                else
                                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                            }
                        }
                        finally
                        {
                            securityHandler?.Reset();
                        }
                    }
                }
            }
        }


        private static void MapInParameters(
            object[] args,
            ManagementBaseObject inParams,
            IWbemClassObjectFreeThreaded inParamsClass)
        {
            int status = (int)ManagementStatus.NoError;

            if (null != inParamsClass)
            {
                if ((null != args) && (0 < args.Length))
                {
                    int maxIndex = args.GetUpperBound(0);
                    int minIndex = args.GetLowerBound(0);
                    int topId = maxIndex - minIndex;

                    /*
                     * Iterate through the [in] parameters of the class to find
                     * the ID positional qualifier. We do this in the class because
                     * we cannot be sure that the qualifier will be propagated to
                     * the instance.
                     */

                    status = inParamsClass.BeginEnumeration_
                            ((int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_NONSYSTEM_ONLY);

                    if (status >= 0)
                    {
                        while (true)
                        {
                            object val = null;
                            int dummy = 0;
                            string propertyName = null;
                            IWbemQualifierSetFreeThreaded qualifierSet = null;

                            status = inParamsClass.Next_(0, ref propertyName, ref val, ref dummy, ref dummy);

                            if (status >= 0)
                            {
                                if (null == propertyName)
                                    break;

                                status = inParamsClass.GetPropertyQualifierSet_(propertyName, out qualifierSet);

                                if (status >= 0)
                                {
                                    try
                                    {
                                        object id = 0;
                                        qualifierSet.Get_(ID, 0, ref id, ref dummy);    // Errors intentionally ignored.

                                        // If the id is in range, map the value into the args array
                                        int idIndex = (int)id;
                                        if ((0 <= idIndex) && (topId >= idIndex))
                                            inParams[propertyName] = args[minIndex + idIndex];
                                    }
                                    finally
                                    {
                                        // Dispose for next iteration.
                                        qualifierSet.Dispose();
                                    }
                                }
                            }

                            if (status < 0)
                            {
                                break;
                            }
                        }
                    }

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

        private static object MapOutParameters(
            object[] args,
            ManagementBaseObject outParams,
            IWbemClassObjectFreeThreaded outParamsClass)
        {
            object result = null;
            int maxIndex = 0, minIndex = 0, topId = 0;

            int status = (int)ManagementStatus.NoError;

            if (null != outParamsClass)
            {
                if ((null != args) && (0 < args.Length))
                {
                    maxIndex = args.GetUpperBound(0);
                    minIndex = args.GetLowerBound(0);
                    topId = maxIndex - minIndex;
                }
                /*
                    * Iterate through the [out] parameters of the class to find
                    * the ID positional qualifier. We do this in the class because
                    * we cannot be sure that the qualifier will be propagated to
                    * the instance.
                */

                status = outParamsClass.BeginEnumeration_
                    ((int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_NONSYSTEM_ONLY);

                if (status >= 0)
                {
                    while (true)
                    {
                        object val = null;
                        int dummy = 0;
                        string propertyName = null;
                        IWbemQualifierSetFreeThreaded qualifierSet = null;

                        status = outParamsClass.Next_(0, ref propertyName, ref val, ref dummy, ref dummy);

                        if (status >= 0)
                        {
                            if (null == propertyName)
                                break;

                            // Handle the result parameter separately
                            if (string.Equals(propertyName, RETURNVALUE, StringComparison.OrdinalIgnoreCase))
                            {
                                result = outParams[RETURNVALUE];
                            }
                            else  // Shouldn't get here if no args!
                            {
                                status = outParamsClass.GetPropertyQualifierSet_(propertyName, out qualifierSet);

                                if (status >= 0)
                                {
                                    try
                                    {
                                        object id = 0;
                                        qualifierSet.Get_(ID, 0, ref id, ref dummy);    // Errors intentionally ignored.

                                        // If the id is in range, map the value into the args array
                                        int idIndex = (int)id;
                                        if ((0 <= idIndex) && (topId >= idIndex))
                                            args[minIndex + idIndex] = outParams[propertyName];
                                    }
                                    finally
                                    {
                                        // Dispose for next iteration.
                                        qualifierSet.Dispose();
                                    }
                                }
                            }
                        }

                        if (status < 0)
                        {
                            break;
                        }
                    }
                }

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }

            return result;
        }

    }//ManagementObject
}
