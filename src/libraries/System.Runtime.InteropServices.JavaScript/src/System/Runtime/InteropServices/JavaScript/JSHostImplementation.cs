﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private const string TaskGetResultName = "get_Result";
        private static MethodInfo? s_taskGetResultMethodInfo;

        public static bool GetTaskResultDynamic(Task task, out object? value)
        {
            var type = task.GetType();
            if (type == typeof(Task))
            {
                value = null;
                return false;
            }
            MethodInfo method = GetTaskResultMethodInfo(type);
            if (method != null)
            {
                value = method.Invoke(task, null);
                return true;
            }
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Gets the MethodInfo for the Task{T}.Result property getter.
        /// </summary>
        /// <remarks>
        /// This ensures the returned MethodInfo is strictly for the Task{T} type, and not
        /// a "Result" property on some other class that derives from Task or a "new Result"
        /// property on a class that derives from Task{T}.
        ///
        /// The reason for this restriction is to make this use of Reflection trim-compatible,
        /// ensuring that trimming doesn't change the application's behavior.
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Task<T>.Result is preserved by the ILLinker because s_taskGetResultMethodInfo was initialized with it.")]
        public static MethodInfo GetTaskResultMethodInfo(Type taskType)
        {
            if (taskType != null)
            {
                if (s_taskGetResultMethodInfo == null)
                {
                    s_taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName);
                }
                MethodInfo? getter = taskType.GetMethod(TaskGetResultName);
                if (getter != null && getter.HasSameMetadataDefinitionAs(s_taskGetResultMethodInfo!))
                {
                    return getter;
                }
            }

            throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowException(ref JSMarshalerArgument arg)
        {
            arg.ToManaged(out Exception? ex);

            if (ex != null)
            {
                throw ex;
            }
            throw new InvalidOperationException();
        }

        public static async Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken)
        {
            Task<JSObject> modulePromise = JavaScriptImports.DynamicImport(moduleName, moduleUrl);
            var wrappedTask = CancellationHelper(modulePromise, cancellationToken);
            return await wrappedTask.ConfigureAwait(
                ConfigureAwaitOptions.ContinueOnCapturedContext |
                ConfigureAwaitOptions.ForceYielding); // this helps to finish the import before we bind the module in [JSImport]
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<JSObject> CancellationHelper(Task<JSObject> jsTask, CancellationToken cancellationToken)
        {
            if (jsTask.IsCompletedSuccessfully)
            {
                return jsTask.Result;
            }
            using (var receiveRegistration = cancellationToken.Register(static s =>
            {
                CancelablePromise.CancelPromise((Task<JSObject>)s!);
            }, jsTask))
            {
                return await jsTask.ConfigureAwait(true);
            }
        }

        // res type is first argument
        public static unsafe JSFunctionBinding GetMethodSignature(ReadOnlySpan<JSMarshalerType> types, string? functionName, string? moduleName)
        {
            int argsCount = types.Length - 1;
            int size = JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + ((argsCount + 2) * sizeof(JSFunctionBinding.JSBindingType));

            int functionNameBytes = 0;
            int functionNameOffset = 0;
            if (functionName != null)
            {
                functionNameOffset = size;
                size += 4;
                functionNameBytes = functionName.Length * 2;
                size += functionNameBytes;
            }
            int moduleNameBytes = 0;
            int moduleNameOffset = 0;
            if (moduleName != null)
            {
                moduleNameOffset = size;
                size += 4;
                moduleNameBytes = moduleName.Length * 2;
                size += moduleNameBytes;
            }

            // this is never unallocated
            IntPtr buffer = Marshal.AllocHGlobal(size);

            var signature = new JSFunctionBinding
            {
                Header = (JSFunctionBinding.JSBindingHeader*)buffer,
                Sigs = (JSFunctionBinding.JSBindingType*)(buffer + JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + (2 * sizeof(JSFunctionBinding.JSBindingType))),
            };

            signature.Version = 2;
            signature.ArgumentCount = argsCount;
            signature.Exception = JSMarshalerType.Exception._signatureType;
            signature.Result = types[0]._signatureType;
#if FEATURE_WASM_MANAGED_THREADS
            signature.ImportHandle = (int)Interlocked.Increment(ref JSFunctionBinding.nextImportHandle);
#else
            signature.ImportHandle = (int)JSFunctionBinding.nextImportHandle++;
#endif

#if DEBUG
            signature.FunctionName = functionName;
#endif
            for (int i = 0; i < argsCount; i++)
            {
                var type = signature.Sigs[i] = types[i + 1]._signatureType;
            }
            signature.IsAsync = types[0]._signatureType.Type == MarshalerType.Task;

            signature.Header[0].ImportHandle = signature.ImportHandle;
            signature.Header[0].FunctionNameLength = functionNameBytes;
            signature.Header[0].FunctionNameOffset = functionNameOffset;
            signature.Header[0].ModuleNameLength = moduleNameBytes;
            signature.Header[0].ModuleNameOffset = moduleNameOffset;
            if (functionNameBytes != 0)
            {
                fixed (void* fn = functionName)
                {
                    Unsafe.CopyBlock((byte*)buffer + functionNameOffset, fn, (uint)functionNameBytes);
                }
            }
            if (moduleNameBytes != 0)
            {
                fixed (void* mn = moduleName)
                {
                    Unsafe.CopyBlock((byte*)buffer + moduleNameOffset, mn, (uint)moduleNameBytes);
                }

            }

            return signature;
        }

        public static unsafe void FreeMethodSignatureBuffer(JSFunctionBinding signature)
        {
            Marshal.FreeHGlobal((nint)signature.Header);
            signature.Header = null;
            signature.Sigs = null;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's always part of the single compilation (and trimming) unit.")]
        public static void LoadLazyAssembly(byte[] dllBytes, byte[]? pdbBytes)
        {
            if (pdbBytes == null)
                AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes));
            else
                AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes), new MemoryStream(pdbBytes));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "It's always part of the single compilation (and trimming) unit.")]
        public static void LoadSatelliteAssembly(byte[] dllBytes)
        {
            AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(dllBytes));
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic access from JavaScript")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Dynamic access from JavaScript")]
        public static Task<int>? CallEntrypoint(string? assemblyName, string?[]? args, bool waitForDebugger)
        {
            try
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    throw new MissingMethodException(SR.MissingManagedEntrypointHandle);
                }
                if (!assemblyName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    assemblyName += ".dll";
                }
                Assembly mainAssembly = Assembly.LoadFrom(assemblyName);

                MethodInfo? method = mainAssembly.EntryPoint;
                if (method == null)
                {
                    throw new InvalidOperationException(string.Format(SR.CannotResolveManagedEntrypoint, "Main", assemblyName));
                }
                if (method.IsSpecialName)
                {
                    // we are looking for the original async method, rather than for the compiler generated wrapper like <Main>
                    // because we need to yield to browser event loop
                    var type = method.DeclaringType!;
                    var name = method.Name;
                    var asyncName = name + "$";
                    method = type.GetMethod(asyncName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method == null)
                    {
                        asyncName = name.Substring(1, name.Length - 2);
                        method = type.GetMethod(asyncName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    if (method == null)
                    {
                        throw new InvalidOperationException(string.Format(SR.CannotResolveManagedEntrypoint, asyncName, assemblyName));
                    }
                }

                Interop.Runtime.SetEntryAssembly(mainAssembly, waitForDebugger ? method.MetadataToken : 0);

                object[] argsToPass = System.Array.Empty<object>();
                Task<int>? result = null;
                var parameterInfos = method.GetParameters();
                if (parameterInfos.Length > 0 && parameterInfos[0].ParameterType == typeof(string[]))
                {
                    argsToPass = new object[] { args ?? System.Array.Empty<string>() };
                }
                if (method.ReturnType == typeof(void))
                {
                    method.Invoke(null, argsToPass);
                }
                else if (method.ReturnType == typeof(int))
                {
                    int intResult = (int)method.Invoke(null, argsToPass)!;
                    result = Task.FromResult(intResult);
                }
                else if (method.ReturnType == typeof(Task))
                {
                    Task methodResult = (Task)method.Invoke(null, argsToPass)!;
                    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                    result = tcs.Task;
                    methodResult.ContinueWith((t) =>
                    {
                        if (t.IsFaulted)
                        {
                            tcs.SetException(t.Exception!);
                        }
                        else
                        {
                            tcs.SetResult(0);
                        }
                    }, TaskScheduler.Default);
                }
                else if (method.ReturnType == typeof(Task<int>))
                {
                    result = (Task<int>)method.Invoke(null, argsToPass)!;
                }
                else
                {
                    throw new InvalidOperationException(SR.Format(SR.ReturnTypeNotSupportedForMain, method.ReturnType.FullName));
                }
                return result;
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException refEx && refEx.InnerException != null)
                    ex = refEx.InnerException;
                return Task.FromException<int>(ex);
            }
        }

        private static string GeneratedInitializerClassName = "System.Runtime.InteropServices.JavaScript.__GeneratedInitializer";
        private static string GeneratedInitializerMethodName = "__Register_";

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic access from JavaScript")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Dynamic access from JavaScript")]
        public static Task BindAssemblyExports(string? assemblyName)
        {
            try
            {
                if (string.IsNullOrEmpty(assemblyName))
                {
                    throw new MissingMethodException("Missing assembly name");
                }
                if (!assemblyName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                {
                    assemblyName += ".dll";
                }

                Assembly assembly = Assembly.LoadFrom(assemblyName);
                Type? type = assembly.GetType(GeneratedInitializerClassName);
                if (type == null)
                {
                    foreach (var module in assembly.Modules)
                    {
                        RuntimeHelpers.RunModuleConstructor(module.ModuleHandle);
                    }
                }
                else
                {
                    MethodInfo? methodInfo = type.GetMethod(GeneratedInitializerMethodName, BindingFlags.NonPublic | BindingFlags.Static);
                    methodInfo?.Invoke(null, []);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException refEx && refEx.InnerException != null)
                    ex = refEx.InnerException;
                return Task.FromException(ex);
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "TODO https://github.com/dotnet/runtime/issues/98366")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "TODO https://github.com/dotnet/runtime/issues/98366")]
        public static unsafe JSFunctionBinding BindManagedFunction(string fullyQualifiedName, int signatureHash, ReadOnlySpan<JSMarshalerType> signatures)
        {
            if (string.IsNullOrEmpty(fullyQualifiedName))
            {
                throw new ArgumentNullException(nameof(fullyQualifiedName));
            }

            var signature = GetMethodSignature(signatures, null, null);
            var (assemblyName, className, nameSpace, shortClassName, methodName) = ParseFQN(fullyQualifiedName);

            Assembly assembly = Assembly.LoadFrom(assemblyName + ".dll");
            Type? type = assembly.GetType(className);
            if (type == null)
            {
                throw new InvalidOperationException("Class not found " + className);
            }
            var wrapper_name = $"__Wrapper_{methodName}_{signatureHash}";
            var methodInfo = type.GetMethod(wrapper_name, BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Method not found " + wrapper_name);
            }

            var monoMethod = GetIntPtrFromMethodHandle(methodInfo.MethodHandle);

            JavaScriptImports.BindCSFunction(monoMethod, assemblyName, nameSpace, shortClassName, methodName, signatureHash, (IntPtr)signature.Header);

            FreeMethodSignatureBuffer(signature);

            return signature;
        }

#if FEATURE_WASM_MANAGED_THREADS
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "external_eventloop")]
        private static extern ref bool GetThreadExternalEventloop(Thread @this);

        public static void SetHasExternalEventLoop(Thread thread)
        {
            GetThreadExternalEventloop(thread) = true;
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr GetIntPtrFromMethodHandle(RuntimeMethodHandle methodHandle)
        {
            var temp = new IntPtrAndHandle { methodHandle = methodHandle };
            return temp.ptr;
        }


        public static (string assemblyName, string className, string nameSpace, string shortClassName, string methodName) ParseFQN(string fqn)
        {
            var assembly = fqn.Substring(fqn.IndexOf('[') + 1, fqn.IndexOf(']') - 1).Trim();
            fqn = fqn.Substring(fqn.IndexOf(']') + 1).Trim();
            var methodName = fqn.Substring(fqn.IndexOf(':') + 1);
            var className = fqn.Substring(0, fqn.IndexOf(':')).Trim();

            var nameSpace = "";
            var shortClassName = className;
            var idx = fqn.LastIndexOf(".");
            if (idx != -1)
            {
                nameSpace = fqn.Substring(0, idx);
                shortClassName = className.Substring(idx + 1);
            }

            if (string.IsNullOrEmpty(assembly))
                throw new InvalidOperationException("No assembly name specified " + fqn);
            if (string.IsNullOrEmpty(className))
                throw new InvalidOperationException("No class name specified " + fqn);
            if (string.IsNullOrEmpty(methodName))
                throw new InvalidOperationException("No method name specified " + fqn);
            return (assembly, className, nameSpace, shortClassName, methodName);
        }
    }
}
