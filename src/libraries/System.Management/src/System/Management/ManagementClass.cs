// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a CIM management class from WMI. CIM (Common Information Model) classes
    ///            represent management information including hardware, software, processes, etc.
    ///            For more information about the CIM classes available in Windows search for "win32 classes".</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This example demonstrates getting information about a class using the ManagementClass object
    /// class Sample_ManagementClass
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
    ///         diskClass.Get();
    ///         Console.WriteLine("Logical Disk class has " + diskClass.Properties.Count + " properties");
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This example demonstrates getting information about a class using the ManagementClass object
    /// Class Sample_ManagementClass
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim diskClass As New ManagementClass("Win32_LogicalDisk")
    ///         diskClass.Get()
    ///         Console.WriteLine(("Logical Disk class has " &amp; _
    ///                            diskClass.Properties.Count.ToString() &amp; _
    ///                            " properties"))
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ManagementClass : ManagementObject
    {
        private MethodDataCollection methods;
        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
        /// <summary>
        /// Internal factory for classes, used when deriving a class
        /// or cloning a class. For these purposes we always mark
        /// the class as "bound".
        /// </summary>
        /// <param name="wbemObject">The underlying WMI object</param>
        /// <param name="mgObj">Seed class from which we will get initialization info</param>
        internal static ManagementClass GetManagementClass(
            IWbemClassObjectFreeThreaded wbemObject,
            ManagementClass mgObj)
        {
            ManagementClass newClass = new ManagementClass();
            newClass.wbemObject = wbemObject;

            if (null != mgObj)
            {
                newClass.scope = ManagementScope._Clone(mgObj.scope);

                ManagementPath objPath = mgObj.Path;

                if (null != objPath)
                    newClass.path = ManagementPath._Clone(objPath);

                // Ensure we have our class name in the path
                object className = null;
                int dummy = 0;

                int status = wbemObject.Get_("__CLASS", 0, ref className, ref dummy, ref dummy);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                if (className != System.DBNull.Value)
                    newClass.path.internalClassName = (string)className;

                ObjectGetOptions options = mgObj.Options;
                if (null != options)
                    newClass.options = ObjectGetOptions._Clone(options);

                // Finally we ensure that this object is marked as bound.
                // We do this as a last step since setting certain properties
                // (Options, Path and Scope) would mark it as unbound
                //
                // ***
                // *    Changed isBound flag to wbemObject==null check.
                // *    newClass.IsBound = true;
                // ***
            }

            return newClass;
        }

        internal static ManagementClass GetManagementClass(
            IWbemClassObjectFreeThreaded wbemObject,
            ManagementScope scope)
        {
            ManagementClass newClass = new ManagementClass();
            newClass.path = new ManagementPath(ManagementPath.GetManagementPath(wbemObject));

            if (null != scope)
                newClass.scope = ManagementScope._Clone(scope);

            newClass.wbemObject = wbemObject;

            return newClass;
        }

        //default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.ManagementClass'/> class.
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class. This is the
        ///    default constructor.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass();
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass()
        ///    </code>
        /// </example>
        public ManagementClass() : this((ManagementScope)null, (ManagementPath)null, null) { }

        //parameterized constructors
        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class initialized to the
        ///    given path.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> specifying which WMI class to bind to.</param>
        /// <remarks>
        /// <para>The <paramref name="path"/> parameter must specify a WMI class
        ///    path.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass(
        ///     new ManagementPath("Win32_LogicalDisk"));
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass( _
        ///     New ManagementPath("Win32_LogicalDisk"))
        ///    </code>
        /// </example>
        public ManagementClass(ManagementPath path) : this(null, path, null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class initialized to the given path.</para>
        /// </summary>
        /// <param name='path'>The path to the WMI class.</param>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new
        ///       ManagementClass("Win32_LogicalDisk");
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_LogicalDisk")
        ///    </code>
        /// </example>
        public ManagementClass(string path) : this(null, new ManagementPath(path), null) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class initialized to the
        ///    given WMI class path using the specified options.</para>
        /// </summary>
        /// <param name='path'>A <see cref='System.Management.ManagementPath'/> representing the WMI class path.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> representing the options to use when retrieving this class.</param>
        /// <example>
        ///    <code lang='C#'>ManagementPath p = new ManagementPath("Win32_Process");
        /// //Options specify that amended qualifiers are to be retrieved along with the class
        /// ObjectGetOptions o = new ObjectGetOptions(null, true);
        /// ManagementClass c = new ManagementClass(p,o);
        ///    </code>
        ///    <code lang='VB'>Dim p As New ManagementPath("Win32_Process")
        /// ' Options specify that amended qualifiers are to be retrieved along with the class
        /// Dim o As New ObjectGetOptions(Null, True)
        /// Dim c As New ManagementClass(p,o)
        ///    </code>
        /// </example>
        public ManagementClass(ManagementPath path, ObjectGetOptions options) : this(null, path, options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class initialized to the given WMI class path
        ///    using the specified options.</para>
        /// </summary>
        /// <param name='path'>The path to the WMI class.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> representing the options to use when retrieving the WMI class.</param>
        /// <example>
        ///    <code lang='C#'>//Options specify that amended qualifiers should be retrieved along with the class
        /// ObjectGetOptions o = new ObjectGetOptions(null, true);
        /// ManagementClass c = new ManagementClass("Win32_ComputerSystem",o);
        ///    </code>
        ///    <code lang='VB'>' Options specify that amended qualifiers should be retrieved along with the class
        /// Dim o As New ObjectGetOptions(Null, True)
        /// Dim c As New ManagementClass("Win32_ComputerSystem",o)
        ///    </code>
        /// </example>
        public ManagementClass(string path, ObjectGetOptions options)
            : this(null, new ManagementPath(path), options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class for the specified
        ///    WMI class in the specified scope and with the specified options.</para>
        /// </summary>
        /// <param name='scope'>A <see cref='System.Management.ManagementScope'/> that specifies the scope (server and namespace) where the WMI class resides. </param>
        /// <param name=' path'>A <see cref='System.Management.ManagementPath'/> that represents the path to the WMI class in the specified scope.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> that specifies the options to use when retrieving the WMI class.</param>
        /// <remarks>
        ///    <para> The path can be specified as a full
        ///       path (including server and namespace). However, if a scope is specified, it will
        ///       override the first portion of the full path.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementScope s = new ManagementScope("\\\\MyBox\\root\\cimv2");
        /// ManagementPath p = new ManagementPath("Win32_Environment");
        /// ObjectGetOptions o = new ObjectGetOptions(null, true);
        /// ManagementClass c = new ManagementClass(s, p, o);
        ///    </code>
        ///    <code lang='VB'>Dim s As New ManagementScope("\\MyBox\root\cimv2")
        /// Dim p As New ManagementPath("Win32_Environment")
        /// Dim o As New ObjectGetOptions(Null, True)
        /// Dim c As New ManagementClass(s, p, o)
        ///    </code>
        /// </example>
        public ManagementClass(ManagementScope scope, ManagementPath path, ObjectGetOptions options)
            : base(scope, path, options) { }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementClass'/> class for the specified WMI class, in the
        ///    specified scope, and with the specified options.</para>
        /// </summary>
        /// <param name='scope'>The scope in which the WMI class resides.</param>
        /// <param name=' path'>The path to the WMI class within the specified scope.</param>
        /// <param name=' options'>An <see cref='System.Management.ObjectGetOptions'/> that specifies the options to use when retrieving the WMI class.</param>
        /// <remarks>
        ///    <para> The path can be specified as a full
        ///       path (including server and namespace). However, if a scope is specified, it will
        ///       override the first portion of the full path.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("\\\\MyBox\\root\\cimv2",
        ///                                         "Win32_Environment",
        ///                                         new ObjectGetOptions(null, true));
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("\\MyBox\root\cimv2", _
        ///                              "Win32_Environment", _
        ///                              new ObjectGetOptions(Null, True))
        ///    </code>
        /// </example>
        public ManagementClass(string scope, string path, ObjectGetOptions options)
            : base(new ManagementScope(scope), new ManagementPath(path), options) { }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ManagementClass(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        ///    <para>Gets or sets the path of the WMI class to
        ///       which the <see cref='System.Management.ManagementClass'/>
        ///       object is bound.</para>
        /// </summary>
        /// <value>
        ///    <para>The path of the object's class.</para>
        /// </value>
        /// <remarks>
        ///    <para> When the property is set to a new value,
        ///       the <see cref='System.Management.ManagementClass'/>
        ///       object will be
        ///       disconnected from any previously-bound WMI class. Reconnect to the new WMI class path.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass();
        /// c.Path = "Win32_Environment";
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass()
        /// c.Path = "Win32_Environment"
        ///    </code>
        /// </example>
        public override ManagementPath Path
        {
            get
            {
                return base.Path;
            }
            set
            {
                // This must be a class path or empty (don't allow instance paths)
                if ((null == value) || value.IsClass || value.IsEmpty)
                    base.Path = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(value));
            }
        }

        /// <summary>
        ///    <para> Gets or sets an array containing all WMI classes in the
        ///       inheritance hierarchy from this class to the top.</para>
        /// </summary>
        /// <value>
        ///    A string collection containing the
        ///    names of all WMI classes in the inheritance hierarchy of this class.
        /// </value>
        /// <remarks>
        ///    <para>This property is read-only.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("Win32_LogicalDisk");
        /// foreach (string s in c.Derivation)
        ///     Console.WriteLine("Further derived from : ", s);
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_LogicalDisk")
        /// Dim s As String
        /// For Each s In c.Derivation
        ///     Console.WriteLine("Further derived from : " &amp; s)
        /// Next s
        ///    </code>
        /// </example>
        public StringCollection Derivation
        {
            get
            {
                StringCollection result = new StringCollection();

                int dummy1 = 0, dummy2 = 0;
                object val = null;

                int status = wbemObject.Get_("__DERIVATION", 0, ref val, ref dummy1, ref dummy2);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                if (null != val)
                    result.AddRange((string[])val);

                return result;
            }
        }


        /// <summary>
        /// <para>Gets or sets a collection of <see cref='System.Management.MethodData'/> objects that
        ///    represent the methods defined in the WMI class.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.MethodDataCollection'/> representing the methods defined in the WMI class.</para>
        /// </value>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("Win32_Process");
        /// foreach (Method m in c.Methods)
        ///     Console.WriteLine("This class contains this method : ", m.Name);
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_Process")
        /// Dim m As Method
        /// For Each m in c.Methods
        ///      Console.WriteLine("This class contains this method : " &amp; m.Name)
        ///    </code>
        /// </example>
        public MethodDataCollection Methods
        {
            get
            {
                Initialize(true);

                return methods ??= new MethodDataCollection(this);
            }
        }

        //
        //Methods
        //

        //******************************************************
        //GetInstances
        //******************************************************
        /// <overload>
        ///    Returns the collection of
        ///    all instances of the class.
        /// </overload>
        /// <summary>
        ///    <para>Returns the collection of all instances of the class.</para>
        /// </summary>
        /// <returns>
        /// <para>A collection of the <see cref='System.Management.ManagementObject'/> objects
        ///    representing the instances of the class.</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("Win32_Process");
        /// foreach (ManagementObject o in c.GetInstances())
        ///      Console.WriteLine("Next instance of Win32_Process : ", o.Path);
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_Process")
        /// Dim o As ManagementObject
        /// For Each o In c.GetInstances()
        ///      Console.WriteLine("Next instance of Win32_Process : " &amp; o.Path)
        /// Next o
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetInstances()
        {
            return GetInstances((EnumerationOptions)null);
        }


        /// <summary>
        ///    <para>Returns the collection of all instances of the class using the specified options.</para>
        /// </summary>
        /// <param name='options'>The additional operation options.</param>
        /// <returns>
        /// <para>A collection of the <see cref='System.Management.ManagementObject'/> objects
        ///    representing the instances of the class, according to the specified options.</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>EnumerationOptions opt = new EnumerationOptions();
        /// //Will enumerate instances of the given class and any subclasses.
        /// o.enumerateDeep = true;
        /// ManagementClass c = new ManagementClass("CIM_Service");
        /// foreach (ManagementObject o in c.GetInstances(opt))
        ///     Console.WriteLine(o["Name"]);
        ///    </code>
        ///    <code lang='VB'>Dim opt As New EnumerationOptions()
        /// 'Will enumerate instances of the given class and any subclasses.
        /// o.enumerateDeep = True
        /// Dim c As New ManagementClass("CIM_Service")
        /// Dim o As ManagementObject
        /// For Each o In c.GetInstances(opt)
        ///     Console.WriteLine(o["Name"])
        /// Next o
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetInstances(EnumerationOptions options)
        {
            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();

            Initialize(false);
            IEnumWbemClassObject enumWbem = null;

            EnumerationOptions o = (null == options) ? new EnumerationOptions() : (EnumerationOptions)options.Clone();
            //Need to make sure that we're not passing invalid flags to enumeration APIs.
            //The only flags in EnumerationOptions not valid for enumerations are EnsureLocatable & PrototypeOnly.
            o.EnsureLocatable = false; o.PrototypeOnly = false;

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = Scope.GetSecurityHandler();
                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).CreateInstanceEnum_(
                    ClassName, o.Flags, o.GetContext(), ref enumWbem);
            }
            finally
            {
                securityHandler?.Reset();
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return new ManagementObjectCollection(Scope, o, enumWbem);
        }

        /// <summary>
        ///    <para>Returns the collection of all instances of the class, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("Win32_Share");
        /// MyHandler h = new MyHandler();
        /// ManagementOperationObserver ob = new ManagementOperationObserver();
        /// ob.ObjectReady += new ObjectReadyEventHandler (h.NewObject);
        /// ob.Completed += new CompletedEventHandler (h.Done);
        ///
        /// c.GetInstances(ob);
        ///
        /// while (!h.Completed)
        ///     System.Threading.Thread.Sleep (1000);
        ///
        /// //Here you can use the object
        /// Console.WriteLine(o["SomeProperty"]);
        ///
        /// public class MyHandler
        /// {
        ///     private bool completed = false;
        ///
        ///     public void NewObject(object sender, ObjectReadyEventArgs e) {
        ///         Console.WriteLine("New result arrived !", ((ManagementObject)(e.NewObject))["Name"]);
        ///     }
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
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_Share")
        /// Dim h As New MyHandler()
        /// Dim ob As New ManagementOperationObserver()
        /// ob.ObjectReady += New ObjectReadyEventHandler(h.NewObject)
        /// ob.Completed += New CompletedEventHandler(h.Done)
        ///
        /// c.GetInstances(ob)
        ///
        /// While Not h.Completed
        ///     System.Threading.Thread.Sleep(1000)
        /// End While
        ///
        /// 'Here you can use the object
        /// Console.WriteLine(o("SomeProperty"))
        ///
        /// Public Class MyHandler
        ///     Private completed As Boolean = false
        ///
        ///     Public Sub Done(sender As Object, e As EventArrivedEventArgs)
        ///         Console.WriteLine("async Get completed !")
        ///     completed = True
        ///     End Sub
        ///
        ///     Public ReadOnly Property Completed() As Boolean
        ///         Get
        ///             Return completed
        ///     End Get
        ///     End Property
        /// End Class
        ///    </code>
        /// </example>
        public void GetInstances(ManagementOperationObserver watcher)
        {
            GetInstances(watcher, (EnumerationOptions)null);
        }


        /// <summary>
        ///    <para>Returns the collection of all instances of the class, asynchronously, using
        ///       the specified options.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        /// <param name=' options'>The specified additional options for getting the instances.</param>
        public void GetInstances(ManagementOperationObserver watcher, EnumerationOptions options)
        {
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));

            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();

            Initialize(false);

            EnumerationOptions o = (null == options) ? new EnumerationOptions() : (EnumerationOptions)options.Clone();

            //Need to make sure that we're not passing invalid flags to enumeration APIs.
            //The only flags in EnumerationOptions not valid for enumerations are EnsureLocatable & PrototypeOnly.
            o.EnsureLocatable = false; o.PrototypeOnly = false;

            // Ensure we switch off ReturnImmediately as this is invalid for async calls
            o.ReturnImmediately = false;

            // If someone has registered for progress, make sure we flag it
            if (watcher.HaveListenersForProgress)
                o.SendStatus = true;

            WmiEventSink sink = watcher.GetNewSink(Scope, o.Context);

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            securityHandler = Scope.GetSecurityHandler();

            status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).CreateInstanceEnumAsync_(
                ClassName, o.Flags, o.GetContext(), sink.Stub);


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

        //******************************************************
        //GetSubclasses
        //******************************************************
        /// <overload>
        ///    Returns the collection of
        ///    all derived classes for the class.
        /// </overload>
        /// <summary>
        ///    <para>Returns the collection of all subclasses for the class.</para>
        /// </summary>
        /// <returns>
        /// <para>A collection of the <see cref='System.Management.ManagementObject'/> objects that
        ///    represent the subclasses of the WMI class.</para>
        /// </returns>
        public ManagementObjectCollection GetSubclasses()
        {
            return GetSubclasses((EnumerationOptions)null);
        }


        /// <summary>
        ///    <para>Retrieves the subclasses of the class using the specified
        ///       options.</para>
        /// </summary>
        /// <param name='options'>The specified additional options for retrieving subclasses of the class.</param>
        /// <returns>
        /// <para>A collection of the <see cref='System.Management.ManagementObject'/> objects
        ///    representing the subclasses of the WMI class, according to the specified
        ///    options.</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>EnumerationOptions opt = new EnumerationOptions();
        ///
        /// //Causes return of deep subclasses as opposed to only immediate ones.
        /// opt.enumerateDeep = true;
        ///
        /// ManagementObjectCollection c = (new
        ///       ManagementClass("Win32_Share")).GetSubclasses(opt);
        ///    </code>
        ///    <code lang='VB'>Dim opt As New EnumerationOptions()
        ///
        /// 'Causes return of deep subclasses as opposed to only immediate ones.
        /// opt.enumerateDeep = true
        ///
        /// Dim cls As New ManagementClass("Win32_Share")
        /// Dim c As ManagementObjectCollection
        ///
        /// c = cls.GetSubClasses(opt)
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetSubclasses(EnumerationOptions options)
        {
            if (null == Path)
                throw new InvalidOperationException();

            Initialize(false);
            IEnumWbemClassObject enumWbem = null;

            EnumerationOptions o = (null == options) ? new EnumerationOptions() : (EnumerationOptions)options.Clone();
            //Need to make sure that we're not passing invalid flags to enumeration APIs.
            //The only flags in EnumerationOptions not valid for enumerations are EnsureLocatable & PrototypeOnly.
            o.EnsureLocatable = false; o.PrototypeOnly = false;

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = Scope.GetSecurityHandler();
                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).CreateClassEnum_(
                    ClassName, o.Flags, o.GetContext(), ref enumWbem);
            }
            finally
            {
                securityHandler?.Reset();
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return new ManagementObjectCollection(Scope, o, enumWbem);
        }

        /// <summary>
        ///    <para>Returns the collection of all classes derived from this class, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        public void GetSubclasses(ManagementOperationObserver watcher)
        {
            GetSubclasses(watcher, (EnumerationOptions)null);
        }


        /// <summary>
        ///    <para>Retrieves all classes derived from this class, asynchronously, using the specified
        ///       options.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        /// <param name='options'>The specified additional options to use in the derived class retrieval.</param>
        public void GetSubclasses(ManagementOperationObserver watcher,
                                        EnumerationOptions options)
        {
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));

            if (null == Path)
                throw new InvalidOperationException();

            Initialize(false);

            EnumerationOptions o = (null == options) ? new EnumerationOptions() :
                                      (EnumerationOptions)options.Clone();

            //Need to make sure that we're not passing invalid flags to enumeration APIs.
            //The only flags in EnumerationOptions not valid for enumerations are EnsureLocatable & PrototypeOnly.
            o.EnsureLocatable = false; o.PrototypeOnly = false;

            // Ensure we switch off ReturnImmediately as this is invalid for async calls
            o.ReturnImmediately = false;

            // If someone has registered for progress, make sure we flag it
            if (watcher.HaveListenersForProgress)
                o.SendStatus = true;

            WmiEventSink sink = watcher.GetNewSink(Scope, o.Context);

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            securityHandler = Scope.GetSecurityHandler();

            status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).CreateClassEnumAsync_(
                ClassName, o.Flags, o.GetContext(), sink.Stub);


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

        //******************************************************
        //Derive
        //******************************************************
        /// <summary>
        ///    <para>Derives a new class from this class.</para>
        /// </summary>
        /// <param name='newClassName'>The name of the new class to be derived.</param>
        /// <returns>
        /// <para>A new <see cref='System.Management.ManagementClass'/>
        /// that represents a new WMI class derived from the original class.</para>
        /// </returns>
        /// <remarks>
        ///    <para>Note that the newly returned class has not been committed
        ///       until the <see cref='System.Management.ManagementObject.Put()'/> method is explicitly called.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass existingClass = new ManagementClass("CIM_Service");
        ///    ManagementClass newClass = existingClass.Derive("My_Service");
        ///    newClass.Put(); //to commit the new class to the WMI repository.
        ///    </code>
        ///    <code lang='VB'>Dim existingClass As New ManagementClass("CIM_Service")
        /// Dim newClass As ManagementClass
        ///
        /// newClass = existingClass.Derive("My_Service")
        /// newClass.Put()  'to commit the new class to the WMI repository.
        ///    </code>
        /// </example>
        public ManagementClass Derive(string newClassName)
        {
            ManagementClass newClass = null;

            if (null == newClassName)
                throw new ArgumentNullException(nameof(newClassName));
            else
            {
                // Check the path is valid
                ManagementPath path = new ManagementPath();

                try
                {
                    path.ClassName = newClassName;
                }
                catch
                {
                    throw new ArgumentOutOfRangeException(nameof(newClassName));
                }

                if (!path.IsClass)
                    throw new ArgumentOutOfRangeException(nameof(newClassName));
            }

            if (PutButNotGot)
            {
                Get();
                PutButNotGot = false;
            }

            IWbemClassObjectFreeThreaded newWbemClass = null;
            int status = this.wbemObject.SpawnDerivedClass_(0, out newWbemClass);

            if (status >= 0)
            {
                object val = newClassName;
                status = newWbemClass.Put_("__CLASS", 0, ref val, 0);

                if (status >= 0)
                    newClass = ManagementClass.GetManagementClass(newWbemClass, this);
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return newClass;
        }

        //******************************************************
        //CreateInstance
        //******************************************************
        /// <summary>
        ///    <para>Creates a new instance of the WMI class.</para>
        /// </summary>
        /// <returns>
        /// <para>A <see cref='System.Management.ManagementObject'/> that represents a new
        ///    instance of the WMI class.</para>
        /// </returns>
        /// <remarks>
        ///    <para>Note that the new instance is not committed until the
        ///    <see cref='System.Management.ManagementObject.Put()'/> method is called. Before committing it, the key properties must
        ///       be specified.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass envClass = new ManagementClass("Win32_Environment");
        ///    ManagementObject newInstance =
        ///       existingClass.CreateInstance("My_Service");
        ///    newInstance["Name"] = "Cori";
        ///    newInstance.Put(); //to commit the new instance.
        ///    </code>
        ///    <code lang='VB'>Dim envClass As New ManagementClass("Win32_Environment")
        /// Dim newInstance As ManagementObject
        ///
        /// newInstance = existingClass.CreateInstance("My_Service")
        /// newInstance("Name") = "Cori"
        /// newInstance.Put()  'to commit the new instance.
        ///    </code>
        /// </example>
        public ManagementObject CreateInstance()
        {
            ManagementObject newInstance = null;

            if (PutButNotGot)
            {
                Get();
                PutButNotGot = false;
            }

            IWbemClassObjectFreeThreaded newWbemInstance = null;
            int status = this.wbemObject.SpawnInstance_(0, out newWbemInstance);

            if (status >= 0)
                newInstance = ManagementObject.GetManagementObject(newWbemInstance, Scope);
            else
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return newInstance;
        }

        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para> The cloned
        ///       object.</para>
        /// </returns>
        /// <remarks>
        ///    <para>Note that this does not create a copy of the
        ///       WMI class; only an additional representation is created.</para>
        /// </remarks>
        public override object Clone()
        {
            IWbemClassObjectFreeThreaded theClone = null;
            int status = wbemObject.Clone_(out theClone);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return ManagementClass.GetManagementClass(theClone, this);
        }


        //******************************************************
        //GetRelatedClasses
        //******************************************************
        /// <overload>
        ///    Retrieves classes related
        ///    to the WMI class.
        /// </overload>
        /// <summary>
        ///    <para> Retrieves classes related to the WMI class.</para>
        /// </summary>
        /// <returns>
        /// <para>A collection of the <see cref='System.Management.ManagementClass'/> or <see cref='System.Management.ManagementObject'/>
        /// objects that represents WMI classes or instances related to
        /// the WMI class.</para>
        /// </returns>
        /// <remarks>
        ///    <para>The method queries the WMI schema for all
        ///       possible associations that the WMI class may have with other classes, or in rare
        ///       cases, to instances.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("Win32_LogicalDisk");
        ///
        /// foreach (ManagementClass r in c.GetRelatedClasses())
        ///     Console.WriteLine("Instances of {0} may have
        ///                        relationships to this class", r["__CLASS"]);
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("Win32_LogicalDisk")
        /// Dim r As ManagementClass
        ///
        /// For Each r In c.GetRelatedClasses()
        ///     Console.WriteLine("Instances of {0} may have relationships _
        ///                        to this class", r("__CLASS"))
        /// Next r
        ///    </code>
        /// </example>
        public ManagementObjectCollection GetRelatedClasses()
        {
            return GetRelatedClasses((string)null);
        }

        /// <summary>
        ///    <para> Retrieves classes related to the WMI class.</para>
        /// </summary>
        /// <param name='relatedClass'><para>The class from which resulting classes have to be derived.</para></param>
        /// <returns>
        ///    A collection of classes related to
        ///    this class.
        /// </returns>
        public ManagementObjectCollection GetRelatedClasses(
            string relatedClass)
        {
            return GetRelatedClasses(relatedClass, null, null, null, null, null, null);
        }


        /// <summary>
        ///    <para> Retrieves classes related to the WMI class based on the specified
        ///       options.</para>
        /// </summary>
        /// <param name=' relatedClass'><para>The class from which resulting classes have to be derived.</para></param>
        /// <param name=' relationshipClass'> The relationship type which resulting classes must have with the source class.</param>
        /// <param name=' relationshipQualifier'>This qualifier must be present on the relationship.</param>
        /// <param name=' relatedQualifier'>This qualifier must be present on the resulting classes.</param>
        /// <param name=' relatedRole'>The resulting classes must have this role in the relationship.</param>
        /// <param name=' thisRole'>The source class must have this role in the relationship.</param>
        /// <param name=' options'>The options for retrieving the resulting classes.</param>
        /// <returns>
        ///    <para>A collection of classes related to
        ///       this class.</para>
        /// </returns>
        public ManagementObjectCollection GetRelatedClasses(
                                            string relatedClass,
                                            string relationshipClass,
                                            string relationshipQualifier,
                                            string relatedQualifier,
                                            string relatedRole,
                                            string thisRole,
                                            EnumerationOptions options)
        {
            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();

            Initialize(false);

            IEnumWbemClassObject enumWbem = null;

            EnumerationOptions o = (null != options) ? (EnumerationOptions)options.Clone() : new EnumerationOptions();
            //Ensure EnumerateDeep flag bit is turned off as it's invalid for queries
            o.EnumerateDeep = true;

            RelatedObjectQuery q = new RelatedObjectQuery(true, Path.Path,
                                                            relatedClass,
                                                            relationshipClass,
                                                            relatedQualifier,
                                                            relationshipQualifier,
                                                            relatedRole, thisRole);

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            try
            {
                securityHandler = Scope.GetSecurityHandler();
                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).ExecQuery_(
                    q.QueryLanguage,
                    q.QueryString,
                    o.Flags,
                    o.GetContext(),
                    ref enumWbem);

            }
            finally
            {
                securityHandler?.Reset();
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            //Create collection object
            return new ManagementObjectCollection(Scope, o, enumWbem);
        }


        /// <summary>
        ///    <para> Retrieves classes
        ///       related to the WMI class, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        public void GetRelatedClasses(
            ManagementOperationObserver watcher)
        {
            GetRelatedClasses(watcher, (string)null);
        }

        /// <summary>
        ///    <para> Retrieves classes related to the WMI class, asynchronously, given the related
        ///       class name.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        /// <param name=' relatedClass'>The name of the related class.</param>
        public void GetRelatedClasses(
            ManagementOperationObserver watcher,
            string relatedClass)
        {
            GetRelatedClasses(watcher, relatedClass, null, null, null, null, null, null);
        }


        /// <summary>
        ///    <para> Retrieves classes related to the
        ///       WMI class, asynchronously, using the specified options.</para>
        /// </summary>
        /// <param name='watcher'>Handler for progress and results of the asynchronous operation.</param>
        /// <param name=' relatedClass'><para>The class from which resulting classes have to be derived.</para></param>
        /// <param name=' relationshipClass'> The relationship type which resulting classes must have with the source class.</param>
        /// <param name=' relationshipQualifier'>This qualifier must be present on the relationship.</param>
        /// <param name=' relatedQualifier'>This qualifier must be present on the resulting classes.</param>
        /// <param name=' relatedRole'>The resulting classes must have this role in the relationship.</param>
        /// <param name=' thisRole'>The source class must have this role in the relationship.</param>
        /// <param name=' options'>The options for retrieving the resulting classes.</param>
        public void GetRelatedClasses(
            ManagementOperationObserver watcher,
            string relatedClass,
            string relationshipClass,
            string relationshipQualifier,
            string relatedQualifier,
            string relatedRole,
            string thisRole,
            EnumerationOptions options)
        {
            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();

            Initialize(true);

            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                EnumerationOptions o = (null != options)
                                ? (EnumerationOptions)options.Clone() : new EnumerationOptions();

                //Ensure EnumerateDeep flag bit is turned off as it's invalid for queries
                o.EnumerateDeep = true;

                // Ensure we switch off ReturnImmediately as this is invalid for async calls
                o.ReturnImmediately = false;

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewSink(
                    Scope,
                    o.Context);

                RelatedObjectQuery q = new RelatedObjectQuery(true, Path.Path,
                                                                relatedClass, relationshipClass,
                                                                relatedQualifier, relationshipQualifier,
                                                                relatedRole, thisRole);

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = Scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).ExecQueryAsync_(
                    q.QueryLanguage, q.QueryString, o.Flags, o.GetContext(), sink.Stub);


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
        //GetRelationshipClasses
        //******************************************************
        /// <overload>
        ///    Retrieves relationship
        ///    classes that relate the class to others.
        /// </overload>
        /// <summary>
        ///    <para>Retrieves relationship classes that relate the class to others.</para>
        /// </summary>
        /// <returns>
        ///    <para>A collection of association classes
        ///       that relate the class to any other class.</para>
        /// </returns>
        public ManagementObjectCollection GetRelationshipClasses()
        {
            return GetRelationshipClasses((string)null);
        }

        /// <summary>
        ///    <para>Retrieves relationship classes that relate the class to others, where the
        ///       endpoint class is the specified class.</para>
        /// </summary>
        /// <param name='relationshipClass'>The endpoint class for all relationship classes returned.</param>
        /// <returns>
        ///    <para>A collection of association classes
        ///       that relate the class to the specified class.</para>
        /// </returns>
        public ManagementObjectCollection GetRelationshipClasses(
            string relationshipClass)
        {
            return GetRelationshipClasses(relationshipClass, null, null, null);
        }


        /// <summary>
        ///    <para> Retrieves relationship classes that relate this class to others, according to
        ///       specified options.</para>
        /// </summary>
        /// <param name='relationshipClass'><para> All resulting relationship classes must derive from this class.</para></param>
        /// <param name=' relationshipQualifier'>Resulting relationship classes must have this qualifier.</param>
        /// <param name=' thisRole'>The source class must have this role in the resulting relationship classes.</param>
        /// <param name=' options'>Specifies options for retrieving the results.</param>
        /// <returns>
        ///    <para>A collection of association classes
        ///       that relate this class to others, according to the specified options.</para>
        /// </returns>
        public ManagementObjectCollection GetRelationshipClasses(
                                            string relationshipClass,
                                            string relationshipQualifier,
                                            string thisRole,
                                            EnumerationOptions options)
        {
            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();

            Initialize(false);

            IEnumWbemClassObject enumWbem = null;

            EnumerationOptions o = options ?? new EnumerationOptions();
            //Ensure EnumerateDeep flag is turned off as it's invalid for queries
            o.EnumerateDeep = true;


            RelationshipQuery q = new RelationshipQuery(true, Path.Path, relationshipClass,
                                                        relationshipQualifier, thisRole);

            SecurityHandler securityHandler = null;
            int status = (int)ManagementStatus.NoError;

            //Execute WMI query
            try
            {
                securityHandler = Scope.GetSecurityHandler();
                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).ExecQuery_(
                    q.QueryLanguage, q.QueryString, o.Flags, o.GetContext(), ref enumWbem);

            }
            finally
            {
                securityHandler?.Reset();
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            //Create collection object
            return new ManagementObjectCollection(Scope, o, enumWbem);
        }


        /// <summary>
        ///    <para>Retrieves relationship classes that relate the class to others,
        ///       asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        public void GetRelationshipClasses(
            ManagementOperationObserver watcher)
        {
            GetRelationshipClasses(watcher, (string)null);
        }

        /// <summary>
        ///    <para>Retrieves relationship classes that relate the class to the specified WMI class,
        ///       asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The object to handle the asynchronous operation's progress. </param>
        /// <param name=' relationshipClass'>The WMI class to which all returned relationships should point.</param>
        public void GetRelationshipClasses(
            ManagementOperationObserver watcher,
            string relationshipClass)
        {
            GetRelationshipClasses(watcher, relationshipClass, null, null, null);
        }


        /// <summary>
        ///    <para>Retrieves relationship classes that relate the class according to the specified
        ///       options, asynchronously.</para>
        /// </summary>
        /// <param name='watcher'>The handler for progress and results of the asynchronous operation.</param>
        /// <param name='relationshipClass'><para>The class from which all resulting relationship classes must derive.</para></param>
        /// <param name=' relationshipQualifier'>The qualifier which the resulting relationship classes must have.</param>
        /// <param name=' thisRole'>The role which the source class must have in the resulting relationship classes.</param>
        /// <param name=' options'> The options for retrieving the results.</param>
        public void GetRelationshipClasses(
            ManagementOperationObserver watcher,
            string relationshipClass,
            string relationshipQualifier,
            string thisRole,
            EnumerationOptions options)
        {
            if ((null == Path) || (null == Path.Path) || (0 == Path.Path.Length))
                throw new InvalidOperationException();
            if (null == watcher)
                throw new ArgumentNullException(nameof(watcher));
            else
            {
                Initialize(true);

                EnumerationOptions o =
                        (null != options) ? (EnumerationOptions)options.Clone() : new EnumerationOptions();

                //Ensure EnumerateDeep flag is turned off as it's invalid for queries
                o.EnumerateDeep = true;

                // Ensure we switch off ReturnImmediately as this is invalid for async calls
                o.ReturnImmediately = false;

                // If someone has registered for progress, make sure we flag it
                if (watcher.HaveListenersForProgress)
                    o.SendStatus = true;

                WmiEventSink sink = watcher.GetNewSink(Scope, o.Context);

                RelationshipQuery q = new RelationshipQuery(true, Path.Path, relationshipClass,
                        relationshipQualifier, thisRole);

                SecurityHandler securityHandler = null;
                int status = (int)ManagementStatus.NoError;

                securityHandler = Scope.GetSecurityHandler();

                status = scope.GetSecuredIWbemServicesHandler(Scope.GetIWbemServices()).ExecQueryAsync_(
                    q.QueryLanguage, q.QueryString, o.Flags, o.GetContext(), sink.Stub);


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


        /// <overload>
        ///    <para>Generates a strongly-typed class for a given WMI class.</para>
        /// </overload>
        /// <summary>
        ///    <para>Generates a strongly-typed class for a given WMI class.</para>
        /// </summary>
        /// <param name='includeSystemClassInClassDef'><see langword='true'/> if the class for managing system properties must be included; otherwise, <see langword='false'/>.</param>
        /// <param name='systemPropertyClass'><see langword='true'/> if the generated class will manage system properties; otherwise, <see langword='false'/>.</param>
        /// <returns>
        /// <para>A <see cref='System.CodeDom.CodeTypeDeclaration'/> instance
        ///    representing the declaration for the strongly-typed class.</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        /// using System.CodeDom;
        /// using System.IO;
        /// using System.CodeDom.Compiler;
        /// using Microsoft.CSharp;
        ///
        /// void GenerateCSharpCode()
        /// {
        ///       string strFilePath = "C:\\temp\\LogicalDisk.cs";
        ///       CodeTypeDeclaration ClsDom;
        ///
        ///       ManagementClass cls1 = new ManagementClass(null,"Win32_LogicalDisk",null);
        ///       ClsDom = cls1.GetStronglyTypedClassCode(false,false);
        ///
        ///       ICodeGenerator cg = (new CSharpCodeProvider()).CreateGenerator ();
        ///       CodeNamespace cn = new CodeNamespace("TestNamespace");
        ///
        ///       // Add any imports to the code
        ///       cn.Imports.Add (new CodeNamespaceImport("System"));
        ///       cn.Imports.Add (new CodeNamespaceImport("System.ComponentModel"));
        ///       cn.Imports.Add (new CodeNamespaceImport("System.Management"));
        ///       cn.Imports.Add(new CodeNamespaceImport("System.Collections"));
        ///
        ///       // Add class to the namespace
        ///       cn.Types.Add (ClsDom);
        ///
        ///       //Now create the filestream (output file)
        ///       TextWriter tw = new StreamWriter(new
        ///       FileStream (strFilePath,FileMode.Create));
        ///
        ///       // And write it to the file
        ///       cg.GenerateCodeFromNamespace (cn, tw, new CodeGeneratorOptions());
        ///
        ///       tw.Close();
        /// }
        ///    </code>
        /// </example>
        public CodeTypeDeclaration GetStronglyTypedClassCode(bool includeSystemClassInClassDef, bool systemPropertyClass)
        {
            // Ensure that the object is valid
            Get();
            ManagementClassGenerator classGen = new ManagementClassGenerator(this);
            return classGen.GenerateCode(includeSystemClassInClassDef, systemPropertyClass);
        }


        /// <summary>
        ///    <para>Generates a strongly-typed class for a given WMI class. This function generates code for Visual Basic,
        ///       C#, or JScript, depending on the input parameters.</para>
        /// </summary>
        /// <param name='lang'>The language of the code to be generated.</param>
        /// <param name='filePath'>The path of the file where the code is to be written.</param>
        /// <param name='classNamespace'>The .NET namespace into which the class should be generated. If this is empty, the namespace will be generated from the WMI namespace.</param>
        /// <returns>
        /// <para><see langword='true'/>, if the method succeeded;
        ///    otherwise, <see langword='false'/> .</para>
        /// </returns>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        ///
        /// ManagementClass cls = new ManagementClass(null,"Win32_LogicalDisk",null,"");
        /// cls.GetStronglyTypedClassCode(CodeLanguage.CSharp,"C:\temp\Logicaldisk.cs",String.Empty);
        ///    </code>
        /// </example>
        public bool GetStronglyTypedClassCode(CodeLanguage lang, string filePath, string classNamespace)
        {
            // Ensure that the object is valid
            Get();
            ManagementClassGenerator classGen = new ManagementClassGenerator(this);
            return classGen.GenerateCode(lang, filePath, classNamespace);
        }

    }//ManagementClass
}
