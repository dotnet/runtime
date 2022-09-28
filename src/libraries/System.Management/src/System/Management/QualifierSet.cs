// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    /// <para> Represents a collection of <see cref='System.Management.QualifierData'/> objects.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates how to list all qualifiers including amended
    /// // qualifiers of a ManagementClass object.
    /// class Sample_QualifierDataCollection
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
    ///         diskClass.Options.UseAmendedQualifiers = true;
    ///         QualifierDataCollection qualifierCollection = diskClass.Qualifiers;
    ///         foreach (QualifierData q in qualifierCollection) {
    ///             Console.WriteLine(q.Name + " = " + q.Value);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    ///       Imports System.Management
    ///       ' This sample demonstrates how to list all qualifiers including amended
    ///       ' qualifiers of a ManagementClass object.
    ///       Class Sample_QualifierDataCollection
    ///       Overloads Public Shared Function Main(args() As String) As Integer
    ///       Dim diskClass As New ManagementClass("Win32_LogicalDisk")
    ///       diskClass.Options.UseAmendedQualifiers = true
    ///       Dim qualifierCollection As QualifierDataCollection = diskClass.Qualifiers
    ///       Dim q As QualifierData
    ///       For Each q In qualifierCollection
    ///       Console.WriteLine(q.Name &amp; " = " &amp; q.Value)
    ///       Next q
    ///       Return 0
    ///       End Function
    ///       End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class QualifierDataCollection : ICollection, IEnumerable
    {
        private readonly ManagementBaseObject parent;
        private readonly string propertyOrMethodName;
        private readonly QualifierType qualifierSetType;

        internal QualifierDataCollection(ManagementBaseObject parent) : base()
        {
            this.parent = parent;
            this.qualifierSetType = QualifierType.ObjectQualifier;
            this.propertyOrMethodName = null;
        }

        internal QualifierDataCollection(ManagementBaseObject parent, string propertyOrMethodName, QualifierType type) : base()
        {
            this.parent = parent;
            this.propertyOrMethodName = propertyOrMethodName;
            this.qualifierSetType = type;
        }

        /// <summary>
        /// Return the qualifier set associated with its type
        /// Overload with use of private data member, qualifierType
        /// </summary>
        private IWbemQualifierSetFreeThreaded GetTypeQualifierSet()
        {
            return GetTypeQualifierSet(qualifierSetType);
        }

        /// <summary>
        /// Return the qualifier set associated with its type
        /// </summary>
        private IWbemQualifierSetFreeThreaded GetTypeQualifierSet(QualifierType qualifierSetType)
        {
            IWbemQualifierSetFreeThreaded qualifierSet = null;

            int status = qualifierSetType switch
            {
                QualifierType.ObjectQualifier => parent.wbemObject.GetQualifierSet_(out qualifierSet),
                QualifierType.PropertyQualifier => parent.wbemObject.GetPropertyQualifierSet_(propertyOrMethodName, out qualifierSet),
                QualifierType.MethodQualifier => parent.wbemObject.GetMethodQualifierSet_(propertyOrMethodName, out qualifierSet),
                _ => throw new ManagementException(ManagementStatus.Unexpected, null, null),    // Is this the best fit error ??
            };
            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            return qualifierSet;
        }

        //
        //ICollection
        //

        /// <summary>
        /// <para>Gets or sets the number of <see cref='System.Management.QualifierData'/> objects in the <see cref='System.Management.QualifierDataCollection'/>.</para>
        /// </summary>
        /// <value>
        ///    <para>The number of objects in the collection.</para>
        /// </value>
        public int Count
        {
            get
            {
                string[] qualifierNames = null;
                IWbemQualifierSetFreeThreaded quals;
                try
                {
                    quals = GetTypeQualifierSet();
                }
                catch (ManagementException e)
                {
                    // If we ask for the number of qualifiers on a system property, we return '0'
                    if (qualifierSetType == QualifierType.PropertyQualifier && e.ErrorCode == ManagementStatus.SystemProperty)
                        return 0;
                    else
                        throw;
                }
                int status = quals.GetNames_(0, out qualifierNames);

                if (status < 0)
                {
                    if ((status & 0xfffff000) == 0x80041000)
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    else
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return qualifierNames.Length;
            }
        }

        /// <summary>
        ///    <para>Gets or sets a value indicating whether the object is synchronized.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/> if the object is synchronized;
        ///    otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        ///    <para>Gets or sets the object to be used for synchronization.</para>
        /// </summary>
        /// <value>
        ///    <para>The object to be used for synchronization.</para>
        /// </value>
        public object SyncRoot
        {
            get { return this; }
        }

        /// <overload>
        /// <para>Copies the <see cref='System.Management.QualifierDataCollection'/> into an array.</para>
        /// </overload>
        /// <summary>
        /// <para> Copies the <see cref='System.Management.QualifierDataCollection'/> into an array.</para>
        /// </summary>
        /// <param name='array'>The array to which to copy the <see cref='System.Management.QualifierDataCollection'/>. </param>
        /// <param name='index'>The index from which to start copying. </param>
        public void CopyTo(Array array, int index)
        {
            if (null == array)
                throw new ArgumentNullException(nameof(array));

            if ((index < array.GetLowerBound(0)) || (index > array.GetUpperBound(0)))
                throw new ArgumentOutOfRangeException(nameof(index));

            // Get the names of the qualifiers
            string[] qualifierNames = null;
            IWbemQualifierSetFreeThreaded quals;
            try
            {
                quals = GetTypeQualifierSet();
            }
            catch (ManagementException e)
            {
                // There are NO qualifiers on system properties, so we just return
                if (qualifierSetType == QualifierType.PropertyQualifier && e.ErrorCode == ManagementStatus.SystemProperty)
                    return;
                else
                    throw;
            }
            int status = quals.GetNames_(0, out qualifierNames);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }

            if ((index + qualifierNames.Length) > array.Length)
                throw new ArgumentException(null, nameof(index));

            foreach (string qualifierName in qualifierNames)
                array.SetValue(new QualifierData(parent, propertyOrMethodName, qualifierName, qualifierSetType), index++);

            return;
        }

        /// <summary>
        /// <para>Copies the <see cref='System.Management.QualifierDataCollection'/> into a specialized
        /// <see cref='System.Management.QualifierData'/>
        /// array.</para>
        /// </summary>
        /// <param name='qualifierArray'><para>The specialized array of <see cref='System.Management.QualifierData'/> objects
        /// to which to copy the <see cref='System.Management.QualifierDataCollection'/>.</para></param>
        /// <param name=' index'>The index from which to start copying.</param>
        public void CopyTo(QualifierData[] qualifierArray, int index)
        {
            CopyTo((Array)qualifierArray, index);
        }

        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)(new QualifierDataEnumerator(parent, propertyOrMethodName, qualifierSetType));
        }

        /// <summary>
        /// <para>Returns an enumerator for the <see cref='System.Management.QualifierDataCollection'/>. This method is strongly typed.</para>
        /// </summary>
        /// <returns>
        /// <para>An <see cref='System.Collections.IEnumerator'/> that can be used to iterate through the
        ///    collection.</para>
        /// </returns>
        public QualifierDataEnumerator GetEnumerator()
        {
            return new QualifierDataEnumerator(parent, propertyOrMethodName, qualifierSetType);
        }

        /// <summary>
        /// <para>Represents the enumerator for <see cref='System.Management.QualifierData'/>
        /// objects in the <see cref='System.Management.QualifierDataCollection'/>.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        ///
        /// // This sample demonstrates how to enumerate qualifiers of a ManagementClass
        /// // using QualifierDataEnumerator object.
        /// class Sample_QualifierDataEnumerator
        /// {
        ///     public static int Main(string[] args) {
        ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
        ///         diskClass.Options.UseAmendedQualifiers = true;
        ///         QualifierDataCollection diskQualifier = diskClass.Qualifiers;
        ///         QualifierDataCollection.QualifierDataEnumerator
        ///             qualifierEnumerator = diskQualifier.GetEnumerator();
        ///         while(qualifierEnumerator.MoveNext()) {
        ///             Console.WriteLine(qualifierEnumerator.Current.Name + " = " +
        ///                 qualifierEnumerator.Current.Value);
        ///         }
        ///         return 0;
        ///     }
        /// }
        ///    </code>
        ///    <code lang='VB'>Imports System
        /// Imports System.Management
        ///
        /// ' This sample demonstrates how to enumerate qualifiers of a ManagementClass
        /// ' using QualifierDataEnumerator object.
        /// Class Sample_QualifierDataEnumerator
        ///     Overloads Public Shared Function Main(args() As String) As Integer
        ///         Dim diskClass As New ManagementClass("win32_logicaldisk")
        ///         diskClass.Options.UseAmendedQualifiers = True
        ///         Dim diskQualifier As QualifierDataCollection = diskClass.Qualifiers
        ///         Dim qualifierEnumerator As _
        ///             QualifierDataCollection.QualifierDataEnumerator = _
        ///                 diskQualifier.GetEnumerator()
        ///         While qualifierEnumerator.MoveNext()
        ///             Console.WriteLine(qualifierEnumerator.Current.Name &amp; _
        ///                 " = " &amp; qualifierEnumerator.Current.Value)
        ///         End While
        ///         Return 0
        ///     End Function
        /// End Class
        ///    </code>
        /// </example>
        public class QualifierDataEnumerator : IEnumerator
        {
            private readonly ManagementBaseObject parent;
            private readonly string propertyOrMethodName;
            private readonly QualifierType qualifierType;
            private readonly string[] qualifierNames;
            private int index = -1;

            //Internal constructor
            internal QualifierDataEnumerator(ManagementBaseObject parent, string propertyOrMethodName,
                                                        QualifierType qualifierType)
            {
                this.parent = parent;
                this.propertyOrMethodName = propertyOrMethodName;
                this.qualifierType = qualifierType;
                this.qualifierNames = null;

                IWbemQualifierSetFreeThreaded qualifierSet = null;
                int status = (int)ManagementStatus.NoError;

                status = qualifierType switch
                {
                    QualifierType.ObjectQualifier => parent.wbemObject.GetQualifierSet_(out qualifierSet),
                    QualifierType.PropertyQualifier => parent.wbemObject.GetPropertyQualifierSet_(propertyOrMethodName, out qualifierSet),
                    QualifierType.MethodQualifier => parent.wbemObject.GetMethodQualifierSet_(propertyOrMethodName, out qualifierSet),
                    _ => throw new ManagementException(ManagementStatus.Unexpected, null, null),    // Is this the best fit error ??
                };

                // If we got an error code back, assume there are NO qualifiers for this object/property/method
                if (status < 0)
                {
                    qualifierNames = Array.Empty<string>();
                }
                else
                {
                    status = qualifierSet.GetNames_(0, out qualifierNames);

                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }
                }
            }

            //Standard "Current" variant
            /// <internalonly/>
            object IEnumerator.Current { get { return (object)this.Current; } }

            /// <summary>
            /// <para>Gets or sets the current <see cref='System.Management.QualifierData'/> in the <see cref='System.Management.QualifierDataCollection'/> enumeration.</para>
            /// </summary>
            /// <value>
            /// <para>The current <see cref='System.Management.QualifierData'/> element in the collection.</para>
            /// </value>
            public QualifierData Current
            {
                get
                {
                    if ((index == -1) || (index == qualifierNames.Length))
                        throw new InvalidOperationException();
                    else
                        return new QualifierData(parent, propertyOrMethodName,
                                                qualifierNames[index], qualifierType);
                }
            }

            /// <summary>
            /// <para> Moves to the next element in the <see cref='System.Management.QualifierDataCollection'/> enumeration.</para>
            /// </summary>
            /// <returns>
            /// <para><see langword='true'/> if the enumerator was successfully advanced to the next
            ///    element; <see langword='false'/> if the enumerator has passed the end of the
            ///    collection.</para>
            /// </returns>
            public bool MoveNext()
            {
                if (index == qualifierNames.Length) //passed the end of the array
                    return false; //don't advance the index any more

                index++;
                return (index == qualifierNames.Length) ? false : true;
            }

            /// <summary>
            /// <para>Resets the enumerator to the beginning of the <see cref='System.Management.QualifierDataCollection'/> enumeration.</para>
            /// </summary>
            public void Reset()
            {
                index = -1;
            }

        }//QualifierDataEnumerator


        //
        //Methods
        //

        /// <summary>
        /// <para> Gets the specified <see cref='System.Management.QualifierData'/> from the <see cref='System.Management.QualifierDataCollection'/>.</para>
        /// </summary>
        /// <param name='qualifierName'>The name of the <see cref='System.Management.QualifierData'/> to access in the <see cref='System.Management.QualifierDataCollection'/>. </param>
        /// <value>
        /// <para>A <see cref='System.Management.QualifierData'/>, based on the name specified.</para>
        /// </value>
        public virtual QualifierData this[string qualifierName]
        {
            get
            {
                if (null == qualifierName)
                    throw new ArgumentNullException(nameof(qualifierName));

                return new QualifierData(parent, propertyOrMethodName, qualifierName, qualifierSetType);
            }
        }

        /// <summary>
        /// <para>Removes a <see cref='System.Management.QualifierData'/> from the <see cref='System.Management.QualifierDataCollection'/> by name.</para>
        /// </summary>
        /// <param name='qualifierName'>The name of the <see cref='System.Management.QualifierData'/> to remove. </param>
        public virtual void Remove(string qualifierName)
        {
            int status = GetTypeQualifierSet().Delete_(qualifierName);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

        /// <overload>
        /// <para>Adds a <see cref='System.Management.QualifierData'/> to the <see cref='System.Management.QualifierDataCollection'/>.</para>
        /// </overload>
        /// <summary>
        /// <para>Adds a <see cref='System.Management.QualifierData'/> to the <see cref='System.Management.QualifierDataCollection'/>. This overload specifies the qualifier name and value.</para>
        /// </summary>
        /// <param name='qualifierName'>The name of the <see cref='System.Management.QualifierData'/> to be added to the <see cref='System.Management.QualifierDataCollection'/>. </param>
        /// <param name='qualifierValue'>The value for the new qualifier. </param>
        public virtual void Add(string qualifierName, object qualifierValue)
        {
            Add(qualifierName, qualifierValue, false, false, false, true);
        }



        /// <summary>
        /// <para>Adds a <see cref='System.Management.QualifierData'/> to the <see cref='System.Management.QualifierDataCollection'/>. This overload
        ///    specifies all property values for a <see cref='System.Management.QualifierData'/> object.</para>
        /// </summary>
        /// <param name='qualifierName'>The qualifier name. </param>
        /// <param name='qualifierValue'>The qualifier value. </param>
        /// <param name='isAmended'><see langword='true'/> to specify that this qualifier is amended (flavor); otherwise, <see langword='false'/>. </param>
        /// <param name='propagatesToInstance'><see langword='true'/> to propagate this qualifier to instances; otherwise, <see langword='false'/>. </param>
        /// <param name='propagatesToSubclass'><see langword='true'/> to propagate this qualifier to subclasses; otherwise, <see langword='false'/>. </param>
        /// <param name='isOverridable'><see langword='true'/> to specify that this qualifier's value is overridable in instances of subclasses; otherwise, <see langword='false'/>. </param>
        public virtual void Add(string qualifierName, object qualifierValue, bool isAmended, bool propagatesToInstance, bool propagatesToSubclass, bool isOverridable)
        {

            //Build the flavors bitmask and call the internal Add that takes a bitmask
            int qualFlavor = 0;
            if (isAmended) qualFlavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_AMENDED;
            if (propagatesToInstance) qualFlavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_INSTANCE;
            if (propagatesToSubclass) qualFlavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_FLAG_PROPAGATE_TO_DERIVED_CLASS;

            // Note we use the NOT condition here since WBEM_FLAVOR_OVERRIDABLE == 0
            if (!isOverridable) qualFlavor |= (int)tag_WBEM_FLAVOR_TYPE.WBEM_FLAVOR_NOT_OVERRIDABLE;

            //Try to add the qualifier to the WMI object
            int status = GetTypeQualifierSet().Put_(qualifierName, ref qualifierValue, qualFlavor);

            if (status < 0)
            {
                if ((status & 0xfffff000) == 0x80041000)
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                else
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
            }
        }

    }//QualifierDataCollection
}
