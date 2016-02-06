// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.IO;

namespace System.Net
{
    /// <devdoc>
    ///    <para>
    ///       A
    ///       response from a Uniform Resource Indentifier (Uri). This is an abstract class.
    ///    </para>
    /// </devdoc>
    public abstract class WebResponse : IDisposable
    {
        /// <devdoc>
        ///    <para>Initializes a new
        ///       instance of the <see cref='System.Net.WebResponse'/>
        ///       class.</para>
        /// </devdoc>
        protected WebResponse()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }


        /// <devdoc>
        ///    <para>When overridden in a derived class, gets or
        ///       sets
        ///       the content length of data being received.</para>
        /// </devdoc>
        public abstract long ContentLength
        {
            get;
        }


        /// <devdoc>
        ///    <para>When overridden in a derived class,
        ///       gets
        ///       or sets the content type of the data being received.</para>
        /// </devdoc>
        public abstract string ContentType
        {
            get;
        }

        /// <devdoc>
        /// <para>When overridden in a derived class, returns the <see cref='System.IO.Stream'/> object used
        ///    for reading data from the resource referenced in the <see cref='System.Net.WebRequest'/>
        ///    object.</para>
        /// </devdoc>
        public abstract Stream GetResponseStream();


        /// <devdoc>
        ///    <para>When overridden in a derived class, gets the Uri that
        ///       actually responded to the request.</para>
        /// </devdoc>
        public abstract Uri ResponseUri
        {
            get;
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
                Contract.Ensures(Contract.Result<WebHeaderCollection>() != null);
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
