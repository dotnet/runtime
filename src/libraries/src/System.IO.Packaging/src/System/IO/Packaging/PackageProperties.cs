// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//-----------------------------------------------------------------------------
//
// Description:
//  This abstract class provides access to the "core properties" a Metro document.
//  The "core properties" are a subset of the standard OLE property sets
//  SummaryInformation and DocumentSummaryInformation, and include such properties
//  as Title and Subject.
//
//  There are two concrete derived classes. PackagePackageProperties represents the
//  core properties of a normal unencrypted Metro document, physically represented
//  as a Zip archive. EncryptedPackagePackageProperties represents the core properties
//  of an RM-protected Metro document, physically represented by an OLE compound
//  file containing a well-known stream in which a Metro Zip archive, encrypted
//  in its entirety, is stored.
//
//-----------------------------------------------------------------------------

using System;
using System.IO.Packaging;

namespace System.IO.Packaging
{
    /// <summary>
    /// This class provides access to the "core properties", such as Title and
    /// Subject, of an RM-protected Metro package. These properties are a subset of
    /// of the standard OLE property sets SummaryInformation and
    /// DocumentSummaryInformation.
    /// </summary>
    public abstract class PackageProperties : IDisposable
    {
        #region IDisposable

        /// <summary>
        /// Allow the object to clean up all resources it holds (both managed and
        /// unmanaged), and ensure that the resources won't be released a
        /// second time by removing it from the finalization queue.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This default implementation is provided for subclasses that do not
        /// make use of the IDisposable functionality.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        #endregion IDisposable

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        #region SummaryInformation properties

        /// <summary>
        /// The title.
        /// </summary>
        public abstract string Title
        {
            get; set;
        }

        /// <summary>
        /// The topic of the contents.
        /// </summary>
        public abstract string Subject
        {
            get; set;
        }

        /// <summary>
        /// The primary creator. The identification is environment-specific and
        /// can consist of a name, email address, employee ID, etc. It is
        /// recommended that this value be only as verbose as necessary to
        /// identify the individual.
        /// </summary>
        public abstract string Creator
        {
            get; set;
        }

        /// <summary>
        /// A delimited set of keywords to support searching and indexing. This
        /// is typically a list of terms that are not available elsewhere in the
        /// properties.
        /// </summary>
        public abstract string Keywords
        {
            get; set;
        }

        /// <summary>
        /// The description or abstract of the contents.
        /// </summary>
        public abstract string Description
        {
            get; set;
        }

        /// <summary>
        /// The user who performed the last modification. The identification is
        /// environment-specific and can consist of a name, email address,
        /// employee ID, etc. It is recommended that this value be only as
        /// verbose as necessary to identify the individual.
        /// </summary>
        public abstract string LastModifiedBy
        {
            get; set;
        }

        /// <summary>
        /// The revision number. This value indicates the number of saves or
        /// revisions. The application is responsible for updating this value
        /// after each revision.
        /// </summary>
        public abstract string Revision
        {
            get; set;
        }

        /// <summary>
        /// The date and time of the last printing.
        /// </summary>
        public abstract Nullable<DateTime> LastPrinted
        {
            get; set;
        }

        /// <summary>
        /// The creation date and time.
        /// </summary>
        public abstract Nullable<DateTime> Created
        {
            get; set;
        }

        /// <summary>
        /// The date and time of the last modification.
        /// </summary>
        public abstract Nullable<DateTime> Modified
        {
            get; set;
        }

        #endregion SummaryInformation properties

        #region DocumentSummaryInformation properties

        /// <summary>
        /// The category. This value is typically used by UI applications to create navigation
        /// controls.
        /// </summary>
        public abstract string Category
        {
            get; set;
        }

        /// <summary>
        /// A unique identifier.
        /// </summary>
        public abstract string Identifier
        {
            get; set;
        }

        /// <summary>
        /// The type of content represented, generally defined by a specific
        /// use and intended audience. Example values include "Whitepaper",
        /// "Security Bulletin", and "Exam". (This property is distinct from
        /// MIME content types as defined in RFC 2045.) 
        /// </summary>
        public abstract string ContentType
        {
            get; set;
        }

        /// <summary>
        /// The primary language of the package content. The language tag is
        /// composed of one or more parts: A primary language subtag and a
        /// (possibly empty) series of subsequent subtags, for example, "EN-US".
        /// These values MUST follow the convention specified in RFC 3066.
        /// </summary>
        public abstract string Language
        {
            get; set;
        }

        /// <summary>
        /// The version number. This value is set by the user or by the application.
        /// </summary>
        public abstract string Version
        {
            get; set;
        }

        /// <summary>
        /// The status of the content. Example values include "Draft",
        /// "Reviewed", and "Final".
        /// </summary>
        public abstract string ContentStatus
        {
            get; set;
        }

        #endregion DocumentSummaryInformation properties

        #endregion Public Properties
    }
}
