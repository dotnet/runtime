// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO
{
    public partial class FileLoadException
    {
        private FileLoadException(string? fileName, string? requestingAssemblyChain, int hResult)
            : base(null)
        {
            HResult = hResult;
            FileName = fileName;
            _requestingAssemblyChain = requestingAssemblyChain;
            _message = FormatFileLoadExceptionMessage(FileName, HResult);
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

        // See clrex.cpp for native version.
        internal enum FileLoadExceptionKind
        {
            FileLoad,
            BadImageFormat,
            FileNotFound,
            OutOfMemory
        }

        [UnmanagedCallersOnly]
        internal static unsafe void Create(FileLoadExceptionKind kind, char* pFileName, char* pRequestingAssemblyChain, int hresult, object* pThrowable, Exception* pException)
        {
            try
            {
                string? fileName = pFileName is not null ? new string(pFileName) : null;
                string? requestingAssemblyChain = pRequestingAssemblyChain is not null ? new string(pRequestingAssemblyChain) : null;
                Debug.Assert(Enum.IsDefined(kind));
                *pThrowable = kind switch
                {
                    FileLoadExceptionKind.BadImageFormat => new BadImageFormatException(fileName, requestingAssemblyChain, hresult),
                    FileLoadExceptionKind.FileNotFound => new FileNotFoundException(fileName, requestingAssemblyChain, hresult),
                    FileLoadExceptionKind.OutOfMemory => new OutOfMemoryException(),
                    _ /* FileLoadExceptionKind.FileLoad */ => new FileLoadException(fileName, requestingAssemblyChain, hresult),
                };
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
