// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
#if !MONO
        [ThreadStatic]
        private static JSProxyContext.PendingJSExport? t_bindingExport;

        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetExternalTypeMappingForAssembly")]
        private static extern Type? GetExternalTypeMappingForAssembly<TTypeMapGroup>(
            [UnsafeAccessorType("System.Runtime.InteropServices.TypeMapping, System.Private.CoreLib")] object? target, Assembly assembly, string key);

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Generated type-map providers preserve their constructors and directly reference their export wrappers.")]
        [DynamicDependency(nameof(PreserveExportProviderMap))]
        private static Type? GetExportProviderType(Assembly assembly, string assemblyName)
            => GetExternalTypeMappingForAssembly<JSExportProvider>(null, assembly, assemblyName);

        // Declare the group through the SDK's existing intrinsic without performing global runtime discovery.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "This method is rooted only to preserve the generated JSExport type-map group.")]
        private static IReadOnlyDictionary<string, Type> PreserveExportProviderMap()
            => TypeMapping.GetOrCreateExternalTypeMapping<JSExportProvider>();

        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The generated GetExportNames override preserves the provider's public parameterless constructor with DynamicDependency.")]
        private static unsafe bool TryBindMappedAssemblyExports(Assembly assembly, string assemblyName)
        {
            var ctx = JSProxyContext.AssertIsInteropThread();
            if (ctx.BoundExportAssemblies.ContainsKey(assemblyName))
                return true;

            Type? providerType = GetExportProviderType(assembly, assemblyName);
            if (providerType is null)
                return false;

            var provider = (JSExportProvider)Activator.CreateInstance(providerType)!;
            string[] names = provider.GetExportNames().ToArray();
            List<JSMarshalerType[]> signatures = provider.TakeSignatures() ?? new List<JSMarshalerType[]>();
            if (signatures.Count != names.Length)
                throw new InvalidOperationException(SR.JSExportMissingSignatures);

            var bindings = new JSFunctionBinding?[names.Length];
            var handles = new int[names.Length];
            var signaturePointers = new int[names.Length];
            bool registered = false;
            try
            {
                for (int index = 0; index < names.Length; index++)
                {
                    JSFunctionBinding signature = GetMethodSignature(signatures[index], null, null);
                    bindings[index] = signature;
                    handles[index] = ctx.AllocJSExportHandle(new JSProxyContext.PendingJSExport(provider, index, names[index], signature));
                    signaturePointers[index] = (int)signature.Header;
                }

                JavaScriptImports.RegisterLazyCSFunctions(assemblyName, names, handles, signaturePointers);
                ctx.BoundExportAssemblies.Add(assemblyName, provider);
                registered = true;
            }
            finally
            {
                foreach (JSFunctionBinding? signature in bindings)
                {
                    if (signature is not null)
                        FreeMethodSignatureBuffer(signature);
                }
                if (!registered)
                {
                    foreach (int handle in handles)
                    {
                        if (handle != 0)
                            ctx.FreeJSExportHandle(handle);
                    }
                }
            }

            return true;
        }

        internal static unsafe bool TryBindJSExport(JSProxyContext ctx, int methodHandle, JSMarshalerArgument* arguments, out JSProxyContext.JSExportEntry export)
        {
            export = default;
            ref JSMarshalerArgument exception = ref arguments[0];
            JSProxyContext.PendingJSExport? previous = t_bindingExport;
            JSProxyContext.PendingJSExport? entry = null;
            bool wasBinding = false;
            try
            {
                if (!ctx.TryGetPendingJSExport(methodHandle, out entry))
                    throw new InvalidOperationException(SR.JSExportMissingBinding);

                wasBinding = entry.IsBinding;
                if (wasBinding)
                    throw new InvalidOperationException(SR.JSExportReentrantBinding);

                entry.IsBinding = true;
                t_bindingExport = entry;
                entry.Provider.Bind(entry.MethodIndex.ToString(CultureInfo.InvariantCulture));
                if (entry.Callback == null)
                    throw new InvalidOperationException(SR.JSExportMissingBinding);

                export = new JSProxyContext.JSExportEntry(entry.Callback);
                ctx.CompleteJSExportBinding(methodHandle, export);
                return true;
            }
            catch (Exception ex)
            {
                if (entry is not null)
                {
                    for (int index = 0; index < entry.ArgumentCount; index++)
                        arguments[index + 2].ReleaseJSExportArgument();
                }
                if (entry is not null && !wasBinding)
                    entry.Callback = null;
                if (arguments[1].slot.Type == MarshalerType.TaskPreCreated)
                    arguments[1].ToJS(Task.FromException(ex));
                else
                    exception.ToJS(ex);

                return false;
            }
            finally
            {
                if (entry is not null)
                    entry.IsBinding = wasBinding;
                t_bindingExport = previous;
            }
        }

        // Current generators pass the wrapper directly as a managed function pointer.
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures, delegate*<JSMarshalerArgument*, void> callback)
        {
            if (t_bindingExport is JSProxyContext.PendingJSExport entry)
                return BindPendingJSExport(entry, fullyQualifiedName, signatureHash, callback);

            var (assemblyName, nameSpace, shortClassName, methodName) = ParseFQN(fullyQualifiedName);
            return BindManagedFunctionCore(assemblyName, nameSpace, shortClassName, methodName, signatureHash, signatures, callback);
        }
#else
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures, delegate*<JSMarshalerArgument*, void> callback)
            => throw new PlatformNotSupportedException();
#endif

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

#if MONO
            Action<IntPtr> wrapper = (IntPtr args) =>
            {
                object boxedLegacyArgs = Pointer.Box((void*)args, typeof(JSMarshalerArgument*));
                wrapperInfo.Invoke(null, new object?[] { boxedLegacyArgs });
            };
            var ctx = JSProxyContext.BindingContextOrMain();
            int methodHandle = ctx.AllocJSExportHandle(wrapper);
            var signature = GetMethodSignature(signatures, null, null);
            JavaScriptImports.BindCSFunction(methodHandle, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);
            FreeMethodSignatureBuffer(signature);
            return signature;
#else
            RuntimeHelpers.PrepareMethod(wrapperInfo.MethodHandle);
            var callback = (delegate*<JSMarshalerArgument*, void>)wrapperInfo.MethodHandle.GetFunctionPointer();

            if (t_bindingExport is JSProxyContext.PendingJSExport entry)
                return BindPendingJSExport(entry, fullyQualifiedName, signatureHash, callback);

            return BindManagedFunctionCore(assemblyName, nameSpace, shortClassName, methodName, signatureHash, signatures, callback);
#endif
        }

#if !MONO
        private static unsafe JSFunctionBinding BindPendingJSExport(JSProxyContext.PendingJSExport entry, string fullyQualifiedName, int signatureHash,
            delegate*<JSMarshalerArgument*, void> callback)
        {
            Span<char> hash = stackalloc char[11]; // Enough for Int32.MinValue.
            ReadOnlySpan<char> key = entry.CatalogKey;
            int nameLength = fullyQualifiedName.Length;
            if (callback == null || entry.Callback != null ||
                !signatureHash.TryFormat(hash, out int hashLength, provider: CultureInfo.InvariantCulture) ||
                key.Length != nameLength + 1 + hashLength || key[nameLength] != '#' ||
                !key[..nameLength].SequenceEqual(fullyQualifiedName) || !key[(nameLength + 1)..].SequenceEqual(hash[..hashLength]))
            {
                throw new InvalidOperationException(SR.JSExportInvalidBinding);
            }

            entry.Callback = callback;
            return entry.Signature;
        }

        private static unsafe JSFunctionBinding BindManagedFunctionCore(string assemblyName, string nameSpace, string shortClassName, string methodName,
            int signatureHash, ReadOnlySpan<JSMarshalerType> signatures, delegate*<JSMarshalerArgument*, void> callback)
        {
            ArgumentOutOfRangeException.ThrowIfZero(signatures.Length, nameof(signatures));

            var ctx = JSProxyContext.BindingContextOrMain();
            int methodHandle = ctx.AllocJSExportHandle(new JSProxyContext.JSExportEntry(callback));

            var signature = GetMethodSignature(signatures, null, null);

            // this will hit JS side possibly on another thread, depending on JSProxyContext.CurrentThreadContext
            JavaScriptImports.BindCSFunction(methodHandle, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);

            FreeMethodSignatureBuffer(signature);

            return signature;
        }
#endif
    }
}
