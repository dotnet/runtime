//------------------------------------------------------------------------------
// <copyright file="PropertyCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace System.DirectoryServices {

    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Diagnostics;
    using System.DirectoryServices.Interop;
    using System.Security.Permissions;
    using System.Globalization;

    /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection"]/*' />
    /// <devdoc>
    /// <para>Contains the properties on a <see cref='System.DirectoryServices.DirectoryEntry'/>.</para>
    /// </devdoc>
    [
        DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted=true)
    ]
    public class PropertyCollection : IDictionary {

        private DirectoryEntry entry;
        internal Hashtable valueTable = null;

        internal PropertyCollection(DirectoryEntry entry) {
            this.entry = entry;            
            Hashtable tempTable = new Hashtable();
            valueTable = Hashtable.Synchronized(tempTable);
        }        

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.this"]/*' />
        /// <devdoc>
        ///    <para>Gets the property with the given name.</para>
        /// </devdoc>
        public PropertyValueCollection this[string propertyName] {
            get {    
                if(propertyName == null)
                    throw new ArgumentNullException("propertyName");
                
                string name = propertyName.ToLower(CultureInfo.InvariantCulture);
                if(valueTable.Contains(name))
                    return (PropertyValueCollection)valueTable[name];
                else
                {
                    PropertyValueCollection value = new PropertyValueCollection(entry, propertyName); 
                    valueTable.Add(name, value);
                    return value;                    
                }
                    
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Count"]/*' />
        /// <devdoc>
        ///    <para>Gets the number of properties available on this entry.</para>
        /// </devdoc>
        public int Count {
            get {
                if (!(entry.AdsObject is UnsafeNativeMethods.IAdsPropertyList))
                    throw new NotSupportedException(Res.GetString(Res.DSCannotCount));

                entry.FillCache("");

                UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList) entry.AdsObject;

                return propList.PropertyCount;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyNames"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection PropertyNames {
            get {
                return new KeysCollection(this);
            }
        }
                        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Values"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ICollection Values {
            get {
                return new ValuesCollection(this);
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.Contains"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(string propertyName) {       
            
            //entry.FillCache(propertyName);
            object var;
            int unmanagedResult = entry.AdsObject.GetEx(propertyName, out var);            
            if(unmanagedResult != 0)
            {
                //  property not found (IIS provider returns 0x80005006, other provides return 0x8000500D).
                if((unmanagedResult == unchecked((int)0x8000500D)) || (unmanagedResult == unchecked((int)0x80005006)))
                {
                    return false;
                }
                else
                {
                    throw COMExceptionHelper.CreateFormattedComException(unmanagedResult);
                }
                
            }
            
            return true;
        }
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.CopyTo"]/*' />
        /// <devdoc>
        /// <para>Copies the elements of this instance into an <see cref='System.Array'/>, starting at a particular index into the array.</para>
        /// </devdoc>
        public void CopyTo(PropertyValueCollection[] array, int index) {
            ((ICollection)this).CopyTo((Array)array, index);
        }
       
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.GetEnumerator"]/*' />
        /// <devdoc>
        ///    <para>Returns an enumerator, which can be used to iterate through the collection.</para>
        /// </devdoc>
        public IDictionaryEnumerator GetEnumerator() {
            if (!(entry.AdsObject is UnsafeNativeMethods.IAdsPropertyList))
                throw new NotSupportedException(Res.GetString(Res.DSCannotEmunerate));

            // Once an object has been used for an enumerator once, it can't be used again, because it only
            // maintains a single cursor. Re-bind to the ADSI object to get a new instance.
            // That's why we must clone entry here. It will be automatically disposed inside Enumerator.
            DirectoryEntry entryToUse = entry.CloneBrowsable(); 
            entryToUse.FillCache("");

            UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList) entryToUse.AdsObject;

            entryToUse.propertiesAlreadyEnumerated = true;
            return new PropertyEnumerator(entry, entryToUse);
        }
             
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.this"]/*' />
        ///<internalonly/>
        object IDictionary.this[object key] {
            get {
                return this[(string)key];
            }     
            
            set {
                throw new NotSupportedException(Res.GetString(Res.DSPropertySetSupported));
            }                               
        }
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.IsFixedSize"]/*' />
        ///<internalonly/>
        bool IDictionary.IsFixedSize {
            get {
                return true;
            }
        } 
                                        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.IsReadOnly"]/*' />
        ///<internalonly/>
        bool IDictionary.IsReadOnly {
            get {
                return true;
            }
        }
                        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Keys"]/*' />
        ///<internalonly/>
        ICollection IDictionary.Keys {
            get {
                return new KeysCollection(this);
            }
        }                                   
            
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Add"]/*' />
        ///<internalonly/>
        void IDictionary.Add(object key, object value) {
            throw new NotSupportedException(Res.GetString(Res.DSAddNotSupported));
        }                            
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Clear"]/*' />
        ///<internalonly/>
        void IDictionary.Clear() {
            throw new NotSupportedException(Res.GetString(Res.DSClearNotSupported));
        }
                                              
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Contains"]/*' />
        ///<internalonly/>
        bool IDictionary.Contains(object value) {
            return this.Contains((string)value);
        }        
                
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IDictionary.Remove"]/*' />
        ///<internalonly/>
        void IDictionary.Remove(object key) {
            throw new NotSupportedException(Res.GetString(Res.DSRemoveNotSupported));
        }                            
                                                                             
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.IEnumerable.GetEnumerator"]/*' />
        ///<internalonly/>
        IEnumerator IEnumerable.GetEnumerator() {
            return (IEnumerator) GetEnumerator();
        }        
                            
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.IsSynchronized"]/*' />
        ///<internalonly/>
        bool ICollection.IsSynchronized {
            get {
                return false;
            }
        }
                
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.SyncRoot"]/*' />
        ///<internalonly/>
        object ICollection.SyncRoot {
            get {
                return this;
            }
        }
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ICollection.CopyTo"]/*' />
        ///<internalonly/>
        void ICollection.CopyTo(Array array, Int32 index) {
            if(array == null)
                throw new ArgumentNullException("array");

            if(array.Rank != 1)
                throw new ArgumentException(Res.GetString(Res.OnlyAllowSingleDimension), "array");

            if(index < 0)
                throw new ArgumentOutOfRangeException(Res.GetString(Res.LessThanZero), "index");            

            if(((index + Count) > array.Length) || ((index + Count) < index))
                throw new ArgumentException(Res.GetString(Res.DestinationArrayNotLargeEnough));
            
            foreach(PropertyValueCollection value in this) {
                array.SetValue(value, index);
                index++;
            }
        }
               
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator"]/*' />
        ///<internalonly/>
        private class PropertyEnumerator : IDictionaryEnumerator, IDisposable {
            private DirectoryEntry entry;               // clone (to be disposed)
            private DirectoryEntry parentEntry;         // original entry to pass to PropertyValueCollection
            private string currentPropName = null;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.PropertyEnumerator"]/*' />
            ///<internalonly/>
            public PropertyEnumerator(DirectoryEntry parent, DirectoryEntry clone) {
                this.entry = clone;
                this.parentEntry = parent;
            }
            
            ~PropertyEnumerator() {
                Dispose(true);      // finalizer is called => Dispose has not been called yet.
            }
            
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyEnumerator.Dispose"]/*' />
            /// <devdoc>        
            /// </devdoc>
            public void Dispose() {
                Dispose(true);
                GC.SuppressFinalize(this);            
            }                
        
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyEnumerator.Dispose1"]/*' />
            /// <devdoc>        
            /// </devdoc>
            protected virtual void Dispose(bool disposing) {            
                if (disposing) {
                    entry.Dispose();
                }                    
            }
        
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Current"]/*' />
            ///<internalonly/>
            public object Current {
                get {
                    return Entry.Value;
                }
            }
             
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Entry"]/*' />
            ///<internalonly/>
            public DictionaryEntry Entry {
                get {
                    if (currentPropName == null)
                        throw new InvalidOperationException(Res.GetString(Res.DSNoCurrentProperty));
                
                    return new DictionaryEntry(currentPropName, new PropertyValueCollection(parentEntry, currentPropName));
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Key"]/*' />
            ///<internalonly/>
            public object Key {
                get {
                    return Entry.Key;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Value"]/*' />
            ///<internalonly/>
            public object Value {
                get {
                    return Entry.Value;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.MoveNext"]/*' />
            ///<internalonly/>
            public bool MoveNext() {
                object prop;
                int hr = 0;
                try {
                    hr = ((UnsafeNativeMethods.IAdsPropertyList) entry.AdsObject).Next(out prop);
                }
                catch (COMException e) {                    
                    hr = e.ErrorCode;
                    prop = null;
                }
                if (hr == 0) {
                    if (prop != null)
                        currentPropName = ((UnsafeNativeMethods.IAdsPropertyEntry) prop).Name;
                    else
                        currentPropName = null;
                                                                    
                    return true;
                }
                else {
					currentPropName = null;
                    return false;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.PropertyEnumerator.Reset"]/*' />
            ///<internalonly/>
            public void Reset() {
                ((UnsafeNativeMethods.IAdsPropertyList) entry.AdsObject).Reset();
                currentPropName = null;
            }
        }        
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection"]/*' />
        ///<internalonly/>
        private class ValuesCollection : ICollection {
            protected PropertyCollection props;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.ValuesCollection"]/*' />
            ///<internalonly/>
            public ValuesCollection(PropertyCollection props) {
                this.props = props;
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.Count"]/*' />
            ///<internalonly/>
            public int Count {
                get {
                    return props.Count;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.IsReadOnly"]/*' />
            ///<internalonly/>
            public bool IsReadOnly {
                get {
                    return true;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.IsSynchronized"]/*' />
            ///<internalonly/>
            public bool IsSynchronized {
                get {
                    return false;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.SyncRoot"]/*' />
            ///<internalonly/>
            public object SyncRoot {
                get {
                    return ((ICollection)props).SyncRoot;
                }
            }
            
            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.CopyTo"]/*' />
            ///<internalonly/>
            public void CopyTo(Array array, int index) {
                foreach (object value in this)
                    array.SetValue(value, index++);
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesCollection.GetEnumerator"]/*' />
            ///<internalonly/>
            public virtual IEnumerator GetEnumerator() {
                return new ValuesEnumerator(props);
            }

        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection"]/*' />
        ///<internalonly/>   
        private class KeysCollection : ValuesCollection {           

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection.KeysCollection"]/*' />
            ///<internalonly/>
            public KeysCollection(PropertyCollection props) 
            : base(props){              
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysCollection.GetEnumerator"]/*' />
            ///<internalonly/>
            public override IEnumerator GetEnumerator() {
                props.entry.FillCache("");
                return new KeysEnumerator(props);
            }

        }
        
        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator"]/*' />
        ///<internalonly/>
        private class ValuesEnumerator : IEnumerator {
            private int currentIndex = -1;
            protected PropertyCollection propCollection;

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.ValuesEnumerator"]/*' />
            ///<internalonly/>
            public ValuesEnumerator(PropertyCollection propCollection) {
                this.propCollection = propCollection;
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.CurrentIndex"]/*' />
            ///<internalonly/>
            protected int CurrentIndex {
                get {
                    if (currentIndex == -1)
                        throw new InvalidOperationException(Res.GetString(Res.DSNoCurrentValue));
                    return currentIndex;
                }
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.Current"]/*' />
            ///<internalonly/>
            public virtual object Current {
                get {

                    UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList) propCollection.entry.AdsObject;                    
                    return propCollection[((UnsafeNativeMethods.IAdsPropertyEntry) propList.Item(CurrentIndex)).Name];                
                }
            }            

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.MoveNext"]/*' />
            ///<internalonly/>
            public bool MoveNext() {
                currentIndex++;
                if (currentIndex >= propCollection.Count) {
                    currentIndex = -1;
                    return false;
                }
                else
                    return true;
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.ValuesEnumerator.Reset"]/*' />
            ///<internalonly/>
            public void Reset() {
                currentIndex = -1;
            }
        }

        /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator"]/*' />
        ///<internalonly/>
        private class KeysEnumerator : ValuesEnumerator {

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator.KeysEnumerator"]/*' />
            ///<internalonly/>
            public KeysEnumerator(PropertyCollection collection) 
            : base(collection) {
            }

            /// <include file='doc\PropertyCollection.uex' path='docs/doc[@for="PropertyCollection.KeysEnumerator.Current"]/*' />
            ///<internalonly/>
            public override object Current {
                get {                
                    UnsafeNativeMethods.IAdsPropertyList propList = (UnsafeNativeMethods.IAdsPropertyList) propCollection.entry.AdsObject;

                    return ((UnsafeNativeMethods.IAdsPropertyEntry) propList.Item(CurrentIndex)).Name;                
                }
            }
        }
    }
}

