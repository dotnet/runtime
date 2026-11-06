// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        private static readonly Lazy<IReadOnlyDictionary<string, Type>> s_exportProviders = new(CreateExportProviderMap);

        private static IReadOnlyDictionary<string, Type> CreateExportProviderMap()
            => TypeMapping.GetOrCreateExternalTypeMapping<JSExportProvider>();

        private static bool TryBindMappedAssemblyExports(string assemblyName)
        {
            if (!s_exportProviders.Value.TryGetValue(assemblyName, out Type? providerType))
                return false;

            TypeMapFactoryAttribute<JSExportProvider>? factory = providerType.GetCustomAttribute<TypeMapFactoryAttribute<JSExportProvider>>(inherit: false);
            if (factory is null)
                throw new InvalidOperationException(SR.Format(SR.JSExportProviderMissingFactory, providerType));

            JSExportProvider provider = factory.CreateInstance();
            JavaScriptImports.RegisterLazyCSFunctions(assemblyName, provider.GetExportNames().ToArray(), provider.Bind);
            return true;
        }

        // Current generators pass the wrapper directly as a managed function pointer.
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures, delegate*<JSMarshalerArgument*, void> callback)
        {
            var (assemblyName, nameSpace, shortClassName, methodName) = ParseFQN(fullyQualifiedName);
            return BindManagedFunctionCore(assemblyName, nameSpace, shortClassName, methodName, signatureHash, signatures, callback);
        }

        // Older generators, including .NET 11, use reflection to locate the wrapper.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's kept from trimming by DynamicDependencyAttribute in the generated code.")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "It's kept from trimming by DynamicDependencyAttribute in the generated code.")]
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
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

            RuntimeHelpers.PrepareMethod(wrapperInfo.MethodHandle);
            var callback = (delegate*<JSMarshalerArgument*, void>)wrapperInfo.MethodHandle.GetFunctionPointer();

            return BindManagedFunctionCore(assemblyName, nameSpace, shortClassName, methodName, signatureHash, signatures, callback);
        }

        private static unsafe JSFunctionBinding BindManagedFunctionCore(string assemblyName, string nameSpace, string shortClassName, string methodName,
            int signatureHash, ReadOnlySpan<JSMarshalerType> signatures, delegate*<JSMarshalerArgument*, void> callback)
        {
            var ctx = JSProxyContext.BindingContextOrMain();
            int methodHandle = ctx.AllocJSExportHandle(new JSProxyContext.JSExportEntry(callback));

            var signature = GetMethodSignature(signatures, null, null);

            // this will hit JS side possibly on another thread, depending on JSProxyContext.CurrentThreadContext
            JavaScriptImports.BindCSFunction(methodHandle, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);

            FreeMethodSignatureBuffer(signature);

            return signature;
        }
    }
}
