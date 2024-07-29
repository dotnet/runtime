// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       A
    ///       response from a Uniform Resource Identifier (Uri). This is an abstract class.
    ///    </para>
    /// </devdoc>
    public abstract class WebResponse : MarshalByRefObject, ISerializable, IDisposable
    {
        /// <devdoc>
        ///    <para>Initializes a new
        ///       instance of the <see cref='System.Net.WebResponse'/>
        ///       class.</para>
        /// </devdoc>
        protected WebResponse()
        {
        }

        [Obsolete("Serialization has been deprecated for WebResponse.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected WebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for WebResponse.")]
        void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        [Obsolete("Serialization has been deprecated for WebResponse.")]
        protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual void Close()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Close();
                }
                catch { }
            }
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, gets or
        ///       sets
        ///       the content length of data being received.</para>
        /// </devdoc>
        public virtual long ContentLength
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }


        /// <devdoc>
        ///    <para>When overridden in a derived class,
        ///       gets
        ///       or sets the content type of the data being received.</para>
        /// </devdoc>
        public virtual string ContentType
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        public virtual bool IsFromCache => false;

        public virtual bool IsMutuallyAuthenticated => false;

        /// <devdoc>
        /// <para>When overridden in a derived class, returns the <see cref='System.IO.Stream'/> object used
        ///    for reading data from the resource referenced in the <see cref='System.Net.WebRequest'/>
        ///    object.</para>
        /// </devdoc>
        public virtual Stream GetResponseStream()
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, gets the Uri that
        ///       actually responded to the request.</para>
        /// </devdoc>
        public virtual Uri ResponseUri
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        /// <devdoc>
        ///    <para>When overridden in a derived class, gets
        ///       a collection of header name-value pairs associated with this
        ///       request.</para>
        /// </devdoc>
        public virtual WebHeaderCollection Headers
        {
            // read-only
            get
            {
                throw NotImplemented.ByDesignWithMessage(SR.net_PropertyNotImplementedException);
            }
        }

        // For portability only:
        //
        // Returning false indicates that the Headers property has not been implemented and should not be used.
        // Derived types with headers should override both Headers and SupportsHeaders.
        public virtual bool SupportsHeaders
        {
            get
            {
                return false;
            }
        }
    }
}
