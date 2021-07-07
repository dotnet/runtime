// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.Common
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract class DbException : System.Runtime.InteropServices.ExternalException
    {
        protected DbException() : base() { }

        protected DbException(string? message) : base(message) { }

        protected DbException(string? message, System.Exception? innerException) : base(message, innerException) { }

        protected DbException(string? message, int errorCode) : base(message, errorCode) { }

        protected DbException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Indicates whether the error represented by this <see cref="DbException" /> could be a transient error, i.e. if retrying the triggering
        /// operation may succeed without any other change. Examples of transient errors include failure to acquire a database lock, networking
        /// issues. This allows automatic retry execution strategies to be developed without knowledge of specific database error codes.
        /// </summary>
        public virtual bool IsTransient => false;

        /// <summary>
        /// <para>
        /// For database providers which support it, contains a standard SQL 5-character return code indicating the success or failure of
        /// the database operation. The first 2 characters represent the <strong>class</strong> of the return code (e.g. error, success),
        /// while the last 3 characters represent the <strong>subclass</strong>, allowing detection of error scenarios in a
        /// database-portable way.
        /// </para>
        /// <para>
        /// For database providers which don't support it, or for inapplicable error scenarios, contains <see langword="null" />.
        /// </para>
        /// </summary>
        /// <returns>
        /// A standard SQL 5-character return code, or <see langword="null" />.
        /// </returns>
        public virtual string? SqlState => null;

        public DbBatchCommand? BatchCommand => DbBatchCommand;

        protected virtual DbBatchCommand? DbBatchCommand => null;
    }
}
