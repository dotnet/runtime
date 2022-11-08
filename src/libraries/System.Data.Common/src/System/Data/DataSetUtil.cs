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

    // only StackOverflowException & ThreadAbortException are sealed classes
    private static readonly Type s_stackOverflowType = typeof(StackOverflowException);
    private static readonly Type s_outOfMemoryType = typeof(OutOfMemoryException);
    private static readonly Type s_threadAbortType = typeof(System.Threading.ThreadAbortException);
    private static readonly Type s_nullReferenceType = typeof(NullReferenceException);
    private static readonly Type s_accessViolationType = typeof(AccessViolationException);
    private static readonly Type s_securityType = typeof(System.Security.SecurityException);

    internal static bool IsCatchableExceptionType(Exception e)
    {
        // a 'catchable' exception is defined by what it is not.
        Type type = e.GetType();

        return ((type != s_stackOverflowType) &&
                 (type != s_outOfMemoryType) &&
                 (type != s_threadAbortType) &&
                 (type != s_nullReferenceType) &&
                 (type != s_accessViolationType) &&
                 !s_securityType.IsAssignableFrom(type));
    }
}
