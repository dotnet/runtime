// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileLoadException
    {
        // Do not delete: this is invoked from native code.
        private FileLoadException(string? fileName, string? fusionLog, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            FusionLog = fusionLog;
            _message = FormatFileLoadExceptionMessage(FileName, HResult);
        }

        internal static string FormatFileLoadExceptionMessage(string? fileName, int hResult)
        {
            string? format = null;
            GetFileLoadExceptionMessage(hResult, JitHelpers.GetStringHandleOnStack(ref format));

            string? message = null;
            if (hResult == System.HResults.COR_E_BADEXEFORMAT)
                message = SR.Arg_BadImageFormatException;
            else 
                GetMessageForHR(hResult, JitHelpers.GetStringHandleOnStack(ref message));

            return string.Format(format, fileName, message);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetFileLoadExceptionMessage(int hResult, StringHandleOnStack retString);

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void GetMessageForHR(int hresult, StringHandleOnStack retString);
    }
}
