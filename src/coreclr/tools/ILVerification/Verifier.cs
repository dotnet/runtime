// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeVerifier;

namespace ILVerify
{
    public class Verifier
    {
        private Lazy<ResourceManager> _stringResourceManager =
            new Lazy<ResourceManager>(() => new ResourceManager("ILVerification.Strings", typeof(Verifier).GetTypeInfo().Assembly));

        private ILVerifyTypeSystemContext _typeSystemContext;
        private VerifierOptions _verifierOptions;

        public Verifier(IResolver resolver) : this(resolver, null){ }

        public Verifier(IResolver resolver, VerifierOptions verifierOptions) : this(new ILVerifyTypeSystemContext(resolver), verifierOptions) { }

        internal Verifier(ILVerifyTypeSystemContext context, VerifierOptions verifierOptions)
        {
            _typeSystemContext = context;
            _verifierOptions = verifierOptions ?? new VerifierOptions();
        }

        public void SetSystemModuleName(AssemblyNameInfo name)
        {
            PEReader peReader = _typeSystemContext._resolver.ResolveAssembly(name);
            if (peReader is null)
            {
                throw new VerifierException("Assembly or module not found: " + name.FullName);
            }
            _typeSystemContext.SetSystemModule(_typeSystemContext.GetModule(peReader));
        }

        internal EcmaModule GetModule(PEReader peReader)
        {
            return _typeSystemContext.GetModule(peReader);
        }

        public IEnumerable<VerificationResult> Verify(PEReader peReader)
            => Verify(peReader, Array.Empty<VerificationResult>());

        internal IEnumerable<VerificationResult> Verify(
            PEReader peReader,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            if (_typeSystemContext.SystemModule == null)
            {
                ThrowMissingSystemModule();
            }

            IEnumerable<VerificationResult> results;
            try
            {
                EcmaModule module = GetModule(peReader);
                results = VerifyMethods(module, module.MetadataReader.MethodDefinitions, metadataErrors);
            }
            catch (VerifierException e)
            {
                results = new[] { new VerificationResult() { Message = e.Message } };
            }

            foreach (var result in results)
            {
                yield return result;
            }
        }

        public IEnumerable<VerificationResult> Verify(PEReader peReader, TypeDefinitionHandle typeHandle, bool verifyMethods = false)
            => Verify(peReader, typeHandle, verifyMethods, Array.Empty<VerificationResult>());

        internal IEnumerable<VerificationResult> Verify(
            PEReader peReader,
            TypeDefinitionHandle typeHandle,
            bool verifyMethods,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            if (typeHandle.IsNil)
            {
                throw new ArgumentNullException(nameof(typeHandle));
            }

            if (_typeSystemContext.SystemModule == null)
            {
                ThrowMissingSystemModule();
            }

            IEnumerable<VerificationResult> results;
            try
            {
                EcmaModule module = GetModule(peReader);
                MetadataReader metadataReader = peReader.GetMetadataReader();

                results = VerifyType(module, typeHandle, metadataErrors);

                if (verifyMethods)
                {
                    TypeDefinition typeDef = metadataReader.GetTypeDefinition(typeHandle);
                    results = results.Union(VerifyMethods(module, typeDef.GetMethods(), metadataErrors));
                }
            }
            catch (VerifierException e)
            {
                results = new[] { new VerificationResult() { Message = e.Message } };
            }

            foreach (var result in results)
            {
                yield return result;
            }
        }

        public IEnumerable<VerificationResult> Verify(PEReader peReader, MethodDefinitionHandle methodHandle)
            => Verify(peReader, methodHandle, Array.Empty<VerificationResult>());

        internal IEnumerable<VerificationResult> Verify(
            PEReader peReader,
            MethodDefinitionHandle methodHandle,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            if (methodHandle.IsNil)
            {
                throw new ArgumentNullException(nameof(methodHandle));
            }

            if (_typeSystemContext.SystemModule == null)
            {
                ThrowMissingSystemModule();
            }

            IEnumerable<VerificationResult> results;
            try
            {
                EcmaModule module = GetModule(peReader);
                results = VerifyMethods(module, new[] { methodHandle }, metadataErrors);
            }
            catch (VerifierException e)
            {
                results = new[] { new VerificationResult() { Message = e.Message } };
            }

            foreach (var result in results)
            {
                yield return result;
            }
        }

        internal IEnumerable<VerificationResult> VerifyMetadataReferences(PEReader peReader)
        {
            EcmaModule module = GetModule(peReader);
            MetadataReader reader = module.MetadataReader;

            foreach (EntityHandle handle in EnumerateReferenceHandles(reader))
            {
                VerificationResult result = TryResolveMetadataHandle(module, handle);
                if (result != null)
                {
                    yield return result;
                }
            }
        }

        private static IEnumerable<EntityHandle> EnumerateReferenceHandles(MetadataReader reader)
        {
            foreach (AssemblyReferenceHandle handle in reader.AssemblyReferences)
            {
                yield return handle;
            }

            // ModuleRef is also used as ImplMap.ImportScope to store unmanaged library names for
            // P/Invoke. Do not try to resolve those entries as managed netmodules.
            HashSet<ModuleReferenceHandle> pInvokeModuleReferences = GetPInvokeModuleReferences(reader);
            for (int row = 1;
                 row <= reader.GetTableRowCount(TableIndex.ModuleRef);
                 row++)
            {
                ModuleReferenceHandle handle = MetadataTokens.ModuleReferenceHandle(row);
                if (!pInvokeModuleReferences.Contains(handle))
                {
                    yield return handle;
                }
            }

            foreach (TypeReferenceHandle handle in reader.TypeReferences)
            {
                yield return handle;
            }

            foreach (MemberReferenceHandle handle in reader.MemberReferences)
            {
                yield return handle;
            }

            foreach (ExportedTypeHandle handle in reader.ExportedTypes)
            {
                yield return handle;
            }

            for (int row = 1;
                 row <= reader.GetTableRowCount(TableIndex.TypeSpec);
                 row++)
            {
                yield return MetadataTokens.TypeSpecificationHandle(row);
            }

            for (int row = 1;
                 row <= reader.GetTableRowCount(TableIndex.MethodSpec);
                 row++)
            {
                yield return MetadataTokens.MethodSpecificationHandle(row);
            }

            for (int row = 1;
                 row <= reader.GetTableRowCount(TableIndex.StandAloneSig);
                 row++)
            {
                yield return MetadataTokens.StandaloneSignatureHandle(row);
            }
        }

        private static HashSet<ModuleReferenceHandle> GetPInvokeModuleReferences(MetadataReader reader)
        {
            var moduleReferences = new HashSet<ModuleReferenceHandle>();

            foreach (MethodDefinitionHandle handle in reader.MethodDefinitions)
            {
                ModuleReferenceHandle module = reader.GetMethodDefinition(handle).GetImport().Module;
                if (!module.IsNil)
                {
                    moduleReferences.Add(module);
                }
            }

            return moduleReferences;
        }

        private static VerificationResult TryResolveMetadataHandle(EcmaModule module, EntityHandle handle)
        {
            try
            {
                if (handle.Kind == HandleKind.StandaloneSignature)
                {
                    ResolveStandaloneSignature(module, (StandaloneSignatureHandle)handle);
                }
                else
                {
                    module.GetObject(handle);
                }

                return null;
            }
            catch (TypeSystemException e)
            {
                return createVerificationResult(
                    e.Message,
                    e.StringID,
                    exceptionArguments: e.Arguments);
            }
            catch (BadImageFormatException e)
            {
                return createVerificationResult(e.Message);
            }
            catch (InvalidProgramException e)
            {
                return createVerificationResult(e.Message);
            }
            catch (VerifierException e)
            {
                return createVerificationResult(e.Message, code: e.Code);
            }
            catch (NotImplementedException e)
            {
                return new VerificationResult
                {
                    Code = VerifierError.TokenResolve,
                    MetadataHandle = handle,
                    ErrorArguments = Array.Empty<ErrorArgument>(),
                    Message = $"Unable to validate metadata reference ({handle.Kind}) because this metadata form is not supported: {e.Message}"
                };
            }

            VerificationResult createVerificationResult(
                string message,
                ExceptionStringID? exceptionID = null,
                VerifierError code = VerifierError.None,
                IReadOnlyList<string> exceptionArguments = null)
            {
                if (code == VerifierError.None && exceptionID == null)
                {
                    code = VerifierError.TokenResolve;
                }

                return new VerificationResult
                {
                    Code = code,
                    ExceptionID = exceptionID,
                    MetadataHandle = handle,
                    ErrorArguments = exceptionArguments == null ? Array.Empty<ErrorArgument>()
                        : new[]
                        {
                            new ErrorArgument(nameof(TypeSystemException.Arguments),
                                exceptionArguments.ToArray())
                        },
                    Message = $"Unable to resolve metadata reference ({handle.Kind}): {message}"
                };
            }
        }

        private static void ResolveStandaloneSignature(EcmaModule module, StandaloneSignatureHandle handle)
        {
            MetadataReader reader = module.MetadataReader;
            StandaloneSignature signature = reader.GetStandaloneSignature(handle);

            if (signature.GetKind() == StandaloneSignatureKind.LocalVariables)
            {
                // Local-variable signature
                var parser = new EcmaSignatureParser(module, reader.GetBlobReader(signature.Signature), NotFoundBehavior.Throw);
                parser.ParseLocalsSignature();
            }
            else
            {
                // Method signature (calli)
                module.GetObject(handle);
            }
        }


        private IEnumerable<VerificationResult> VerifyMethods(
            EcmaModule module,
            IEnumerable<MethodDefinitionHandle> methodHandles,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            foreach (var methodHandle in methodHandles)
            {
                var method = module.GetMethod(methodHandle);
                var methodIL = EcmaMethodIL.Create(method);

                if (methodIL != null)
                {
                    var results = VerifyMethod(module, methodIL, methodHandle, metadataErrors);
                    foreach (var result in results)
                    {
                        yield return result;
                    }
                }
            }
        }

        private IEnumerable<VerificationResult> VerifyMethod(
            EcmaModule module,
            MethodIL methodIL,
            MethodDefinitionHandle methodHandle,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            var builder = new ArrayBuilder<VerificationResult>();
            MethodDesc method = methodIL.OwningMethod;

            try
            {
                var importer = new ILImporter(method, methodIL)
                {
                    SanityChecks = _verifierOptions.SanityChecks
                };

                importer.ReportVerificationError = (args, code) =>
                {
                    var codeResource = _stringResourceManager.Value.GetString(code.ToString(), CultureInfo.InvariantCulture);

                    builder.Add(new VerificationResult()
                    {
                        Code = code,
                        Method = methodHandle,
                        ErrorArguments = args,
                        Message = string.IsNullOrEmpty(codeResource) ? code.ToString() : codeResource
                    });
                };

                importer.Verify();
            }
            catch (VerificationException)
            {
                // a result was reported already (before aborting)
            }
            catch (BadImageFormatException)
            {
                builder.Add(new VerificationResult()
                {
                    Method = methodHandle,
                    Message = "Unable to resolve token"
                });
            }
            catch (NotImplementedException e)
            {
                reportException(e);
            }
            catch (InvalidProgramException e)
            {
                reportException(e);
            }
            catch (PlatformNotSupportedException e)
            {
                reportException(e);
            }
            catch (VerifierException e)
            {
                builder.Add(new VerificationResult()
                {
                    Code = e.Code,
                    Method = methodHandle,
                    ErrorArguments = Array.Empty<ErrorArgument>(),
                    Message = e.Message
                });
            }
            catch (TypeSystemException e)
            {
                if (!IsDuplicateMetadataResolutionError(e, metadataErrors))
                {
                    reportTypeSystemException(e);
                }
            }

            return builder.ToArray();

            void reportException(Exception e)
            {
                builder.Add(new VerificationResult()
                {
                    Method = methodHandle,
                    Message = e.Message
                });
            }

            void reportTypeSystemException(TypeSystemException e)
            {
                builder.Add(new VerificationResult()
                {
                    ExceptionID = e.StringID,
                    Method = methodHandle,
                    Message = e.Message
                });
            }
        }

        private IEnumerable<VerificationResult> VerifyType(
            EcmaModule module,
            TypeDefinitionHandle typeHandle,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            var builder = new ArrayBuilder<VerificationResult>();

            try
            {
                TypeVerifier typeVerifier = new TypeVerifier(module, typeHandle, _typeSystemContext, _verifierOptions);

                typeVerifier.ReportVerificationError = (code, args) =>
                {
                    builder.Add(new VerificationResult()
                    {
                        Code = code,
                        Message = $"[MD]: Error: {_stringResourceManager.Value.GetString(code.ToString(), CultureInfo.InvariantCulture)}",
                        Args = args
                    });
                };

                typeVerifier.Verify();
            }
            catch (BadImageFormatException)
            {
                builder.Add(new VerificationResult()
                {
                    Type = typeHandle,
                    Message = "Unable to resolve token"
                });
            }
            catch (NotImplementedException e)
            {
                reportException(e);
            }
            catch (InvalidProgramException e)
            {
                reportException(e);
            }
            catch (PlatformNotSupportedException e)
            {
                reportException(e);
            }
            catch (VerifierException e)
            {
                builder.Add(new VerificationResult()
                {
                    Code = e.Code,
                    Type = typeHandle,
                    ErrorArguments = Array.Empty<ErrorArgument>(),
                    // Type verification results are printed directly, so metadata errors include the prefix in the message.
                    Message = e.Code == VerifierError.None ? e.Message : $"[MD]: Error: {e.Message}"
                });
            }
            catch (TypeSystemException e)
            {
                if (!IsDuplicateMetadataResolutionError(e, metadataErrors))
                {
                    reportException(e);
                }
            }

            return builder.ToArray();

            void reportException(Exception e)
            {
                builder.Add(new VerificationResult()
                {
                    Type = typeHandle,
                    Message = e.Message
                });
            }
        }

        private static bool IsDuplicateMetadataResolutionError(
            TypeSystemException exception,
            IReadOnlyCollection<VerificationResult> metadataErrors)
        {
            if (!CanDeduplicateMetadataResolutionException(exception.StringID))
            {
                return false;
            }

            foreach (VerificationResult metadataError in metadataErrors)
            {
                if (metadataError.ExceptionID == exception.StringID &&
                    metadataError.Message.EndsWith(exception.Message, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool CanDeduplicateMetadataResolutionException(ExceptionStringID exceptionID)
            => exceptionID is ExceptionStringID.ClassLoadGeneral
                or ExceptionStringID.ClassLoadExplicitGeneric
                or ExceptionStringID.ClassLoadBadFormat
                or ExceptionStringID.ClassLoadExplicitLayout
                or ExceptionStringID.ClassLoadValueClassTooLarge
                or ExceptionStringID.ClassLoadRankTooLarge
                or ExceptionStringID.ClassLoadInlineArrayFieldCount
                or ExceptionStringID.ClassLoadInlineArrayLength
                or ExceptionStringID.ClassLoadInlineArrayExplicit
                or ExceptionStringID.ClassLoadInlineArrayExplicitSize

                or ExceptionStringID.MissingMethod
                or ExceptionStringID.MissingField

                or ExceptionStringID.FileLoadErrorGeneric;

        private void ThrowMissingSystemModule()
        {
            throw new VerifierException("No system module specified");
        }
    }

    public class VerifierOptions
    {
        public bool IncludeMetadataTokensInErrorMessages { get; set; }
        public bool SanityChecks { get; set; }
    }
}
