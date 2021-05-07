// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection.Metadata;
using System.Reflection;

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

        private bool _crossgen2IsVersion5;

        private List<ITaskItem> _compileList = new List<ITaskItem>();
        private List<ITaskItem> _symbolsCompileList = new List<ITaskItem>();
        private List<ITaskItem> _r2rFiles = new List<ITaskItem>();
        private List<ITaskItem> _r2rReferences = new List<ITaskItem>();

        protected override void ExecuteCore()
        {
            if (ReadyToRunUseCrossgen2)
            {
                string isVersion5 = Crossgen2Tool.GetMetadata(MetadataKeys.IsVersion5);
                _crossgen2IsVersion5 = !string.IsNullOrEmpty(isVersion5) && bool.Parse(isVersion5);

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
            ProcessInputFileList(Assemblies, _compileList, _symbolsCompileList, _r2rFiles, _r2rReferences, hasValidDiaSymReaderLib);
        }

        private void ProcessInputFileList(
            ITaskItem[] inputFiles,
            List<ITaskItem> imageCompilationList,
            List<ITaskItem> symbolsCompilationList,
            List<ITaskItem> r2rFilesPublishList,
            List<ITaskItem> r2rReferenceList,
            bool hasValidDiaSymReaderLib)
        {
            if (inputFiles == null)
            {
                return;
            }

            // TODO: ExcludeList for composite mode
            var exclusionSet = ExcludeList == null || Crossgen2Composite ? null : new HashSet<string>(ExcludeList, StringComparer.OrdinalIgnoreCase);
            bool publishedCompositeImage = false;

            foreach (var file in inputFiles)
            {
                var eligibility = GetInputFileEligibility(file, exclusionSet);

                if (eligibility == Eligibility.None)
                {
                    continue;
                }

                r2rReferenceList.Add(file);

                if (!Crossgen2Composite && (eligibility == Eligibility.ReferenceOnly))
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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && hasValidDiaSymReaderLib)
                    {
                        outputPDBImage = Path.ChangeExtension(outputR2RImage, "ni.pdb");
                        outputPDBImageRelativePath = Path.ChangeExtension(outputR2RImageRelativePath, "ni.pdb");
                        crossgen1CreatePDBCommand = $"/CreatePDB \"{Path.GetDirectoryName(outputPDBImage)}\"";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        using (FileStream fs = new FileStream(file.ItemSpec, FileMode.Open, FileAccess.Read))
                        {
                            PEReader pereader = new PEReader(fs);
                            MetadataReader mdReader = pereader.GetMetadataReader();
                            Guid mvid = mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid);

                            outputPDBImage = Path.ChangeExtension(outputR2RImage, "ni.{" + mvid + "}.map");
                            outputPDBImageRelativePath = Path.ChangeExtension(outputR2RImageRelativePath, "ni.{" + mvid + "}.map");
                            crossgen1CreatePDBCommand = $"/CreatePerfMap \"{Path.GetDirectoryName(outputPDBImage)}\"";
                        }
                    }
                }

                if (!Crossgen2Composite)
                {
                    // This TaskItem is the IL->R2R entry, for an input assembly that needs to be compiled into a R2R image. This will be used as
                    // an input to the ReadyToRunCompiler task
                    TaskItem r2rCompilationEntry = new TaskItem(file);
                    r2rCompilationEntry.SetMetadata(MetadataKeys.OutputR2RImage, outputR2RImage);
                    if (outputPDBImage != null && ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5)
                    {
                        r2rCompilationEntry.SetMetadata(MetadataKeys.EmitSymbols, "true");
                        r2rCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, Path.GetDirectoryName(outputPDBImage));
                    }
                    r2rCompilationEntry.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                    imageCompilationList.Add(r2rCompilationEntry);
                }
                else if (file.ItemSpec == MainAssembly.ItemSpec)
                {
                    // Create a TaskItem for <MainAssembly>.r2r.dll
                    publishedCompositeImage = true;
                    CreateAndPublishCompositeImage(file, compositeDllWithUniqueName: false);
                }

                // This TaskItem corresponds to the output R2R image. It is equivalent to the input TaskItem, only the ItemSpec for it points to the new path
                // for the newly created R2R image
                TaskItem r2rFileToPublish = new TaskItem(file);
                r2rFileToPublish.ItemSpec = outputR2RImage;
                r2rFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                r2rFilesPublishList.Add(r2rFileToPublish);

                // Note: ReadyToRun PDB/Map files are not needed for debugging. They are only used for profiling, therefore the default behavior is to not generate them
                // unless an explicit PublishReadyToRunEmitSymbols flag is enabled by the app developer. There is also another way to profile that the runtime supports, which does
                // not rely on the native PDBs/Map files, so creating them is really an opt-in option, typically used by advanced users.
                // For debugging, only the IL PDBs are required.
                if (!Crossgen2Composite && outputPDBImage != null)
                {
                    if (!ReadyToRunUseCrossgen2 || _crossgen2IsVersion5)
                    {
                        // This TaskItem is the R2R->R2RPDB entry, for a R2R image that was just created, and for which we need to create native PDBs. This will be used as
                        // an input to the ReadyToRunCompiler task
                        TaskItem pdbCompilationEntry = new TaskItem(file);
                        pdbCompilationEntry.ItemSpec = outputR2RImage;
                        pdbCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, outputPDBImage);
                        pdbCompilationEntry.SetMetadata(MetadataKeys.CreatePDBCommand, crossgen1CreatePDBCommand);
                        symbolsCompilationList.Add(pdbCompilationEntry);
                    }

                    // This TaskItem corresponds to the output PDB image. It is equivalent to the input TaskItem, only the ItemSpec for it points to the new path
                    // for the newly created PDB image.
                    TaskItem r2rSymbolsFileToPublish = new TaskItem(file);
                    r2rSymbolsFileToPublish.ItemSpec = outputPDBImage;
                    r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.RelativePath, outputPDBImageRelativePath);
                    r2rSymbolsFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                    if (!IncludeSymbolsInSingleFile)
                    {
                        r2rSymbolsFileToPublish.SetMetadata(MetadataKeys.ExcludeFromSingleFile, "true");
                    }

                    r2rFilesPublishList.Add(r2rSymbolsFileToPublish);
                }
            }

            if (Crossgen2Composite && !publishedCompositeImage)
            {
                MainAssembly.SetMetadata(MetadataKeys.RelativePath, Path.GetFileName(MainAssembly.ItemSpec));
                CreateAndPublishCompositeImage(MainAssembly, compositeDllWithUniqueName: true);
            }

            void CreateAndPublishCompositeImage(ITaskItem file, bool compositeDllWithUniqueName)
            {
                var compositeR2RImageRelativePath = file.GetMetadata(MetadataKeys.RelativePath);
                compositeR2RImageRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, "r2r" + Path.GetExtension(compositeR2RImageRelativePath));
                var compositeR2RImage = Path.Combine(OutputPath, compositeR2RImageRelativePath);

                TaskItem r2rCompilationEntry = new TaskItem(file);
                r2rCompilationEntry.SetMetadata(MetadataKeys.OutputR2RImage, compositeR2RImage);
                r2rCompilationEntry.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                if (compositeDllWithUniqueName)
                    r2rCompilationEntry.SetMetadata(MetadataKeys.CompositeImageWithoutSource, "true");

                if (EmitSymbols)
                {
                    string compositePDBImage = null;
                    string compositePDBRelativePath = null;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && hasValidDiaSymReaderLib)
                    {
                        compositePDBImage = Path.ChangeExtension(compositeR2RImage, ".ni.pdb");
                        compositePDBRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, ".ni.pdb");
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        compositePDBImage = Path.ChangeExtension(compositeR2RImage, ".ni.{composite}.map");
                        compositePDBRelativePath = Path.ChangeExtension(compositeR2RImageRelativePath, ".ni.{composite}.map");
                    }

                    if (compositePDBImage != null && ReadyToRunUseCrossgen2 && !_crossgen2IsVersion5)
                    {
                        r2rCompilationEntry.SetMetadata(MetadataKeys.EmitSymbols, "true");
                        r2rCompilationEntry.SetMetadata(MetadataKeys.OutputPDBImage, Path.GetDirectoryName(compositePDBImage));

                        // Publish composite PDB file
                        TaskItem r2rSymbolsFileToPublish = new TaskItem(file);
                        r2rSymbolsFileToPublish.ItemSpec = compositePDBImage;
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
                TaskItem compositeR2RFileToPublish = new TaskItem(file);
                compositeR2RFileToPublish.ItemSpec = compositeR2RImage;
                compositeR2RFileToPublish.RemoveMetadata(MetadataKeys.OriginalItemSpec);
                compositeR2RFileToPublish.SetMetadata(MetadataKeys.RelativePath, compositeR2RImageRelativePath);
                r2rFilesPublishList.Add(compositeR2RFileToPublish);
            }
        }

        private enum Eligibility
        {
            None,
            ReferenceOnly,
            CompileAndReference
        };

        private static Eligibility GetInputFileEligibility(ITaskItem file, HashSet<string> exclusionSet)
        {
            // Check to see if this is a valid ILOnly image that we can compile
            using (FileStream fs = new FileStream(file.ItemSpec, FileMode.Open, FileAccess.Read))
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

                        if ((pereader.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) != CorFlags.ILOnly)
                        {
                            return Eligibility.ReferenceOnly;
                        }

                        if (file.HasMetadataValue(MetadataKeys.ReferenceOnly, "true"))
                        {
                            return Eligibility.ReferenceOnly;
                        }

                        if (exclusionSet != null && exclusionSet.Contains(Path.GetFileName(file.ItemSpec)))
                        {
                            return Eligibility.ReferenceOnly;
                        }

                        // save these most expensive checks for last. We don't want to scan all references for IL code
                        if (ReferencesWinMD(mdReader) || !HasILCode(pereader, mdReader))
                        {
                            return Eligibility.ReferenceOnly;
                        }

                        return Eligibility.CompileAndReference;
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
