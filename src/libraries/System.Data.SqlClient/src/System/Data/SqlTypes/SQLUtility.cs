// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------


using Res = System.SR;


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

    public class SqlTypeException :
        Exception
    {
        public SqlTypeException() : this(Res.GetString(Res.SqlMisc_SqlTypeMessage), null)
        {
        }

        // Creates a new SqlTypeException with its message string set to message.
        public SqlTypeException(String message) : this(message, null)
        {
        }

        public SqlTypeException(String message, Exception e) : base(message, e)
        {
            HResult = unchecked((int)0x80131930);
        }
    } // SqlTypeException

    public sealed class SqlNullValueException : SqlTypeException
    {
        // Creates a new SqlNullValueException with its message string set to the common string.
        public SqlNullValueException() : this(SQLResource.NullValueMessage, null)
        {
        }

        // Creates a new NullValueException with its message string set to message.
        public SqlNullValueException(String message) : this(message, null)
        {
        }

        public SqlNullValueException(String message, Exception e) : base(message, e)
        {
            HResult = unchecked((int)0x80131931);
        }
    } // NullValueException

    public sealed class SqlTruncateException : SqlTypeException
    {
        // Creates a new SqlTruncateException with its message string set to the empty string.
        public SqlTruncateException() : this(SQLResource.TruncationMessage, null)
        {
        }

        // Creates a new SqlTruncateException with its message string set to message.
        public SqlTruncateException(String message) : this(message, null)
        {
        }

        public SqlTruncateException(String message, Exception e) : base(message, e)
        {
            HResult = unchecked((int)0x80131932);
        }
    } // SqlTruncateException
} // namespace System.Data.SqlTypes
