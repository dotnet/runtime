// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//	
    /// <summary>
    ///    <para> Represents the set of properties of a WMI object.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System; 
    /// using System.Management; 
    /// 
    /// // This sample demonstrates how to enumerate properties 
    /// // in a ManagementObject object.
    /// class Sample_PropertyDataCollection 
    /// { 
    ///     public static int Main(string[] args) {
    ///         ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid = \"c:\""); 
    ///         PropertyDataCollection diskProperties = disk.Properties;
    ///         foreach (PropertyData diskProperty in diskProperties) {
    ///             Console.WriteLine("Property = " + diskProperty.Name);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    /// 
    /// ' This sample demonstrates how to enumerate properties
    /// ' in a ManagementObject object.
    /// Class Sample_PropertyDataCollection
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim disk As New ManagementObject("win32_logicaldisk.deviceid=""c:""")
    ///         Dim diskProperties As PropertyDataCollection = disk.Properties
    ///         Dim diskProperty As PropertyData
    ///         For Each diskProperty In diskProperties
    ///             Console.WriteLine("Property = " &amp; diskProperty.Name)
    ///         Next diskProperty
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class PropertyDataCollection : ICollection, IEnumerable
    {
        private ManagementBaseObject parent;
        bool isSystem;

        internal PropertyDataCollection(ManagementBaseObject parent, bool isSystem) : base()
        {
            this.parent = parent;
            this.isSystem = isSystem;
        }

        //
        //ICollection
        //

        /// <summary>
        /// <para>Gets or sets the number of objects in the <see cref='System.Management.PropertyDataCollection'/>.</para>
        /// </summary>
        /// <value>
        ///    <para>The number of objects in the collection.</para>
        /// </value>
        public int Count 
        {
            get {
                string[] propertyNames = null; object qualVal = null;
                int flag;
                if (isSystem)
                    flag = (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_SYSTEM_ONLY;
                else
                    flag = (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_NONSYSTEM_ONLY;

                flag |= (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_ALWAYS;

                int status = parent.wbemObject.GetNames_(null, flag, ref qualVal, out propertyNames);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return propertyNames.Length;
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the object is synchronized.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the object is synchronized; 
        ///    otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsSynchronized { get { return false; } 
        }

        /// <summary>
        ///    <para>Gets or sets the object to be used for synchronization.</para>
        /// </summary>
        /// <value>
        ///    <para>The object to be used for synchronization.</para>
        /// </value>
        public object SyncRoot { get { return this; } 
        }

        /// <overload>
        /// <para>Copies the <see cref='System.Management.PropertyDataCollection'/> into an array.</para>
        /// </overload>
        /// <summary>
        /// <para>Copies the <see cref='System.Management.PropertyDataCollection'/> into an array.</para>
        /// </summary>
        /// <param name='array'>The array to which to copy the <see cref='System.Management.PropertyDataCollection'/>. </param>
        /// <param name='index'>The index from which to start copying. </param>
        public void CopyTo(Array array, int index) 
        {
            if (null == array)
                throw new ArgumentNullException(nameof(array));

            if ((index < array.GetLowerBound(0)) || (index > array.GetUpperBound(0)))
                throw new ArgumentOutOfRangeException(nameof(index));

            // Get the names of the properties 
            string[] nameArray = null;
            object dummy = null;
            int flag = 0;

            if (isSystem)
                flag |= (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_SYSTEM_ONLY;
            else
                flag |= (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_NONSYSTEM_ONLY;
                
            flag |= (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_ALWAYS;
                
            int status = this.parent.wbemObject.GetNames_(null, flag, ref dummy, out nameArray);

            if (status >= 0)
            {
                if ((index + nameArray.Length) > array.Length)
                    throw new ArgumentException(null,nameof(index));

                foreach (string propertyName in nameArray)
                    array.SetValue(new PropertyData(parent, propertyName), index++);
            }

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return;
        }

        /// <summary>
        /// <para>Copies the <see cref='System.Management.PropertyDataCollection'/> to a specialized <see cref='System.Management.PropertyData'/> object
        ///    array.</para>
        /// </summary>
        /// <param name='propertyArray'>The destination array to contain the copied <see cref='System.Management.PropertyDataCollection'/>.</param>
        /// <param name=' index'>The index in the destination array from which to start copying.</param>
        public void CopyTo(PropertyData[] propertyArray, int index)
        {
            CopyTo((Array)propertyArray, index);	
        }
        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)(new PropertyDataEnumerator(parent, isSystem));
        }

        /// <summary>
        /// <para>Returns the enumerator for this <see cref='System.Management.PropertyDataCollection'/>.</para>
        /// </summary>
        /// <returns>
        /// <para>An <see cref='System.Collections.IEnumerator'/> 
        /// that can be used to iterate through the collection.</para>
        /// </returns>
        public PropertyDataEnumerator GetEnumerator()
        {
            return new PropertyDataEnumerator(parent, isSystem);
        }

        //Enumerator class
        /// <summary>
        /// <para>Represents the enumerator for <see cref='System.Management.PropertyData'/> 
        /// objects in the <see cref='System.Management.PropertyDataCollection'/>.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>using System; 
        /// using System.Management; 
        /// 
        /// // This sample demonstrates how to enumerate all properties in a 
        /// // ManagementObject using the PropertyDataEnumerator object. 
        /// class Sample_PropertyDataEnumerator 
        /// {
        ///     public static int Main(string[] args) { 
        ///         ManagementObject disk = new ManagementObject("Win32_LogicalDisk.DeviceID='C:'");
        ///         PropertyDataCollection.PropertyDataEnumerator propertyEnumerator = disk.Properties.GetEnumerator();
        ///         while(propertyEnumerator.MoveNext()) {
        ///             PropertyData p = (PropertyData)propertyEnumerator.Current;
        ///             Console.WriteLine("Property found: " + p.Name);
        ///         }
        ///         return 0;
        ///     }
        /// }
        ///    </code>
        ///    <code lang='VB'>Imports System
        /// Imports System.Management
        /// 
        /// ' This sample demonstrates how to enumerate all properties in a
        /// ' ManagementObject using PropertyDataEnumerator object.
        /// Class Sample_PropertyDataEnumerator
        ///     Overloads Public Shared Function Main(args() As String) As Integer
        ///         Dim disk As New ManagementObject("Win32_LogicalDisk.DeviceID='C:'")
        ///         Dim propertyEnumerator As _
        ///           PropertyDataCollection.PropertyDataEnumerator = disk.Properties.GetEnumerator()
        ///         While propertyEnumerator.MoveNext()
        ///             Dim p As PropertyData = _
        ///                 CType(propertyEnumerator.Current, PropertyData)
        ///             Console.WriteLine("Property found: " &amp; p.Name)
        ///          End While
        ///          Return 0
        ///      End Function
        /// End Class
        ///    </code>
        /// </example>
        public class PropertyDataEnumerator : IEnumerator
        {
            private ManagementBaseObject parent;
            private string[] propertyNames;
            private int index;

            internal PropertyDataEnumerator(ManagementBaseObject parent, bool isSystem)
            {
                this.parent = parent;
                propertyNames = null; index = -1;
                int flag; object qualVal = null;

                if (isSystem)
                    flag = (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_SYSTEM_ONLY;
                else
                    flag = (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_NONSYSTEM_ONLY;

                flag |= (int)tag_WBEM_CONDITION_FLAG_TYPE.WBEM_FLAG_ALWAYS;

                int status = parent.wbemObject.GetNames_(null, flag, ref qualVal, out propertyNames);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        
            /// <internalonly/>
            object IEnumerator.Current { get { return (object)this.Current; } }

            /// <summary>
            /// <para>Gets the current <see cref='System.Management.PropertyData'/> in the <see cref='System.Management.PropertyDataCollection'/> enumeration.</para>
            /// </summary>
            /// <value>
            ///    The current <see cref='System.Management.PropertyData'/>
            ///    element in the collection.
            /// </value>
            public PropertyData Current 
            {
                get {
                    if ((index == -1) || (index == propertyNames.Length))
                        throw new InvalidOperationException();
                    else
                        return new PropertyData(parent, propertyNames[index]);
                }
            }

            /// <summary>
            /// <para> Moves to the next element in the <see cref='System.Management.PropertyDataCollection'/> 
            /// enumeration.</para>
            /// </summary>
            /// <returns>
            /// <para><see langword='true'/> if the enumerator was successfully advanced to the next element; 
            /// <see langword='false'/> if the enumerator has passed the end of the collection.</para>
            /// </returns>
            public bool MoveNext()
            {
                if (index == propertyNames.Length) //passed the end of the array
                    return false; //don't advance the index any more

                index++;
                return (index == propertyNames.Length) ? false : true;
            }

            /// <summary>
            /// <para>Resets the enumerator to the beginning of the <see cref='System.Management.PropertyDataCollection'/> 
            /// enumeration.</para>
            /// </summary>
            public void Reset()
            {
                index = -1;
            }
            
        }//PropertyDataEnumerator



        //
        // Methods
        //

        /// <summary>
        /// <para> Returns the specified property from the <see cref='System.Management.PropertyDataCollection'/>, using [] syntax.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property to retrieve.</param>
        /// <value>
        /// <para> A <see cref='System.Management.PropertyData'/>, based on
        ///    the name specified.</para>
        /// </value>
        /// <example>
        ///    <code lang='C#'>ManagementObject o = new ManagementObject("Win32_LogicalDisk.Name = 'C:'");
        /// Console.WriteLine("Free space on C: drive is: ", c.Properties["FreeSpace"].Value);
        ///    </code>
        ///    <code lang='VB'>Dim o As New ManagementObject("Win32_LogicalDisk.Name=""C:""")
        /// Console.WriteLine("Free space on C: drive is: " &amp; c.Properties("FreeSpace").Value)
        ///    </code>
        /// </example>
        public virtual PropertyData this[string propertyName] 
        {
            get { 
                if (null == propertyName)
                    throw new ArgumentNullException(nameof(propertyName));

                return new PropertyData(parent, propertyName);
            }
        }

        /// <summary>
        /// <para>Removes a <see cref='System.Management.PropertyData'/> from the <see cref='System.Management.PropertyDataCollection'/>.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property to be removed.</param>
        /// <remarks>
        ///    <para> Properties can only be removed from class definitions, 
        ///       not from instances. This method is only valid when invoked on a property
        ///       collection in a <see cref='System.Management.ManagementClass'/>.</para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>ManagementClass c = new ManagementClass("MyClass");
        /// c.Properties.Remove("PropThatIDontWantOnThisClass");
        ///    </code>
        ///    <code lang='VB'>Dim c As New ManagementClass("MyClass")
        /// c.Properties.Remove("PropThatIDontWantOnThisClass")
        ///    </code>
        /// </example>
        public virtual void Remove(string propertyName)
        {
            // On instances, reset the property to the default value for the class.
            if (parent.GetType() == typeof(ManagementObject))
            {
                ManagementClass cls = new ManagementClass(parent.ClassPath);
                parent.SetPropertyValue(propertyName, cls.GetPropertyValue(propertyName));
            }
            else
            {
                int status = parent.wbemObject.Delete_(propertyName);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
            }
        }

        /// <overload>
        /// <para>Adds a new <see cref='System.Management.PropertyData'/> with the specified value.</para>
        /// </overload>
        /// <summary>
        /// <para>Adds a new <see cref='System.Management.PropertyData'/> with the specified value. The value cannot
        ///    be null and must be convertable to a CIM type.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the new property.</param>
        /// <param name='propertyValue'>The value of the property (cannot be null).</param>
        /// <remarks>
        ///    <para> Properties can only be added to class definitions, not 
        ///       to instances. This method is only valid when invoked on a <see cref='System.Management.PropertyDataCollection'/>
        ///       in
        ///       a <see cref='System.Management.ManagementClass'/>.</para>
        /// </remarks>
        public virtual void Add(string propertyName, object propertyValue)
        {
            if (null == propertyValue)
                throw new ArgumentNullException(nameof(propertyValue));

            if (parent.GetType() == typeof(ManagementObject)) //can't add properties to instance
                throw new InvalidOperationException();

            CimType cimType = 0;
            bool isArray = false;
            object wmiValue = PropertyData.MapValueToWmiValue(propertyValue, out isArray, out cimType);
            int wmiCimType = (int)cimType;

            if (isArray)
                wmiCimType |= (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY;

            int status = parent.wbemObject.Put_(propertyName, 0, ref wmiValue, wmiCimType);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <summary>
        /// <para>Adds a new <see cref='System.Management.PropertyData'/> with the specified value and CIM type.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property.</param>
        /// <param name='propertyValue'>The value of the property (which can be null).</param>
        /// <param name='propertyType'>The CIM type of the property.</param>
        /// <remarks>
        ///    <para> Properties can only be added to class definitions, not 
        ///       to instances. This method is only valid when invoked on a <see cref='System.Management.PropertyDataCollection'/>
        ///       in
        ///       a <see cref='System.Management.ManagementClass'/>.</para>
        /// </remarks>
        public void Add(string propertyName, object propertyValue, CimType propertyType)
        {
            if (null == propertyName)
                throw new ArgumentNullException(nameof(propertyName));

            if (parent.GetType() == typeof(ManagementObject)) //can't add properties to instance
                throw new InvalidOperationException();

            int wmiCimType = (int)propertyType;
            bool isArray = false;

            if ((null != propertyValue) && propertyValue.GetType().IsArray)
            {
                isArray = true;
                wmiCimType = (wmiCimType | (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY);
            }

            object wmiValue = PropertyData.MapValueToWmiValue(propertyValue, propertyType, isArray);

            int status = parent.wbemObject.Put_(propertyName, 0, ref wmiValue, wmiCimType);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }
        
        /// <summary>
        /// <para>Adds a new <see cref='System.Management.PropertyData'/> with no assigned value.</para>
        /// </summary>
        /// <param name='propertyName'>The name of the property.</param>
        /// <param name='propertyType'>The CIM type of the property.</param>
        /// <param name='isArray'><see langword='true'/> to specify that the property is an array type; otherwise, <see langword='false'/>.</param>
        /// <remarks>
        ///    <para> Properties can only be added to class definitions, not 
        ///       to instances. This method is only valid when invoked on a <see cref='System.Management.PropertyDataCollection'/>
        ///       in
        ///       a <see cref='System.Management.ManagementClass'/>.</para>
        /// </remarks>
        public void Add(string propertyName, CimType propertyType, bool isArray)
        {
            if (null == propertyName)
                throw new ArgumentNullException(propertyName);

            if (parent.GetType() == typeof(ManagementObject)) //can't add properties to instance
                throw new InvalidOperationException();

            int wmiCimType = (int)propertyType;  
            
            if (isArray)
                wmiCimType = (wmiCimType | (int)tag_CIMTYPE_ENUMERATION.CIM_FLAG_ARRAY);

            object dummyObj = System.DBNull.Value;

            int status = parent.wbemObject.Put_(propertyName, 0, ref dummyObj, wmiCimType);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }
        
    }//PropertyDataCollection
}
