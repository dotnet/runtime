// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// This is similar to ComTypes.EXCEPINFO, but lets us do our own custom marshaling
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExcepInfo
    {
        private short wCode;
        private short wReserved;
        private IntPtr bstrSource;
        private IntPtr bstrDescription;
        private IntPtr bstrHelpFile;
        private int dwHelpContext;
        private IntPtr pvReserved;
        private IntPtr pfnDeferredFillIn;
        private int scode;

#if DEBUG
        static ExcepInfo()
        {
            Debug.Assert(Marshal.SizeOf(typeof(ExcepInfo)) == Marshal.SizeOf(typeof(ComTypes.EXCEPINFO)));
        }
#endif

        private static string ConvertAndFreeBstr(ref IntPtr bstr)
        {
            if (bstr == IntPtr.Zero)
            {
                return null;
            }

            string result = Marshal.PtrToStringBSTR(bstr);
            Marshal.FreeBSTR(bstr);
            bstr = IntPtr.Zero;
            return result;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal Exception GetException()
        {
            Debug.Assert(pfnDeferredFillIn == IntPtr.Zero);
#if DEBUG
            System.Diagnostics.Debug.Assert(wReserved != -1);
            wReserved = -1; // to ensure that the method gets called only once
#endif

            int errorCode = (scode != 0) ? scode : wCode;
            Exception exception = Marshal.GetExceptionForHR(errorCode);

            string message = ConvertAndFreeBstr(ref bstrDescription);
            if (message != null)
            {
                // If we have a custom message, create a new Exception object with the message set correctly.
                // We need to create a new object because "exception.Message" is a read-only property.
                if (exception is COMException)
                {
                    exception = new COMException(message, errorCode);
                }
                else
                {
                    Type exceptionType = exception.GetType();
                    ConstructorInfo ctor = exceptionType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        exception = (Exception)ctor.Invoke(new object[] { message });
                    }
                }
            }

            exception.Source = ConvertAndFreeBstr(ref bstrSource);

            string helpLink = ConvertAndFreeBstr(ref bstrHelpFile);
            if (helpLink != null && dwHelpContext != 0)
            {
                helpLink += "#" + dwHelpContext;
            }
            exception.HelpLink = helpLink;

            return exception;
        }
    }
}
