// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Data.SqlClient.ManualTesting.Tests.SystemDataInternals
{
    internal static class DataReaderHelper
    {
        private static Type s_sqlDataReader = typeof(SqlDataReader);
        private static MethodInfo s_completePendingReadWithSuccess = s_sqlDataReader.GetMethod("CompletePendingReadWithSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo s_completePendingReadWithFailure = s_sqlDataReader.GetMethod("CompletePendingReadWithFailure", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo s_defaultTimeoutMilliseconds = s_sqlDataReader.GetField("_defaultTimeoutMilliseconds", BindingFlags.NonPublic | BindingFlags.Instance);

        private static PropertyInfo s_metaData = s_sqlDataReader.GetProperty("MetaData", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static void CompletePendingReadWithSuccess(SqlDataReader reader, bool resetForcePendingReadsToWait)
        {
            s_completePendingReadWithSuccess.Invoke(reader, new object[] { resetForcePendingReadsToWait });
        }

        internal static void CompletePendingReadWithFailure(SqlDataReader reader, int errorCode, bool resetForcePendingReadsToWait)
        {
            s_completePendingReadWithFailure.Invoke(reader, new object[] { errorCode, resetForcePendingReadsToWait });
        }

        internal static void SetDefaultTimeout(SqlDataReader reader, long milliseconds)
        {
            s_defaultTimeoutMilliseconds.SetValue(reader, milliseconds);
        }

        internal static bool IsLong(SqlDataReader reader, int row)
        {
            object schema = GetSchemaEntry<object>(reader, row, "metaType");
            object islong = schema.GetType().GetMethod("IsLong").Invoke(schema, new object[] { null });
            return (bool)islong;
        }

        internal static T GetSchemaEntry<T>(SqlDataReader reader, int row, string schemaEntry)
        {
            object[] metadataarray = GetMetaEntries(reader);
            object schema = metadataarray[row].GetType().GetField(schemaEntry).GetValue(metadataarray[row]);

            return (T)schema;
        }

        internal static object[] GetMetaEntries(SqlDataReader reader)
        {
            object metadatas = s_metaData.GetValue(reader);
            object metadataarray = metadatas.GetType().GetField("metaDataArray").GetValue(metadatas);

            return (object[])metadataarray;
        }
    }
}
