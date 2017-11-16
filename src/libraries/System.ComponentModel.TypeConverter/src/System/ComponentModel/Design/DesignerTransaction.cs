// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///     Identifies a transaction within a designer.  Transactions are
    ///     used to wrap several changes into one unit of work, which 
    ///     helps performance.
    /// </summary>
    public abstract class DesignerTransaction : IDisposable
    {
        private bool _suppressedFinalization;

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected DesignerTransaction() : this("")
        {
        }


        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected DesignerTransaction(string description)
        {
            Description = description;
        }


        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool Canceled { get; private set; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public bool Committed { get; private set; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public string Description { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        public void Cancel()
        {
            if (!Canceled && !Committed)
            {
                Canceled = true;
                GC.SuppressFinalize(this);
                _suppressedFinalization = true;
                OnCancel();
            }
        }

        /// <summary>
        ///     Commits this transaction.  Once a transaction has
        ///     been committed, further calls to this method
        ///     will do nothing.  You should always call this
        ///     method after creating a transaction to ensure
        ///     that the transaction is closed properly.
        /// </summary>
        public void Commit()
        {
            if (!Committed && !Canceled)
            {
                Committed = true;
                GC.SuppressFinalize(this);
                _suppressedFinalization = true;
                OnCommit();
            }
        }

        /// <summary>
        ///     User code should implement this method to perform
        ///     the actual work of committing a transaction.
        /// </summary>
        protected abstract void OnCancel();

        /// <summary>
        ///     User code should implement this method to perform
        ///     the actual work of committing a transaction.
        /// </summary>
        protected abstract void OnCommit();

        /// <summary>
        ///     Overrides Object to commit this transaction
        ///     in case the user forgot.
        /// </summary>
        ~DesignerTransaction()
        {
            Dispose(false);
        }

        /// <internalonly/>
        /// <summary>
        /// Private implementation of IDisaposable.
        /// When a transaction is disposed it is
        /// committed.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);

            // note - Dispose calls Cancel which sets this bit, so
            //        this should never be hit.
            //
            if (!_suppressedFinalization)
            {
                System.Diagnostics.Debug.Fail("Invalid state. Dispose(true) should have called cancel which does the SuppressFinalize");
                GC.SuppressFinalize(this);
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            System.Diagnostics.Debug.Assert(disposing, "Designer transaction garbage collected, unable to cancel, please Cancel, Close, or Dispose your transaction.");
            System.Diagnostics.Debug.Assert(disposing || Canceled || Committed, "Disposing DesignerTransaction that has not been comitted or canceled; forcing Cancel");
            Cancel();
        }
    }
}

