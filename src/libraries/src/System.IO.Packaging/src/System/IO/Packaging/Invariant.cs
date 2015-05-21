// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//---------------------------------------------------------------------------
//
// File: Invariant.cs
//
// Description: Provides methods that assert an application is in a valid state.
//
//---------------------------------------------------------------------------

namespace System.IO.Packaging
{
    using System;
    using System.Security;
    using System.Diagnostics;

    /// <summary>
    /// Provides methods that assert an application is in a valid state. 
    /// </summary>
    internal // DO NOT MAKE PUBLIC - See security notes on Assert
        static class Invariant
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Static ctor.  Initializes the Strict property.
        /// </summary>
        ///<SecurityNote>
        /// Critical - this function elevates to read from the registry. 
        /// TreatAsSafe - Not controllable from external input. 
        ///               The information stored indicates whether invariant behavior is "strict" or not. Considered safe. 
        ///</SecurityNote>
        // [SecurityCritical, SecurityTreatAsSafe] todo ew
        // [SecurityCritical]
        static Invariant()
        {
            s_strict = _strictDefaultValue;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        /// <SecurityNote>
        ///     Critical: This code will close the current process 
        ///     TreatAsSafe: This code is safe to call.
        ///                  Note that if this code were ever to become public,
        ///                  we have a potential denial-of-service vulnerability.
        ///                  Passing in false shuts down the process, even in
        ///                  partial trust.  However, not shutting down in
        ///                  partial trust is even worse: by definition a false condition
        ///                  means we've hit a bug in avalon code and we cannot safely
        ///                  continue.
        /// </SecurityNote>
        // [SecurityCritical, SecurityTreatAsSafe] - Removed for performance, OK so long as this class remains internal
        internal static void Assert(bool condition)
        {
            if (!condition)
            {
                FailFast(null, null);
            }
        }

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        /// <param name="invariantMessage">
        /// Message to display before shutting down the application.
        /// </param>
        /// <SecurityNote>
        ///     Critical: This code will close the current process 
        ///     TreatAsSafe: This code is safe to call.
        ///                  Note that if this code were ever to become public,
        ///                  we have a potential denial-of-service vulnerability.
        ///                  Passing in false shuts down the process, even in
        ///                  partial trust.  However, not shutting down in
        ///                  partial trust is even worse: by definition a false condition
        ///                  means we've hit a bug in avalon code and we cannot safely
        ///                  continue.
        /// </SecurityNote>
        // [SecurityCritical, SecurityTreatAsSafe] - Removed for performance, OK so long as this class remains internal
        internal static void Assert(bool condition, string invariantMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, null);
            }
        }

        /// <summary>
        /// Checks for a condition and shuts down the application if false.
        /// </summary>
        /// <param name="condition">
        /// If condition is true, does nothing.
        ///
        /// If condition is false, raises an assert dialog then shuts down the
        /// process unconditionally.
        /// </param>
        /// <param name="invariantMessage">
        /// Message to display before shutting down the application.
        /// </param>
        /// <param name="detailMessage">
        /// Additional message to display before shutting down the application.
        /// </param>
        /// <SecurityNote>
        ///     Critical: This code will close the current process 
        ///     TreatAsSafe: This code is safe to call.
        ///                  Note that if this code were ever to become public,
        ///                  we have a potential denial-of-service vulnerability.
        ///                  Passing in false shuts down the process, even in
        ///                  partial trust.  However, not shutting down in
        ///                  partial trust is even worse: by definition a false condition
        ///                  means we've hit a bug in avalon code and we cannot safely
        ///                  continue.
        /// </SecurityNote>
        // [SecurityCritical, SecurityTreatAsSafe] - Removed for performance, OK so long as this class remains internal
        internal static void Assert(bool condition, string invariantMessage, string detailMessage)
        {
            if (!condition)
            {
                FailFast(invariantMessage, detailMessage);
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Property specifying whether or not the user wants to enable expensive
        /// verification diagnostics.  The Strict property is rarely used -- only
        /// when performance profiling shows a real problem.
        /// </summary>
        ///
        // Default value is false on FRE builds, true on 
        internal static bool Strict
        {
            get { return s_strict; }

            set { s_strict = value; }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        ///     Shuts down the process immediately, with no chance for additional
        ///     code to run.
        /// 
        ///     In debug we raise a Debug.Assert dialog before shutting down.
        /// </summary>
        /// <param name="message">
        ///     Message to display before shutting down the application.
        /// </param>
        /// <param name="detailMessage">
        ///     Additional message to display before shutting down the application.
        /// </param>
        /// <SecurityNote>
        ///     Critical: This code will close the current process.
        ///     TreatAsSafe: This code is safe to call.
        ///         Note that if this code were made to be callable publicly,
        ///         we would have a potential denial-of-service vulnerability.
        /// </SecurityNote>
        // [SecurityCritical, SecurityTreatAsSafe] todo ew
        // [SecurityCritical]
        private // DO NOT MAKE PUBLIC OR INTERNAL -- See security note
            static void FailFast(string message, string detailMessage)
        {
            Debug.Assert(false, string.Format("Invariant failure: {0}\n{1}", message, detailMessage));

            Environment.FailFast(SR.Get(SRID.InvariantFailure));
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Property specifying whether or not the user wants to enable expensive
        // verification diagnostics.
        ///<SecurityNote>
        /// Critical - this data member required elevated permissions to be set. 
        /// TreatAsSafe - this data indicates whether "strict" invariant mode is to be used. Considered safe
        ///</SecurityNote> 
        // [SecurityCritical, SecurityTreatAsSafe] ew todo
        // [SecurityCritical]
        private static bool s_strict;

        // Used to initialize the default value of _strict in the static ctor.
        private const bool _strictDefaultValue
#if DEBUG
            = true;     // Enable strict asserts by default on CHK builds.
#else
            = false;
#endif

        #endregion Private Fields
    }
}

