// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public class ModulesSectionNode : ObjectNode, ISymbolDefinitionNode
    {
        // Each compilation unit produces one module. When all compilation units are linked
        // together in multifile mode, the runtime needs to get list of modules present
        // in the final binary. This list is created via a special .modules section that
        // contains list of pointers to all module headers.

        private TargetDetails _target;

        public ModulesSectionNode(TargetDetails target)
        {
            _target = target;
        }

        public override ObjectNodeSection Section
        {
            get
            {
                return _target.IsWindows ?
                    ObjectNodeSection.ModulesWindowsContentSection :
                    ObjectNodeSection.ModulesUnixContentSection;
            }
        }

        protected override string GetName(NodeFactory factory) => this.GetMangledName(factory.NameMangler);

        public override bool StaticDependenciesAreComputed => true;

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix).Append("__Module");
        }
        public int Offset => 0;
        public override bool IsShareable => false;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder objData = new ObjectDataBuilder(factory, relocsOnly);
            objData.RequireInitialPointerAlignment();
            objData.AddSymbol(this);
            objData.EmitPointerReloc(factory.ReadyToRunHeader);

            return objData.ToObjectData();
        }

        public override int ClassCode => -1225116970;
    }
}
