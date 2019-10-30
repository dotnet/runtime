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
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
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
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class SqlNullValueException : SqlTypeException
    {
        // Creates a new SqlNullValueException with its message string set to the common string.
        public SqlNullValueException() : this(SQLResource.NullValueMessage, null)
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
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class SqlTruncateException : SqlTypeException
    {
        // Creates a new SqlTruncateException with its message string set to the empty string.
        public SqlTruncateException() : this(SQLResource.TruncationMessage, null)
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
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class SqlNotFilledException : SqlTypeException
    {
        // Creates a new SqlNotFilledException with its message string set to the common string.
        public SqlNotFilledException() : this(SQLResource.NotFilledMessage, null)
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

        private SqlNotFilledException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class SqlAlreadyFilledException : SqlTypeException
    {
        // Creates a new SqlNotFilledException with its message string set to the common string.
        public SqlAlreadyFilledException() : this(SQLResource.AlreadyFilledMessage, null)
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

        private SqlAlreadyFilledException(SerializationInfo si, StreamingContext sc) : base(si, sc)
        {
        }
    }
}
