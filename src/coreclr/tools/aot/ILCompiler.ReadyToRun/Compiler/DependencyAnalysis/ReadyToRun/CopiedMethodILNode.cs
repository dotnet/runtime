// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

using Internal.Text;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class CopiedMethodILNode : ObjectNode, ISymbolDefinitionNode
    {
        EcmaMethod _method;

        public CopiedMethodILNode(EcmaMethod method)
        {
            Debug.Assert(!method.IsAbstract);

            _method = method.GetTypicalMethodDefinition();
        }

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override bool IsShareable => false;

        public override bool StaticDependenciesAreComputed => true;

        public int Offset => 0;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);
        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("ILMethod_"u8);
            sb.Append(nameMangler.GetMangledMethodName(_method));
        }

        // Minimal IL method body with tiny header and single 'ret' instruction.
        // Format: 0x06 = tiny header with code size 1, 0x2A = ret opcode
        private static readonly byte[] s_stubMethodBody = new byte[] { 0x06, 0x2A };

        /// <summary>
        /// Checks if an assembly is an SDK/platform assembly that should preserve IL bodies.
        /// SDK assemblies may have methods called via reflection by the runtime or platform.
        /// </summary>
        private static bool IsSdkAssembly(string assemblyName)
        {
            // .NET SDK assemblies
            if (assemblyName.StartsWith("System.", StringComparison.Ordinal) ||
                assemblyName.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                assemblyName == "System" ||
                assemblyName == "mscorlib" ||
                assemblyName == "netstandard" ||
                assemblyName == "WindowsBase" ||
                assemblyName == "PresentationCore" ||
                assemblyName == "PresentationFramework")
            {
                return true;
            }

            // Xamarin/MAUI platform assemblies
            if (assemblyName.StartsWith("Xamarin.", StringComparison.Ordinal) ||
                assemblyName.StartsWith("Mono.", StringComparison.Ordinal))
            {
                return true;
            }

            // Common NuGet packages that may use reflection
            if (assemblyName.StartsWith("Newtonsoft.", StringComparison.Ordinal) ||
                assemblyName.StartsWith("NuGet.", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a method requires preserving the IL body.
        /// Only void methods with no return value can safely use a simple 'ret' stub.
        /// Methods in SDK/platform assemblies or with reflection-related attributes
        /// may be called via reflection or native callbacks at runtime.
        /// </summary>
        private bool ShouldPreserveILBody()
        {
            // Only strip void methods - the ret stub doesn't return a valid value for non-void methods
            if (!_method.Signature.ReturnType.IsVoid)
                return true;

            // Preserve methods marked with UnmanagedCallersOnly - they're exported to native code
            if (_method.IsUnmanagedCallersOnly)
                return true;

            // Preserve all methods in SDK/platform assemblies - only strip user app assemblies
            // string assemblyName = _method.Module.Assembly.GetName().Name;
            // if (IsSdkAssembly(assemblyName))
            // {
            //     return true;
            // }

            // Check for MonoPInvokeCallback and Preserve attributes (used by Xamarin/MAUI)
            // These can be in any namespace, so we check by name only
            var metadataReader = _method.MetadataReader;
            var methodDef = metadataReader.GetMethodDefinition(_method.Handle);

            foreach (var attributeHandle in methodDef.GetCustomAttributes())
            {
                if (!metadataReader.GetAttributeNamespaceAndName(attributeHandle, out _, out var nameHandle))
                    continue;

                string attributeName = metadataReader.GetString(nameHandle);
                if (attributeName == "MonoPInvokeCallbackAttribute" ||
                    attributeName == "PreserveAttribute" ||
                    attributeName == "DynamicDependencyAttribute" ||
                    attributeName == "BindingImplAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            if (relocsOnly)
            {
                return new ObjectData(
                    data: Array.Empty<byte>(),
                    relocs: Array.Empty<Relocation>(),
                    alignment: 1,
                    definedSymbols: new ISymbolDefinitionNode[] { this });
            }

            // When stripping IL bodies, emit a minimal stub method body instead of the original IL.
            // This significantly reduces the size of component assemblies in composite R2R mode
            // on platforms that don't need IL for runtime execution (e.g., Apple mobile).
            // However, preserve IL bodies for methods with reflection-related attributes.
            if (factory.OptimizationFlags.StripILBodies && !ShouldPreserveILBody())
            {
                return new ObjectData(new byte[] { 0x06, 0x2A }, Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
            }

            var rva = _method.MetadataReader.GetMethodDefinition(_method.Handle).RelativeVirtualAddress;
            var reader = _method.Module.PEReader.GetSectionData(rva).GetReader();
            int size = MethodBodyBlock.Create(reader).Size;

            return new ObjectData(reader.ReadBytes(size), Array.Empty<Relocation>(), 4, new ISymbolDefinitionNode[] { this });
        }

        public override int ClassCode => 541651465;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return comparer.Compare(_method, ((CopiedMethodILNode)other)._method);
        }
    }
}
