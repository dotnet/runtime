// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Runtime.Assemblies;
using System.Reflection.Runtime.Dispensers;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.PropertyInfos;
using System.Reflection.Runtime.TypeInfos;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

//=================================================================================================================
// This file collects the various chokepoints that create the various Runtime*Info objects. This allows
// easy reviewing of the overall caching and unification policy.
//
// The dispenser functions are defined as static members of the associated Info class. This permits us
// to keep the constructors private to ensure that these really are the only ways to obtain these objects.
//=================================================================================================================

namespace System.Reflection.Runtime.Assemblies
{
    //-----------------------------------------------------------------------------------------------------------
    // Assemblies (maps 1-1 with a MetadataReader/ScopeDefinitionHandle.
    //-----------------------------------------------------------------------------------------------------------
    internal partial class RuntimeAssemblyInfo
    {
        /// <summary>
        /// Returns non-null or throws.
        /// </summary>
        internal static RuntimeAssembly GetRuntimeAssembly(RuntimeAssemblyName assemblyRefName)
        {
            Exception assemblyLoadException = TryGetRuntimeAssembly(assemblyRefName, out RuntimeAssemblyInfo result);
            if (assemblyLoadException != null)
                throw assemblyLoadException;
            return result;
        }

        /// <summary>
        /// Returns non-null or throws.
        /// </summary>
        internal static RuntimeAssembly GetRuntimeAssemblyFromByteArray(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> pdbSymbolStore)
        {
            AssemblyBinder binder = ReflectionCoreExecution.ExecutionEnvironment.AssemblyBinder;
            if (!binder.Bind(rawAssembly, pdbSymbolStore, out AssemblyBindResult bindResult, out Exception exception))
            {
                if (exception != null)
                    throw exception;
                else
                    throw new BadImageFormatException();
            }

            RuntimeAssembly result = GetRuntimeAssembly(bindResult);
            return result;
        }

        /// <summary>
        /// Returns non-null or throws.
        /// </summary>
        internal static RuntimeAssembly GetRuntimeAssemblyFromPath(string assemblyPath)
        {
            AssemblyBinder binder = ReflectionCoreExecution.ExecutionEnvironment.AssemblyBinder;
            if (!binder.Bind(assemblyPath, out AssemblyBindResult bindResult, out Exception exception))
            {
                if (exception != null)
                    throw exception;
                else
                    throw new BadImageFormatException();
            }

            RuntimeAssembly result = GetRuntimeAssembly(bindResult, assemblyPath);
            return result;
        }

        /// <summary>
        /// Returns null if no assembly matches the assemblyRefName. Throws for other error cases.
        /// </summary>
        internal static RuntimeAssemblyInfo GetRuntimeAssemblyIfExists(RuntimeAssemblyName assemblyRefName)
        {
            object runtimeAssemblyOrException = s_assemblyRefNameToAssemblyDispenser.GetOrAdd(assemblyRefName);
            if (runtimeAssemblyOrException is RuntimeAssemblyInfo runtimeAssembly)
                return runtimeAssembly;
            return null;
        }

        internal static Exception TryGetRuntimeAssembly(RuntimeAssemblyName assemblyRefName, out RuntimeAssemblyInfo result)
        {
            object runtimeAssemblyOrException = s_assemblyRefNameToAssemblyDispenser.GetOrAdd(assemblyRefName);
            if (runtimeAssemblyOrException is RuntimeAssemblyInfo runtimeAssembly)
            {
                result = runtimeAssembly;
                return null;
            }
            else
            {
                result = null;
                return (Exception)runtimeAssemblyOrException;
            }
        }

        // The "object" here is either a RuntimeAssembly or an Exception.
        private static readonly Dispenser<RuntimeAssemblyName, object> s_assemblyRefNameToAssemblyDispenser =
            DispenserFactory.CreateDispenser<RuntimeAssemblyName, object>(
                DispenserScenario.AssemblyRefName_Assembly,
                delegate (RuntimeAssemblyName assemblyRefName)
                {
                    AssemblyBinder binder = ReflectionCoreExecution.ExecutionEnvironment.AssemblyBinder;
                    if (!binder.Bind(assemblyRefName, cacheMissedLookups: true, out AssemblyBindResult bindResult, out Exception exception))
                        return exception;

                    return GetRuntimeAssembly(bindResult);
                }
        );

        private static RuntimeAssembly GetRuntimeAssembly(AssemblyBindResult bindResult, string assemblyPath = null)
        {
            RuntimeAssembly? result = null;

            GetNativeFormatRuntimeAssembly(bindResult, ref result);
            if (result != null)
                return result;

            GetEcmaRuntimeAssembly(bindResult, assemblyPath, ref result);
            if (result != null)
                return result;

            throw new PlatformNotSupportedException();
        }

        // Use C# partial method feature to avoid complex #if logic, whichever code files are included will drive behavior
        static partial void GetNativeFormatRuntimeAssembly(AssemblyBindResult bindResult, ref RuntimeAssembly? runtimeAssembly);
        static partial void GetEcmaRuntimeAssembly(AssemblyBindResult bindResult, string assemblyPath, ref RuntimeAssembly? runtimeAssembly);
    }
}

namespace System.Reflection.Runtime.MethodInfos
{
    //-----------------------------------------------------------------------------------------------------------
    // ConstructorInfos
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimePlainConstructorInfo<TRuntimeMethodCommon> : RuntimeConstructorInfo
    {
        internal static RuntimePlainConstructorInfo<TRuntimeMethodCommon> GetRuntimePlainConstructorInfo(TRuntimeMethodCommon common)
        {
            return new RuntimePlainConstructorInfo<TRuntimeMethodCommon>(common);
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // Constructors for array types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeSyntheticConstructorInfo : RuntimeConstructorInfo
    {
        internal static RuntimeSyntheticConstructorInfo GetRuntimeSyntheticConstructorInfo(SyntheticMethodId syntheticMethodId, RuntimeArrayTypeInfo declaringType, RuntimeTypeInfo[] runtimeParameterTypes, InvokerOptions options, CustomMethodInvokerAction action)
        {
            return new RuntimeSyntheticConstructorInfo(syntheticMethodId, declaringType, runtimeParameterTypes, options, action);
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // MethodInfos for method definitions (i.e. Foo.Moo() or Foo.Moo<>() but not Foo.Moo<int>)
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeNamedMethodInfo<TRuntimeMethodCommon>
    {
        internal static RuntimeNamedMethodInfo<TRuntimeMethodCommon> GetRuntimeNamedMethodInfo(TRuntimeMethodCommon common, RuntimeTypeInfo reflectedType)
        {
            RuntimeNamedMethodInfo<TRuntimeMethodCommon> method = new RuntimeNamedMethodInfo<TRuntimeMethodCommon>(common, reflectedType);
            method.WithDebugName();
            return method;
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // MethodInfos for constructed generic methods (Foo.Moo<int> but not Foo.Moo<>)
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeConstructedGenericMethodInfo : RuntimeMethodInfo
    {
        internal static RuntimeMethodInfo GetRuntimeConstructedGenericMethodInfo(RuntimeNamedMethodInfo genericMethodDefinition, RuntimeTypeInfo[] genericTypeArguments)
        {
            return new RuntimeConstructedGenericMethodInfo(genericMethodDefinition, genericTypeArguments).WithDebugName();
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // MethodInfos for the Get/Set methods on array types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeSyntheticMethodInfo : RuntimeMethodInfo
    {
        internal static RuntimeMethodInfo GetRuntimeSyntheticMethodInfo(SyntheticMethodId syntheticMethodId, string name, RuntimeArrayTypeInfo declaringType, RuntimeTypeInfo[] runtimeParameterTypes, RuntimeTypeInfo returnType, InvokerOptions options, CustomMethodInvokerAction action)
        {
            return new RuntimeSyntheticMethodInfo(syntheticMethodId, name, declaringType, runtimeParameterTypes, returnType, options, action).WithDebugName();
        }
    }
}

namespace System.Reflection.Runtime.ParameterInfos
{
    //-----------------------------------------------------------------------------------------------------------
    // ParameterInfos for MethodBase objects with no Parameter metadata.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeThinMethodParameterInfo : RuntimeMethodParameterInfo
    {
        internal static RuntimeThinMethodParameterInfo GetRuntimeThinMethodParameterInfo(MethodBase member, int position, QSignatureTypeHandle qualifiedParameterType, TypeContext typeContext)
        {
            return new RuntimeThinMethodParameterInfo(member, position, qualifiedParameterType, typeContext);
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // ParameterInfos returned by PropertyInfo.GetIndexParameters()
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimePropertyIndexParameterInfo : RuntimeParameterInfo
    {
        internal static RuntimePropertyIndexParameterInfo GetRuntimePropertyIndexParameterInfo(RuntimePropertyInfo member, RuntimeParameterInfo backingParameter)
        {
            return new RuntimePropertyIndexParameterInfo(member, backingParameter);
        }
    }

    //-----------------------------------------------------------------------------------------------------------
    // ParameterInfos returned by Get/Set methods on array types.
    //-----------------------------------------------------------------------------------------------------------
    internal sealed partial class RuntimeSyntheticParameterInfo : RuntimeParameterInfo
    {
        internal static RuntimeSyntheticParameterInfo GetRuntimeSyntheticParameterInfo(MemberInfo member, int position, RuntimeTypeInfo parameterType)
        {
            return new RuntimeSyntheticParameterInfo(member, position, parameterType);
        }
    }
}
