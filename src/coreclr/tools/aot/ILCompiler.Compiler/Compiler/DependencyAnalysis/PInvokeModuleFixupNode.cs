// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Internal.Text;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using InteropDataConstants = Internal.Runtime.InteropDataConstants;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a single PInvoke ModuleFixupCell as defined in the core library.
    /// </summary>
    public class PInvokeModuleFixupNode : ObjectNode, ISymbolDefinitionNode
    {
        public readonly PInvokeModuleData _pInvokeModuleData;

        public PInvokeModuleFixupNode(PInvokeModuleData pInvokeModuleData)
        {
            _pInvokeModuleData = pInvokeModuleData;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("__nativemodule_");
            _pInvokeModuleData.AppendMangledName(nameMangler, sb);
        }
        public int Offset => 0;
        public override bool IsShareable => true;

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override ObjectNodeSection Section => ObjectNodeSection.DataSection;

        public override bool StaticDependenciesAreComputed => true;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialPointerAlignment();

            builder.AddSymbol(this);

            ISymbolNode nameSymbol = factory.ConstantUtf8String(_pInvokeModuleData.ModuleName);
            ISymbolNode moduleTypeSymbol = factory.NecessaryTypeSymbol(_pInvokeModuleData.DeclaringModule.GetGlobalModuleType());

            //
            // Emit a ModuleFixupCell struct
            //

            builder.EmitZeroPointer();
            builder.EmitPointerReloc(nameSymbol);
            builder.EmitPointerReloc(moduleTypeSymbol);

            uint dllImportSearchPath = 0;
            if (_pInvokeModuleData.DllImportSearchPath.HasValue)
            {
                dllImportSearchPath = (uint)_pInvokeModuleData.DllImportSearchPath.Value;
                Debug.Assert((dllImportSearchPath & InteropDataConstants.HasDllImportSearchPath) == 0);
                dllImportSearchPath |= InteropDataConstants.HasDllImportSearchPath;
            }
            builder.EmitInt((int)dllImportSearchPath);

            return builder.ToObjectData();
        }

        public override int ClassCode => 159930099;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return _pInvokeModuleData.CompareTo(((PInvokeModuleFixupNode)other)._pInvokeModuleData, comparer);
        }
    }

    public readonly struct PInvokeModuleData : IEquatable<PInvokeModuleData>
    {
        public readonly string ModuleName;
        public readonly DllImportSearchPath? DllImportSearchPath;
        public readonly ModuleDesc DeclaringModule;

        public PInvokeModuleData(string moduleName, DllImportSearchPath? dllImportSearchPath, ModuleDesc declaringModule)
        {
            ModuleName = moduleName;
            DllImportSearchPath = dllImportSearchPath;
            DeclaringModule = declaringModule;
        }

        public bool Equals(PInvokeModuleData other)
        {
            return DeclaringModule == other.DeclaringModule &&
                DllImportSearchPath == other.DllImportSearchPath &&
                ModuleName == other.ModuleName;
        }

        public override bool Equals(object obj)
        {
            return obj is PInvokeModuleData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ModuleName.GetHashCode() ^ DeclaringModule.GetHashCode();
        }

        public int CompareTo(PInvokeModuleData other, CompilerComparer comparer)
        {
            int result = StringComparer.Ordinal.Compare(ModuleName, other.ModuleName);
            if (result != 0)
                return result;

            result = comparer.Compare(DeclaringModule.GetGlobalModuleType(),
                other.DeclaringModule.GetGlobalModuleType());
            if (result != 0)
                return result;

            return Nullable.Compare(DllImportSearchPath, other.DllImportSearchPath);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledTypeName(DeclaringModule.GetGlobalModuleType()));
            sb.Append('_');
            sb.Append(nameMangler.SanitizeName(ModuleName));
            if (DllImportSearchPath.HasValue)
            {
                sb.Append('_');
                sb.Append(((int)DllImportSearchPath.Value).ToString());
            }
        }
    }
}
