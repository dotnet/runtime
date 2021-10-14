using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using ILVerify;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Xunit;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class ResultChecker
    {
        readonly BaseAssemblyResolver _originalsResolver;
        readonly BaseAssemblyResolver _linkedResolver;
        readonly ReaderParameters _originalReaderParameters;
        readonly ReaderParameters _linkedReaderParameters;

        public ResultChecker ()
            : this (new TestCaseAssemblyResolver (), new TestCaseAssemblyResolver (),
                new ReaderParameters {
                    SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
                },
                new ReaderParameters {
                    SymbolReaderProvider = new DefaultSymbolReaderProvider (false)
                })
        {
        }

        public ResultChecker (BaseAssemblyResolver originalsResolver, BaseAssemblyResolver linkedResolver,
            ReaderParameters originalReaderParameters, ReaderParameters linkedReaderParameters)
        {
            _originalsResolver = originalsResolver;
            _linkedResolver = linkedResolver;
            _originalReaderParameters = originalReaderParameters;
            _linkedReaderParameters = linkedReaderParameters;
        }

        public virtual void Check (TrimmedTestCaseResult trimmedResult)
        {
            InitializeResolvers (trimmedResult);

            try {
                var original = ResolveOriginalsAssembly (trimmedResult.ExpectationsAssemblyPath.FileNameWithoutExtension);
                if (!HasAttribute (original, nameof (NoLinkedOutputAttribute))) {
                    Assert.True (trimmedResult.OutputAssemblyPath.FileExists (), $"The linked output assembly was not found.  Expected at {trimmedResult.OutputAssemblyPath}");
                    var linked = ResolveLinkedAssembly (trimmedResult.OutputAssemblyPath.FileNameWithoutExtension);

                    InitialChecking (trimmedResult, original, linked);

                    PerformOutputAssemblyChecks (original, trimmedResult.OutputAssemblyPath.Parent);
                    PerformOutputSymbolChecks (original, trimmedResult.OutputAssemblyPath.Parent);

                    if (!HasAttribute (original.MainModule.GetType (trimmedResult.TestCase.ReconstructedFullTypeName), nameof (SkipKeptItemsValidationAttribute))) {
                        CreateAssemblyChecker (original, linked).Verify ();
                    }
                }

                VerifyLinkingOfOtherAssemblies (original);
                AdditionalChecking (trimmedResult, original);
            } finally {
                _originalsResolver.Dispose ();
                _linkedResolver.Dispose ();
            }
        }

        protected virtual AssemblyChecker CreateAssemblyChecker (AssemblyDefinition original, AssemblyDefinition linked)
        {
            return new AssemblyChecker (original, linked);
        }

        void InitializeResolvers (TrimmedTestCaseResult linkedResult)
        {
            _originalsResolver.AddSearchDirectory (linkedResult.ExpectationsAssemblyPath.Parent.ToString ());
            _linkedResolver.AddSearchDirectory (linkedResult.OutputAssemblyPath.Parent.ToString ());
        }

        protected AssemblyDefinition ResolveLinkedAssembly (string assemblyName)
        {
            var cleanAssemblyName = assemblyName;
            if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
                cleanAssemblyName = System.IO.Path.GetFileNameWithoutExtension (assemblyName);
            return _linkedResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _linkedReaderParameters);
        }

        protected AssemblyDefinition ResolveOriginalsAssembly (string assemblyName)
        {
            var cleanAssemblyName = assemblyName;
            if (assemblyName.EndsWith (".exe") || assemblyName.EndsWith (".dll"))
                cleanAssemblyName = Path.GetFileNameWithoutExtension (assemblyName);
            return _originalsResolver.Resolve (new AssemblyNameReference (cleanAssemblyName, null), _originalReaderParameters);
        }

        void PerformOutputAssemblyChecks (AssemblyDefinition original, NPath outputDirectory)
        {
            var assembliesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (attr => ExpectationsProvider.IsAssemblyAssertion (attr));
            var actionAssemblies = new HashSet<string> ();
            bool trimModeIsCopy = false;

            foreach (var assemblyAttr in assembliesToCheck) {
                var name = (string) assemblyAttr.ConstructorArguments.First ().Value;
                var expectedPath = outputDirectory.Combine (name);

                if (assemblyAttr.AttributeType.Name == nameof (RemovedAssemblyAttribute))
                    Assert.False (expectedPath.FileExists (), $"Expected the assembly {name} to not exist in {outputDirectory}, but it did");
                else if (assemblyAttr.AttributeType.Name == nameof (KeptAssemblyAttribute))
                    Assert.True (expectedPath.FileExists (), $"Expected the assembly {name} to exist in {outputDirectory}, but it did not");
                else if (assemblyAttr.AttributeType.Name == nameof (SetupLinkerActionAttribute)) {
                    string assemblyName = (string) assemblyAttr.ConstructorArguments[1].Value;
                    if ((string) assemblyAttr.ConstructorArguments[0].Value == "copy") {
                        VerifyCopyAssemblyIsKeptUnmodified (outputDirectory, assemblyName + (assemblyName == "test" ? ".exe" : ".dll"));
                    }

                    actionAssemblies.Add (assemblyName);
                } else if (assemblyAttr.AttributeType.Name == nameof (SetupLinkerTrimModeAttribute)) {
                    // We delay checking that everything was copied after processing all assemblies
                    // with a specific action, since assembly action wins over trim mode.
                    if ((string) assemblyAttr.ConstructorArguments[0].Value == "copy")
                        trimModeIsCopy = true;
                } else
                    throw new NotImplementedException ($"Unknown assembly assertion of type {assemblyAttr.AttributeType}");
            }

            if (trimModeIsCopy) {
                foreach (string assemblyName in Directory.GetFiles (Directory.GetParent (outputDirectory)!.ToString (), "input")) {
                    var fileInfo = new FileInfo (assemblyName);
                    if (fileInfo.Extension == ".dll" && !actionAssemblies.Contains (assemblyName))
                        VerifyCopyAssemblyIsKeptUnmodified (outputDirectory, assemblyName + (assemblyName == "test" ? ".exe" : ".dll"));
                }
            }
        }

        void PerformOutputSymbolChecks (AssemblyDefinition original, NPath outputDirectory)
        {
            var symbolFilesToCheck = original.MainModule.Types.SelectMany (t => t.CustomAttributes).Where (ExpectationsProvider.IsSymbolAssertion);

            foreach (var symbolAttr in symbolFilesToCheck) {
                if (symbolAttr.AttributeType.Name == nameof (RemovedSymbolsAttribute))
                    VerifyRemovedSymbols (symbolAttr, outputDirectory);
                else if (symbolAttr.AttributeType.Name == nameof (KeptSymbolsAttribute))
                    VerifyKeptSymbols (symbolAttr);
                else
                    throw new NotImplementedException ($"Unknown symbol file assertion of type {symbolAttr.AttributeType}");
            }
        }

        void VerifyKeptSymbols (CustomAttribute symbolsAttribute)
        {
            var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
            var originalAssembly = ResolveOriginalsAssembly (assemblyName);
            var linkedAssembly = ResolveLinkedAssembly (assemblyName);

            if (linkedAssembly.MainModule.SymbolReader == null)
            {
                Assert.True(false, $"Missing symbols for assembly `{linkedAssembly.MainModule.FileName}`");
                return;
            }

            if (linkedAssembly.MainModule.SymbolReader.GetType () != originalAssembly.MainModule.SymbolReader.GetType ())
                Assert.True (false, $"Expected symbol provider of type `{originalAssembly.MainModule.SymbolReader}`, but was `{linkedAssembly.MainModule.SymbolReader}`");
        }

        void VerifyRemovedSymbols (CustomAttribute symbolsAttribute, NPath outputDirectory)
        {
            var assemblyName = (string) symbolsAttribute.ConstructorArguments[0].Value;
            try {
                var linkedAssembly = ResolveLinkedAssembly (assemblyName);

                if (linkedAssembly.MainModule.SymbolReader != null)
                    Assert.True (false, $"Expected no symbols to be found for assembly `{linkedAssembly.MainModule.FileName}`, however, symbols were found of type {linkedAssembly.MainModule.SymbolReader}");
            } catch (AssemblyResolutionException) {
                // If we failed to resolve, then the entire assembly may be gone.
                // The assembly being gone confirms that embedded pdbs were removed, but technically, for the other symbol types, the symbol file could still exist on disk
                // let's check to make sure that it does not.
                var possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}").ChangeExtension ("pdb");
                if (possibleSymbolFilePath.Exists ())
                    Assert.True (false, $"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}");

                possibleSymbolFilePath = outputDirectory.Combine ($"{assemblyName}.mdb");
                if (possibleSymbolFilePath.Exists())
                    Assert.True(false, $"Expected no symbols to be found for assembly `{assemblyName}`, however, a symbol file was found at {possibleSymbolFilePath}"); ;
            }
        }

        private sealed class Resolver : IResolver
        {
            private readonly Dictionary<string, string> _assemblyPaths = new();
            private readonly Dictionary<string, PEReader> _assemblyReaders = new();
            public Resolver(PEReader mainAssemblyReader, string mainAssemblyName, string extraDllsPath)
            {
                _assemblyReaders.Add(mainAssemblyName, mainAssemblyReader);

                foreach (var assembly in Directory.EnumerateFiles(extraDllsPath, "*.dll"))
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                    _assemblyPaths.Add(assemblyName, assembly);
                }

                var netcoreappDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                foreach (var assembly in Directory.EnumerateFiles(netcoreappDir, "*.dll"))
                {
                    var assemblyName = Path.GetFileNameWithoutExtension(assembly);
                    if (assemblyName.Contains("Native") || assemblyName == mainAssemblyName)
                    {
                        continue;
                    }
                    if (assemblyName.StartsWith("Microsoft") ||
                        assemblyName.StartsWith("System") ||
                        assemblyName == "mscorlib" || assemblyName == "netstandard")
                    {
                        _assemblyPaths.Add(assemblyName, assembly);
                    }
                }
            }

            public PEReader? Resolve(string simpleName)
            {
                if (_assemblyReaders.TryGetValue(simpleName, out var reader))
                {
                    return reader;
                }

                if (_assemblyPaths.TryGetValue(simpleName, out var asmPath))
                {
                    reader = new PEReader(File.OpenRead(asmPath));
                    _assemblyReaders.Add(simpleName, reader);
                    return reader;
                }

                return null;
            }
        }

        protected virtual void AdditionalChecking (TrimmedTestCaseResult linkResult, AssemblyDefinition original)
        {
            using var peReader = new PEReader(File.OpenRead(linkResult.OutputAssemblyPath.ToString()));
            var verifier = new Verifier(
                new Resolver(peReader, linkResult.OutputAssemblyPath.FileNameWithoutExtension, linkResult.OutputAssemblyPath.Parent.ToString()),
                new VerifierOptions() { });
            verifier.SetSystemModuleName(typeof(object).Assembly.GetName());
            foreach (var result in verifier.Verify(peReader))
            {
                Assert.True(false, $"IL Verififaction failed: {result.Message}{Environment.NewLine}Type token: {MetadataTokens.GetToken(result.Type):x}, Method token: {MetadataTokens.GetToken(result.Method):x}");
            }
        }

        protected virtual void InitialChecking (TrimmedTestCaseResult linkResult, AssemblyDefinition original, AssemblyDefinition linked)
        {
        }

        void VerifyLinkingOfOtherAssemblies (AssemblyDefinition original)
        {
            var checks = BuildOtherAssemblyCheckTable (original);

            try {
                foreach (var assemblyName in checks.Keys) {
                    using (var linkedAssembly = ResolveLinkedAssembly (assemblyName)) {
                        foreach (var checkAttrInAssembly in checks[assemblyName]) {
                            var attributeTypeName = checkAttrInAssembly.AttributeType.Name;

                            switch (attributeTypeName) {
                            case nameof (KeptAllTypesAndMembersInAssemblyAttribute):
                                VerifyKeptAllTypesAndMembersInAssembly (linkedAssembly);
                                continue;
                            case nameof (KeptAttributeInAssemblyAttribute):
                                VerifyKeptAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
                                continue;
                            case nameof (RemovedAttributeInAssembly):
                                VerifyRemovedAttributeInAssembly (checkAttrInAssembly, linkedAssembly);
                                continue;
                            default:
                                break;
                            }

                            var expectedTypeName = checkAttrInAssembly.ConstructorArguments[1].Value.ToString ()!;
                            TypeDefinition? linkedType = linkedAssembly.MainModule.GetType (expectedTypeName);

                            if (linkedType == null && linkedAssembly.MainModule.HasExportedTypes) {
                                ExportedType? exportedType = linkedAssembly.MainModule.ExportedTypes
                                        .FirstOrDefault (exported => exported.FullName == expectedTypeName);

                                // Note that copied assemblies could have dangling references.
                                if (exportedType != null && original.EntryPoint.DeclaringType.CustomAttributes.FirstOrDefault (
                                    ca => ca.AttributeType.Name == nameof (RemovedAssemblyAttribute)
                                    && ca.ConstructorArguments[0].Value.ToString () == exportedType.Scope.Name + ".dll") != null)
                                    continue;

                                linkedType = exportedType?.Resolve ();
                            }

                            switch (attributeTypeName) {
                            case nameof (RemovedTypeInAssemblyAttribute):
                                if (linkedType != null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been removed"); ;
                                GetOriginalTypeFromInAssemblyAttribute (checkAttrInAssembly);
                                break;
                            case nameof (KeptTypeInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been kept");
                                break;
                            case nameof (RemovedInterfaceOnTypeInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been kept");
                                VerifyRemovedInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType!);
                                break;
                            case nameof (KeptInterfaceOnTypeInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been kept");
                                VerifyKeptInterfaceOnTypeInAssembly (checkAttrInAssembly, linkedType!);
                                break;
                            case nameof (RemovedMemberInAssemblyAttribute):
                                if (linkedType == null)
                                    continue;

                                VerifyRemovedMemberInAssembly (checkAttrInAssembly, linkedType);
                                break;
                            case nameof (KeptBaseOnTypeInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been kept");
                                VerifyKeptBaseOnTypeInAssembly (checkAttrInAssembly, linkedType!);
                                break;
                            case nameof (KeptMemberInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}' should have been kept");

                                VerifyKeptMemberInAssembly (checkAttrInAssembly, linkedType!);
                                break;
                            case nameof (RemovedForwarderAttribute):
                                if (linkedAssembly.MainModule.ExportedTypes.Any (l => l.Name == expectedTypeName))
                                    Assert.True(false, $"Forwarder `{expectedTypeName}' should have been removed");

                                break;

                            case nameof (RemovedAssemblyReferenceAttribute):
                                Assert.False (linkedAssembly.MainModule.AssemblyReferences.Any (l => l.Name == expectedTypeName),
                                    $"AssemblyRef '{expectedTypeName}' should have been removed");
                                break;

                            case nameof (KeptResourceInAssemblyAttribute):
                                VerifyKeptResourceInAssembly (checkAttrInAssembly);
                                break;
                            case nameof (RemovedResourceInAssemblyAttribute):
                                VerifyRemovedResourceInAssembly (checkAttrInAssembly);
                                break;
                            case nameof (KeptReferencesInAssemblyAttribute):
                                VerifyKeptReferencesInAssembly (checkAttrInAssembly);
                                break;
                            case nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute):
                                if (linkedType == null)
                                    Assert.True(false, $"Type `{expectedTypeName}` should have been kept");
                                VerifyExpectedInstructionSequenceOnMemberInAssembly (checkAttrInAssembly, linkedType!);
                                break;
                            default:
                                UnhandledOtherAssemblyAssertion (expectedTypeName, checkAttrInAssembly, linkedType);
                                break;
                            }
                        }
                    }
                }
            } catch (AssemblyResolutionException e) {
                Assert.True(false, $"Failed to resolve linked assembly `{e.AssemblyReference.Name}`.  It must not exist in any of the output directories:\n\t{_linkedResolver.GetSearchDirectories ().Aggregate ((buff, s) => $"{buff}\n\t{s}")}\n");
            }
        }

        void VerifyKeptAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
        {
            VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeKept);
        }

        void VerifyRemovedAttributeInAssembly (CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly)
        {
            VerifyAttributeInAssembly (inAssemblyAttribute, linkedAssembly, VerifyCustomAttributeRemoved);
        }

        void VerifyAttributeInAssembly(CustomAttribute inAssemblyAttribute, AssemblyDefinition linkedAssembly, Action<ICustomAttributeProvider, string> assertExpectedAttribute)
        {
            var assemblyName = (string)inAssemblyAttribute.ConstructorArguments[0].Value;
            string expectedAttributeTypeName;
            var attributeTypeOrTypeName = inAssemblyAttribute.ConstructorArguments[1].Value;
            if (attributeTypeOrTypeName is TypeReference typeReference) {
                expectedAttributeTypeName = typeReference.FullName;
            } else {
                expectedAttributeTypeName = attributeTypeOrTypeName.ToString()!;
            }

            if (inAssemblyAttribute.ConstructorArguments.Count == 2) {
                // Assembly
                assertExpectedAttribute(linkedAssembly, expectedAttributeTypeName);
                return;
            }

            // We are asserting on type or member
            var typeOrTypeName = inAssemblyAttribute.ConstructorArguments[2].Value;
            var originalType = GetOriginalTypeFromInAssemblyAttribute(inAssemblyAttribute.ConstructorArguments[0].Value.ToString()!, typeOrTypeName);
            if (originalType == null)
            {
                Assert.True(false, $"Invalid test assertion.  The original `{assemblyName}` does not contain a type `{typeOrTypeName}`");
                return;
            }

            var linkedType = linkedAssembly.MainModule.GetType(originalType.FullName);
            if (linkedType == null)
            {
                Assert.True(false, $"Missing expected type `{typeOrTypeName}` in `{assemblyName}`");
                return;
            }

            if (inAssemblyAttribute.ConstructorArguments.Count == 3) {
                assertExpectedAttribute (linkedType, expectedAttributeTypeName);
                return;
            }

            // we are asserting on a member
            string memberName = (string) inAssemblyAttribute.ConstructorArguments[3].Value;

            // We will find the matching type from the original assembly first that way we can confirm
            // that the name defined in the attribute corresponds to a member that actually existed
            var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
            if (originalFieldMember != null) {
                var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
                if (linkedField == null)
                {
                    Assert.True(false, $"Field `{memberName}` on Type `{originalType}` should have been kept");
                    return;
                }

                assertExpectedAttribute (linkedField, expectedAttributeTypeName);
                return;
            }

            var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
            if (originalPropertyMember != null) {
                var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
                if (linkedProperty == null)
                {
                    Assert.True(false, $"Property `{memberName}` on Type `{originalType}` should have been kept");
                    return;
                }

                assertExpectedAttribute (linkedProperty, expectedAttributeTypeName);
                return;
            }

            var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
            if (originalMethodMember != null) {
                var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
                if (linkedMethod == null)
                {
                    Assert.True(false, $"Method `{memberName}` on Type `{originalType}` should have been kept");
                    return;
                }

                assertExpectedAttribute (linkedMethod, expectedAttributeTypeName);
                return;
            }

            Assert.True(false, $"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
        }

        void VerifyCopyAssemblyIsKeptUnmodified (NPath outputDirectory, string assemblyName)
        {
            string inputAssemblyPath = Path.Combine (Directory.GetParent (outputDirectory)!.ToString (), "input", assemblyName);
            string outputAssemblyPath = Path.Combine (outputDirectory, assemblyName);
            Assert.True (File.ReadAllBytes (inputAssemblyPath).SequenceEqual (File.ReadAllBytes (outputAssemblyPath)),
                $"Expected assemblies\n" +
                $"\t{inputAssemblyPath}\n" +
                $"\t{outputAssemblyPath}\n" +
                $"binaries to be equal, since the input assembly has copy action.");
        }

        void VerifyCustomAttributeKept (ICustomAttributeProvider provider, string expectedAttributeTypeName)
        {
            var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
            if (match == null)
                Assert.True(false, $"Expected `{provider}` to have an attribute of type `{expectedAttributeTypeName}`");
        }

        void VerifyCustomAttributeRemoved (ICustomAttributeProvider provider, string expectedAttributeTypeName)
        {
            var match = provider.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.FullName == expectedAttributeTypeName);
            if (match != null)
                Assert.True(false, $"Expected `{provider}` to no longer have an attribute of type `{expectedAttributeTypeName}`");
        }

        void VerifyRemovedInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

            var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
            var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

            var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
            if (!originalType.HasInterfaces)
                Assert.True(false, "Invalid assertion.  Original type does not have any interfaces");

            var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
            if (originalInterfaceImpl == null)
                Assert.True(false, $"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

            var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
            if (linkedInterfaceImpl != null)
                Assert.True(false, $"Expected `{linkedType}` to no longer have an interface of type {originalInterface.FullName}");
        }

        void VerifyKeptInterfaceOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

            var interfaceAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
            var interfaceType = inAssemblyAttribute.ConstructorArguments[3].Value;

            var originalInterface = GetOriginalTypeFromInAssemblyAttribute (interfaceAssemblyName, interfaceType);
            if (!originalType.HasInterfaces)
                Assert.True(false, "Invalid assertion.  Original type does not have any interfaces");

            var originalInterfaceImpl = GetMatchingInterfaceImplementationOnType (originalType, originalInterface.FullName);
            if (originalInterfaceImpl == null)
                Assert.True(false, $"Invalid assertion.  Original type never had an interface of type `{originalInterface}`");

            var linkedInterfaceImpl = GetMatchingInterfaceImplementationOnType (linkedType, originalInterface.FullName);
            if (linkedInterfaceImpl == null)
                Assert.True(false, $"Expected `{linkedType}` to have interface of type {originalInterface.FullName}");
        }

        void VerifyKeptBaseOnTypeInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);

            var baseAssemblyName = inAssemblyAttribute.ConstructorArguments[2].Value.ToString ()!;
            var baseType = inAssemblyAttribute.ConstructorArguments[3].Value;

            var originalBase = GetOriginalTypeFromInAssemblyAttribute (baseAssemblyName, baseType);
            if (originalType.BaseType.Resolve () != originalBase)
                Assert.True(false, "Invalid assertion.  Original type's base does not match the expected base");

            linkedType.BaseType.FullName.Should().Be(originalBase.FullName, $"Incorrect base on `{linkedType.FullName}`.  Expected `{originalBase.FullName}` but was `{linkedType.BaseType.FullName}`");
        }

        protected static InterfaceImplementation? GetMatchingInterfaceImplementationOnType (TypeDefinition type, string expectedInterfaceTypeName)
        {
            return type.Interfaces.FirstOrDefault (impl => {
                var resolvedImpl = impl.InterfaceType.Resolve ();

                if (resolvedImpl == null)
                {
                    Assert.True(false, $"Failed to resolve interface : `{impl.InterfaceType}` on `{type}`");
                    return false;
                }

                return resolvedImpl.FullName == expectedInterfaceTypeName;
            });
        }

        void VerifyRemovedMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
            foreach (var memberNameAttr in (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value) {
                string memberName = (string) memberNameAttr.Value;

                // We will find the matching type from the original assembly first that way we can confirm
                // that the name defined in the attribute corresponds to a member that actually existed
                var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
                if (originalFieldMember != null) {
                    var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
                    if (linkedField != null)
                        Assert.True(false, $"Field `{memberName}` on Type `{originalType}` should have been removed");

                    continue;
                }

                var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
                if (originalPropertyMember != null) {
                    var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
                    if (linkedProperty != null)
                        Assert.True(false, $"Property `{memberName}` on Type `{originalType}` should have been removed");

                    continue;
                }

                var originalMethodMember = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
                if (originalMethodMember != null) {
                    var linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
                    if (linkedMethod != null)
                        Assert.True(false, $"Method `{memberName}` on Type `{originalType}` should have been removed");

                    continue;
                }

                Assert.True(false, $"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
            }
        }

        void VerifyKeptMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
            var memberNames = (CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[2].Value;
            Assert.True (memberNames.Length > 0, "Invalid KeptMemberInAssemblyAttribute. Expected member names.");
            foreach (var memberNameAttr in memberNames) {
                string memberName = (string) memberNameAttr.Value;

                // We will find the matching type from the original assembly first that way we can confirm
                // that the name defined in the attribute corresponds to a member that actually existed

                if (TryVerifyKeptMemberInAssemblyAsField (memberName, originalType, linkedType))
                    continue;

                if (TryVerifyKeptMemberInAssemblyAsProperty (memberName, originalType, linkedType))
                    continue;

                if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType))
                    continue;

                Assert.True(false, $"Invalid test assertion.  No member named `{memberName}` exists on the original type `{originalType}`");
            }
        }

        protected virtual bool TryVerifyKeptMemberInAssemblyAsField (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
        {
            var originalFieldMember = originalType.Fields.FirstOrDefault (m => m.Name == memberName);
            if (originalFieldMember != null) {
                var linkedField = linkedType.Fields.FirstOrDefault (m => m.Name == memberName);
                if (linkedField == null)
                    Assert.True(false, $"Field `{memberName}` on Type `{originalType}` should have been kept");

                return true;
            }

            return false;
        }

        protected virtual bool TryVerifyKeptMemberInAssemblyAsProperty (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
        {
            var originalPropertyMember = originalType.Properties.FirstOrDefault (m => m.Name == memberName);
            if (originalPropertyMember != null) {
                var linkedProperty = linkedType.Properties.FirstOrDefault (m => m.Name == memberName);
                if (linkedProperty == null)
                    Assert.True(false, $"Property `{memberName}` on Type `{originalType}` should have been kept");

                return true;
            }

            return false;
        }

        protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType)
        {
            return TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition? _originalMethod, out MethodDefinition? _linkedMethod);
        }

        protected virtual bool TryVerifyKeptMemberInAssemblyAsMethod (string memberName, TypeDefinition originalType, TypeDefinition linkedType, out MethodDefinition? originalMethod, out MethodDefinition? linkedMethod)
        {
            originalMethod = originalType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
            if (originalMethod != null) {
                linkedMethod = linkedType.Methods.FirstOrDefault (m => m.GetSignature () == memberName);
                if (linkedMethod == null)
                    Assert.True(false, $"Method `{memberName}` on Type `{originalType}` should have been kept");

                return true;
            }

            linkedMethod = null;
            return false;
        }

        void VerifyKeptReferencesInAssembly (CustomAttribute inAssemblyAttribute)
        {
            var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
            var expectedReferenceNames = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[1].Value).Select (attr => (string) attr.Value).ToList ();
            for (int i = 0; i < expectedReferenceNames.Count (); i++)
                if (expectedReferenceNames[i].EndsWith (".dll"))
                    expectedReferenceNames[i] = expectedReferenceNames[i].Substring (0, expectedReferenceNames[i].LastIndexOf ("."));

            assembly.MainModule.AssemblyReferences.Select (asm => asm.Name).Should().BeEquivalentTo (expectedReferenceNames);
        }

        void VerifyKeptResourceInAssembly (CustomAttribute inAssemblyAttribute)
        {
            var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
            var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

            assembly.MainModule.Resources.Select (r => r.Name).Should().Contain(resourceName);
        }

        void VerifyRemovedResourceInAssembly (CustomAttribute inAssemblyAttribute)
        {
            var assembly = ResolveLinkedAssembly (inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!);
            var resourceName = inAssemblyAttribute.ConstructorArguments[1].Value.ToString ();

            assembly.MainModule.Resources.Select (r => r.Name).Should().NotContain(resourceName);
        }

        void VerifyKeptAllTypesAndMembersInAssembly (AssemblyDefinition linked)
        {
            var original = ResolveOriginalsAssembly (linked.MainModule.Assembly.Name.Name);

            if (original == null)
            {
                Assert.True(false, $"Failed to resolve original assembly {linked.MainModule.Assembly.Name.Name}");
                return;
            }

            var originalTypes = original.AllDefinedTypes ().ToDictionary (t => t.FullName);
            var linkedTypes = linked.AllDefinedTypes ().ToDictionary (t => t.FullName);

            var missingInLinked = originalTypes.Keys.Except (linkedTypes.Keys);

            missingInLinked.Should().BeEmpty($"Expected all types to exist in the linked assembly, but one or more were missing");

            foreach (var originalKvp in originalTypes) {
                var linkedType = linkedTypes[originalKvp.Key];

                var originalMembers = originalKvp.Value.AllMembers ().Select (m => m.FullName);
                var linkedMembers = linkedType.AllMembers ().Select (m => m.FullName);

                var missingMembersInLinked = originalMembers.Except (linkedMembers);

                missingMembersInLinked.Should().BeEmpty($"Expected all members of `{originalKvp.Key}`to exist in the linked assembly, but one or more were missing");
            }
        }

        bool IsProducedByLinker (CustomAttribute attr)
        {
            var producedBy = attr.GetPropertyValue ("ProducedBy");
            return producedBy is null ? true : ((ProducedBy) producedBy).HasFlag (ProducedBy.Trimmer);
        }
        IEnumerable<ICustomAttributeProvider> GetAttributeProviders (AssemblyDefinition assembly)
        {
            foreach (var testType in assembly.AllDefinedTypes ()) {
                foreach (var provider in testType.AllMembers ())
                    yield return provider;

                yield return testType;
            }

            foreach (var module in assembly.Modules)
                yield return module;

            yield return assembly;
        }

        void VerifyExpectedInstructionSequenceOnMemberInAssembly (CustomAttribute inAssemblyAttribute, TypeDefinition linkedType)
        {
            var originalType = GetOriginalTypeFromInAssemblyAttribute (inAssemblyAttribute);
            var memberName = (string) inAssemblyAttribute.ConstructorArguments[2].Value;

            if (TryVerifyKeptMemberInAssemblyAsMethod (memberName, originalType, linkedType, out MethodDefinition? originalMethod, out MethodDefinition? linkedMethod)) {
                static string[] valueCollector (MethodDefinition m) => AssemblyChecker.FormatMethodBody (m.Body);
                var linkedValues = linkedMethod == null ? new string[0] : valueCollector (linkedMethod);
                var srcValues = originalMethod == null ? new string[0] : valueCollector (originalMethod);

                var expected = ((CustomAttributeArgument[]) inAssemblyAttribute.ConstructorArguments[3].Value)!.Select (arg => arg.Value.ToString ()!).ToArray ();
                linkedValues.Should().BeEquivalentTo(
                    expected,
                    $"Expected method `{originalMethod} to have its {nameof (ExpectedInstructionSequenceOnMemberInAssemblyAttribute)} modified, however, the sequence does not match the expected value\n{FormattingUtils.FormatSequenceCompareFailureMessage2 (linkedValues, expected, srcValues)}");

                return;
            }

            Assert.True (false, $"Invalid test assertion.  No method named `{memberName}` exists on the original type `{originalType}`");
        }

        protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (CustomAttribute inAssemblyAttribute)
        {
            string assemblyName;
            if (inAssemblyAttribute.HasProperties && inAssemblyAttribute.Properties[0].Name == "ExpectationAssemblyName")
                assemblyName = inAssemblyAttribute.Properties[0].Argument.Value.ToString ()!;
            else
                assemblyName = inAssemblyAttribute.ConstructorArguments[0].Value.ToString ()!;

            return GetOriginalTypeFromInAssemblyAttribute (assemblyName, inAssemblyAttribute.ConstructorArguments[1].Value);
        }

        protected TypeDefinition GetOriginalTypeFromInAssemblyAttribute (string assemblyName, object typeOrTypeName)
        {
            if (typeOrTypeName is TypeReference attributeValueAsTypeReference)
                return attributeValueAsTypeReference.Resolve ();

            var assembly = ResolveOriginalsAssembly (assemblyName);

            var expectedTypeName = typeOrTypeName.ToString ();
            var originalType = assembly.MainModule.GetType (expectedTypeName);
            if (originalType == null)
                Assert.True (false, $"Invalid test assertion.  Unable to locate the original type `{expectedTypeName}.`");
            return originalType!;
        }

        Dictionary<string, List<CustomAttribute>> BuildOtherAssemblyCheckTable (AssemblyDefinition original)
        {
            var checks = new Dictionary<string, List<CustomAttribute>> ();

            foreach (var typeWithRemoveInAssembly in original.AllDefinedTypes ()) {
                foreach (var attr in typeWithRemoveInAssembly.CustomAttributes.Where (IsTypeInOtherAssemblyAssertion)) {
                    var assemblyName = (string) attr.ConstructorArguments[0].Value;
                    if (!checks.TryGetValue (assemblyName, out List<CustomAttribute>? checksForAssembly))
                        checks[assemblyName] = checksForAssembly = new List<CustomAttribute> ();

                    checksForAssembly.Add (attr);
                }
            }

            return checks;
        }

        protected virtual void UnhandledOtherAssemblyAssertion (string expectedTypeName, CustomAttribute checkAttrInAssembly, TypeDefinition? linkedType)
        {
            throw new NotImplementedException ($"Type {expectedTypeName}, has an unknown other assembly attribute of type {checkAttrInAssembly.AttributeType}");
        }

        bool IsTypeInOtherAssemblyAssertion (CustomAttribute attr)
        {
            return attr.AttributeType.Resolve ()?.DerivesFrom (nameof (BaseInAssemblyAttribute)) ?? false;
        }

        bool HasAttribute (ICustomAttributeProvider caProvider, string attributeName)
        {
            if (caProvider is AssemblyDefinition assembly && assembly.EntryPoint != null)
                return assembly.EntryPoint.DeclaringType.CustomAttributes
                    .Any (attr => attr.AttributeType.Name == attributeName);

            if (caProvider is TypeDefinition type)
                return type.CustomAttributes.Any (attr => attr.AttributeType.Name == attributeName);

            return false;
        }
    }
}
