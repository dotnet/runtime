// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

namespace ILCompiler.DependencyAnalysis
{
    internal static class ReflectionInvokeSupportDependencyAlgorithm
    {
        // Inserts dependencies to make the following corner case work (we need MethodTable for `MyStruct[]`):
        //
        // struct MyStruct
        // {
        //     public static int Count(params MyStruct[] myStructs)
        //     {
        //         return myStructs.Length;
        //     }
        //
        //     public static void Main()
        //     {
        //         typeof(MyStruct).InvokeMember(nameof(Count), BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, null, new object[] { default(MyStruct) });
        //     }
        // }
        public static void GetDependenciesFromParamsArray(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            MethodSignature sig = method.Signature;
            if (sig.Length < 1 || !sig[sig.Length - 1].IsArray)
                return;

            if (method.GetTypicalMethodDefinition() is not EcmaMethod ecmaMethod)
                return;

            MetadataReader reader = ecmaMethod.MetadataReader;
            MethodDefinition methodDef = reader.GetMethodDefinition(ecmaMethod.Handle);

            foreach (ParameterHandle paramHandle in methodDef.GetParameters())
            {
                Parameter param = reader.GetParameter(paramHandle);
                if (param.SequenceNumber == sig.Length /* SequenceNumber is 1-based */)
                {
                    if (!reader.GetCustomAttributeHandle(param.GetCustomAttributes(), "System", "ParamArrayAttribute").IsNil)
                    {
                        dependencies ??= new DependencyList();
                        dependencies.Add(
                            factory.ConstructedTypeSymbol(sig[sig.Length - 1].NormalizeInstantiation()),
                            "Reflection invoke");
                    }

                    break;
                }
            }
        }
    }
}
