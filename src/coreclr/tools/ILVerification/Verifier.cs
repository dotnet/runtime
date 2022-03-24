// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
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

        public void SetSystemModuleName(AssemblyName name)
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
                results = VerifyMethods(module, module.MetadataReader.MethodDefinitions);
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

                results = VerifyType(module, typeHandle);

                if (verifyMethods)
                {
                    TypeDefinition typeDef = metadataReader.GetTypeDefinition(typeHandle);
                    results = results.Union(VerifyMethods(module, typeDef.GetMethods()));
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
                results = VerifyMethods(module, new[] { methodHandle });
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

        private IEnumerable<VerificationResult> VerifyMethods(EcmaModule module, IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            foreach (var methodHandle in methodHandles)
            {
                var method = (EcmaMethod)module.GetMethod(methodHandle);
                var methodIL = EcmaMethodIL.Create(method);

                if (methodIL != null)
                {
                    var results = VerifyMethod(module, methodIL, methodHandle);
                    foreach (var result in results)
                    {
                        yield return result;
                    }
                }
            }
        }

        private IEnumerable<VerificationResult> VerifyMethod(EcmaModule module, MethodIL methodIL, MethodDefinitionHandle methodHandle)
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
                reportException(e);
            }
            catch (TypeSystemException e)
            {
                reportTypeSystemException(e);
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

        private IEnumerable<VerificationResult> VerifyType(EcmaModule module, TypeDefinitionHandle typeHandle)
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
                reportException(e);
            }
            catch (TypeSystemException e)
            {
                reportException(e);
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
