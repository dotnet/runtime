// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        public static Task BindAssemblyExports(string? assemblyName)
        {
            Interop.Runtime.BindAssemblyExports(Marshal.StringToCoTaskMemUTF8(assemblyName));
            return Task.CompletedTask;
        }

        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var (assemblyName, nameSpace, shortClassName, methodName) = ParseFQN(fullyQualifiedName);

            IntPtr monoMethod;
            Interop.Runtime.GetAssemblyExport(
                // FIXME: Pass UTF-16 through directly so C can work with it, doing the conversion
                //  in C# pulls in a bunch of dependencies we don't need this early in startup.
                // I tested removing the UTF8 conversion from this specific call, but other parts
                //  of startup I can't identify still pull in UTF16->UTF8 conversion, so it's not
                //  worth it to do that yet.
                Marshal.StringToCoTaskMemUTF8(assemblyName),
                Marshal.StringToCoTaskMemUTF8(nameSpace),
                Marshal.StringToCoTaskMemUTF8(shortClassName),
                Marshal.StringToCoTaskMemUTF8(methodName),
                signatureHash,
                &monoMethod);

            if (monoMethod == IntPtr.Zero)
            {
                Environment.FailFast($"Can't find {nameSpace}{shortClassName}{methodName} in {assemblyName}.dll");
            }

            var signature = GetMethodSignature(signatures, null, null);

            // this will hit JS side possibly on another thread, depending on JSProxyContext.CurrentThreadContext
            JavaScriptImports.BindCSFunction(monoMethod, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);

            FreeMethodSignatureBuffer(signature);

            return signature;
        }
    }
}
