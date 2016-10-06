// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Data.SqlTypes
{
    internal enum EComparison
    {
        LT,
        LE,
        EQ,
        GE,
        GT,
        NE
    }

    // This enumeration is used to inquire about internal storage of a SqlBytes or SqlChars instance:
    public enum StorageState
    {
        Buffer = 0,
        Stream = 1,
        UnmanagedBuffer = 2
    }

    [Serializable]
    public class SqlTypeException : SystemException
    {
        public SqlTypeException() : this(SR.SqlMisc_SqlTypeMessage, null)
        {
        }

        // Creates a new SqlTypeException with its message string set to message.
        public SqlTypeException(string message) : this(message, null)
        {
        }

        public SqlTypeException(string message, Exception e) : base(message, e)
        {
            HResult = HResults.SqlType;
        }

        // runtime will call even if private...
        // <fxcop ignore=SerializableTypesMustHaveMagicConstructorWithAdequateSecurity />
        protected SqlTypeException(SerializationInfo si, StreamingContext sc) : base(SqlTypeExceptionSerialization(si, sc), sc)
        {
        }

        private static SerializationInfo SqlTypeExceptionSerialization(SerializationInfo si, StreamingContext sc)
        {
            if ((null != si) && (1 == si.MemberCount))
            {
                string message = si.GetString("SqlTypeExceptionMessage");
                SqlTypeException fakeValue = new SqlTypeException(message);
                fakeValue.GetObjectData(si, sc);
            }
            return si;
        }
    } // SqlTypeException

    [Serializable]
    public sealed class SqlNullValueException : SqlTypeException
    {
        // Creates a new SqlNullValueException with its message string set to the common string.
        public SqlNullValueException() : this(SQLResource.s_nullValueMessage, null)
        {
        }

        // Creates a new NullValueException with its message string set to message.
        public SqlNullValueException(string message) : this(message, null)
        {
        }

        public SqlNullValueException(string message, Exception e) : base(message, e)
        {
            HResult = HResults.SqlNullValue;
        }

        // runtime will call even if private...
        // <fxcop ignore=SerializableTypesMustHaveMagicConstructorWithAdequateSecurity />
        private SqlNullValueException(SerializationInfo si, StreamingContext sc) : base(SqlNullValueExceptionSerialization(si, sc), sc)
        {
        }

        private static SerializationInfo SqlNullValueExceptionSerialization(SerializationInfo si, StreamingContext sc)
        {
            if ((null != si) && (1 == si.MemberCount))
            {
                string message = si.GetString("SqlNullValueExceptionMessage");
                SqlNullValueException fakeValue = new SqlNullValueException(message);
                fakeValue.GetObjectData(si, sc);
            }
            return si;
        }
    } // NullValueException

    [Serializable]
    public sealed class SqlTruncateException : SqlTypeException
    {
        // Creates a new SqlTruncateException with its message string set to the empty string.
        public SqlTruncateException() : this(SQLResource.s_truncationMessage, null)
        {
        }

        // Creates a new SqlTruncateException with its message string set to message.
        public SqlTruncateException(string message) : this(message, null)
        {
        }

        public SqlTruncateException(string message, Exception e) : base(message, e)
        {
            HResult = HResults.SqlTruncate;
        }

        // runtime will call even if private...
        // <fxcop ignore=SerializableTypesMustHaveMagicConstructorWithAdequateSecurity />
        private SqlTruncateException(SerializationInfo si, StreamingContext sc) : base(SqlTruncateExceptionSerialization(si, sc), sc)
        {
        }

        private static SerializationInfo SqlTruncateExceptionSerialization(SerializationInfo si, StreamingContext sc)
        {
            if ((null != si) && (1 == si.MemberCount))
            {
                string message = si.GetString("SqlTruncateExceptionMessage");
                SqlTruncateException fakeValue = new SqlTruncateException(message);
                fakeValue.GetObjectData(si, sc);
            }
            return si;
        }
    } // SqlTruncateException

    [Serializable]
    public sealed class SqlNotFilledException : SqlTypeException
    {
        // Creates a new SqlNotFilledException with its message string set to the common string.
        public SqlNotFilledException() : this(SQLResource.s_notFilledMessage, null)
        {
        }

        // Creates a new NullValueException with its message string set to message.
        public SqlNotFilledException(string message) : this(message, null)
        {
        }

        public SqlNotFilledException(string message, Exception e) : base(message, e)
        {
            HResult = HResults.SqlNullValue;
        }

        // runtime will call even if private...
        // <fxcop ignore=SerializableTypesMustHaveMagicConstructorWithAdequateSecurity />
        private SqlNotFilledException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }
    } // SqlNotFilledException

    [Serializable]
    public sealed class SqlAlreadyFilledException : SqlTypeException
    {
        // Creates a new SqlNotFilledException with its message string set to the common string.
        public SqlAlreadyFilledException() : this(SQLResource.s_alreadyFilledMessage, null)
        {
        }

        // Creates a new NullValueException with its message string set to message.
        public SqlAlreadyFilledException(string message) : this(message, null)
        {
        }

        public SqlAlreadyFilledException(string message, Exception e) : base(message, e)
        {
            HResult = HResults.SqlNullValue;
        }

        // runtime will call even if private...
        // <fxcop ignore=SerializableTypesMustHaveMagicConstructorWithAdequateSecurity />
        private SqlAlreadyFilledException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }
    } // SqlNotFilledException
}
