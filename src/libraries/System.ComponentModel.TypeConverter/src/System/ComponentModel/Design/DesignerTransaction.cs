// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Identifies a transaction within a designer. Transactions are
    /// used to wrap several changes into one unit of work, which 
    /// helps performance.
    /// </summary>
    public abstract class DesignerTransaction : IDisposable
    {
        private bool _suppressedFinalization;

        protected DesignerTransaction() : this("")
        {
        }
        
        protected DesignerTransaction(string description) => Description = description;

        public bool Canceled { get; private set; }

        public bool Committed { get; private set; }

        public string Description { get; }

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
        /// Commits this transaction. Once a transaction has been committed, further
        /// calls to this method will do nothing. You should always call this method
        /// after creating a transaction to ensure that the transaction is closed properly.
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
        /// User code should implement this method to perform the actual work of
        /// committing a transaction.
        /// </summary>
        protected abstract void OnCancel();

        /// <summary>
        /// User code should implement this method to perform the actual work of
        /// committing a transaction.
        /// </summary>
        protected abstract void OnCommit();

        /// <summary>
        /// Overrides Object to commit this transaction in case the user forgot.
        /// </summary>
        ~DesignerTransaction() => Dispose(false);

        /// <summary>
        /// Private implementation of IDisaposable. When a transaction is disposed
        /// it is committed.
        /// </summary>
        void IDisposable.Dispose()
        {
            Dispose(true);

            if (!_suppressedFinalization)
            {
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing) => Cancel();
    }
}
