// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------

//------------------------------------------------------------

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

namespace System.ServiceModel
{
    internal static class DiagnosticUtility
    {
        static object lockObject = new object();
        private static ExceptionUtility exceptionUtility;

        internal static ExceptionUtility ExceptionUtility
        {
            get
            {
                return DiagnosticUtility.exceptionUtility ?? GetExceptionUtility();
            }
        }

        static ExceptionUtility GetExceptionUtility()
        {
            lock (DiagnosticUtility.lockObject)
            {
                if (DiagnosticUtility.exceptionUtility == null)
                {
                    DiagnosticUtility.exceptionUtility = new ExceptionUtility();
                }
            }
            return DiagnosticUtility.exceptionUtility;
        }

        // TODO, mconnew: Tracing - need to wire this in to the tracing infrastructure
        public static bool ShouldUseActivity { get { return false; } }

        public static bool ShouldTraceVerbose { get { return false; } }

        public static bool ShouldTraceInformation { get { return false; } }

        public static bool ShouldTraceWarning { get { return false; } }

        public static bool ShouldTraceError { get { return false; } }
    }
}