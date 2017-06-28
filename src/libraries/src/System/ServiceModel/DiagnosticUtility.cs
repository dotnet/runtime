// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Security;
using System.ServiceModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.ServiceModel.Diagnostics;

namespace Microsoft.ServiceModel
{
    internal static class DiagnosticUtility
    {
        private static object s_lockObject = new object();
        private static ExceptionUtility s_exceptionUtility;

        internal static ExceptionUtility ExceptionUtility
        {
            get
            {
                return DiagnosticUtility.s_exceptionUtility ?? GetExceptionUtility();
            }
        }

        private static ExceptionUtility GetExceptionUtility()
        {
            lock (DiagnosticUtility.s_lockObject)
            {
                if (DiagnosticUtility.s_exceptionUtility == null)
                {
                    DiagnosticUtility.s_exceptionUtility = new ExceptionUtility();
                }
            }
            return DiagnosticUtility.s_exceptionUtility;
        }

        // TODO, mconnew: Tracing - need to wire this in to the tracing infrastructure
        public static bool ShouldUseActivity { get { return false; } }

        public static bool ShouldTraceVerbose { get { return false; } }

        public static bool ShouldTraceInformation { get { return false; } }

        public static bool ShouldTraceWarning { get { return false; } }

        public static bool ShouldTraceError { get { return false; } }
    }
}