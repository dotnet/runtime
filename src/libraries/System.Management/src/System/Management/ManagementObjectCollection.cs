// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC// 
    /// <summary>
    ///    <para> Represents different collections of management objects 
    ///       retrieved through WMI. The objects in this collection are of <see cref='System.Management.ManagementBaseObject'/>-derived types, including <see cref='System.Management.ManagementObject'/> and <see cref='System.Management.ManagementClass'/>
    ///       .</para>
    ///    <para> The collection can be the result of a WMI 
    ///       query executed through a <see cref='System.Management.ManagementObjectSearcher'/> object, or an enumeration of
    ///       management objects of a specified type retrieved through a <see cref='System.Management.ManagementClass'/> representing that type.
    ///       In addition, this can be a collection of management objects related in a specified
    ///       way to a specific management object - in this case the collection would
    ///       be retrieved through a method such as <see cref='System.Management.ManagementObject.GetRelated()'/>.</para>
    /// <para>The collection can be walked using the <see cref='System.Management.ManagementObjectCollection.ManagementObjectEnumerator'/> and objects in it can be inspected or 
    ///    manipulated for various management tasks.</para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>using System; 
    /// using System.Management; 
    /// 
    /// // This example demonstrates how to enumerate instances of a ManagementClass object.
    /// class Sample_ManagementObjectCollection 
    /// { 
    ///     public static int Main(string[] args) { 
    ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
    ///         ManagementObjectCollection disks = diskClass.GetInstances(); 
    ///         foreach (ManagementObject disk in disks) { 
    ///             Console.WriteLine("Disk = " + disk["deviceid"]); 
    ///         } 
    ///         return 0;
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>Imports System 
    /// Imports System.Management 
    /// 
    /// ' This example demonstrates how to enumerate instances of a ManagementClass object.
    /// Class Sample_ManagementObjectCollection 
    ///     Overloads Public Shared Function Main(args() As String) As Integer 
    ///         Dim diskClass As New ManagementClass("Win32_LogicalDisk") 
    ///         Dim disks As ManagementObjectCollection = diskClass.GetInstances()
    ///         Dim disk As ManagementObject
    ///         For Each disk In disks
    ///             Console.WriteLine("Disk = " &amp; disk("deviceid").ToString())
    ///         Next disk
    ///         Return 0
    ///     End Function
    /// End Class
    ///    </code>
    /// </example>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ManagementObjectCollection : ICollection, IEnumerable, IDisposable
    {
        private static readonly string name = typeof(ManagementObjectCollection).FullName;

        //fields
        internal ManagementScope scope;
        internal EnumerationOptions options;
        private IEnumWbemClassObject enumWbem; //holds WMI enumerator for this collection
        private bool isDisposed = false;

        //Constructor
        internal ManagementObjectCollection(
            ManagementScope scope,
            EnumerationOptions options, 
            IEnumWbemClassObject enumWbem)
        {
            if (null != options)
                this.options = (EnumerationOptions) options.Clone();
            else
                this.options = new EnumerationOptions ();

            if (null != scope)
                this.scope = (ManagementScope)scope.Clone ();
            else
                this.scope = ManagementScope._Clone(null);

            this.enumWbem = enumWbem;
        }

        /// <summary>
        /// <para>Disposes of resources the object is holding. This is the destructor for the object.</para>
        /// </summary>      
        ~ManagementObjectCollection ()
        {
            Dispose ( false );
        }

        /// <summary>
        /// Releases resources associated with this object. After this
        /// method has been called, an attempt to use this object will
        /// result in an ObjectDisposedException being thrown.
        /// </summary>
        public void Dispose ()
        {
            if (!isDisposed)
            {
                Dispose ( true ) ;
            }
        }

        private void Dispose ( bool disposing )
        {
            if ( disposing )
            {
                GC.SuppressFinalize (this);
                isDisposed = true;
            }
            Marshal.ReleaseComObject (enumWbem);
        }


        //
        //ICollection properties & methods
        //

        /// <summary>
        ///    <para>Represents the number of objects in the collection.</para>
        /// </summary>
        /// <value>
        ///    <para>The number of objects in the collection.</para>
        /// </value>
        /// <remarks>
        ///    <para>This property is very expensive - it requires that
        ///    all members of the collection be enumerated.</para>
        /// </remarks>
        public int Count 
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(name);

                //
                // We can not use foreach since it _always_ calls Dispose on the collection
                // invalidating the IEnumWbemClassObject pointers.
                // We prevent this by doing a manual walk of the collection.
                //
                int count = 0;

                IEnumerator enumCol = this.GetEnumerator ( ) ;
                while ( enumCol.MoveNext() == true )
                {
                    count++ ;
                }
                return count ;
            }
        }

        /// <summary>
        ///    <para>Represents whether the object is synchronized.</para>
        /// </summary>
        /// <value>
        /// <para><see langword='true'/>, if the object is synchronized; 
        ///    otherwise, <see langword='false'/>.</para>
        /// </value>
        public bool IsSynchronized 
        {
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(name);

                return false;
            }
        }

        /// <summary>
        ///    <para>Represents the object to be used for synchronization.</para>
        /// </summary>
        /// <value>
        ///    <para> The object to be used for synchronization.</para>
        /// </value>
        public Object SyncRoot 
        { 
            get
            {
                if (isDisposed)
                    throw new ObjectDisposedException(name);

                return this; 
            }
        }

        /// <overload>
        ///    Copies the collection to an array.
        /// </overload>
        /// <summary>
        ///    <para> Copies the collection to an array.</para>
        /// </summary>
        /// <param name='array'>An array to copy to. </param>
        /// <param name='index'>The index to start from. </param>
        public void CopyTo (Array array, Int32 index) 
        {
            if (isDisposed)
                throw new ObjectDisposedException(name);

            if (null == array)
                throw new ArgumentNullException ("array");

            if ((index < array.GetLowerBound (0)) || (index > array.GetUpperBound(0)))
                throw new ArgumentOutOfRangeException ("index");

            // Since we don't know the size until we've enumerated
            // we'll have to dump the objects in a list first then
            // try to copy them in.

            int capacity = array.Length - index;
            int numObjects = 0;
            ArrayList arrList = new ArrayList ();

            ManagementObjectEnumerator en = this.GetEnumerator();
            ManagementBaseObject obj;

            while (en.MoveNext())
            {
                obj = en.Current;

                arrList.Add(obj);
                numObjects++;

                if (numObjects > capacity)
                    throw new ArgumentException (null, "index");
            }

            // If we get here we are OK. Now copy the list to the array
            arrList.CopyTo (array, index);

            return;
        }

        /// <summary>
        /// <para>Copies the items in the collection to a <see cref='System.Management.ManagementBaseObject'/> 
        /// array.</para>
        /// </summary>
        /// <param name='objectCollection'>The target array.</param>
        /// <param name=' index'>The index to start from.</param>
        public void CopyTo (ManagementBaseObject[] objectCollection, Int32 index)
        {
            CopyTo ((Array)objectCollection, index);
        }

        //
        //IEnumerable methods
        //

        //****************************************
        //GetEnumerator
        //****************************************
        /// <summary>
        ///    <para>Returns the enumerator for the collection. If the collection was retrieved from an operation that
        /// specified the EnumerationOptions.Rewindable = false only one iteration through this enumerator is allowed.
        /// Note that this applies to using the Count property of the collection as well since an iteration over the collection
        /// is required. Due to this, code using the Count property should never specify EnumerationOptions.Rewindable = false.
        /// </para>
        /// </summary>
        /// <returns>
        ///    An <see cref='System.Collections.IEnumerator'/>that can be used to iterate through the
        ///    collection.
        /// </returns>
        public ManagementObjectEnumerator GetEnumerator()
        {
            if (isDisposed)
                throw new ObjectDisposedException(name);


            //
            // We do not clone the enumerator if its the first enumerator.
            // If it is the first enumerator we pass the reference
            // to the enumerator implementation rather than a clone. If the enumerator is used 
            // from within a foreach statement in the client code, the foreach statement will
            // dec the ref count on the reference which also happens to be the reference to the
            // original enumerator causing subsequent uses of the collection to fail.
            // To prevent this we always clone the enumerator (assuming its a rewindable enumerator)
            // to avoid invalidating the collection.
            //
            // If its a forward only enumerator we simply pass back the original enumerator (i.e.
            // not cloned) and if it gets disposed we end up throwing the next time its used. Essentially,
            // the enumerator becomes the collection.
            //
            
            // Unless this is the first enumerator, we have
            // to clone. This may throw if we are non-rewindable.
            if ( this.options.Rewindable == true )
            {
                IEnumWbemClassObject enumWbemClone = null;
                int status = (int)ManagementStatus.NoError;

                try 
                {
                    status = scope.GetSecuredIEnumWbemClassObjectHandler(enumWbem ).Clone_( ref enumWbemClone);

                    if ((status & 0x80000000) == 0)
                    {
                        //since the original enumerator might not be reset, we need
                        //to reset the new one.
                        status = scope.GetSecuredIEnumWbemClassObjectHandler(enumWbemClone ).Reset_( );
                    }
                } 
                catch (COMException e) 
                {
                    ManagementException.ThrowWithExtendedInfo (e);
                }

                if ((status & 0xfffff000) == 0x80041000)
                {
                    ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                }
                else if ((status & 0x80000000) != 0)
                {
                    Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                }
                return new ManagementObjectEnumerator (this, enumWbemClone);
            }
            else
            {
                //
                // Notice that we use the original enumerator and hence enum position is retained.
                // For example, if the client code manually walked half the collection and then
                // used a foreach statement, the foreach statement would continue from where the
                // manual walk ended.
                //
                return new ManagementObjectEnumerator(this, enumWbem);
            }
        }


        /// <internalonly/>
        /// <summary>
        ///    <para>Returns an enumerator that can iterate through a collection.</para>
        /// </summary>
        /// <returns>
        ///    An <see cref='System.Collections.IEnumerator'/> that can be used to iterate
        ///    through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator ();
        }

        

        //
        // ManagementObjectCollection methods
        //

        //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
        /// <summary>
        ///    <para>Represents the enumerator on the collection.</para>
        /// </summary>
        /// <example>
        ///    <code lang='C#'>using System; 
        /// using System.Management; 
        /// 
        /// // This example demonstrates how to enumerate all logical disks 
        /// // using the ManagementObjectEnumerator object. 
        /// class Sample_ManagementObjectEnumerator 
        /// {
        ///     public static int Main(string[] args) { 
        ///         ManagementClass diskClass = new ManagementClass("Win32_LogicalDisk");
        ///         ManagementObjectCollection disks = diskClass.GetInstances();
        ///         ManagementObjectCollection.ManagementObjectEnumerator disksEnumerator =
        ///             disks.GetEnumerator();
        ///         while(disksEnumerator.MoveNext()) { 
        ///             ManagementObject disk = (ManagementObject)disksEnumerator.Current;
        ///            Console.WriteLine("Disk found: " + disk["deviceid"]);
        ///         }
        ///         return 0;
        ///     }
        /// }
        ///    </code>
        ///    <code lang='VB'>Imports System
        ///       Imports System.Management
        ///       ' This sample demonstrates how to enumerate all logical disks
        ///       ' using ManagementObjectEnumerator object.
        ///       Class Sample_ManagementObjectEnumerator
        ///       Overloads Public Shared Function Main(args() As String) As Integer
        ///       Dim diskClass As New ManagementClass("Win32_LogicalDisk")
        ///       Dim disks As ManagementObjectCollection = diskClass.GetInstances()
        ///       Dim disksEnumerator As _
        ///       ManagementObjectCollection.ManagementObjectEnumerator = _
        ///       disks.GetEnumerator()
        ///       While disksEnumerator.MoveNext()
        ///       Dim disk As ManagementObject = _
        ///       CType(disksEnumerator.Current, ManagementObject)
        ///       Console.WriteLine("Disk found: " &amp; disk("deviceid"))
        ///       End While
        ///       Return 0
        ///       End Function
        ///       End Class
        ///    </code>
        /// </example>
        //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
        public class ManagementObjectEnumerator : IEnumerator, IDisposable
        {
            private static readonly string name = typeof(ManagementObjectEnumerator).FullName;
            private IEnumWbemClassObject enumWbem;
            private ManagementObjectCollection collectionObject;
            private uint cachedCount; //says how many objects are in the enumeration cache (when using BlockSize option)
            private int cacheIndex; //used to walk the enumeration cache
            private IWbemClassObjectFreeThreaded[] cachedObjects; //points to objects currently available in enumeration cache
            private bool atEndOfCollection;
            private bool isDisposed = false;

            //constructor
            internal ManagementObjectEnumerator(
                ManagementObjectCollection collectionObject,
                IEnumWbemClassObject enumWbem)
            {
                this.enumWbem = enumWbem;
                this.collectionObject = collectionObject;
                cachedObjects = new IWbemClassObjectFreeThreaded[collectionObject.options.BlockSize];
                cachedCount = 0; 
                cacheIndex = -1; // Reset position
                atEndOfCollection = false;
            }


            /// <summary>
            /// <para>Disposes of resources the object is holding. This is the destructor for the object.</para>
            /// </summary>      
            ~ManagementObjectEnumerator ()
            {
                Dispose ();
            }


            /// <summary>
            /// Releases resources associated with this object. After this
            /// method has been called, an attempt to use this object will
            /// result in an ObjectDisposedException being thrown.
            /// </summary>
            public void Dispose ()
            {
                if (!isDisposed)
                {
                    if (null != enumWbem)
                    {
                        Marshal.ReleaseComObject (enumWbem);
                        enumWbem = null;
                    }

                    cachedObjects = null;
                    
                    // DO NOT dispose of collectionObject.  It is merely a reference - its lifetime
                    // exceeds that of this object.  If collectionObject.Dispose was to be done here,
                    // a reference count would be needed.
                    //
                    collectionObject = null;

                    isDisposed = true;

                    GC.SuppressFinalize (this);
                }
            }

            
            /// <summary>
            /// <para>Gets the current <see cref='System.Management.ManagementBaseObject'/> that this enumerator points
            ///    to.</para>
            /// </summary>
            /// <value>
            ///    <para>The current object in the enumeration.</para>
            /// </value>
            public ManagementBaseObject Current 
            {
                get 
                {
                    if (isDisposed)
                        throw new ObjectDisposedException(name);

                    if (cacheIndex < 0)
                        throw new InvalidOperationException();

                    return ManagementBaseObject.GetBaseObject (cachedObjects[cacheIndex],
                        collectionObject.scope);
                }
            }

            /// <internalonly/>
            /// <summary>
            ///    <para>Returns the current object in the enumeration.</para>
            /// </summary>
            /// <value>
            ///    <para>The current object in the enumeration.</para>
            /// </value>
            object IEnumerator.Current 
            {
                get 
                {
                    return Current;
                }
            }

            //****************************************
            //MoveNext
            //****************************************
            /// <summary>
            ///    Indicates whether the enumerator has moved to
            ///    the next object in the enumeration.
            /// </summary>
            /// <returns>
            /// <para><see langword='true'/>, if the enumerator was 
            ///    successfully advanced to the next element; <see langword='false'/> if the enumerator has
            ///    passed the end of the collection.</para>
            /// </returns>
            public bool MoveNext ()
            {
                if (isDisposed)
                    throw new ObjectDisposedException(name);
                
                //If there are no more objects in the collection return false
                if (atEndOfCollection) 
                    return false;

                //Look for the next object
                cacheIndex++;

                if ((cachedCount - cacheIndex) == 0) //cache is empty - need to get more objects
                {

                    //If the timeout is set to infinite, need to use the WMI infinite constant
                    int timeout = (collectionObject.options.Timeout.Ticks == Int64.MaxValue) ? 
                        (int)tag_WBEM_TIMEOUT_TYPE.WBEM_INFINITE : (int)collectionObject.options.Timeout.TotalMilliseconds;

                    //Get the next [BLockSize] objects within the specified timeout
                    SecurityHandler securityHandler = collectionObject.scope.GetSecurityHandler();

                    //Because Interop doesn't support custom marshalling for arrays, we have to use
                    //the "DoNotMarshal" objects in the interop and then convert to the "FreeThreaded"
                    //counterparts afterwards.
                    IWbemClassObject_DoNotMarshal[] tempArray = new IWbemClassObject_DoNotMarshal[collectionObject.options.BlockSize];

                    int status = collectionObject.scope.GetSecuredIEnumWbemClassObjectHandler(enumWbem ).Next_(timeout, (uint)collectionObject.options.BlockSize,tempArray, ref cachedCount);

                    securityHandler.Reset();

                    if (status >= 0)
                    {
                        //Convert results and put them in cache.

                        for (int i = 0; i < cachedCount; i++)
                        {
                            cachedObjects[i] = new IWbemClassObjectFreeThreaded
                                                (
                                                    Marshal.GetIUnknownForObject(tempArray[i])
                                                );
                        }
                    }

                    if (status < 0)
                    {
                        if ((status & 0xfffff000) == 0x80041000)
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                        else
                            Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }
                    else
                    {
                        //If there was a timeout and no object can be returned we throw a timeout exception... 
                        if ((status == (int)tag_WBEMSTATUS.WBEM_S_TIMEDOUT) && (cachedCount == 0))
                            ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);

                        //If not timeout and no objects were returned - we're at the end of the collection
                        if ((status == (int)tag_WBEMSTATUS.WBEM_S_FALSE) && (cachedCount == 0))
                        {
                            atEndOfCollection = true;
                            cacheIndex--; //back to last object

                            /* This call to Dispose is being removed as per discussion with URT people and the newly supported
                             * Dispose() call in the foreach implementation itself.
                             * 
                             *                              //Release the COM object (so that the user doesn't have to)
                                                            Dispose();
                            */
                            return false;
                        }
                    }

                    cacheIndex = 0;
                }

                return true;
            }

            //****************************************
            //Reset
            //****************************************
            /// <summary>
            ///    <para>Resets the enumerator to the beginning of the collection.</para>
            /// </summary>
            public void Reset ()
            {
                if (isDisposed)
                    throw new ObjectDisposedException(name);

                //If the collection is not rewindable you can't do this
                if (!collectionObject.options.Rewindable)
                    throw new InvalidOperationException();
                else
                {
                    //Reset the WMI enumerator
                    SecurityHandler securityHandler = collectionObject.scope.GetSecurityHandler();
                    int status = (int)ManagementStatus.NoError;

                    try 
                    {
                        status = collectionObject.scope.GetSecuredIEnumWbemClassObjectHandler(enumWbem).Reset_();
                    } 
                    catch (COMException e) 
                    {
                        ManagementException.ThrowWithExtendedInfo (e);
                    } 
                    finally 
                    {
                        securityHandler.Reset ();
                    }

                    if ((status & 0xfffff000) == 0x80041000)
                    {
                        ManagementException.ThrowWithExtendedInfo((ManagementStatus)status);
                    }
                    else if ((status & 0x80000000) != 0)
                    {
                        Marshal.ThrowExceptionForHR(status, WmiNetUtilsHelper.GetErrorInfo_f());
                    }

                    //Flush the current enumeration cache
                    for (int i=(cacheIndex >= 0 ? cacheIndex : 0); i<cachedCount; i++)
                        Marshal.ReleaseComObject((IWbemClassObject_DoNotMarshal)(Marshal.GetObjectForIUnknown(cachedObjects[i]))); 

                    cachedCount = 0; 
                    cacheIndex = -1; 
                    atEndOfCollection = false;
                }
            }

        } //ManagementObjectEnumerator class

    } //ManagementObjectCollection class
}
