// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;

using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal sealed class AuxiliaryModuleMetadataNode : HeaderTableNode
    {
        public const int HeaderSize = 6 * sizeof(int);
        public const int EntrySize = 4 * sizeof(int);
        public const int EntryActivationStatusOffset = 3 * sizeof(int);

        private readonly string _primaryModuleFileName;

        /// <param name="primaryModuleFileName">The primary output file name, from which auxiliary module file names are derived.</param>
        public AuxiliaryModuleMetadataNode(string primaryModuleFileName)
        {
            Debug.Assert(primaryModuleFileName is not null);
            _primaryModuleFileName = primaryModuleFileName;
        }

        /// <summary>
        /// Gets the file name of the auxiliary module with the given index, relative to the primary module's directory.
        /// </summary>
        public static string GetAuxiliaryModuleFileName(string primaryModuleFileName, int auxiliaryModuleIndex) =>
            $"{Path.GetFileNameWithoutExtension(primaryModuleFileName)}.{auxiliaryModuleIndex + 1}.wasm";

        public static int GetActivationStatusOffset(int auxiliaryModuleIndex) =>
            HeaderSize + (auxiliaryModuleIndex * EntrySize) + EntryActivationStatusOffset;

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
            builder.EmitInt(0); // EntryPointsModuleActivationStatus
            builder.EmitInt(primaryMethodCount); // PrimaryModuleMethodCount
            builder.EmitInt(auxiliaryMethodCounts.Length); // AuxiliaryModuleCount

            byte[][] fileNames = new byte[auxiliaryMethodCounts.Length][];
            int fileNameOffset = HeaderSize + (auxiliaryMethodCounts.Length * EntrySize);
            int relativeTableBase = primaryMethodCount;
            for (int i = 0; i < auxiliaryMethodCounts.Length; i++)
            {
                fileNames[i] = Encoding.UTF8.GetBytes(GetAuxiliaryModuleFileName(_primaryModuleFileName, i));
                builder.EmitReloc(this, RelocType.IMAGE_REL_BASED_ADDR32NB, fileNameOffset); // FilenameRVA
                builder.EmitInt(relativeTableBase); // RelativeTableBase
                builder.EmitInt(auxiliaryMethodCounts[i]); // MethodCount
                builder.EmitInt(0); // ActivationStatus
                relativeTableBase += auxiliaryMethodCounts[i];
                fileNameOffset += fileNames[i].Length + 1;
            }

            foreach (byte[] fileName in fileNames)
            {
                builder.EmitBytes(fileName);
                builder.EmitByte(0);
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
