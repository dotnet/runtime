// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC// 
    /// <summary>
    ///    <para> Contains information about a WMI method.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System; 
    /// using System.Management; 
    /// 
    /// // This example shows how to obtain meta data 
    /// // about a WMI method with a given name in a given WMI class 
    /// 
    /// class Sample_MethodData 
    /// { 
    ///     public static int Main(string[] args) { 
    /// 
    ///         // Get the "SetPowerState" method in the Win32_LogicalDisk class 
    ///         ManagementClass diskClass = new ManagementClass("win32_logicaldisk");
    ///         MethodData m = diskClass.Methods["SetPowerState"];
    ///       
    ///         // Get method name (albeit we already know it)
    ///         Console.WriteLine("Name: " + m.Name);
    /// 
    ///         // Get the name of the top-most class where this specific method was defined
    ///         Console.WriteLine("Origin: " + m.Origin);
    ///       
    ///         // List names and types of input parameters
    ///         ManagementBaseObject inParams = m.InParameters;
    ///         foreach(PropertyData pdata in inParams.Properties) {
    ///             Console.WriteLine();
    ///             Console.WriteLine("InParam_Name: " + pdata.Name);
    ///             Console.WriteLine("InParam_Type: " + pdata.Type);
    ///         }
    ///       
    ///         // List names and types of output parameters
    ///         ManagementBaseObject outParams = m.OutParameters;
    ///         foreach(PropertyData pdata in outParams.Properties) {
    ///             Console.WriteLine();
    ///             Console.WriteLine("OutParam_Name: " + pdata.Name);
    ///             Console.WriteLine("OutParam_Type: " + pdata.Type);
    ///         }
    ///       
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///       
    /// ' This example shows how to obtain meta data 
    /// ' about a WMI method with a given name in a given WMI class 
    /// 
    /// Class Sample_ManagementClass
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         
    ///         ' Get the "SetPowerState" method in the Win32_LogicalDisk class 
    ///         Dim diskClass As New ManagementClass("Win32_LogicalDisk")
    ///         Dim m As MethodData = diskClass.Methods("SetPowerState")
    ///       
    ///         ' Get method name (albeit we already know it)
    ///         Console.WriteLine("Name: " &amp; m.Name)
    ///         
    ///         ' Get the name of the top-most class where
    ///         ' this specific method was defined
    ///         Console.WriteLine("Origin: " &amp; m.Origin)
    /// 
    ///         ' List names and types of input parameters
    ///         Dim inParams As ManagementBaseObject 
    ///         inParams = m.InParameters
    ///         Dim pdata As PropertyData
    ///         For Each pdata In inParams.Properties
    ///             Console.WriteLine()
    ///             Console.WriteLine("InParam_Name: " &amp; pdata.Name)
    ///             Console.WriteLine("InParam_Type: " &amp; pdata.Type)
    ///         Next pdata
    /// 
    ///         ' List names and types of output parameters
    ///         Dim outParams As ManagementBaseObject 
    ///         outParams = m.OutParameters
    ///         For Each pdata in outParams.Properties
    ///             Console.WriteLine()
    ///             Console.WriteLine("OutParam_Name: " &amp; pdata.Name)
    ///             Console.WriteLine("OutParam_Type: " &amp; pdata.Type)
    ///         Next pdata
    ///      
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class MethodData
    {
        private ManagementObject parent; //needed to be able to get method qualifiers
        private string methodName;
        private IWbemClassObjectFreeThreaded wmiInParams;
        private IWbemClassObjectFreeThreaded wmiOutParams;
        private QualifierDataCollection qualifiers;

        internal MethodData(ManagementObject parent, string methodName)
        {
            this.parent = parent;
            this.methodName = methodName;
            RefreshMethodInfo();
            qualifiers = null;
        }


        //This private function is used to refresh the information from the Wmi object before returning the requested data
        private void RefreshMethodInfo()
        {
            int status = (int)ManagementStatus.Failed;

            try 
            {
                status = parent.wbemObject.GetMethod_(methodName, 0, out wmiInParams, out wmiOutParams);
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


        /// <summary>
        ///    <para>Gets or sets the name of the method.</para>
        /// </summary>
        /// <value>
        ///    <para>The name of the method.</para>
        /// </value>
        public string Name 
        {
            get { return methodName != null ? methodName : ""; }
        }

        /// <summary>
        ///    <para> Gets or sets the input parameters to the method. Each 
        ///       parameter is described as a property in the object. If a parameter is both in
        ///       and out, it appears in both the <see cref='System.Management.MethodData.InParameters'/> and <see cref='System.Management.MethodData.OutParameters'/>
        ///       properties.</para>
        /// </summary>
        /// <value>
        ///    <para> 
        ///       A <see cref='System.Management.ManagementBaseObject'/>
        ///       containing all the input parameters to the
        ///       method.</para>
        /// </value>
        /// <remarks>
        ///    <para>Each parameter in the object should have an 
        ///    <see langword='ID'/> 
        ///    qualifier, identifying the order of the parameters in the method call.</para>
        /// </remarks>
        public ManagementBaseObject InParameters 
        {
            get 
            { 
                RefreshMethodInfo();
                return (null == wmiInParams) ? null : new ManagementBaseObject(wmiInParams); }
        }

        /// <summary>
        ///    <para> Gets or sets the output parameters to the method. Each 
        ///       parameter is described as a property in the object. If a parameter is both in
        ///       and out, it will appear in both the <see cref='System.Management.MethodData.InParameters'/> and <see cref='System.Management.MethodData.OutParameters'/>
        ///       properties.</para>
        /// </summary>
        /// <value>
        /// <para>A <see cref='System.Management.ManagementBaseObject'/> containing all the output parameters to the method. </para>
        /// </value>
        /// <remarks>
        ///    <para>Each parameter in this object should have an 
        ///    <see langword='ID'/> qualifier to identify the 
        ///       order of the parameters in the method call.</para>
        ///    <para>The ReturnValue property is a special property of 
        ///       the <see cref='System.Management.MethodData.OutParameters'/>
        ///       object and
        ///       holds the return value of the method.</para>
        /// </remarks>
        public ManagementBaseObject OutParameters 
        {
            get 
            { 
                RefreshMethodInfo();
                return (null == wmiOutParams) ? null : new ManagementBaseObject(wmiOutParams); }
        }

        /// <summary>
        ///    <para>Gets the name of the management class in which the method was first 
        ///       introduced in the class inheritance hierarchy.</para>
        /// </summary>
        /// <value>
        ///    A string representing the originating
        ///    management class name.
        /// </value>
        public string Origin 
        {
            get 
            {
                string className = null;
                int status = parent.wbemObject.GetMethodOrigin_(methodName, out className);

                if (status < 0)
                {
                    if (status == (int)tag_WBEMSTATUS.WBEM_E_INVALID_OBJECT)
                        className = String.Empty;   // Interpret as an unspecified property - return ""
                    else if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return className;
            }
        }

        /// <summary>
        ///    <para>Gets a collection of qualifiers defined in the 
        ///       method. Each element is of type <see cref='System.Management.QualifierData'/>
        ///       and contains information such as the qualifier name, value, and
        ///       flavor.</para>
        /// </summary>
        /// <value>
        ///    A <see cref='System.Management.QualifierDataCollection'/> containing the
        ///    qualifiers for this method.
        /// </value>
        /// <seealso cref='System.Management.QualifierData'/>
        public QualifierDataCollection Qualifiers 
        {
            get 
            {
                if (qualifiers == null)
                    qualifiers = new QualifierDataCollection(parent, methodName, QualifierType.MethodQualifier);
                return qualifiers;
            }
        }

    }//MethodData
}
