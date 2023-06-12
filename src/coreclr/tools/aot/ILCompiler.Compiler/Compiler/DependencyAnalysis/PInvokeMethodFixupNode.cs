// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using Internal.IL.Stubs;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a single PInvoke MethodFixupCell as defined in the core library.
    /// </summary>
    public class PInvokeMethodFixupNode : ObjectNode, ISymbolDefinitionNode
    {
        private readonly PInvokeMethodData _pInvokeMethodData;

        public PInvokeMethodFixupNode(PInvokeMethodData pInvokeMethodData)
        {
            _pInvokeMethodData = pInvokeMethodData;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__pinvoke_");
            _pInvokeMethodData.AppendMangledName(nameMangler, sb);
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection GetSection(NodeFactory factory) => ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();

            builder.AddSymbol(this);

            //
            // Emit a MethodFixupCell struct
            //

            // Address (to be fixed up at runtime)
            builder.EmitZeroPointer();

            // Entry point name
            string entryPointName = _pInvokeMethodData.EntryPointName;
            if (factory.Target.IsWindows && entryPointName.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                // Windows-specific ordinal import
                // CLR-compatible behavior: Strings that can't be parsed as a signed integer are treated as zero.
                int entrypointOrdinal;
                if (!int.TryParse(entryPointName.AsSpan(1), out entrypointOrdinal))
                    entrypointOrdinal = 0;

                // CLR-compatible behavior: Ordinal imports are 16-bit on Windows. Discard rest of the bits.
                builder.EmitNaturalInt((ushort)entrypointOrdinal);
            }
            else
            {
                // Import by name
                builder.EmitPointerReloc(factory.ConstantUtf8String(entryPointName));
            }

            // Module fixup cell
            builder.EmitPointerReloc(factory.PInvokeModuleFixup(_pInvokeMethodData.ModuleData));

            int flags = 0;

            int charsetFlags = (int)_pInvokeMethodData.CharSetMangling;
            Debug.Assert((charsetFlags & MethodFixupCellFlagsConstants.CharSetMask) == charsetFlags);
            charsetFlags &= MethodFixupCellFlagsConstants.CharSetMask;
            flags |= charsetFlags;

            int? objcFunction = MarshalHelpers.GetObjectiveCMessageSendFunction(factory.Target, _pInvokeMethodData.ModuleData.ModuleName, _pInvokeMethodData.EntryPointName);
            if (objcFunction.HasValue)
            {
                flags |= MethodFixupCellFlagsConstants.IsObjectiveCMessageSendMask;

                int objcFunctionFlags = objcFunction.Value << MethodFixupCellFlagsConstants.ObjectiveCMessageSendFunctionShift;
                Debug.Assert((objcFunctionFlags & MethodFixupCellFlagsConstants.ObjectiveCMessageSendFunctionMask) == objcFunctionFlags);
                objcFunctionFlags &= MethodFixupCellFlagsConstants.ObjectiveCMessageSendFunctionMask;
                flags |= objcFunctionFlags;
            }

            builder.EmitInt(flags);

            return builder.ToObjectData();
        }

        public override int ClassCode => -1592006940;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _pInvokeMethodData.CompareTo(((PInvokeMethodFixupNode)other)._pInvokeMethodData, comparer);
        }
    }

    public readonly struct PInvokeMethodData : IEquatable<PInvokeMethodData>
    {
        public readonly PInvokeModuleData ModuleData;
        public readonly string EntryPointName;
        public readonly CharSet CharSetMangling;

        public PInvokeMethodData(PInvokeLazyFixupField pInvokeLazyFixupField)
        {
            PInvokeMetadata metadata = pInvokeLazyFixupField.PInvokeMetadata;
            ModuleDesc declaringModule = ((MetadataType)pInvokeLazyFixupField.TargetMethod.OwningType).Module;

            CustomAttributeValue<TypeDesc>? decodedAttr = null;

            // Look for DefaultDllImportSearchPath on the method
            if (pInvokeLazyFixupField.TargetMethod is EcmaMethod method)
            {
                decodedAttr = method.GetDecodedCustomAttribute("System.Runtime.InteropServices", "DefaultDllImportSearchPathsAttribute");
            }

            // If the attribute it wasn't found on the method, look for it on the assembly
            if (!decodedAttr.HasValue && declaringModule.Assembly is EcmaAssembly asm)
            {
                // We look for [assembly:DefaultDllImportSearchPaths(...)]
                var attrHandle = asm.MetadataReader.GetCustomAttributeHandle(asm.AssemblyDefinition.GetCustomAttributes(),
                    "System.Runtime.InteropServices", "DefaultDllImportSearchPathsAttribute");
                if (!attrHandle.IsNil)
                {
                    var attr = asm.MetadataReader.GetCustomAttribute(attrHandle);
                    decodedAttr = attr.DecodeValue(new CustomAttributeTypeProvider(asm));
                }
            }

            DllImportSearchPath? dllImportSearchPath = default;
            if (decodedAttr.HasValue
                && decodedAttr.Value.FixedArguments.Length == 1
                && decodedAttr.Value.FixedArguments[0].Value is int searchPath)
            {
                dllImportSearchPath = (DllImportSearchPath)searchPath;
            }

            ModuleData = new PInvokeModuleData(metadata.Module, dllImportSearchPath, declaringModule);

            EntryPointName = metadata.Name;

            CharSet charSetMangling = default;
            if (declaringModule.Context.Target.IsWindows && !metadata.Flags.ExactSpelling)
            {
                // Mirror CharSet normalization from Marshaller.CreateMarshaller
                bool isAnsi = metadata.Flags.CharSet switch
                {
                    CharSet.Ansi => true,
                    CharSet.Unicode => false,
                    CharSet.Auto => false,
                    _ => true
                };

                charSetMangling = isAnsi ? CharSet.Ansi : CharSet.Unicode;
            }
            CharSetMangling = charSetMangling;
        }

        public bool Equals(PInvokeMethodData other)
        {
            return ModuleData.Equals(other.ModuleData) &&
                EntryPointName == other.EntryPointName &&
                CharSetMangling == other.CharSetMangling;
        }

        public override bool Equals(object obj)
        {
            return obj is PInvokeMethodData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ModuleData.GetHashCode() ^ EntryPointName.GetHashCode();
        }

        public int CompareTo(PInvokeMethodData other, CompilerComparer comparer)
        {
            var entryPointCompare = StringComparer.Ordinal.Compare(EntryPointName, other.EntryPointName);
            if (entryPointCompare != 0)
                return entryPointCompare;

            var moduleCompare = ModuleData.CompareTo(other.ModuleData, comparer);
            if (moduleCompare != 0)
                return moduleCompare;

            return CharSetMangling.CompareTo(other.CharSetMangling);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            ModuleData.AppendMangledName(nameMangler, sb);
            sb.Append("__");
            sb.Append(EntryPointName);
            if (CharSetMangling != default)
            {
                sb.Append("__");
                sb.Append(CharSetMangling.ToString());
            }
        }
    }
}
