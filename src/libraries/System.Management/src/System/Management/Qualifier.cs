// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Globalization;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//	
    /// <summary>
    ///    <para> Contains information about a WMI qualifier.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    /// 
    /// // This sample demonstrates how to enumerate qualifiers
    /// // of a ManagementClass object.
    /// class Sample_QualifierData
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
    ///         diskClass.Options.UseAmendedQualifiers = true;
    ///         QualifierData diskQualifier = diskClass.Qualifiers["Description"];
    ///         Console.WriteLine(diskQualifier.Name + " = " + diskQualifier.Value);
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    /// 
    /// ' This sample demonstrates how to enumerate qualifiers
    /// ' of a ManagementClass object.
    /// Class Sample_QualifierData
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim diskClass As New ManagementClass("win32_logicaldisk")
    ///         diskClass.Options.UseAmendedQualifiers = True
    ///         Dim diskQualifier As QualifierData = diskClass.Qualifiers("Description")
    ///         Console.WriteLine(diskQualifier.Name + " = " + diskQualifier.Value)
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class QualifierData
    {
        private ManagementBaseObject parent;  //need access to IWbemClassObject pointer to be able to refresh qualifiers
        private string propertyOrMethodName; //remains null for object qualifiers
        private string qualifierName;
        private QualifierType qualifierType;
        private object qualifierValue;
        private int qualifierFlavor;
        private IWbemQualifierSetFreeThreaded qualifierSet;

        internal QualifierData(ManagementBaseObject parent, string propName, string qualName, QualifierType type)		
        {
            this.parent = parent;
            this.propertyOrMethodName = propName;
            this.qualifierName = qualName;
            this.qualifierType = type;
            RefreshQualifierInfo();
        }

        //This private function is used to refresh the information from the Wmi object before returning the requested data
        private void RefreshQualifierInfo()
        {
            int status = (int)ManagementStatus.Failed;

            qualifierSet = null;
            switch (qualifierType) {
                case QualifierType.ObjectQualifier :
                    status = parent.wbemObject.GetQualifierSet_(out qualifierSet);
                    break;
                case QualifierType.PropertyQualifier :
                    status = parent.wbemObject.GetPropertyQualifierSet_(propertyOrMethodName, out qualifierSet);
                    break;
                case QualifierType.MethodQualifier :
                    status = parent.wbemObject.GetMethodQualifierSet_(propertyOrMethodName, out qualifierSet);
                    break;
                default :
                    throw new ManagementException(ManagementStatus.Unexpected, null, null); //is this the best fit error ??
            }

            if ((status & 0x80000000) == 0) //success
            {
                qualifierValue = null; //Make sure it's null so that we don't leak !
                if (qualifierSet != null)
                    status = qualifierSet.Get_(qualifierName, 0, ref qualifierValue, ref qualifierFlavor);
            }

            if ((status & 0xfffff000) == 0x80041000) //WMI error
                ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
            else if ((status & 0x80000000) != 0) //any failure
                Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
        }


        private static object MapQualValueToWmiValue(object qualVal)
        {
            object wmiValue = System.DBNull.Value;

            if (null != qualVal)
            {
                if (qualVal is Array)
                {
                    if ((qualVal is int[]) || (qualVal is double[]) || (qualVal is string[]) || (qualVal is bool[]))
                        wmiValue = qualVal;
                    else
                    {
                        Array valArray = (Array)qualVal;
                        int length = valArray.Length;
                        Type elementType = (length > 0 ? valArray.GetValue(0).GetType() : null);

                        if (elementType == typeof(int))
                        {
                            wmiValue = new int [length];
                            for (int i = 0; i < length; i++)
                                ((int[])(wmiValue))[i] = Convert.ToInt32(valArray.GetValue(i),(IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int)));
                        }
                        else if (elementType == typeof(double))
                        {
                            wmiValue = new double [length];
                            for (int i = 0; i < length; i++)
                                ((double[])(wmiValue))[i] = Convert.ToDouble(valArray.GetValue(i),(IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(double)));
                        }
                        else if (elementType == typeof(string))
                        {
                            wmiValue = new string [length];
                            for (int i = 0; i < length; i++)
                                ((string[])(wmiValue))[i] = (valArray.GetValue(i)).ToString();
                        }
                        else if (elementType == typeof(bool))
                        {
                            wmiValue = new bool [length];
                            for (int i = 0; i < length; i++)
                                ((bool[])(wmiValue))[i] = Convert.ToBoolean(valArray.GetValue(i),(IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(bool)));
                        }
                        else
                            wmiValue = valArray; //should this throw ?
                    }
                }
                else
                    wmiValue = qualVal;
            }

            return wmiValue;
        }


        /// <summary>
        ///    <para>Represents the name of the qualifier.</para>
        /// </summary>
        /// <value>
        ///    <para>The name of the qualifier.</para>
        /// </value>
        public string Name 
        {
            get { return qualifierName != null ? qualifierName : ""; }
        }

        /// <summary>
        ///    <para>Gets or sets the value of the qualifier.</para>
        /// </summary>
        /// <value>
        ///    <para>The value of the qualifier.</para>
        /// </value>
        /// <remarks>
        ///    <para> Qualifiers can only be of the following subset of CIM 
        ///       types: <see langword='string'/>, <see langword='uint16'/>,
        ///    <see langword='uint32'/>, <see langword='sint32'/>, <see langword='uint64'/>, 
        ///    <see langword='sint64'/>, <see langword='real32'/>, <see langword='real64'/>, 
        ///    <see langword='bool'/>.
        ///       </para>
        /// </remarks>
        public object Value 
        {
            get { 
                RefreshQualifierInfo();
                return ValueTypeSafety.GetSafeObject(qualifierValue); 
            }
            set {
                int status = (int)ManagementStatus.NoError;

                RefreshQualifierInfo();
                object newValue = MapQualValueToWmiValue(value);
                    
                status = qualifierSet.Put_(qualifierName, ref newValue, 
                    qualifierFlavor & ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN);  

                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if ((status & 0x80000000) != 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        ///    <para> Gets or sets a value indicating whether the qualifier is amended.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this qualifier is amended; 
        ///    otherwise, <see langword='false'/>.</para>
        /// </value>
        /// <remarks>
        ///    <para> Amended qualifiers are
        ///       qualifiers whose value can be localized through WMI. Localized qualifiers
        ///       reside in separate namespaces in WMI and can be merged into the basic class
        ///       definition when retrieved.</para>
        /// </remarks>
        public bool IsAmended 
        {
            get { 
                RefreshQualifierInfo(); 
                return ((int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_AMENDED ==
                    (qualifierFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_AMENDED));
            }
            set 
            {
                int status = (int)ManagementStatus.NoError;

                RefreshQualifierInfo ();
                // Mask out origin bits 
                int flavor = qualifierFlavor & ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN;

                if (value)
                    flavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_AMENDED;
                else
                    flavor &= ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_AMENDED;

                status = qualifierSet.Put_(qualifierName, ref qualifierValue, flavor);

                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if ((status & 0x80000000) != 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the qualifier has been defined locally on 
        ///       this class or has been propagated from a base class.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the qualifier has been defined 
        ///    locally on this class; otherwise, <see langword='false'/>. </para>
        /// </value>
        public bool IsLocal 
        {
            get {
                RefreshQualifierInfo();
                return ((int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_ORIGIN_LOCAL ==
                    (qualifierFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN));
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the qualifier should be propagated to instances of the 
        ///       class.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if this qualifier should be 
        ///    propagated to instances of the class; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool PropagatesToInstance 
        {
            get {
                RefreshQualifierInfo();
                return ((int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE ==
                    (qualifierFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE));
            }
            set {
                int status = (int)ManagementStatus.NoError;

                RefreshQualifierInfo ();
                // Mask out origin bits 
                int flavor = qualifierFlavor & ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN;

                if (value)
                    flavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE;
                else
                    flavor &= ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE;

                status = qualifierSet.Put_(qualifierName, ref qualifierValue, flavor);

                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if ((status & 0x80000000) != 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the qualifier should be propagated to 
        ///       subclasses of the class.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the qualifier should be 
        ///    propagated to subclasses of this class; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool PropagatesToSubclass 
        {
            get {
                RefreshQualifierInfo();
                return ((int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS ==
                    (qualifierFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS));
            }
            set {
                int status = (int)ManagementStatus.NoError;

                RefreshQualifierInfo ();
                // Mask out origin bits 
                int flavor = qualifierFlavor & ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN;

                if (value)
                    flavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS;
                else
                    flavor &= ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS;

                status = qualifierSet.Put_(qualifierName, ref qualifierValue, flavor);

                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if ((status & 0x80000000) != 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the value of the qualifier can be 
        ///       overridden when propagated.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the value of the qualifier 
        ///    can be overridden when propagated; otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsOverridable 
        {
            get {
                RefreshQualifierInfo();
                return ((int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_OVERRIDABLE == 
                    (qualifierFlavor & (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_PERMISSIONS));
            }
            set {
                int status = (int)ManagementStatus.NoError;

                RefreshQualifierInfo ();
                // Mask out origin bits 
                int flavor = qualifierFlavor & ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_MASK_ORIGIN;
                    
                if (value)
                    flavor &= ~(int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_NOT_OVERRIDABLE;
                else
                    flavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_NOT_OVERRIDABLE;

                status = qualifierSet.Put_(qualifierName, ref qualifierValue, flavor);

                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else if ((status & 0x80000000) != 0)
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

    }//QualifierData
}
