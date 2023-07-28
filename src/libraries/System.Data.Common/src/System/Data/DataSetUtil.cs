// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data;
using System.Diagnostics;

internal static class DataSetUtil
{
    internal static void CheckArgumentNull<T>(T argumentValue, string argumentName) where T : class
    {
        if (null == argumentValue)
        {
            throw ArgumentNull(argumentName);
        }
    }

    internal static ArgumentException Argument(string message)
    {
        return new ArgumentException(message);
    }

    internal static ArgumentNullException ArgumentNull(string message)
    {
        return new ArgumentNullException(message);
    }

    internal static ArgumentOutOfRangeException ArgumentOutOfRange(string message, string parameterName)
    {
        return new ArgumentOutOfRangeException(parameterName, message);
    }

    internal static InvalidCastException InvalidCast(string message)
    {
        return new InvalidCastException(message);
    }

    internal static InvalidOperationException InvalidOperation(string message)
    {
        return new InvalidOperationException(message);
    }

    internal static NotSupportedException NotSupported(string message)
    {
        return new NotSupportedException(message);
    }

    internal static ArgumentOutOfRangeException InvalidEnumerationValue(Type type, int value)
    {
        return ArgumentOutOfRange(SR.Format(SR.DataSetLinq_InvalidEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
    }

    internal static ArgumentOutOfRangeException InvalidDataRowState(DataRowState value)
    {
#if DEBUG
        switch (value)
        {
            case DataRowState.Detached:
            case DataRowState.Unchanged:
            case DataRowState.Added:
            case DataRowState.Deleted:
            case DataRowState.Modified:
                Debug.Fail("valid DataRowState " + value.ToString());
                break;
        }
#endif
        return InvalidEnumerationValue(typeof(DataRowState), (int)value);
    }

    internal static ArgumentOutOfRangeException InvalidLoadOption(LoadOption value)
    {
#if DEBUG
        switch (value)
        {
            case LoadOption.OverwriteChanges:
            case LoadOption.PreserveChanges:
            case LoadOption.Upsert:
                Debug.Fail("valid LoadOption " + value.ToString());
                break;
        }
#endif
        return InvalidEnumerationValue(typeof(LoadOption), (int)value);
    }


    internal static bool IsCatchableExceptionType(Exception e)
    {
        // a 'catchable' exception is defined by what it is not.
        // only StackOverflowException & ThreadAbortException are sealed classes

        return ((e.GetType() != typeof(StackOverflowException)) &&
                 (e.GetType() != typeof(OutOfMemoryException)) &&
                 (e.GetType() != typeof(System.Threading.ThreadAbortException)) &&
                 (e.GetType() != typeof(NullReferenceException)) &&
                 (e.GetType() != typeof(System.Security.SecurityException)) &&
                 !typeof(System.Security.SecurityException).IsAssignableFrom(e.GetType()));
    }
}
