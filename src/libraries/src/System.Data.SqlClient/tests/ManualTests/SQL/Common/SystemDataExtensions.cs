// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlClient.ManualTesting.Tests.SystemDataInternals;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SystemDataExtensions
    {
        public static void CompletePendingReadWithSuccess(this SqlDataReader reader, bool resetForcePendingReadsToWait)
        {
            DataReaderHelper.CompletePendingReadWithSuccess(reader, resetForcePendingReadsToWait);
        }

        public static void CompletePendingReadWithFailure(this SqlDataReader reader, int errorCode, bool resetForcePendingReadsToWait)
        {
            DataReaderHelper.CompletePendingReadWithFailure(reader, errorCode, resetForcePendingReadsToWait);
        }

        public static void CompletePendingReadWithSuccess(this SqlCommand command, bool resetForcePendingReadsToWait)
        {
            CommandHelper.CompletePendingReadWithSuccess(command, resetForcePendingReadsToWait);
        }

        public static void CompletePendingReadWithFailure(this SqlCommand command, int errorCode, bool resetForcePendingReadsToWait)
        {
            CommandHelper.CompletePendingReadWithFailure(command, errorCode, resetForcePendingReadsToWait);
        }

        public static void SetDefaultTimeout(this SqlDataReader reader, long milliseconds)
        {
            DataReaderHelper.SetDefaultTimeout(reader, milliseconds);
        }

        public static T GetSchemaEntry<T>(this SqlDataReader reader, int row, string schemaEntry)
        {
            return DataReaderHelper.GetSchemaEntry<T>(reader, row, schemaEntry);
        }

        public static object[] GetMetaEntries(this SqlDataReader reader)
        {
            return DataReaderHelper.GetMetaEntries(reader);
        }

        public static bool IsLong(this SqlDataReader reader, int row)
        {
            return DataReaderHelper.IsLong(reader, row);
        }
    }
}
