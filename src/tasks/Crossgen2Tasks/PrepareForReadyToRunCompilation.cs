// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class PrepareForReadyToRunCompilation : TaskBase
    {
        [Required]
        public ITaskItem MainAssembly { get; set; }
        public ITaskItem[] Assemblies { get; set; }
        public string[] ExcludeList { get; set; }
        public bool EmitSymbols { get; set; }
        public bool ReadyToRunUseCrossgen2 { get; set; }
        public bool Crossgen2Composite { get; set; }

        [Required]
        public string OutputPath { get; set; }
        [Required]
        public bool IncludeSymbolsInSingleFile { get; set; }

        public string[] PublishReadyToRunCompositeExclusions { get; set; }

        // When specified, only these assemblies will be fully compiled into the composite image.
        // All other input (non-reference) assemblies will only have code compiled for methods
        // called by a method in a rooted assembly (possibly transitively).
        public string[] PublishReadyToRunCompositeRoots { get; set; }

        public ITaskItem CrossgenTool { get; set; }
        public ITaskItem Crossgen2Tool { get; set; }

        // Output lists of files to compile. Currently crossgen has to run in two steps, the first to generate the R2R image
        // and the second to create native PDBs for the compiled images (the output of the first step is an input to the second step)
        [Output]
        public ITaskItem[] ReadyToRunCompileList => _compileList.ToArray();
        [Output]
        public ITaskItem[] ReadyToRunSymbolsCompileList => _symbolsCompileList.ToArray();

        // Output files to publish after compilation. These lists are equivalent to the input list, but contain the new
        // paths to the compiled R2R images and native PDBs.
        [Output]
        public ITaskItem[] ReadyToRunFilesToPublish => _r2rFiles.ToArray();

        [Output]
        public ITaskItem[] ReadyToRunAssembliesToReference => _r2rReferences.ToArray();

        [Output]
        public ITaskItem[] ReadyToRunCompositeBuildReferences => _r2rCompositeReferences.ToArray();

        [Output]
        public ITaskItem[] ReadyToRunCompositeBuildInput => _r2rCompositeInput.ToArray();

        [Output]
        public ITaskItem[] ReadyToRunCompositeUnrootedBuildInput => _r2rCompositeUnrootedInput.ToArray();

        private bool _crossgen2IsVersion5;
        private int _perfmapFormatVersion;

        private List<ITaskItem> _compileList = new();
        private List<ITaskItem> _symbolsCompileList = new();
        private List<ITaskItem> _r2rFiles = new();
        private List<ITaskItem> _r2rReferences = new();
        private List<ITaskItem> _r2rCompositeReferences = new();
        private List<ITaskItem> _r2rCompositeInput = new();
        private List<ITaskItem> _r2rCompositeUnrootedInput = new();

        private bool IsTargetWindows
        {
            get
            {
                // Crossgen2 V6 and above always has TargetOS metadata available
                if (ReadyToRunUseCrossgen2 && !string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)))
                    return Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "windows";
                else
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
        }

        private bool IsTargetLinux
        {
            get
            {
                // Crossgen2 V6 and above always has TargetOS metadata available
                if (ReadyToRunUseCrossgen2 && !string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)))
                    return Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "linux";
                else
                    return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            }
        }

        protected override void ExecuteCore()
        {
            if (ReadyToRunUseCrossgen2)
            {
                string isVersion5 = Crossgen2Tool.GetMetadata(MetadataKeys.IsVersion5);
                _crossgen2IsVersion5 = !string.IsNullOrEmpty(isVersion5) && bool.Parse(isVersion5);

                string perfmapVersion = Crossgen2Tool.GetMetadata(MetadataKeys.PerfmapFormatVersion);
                _perfmapFormatVersion = !string.IsNullOrEmpty(perfmapVersion) ? int.Parse(perfmapVersion) : 0;

                if (Crossgen2Composite && EmitSymbols && _crossgen2IsVersion5)
                {
                    Log.LogError(Strings.Crossgen5CannotEmitSymbolsInCompositeMode);
                    return;
                }
            }

            string diaSymReaderPath = CrossgenTool?.GetMetadata(MetadataKeys.DiaSymReader);

            bool hasValidDiaSymReaderLib =
                ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5 ||
                !string.IsNullOrEmpty(diaSymReaderPath) && File.Exists(diaSymReaderPath);

            // Process input lists of files
            ProcessInputFileList(Assemblies, _compileList, _symbolsCompileList, _r2rFiles, _r2rReferences, _r2rCompositeReferences, _r2rCompositeInput, _r2rCompositeUnrootedInput, hasValidDiaSymReaderLib);
        }

        private void ProcessInputFileList(
            ITaskItem[] inputFiles,
            List<ITaskItem> imageCompilationList,
            List<ITaskItem> symbolsCompilationList,
            List<ITaskItem> r2rFilesPublishList,
            List<ITaskItem> r2rReferenceList,
            List<ITaskItem> r2rCompositeReferenceList,
            List<ITaskItem> r2rCompositeInputList,
            List<ITaskItem> r2rCompositeUnrootedInput,
            bool hasValidDiaSymReaderLib)
        {
            if (inputFiles == null)
            {
                return;
            }

            var exclusionSet = ExcludeList == null || Crossgen2Composite ? null : new HashSet<string>(ExcludeList, StringComparer.OrdinalIgnoreCase);
            var compositeExclusionSet = PublishReadyToRunCompositeExclusions == null || !Crossgen2Composite ? null : new HashSet<string>(PublishReadyToRunCompositeExclusions, StringComparer.OrdinalIgnoreCase);
            var compositeRootSet = PublishReadyToRunCompositeRoots == null || !Crossgen2Composite ? null : new HashSet<string>(PublishReadyToRunCompositeRoots, StringComparer.OrdinalIgnoreCase);

            foreach (var file in inputFiles)
            {
                var eligibility = GetInputFileEligibility(file, Crossgen2Composite, exclusionSet, compositeExclusionSet, compositeRootSet);

                if (eligibility.NoEligibility)
                {
                    continue;
                }

                if (eligibility.IsReference)
                    r2rReferenceList.Add(file);

                if (eligibility.IsReference && !eligibility.ReferenceHiddenFromCompositeBuild && !eligibility.Compile)
                    r2rCompositeReferenceList.Add(file);

                if (!eligibility.Compile)
                {
                    continue;
                }

                var outputR2RImageRelativePath = file.GetMetadata(MetadataKeys.RelativePath);
                var outputR2RImage = Path.Combine(OutputPath, outputR2RImageRelativePath);

                string outputPDBImage = null;
                string outputPDBImageRelativePath = null;
                string crossgen1CreatePDBCommand = null;

                if (EmitSymbols)
                {
                    if (IsTargetWindows)
                    {
                        if (hasValidDiaSymReaderLib)
                        {
                            outputPDBImage = Path.ChangeExtension(outputR2RImage, "ni.pdb");
                            outputPDBImageRelativePath = Path.ChangeExtension(outputR2RImageRelativePath, "ni.pdb");
                            crossgen1CreatePDBCommand = $"/CreatePDB \"{Path.GetDirectoryName(outputPDBImage)}\"";
                        }
                    }
                    else if ((ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5) || IsTargetLinux)
                    {
                        string perfmapExtension;
                        if (ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5 && _perfmapFormatVersion >= 1)
                        {
                            perfmapExtension = ".ni.r2rmap";
                        }
                        else
                        {
                            using (FileStream fs = new(file.ItemSpec, FileMode.Open, FileAccess.Read))
                            {
                                PEReader pereader = new(fs);
                                MetadataReader mdReader = pereader.GetMetadataReader();
                                Guid mvid = mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid);
                                perfmapExtension = ".ni.{" + mvid + "}.map";
                            }
                        }

                        outputPDBImage = Path.ChangeExtension(outputR2RImage, perfmapExtension);
                        outputPDBImageRelativePath = Path.ChangeExtension(outputR2RImageRelativePath, perfmapExtension);
                        crossgen1CreatePDBCommand = $"/CreatePerfMap \"{Path.GetDirectoryName(outputPDBImage)}\"";
                    }
                }

                if (eligibility.CompileSeparately)
                {
                    // This TaskItem is the IL->R2R entry, for an input assembly that needs to be compiled into a R2R image. This will be used as
                    // an input to the ReadyToRunCompiler task
                    TaskItem r2rCompilationEntry = new(file);
                    r2rCompilationEntry.SetMetadata(MetadataKeys.OutputR2RImage, outputR2RImage);
                    if (outputPDBImage != null && ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5)
                    {
                        r2rCompilationEntry.SetMetadata(MetadataKeys.EmitSymbols, "true");
                        r2rCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, outputPDBImage);
                    }
                    r2rCompilationEntry.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                    imageCompilationList.Add(r2rCompilationEntry);
                }
                else if (eligibility.CompileUnrootedIntoCompositeImage)
                {
                    r2rCompositeUnrootedInput.Add(file);
                }
                else if (eligibility.CompileIntoCompositeImage)
                {
                    r2rCompositeInputList.Add(file);
                }

                // This TaskItem corresponds to the output R2R image. It is equivalent to the input TaskItem, only the ItemSpec for it points to the new path
                // for the newly created R2R image
                TaskItem r2rFileToPublish = new(file)
                {
                    ItemSpec = outputR2RImage
                };
                r2rFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                r2rFilesPublishList.Add(r2rFileToPublish);

                // Note: ReadyToRun PDB/Map files are not needed for debugging. They are only used for profiling, therefore the default behavior is to not generate them
                // unless an explicit PublishReadyToRunEmitSymbols flag is enabled by the app developer. There is also another way to profile that the runtime supports, which does
                // not rely on the native PDBs/Map files, so creating them is really an opt-in option, typically used by advanced users.
                // For debugging, only the IL PDBs are required.
                if (eligibility.CompileSeparately && outputPDBImage != null)
                {
                    if (!ReadyToRunUseCrossgen2 || _crossgen2IsVersion5)
                    {
                        // This TaskItem is the R2R->R2RPDB entry, for a R2R image that was just created, and for which we need to create native PDBs. This will be used as
                        // an input to the ReadyToRunCompiler task
                        TaskItem pdbCompilationEntry = new(file)
                        {
                            ItemSpec = outputR2RImage
                        };
                        pdbCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, outputPDBImage);
                        pdbCompilationEntry.SetMetadata(MetadataKeys.CreatePDBCommand, crossgen1CreatePDBCommand);
                        symbolsCompilationList.Add(pdbCompilationEntry);
                    }

                    // This TaskItem corresponds to the output PDB image. It is equivalent to the input TaskItem, only the ItemSpec for it points to the new path
                    // for the newly created PDB image.
                    TaskItem r2rSymbolsFileToPublish = new(file)
                    {
                        ItemSpec = outputPDBImage
                    };
                    r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.RelativePath, outputPDBImageRelativePath);
                    r2rSymbolsFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                    if (!IncludeSymbolsInSingleFile)
                    {
                        r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.ExcludeFromSingleFile, "true");
                    }

                    r2rFilesPublishList.Add(r2rSymbolsFileToPublish);
                }
            }

            if (Crossgen2Composite)
            {
                MainAssembly.SetMetadata(MetadataKeys.RelativePath, Path.GetFileName(MainAssembly.ItemSpec));

                var compositeR2RImageRelativePath = MainAssembly.GetMetadata(MetadataKeys.RelativePath);
                compositeR2RImageRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, "r2r" + Path.GetExtension(compositeR2RImageRelativePath));
                var compositeR2RImage = Path.Combine(OutputPath, compositeR2RImageRelativePath);

                TaskItem r2rCompilationEntry = new(MainAssembly)
                {
                    ItemSpec = r2rCompositeInputList[0].ItemSpec
                };
                r2rCompilationEntry.SetMetadata(MetadataKeys.OutputR2RImage, compositeR2RImage);
                r2rCompilationEntry.SetMetadata(MetadataKeys.CreateCompositeImage, "true");
                r2rCompilationEntry.RemoveMetadata(MetadataKeys.OriginalItemSpec);

                if (EmitSymbols)
                {
                    string compositePDBImage = null;
                    string compositePDBRelativePath = null;
                    if (IsTargetWindows)
                    {
                        if (hasValidDiaSymReaderLib)
                        {
                            compositePDBImage = Path.ChangeExtension(compositeR2RImage, ".ni.pdb");
                            compositePDBRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, ".ni.pdb");
                        }
                    }
                    else
                    {
                        string perfmapExtension = (_perfmapFormatVersion >= 1 ? ".ni.r2rmap" : ".ni.{composite}.map");
                        compositePDBImage = Path.ChangeExtension(compositeR2RImage, perfmapExtension);
                        compositePDBRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, perfmapExtension);
                    }

                    if (compositePDBImage != null && ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5)
                    {
                        r2rCompilationEntry.SetMetadata(MetadataKeys.EmitSymbols, "true");
                        r2rCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, compositePDBImage);

                        // Publish composite PDB file
                        TaskItem r2rSymbolsFileToPublish = new(MainAssembly)
                        {
                            ItemSpec = compositePDBImage
                        };
                        r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.RelativePath, compositePDBRelativePath);
                        r2rSymbolsFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                        if (!IncludeSymbolsInSingleFile)
                        {
                            r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.ExcludeFromSingleFile, "true");
                        }

                        r2rFilesPublishList.Add(r2rSymbolsFileToPublish);
                    }
                }

                imageCompilationList.Add(r2rCompilationEntry);

                // Publish it
                TaskItem compositeR2RFileToPublish = new(MainAssembly)
                {
                    ItemSpec = compositeR2RImage
                };
                compositeR2RFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                compositeR2RFileToPublish.SetMetadata(MetadataKeys.RelativePath, compositeR2RImageRelativePath);
                r2rFilesPublishList.Add(compositeR2RFileToPublish);
            }
        }

        private struct Eligibility
        {
            [Flags]
            private enum EligibilityEnum
            {
                None = 0,
                Reference = 1,
                HideReferenceFromComposite = 2,
                CompileSeparately = 4,
                CompileIntoCompositeImage = 8,
                CompileUnrootedIntoCompositeImage = 16,
            }

            private readonly EligibilityEnum _flags;

            public static Eligibility None => new(EligibilityEnum.None);

            public bool NoEligibility => _flags == EligibilityEnum.None;
            public bool IsReference => (_flags & EligibilityEnum.Reference) == EligibilityEnum.Reference;
            public bool ReferenceHiddenFromCompositeBuild => (_flags & EligibilityEnum.HideReferenceFromComposite) == EligibilityEnum.HideReferenceFromComposite;
            public bool CompileIntoCompositeImage => (_flags & EligibilityEnum.CompileIntoCompositeImage) == EligibilityEnum.CompileIntoCompositeImage;
            public bool CompileUnrootedIntoCompositeImage => (_flags & EligibilityEnum.CompileUnrootedIntoCompositeImage) == EligibilityEnum.CompileUnrootedIntoCompositeImage;
            public bool CompileSeparately => (_flags & EligibilityEnum.CompileSeparately) == EligibilityEnum.CompileSeparately;
            public bool Compile => CompileIntoCompositeImage || CompileUnrootedIntoCompositeImage || CompileSeparately;

            private Eligibility(EligibilityEnum flags)
            {
                _flags = flags;
            }

            public static Eligibility CreateReferenceEligibility(bool hideFromCompositeBuilds)
            {
                if (hideFromCompositeBuilds)
                    return new Eligibility(EligibilityEnum.Reference | EligibilityEnum.HideReferenceFromComposite);
                else
                    return new Eligibility(EligibilityEnum.Reference);
            }

            public static Eligibility CreateCompileEligibility(bool doNotBuildIntoComposite, bool rootedInComposite)
            {
                if (doNotBuildIntoComposite)
                    return new Eligibility(EligibilityEnum.Reference | EligibilityEnum.HideReferenceFromComposite | EligibilityEnum.CompileSeparately);
                else if (rootedInComposite)
                    return new Eligibility(EligibilityEnum.Reference | EligibilityEnum.CompileIntoCompositeImage);
                else
                    return new Eligibility(EligibilityEnum.Reference | EligibilityEnum.CompileUnrootedIntoCompositeImage);
            }
        };

        private static bool IsNonCompositeReadyToRunImage(PEReader peReader)
        {
            if (peReader.PEHeaders == null)
                return false;

            if (peReader.PEHeaders.CorHeader == null)
                return false;

            if ((peReader.PEHeaders.CorHeader.Flags & CorFlags.ILLibrary) == 0)
            {
                // This is likely a composite image, but those can't be re-r2r'd
                return false;
            }
            else
            {
                return peReader.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size != 0;
            }
        }

        private static Eligibility GetInputFileEligibility(ITaskItem file, bool compositeCompile, HashSet<string> exclusionSet, HashSet<string> r2rCompositeExclusionSet, HashSet<string> r2rCompositeRootSet)
        {
            // Check to see if this is a valid ILOnly image that we can compile
            if (!file.ItemSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && !file.ItemSpec.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                // If it isn't a dll or an exe, it certainly isn't a valid ILOnly image for compilation
                return Eligibility.None;
            }

            using (FileStream fs = new(file.ItemSpec, FileMode.Open, FileAccess.Read))
            {
                try
                {
                    using (var pereader = new PEReader(fs))
                    {
                        if (!pereader.HasMetadata)
                        {
                            return Eligibility.None;
                        }

                        MetadataReader mdReader = pereader.GetMetadataReader();
                        if (!mdReader.IsAssembly)
                        {
                            return Eligibility.None;
                        }

                        if (IsReferenceAssembly(mdReader))
                        {
                            // crossgen can only take implementation assemblies, even as references
                            return Eligibility.None;
                        }

                        bool excludeFromR2R = (exclusionSet != null && exclusionSet.Contains(Path.GetFileName(file.ItemSpec)));
                        bool excludeFromComposite = (r2rCompositeExclusionSet != null && r2rCompositeExclusionSet.Contains(Path.GetFileName(file.ItemSpec))) || excludeFromR2R;

                        // Default to rooting all assemblies.
                        // If a root set is specified, only root if in the set.
                        bool rootedInComposite = (r2rCompositeRootSet == null || r2rCompositeRootSet.Contains(Path.GetFileName(file.ItemSpec)));

                        if ((pereader.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) != CorFlags.ILOnly)
                        {
                            // This can happen due to C++/CLI binaries or due to previously R2R compiled binaries.

                            if (!IsNonCompositeReadyToRunImage(pereader))
                            {
                                // For C++/CLI always treat as only a reference
                                return Eligibility.CreateReferenceEligibility(excludeFromComposite);
                            }
                            else
                            {
                                // If previously compiled as R2R, treat as reference if this would be compiled seperately
                                if (!compositeCompile || excludeFromComposite)
                                {
                                    return Eligibility.CreateReferenceEligibility(excludeFromComposite);
                                }
                            }
                        }

                        if (file.HasMetadataValue(MetadataKeys.ReferenceOnly, "true"))
                        {
                            return Eligibility.CreateReferenceEligibility(excludeFromComposite);
                        }

                        if (excludeFromR2R)
                        {
                            return Eligibility.CreateReferenceEligibility(excludeFromComposite);
                        }

                        // save these most expensive checks for last. We don't want to scan all references for IL code
                        if (ReferencesWinMD(mdReader) || !HasILCode(pereader, mdReader))
                        {
                            // Forwarder assemblies are not separately compiled via R2R, but when performing composite compilation, they are included in the bundle
                            if (excludeFromComposite || !compositeCompile)
                                return Eligibility.CreateReferenceEligibility(excludeFromComposite);
                        }

                        return Eligibility.CreateCompileEligibility(!compositeCompile || excludeFromComposite, rootedInComposite);
                    }
                }
                catch (BadImageFormatException)
                {
                    // Not a valid assembly file
                    return Eligibility.None;
                }
            }
        }

        private static bool IsReferenceAssembly(MetadataReader mdReader)
        {
            foreach (var attributeHandle in mdReader.GetAssemblyDefinition().GetCustomAttributes())
            {
                EntityHandle attributeCtor = mdReader.GetCustomAttribute(attributeHandle).Constructor;

                StringHandle attributeTypeName = default;
                StringHandle attributeTypeNamespace = default;

                if (attributeCtor.Kind == HandleKind.MemberReference)
                {
                    EntityHandle attributeMemberParent = mdReader.GetMemberReference((MemberReferenceHandle)attributeCtor).Parent;
                    if (attributeMemberParent.Kind == HandleKind.TypeReference)
                    {
                        TypeReference attributeTypeRef = mdReader.GetTypeReference((TypeReferenceHandle)attributeMemberParent);
                        attributeTypeName = attributeTypeRef.Name;
                        attributeTypeNamespace = attributeTypeRef.Namespace;
                    }
                }
                else if (attributeCtor.Kind == HandleKind.MethodDefinition)
                {
                    TypeDefinitionHandle attributeTypeDefHandle = mdReader.GetMethodDefinition((MethodDefinitionHandle)attributeCtor).GetDeclaringType();
                    TypeDefinition attributeTypeDef = mdReader.GetTypeDefinition(attributeTypeDefHandle);
                    attributeTypeName = attributeTypeDef.Name;
                    attributeTypeNamespace = attributeTypeDef.Namespace;
                }

                if (!attributeTypeName.IsNil &&
                    !attributeTypeNamespace.IsNil &&
                    mdReader.StringComparer.Equals(attributeTypeName, "ReferenceAssemblyAttribute") &&
                    mdReader.StringComparer.Equals(attributeTypeNamespace, "System.Runtime.CompilerServices"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ReferencesWinMD(MetadataReader mdReader)
        {
            foreach (var assemblyRefHandle in mdReader.AssemblyReferences)
            {
                AssemblyReference assemblyRef = mdReader.GetAssemblyReference(assemblyRefHandle);
                if ((assemblyRef.Flags & AssemblyFlags.WindowsRuntime) == AssemblyFlags.WindowsRuntime)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasILCode(PEReader peReader, MetadataReader mdReader)
        {
            foreach (var methoddefHandle in mdReader.MethodDefinitions)
            {
                MethodDefinition methodDef = mdReader.GetMethodDefinition(methoddefHandle);
                if (methodDef.RelativeVirtualAddress > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
