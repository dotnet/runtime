// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Reflection;
using System.ComponentModel.Design.Serialization;
using System.Text;

namespace System.Management
{
    /// <summary>
    ///    <para>Provides a wrapper for parsing and building paths to WMI objects.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System; 
    /// using System.Management;
    ///  
    /// // This sample displays all properties in a ManagementPath object. 
    /// 
    /// class Sample_ManagementPath 
    /// { 
    ///     public static int Main(string[] args) { 
    ///         ManagementPath path = new ManagementPath( "\\\\MyServer\\MyNamespace:Win32_logicaldisk='c:'");
    ///       
    ///         // Results of full path parsing 
    ///         Console.WriteLine("Path: " + path.Path);
    ///         Console.WriteLine("RelativePath: " + path.RelativePath);
    ///         Console.WriteLine("Server: " + path.Server); 
    ///         Console.WriteLine("NamespacePath: " + path.NamespacePath); 
    ///         Console.WriteLine("ClassName: " + path.ClassName);
    ///         Console.WriteLine("IsClass: " + path.IsClass); 
    ///         Console.WriteLine("IsInstance: " + path.IsInstance); 
    ///         Console.WriteLine("IsSingleton: " + path.IsSingleton); 
    ///            
    ///         // Change a portion of the full path 
    ///         path.Server = "AnotherServer";
    ///         Console.WriteLine("New Path: " + path.Path); 
    ///         return 0; 
    ///    } 
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management 
    /// 
    /// 'This sample displays all properties in a ManagementPath object. 
    /// Class Sample_ManagementPath Overloads
    ///     Public Shared Function Main(args() As String) As Integer
    ///         Dim path As _ New
    ///         ManagementPath("\\MyServer\MyNamespace:Win32_LogicalDisk='c:'") 
    /// 
    ///         ' Results of full path parsing
    ///         Console.WriteLine("Path: " &amp; path.Path) 
    ///         Console.WriteLine("RelativePath: " &amp; path.RelativePath)
    ///         Console.WriteLine("Server: " &amp; path.Server)
    ///         Console.WriteLine("NamespacePath: " &amp; path.NamespacePath) 
    ///         Console.WriteLine("ClassName: " &amp; path.ClassName) 
    ///         Console.WriteLine("IsClass: " &amp; path.IsClass)
    ///         Console.WriteLine("IsInstance: " &amp; path.IsInstance) 
    ///         Console.WriteLine("IsSingleton: " &amp; path.IsSingleton) 
    /// 
    ///         ' Change a portion of the full path 
    ///         path.Server= "AnotherServer"
    ///         Console.WriteLine("New Path: " &amp; path.Path)
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    [TypeConverter(typeof(ManagementPathConverter ))]
    public class ManagementPath : ICloneable
    {
        private static ManagementPath defaultPath = new ManagementPath("//./root/cimv2");

        //Used to minimize the cases in which new wbemPath (WMI object path parser) objects need to be constructed
        //This is done for performance reasons.
        private bool   isWbemPathShared = false; 
        
        internal event IdentifierChangedEventHandler IdentifierChanged;

        //Fires IdentifierChanged event
        private void FireIdentifierChanged()
        {
            if (IdentifierChanged != null)
                IdentifierChanged(this, null);
        }

        //internal factory
        /// <summary>
        /// Internal static "factory" method for making a new ManagementPath
        /// from the system property of a WMI object
        /// </summary>
        /// <param name="wbemObject">The WMI object whose __PATH property will
        /// be used to supply the returned object</param>
        internal static string GetManagementPath (
            IWbemClassObjectFreeThreaded wbemObject)
        {
            string path = null;
            int status  = (int)ManagementStatus.Failed;

            if (null != wbemObject)
            {
                int dummy1 = 0, dummy2 = 0;
                object val = null;
                status = wbemObject.Get_ ("__PATH", 0, ref val, ref dummy1, ref dummy2);
                if ((status < 0) || (val == System.DBNull.Value))
                {
                    //try to get the relpath instead
                    status = wbemObject.Get_ ("__RELPATH", 0, ref val, ref dummy1, ref dummy2);
                    if (status < 0) 
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }
                }
                
                if (System.DBNull.Value == val)
                    path = null;
                else
                    path = (string)val;
            }

            return path;
        }

        //Used internally to check whether a string passed in as a namespace is indeed syntactically correct
        //for a namespace (e.g. either has "\" or "/" in it or is the special case of "root")
        //This doesn't check for the existance of that namespace, nor does it guarrantee correctness.
        internal static bool IsValidNamespaceSyntax(string nsPath)
        {
            if (nsPath.Length != 0)
            {
                // Any path separators present?
                char[] pathSeparators = { '\\', '/' };
                if (nsPath.IndexOfAny(pathSeparators) == -1)
                {
                    // No separators.  The only valid path is "root".
                    if (!string.Equals("root", nsPath, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            
            return true;
        }


        internal static ManagementPath _Clone(ManagementPath path)
        {
            return ManagementPath._Clone(path, null);
        }

        internal static ManagementPath _Clone(ManagementPath path, IdentifierChangedEventHandler handler)
        {
            ManagementPath pathTmp = new ManagementPath();

            // Wire up change handler chain. Use supplied handler, if specified;
            // otherwise, default to that of the path argument.
            if (handler != null)
                pathTmp.IdentifierChanged = handler;

            // Assign ManagementPath IWbemPath to this.wmiPath.  
            // Optimization for performance : As long as the path is only read, we share this interface.
            // On the first write, a private copy will be needed; 
            // isWbemPathShared signals ManagementPath to create such a copy at write-time.
            if (path != null && path.wmiPath != null)
            {
                pathTmp.wmiPath = path.wmiPath;
                pathTmp.isWbemPathShared = path.isWbemPathShared = true;
            }

            return pathTmp;
        }

        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.ManagementPath'/> class.
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementPath'/> class that is empty. This is the default constructor.</para>
        /// </summary>
        public ManagementPath () : this ((string) null) {}

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementPath'/> class for the given path.</para>
        /// </summary>
        /// <param name='path'> The object path. </param>
        public ManagementPath(string path) 
        {
            if ((null != path) && (0 < path.Length))
                wmiPath = CreateWbemPath(path);
        }
        
        /// <summary>
        ///    <para>Returns the full object path as the string representation.</para>
        /// </summary>
        /// <returns>
        ///    A string containing the full object
        ///    path represented by this object. This value is equivalent to the value of the
        /// <see cref='System.Management.ManagementPath.Path'/> property.
        /// </returns>
        public override string ToString () 
        {
            return this.Path;
        }

        /// <summary>
        /// <para>Returns a copy of the <see cref='System.Management.ManagementPath'/>.</para>
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        public ManagementPath Clone ()
        {
            return new ManagementPath (Path);
        }

        /// <summary>
        /// Standard Clone returns a copy of this ManagementPath as a generic "Object" type
        /// </summary>
        /// <returns>
        ///    The cloned object.
        /// </returns>
        object ICloneable.Clone ()
        {
            return Clone ();    
        }

        /// <summary>
        ///    <para>Gets or sets the default scope path used when no scope is specified.
        ///       The default scope is /-/ \\.\root\cimv2, and can be changed by setting this property.</para>
        /// </summary>
        /// <value>
        ///    <para>By default the scope value is /-/ \\.\root\cimv2, or a different scope path if
        ///       the default was changed.</para>
        /// </value>
        public static ManagementPath DefaultPath 
        {
            get { return ManagementPath.defaultPath; }
            set { ManagementPath.defaultPath = value; }
        }
        
        //private members
        private IWbemPath       wmiPath;

        private IWbemPath CreateWbemPath(string path)
        {
            IWbemPath wbemPath = (IWbemPath)MTAHelper.CreateInMTA(typeof(WbemDefPath));//new WbemDefPath();
            SetWbemPath(wbemPath, path);
            return wbemPath;
        }

        private void SetWbemPath(string path)
        {
            // Test/utilize isWbemPathShared *only* on public + internal members!
            if (wmiPath == null)
                wmiPath = CreateWbemPath(path);
            else
                SetWbemPath(wmiPath, path);
        }

        private static void SetWbemPath(IWbemPath wbemPath, string path)
        {
            if (null != wbemPath)
            {
                uint flags = (uint) tag_WBEM_PATH_CREATE_FLAG.WBEMPATH_CREATE_ACCEPT_ALL;

                //For now we have to special-case the "root" namespace - 
                //  this is because in the case of "root", the path parser cannot tell whether 
                //  this is a namespace name or a class name
                if (string.Equals(path, "root", StringComparison.OrdinalIgnoreCase))
                    flags = flags | (uint) tag_WBEM_PATH_CREATE_FLAG.WBEMPATH_TREAT_SINGLE_IDENT_AS_NS;

                int status = wbemPath.SetText_(flags, path);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        private string GetWbemPath()
        {
            return GetWbemPath(this.wmiPath);
        }

        private static string GetWbemPath(IWbemPath wbemPath)
        {
            string pathStr = string.Empty;

            if (null != wbemPath)
            {
                // Requesting the path from a parser which has
                // been only given a relative path results in an incorrect
                // value being returned (e.g. \\.\win32_logicaldisk). To work
                // around this we check if there are any namespaces,
                // and if not ask for the relative path instead.
                int flags = (int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_TOO;
                uint nCount = 0;

                int status = (int)ManagementStatus.NoError;

                status = wbemPath.GetNamespaceCount_(out nCount);

                if (status >= 0)
                {
                    if (0 == nCount)
                        flags = (int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_RELATIVE_ONLY;

                    // Get the space we need to reserve
                    uint bufLen = 0;
                
                    status = wbemPath.GetText_(flags, ref bufLen, null);

                    if (status >= 0 && 0 < bufLen)
                    {
                        char[] pathChars = new char[(int)bufLen];
                        status = wbemPath.GetText_(flags, ref bufLen, pathChars);
                        pathStr = new string(pathChars, 0, Array.IndexOf(pathChars, '\0'));
                    }
                }

                if (status < 0)
                {
                    if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_PARAMETER) 
                    {
                        // Interpret as unspecified - return ""
                    }
                    
                    else if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }

            return pathStr;
        }

        private void ClearKeys (bool setAsSingleton)
        {
            // Test/utilize isWbemPathShared *only* on public + internal members!
            int status = (int)ManagementStatus.NoError;

            try 
            {
                if (null != wmiPath)
                {
                    IWbemPathKeyList keyList = null;
                    status = wmiPath.GetKeyList_(out keyList);

                    if (null != keyList)
                    {
                        status = keyList.RemoveAllKeys_(0);
                        if ((status & 0x80000000) == 0)
                        {
                            sbyte bSingleton = (setAsSingleton) ? (sbyte)(-1) : (sbyte)0;
                            status = keyList.MakeSingleton_(bSingleton);
                            FireIdentifierChanged ();
                        }
                    }
                }
            }
            catch (COMException e) 
            {
                ManagementException.ThrowWithExtendedInfo(e);
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
        
        internal bool IsEmpty 
        {
            get 
            {
                return (Path.Length == 0 ) ;
            }
        }


        //
        // Methods
        //

        /// <summary>
        ///    <para> Sets the path as a new class path. This means that the path must have
        ///       a class name but not key values.</para>
        /// </summary>
        public void SetAsClass ()
        {
            if (IsClass || IsInstance)
            {
                // Check if this IWbemPath is shared among multiple managed objects.
                // With this write, it will have to maintain its own copy.
                if (isWbemPathShared)
                {
                    wmiPath = CreateWbemPath(this.GetWbemPath());
                    isWbemPathShared = false;
                }

                ClearKeys (false);
            }
            else
                throw new ManagementException (ManagementStatus.InvalidOperation, null, null);
        }

        /// <summary>
        ///    <para> Sets the path as a new singleton object path. This means that it is a path to an instance but
        ///       there are no key values.</para>
        /// </summary>
        public void SetAsSingleton ()
        {
            if (IsClass || IsInstance)
            {
                // Check if this IWbemPath is shared among multiple managed objects.
                // With this write, it will have to maintain its own copy.
                if (isWbemPathShared)
                {
                    wmiPath = CreateWbemPath(this.GetWbemPath());
                    isWbemPathShared = false;
                }

                ClearKeys (true);
            }
            else
                throw new ManagementException (ManagementStatus.InvalidOperation, null, null);
        }

        //
        // Properties
        //

        /// <summary>
        ///    <para> Gets or sets the string representation of the full path in the object.</para>
        /// </summary>
        /// <value>
        ///    <para>A string containing the full path
        ///       represented in this object.</para>
        /// </value>
        [RefreshProperties(RefreshProperties.All)]
        public string Path
        {
            get
            {
                return this.GetWbemPath();
            }
            set
            {
                try
                {
                    // Before overwriting, check it's OK
                    // Note, we've never done such validation, should we?
                    //
                    // Check if this IWbemPath is shared among multiple managed objects.
                    // With this write, it will have to maintain its own copy.
                    if (isWbemPathShared)
                    {
                        wmiPath = CreateWbemPath(this.GetWbemPath());
                        isWbemPathShared = false;
                    }

                    this.SetWbemPath(value);
                }
                catch
                {
                    throw new ArgumentOutOfRangeException (nameof(value));
                }
                FireIdentifierChanged();
            }
        }

        /// <summary>
        ///    <para> Gets or sets the relative path: class name and keys only.</para>
        /// </summary>
        /// <value>
        ///    A string containing the relative
        ///    path (not including the server and namespace portions) represented in this
        ///    object.
        /// </value>
        [RefreshProperties(RefreshProperties.All)]
        public string RelativePath
        {
            get 
            { 
                string pathStr = string.Empty;

                if (null != wmiPath)
                {
                    // Get the space we need to reserve
                    uint bufLen = 0;
                    int status = wmiPath.GetText_(
                        (int) tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_RELATIVE_ONLY,
                        ref bufLen, 
                        null);

                    if (status >= 0 && 0 < bufLen)
                    {
                        char[] pathChars = new char[(int)bufLen];
                        status = wmiPath.GetText_(
                            (int) tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_RELATIVE_ONLY,
                            ref bufLen, 
                            pathChars);
                        pathStr = new string(pathChars, 0, Array.IndexOf(pathChars, '\0'));
                    }

                    if (status < 0)
                    {
                        if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_PARAMETER) 
                        {
                            // Interpret as unspecified - return ""
                        }
                        else if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }
                }

                return pathStr;
            }

            set 
            {
                try 
                {
                    // No need for isWbemPathShared here since internal SetRelativePath
                    // always creates a new copy.
                    SetRelativePath (value);
                } 
                catch (COMException) 
                {
                    throw new ArgumentOutOfRangeException (nameof(value));
                }
                FireIdentifierChanged();
            }
        }

        internal void SetRelativePath (string relPath)
        {
            // No need for isWbemPathShared here since internal SetRelativePath
            // always creates a new copy.
            ManagementPath newPath = new ManagementPath (relPath);
            newPath.NamespacePath = this.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);
            newPath.Server = this.Server;
            wmiPath = newPath.wmiPath;
        }

        //Used to update the relative path when the user changes any key properties
        internal void UpdateRelativePath(string relPath)
        {
            if (relPath == null)
                return;

            //Get the server & namespace part from the existing path, and concatenate the given relPath.
            //NOTE : we need to do this because IWbemPath doesn't have a function to set the relative path alone...
            string newPath = string.Empty;
            string nsPath = this.GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_SERVER_AND_NAMESPACE_ONLY);

            if (nsPath.Length>0 )
                newPath = string.Concat(nsPath, ":", relPath);
            else
                newPath = relPath;

            // Check if this IWbemPath is shared among multiple managed objects.
            // With this write, it will have to maintain its own copy.
            if (isWbemPathShared)
            {
                wmiPath = CreateWbemPath(this.GetWbemPath());
                isWbemPathShared = false;
            }

            this.SetWbemPath(newPath);
        }

        
        /// <summary>
        ///    <para>Gets or sets the server part of the path.</para>
        /// </summary>
        /// <value>
        ///    A string containing the server name
        ///    from the path represented in this object.
        /// </value>
        [RefreshProperties(RefreshProperties.All)]
        public string Server
        {
            get 
            { 
                string pathStr = string.Empty;

                if (null != wmiPath) 
                {

                    uint uLen = 0;
                    int status = wmiPath.GetServer_(ref uLen, null);

                    if (status >= 0 && 0 < uLen)
                    {
                        char[] pathChars = new char[(int)uLen];
                        status = wmiPath.GetServer_(ref uLen, pathChars);
                        pathStr = new string(pathChars, 0, Array.IndexOf(pathChars, '\0'));
                    }

                    if (status < 0)
                    {
                        if (status == (int)tag_WBEMSTATUS.WBEM_E_NOT_AVAILABLE) 
                        {
                            // Interpret as unspecified - return ""
                        }
                        else if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }
                }

                return pathStr;
            }
            set 
            {
                string oldValue = Server;

                // Only set if changed
                if (0 != string.Compare(oldValue,value,StringComparison.OrdinalIgnoreCase))
                {
                    if (null == wmiPath)
                        wmiPath = (IWbemPath)MTAHelper.CreateInMTA(typeof(WbemDefPath));//new WbemDefPath ();
                    else if (isWbemPathShared)
                    {
                        // Check if this IWbemPath is shared among multiple managed objects.
                        // With this write, it will have to maintain its own copy.
                        wmiPath = CreateWbemPath(this.GetWbemPath());
                        isWbemPathShared = false;
                    }

                    int status = wmiPath.SetServer_(value);

                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }

                    FireIdentifierChanged();
                }
            }
        }

        internal string SetNamespacePath(string nsPath, out bool bChange) 
        {
            int         status = (int)ManagementStatus.NoError;
            string      nsOrg = null;
            string      nsNew = null;
            IWbemPath   wmiPathTmp = null; 
            bChange = false;

            Debug.Assert(nsPath != null);

            //Do some validation on the path to make sure it is a valid namespace path (at least syntactically)
            if (!IsValidNamespaceSyntax(nsPath))
                ManagementException.ThrowWithExtendedInfo((ManagementStatus)tag_WBEMSTATUS.WBEM_E_INVALID_NAMESPACE);

            wmiPathTmp = CreateWbemPath(nsPath);
            if (wmiPath == null)
                wmiPath = this.CreateWbemPath("");
            else if (isWbemPathShared)
            {
                // Check if this IWbemPath is shared among multiple managed objects.
                // With this write, it will have to maintain its own copy.
                wmiPath = CreateWbemPath(this.GetWbemPath());
                isWbemPathShared = false;
            }

            nsOrg = GetNamespacePath(wmiPath,
                (int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_NAMESPACE_ONLY);
            nsNew = GetNamespacePath(wmiPathTmp,
                (int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_NAMESPACE_ONLY);

            if (!string.Equals(nsOrg, nsNew, StringComparison.OrdinalIgnoreCase))
            {
                wmiPath.RemoveAllNamespaces_();                                 // Out with the old... Ignore status code.

                // Add the new ones in
                bChange = true;                                                 // Now dirty from above.
                uint nCount = 0;
                status = wmiPathTmp.GetNamespaceCount_(out nCount);

                if (status >= 0)
                {
                    for (uint i = 0; i < nCount; i++) 
                    {
                        uint uLen = 0;
                        status = wmiPathTmp.GetNamespaceAt_(i, ref uLen, null);
                            
                        if (status >= 0)
                        {
                            char[] space = new char[(int)uLen];
                            status = wmiPathTmp.GetNamespaceAt_(i, ref uLen, space);
                            if (status >= 0)
                            {
                                status = wmiPath.SetNamespaceAt_(i, space);
                                    
                                if (status < 0)
                                    break;
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                }
            }
            else {;}    // Continue on. Could have different server name, same ns specified.

            //
            // Update Server property if specified in the namespace.
            // eg: "\\MyServer\root\cimv2".
            //
            if (status >= 0 && nsPath.Length > 1 &&
                (nsPath[0] == '\\' && nsPath[1] == '\\' ||
                nsPath[0] == '/'  && nsPath[1] == '/'))
            {
                uint uLen = 0;
                status = wmiPathTmp.GetServer_(ref uLen, null);

                if (status >= 0 && uLen > 0)
                {
                    char[] newServerChars = new char[(int)uLen];
                    status = wmiPathTmp.GetServer_(ref uLen, newServerChars);
                    string serverNew = new string(newServerChars, 0, Array.IndexOf(newServerChars, '\0'));

                    if (status >= 0)
                    {
                        // Compare server name on this object, if specified, to the caller's.
                        //     Update this object if different or unspecified.
                        uLen = 0;
                        status = wmiPath.GetServer_(ref uLen, null);            // NB: Cannot use property get since it may throw.

                        if (status >= 0)
                        {
                            char[] orgServerChars = new char[(int)uLen];
                            status = wmiPath.GetServer_(ref uLen, orgServerChars);
                            string serverOrg = new string(orgServerChars, 0, Array.IndexOf(orgServerChars, '\0'));

                            if (status >= 0 && !string.Equals(serverOrg, serverNew, StringComparison.OrdinalIgnoreCase))
                                status = wmiPath.SetServer_(serverNew);
                        }
                        else if (status == (int)tag_WBEMSTATUS.WBEM_E_NOT_AVAILABLE)
                        {
                            status = wmiPath.SetServer_(serverNew);
                            if (status >= 0)
                                bChange = true;
                        }
                    }
                }
                else if (status == (int)tag_WBEMSTATUS.WBEM_E_NOT_AVAILABLE)    // No caller-supplied server name;
                    status = (int)ManagementStatus.NoError;                     // Ignore error.
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return nsNew;
        }

        internal string GetNamespacePath(int flags)
        {
            return GetNamespacePath(wmiPath, flags);
        }

        internal static string GetNamespacePath(IWbemPath wbemPath, int flags)
        {
            string pathStr = string.Empty;

            if (null != wbemPath)
            {
                // Requesting the namespace path from a parser which has
                // been only given a relative path results in an incorrect
                // value being returned (e.g. \\.\). To work
                // around this, check if there are any namespaces,
                // and if not just return "".
                uint nCount = 0;
                int status = (int)ManagementStatus.NoError;

                status = wbemPath.GetNamespaceCount_(out nCount);

                if (status >= 0 && nCount > 0)
                {
                    // Get the space we need to reserve
                    uint bufLen = 0;
                    status = wbemPath.GetText_(flags, ref bufLen, null);

                    if (status >= 0 && bufLen > 0)
                    {
                        char[] pathChars = new char[(int)bufLen];
                        status = wbemPath.GetText_(flags, ref bufLen, pathChars);
                        pathStr = new string(pathChars, 0, Array.IndexOf(pathChars, '\0'));
                    }
                }

                if (status < 0)
                {
                    if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_PARAMETER) 
                    {
                        // Interpret as unspecified - return ""
                    }
                    else if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }

            return pathStr;
        }

        /// <summary>
        ///    <para>Gets or sets the namespace part of the path. Note that this does not include
        ///       the server name, which can be retrieved separately.</para>
        /// </summary>
        /// <value>
        ///    A string containing the namespace
        ///    portion of the path represented in this object.
        /// </value>
        [RefreshProperties(RefreshProperties.All)]
        public string NamespacePath 
        {
            get 
            {
                return GetNamespacePath((int)tag_WBEM_GET_TEXT_FLAGS.WBEMPATH_GET_NAMESPACE_ONLY);
            }
            set 
            {
                bool bChange = false;

                try
                {
                    // isWbemPathShared handled in internal SetNamespacePath.
                    SetNamespacePath(value, out bChange);
                }
                catch (COMException)
                {
                    throw new ArgumentOutOfRangeException (nameof(value));
                }

                if (bChange)
                    FireIdentifierChanged();
            }
        }

        /// <summary>
        ///    Gets or sets the class portion of the path.
        /// </summary>
        /// <value>
        ///    A string containing the name of the
        ///    class.
        /// </value>
        [RefreshProperties(RefreshProperties.All)]
        public string ClassName
        {
            get
            {
                return internalClassName;
            }
            set 
            {
                string oldValue = ClassName;

                // Only set if changed
                if (0 != string.Compare(oldValue,value,StringComparison.OrdinalIgnoreCase))
                {
                    // isWbemPathShared handled in internal className property accessor.
                    internalClassName = value;
                    FireIdentifierChanged();
                }
            }
        }

        internal string internalClassName
        {
            get
            {
                string pathStr = string.Empty;
                int status = (int)ManagementStatus.NoError;

                if (null != wmiPath)
                {
                    uint bufLen = 0;
                    status = wmiPath.GetClassName_(ref bufLen, null);

                    if (status >= 0 && 0 < bufLen)
                    {
                        char[] pathChars = new char[(int)bufLen];
                        status = wmiPath.GetClassName_(ref bufLen, pathChars);
                        pathStr = new string(pathChars, 0, Array.IndexOf(pathChars, '\0'));

                        if (status < 0)
                            pathStr = string.Empty;
                    }
                }

                return pathStr;
            }
            set
            {
                int status = (int)ManagementStatus.NoError;

                if (wmiPath == null)
                    wmiPath = (IWbemPath)MTAHelper.CreateInMTA(typeof(WbemDefPath));//new WbemDefPath();
                else if (isWbemPathShared)
                {
                    // Check if this IWbemPath is shared among multiple managed objects.
                    // With this write, it will have to maintain its own copy.
                    wmiPath = CreateWbemPath(this.GetWbemPath());
                    isWbemPathShared = false;
                }

                try
                {
                    status = wmiPath.SetClassName_(value);
                }
                catch (COMException)
                {
                    throw new ArgumentOutOfRangeException (nameof(value));
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
        ///    <para>Gets or sets a value indicating whether this is a class path.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this is a class path; otherwise, 
        /// <see langword='false'/>.</para>
        /// </value>
        public bool IsClass 
        {
            get
            {
                if (null == wmiPath)
                    return false;

                ulong uInfo = 0;
                int status = wmiPath.GetInfo_(0, out uInfo);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return (0 != (uInfo & (ulong)tag_WBEM_PATH_STATUS_FLAG.WBEMPATH_INFO_IS_CLASS_REF));
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether this is an instance path.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this is an instance path; otherwise, 
        /// <see langword='false'/>.</para>
        /// </value>
        public bool IsInstance 
        {
            get
            {
                if (null == wmiPath)
                    return false;

                ulong uInfo = 0;
                int status = wmiPath.GetInfo_(0, out uInfo);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return (0 != (uInfo & (ulong)tag_WBEM_PATH_STATUS_FLAG.WBEMPATH_INFO_IS_INST_REF));
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether this is a singleton instance path.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this is a singleton instance path; otherwise, 
        /// <see langword='false'/>.</para>
        /// </value>
        public bool IsSingleton 
        {
            get
            {
                if (null == wmiPath)
                    return false;

                ulong uInfo = 0;
                int status = wmiPath.GetInfo_(0, out uInfo);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return (0 != (uInfo & (ulong)tag_WBEM_PATH_STATUS_FLAG.WBEMPATH_INFO_IS_SINGLETON));
            }
        }
    }

    /// <summary>
    /// Converts a String to a ManagementPath
    /// </summary>
    class ManagementPathConverter : ExpandableObjectConverter 
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
            if ((sourceType == typeof(ManagementPath))) 
            {
                return true;
            }
            return base.CanConvertFrom(context,sourceType);
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
            return base.CanConvertTo(context,destinationType);
        }
        
        /// <summary>
        ///      Converts the given object to another type.  The most common types to convert
        ///      are to and from a string object.  The default implementation will make a call
        ///      to ToString on the object if the object is valid and if the destination
        ///      type is string.  If this cannot convert to the desitnation type, this will
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

            if (value is ManagementPath && destinationType == typeof(InstanceDescriptor)) 
            {
                ManagementPath obj = ((ManagementPath)(value));
                ConstructorInfo ctor = typeof(ManagementPath).GetConstructor(new Type[] {typeof(string)});
                if (ctor != null) 
                {
                    return new InstanceDescriptor(ctor, new object[] {obj.Path});
                }
            }
            return base.ConvertTo(context,culture,value,destinationType);
        }
    }
}
