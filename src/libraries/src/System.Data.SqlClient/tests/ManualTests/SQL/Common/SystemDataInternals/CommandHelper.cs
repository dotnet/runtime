// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Data.SqlClient;
using System.Reflection;

namespace System.Data.SqlClient.ManualTesting.Tests.SystemDataInternals
{
    internal static class CommandHelper
    {
        private static Type s_sqlCommand = typeof(SqlCommand);
        private static MethodInfo s_completePendingReadWithSuccess = s_sqlCommand.GetMethod("CompletePendingReadWithSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo s_completePendingReadWithFailure = s_sqlCommand.GetMethod("CompletePendingReadWithFailure", BindingFlags.NonPublic | BindingFlags.Instance);
        private static PropertyInfo s_debugForceAsyncWriteDelay = s_sqlCommand.GetProperty("DebugForceAsyncWriteDelay", BindingFlags.NonPublic | BindingFlags.Static);

        internal static void CompletePendingReadWithSuccess(SqlCommand command, bool resetForcePendingReadsToWait)
        {
            s_completePendingReadWithSuccess.Invoke(command, new object[] { resetForcePendingReadsToWait });
        }

        internal static void CompletePendingReadWithFailure(SqlCommand command, int errorCode, bool resetForcePendingReadsToWait)
        {
            s_completePendingReadWithFailure.Invoke(command, new object[] { errorCode, resetForcePendingReadsToWait });
        }

        internal static int ForceAsyncWriteDelay
        {
            get { return (int)s_debugForceAsyncWriteDelay.GetValue(null); }
            set { s_debugForceAsyncWriteDelay.SetValue(null, value); }
        }
    }
}
