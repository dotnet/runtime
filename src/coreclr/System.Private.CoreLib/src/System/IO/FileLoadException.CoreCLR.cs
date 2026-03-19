// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileLoadException
    {
        // Do not delete: this is invoked from native code.
        private FileLoadException(string? fileName, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            _message = FormatFileLoadExceptionMessage(FileName, HResult);
        }

        // Do not delete: this is invoked from native code.
        // Used when the requesting assembly chain is known, to provide assembly load dependency context.
        // The requestingAssemblyChain parameter is a newline-separated list of assembly display names,
        // from immediate parent to root ancestor.
        private FileLoadException(string? fileName, string? requestingAssemblyChain, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            _message = FormatFileLoadExceptionMessage(FileName, HResult);
            if (requestingAssemblyChain is not null)
                _message += Environment.NewLine + FormatRequestingAssemblyChain(requestingAssemblyChain);
        }

        internal static string FormatRequestingAssemblyChain(string requestingAssemblyChain)
        {
            int newlineIndex = requestingAssemblyChain.IndexOf('\n');
            if (newlineIndex < 0)
                return SR.Format(SR.IO_FileLoad_RequestingAssembly, requestingAssemblyChain);

            var result = new System.Text.StringBuilder();
            int start = 0;
            while (start < requestingAssemblyChain.Length)
            {
                int end = requestingAssemblyChain.IndexOf('\n', start);
                string name = end >= 0
                    ? requestingAssemblyChain.Substring(start, end - start)
                    : requestingAssemblyChain.Substring(start);

                if (result.Length > 0)
                    result.Append(Environment.NewLine);
                result.Append(SR.Format(SR.IO_FileLoad_RequestingAssembly, name));

                start = end >= 0 ? end + 1 : requestingAssemblyChain.Length;
            }

            return result.ToString();
        }

        internal static string FormatFileLoadExceptionMessage(string? fileName, int hResult)
        {
            string? format = null;
            GetFileLoadExceptionMessage(hResult, new StringHandleOnStack(ref format));

            string? message = null;
            if (hResult == HResults.COR_E_BADEXEFORMAT)
                message = SR.Arg_BadImageFormatException;
            else
                GetMessageForHR(hResult, new StringHandleOnStack(ref message));

            return string.Format(format!, fileName, message);
        }

        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void GetFileLoadExceptionMessage(int hResult, StringHandleOnStack retString);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "FileLoadException_GetMessageForHR")]
        private static partial void GetMessageForHR(int hresult, StringHandleOnStack retString);
    }
}
