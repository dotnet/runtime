// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class AuxiliaryModuleMetadataNode : HeaderTableNode
    {
        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder builder)
        {
            builder.Append(nameMangler.CompilationUnitPrefix);
            builder.Append("__ReadyToRunAuxiliaryModuleMetadata"u8);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            // Method counts are only final after graph analysis, and enumerating compiled methods
            // during marking would fix the RuntimeFunctions order too early.
            if (relocsOnly)
                return new ObjectData([], [], 1, [this]);

            (int primaryMethodCount, int[] auxiliaryMethodCounts) = CountMethods(factory);

            ObjectDataBuilder builder = new ObjectDataBuilder(factory, relocsOnly);
            builder.RequireInitialAlignment(sizeof(uint));
            builder.AddSymbol(this);
            builder.EmitInt(0); // EntryPoints filename RVA
            builder.EmitInt(0); // InstanceMethodEntryPointsRVA
            builder.EmitInt(0); // MethodDefEntryPointsInfoRVA
            builder.EmitInt(1); // EntryPointsModuleActivationStatus
            builder.EmitInt(primaryMethodCount); // PrimaryModuleMethodCount
            builder.EmitInt(auxiliaryMethodCounts.Length); // AuxiliaryModuleCount

            int relativeTableBase = primaryMethodCount;
            foreach (int methodCount in auxiliaryMethodCounts)
            {
                builder.EmitInt(0); // FilenameRVA
                builder.EmitInt(relativeTableBase); // RelativeTableBase
                builder.EmitInt(methodCount); // MethodCount
                builder.EmitInt(0); // ActivationStatus
                relativeTableBase += methodCount;
            }

            return builder.ToObjectData();
        }

        /// <summary>
        /// Counts RuntimeFunctions entries (including funclets) for the primary module and each auxiliary module.
        /// Each module must occupy a contiguous range of the RuntimeFunctions table, with the primary module first.
        /// </summary>
        private static (int PrimaryMethodCount, int[] AuxiliaryMethodCounts) CountMethods(NodeFactory factory)
        {
            int primaryMethodCount = 0;
            int primaryColdMethodCount = 0;
            int auxiliaryMethodCount = 0;
            foreach (MethodWithGCInfo method in factory.EnumerateCompiledMethods())
            {
                if (factory.IsAuxiliaryMethod(method))
                {
                    Debug.Assert(method.ColdCodeNode is null);
                    auxiliaryMethodCount = checked(auxiliaryMethodCount + method.FrameInfos.Length);
                }
                else
                {
                    Debug.Assert(auxiliaryMethodCount == 0, "Primary methods must precede auxiliary methods in the RuntimeFunctions table.");
                    primaryMethodCount = checked(primaryMethodCount + method.FrameInfos.Length);
                    if (method.ColdCodeNode is not null)
                        primaryColdMethodCount = checked(primaryColdMethodCount + method.ColdFrameInfos.Length);
                }
            }

            // RuntimeFunctionsTableNode emits all cold entries after all hot entries, so cold code would land
            // after the auxiliary ranges instead of inside the primary range. Cold code is only valid primary-only.
            Debug.Assert(auxiliaryMethodCount == 0 || primaryColdMethodCount == 0,
                "Hot/cold splitting is not supported together with auxiliary modules.");
            primaryMethodCount = checked(primaryMethodCount + primaryColdMethodCount);

            int[] auxiliaryMethodCounts = auxiliaryMethodCount == 0 ? [] : [auxiliaryMethodCount];
            return (primaryMethodCount, auxiliaryMethodCounts);
        }

        public override int ClassCode => 193275842;
    }
}
