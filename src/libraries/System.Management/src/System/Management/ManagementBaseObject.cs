// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Management
{
    /// <summary>
    /// <para>Describes the possible text formats that can be used with <see cref='System.Management.ManagementBaseObject.GetText'/>.</para>
    /// </summary>
    public enum TextFormat
    {
        /// <summary>
        /// Managed Object Format
        /// </summary>
        Mof = 0,
        /// <summary>
        /// XML DTD that corresponds to CIM DTD version 2.0
        /// </summary>
        CimDtd20 = 1,
        /// <summary>
        /// XML WMI DTD that corresponds to CIM DTD version 2.0.
        /// Using this value enables a few WMI-specific extensions, like embedded objects.
        /// </summary>
        WmiDtd20 = 2
    };

    /// <summary>
    ///    <para>Describes the possible CIM types for properties, qualifiers, or method parameters.</para>
    /// </summary>
    public enum CimType
    {
        /// <summary>
        ///    <para>Invalid Type</para>
        /// </summary>
        None = 0,
        /// <summary>
        ///    <para>A signed 8-bit integer.</para>
        /// </summary>
        SInt8 = 16,
        /// <summary>
        ///    <para>An unsigned 8-bit integer.</para>
        /// </summary>
        UInt8 = 17,
        /// <summary>
        ///    <para>A signed 16-bit integer.</para>
        /// </summary>
        SInt16 = 2,
        /// <summary>
        ///    <para>An unsigned 16-bit integer.</para>
        /// </summary>
        UInt16 = 18,
        /// <summary>
        ///    <para>A signed 32-bit integer.</para>
        /// </summary>
        SInt32 = 3,
        /// <summary>
        ///    <para>An unsigned 32-bit integer.</para>
        /// </summary>
        UInt32 = 19,
        /// <summary>
        ///    <para>A signed 64-bit integer.</para>
        /// </summary>
        SInt64 = 20,
        /// <summary>
        ///    <para>An unsigned 64-bit integer.</para>
        /// </summary>
        UInt64 = 21,
        /// <summary>
        ///    <para>A floating-point 32-bit number.</para>
        /// </summary>
        Real32 = 4,
        /// <summary>
        ///    <para>A floating point 64-bit number.</para>
        /// </summary>
        Real64 = 5,
        /// <summary>
        ///    <para> A boolean.</para>
        /// </summary>
        Boolean = 11,
        /// <summary>
        ///    <para>A string.</para>
        /// </summary>
        String = 8,
        /// <summary>
        ///    <para> A date or time value, represented in a string in DMTF
        ///       date/time format: yyyymmddHHMMSS.mmmmmmsUUU</para>
        ///    <para>where:</para>
        ///    <para>yyyymmdd - is the date in year/month/day</para>
        ///    <para>HHMMSS - is the time in hours/minutes/seconds</para>
        ///    <para>mmmmmm - is the number of microseconds in 6 digits</para>
        ///    <para>sUUU - is a sign (+ or -) and a 3-digit UTC offset</para>
        /// </summary>
        DateTime = 101,
        /// <summary>
        ///    <para>A reference to another object. This is represented by a
        ///       string containing the path to the referenced object</para>
        /// </summary>
        Reference = 102,
        /// <summary>
        ///    <para> A 16-bit character.</para>
        /// </summary>
        Char16 = 103,
        /// <summary>
        ///    <para>An embedded object.</para>
        ///    <para>Note that embedded objects differ from references in that the embedded object
        ///       doesn't have a path and its lifetime is identical to the lifetime of the
        ///       containing object.</para>
        /// </summary>
        Object = 13,
    };

    /// <summary>
    /// <para>Describes the object comparison modes that can be used with <see cref='System.Management.ManagementBaseObject.CompareTo'/>.
    ///    Note that these values may be combined.</para>
    /// </summary>
    [Flags]
    public enum ComparisonSettings
    {
        /// <summary>
        ///    <para>A mode that compares all elements of the compared objects.</para>
        /// </summary>
        IncludeAll = 0,
        /// <summary>
        ///    <para>A mode that compares the objects, ignoring qualifiers.</para>
        /// </summary>
        IgnoreQualifiers = 0x1,
        /// <summary>
        ///    <para> A mode that ignores the source of the objects, namely the server
        ///       and the namespace they came from, in comparison to other objects.</para>
        /// </summary>
        IgnoreObjectSource = 0x2,
        /// <summary>
        ///    <para> A mode that ignores the default values of properties.
        ///       This value is only meaningful when comparing classes.</para>
        /// </summary>
        IgnoreDefaultValues = 0x4,
        /// <summary>
        ///    <para>A mode that assumes that the objects being compared are instances of
        ///       the same class. Consequently, this value causes comparison
        ///       of instance-related information only. Use this flag to optimize
        ///       performance. If the objects are not of the same class, the results are undefined.</para>
        /// </summary>
        IgnoreClass = 0x8,
        /// <summary>
        ///    <para> A mode that compares string values in a case-insensitive
        ///       manner. This applies to strings and to qualifier values. Property and qualifier
        ///       names are always compared in a case-insensitive manner whether this flag is
        ///       specified or not.</para>
        /// </summary>
        IgnoreCase = 0x10,
        /// <summary>
        ///    <para>A mode that ignores qualifier flavors. This flag still takes
        ///       qualifier values into account, but ignores flavor distinctions such as
        ///       propagation rules and override restrictions.</para>
        /// </summary>
        IgnoreFlavor = 0x20
    };


    internal enum QualifierType
    {
        ObjectQualifier,
        PropertyQualifier,
        MethodQualifier
    }


    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Contains the basic elements of a management
    ///       object. It serves as a base class to more specific management object classes.</para>
    /// </summary>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    [ToolboxItem(false)]
    public class ManagementBaseObject : Component, ICloneable, ISerializable
    {
        // This field holds onto a WbemContext for the lifetime of the appdomain.  This should
        // prevent Fastprox.dll from unloading prematurely.
        // Since this is fixed in WinXP, we only hold onto a WbemContext if we are NOT running XP or later.

#pragma warning disable CA1823
#pragma warning disable CS0169 // Kept for possible reflection, comment above for history
        private static readonly WbemContext lockOnFastProx; // RemovedDuringPort System.Management.Instrumentation.WMICapabilities.IsWindowsXPOrHigher()?null:new WbemContext();
#pragma warning restore CS0169
#pragma warning restore CA1823

        //
        // The wbemObject is changed from a field to a property. This is to avoid major code churn and simplify the solution to
        // the problem where the Initialize call actually binds to the object. This occurred even in cases like Get() whereby we
        // ended up getting the object twice. Any direct usage of this property will cause a call to Initialize ( true ) to be made
        // (if not already done) indicating that we wish to bind to the underlying WMI object.
        //
        // See changes to Initialize
        //
        internal IWbemClassObjectFreeThreaded wbemObject
        {
            get
            {
                if (_wbemObject == null)
                {
                    Initialize(true);
                }
                return _wbemObject;
            }
            set
            {
                _wbemObject = value;
            }
        }

        internal IWbemClassObjectFreeThreaded _wbemObject;

        private PropertyDataCollection properties;
        private PropertyDataCollection systemProperties;
        private QualifierDataCollection qualifiers;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementBaseObject'/> class that is serializable.</para>
        /// </summary>
        /// <param name='info'>The <see cref='System.Runtime.Serialization.SerializationInfo'/> to populate with data.</param>
        /// <param name='context'>The destination (see <see cref='System.Runtime.Serialization.StreamingContext'/> ) for this serialization.</param>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ManagementBaseObject(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public new void Dispose()
        {
            if (_wbemObject != null)
            {
                _wbemObject.Dispose();
                _wbemObject = null;
            }
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///    <para>Provides the internal WMI object represented by a ManagementObject.</para>
        ///    <para>See remarks with regard to usage.</para>
        /// </summary>
        /// <param name='managementObject'>The <see cref='System.Management.ManagementBaseObject'/> that references the requested WMI object. </param>
        /// <returns>
        /// <para>An <see cref='System.IntPtr'/> representing the internal WMI object.</para>
        /// </returns>
        /// <remarks>
        ///    <para>This operator is used internally by instrumentation code. It is not intended
        ///       for direct use by regular client or instrumented applications.</para>
        /// </remarks>
        public static explicit operator IntPtr(ManagementBaseObject managementObject)
        {
            if (null == managementObject)
                return IntPtr.Zero;

            return (IntPtr)managementObject.wbemObject;
        }


        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Factory
        /// <summary>
        /// Factory for various types of base object
        /// </summary>
        /// <param name="wbemObject"> IWbemClassObject </param>
        /// <param name="scope"> The scope</param>
        internal static ManagementBaseObject GetBaseObject(
            IWbemClassObjectFreeThreaded wbemObject,
            ManagementScope scope)
        {
            ManagementBaseObject newObject = null;

            if (_IsClass(wbemObject))
                newObject = ManagementClass.GetManagementClass(wbemObject, scope);
            else
                newObject = ManagementObject.GetManagementObject(wbemObject, scope);

            return newObject;
        }

        //Constructor
        internal ManagementBaseObject(IWbemClassObjectFreeThreaded wbemObject)
        {
            this.wbemObject = wbemObject;
            properties = null;
            systemProperties = null;
            qualifiers = null;
        }

        /// <summary>
        ///    <para>Returns a copy of the object.</para>
        /// </summary>
        /// <returns>
        ///    <para>The new cloned object.</para>
        /// </returns>
        public virtual object Clone()
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

            return new ManagementBaseObject(theClone);
        }

        internal virtual void Initialize(bool getObject) { }

        //
        //Properties
        //

        /// <summary>
        /// <para>Gets or sets a collection of <see cref='System.Management.PropertyData'/> objects describing the properties of the
        ///    management object.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.PropertyDataCollection'/> that represents the
        ///    properties of the management object.</para>
        /// </value>
        /// <seealso cref='System.Management.PropertyData'/>
        public virtual PropertyDataCollection Properties
        {
            get
            {
                Initialize(true);

                return properties ??= new PropertyDataCollection(this, false);
            }
        }

        /// <summary>
        ///    <para>Gets or sets the collection of WMI system properties of the management object (for example, the
        ///       class name, server, and namespace). WMI system property names begin with
        ///       "__".</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.PropertyDataCollection'/> that represents the system properties of the management object.</para>
        /// </value>
        /// <seealso cref='System.Management.PropertyData'/>
        public virtual PropertyDataCollection SystemProperties
        {
            get
            {
                Initialize(false);

                return systemProperties ??= new PropertyDataCollection(this, true);
            }
        }

        /// <summary>
        /// <para>Gets or sets the collection of <see cref='System.Management.QualifierData'/> objects defined on the management object.
        ///    Each element in the collection holds information such as the qualifier name,
        ///    value, and flavor.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.QualifierDataCollection'/> that represents the qualifiers
        ///    defined on the management object.</para>
        /// </value>
        /// <seealso cref='System.Management.QualifierData'/>
        public virtual QualifierDataCollection Qualifiers
        {
            get
            {
                Initialize(true);

                return qualifiers ??= new QualifierDataCollection(this);
            }
        }

        /// <summary>
        ///    <para>Gets or sets the path to the management object's class.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementPath'/> that represents the path to the management object's class.</para>
        /// </value>
        /// <example>
        ///    <para>For example, for the \\MyBox\root\cimv2:Win32_LogicalDisk=
        ///       'C:' object, the class path is \\MyBox\root\cimv2:Win32_LogicalDisk
        ///       .</para>
        /// </example>
        public virtual ManagementPath ClassPath
        {
            get
            {
                object serverName = null;
                object scopeName = null;
                object className = null;
                int propertyType = 0;
                int propertyFlavor = 0;
                int status = (int)ManagementStatus.NoError;

                status = wbemObject.Get_("__SERVER", 0, ref serverName, ref propertyType, ref propertyFlavor);

                if (status == (int)ManagementStatus.NoError)
                {
                    status = wbemObject.Get_("__NAMESPACE", 0, ref scopeName, ref propertyType, ref propertyFlavor);

                    if (status == (int)ManagementStatus.NoError)
                        status = wbemObject.Get_("__CLASS", 0, ref className, ref propertyType, ref propertyFlavor);
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
        //[] operator by property name
        //******************************************************
        /// <summary>
        ///    <para> Gets access to property values through [] notation.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property of interest. </param>
        /// <value>
        ///    An <see cref='object'/> containing the
        ///    value of the requested property.
        /// </value>
        public object this[string propertyName]
        {
            get { return GetPropertyValue(propertyName); }
            set
            {
                Initialize(true);
                try
                {
                    SetPropertyValue(propertyName, value);
                }
                catch (COMException e)
                {
                    ManagementException.ThrowWithExtendedInfo(e);
                }
            }
        }

        //******************************************************
        //GetPropertyValue
        //******************************************************
        /// <summary>
        ///    <para>Gets an equivalent accessor to a property's value.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property of interest. </param>
        /// <returns>
        ///    <para>The value of the specified property.</para>
        /// </returns>
        public object GetPropertyValue(string propertyName)
        {
            if (null == propertyName)
                throw new ArgumentNullException(nameof(propertyName));

            // Check for system properties
            if (propertyName.StartsWith("__", StringComparison.Ordinal))
                return SystemProperties[propertyName].Value;
            else
                return Properties[propertyName].Value;
        }

        //******************************************************
        //GetQualifierValue
        //******************************************************
        /// <summary>
        ///    <para>Gets the value of the specified qualifier.</para>
        /// </summary>
        /// <param name='qualifierName'>The name of the qualifier of interest. </param>
        /// <returns>
        ///    <para>The value of the specified qualifier.</para>
        /// </returns>
        public object GetQualifierValue(string qualifierName)
        {
            return Qualifiers[qualifierName].Value;
        }

        //******************************************************
        //SetQualifierValue
        //******************************************************
        /// <summary>
        ///    <para>Sets the value of the named qualifier.</para>
        /// </summary>
        /// <param name='qualifierName'>The name of the qualifier to set. This parameter cannot be null.</param>
        /// <param name='qualifierValue'>The value to set.</param>
        public void SetQualifierValue(string qualifierName, object qualifierValue)
        {
            Qualifiers[qualifierName].Value = qualifierValue;
        }


        //******************************************************
        //GetPropertyQualifierValue
        //******************************************************
        /// <summary>
        ///    <para>Returns the value of the specified property qualifier.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property to which the qualifier belongs. </param>
        /// <param name='qualifierName'>The name of the property qualifier of interest. </param>
        /// <returns>
        ///    <para>The value of the specified qualifier.</para>
        /// </returns>
        public object GetPropertyQualifierValue(string propertyName, string qualifierName)
        {
            return Properties[propertyName].Qualifiers[qualifierName].Value;
        }

        //******************************************************
        //SetPropertyQualifierValue
        //******************************************************
        /// <summary>
        ///    <para>Sets the value of the specified property qualifier.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property to which the qualifier belongs.</param>
        /// <param name='qualifierName'>The name of the property qualifier of interest.</param>
        /// <param name='qualifierValue'>The new value for the qualifier.</param>
        public void SetPropertyQualifierValue(string propertyName, string qualifierName,
            object qualifierValue)
        {
            Properties[propertyName].Qualifiers[qualifierName].Value = qualifierValue;
        }

        //******************************************************
        //GetText
        //******************************************************
        /// <summary>
        ///    <para>Returns a textual representation of the object in the specified format.</para>
        /// </summary>
        /// <param name='format'>The requested textual format. </param>
        /// <returns>
        ///    <para>The textual representation of the
        ///       object in the specified format.</para>
        /// </returns>
        public string GetText(TextFormat format)
        {
            string objText = null;
            int status = (int)ManagementStatus.NoError;

            //
            // Removed Initialize call since wbemObject is a property that will call Initialize ( true ) on
            // its getter.
            //
            switch (format)
            {
                case TextFormat.Mof:

                    status = wbemObject.GetObjectText_(0, out objText);

                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }

                    return objText;

                case TextFormat.CimDtd20:
                case TextFormat.WmiDtd20:

                    //This may throw on non-XP platforms... - should we catch ?
                    IWbemObjectTextSrc wbemTextSrc = (IWbemObjectTextSrc)new WbemObjectTextSrc();
                    IWbemContext ctx = (IWbemContext)new WbemContext();
                    object v = (bool)true;
                    ctx.SetValue_("IncludeQualifiers", 0, ref v);
                    ctx.SetValue_("IncludeClassOrigin", 0, ref v);

                    if (wbemTextSrc != null)
                    {
                        status = wbemTextSrc.GetText_(0,
                            (IWbemClassObject_DoNotMarshal)(Marshal.GetObjectForIUnknown(wbemObject)),
                            (uint)format, //note: this assumes the format enum has the same values as the underlying WMI enum !!
                            ctx,
                            out objText);
                        if (status < 0)
                        {
                            if ((status & 0xfffff000) == 0x80041000)
                                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                            else
                                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                        }
                    }

                    return objText;

                default:

                    return null;
            }
        }

        /// <summary>
        ///    <para>Compares two management objects.</para>
        /// </summary>
        /// <param name='obj'>An object to compare with this instance.</param>
        /// <returns>
        /// <see langword='true'/> if
        /// <paramref name="obj"/> is an instance of <see cref='System.Management.ManagementBaseObject'/> and represents
        ///    the same object as this instance; otherwise, <see langword='false'/>.
        /// </returns>
        public override bool Equals(object obj)
        {
            bool result = false;

            try
            {
                if (obj is ManagementBaseObject)
                {
                    result = CompareTo((ManagementBaseObject)obj, ComparisonSettings.IncludeAll);
                }
                else
                {
                    return false;
                }
            }
            catch (ManagementException exc)
            {
                if (exc.ErrorCode == ManagementStatus.NotFound)
                {
                    //we could wind up here if Initialize() throws (either here or inside CompareTo())
                    //Since we cannot throw from Equals() imprelemtation and it is invalid to assume
                    //that two objects are different because they fail to initialize
                    //so, we can just compare these invalid paths "by value"

                    if (this is ManagementObject && obj is ManagementObject)
                    {
                        int compareRes = string.Compare(((ManagementObject)this).Path.Path,
                            ((ManagementObject)obj).Path.Path,
                            StringComparison.OrdinalIgnoreCase);
                        return (compareRes == 0);
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            return result;
        }

        /// <summary>
        ///     <para>Serves as a hash function for a particular type, suitable for use in hashing algorithms and data structures like a hash table.</para>
        ///        <para>The hash code for ManagementBaseObjects is based on the MOF for the WbemObject that this instance is based on.  Two different ManagementBaseObject instances pointing to the same WbemObject in WMI will have the same mof and thus the same hash code.  Changing a property value of an object will change the hash code. </para>
        /// </summary>
        /// <returns>
        ///     <para>A hash code for the current object. </para>
        /// </returns>
        public override int GetHashCode()
        {
            //This implementation has to match the Equals() implementation. In Equals(), we use
            //the WMI CompareTo() which compares values of properties, qualifiers etc.
            //Probably the closest we can get is to take the MOF representation of the object and get it's hash code.
            int localHash = 0;
            try
            {
                // GetText may throw if it cannot get a string for the mof for various reasons
                // This should be a very rare event
                localHash = this.GetText(TextFormat.Mof).GetHashCode();
            }
            catch (ManagementException)
            {
                // use the hash code of an empty string on failure to get the mof
                localHash = string.Empty.GetHashCode();
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // use the hash code of an empty string on failure to get the mof
                localHash = string.Empty.GetHashCode();
            }
            return localHash;
        }

        //******************************************************
        //CompareTo
        //******************************************************
        /// <summary>
        ///    <para>Compares this object to another, based on specified options.</para>
        /// </summary>
        /// <param name='otherObject'>The object to which to compare this object. </param>
        /// <param name='settings'>Options on how to compare the objects. </param>
        /// <returns>
        /// <para><see langword='true'/> if the objects compared are equal
        ///    according to the given options; otherwise, <see langword='false'/>
        ///    .</para>
        /// </returns>
        public bool CompareTo(ManagementBaseObject otherObject, ComparisonSettings settings)
        {
            if (null == otherObject)
                throw new ArgumentNullException(nameof(otherObject));

            bool result = false;

            if (null != wbemObject)
            {
                int status = (int)ManagementStatus.NoError;

                status = wbemObject.CompareTo_((int)settings, otherObject.wbemObject);

                if ((int)ManagementStatus.Different == status)
                    result = false;
                else if ((int)ManagementStatus.NoError == status)
                    result = true;
                else if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if (status < 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return result;
        }

        internal string ClassName
        {
            get
            {
                object val = null;
                int dummy1 = 0, dummy2 = 0;
                int status = (int)ManagementStatus.NoError;

                status = wbemObject.Get_("__CLASS", 0, ref val, ref dummy1, ref dummy2);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                if (val is System.DBNull)
                    return string.Empty;
                else
                    return ((string)val);
            }
        }

        private static bool _IsClass(IWbemClassObjectFreeThreaded wbemObject)
        {
            object val = null;
            int dummy1 = 0, dummy2 = 0;

            int status = wbemObject.Get_("__GENUS", 0, ref val, ref dummy1, ref dummy2);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return ((int)val == (int)tag_WBEM_GENUS_TYPE.WBEM_GENUS_CLASS);
        }

        internal bool IsClass
        {
            get
            {
                return _IsClass(wbemObject);
            }
        }

        /// <summary>
        ///    <para>Sets the value of the named property.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property to be changed.</param>
        /// <param name='propertyValue'>The new value for this property.</param>
        public void SetPropertyValue(
            string propertyName,
            object propertyValue)
        {
            if (null == propertyName)
                throw new ArgumentNullException(nameof(propertyName));

            // Check for system properties
            if (propertyName.StartsWith("__", StringComparison.Ordinal))
                SystemProperties[propertyName].Value = propertyValue;
            else
                Properties[propertyName].Value = propertyValue;
        }

    }//ManagementBaseObject
}
