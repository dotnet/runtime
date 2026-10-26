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
    /// <summary>
    /// Verifies the IL and metadata of modules read through <see cref="PEReader"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Verifier"/> is not thread-safe. Do not call its methods, or enumerate sequences it returned,
    /// on more than one thread at the same time. Calls that do not overlap may come from different threads.
    /// </para>
    /// <para>
    /// The <c>Verify</c> methods return lazily evaluated sequences. Verification, including the
    /// <see cref="IResolver"/> calls it needs, happens while the returned sequence is enumerated.
    /// <see cref="SetSystemModuleName"/> calls the <see cref="IResolver"/> immediately.
    /// </para>
    /// <para>
    /// The verifier creates a module for each <see cref="PEReader"/> it uses, whether passed to a <c>Verify</c>
    /// method or returned by the <see cref="IResolver"/>. It may keep reading from that reader after the enumeration
    /// that first used it has completed. Keep each such <see cref="PEReader"/> undisposed until the
    /// <see cref="Verifier"/> and every sequence it returned are no longer used. Keep the memory it reads alive and
    /// unchanged for the same period. The verifier does not dispose these readers.
    /// </para>
    /// </remarks>
    public class Verifier
    {
        private Lazy<ResourceManager> _stringResourceManager =
            new Lazy<ResourceManager>(() => new ResourceManager("ILVerification.Strings", typeof(Verifier).GetTypeInfo().Assembly));

        private ILVerifyTypeSystemContext _typeSystemContext;
        private VerifierOptions _verifierOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Verifier"/> class that uses the specified resolver and the
        /// default options.
        /// </summary>
        /// <param name="resolver">
        /// The resolver that provides a <see cref="PEReader"/> for each assembly and module that the verifier needs.
        /// </param>
        public Verifier(IResolver resolver) : this(resolver, null){ }

        /// <summary>
        /// Initializes a new instance of the <see cref="Verifier"/> class that uses the specified resolver and options.
        /// </summary>
        /// <param name="resolver">
        /// The resolver that provides a <see cref="PEReader"/> for each assembly and module that the verifier needs.
        /// </param>
        /// <param name="verifierOptions">
        /// The options that control verification, or <see langword="null"/> to use the default options.
        /// </param>
        public Verifier(IResolver resolver, VerifierOptions verifierOptions)
            : this(new ILVerifyTypeSystemContext(resolver) { AllowIgnoresAccessChecksTo = verifierOptions?.AllowIgnoresAccessChecksTo == true }, verifierOptions) { }

        internal Verifier(ILVerifyTypeSystemContext context, VerifierOptions verifierOptions)
        {
            _typeSystemContext = context;
            _verifierOptions = verifierOptions ?? new VerifierOptions();
        }

        /// <summary>
        /// Sets the assembly that defines the core types, such as <see cref="object"/>, that verification needs.
        /// </summary>
        /// <param name="name">The name of the assembly, for example <c>System.Private.CoreLib</c>.</param>
        /// <remarks>
        /// Call this method before enumerating a sequence returned by a <c>Verify</c> method; otherwise, the
        /// enumeration throws an exception. This method asks the <see cref="IResolver"/> for the assembly immediately,
        /// and throws an exception if the resolver returns <see langword="null"/>.
        /// </remarks>
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

        /// <summary>
        /// Verifies the IL of each method defined in a module.
        /// </summary>
        /// <param name="peReader">The reader for the module to verify.</param>
        /// <returns>
        /// A sequence of the problems found. Verification happens while the sequence is enumerated. Call
        /// <see cref="SetSystemModuleName"/> before enumerating the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="peReader"/> is <see langword="null"/>. The exception is thrown when the returned sequence is
        /// enumerated, not when this method is called.
        /// </exception>
        /// <remarks>
        /// Methods that have no IL body are skipped. This method does not verify type metadata; use
        /// <see cref="Verify(PEReader, TypeDefinitionHandle, bool)"/> for that.
        /// </remarks>
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

        /// <summary>
        /// Verifies the metadata of a type definition and, optionally, the IL of the methods that the type defines.
        /// </summary>
        /// <param name="peReader">The reader for the module that defines the type.</param>
        /// <param name="typeHandle">The type definition to verify.</param>
        /// <param name="verifyMethods">
        /// <see langword="true"/> to also verify the IL of the methods that the type defines; otherwise,
        /// <see langword="false"/>.
        /// </param>
        /// <returns>
        /// A sequence of the problems found. Verification happens while the sequence is enumerated. Call
        /// <see cref="SetSystemModuleName"/> before enumerating the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="peReader"/> is <see langword="null"/>, or <paramref name="typeHandle"/> is a nil handle.
        /// The exception is thrown when the returned sequence is enumerated, not when this method is called.
        /// </exception>
        /// <remarks>
        /// Nested types are not verified.
        /// </remarks>
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

        /// <summary>
        /// Verifies the IL of a method.
        /// </summary>
        /// <param name="peReader">The reader for the module that defines the method.</param>
        /// <param name="methodHandle">The method definition to verify.</param>
        /// <returns>
        /// A sequence of the problems found, which is empty if the method has no IL body. Verification happens while
        /// the sequence is enumerated. Call <see cref="SetSystemModuleName"/> before enumerating the sequence.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="peReader"/> is <see langword="null"/>, or <paramref name="methodHandle"/> is a nil
        /// handle. The exception is thrown when the returned sequence is enumerated, not when this method is called.
        /// </exception>
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
                        Type = typeHandle,
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

    /// <summary>
    /// Provides options that control what a <see cref="Verifier"/> reports.
    /// </summary>
    public class VerifierOptions
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the names of types and methods in the results of type
        /// verification include their metadata tokens.
        /// </summary>
        /// <remarks>
        /// The names are in <see cref="VerificationResult.Args"/>. The default is <see langword="false"/>.
        /// </remarks>
        public bool IncludeMetadataTokensInErrorMessages { get; set; }
        /// <summary>
        /// Gets or sets a value that indicates whether the verifier also reports valid IL that is likely a mistake.
        /// </summary>
        /// <remarks>
        /// When this property is <see langword="true"/>, the verifier reports
        /// <see cref="VerifierError.ThrowOrCatchOnlyExceptionType"/> for a method that throws or catches a type that
        /// is neither <see cref="object"/> nor assignable to <see cref="Exception"/>. The default is
        /// <see langword="false"/>.
        /// </remarks>
        public bool SanityChecks { get; set; }

        /// <summary>
        /// Respect IgnoresAccessChecksToAttribute. Off by default, since code that relies on it is not verifiable.
        /// </summary>
        public bool AllowIgnoresAccessChecksTo { get; set; }
    }
}
