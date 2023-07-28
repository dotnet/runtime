// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Management
{
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    /// <summary>
    ///    <para> Represents a collection of named values
    ///       suitable for use as context information to WMI operations. The
    ///       names are case-insensitive.</para>
    /// </summary>
    //CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC//
    public class ManagementNamedValueCollection : NameObjectCollectionBase
    {
        // Notification of when the content of this collection changes
        internal event IdentifierChangedEventHandler IdentifierChanged;

        //Fires IdentifierChanged event
        private void FireIdentifierChanged()
        {
            IdentifierChanged?.Invoke(this, null);
        }

        //default constructor
        /// <overload>
        ///    Initializes a new instance
        ///    of the <see cref='System.Management.ManagementNamedValueCollection'/> class.
        /// </overload>
        /// <summary>
        /// <para> Initializes a new instance of the <see cref='System.Management.ManagementNamedValueCollection'/> class, which is empty. This is
        ///    the default constructor.</para>
        /// </summary>
        public ManagementNamedValueCollection()
        {
        }


        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.Management.ManagementNamedValueCollection'/> class that is serializable
        ///    and uses the specified <see cref='System.Runtime.Serialization.SerializationInfo'/>
        ///    and <see cref='System.Runtime.Serialization.StreamingContext'/>.</para>
        /// </summary>
        /// <param name='info'>The <see cref='System.Runtime.Serialization.SerializationInfo'/> to populate with data.</param>
        /// <param name='context'>The destination (see <see cref='System.Runtime.Serialization.StreamingContext'/> ) for this serialization.</param>
#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected ManagementNamedValueCollection(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        ///    <para>Internal method to return an IWbemContext representation
        ///    of the named value collection.</para>
        /// </summary>
        internal IWbemContext GetContext()
        {
            IWbemContext wbemContext = null;

            // Only build a context if we have something to put in it
            if (0 < Count)
            {
                int status = (int)ManagementStatus.NoError;

                try
                {
                    wbemContext = (IWbemContext)new WbemContext();

                    foreach (string name in this)
                    {
                        object val = base.BaseGet(name);
                        status = wbemContext.SetValue_(name, 0, ref val);
                        if ((status & 0x80000000) != 0)
                        {
                            break;
                        }
                    }
                }
                catch { }
            }

            return wbemContext;
        }

        /// <summary>
        ///    <para> Adds a single-named value to the collection.</para>
        /// </summary>
        /// <param name=' name'>The name of the new value.</param>
        /// <param name=' value'>The value to be associated with the name.</param>
        public void Add(string name, object value)
        {
            // Remove any old entry
            try
            {
                base.BaseRemove(name);
            }
            catch { }

            base.BaseAdd(name, value);
            FireIdentifierChanged();
        }

        /// <summary>
        ///    <para> Removes a single-named value from the collection.
        ///       If the collection does not contain an element with the
        ///       specified name, the collection remains unchanged and no
        ///       exception is thrown.</para>
        /// </summary>
        /// <param name=' name'>The name of the value to be removed.</param>
        public void Remove(string name)
        {
            base.BaseRemove(name);
            FireIdentifierChanged();
        }

        /// <summary>
        ///    <para>Removes all entries from the collection.</para>
        /// </summary>
        public void RemoveAll()
        {
            base.BaseClear();
            FireIdentifierChanged();
        }

        /// <summary>
        ///    <para>Creates a clone of the collection. Individual values
        ///       are cloned. If a value does not support cloning, then a <see cref='System.NotSupportedException'/>
        ///       is thrown. </para>
        /// </summary>
        /// <returns>
        ///    The new copy of the collection.
        /// </returns>
        public ManagementNamedValueCollection Clone()
        {
            ManagementNamedValueCollection nvc = new ManagementNamedValueCollection();

            foreach (string name in this)
            {
                // If we can clone the value, do so. Otherwise throw.
                object val = base.BaseGet(name);

                if (null != val)
                {
                    Type valueType = val.GetType();

                    if (valueType.IsByRef)
                    {
                        try
                        {
                            object clonedValue = ((ICloneable)val).Clone();
                            nvc.Add(name, clonedValue);
                        }
                        catch
                        {
                            throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        nvc.Add(name, val);
                    }
                }
                else
                {
                    nvc.Add(name, null);
                }
            }

            return nvc;
        }

        /// <summary>
        ///    <para>Returns the value associated with the specified name from this collection.</para>
        /// </summary>
        /// <param name=' name'>The name of the value to be returned.</param>
        /// <value>
        /// <para>An <see cref='object'/> containing the
        ///    value of the specified item in this collection.</para>
        /// </value>
        public object this[string name]
        {
            get
            {
                return base.BaseGet(name);
            }
        }
    }

}
