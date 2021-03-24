// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents the set of methods available in the collection.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System;
    /// using System.Management;
    ///
    /// // This sample demonstrates enumerate all methods in a ManagementClass object.
    /// class Sample_MethodDataCollection
    /// {
    ///     public static int Main(string[] args) {
    ///         ManagementClass diskClass = new ManagementClass("win32_logicaldisk");
    ///         MethodDataCollection diskMethods = diskClass.Methods;
    ///         foreach (MethodData method in diskMethods) {
    ///             Console.WriteLine("Method = " + method.Name);
    ///         }
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System
    /// Imports System.Management
    ///
    /// ' This sample demonstrates enumerate all methods in a ManagementClass object.
    /// Class Sample_MethodDataCollection
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim diskClass As New ManagementClass("win32_logicaldisk")
    ///         Dim diskMethods As MethodDataCollection = diskClass.Methods
    ///         Dim method As MethodData
    ///         For Each method In diskMethods
    ///             Console.WriteLine("Method = " &amp; method.Name)
    ///         Next method
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class MethodDataCollection : ICollection, IEnumerable
    {
        private readonly ManagementObject parent;

        private sealed class enumLock
        {
        } //used to lock usage of BeginMethodEnum/NextMethod

        internal MethodDataCollection(ManagementObject parent) : base()
        {
            this.parent = parent;
        }

        //
        //ICollection
        //

        /// <summary>
        /// <para>Represents the number of objects in the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </summary>
        /// <value>
        /// <para> The number of objects in the <see cref='System.Management.MethodDataCollection'/>. </para>
        /// </value>
        public int Count
        {
            get
            {
                int i = 0;
                IWbemClassObjectFreeThreaded inParameters = null, outParameters = null;
                string methodName;
                int status = (int)ManagementStatus.Failed;

                lock (typeof(enumLock))
                {
                    try
                    {
                        status = parent.wbemObject.BeginMethodEnumeration_(0);

                        if (status >= 0)
                        {
                            methodName = "";    // Condition primer to branch into the while loop.
                            while (methodName != null && status >= 0 && status != (int)tag_WBEMSTATUS.WBEM_S_NO_MORE_DATA)
                            {
                                methodName = null; inParameters = null; outParameters = null;
                                status = parent.wbemObject.NextMethod_(0, out methodName, out inParameters, out outParameters);
                                if (status >= 0 && status != (int)tag_WBEMSTATUS.WBEM_S_NO_MORE_DATA)
                                    i++;
                            }
                            parent.wbemObject.EndMethodEnumeration_();  // Ignore status.
                        }
                    }
                    catch (COMException e)
                    {
                        ManagementException.ThrowWithExtendedInfo(e);
                    }
                } // lock

                if ((status & 0xfffff000) == 0x80041000)
                {
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                }
                else if ((status & 0x80000000) != 0)
                {
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }

                return i;
            }
        }

        /// <summary>
        ///    <para>Indicates whether the object is synchronized.</para>
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
        ///    <para>Represents the object to be used for synchronization.</para>
        /// </summary>
        /// <value>
        ///    <para>The object to be used for synchronization.</para>
        /// </value>
        public object SyncRoot
        {
            get { return this; }
        }

        /// <overload>
        /// <para>Copies the <see cref='System.Management.MethodDataCollection'/> into an array.</para>
        /// </overload>
        /// <summary>
        /// <para> Copies the <see cref='System.Management.MethodDataCollection'/> into an array.</para>
        /// </summary>
        /// <param name='array'>The array to which to copy the collection. </param>
        /// <param name='index'>The index from which to start. </param>
        public void CopyTo(Array array, int index)
        {
            //Use an enumerator to get the MethodData objects and attach them into the target array
            foreach (MethodData m in this)
                array.SetValue(m, index++);
        }

        /// <summary>
        /// <para>Copies the <see cref='System.Management.MethodDataCollection'/> to a specialized <see cref='System.Management.MethodData'/>
        /// array.</para>
        /// </summary>
        /// <param name='methodArray'>The destination array to which to copy the <see cref='System.Management.MethodData'/> objects.</param>
        /// <param name=' index'>The index in the destination array from which to start the copy.</param>
        public void CopyTo(MethodData[] methodArray, int index)
        {
            CopyTo((Array)methodArray, index);
        }

        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)(new MethodDataEnumerator(parent));
        }

        /// <summary>
        /// <para>Returns an enumerator for the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </summary>
        /// <remarks>
        ///    <para> Each call to this method
        ///       returns a new enumerator on the collection. Multiple enumerators can be obtained
        ///       for the same method collection. However, each enumerator takes a snapshot
        ///       of the collection, so changes made to the collection after the enumerator was
        ///       obtained are not reflected.</para>
        /// </remarks>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> to enumerate through the collection.</returns>
        public MethodDataEnumerator GetEnumerator()
        {
            return new MethodDataEnumerator(parent);
        }

        //Enumerator class
        /// <summary>
        /// <para>Represents the enumerator for <see cref='System.Management.MethodData'/>
        /// objects in the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>using System;
        /// using System.Management;
        ///
        /// // This sample demonstrates how to enumerate all methods in
        /// // Win32_LogicalDisk class using MethodDataEnumerator object.
        ///
        /// class Sample_MethodDataEnumerator
        /// {
        ///  public static int Main(string[] args)
        ///  {
        ///   ManagementClass diskClass = new ManagementClass("win32_logicaldisk");
        ///   MethodDataCollection.MethodDataEnumerator diskEnumerator =
        ///    diskClass.Methods.GetEnumerator();
        ///   while(diskEnumerator.MoveNext())
        ///   {
        ///    MethodData method = diskEnumerator.Current;
        ///    Console.WriteLine("Method = " + method.Name);
        ///   }
        ///   return 0;
        ///  }
        /// }
        ///    </code>
        ///    <code lang='VB'>Imports System
        /// Imports System.Management
        ///
        /// ' This sample demonstrates how to enumerate all methods in
        /// ' Win32_LogicalDisk class using MethodDataEnumerator object.
        ///
        /// Class Sample_MethodDataEnumerator
        ///  Overloads Public Shared Function Main(args() As String) As Integer
        ///   Dim diskClass As New ManagementClass("win32_logicaldisk")
        ///   Dim diskEnumerator As _
        ///        MethodDataCollection.MethodDataEnumerator = _
        ///       diskClass.Methods.GetEnumerator()
        ///   While diskEnumerator.MoveNext()
        ///    Dim method As MethodData = diskEnumerator.Current
        ///    Console.WriteLine("Method = " &amp; method.Name)
        ///   End While
        ///   Return 0
        ///  End Function
        /// End Class
        ///    </code>
        /// </example>
        public class MethodDataEnumerator : IEnumerator
        {
            private readonly ManagementObject parent;
            private readonly ArrayList methodNames; //can't use simple array because we don't know the size...
            private readonly IEnumerator en;

            //Internal constructor
            //Because WMI doesn't provide a "GetMethodNames" for methods similar to "GetNames" for properties,
            //We have to walk the methods list and cache the names here.
            //We lock to ensure that another thread doesn't interfere in the Begin/Next sequence.
            internal MethodDataEnumerator(ManagementObject parent)
            {
                this.parent = parent;
                methodNames = new ArrayList();
                IWbemClassObjectFreeThreaded inP = null, outP = null;
                string tempMethodName;
                int status = (int)ManagementStatus.Failed;

                lock (typeof(enumLock))
                {
                    try
                    {
                        status = parent.wbemObject.BeginMethodEnumeration_(0);

                        if (status >= 0)
                        {
                            tempMethodName = "";    // Condition primer to branch into the while loop.
                            while (tempMethodName != null && status >= 0 && status != (int)tag_WBEMSTATUS.WBEM_S_NO_MORE_DATA)
                            {
                                tempMethodName = null;
                                status = parent.wbemObject.NextMethod_(0, out tempMethodName, out inP, out outP);
                                if (status >= 0 && status != (int)tag_WBEMSTATUS.WBEM_S_NO_MORE_DATA)
                                    methodNames.Add(tempMethodName);
                            }
                            parent.wbemObject.EndMethodEnumeration_();  // Ignore status.
                        }
                    }
                    catch (COMException e)
                    {
                        ManagementException.ThrowWithExtendedInfo(e);
                    }
                    en = methodNames.GetEnumerator();
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

            /// <internalonly/>
            object IEnumerator.Current { get { return (object)this.Current; } }

            /// <summary>
            /// <para>Returns the current <see cref='System.Management.MethodData'/> in the <see cref='System.Management.MethodDataCollection'/>
            /// enumeration.</para>
            /// </summary>
            /// <value>The current <see cref='System.Management.MethodData'/> item in the collection.</value>
            public MethodData Current
            {
                get
                {
                    return new MethodData(parent, (string)en.Current);
                }
            }

            /// <summary>
            /// <para>Moves to the next element in the <see cref='System.Management.MethodDataCollection'/> enumeration.</para>
            /// </summary>
            /// <returns><see langword='true'/> if the enumerator was successfully advanced to the next method; <see langword='false'/> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                return en.MoveNext();
            }

            /// <summary>
            /// <para>Resets the enumerator to the beginning of the <see cref='System.Management.MethodDataCollection'/> enumeration.</para>
            /// </summary>
            public void Reset()
            {
                en.Reset();
            }

        }//MethodDataEnumerator


        //
        //Methods
        //

        /// <summary>
        /// <para>Returns the specified <see cref='System.Management.MethodData'/> from the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method requested.</param>
        /// <value>A <see cref='System.Management.MethodData'/> instance containing all information about the specified method.</value>
        public virtual MethodData this[string methodName]
        {
            get
            {
                if (null == methodName)
                    throw new ArgumentNullException(nameof(methodName));

                return new MethodData(parent, methodName);
            }
        }


        /// <summary>
        /// <para>Removes a <see cref='System.Management.MethodData'/> from the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method to remove from the collection.</param>
        /// <remarks>
        ///    <para>
        ///       Removing <see cref='System.Management.MethodData'/> objects from the <see cref='System.Management.MethodDataCollection'/>
        ///       can only be done when the class has no
        ///       instances. Any other case will result in an exception.</para>
        /// </remarks>
        public virtual void Remove(string methodName)
        {
            if (parent.GetType() == typeof(ManagementObject)) //can't remove methods from instance
                throw new InvalidOperationException();

            int status = (int)ManagementStatus.Failed;

            try
            {
                status = parent.wbemObject.DeleteMethod_(methodName);
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

        //This variant takes only a method name and assumes a void method with no in/out parameters
        /// <overload>
        /// <para>Adds a <see cref='System.Management.MethodData'/> to the <see cref='System.Management.MethodDataCollection'/>.</para>
        /// </overload>
        /// <summary>
        /// <para>Adds a <see cref='System.Management.MethodData'/> to the <see cref='System.Management.MethodDataCollection'/>. This overload will
        ///    add a new method with no parameters to the collection.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method to add.</param>
        /// <remarks>
        /// <para> Adding <see cref='System.Management.MethodData'/> objects to the <see cref='System.Management.MethodDataCollection'/> can only
        ///    be done when the class has no instances. Any other case will result in an
        ///    exception.</para>
        /// </remarks>
        public virtual void Add(string methodName)
        {
            Add(methodName, null, null);
        }



        //This variant takes the full information, i.e. the method name and in & out param objects
        /// <summary>
        /// <para>Adds a <see cref='System.Management.MethodData'/> to the <see cref='System.Management.MethodDataCollection'/>. This overload will add a new method with the
        ///    specified parameter objects to the collection.</para>
        /// </summary>
        /// <param name='methodName'>The name of the method to add.</param>
        /// <param name=' inParameters'>The <see cref='System.Management.ManagementBaseObject'/> holding the input parameters to the method.</param>
        /// <param name=' outParameters'>The <see cref='System.Management.ManagementBaseObject'/> holding the output parameters to the method.</param>
        /// <remarks>
        /// <para> Adding <see cref='System.Management.MethodData'/> objects to the <see cref='System.Management.MethodDataCollection'/> can only be
        ///    done when the class has no instances. Any other case will result in an
        ///    exception.</para>
        /// </remarks>
        public virtual void Add(string methodName, ManagementBaseObject inParameters, ManagementBaseObject outParameters)
        {
            IWbemClassObjectFreeThreaded wbemIn = null, wbemOut = null;

            if (parent.GetType() == typeof(ManagementObject)) //can't add methods to instance
                throw new InvalidOperationException();

            if (inParameters != null)
                wbemIn = inParameters.wbemObject;
            if (outParameters != null)
                wbemOut = outParameters.wbemObject;

            int status = (int)ManagementStatus.Failed;

            try
            {
                status = parent.wbemObject.PutMethod_(methodName, 0, wbemIn, wbemOut);
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

    }//MethodDataCollection
}
