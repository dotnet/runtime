// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's kept from trimming by DynamicDependencyAttribute in the generated code.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "It's kept from trimming by DynamicDependencyAttribute in the generated code.")]
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            var ctx = JSProxyContext.BindingContextOrMain();
            var (assemblyName, nameSpace, shortClassName, methodName) = ParseFQN(fullyQualifiedName);
            var wrapperName = $"__Wrapper_{methodName}_{signatureHash}";
            // reflection wants '+' between nested types, but the JS side walks the export tree on '/', so keep shortClassName as parsed
            var reflectionClassName = shortClassName.Replace('/', '+');

            // get MethodInfo from the fully qualified name
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            var clazz = string.IsNullOrEmpty(nameSpace)
                ? assembly.GetType(reflectionClassName)
                : assembly.GetType(nameSpace + "." + reflectionClassName);
            if (clazz == null)
            {
                Environment.FailFast($"Can't find {nameSpace}{shortClassName} in {assemblyName} assembly");
            }
            var wrapperInfo = clazz.GetMethod(wrapperName, BindingFlags.Static | BindingFlags.NonPublic);
            if (wrapperInfo == null)
            {
                Environment.FailFast($"Can't find method wrapper {wrapperName} in {nameSpace}.{shortClassName} in {assemblyName} assembly");
            }

            Action<IntPtr> wrapper = (IntPtr args) =>
            {
                object boxedLegacyArgs = Pointer.Box((void*)args, typeof(JSMarshalerArgument*));
                // real signature is void (JSMarshalerArgument* args)
                wrapperInfo.Invoke(null, new object?[] { boxedLegacyArgs });
            };

            int methodHandle = ctx.AllocJSExportHandle(wrapper);

            var signature = GetMethodSignature(signatures, null, null);

            // this will hit JS side possibly on another thread, depending on JSProxyContext.CurrentThreadContext
            JavaScriptImports.BindCSFunction(methodHandle, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);

            FreeMethodSignatureBuffer(signature);

            return signature;
        }
    }
}
