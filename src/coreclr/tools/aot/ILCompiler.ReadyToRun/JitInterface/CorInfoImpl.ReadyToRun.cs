// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using Internal.CorConstants;
using Internal.Pgo;
using Internal.ReadyToRunConstants;

using ILCompiler;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using System.Text;
using System.Runtime.CompilerServices;

namespace Internal.JitInterface
{
    class InfiniteCompileStress
    {
        public static bool Enabled;
        static InfiniteCompileStress()
        {
            Enabled = JitConfigProvider.Instance.GetIntConfigValue("InfiniteCompileStress", 0) != 0;
        }
    }

    internal class RequiresRuntimeJitIfUsedSymbol
    {
        public RequiresRuntimeJitIfUsedSymbol(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public class FieldWithToken : IEquatable<FieldWithToken>
    {
        public readonly FieldDesc Field;
        public readonly ModuleToken Token;
        public readonly bool OwningTypeNotDerivedFromToken;

        public FieldWithToken(FieldDesc field, ModuleToken token)
        {
            Field = field;
            Token = token;
            if (token.TokenType == CorTokenType.mdtMemberRef)
            {
                var memberRef = token.MetadataReader.GetMemberReference((MemberReferenceHandle)token.Handle);
                switch (memberRef.Parent.Kind)
                {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    OwningTypeNotDerivedFromToken = token.Module.GetType(memberRef.Parent) != field.OwningType;
                    break;

                default:
                    break;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is FieldWithToken fieldWithToken &&
                Equals(fieldWithToken);
        }

        public override int GetHashCode()
        {
            return Field.GetHashCode() ^ unchecked(17 * Token.GetHashCode());
        }

        public bool Equals(FieldWithToken fieldWithToken)
        {
            if (fieldWithToken == null)
                return false;

            return Field == fieldWithToken.Field && Token.Equals(fieldWithToken.Token);
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledFieldName(Field));
            sb.Append("; ");
            sb.Append(Token.ToString());
        }

        public override string ToString()
        {
            StringBuilder debuggingName = new StringBuilder();
            debuggingName.Append(Field.ToString());

            debuggingName.Append("; ");
            debuggingName.Append(Token.ToString());

            return debuggingName.ToString();
        }

        public int CompareTo(FieldWithToken other, TypeSystemComparer comparer)
        {
            int result;

            result = comparer.Compare(Field, other.Field);
            if (result != 0)
                return result;

            return Token.CompareTo(other.Token);
        }
    }

    public class MethodWithToken
    {
        public readonly MethodDesc Method;
        public readonly ModuleToken Token;
        public readonly TypeDesc ConstrainedType;
        public readonly bool Unboxing;
        public readonly bool OwningTypeNotDerivedFromToken;
        public readonly TypeDesc OwningType;


        public MethodWithToken(MethodDesc method, ModuleToken token, TypeDesc constrainedType, bool unboxing, object context, TypeDesc devirtualizedMethodOwner = null)
        {
            Debug.Assert(!method.IsUnboxingThunk());
            Method = method;
            Token = token;
            ConstrainedType = constrainedType;
            Unboxing = unboxing;
            OwningType = GetMethodTokenOwningType(this, constrainedType, context, devirtualizedMethodOwner, out OwningTypeNotDerivedFromToken);
        }

        private static TypeDesc GetMethodTokenOwningType(MethodWithToken methodToken, TypeDesc constrainedType, object context, TypeDesc devirtualizedMethodOwner, out bool owningTypeNotDerivedFromToken)
        {
            ModuleToken moduleToken = methodToken.Token;
            owningTypeNotDerivedFromToken = false;

            // Strip off method spec details. The owning type is only associated with a MethodDef or a MemberRef
            if (moduleToken.TokenType == CorTokenType.mdtMethodSpec)
            {
                var methodSpecification = moduleToken.MetadataReader.GetMethodSpecification((MethodSpecificationHandle)moduleToken.Handle);
                moduleToken = new ModuleToken(moduleToken.Module, methodSpecification.Method);
            }

            if (moduleToken.TokenType == CorTokenType.mdtMethodDef)
            {
                var methodDefinition = moduleToken.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)moduleToken.Handle);
                return HandleContext(moduleToken.Module, methodDefinition.GetDeclaringType(), methodToken.Method.OwningType, constrainedType, context, devirtualizedMethodOwner, ref owningTypeNotDerivedFromToken);
            }

            // At this point moduleToken must point at a MemberRef.
            Debug.Assert(moduleToken.TokenType == CorTokenType.mdtMemberRef);
            var memberRef = moduleToken.MetadataReader.GetMemberReference((MemberReferenceHandle)moduleToken.Handle);
            switch (memberRef.Parent.Kind)
            {
                case HandleKind.TypeDefinition:
                case HandleKind.TypeReference:
                case HandleKind.TypeSpecification:
                    {
                        Debug.Assert(devirtualizedMethodOwner == null); // Devirtualization is expected to always use a methoddef token
                        return HandleContext(moduleToken.Module, memberRef.Parent, methodToken.Method.OwningType, constrainedType, context, null, ref owningTypeNotDerivedFromToken);
                    }

                default:
                    return methodToken.Method.OwningType;
            }

            TypeDesc HandleContext(IEcmaModule module, EntityHandle handle, TypeDesc methodTargetOwner, TypeDesc constrainedType, object context, TypeDesc devirtualizedMethodOwner, ref bool owningTypeNotDerivedFromToken)
            {
                var tokenOnlyOwningType = module.GetType(handle);
                TypeDesc actualOwningType;

                if (context == null)
                {
                    actualOwningType = methodTargetOwner;
                }
                else
                {
                    Instantiation typeInstantiation;
                    Instantiation methodInstantiation = new Instantiation();

                    if (context is MethodDesc methodContext)
                    {
                        typeInstantiation = methodContext.OwningType.Instantiation;
                        methodInstantiation = methodContext.Instantiation;
                    }
                    else
                    {
                        TypeDesc typeContext = (TypeDesc)context;
                        typeInstantiation = typeContext.Instantiation;
                    }

                    TypeDesc instantiatedOwningType = null;

                    if (devirtualizedMethodOwner != null)
                    {
                        // We might be in a situation where we use the passed in type (devirtualization scenario)
                        // Check to see if devirtualizedMethodOwner actually is a type derived from the type definition in some way.
                        bool derivesFromTypeDefinition = false;
                        TypeDesc currentType = devirtualizedMethodOwner;
                        do
                        {
                            derivesFromTypeDefinition = currentType.GetTypeDefinition() == tokenOnlyOwningType;
                            currentType = currentType.BaseType;
                        } while(currentType != null && !derivesFromTypeDefinition);

                        if (derivesFromTypeDefinition)
                        {
                            instantiatedOwningType = devirtualizedMethodOwner;
                        }
                        else
                        {
                            Debug.Assert(false); // This is expected to fire if and only if we implement devirtualization to default interface methods
                            throw new RequiresRuntimeJitException(methodTargetOwner.ToString());
                        }
                    }

                    if (instantiatedOwningType == null)
                    {
                        instantiatedOwningType = tokenOnlyOwningType.InstantiateSignature(typeInstantiation, methodInstantiation);
                    }

                    var canonicalizedOwningType = instantiatedOwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                    if ((instantiatedOwningType == canonicalizedOwningType) || (constrainedType != null))
                    {
                        actualOwningType = instantiatedOwningType;
                    }
                    else
                    {
                        actualOwningType = ComputeActualOwningType(methodTargetOwner, instantiatedOwningType, canonicalizedOwningType);

                        // Implement via a helper function, so that managing the loop escape behavior is easier to read
                        TypeDesc ComputeActualOwningType(TypeDesc methodTargetOwner, TypeDesc instantiatedOwningType, TypeDesc canonicalizedOwningType)
                        {
                            // Pick between Canonical and Exact OwningTypes.
                            //
                            // If the canonicalizedOwningType is the OwningType (or parent type) of the associated method
                            //   Then return canonicalizedOwningType
                            // Else If the Exact Owning type is the OwningType (or parent type) of the associated method
                            //   Then return actualOwningType
                            // Else If the canonicallized owningType (or canonicalized parent type) of the associated method
                            //   Return the canonicalizedOwningType
                            // Else
                            //   Fail, unexpected behavior
                            var currentType = canonicalizedOwningType;
                            while (currentType != null)
                            {
                                if (currentType == methodTargetOwner)
                                    return canonicalizedOwningType;
                                currentType = currentType.BaseType;
                            }

                            currentType = instantiatedOwningType;
                            while (currentType != null)
                            {
                                if (currentType == methodTargetOwner)
                                    return instantiatedOwningType;
                                currentType = currentType.BaseType;
                            }

                            currentType = canonicalizedOwningType;
                            while (currentType != null)
                            {
                                currentType = currentType.ConvertToCanonForm(CanonicalFormKind.Specific);
                                if (currentType == methodTargetOwner)
                                    return canonicalizedOwningType;
                                currentType = currentType.BaseType;
                            }

                            foreach (DefType interfaceType in methodTargetOwner.RuntimeInterfaces)
                            {
                                if (interfaceType == instantiatedOwningType ||
                                    interfaceType.ConvertToCanonForm(CanonicalFormKind.Specific) == canonicalizedOwningType)
                                {
                                    return methodTargetOwner;
                                }
                            }

                            Debug.Assert(false);
                            throw new Exception();
                        }
                    }
                }

                if (actualOwningType != tokenOnlyOwningType)
                {
                    owningTypeNotDerivedFromToken = true;
                }
                return actualOwningType;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is MethodWithToken methodWithToken &&
                Equals(methodWithToken);
        }

        public override int GetHashCode()
        {
            return Method.GetHashCode() ^ unchecked(17 * Token.GetHashCode()) ^ unchecked (39 * (ConstrainedType?.GetHashCode() ?? 0));
        }

        public bool Equals(MethodWithToken methodWithToken)
        {
            if (methodWithToken == null)
                return false;

            bool equals = Method == methodWithToken.Method && Token.Equals(methodWithToken.Token)
                && OwningType == methodWithToken.OwningType && ConstrainedType == methodWithToken.ConstrainedType
                && Unboxing == methodWithToken.Unboxing;
            if (equals)
            {
                Debug.Assert(OwningTypeNotDerivedFromToken == methodWithToken.OwningTypeNotDerivedFromToken);
                Debug.Assert(OwningType == methodWithToken.OwningType);
            }

            return equals;
        }

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.GetMangledMethodName(Method));
            if (ConstrainedType != null)
            {
                sb.Append(" @ ");
                sb.Append(nameMangler.GetMangledTypeName(ConstrainedType));
            }
            sb.Append("; ");
            sb.Append(Token.ToString());
            if (OwningTypeNotDerivedFromToken)
            {
                sb.Append("; OWNINGTYPE");
                sb.Append(nameMangler.GetMangledTypeName(OwningType));
                sb.Append("; ");
            }
            if (Unboxing)
                sb.Append("; UNBOXING");
        }

        public override string ToString()
        {
            StringBuilder debuggingName = new StringBuilder();
            debuggingName.Append(Method.ToString());
            if (ConstrainedType != null)
            {
                debuggingName.Append(" @ ");
                debuggingName.Append(ConstrainedType.ToString());
            }

            debuggingName.Append("; ");
            debuggingName.Append(Token.ToString());
            if (Unboxing)
                debuggingName.Append("; UNBOXING");

            return debuggingName.ToString();
        }

        public int CompareTo(MethodWithToken other, TypeSystemComparer comparer)
        {
            int result;
            if (ConstrainedType != null || other.ConstrainedType != null)
            {
                if (ConstrainedType == null)
                    return -1;
                else if (other.ConstrainedType == null)
                    return 1;

                result = comparer.Compare(ConstrainedType, other.ConstrainedType);
                if (result != 0)
                    return result;
            }

            result = comparer.Compare(Method, other.Method);
            if (result != 0)
                return result;

            result = Token.CompareTo(other.Token);
            if (result != 0)
                return result;

            result = Unboxing.CompareTo(other.Unboxing);
            if (result != 0)
                return result;

            // The OwningType/OwningTypeNotDerivedFromToken should be equivalent if the above conditions are equal.
            Debug.Assert(OwningTypeNotDerivedFromToken == other.OwningTypeNotDerivedFromToken);
            Debug.Assert(OwningTypeNotDerivedFromToken || (OwningType == other.OwningType));

            if (OwningTypeNotDerivedFromToken != other.OwningTypeNotDerivedFromToken)
            {
                if (OwningTypeNotDerivedFromToken)
                    return 1;
                else
                    return -1;
            }

            return comparer.Compare(OwningType, other.OwningType);
        }
    }

    public struct GenericContext : IEquatable<GenericContext>
    {
        public readonly TypeSystemEntity Context;

        public TypeDesc ContextType { get { return (Context is MethodDesc contextAsMethod ? contextAsMethod.OwningType : (TypeDesc)Context); } }

        public MethodDesc ContextMethod { get { return (MethodDesc)Context; } }

        public GenericContext(TypeSystemEntity context)
        {
            Context = context;
        }

        public bool Equals(GenericContext other) => Context == other.Context;

        public override bool Equals(object obj) => obj is GenericContext other && Context == other.Context;

        public override int GetHashCode() => Context.GetHashCode();

        public void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            if (Context is MethodDesc contextAsMethod)
            {
                sb.Append(nameMangler.GetMangledMethodName(contextAsMethod));
            }
            else
            {
                sb.Append(nameMangler.GetMangledTypeName(ContextType));
            }
        }
    }

    public class RequiresRuntimeJitException : Exception
    {
        public RequiresRuntimeJitException(object reason)
            : base(reason.ToString())
        {
        }
    }

    unsafe partial class CorInfoImpl
    {
        private const CORINFO_RUNTIME_ABI TargetABI = CORINFO_RUNTIME_ABI.CORINFO_CORECLR_ABI;

        private uint OffsetOfDelegateFirstTarget => (uint)(3 * PointerSize); // Delegate::_functionPointer

        private readonly ReadyToRunCodegenCompilation _compilation;
        private MethodWithGCInfo _methodCodeNode;
        private MethodColdCodeNode _methodColdCodeNode;
        private OffsetMapping[] _debugLocInfos;
        private NativeVarInfo[] _debugVarInfos;
        private HashSet<MethodDesc> _inlinedMethods;
        private UnboxingMethodDescFactory _unboxingThunkFactory = new UnboxingMethodDescFactory();
        private List<ISymbolNode> _precodeFixups;
        private List<EcmaMethod> _ilBodiesNeeded;
        private Dictionary<TypeDesc, bool> _preInitedTypes = new Dictionary<TypeDesc, bool>();
        private HashSet<MethodDesc> _synthesizedPgoDependencies;

        public bool HasColdCode { get; private set; }

        public CorInfoImpl(ReadyToRunCodegenCompilation compilation)
            : this()
        {
            _compilation = compilation;
        }

        private void AddPrecodeFixup(ISymbolNode node)
        {
            _precodeFixups = _precodeFixups ?? new List<ISymbolNode>();
            _precodeFixups.Add(node);
        }

        private static mdToken FindGenericMethodArgTypeSpec(EcmaModule module)
        {
            // Find the TypeSpec for "!!0"
            MetadataReader reader = module.MetadataReader;
            int numTypeSpecs = reader.GetTableRowCount(TableIndex.TypeSpec);
            for (int i = 1; i < numTypeSpecs + 1; i++)
            {
                TypeSpecificationHandle handle = MetadataTokens.TypeSpecificationHandle(i);
                BlobHandle typeSpecSigHandle = reader.GetTypeSpecification(handle).Signature;
                BlobReader typeSpecSig = reader.GetBlobReader(typeSpecSigHandle);
                SignatureTypeCode typeCode = typeSpecSig.ReadSignatureTypeCode();
                if (typeCode == SignatureTypeCode.GenericMethodParameter)
                {
                    if (typeSpecSig.ReadByte() == 0)
                    {
                        return (mdToken)MetadataTokens.GetToken(handle);
                    }
                }
            }

            // Should be unreachable - couldn't find a TypeSpec.
            // Are we still compiling CoreLib?
            throw new NotSupportedException();
        }

        public static bool ShouldSkipCompilation(InstructionSetSupport instructionSetSupport, MethodDesc methodNeedingCode)
        {
            if (methodNeedingCode.IsAggressiveOptimization)
            {
                return true;
            }
            if (HardwareIntrinsicHelpers.IsHardwareIntrinsic(methodNeedingCode))
            {
                return true;
            }
            if (methodNeedingCode.IsAbstract)
            {
                return true;
            }
            if (methodNeedingCode.IsInternalCall)
            {
                return true;
            }
            if (methodNeedingCode.OwningType.IsDelegate && (
                methodNeedingCode.IsConstructor ||
                methodNeedingCode.Name == "BeginInvoke" ||
                methodNeedingCode.Name == "Invoke" ||
                methodNeedingCode.Name == "EndInvoke"))
            {
                // Special methods on delegate types
                return true;
            }
            if (ShouldCodeNotBeCompiledIntoFinalImage(instructionSetSupport, methodNeedingCode))
            {
                return true;
            }

            return false;
        }

        public static bool ShouldCodeNotBeCompiledIntoFinalImage(InstructionSetSupport instructionSetSupport, MethodDesc method)
        {
            EcmaMethod ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;

            var metadataReader = ecmaMethod.MetadataReader;
            var stringComparer = metadataReader.StringComparer;

            var handle = ecmaMethod.Handle;

            List<TypeDesc> compExactlyDependsOnList = null;

            foreach (var attributeHandle in metadataReader.GetMethodDefinition(handle).GetCustomAttributes())
            {
                StringHandle namespaceHandle, nameHandle;
                if (!metadataReader.GetAttributeNamespaceAndName(attributeHandle, out namespaceHandle, out nameHandle))
                    continue;

                if (metadataReader.StringComparer.Equals(namespaceHandle, "System.Runtime"))
                {
                    if (metadataReader.StringComparer.Equals(nameHandle, "BypassReadyToRunAttribute"))
                    {
                        return true;
                    }
                }
                else if (metadataReader.StringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                {
                    if (metadataReader.StringComparer.Equals(nameHandle, "CompExactlyDependsOnAttribute"))
                    {
                        var customAttribute = metadataReader.GetCustomAttribute(attributeHandle);
                        var typeProvider = new CustomAttributeTypeProvider(ecmaMethod.Module);
                        var fixedArguments = customAttribute.DecodeValue(typeProvider).FixedArguments;
                        if (fixedArguments.Length < 1)
                            continue;

                        TypeDesc typeForBypass = fixedArguments[0].Value as TypeDesc;
                        if (typeForBypass != null)
                        {
                            if (compExactlyDependsOnList == null)
                                compExactlyDependsOnList = new List<TypeDesc>();

                            compExactlyDependsOnList.Add(typeForBypass);
                        }
                    }
                }
            }

            if (compExactlyDependsOnList != null && compExactlyDependsOnList.Count > 0)
            {
                // Default to true, and set to false if at least one of the types is actually supported in the current environment, and none of the
                // intrinsic types are in an opportunistic state.
                bool doBypass = true;

                foreach (var intrinsicType in compExactlyDependsOnList)
                {
                    InstructionSet instructionSet = InstructionSetParser.LookupPlatformIntrinsicInstructionSet(intrinsicType.Context.Target.Architecture, intrinsicType);
                    if (instructionSet == InstructionSet.ILLEGAL)
                    {
                        // This instruction set isn't supported on the current platform at all.
                        continue;
                    }
                    if (instructionSetSupport.IsInstructionSetSupported(instructionSet) || instructionSetSupport.IsInstructionSetExplicitlyUnsupported(instructionSet))
                    {
                        doBypass = false;
                    }
                    else
                    {
                        // If we reach here this is an instruction set generally supported on this platform, but we don't know what the behavior will be at runtime
                        return true;
                    }
                }

                return doBypass;
            }

            // No reason to bypass compilation and code generation.
            return false;
        }

        private static bool FunctionJustThrows(MethodIL ilBody)
        {
            try
            {
                if (ilBody.GetExceptionRegions().Length != 0)
                    return false;

                ILReader reader = new ILReader(ilBody.GetILBytes());

                while (reader.HasNext)
                {
                    var ilOpcode = reader.ReadILOpcode();
                    if (ilOpcode == ILOpcode.throw_)
                        return true;
                    if (ilOpcode.IsBranch() || ilOpcode == ILOpcode.switch_)
                        return false;
                    reader.Skip(ilOpcode);
                }
            }
            catch
            { }

            return false;
        }

        class DeterminismData
        {
            public int Iterations = 0;
            public int HashCode = 0;
        }

        private static ConditionalWeakTable<MethodDesc, DeterminismData> _determinismTable = new ConditionalWeakTable<MethodDesc, DeterminismData>();

        partial void DetermineIfCompilationShouldBeRetried(ref CompilationResult result)
        {
            if ((_ilBodiesNeeded == null) && _compilation.NodeFactory.OptimizationFlags.DeterminismStress > 0)
            {
                HashCode hashCode = default(HashCode);
                hashCode.AddBytes(_code);
                hashCode.AddBytes(_roData);
                int functionOutputHashCode = hashCode.ToHashCode();

                lock (_determinismTable)
                {
                    DeterminismData data = _determinismTable.GetOrCreateValue(MethodBeingCompiled);
                    if (data.Iterations == 0)
                    {
                        data.Iterations = 1;
                        data.HashCode = functionOutputHashCode;
                    }
                    else
                    {
                        data.Iterations++;
                    }

                    if (data.HashCode != functionOutputHashCode)
                    {
                        _compilation.DeterminismCheckFailed = true;
                        _compilation.Logger.LogMessage($"ERROR: Determinism check compiling method '{MethodBeingCompiled}' failed. Use '{_compilation.GetReproInstructions(MethodBeingCompiled)}' on command line to reproduce the failure.");
                    }
                    else if (data.Iterations <= _compilation.NodeFactory.OptimizationFlags.DeterminismStress)
                    {
                        result = CompilationResult.CompilationRetryRequested;
                    }
                }
            }

            // If any il bodies need to be recomputed, force recompilation
            if ((_ilBodiesNeeded != null) || InfiniteCompileStress.Enabled || result == CompilationResult.CompilationRetryRequested)
            {
                _compilation.PrepareForCompilationRetry(_methodCodeNode, _ilBodiesNeeded);
                result = CompilationResult.CompilationRetryRequested;
            }
        }

        // At the moment, cross module code embedding does not support embedding cross module references within the EH clauses of a method
        // Obviously this could be fixed, but it is not necessary to demonstrate significant value from the feature as written now.
        private static bool FunctionHasNonReferenceableTypedILCatchClause(MethodIL methodIL, CompilationModuleGroup compilationGroup)
        {
            if (!compilationGroup.VersionsWithMethodBody(methodIL.OwningMethod.GetTypicalMethodDefinition()))
            {
                foreach (var clause in methodIL.GetExceptionRegions())
                {
                    if (clause.Kind == ILExceptionRegionKind.Catch)
                        return true;
                }
            }
            return false;
        }

        public static bool IsMethodCompilable(Compilation compilation, MethodDesc method)
        {
            // This logic must mirror the logic in CompileMethod used to get to the point of calling CompileMethodInternal
            if (ShouldSkipCompilation(compilation.InstructionSetSupport, method) || MethodSignatureIsUnstable(method.Signature, out var _))
                return false;

            MethodIL methodIL = compilation.GetMethodIL(method);
            if (methodIL == null)
                return false;

            if (FunctionJustThrows(methodIL))
                return false;

            if (FunctionHasNonReferenceableTypedILCatchClause(methodIL, compilation.NodeFactory.CompilationModuleGroup))
                return false;

            return true;
        }

        public void CompileMethod(MethodWithGCInfo methodCodeNodeNeedingCode, Logger logger)
        {
            bool codeGotPublished = false;
            _methodCodeNode = methodCodeNodeNeedingCode;

            try
            {
                if (ShouldSkipCompilation(_compilation.InstructionSetSupport, MethodBeingCompiled))
                {
                    if (logger.IsVerbose)
                        logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` was not compiled because it is skipped.");
                    return;
                }

                if (MethodSignatureIsUnstable(MethodBeingCompiled.Signature, out var _))
                {
                    if (logger.IsVerbose)
                        logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` was not compiled because it has an non version resilient signature.");
                    return;
                }
                MethodIL methodIL = _compilation.GetMethodIL(MethodBeingCompiled);
                if (methodIL == null)
                {
                    if (logger.IsVerbose)
                        logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` was not compiled because IL code could not be found for the method.");
                    return;
                }

                if (FunctionJustThrows(methodIL))
                {
                    if (logger.IsVerbose)
                        logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` was not compiled because it always throws an exception");
                    return;
                }

                if (FunctionHasNonReferenceableTypedILCatchClause(methodIL, _compilation.NodeFactory.CompilationModuleGroup))
                {
                    if (logger.IsVerbose)
                        logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` was not compiled because it has a non referenceable catch clause");
                    return;
                }

                if (MethodBeingCompiled.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
                {
                    if ((methodIL.GetMethodILScopeDefinition() is IEcmaMethodIL && _compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && ecmaMethod.Module == ecmaMethod.Context.SystemModule) ||
                        (!_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(MethodBeingCompiled) &&
                            _compilation.NodeFactory.CompilationModuleGroup.CrossModuleInlineable(MethodBeingCompiled)))
                    {
                        ISymbolNode ilBodyNode = _compilation.SymbolNodeFactory.CheckILBodyFixupSignature((EcmaMethod)MethodBeingCompiled.GetTypicalMethodDefinition());
                        AddPrecodeFixup(ilBodyNode);
                    }

                    // Harvest the method being compiled for the purpose of populating the type resolver
                    var resolver = _compilation.NodeFactory.Resolver;
                    if (_compilation.CompilationModuleGroup.VersionsWithModule(ecmaMethod.Module))
                    {
                        resolver.AddModuleTokenForMethod(MethodBeingCompiled, new ModuleToken(ecmaMethod.Module, ecmaMethod.Handle));
                    }
                    else
                    {
                        // Cross module optimization scenario. If we aren't able to come up with a token for the method being compiled, then force it to be
                        // computed.
                        // NOTE: Technically, we could arrange to require only the set of tokens necessary to describe the type that owns this method
                        // as well as the parameters and return value to the method. The token for the actual method is not required.
                        EntityHandle? handle = _compilation.NodeFactory.ManifestMetadataTable._mutableModule.TryGetExistingEntityHandle(ecmaMethod);
                        if (handle.HasValue)
                        {
                            resolver.AddModuleTokenForMethod(MethodBeingCompiled, new ModuleToken(_compilation.NodeFactory.ManifestMetadataTable._mutableModule, handle.Value));
                        }
                        else
                        {
                            _ilBodiesNeeded = _ilBodiesNeeded ?? new List<EcmaMethod>();
                            _ilBodiesNeeded.Add(ecmaMethod);
                        }
                    }
                }
                var compilationResult = CompileMethodInternal(methodCodeNodeNeedingCode, methodIL);
                codeGotPublished = true;

                if (compilationResult == CompilationResult.CompilationRetryRequested && logger.IsVerbose)
                {
                    logger.Writer.WriteLine($"Info: Method `{MethodBeingCompiled}` triggered recompilation to acquire stable tokens for cross module inline.");
                }
            }
            finally
            {
                if (!codeGotPublished)
                {
                    PublishEmptyCode();
                }

                HasColdCode = (_methodColdCodeNode != null);
                CompileMethodCleanup();
            }
        }

        private bool getReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_LOOKUP_KIND pGenericLookupKind, CorInfoHelpFunc id, ref CORINFO_CONST_LOOKUP pLookup)
        {
            switch (id)
            {
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NEW:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        Debug.Assert(type.IsDefType);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;

                        pLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.NewHelper, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NEWARR_1:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        Debug.Assert(type.IsSzArray);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;

                        pLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.NewArr1, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_ISINSTANCEOF:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;

                        // ECMA-335 III.4.3:  If typeTok is a nullable type, Nullable<T>, it is interpreted as "boxed" T
                        if (type.IsNullable)
                            type = type.Instantiation[0];

                        pLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.IsInstanceOf, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_CHKCAST:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;

                        // ECMA-335 III.4.3:  If typeTok is a nullable type, Nullable<T>, it is interpreted as "boxed" T
                        if (type.IsNullable)
                            type = type.Instantiation[0];

                        pLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.CastClass, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GCSTATIC_BASE:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_THREADSTATIC_BASE:
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE:
                    {
                        var type = HandleToObject(pResolvedToken.hClass);
                        if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                            return false;
                        var helperId = GetReadyToRunHelperFromStaticBaseHelper(id);
                        pLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(helperId, type));
                    }
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GENERIC_HANDLE:
                    {
                        Debug.Assert(pGenericLookupKind.needsRuntimeLookup);

                        ReadyToRunHelperId helperId = (ReadyToRunHelperId)pGenericLookupKind.runtimeLookupFlags;
                        TypeDesc constrainedType = null;
                        if (helperId == ReadyToRunHelperId.MethodEntry && pGenericLookupKind.runtimeLookupArgs != null)
                        {
                            constrainedType = (TypeDesc)GetRuntimeDeterminedObjectForToken(ref *(CORINFO_RESOLVED_TOKEN*)pGenericLookupKind.runtimeLookupArgs);
                            _compilation.NodeFactory.DetectGenericCycles(MethodBeingCompiled, constrainedType);
                        }
                        object helperArg = GetRuntimeDeterminedObjectForToken(ref pResolvedToken);
                        if (helperArg is MethodDesc methodDesc)
                        {
                            var methodIL = HandleToObject(pResolvedToken.tokenScope);
                            MethodDesc sharedMethod = methodIL.OwningMethod.GetSharedRuntimeFormMethodTarget();
                            _compilation.NodeFactory.DetectGenericCycles(MethodBeingCompiled, sharedMethod);
                            helperArg = new MethodWithToken(methodDesc, HandleToModuleToken(ref pResolvedToken), constrainedType, unboxing: false, context: sharedMethod);
                        }
                        else if (helperArg is FieldDesc fieldDesc)
                        {
                            helperArg = new FieldWithToken(fieldDesc, HandleToModuleToken(ref pResolvedToken));
                        }

                        GenericContext methodContext = new GenericContext(entityFromContext(pResolvedToken.tokenContext));
                        ISymbolNode helper = _compilation.SymbolNodeFactory.GenericLookupHelper(
                            pGenericLookupKind.runtimeLookupKind,
                            helperId,
                            helperArg,
                            methodContext);
                        pLookup = CreateConstLookupToSymbol(helper);
                    }
                    break;
                default:
                    throw new NotImplementedException("ReadyToRun: " + id.ToString());
            }
            return true;
        }

        private void getReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType, ref CORINFO_LOOKUP pLookup)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            fixed (CORINFO_LOOKUP* tmp = &pLookup)
                MemoryHelper.FillMemory((byte*)tmp, 0xcc, sizeof(CORINFO_LOOKUP));
#endif

            TypeDesc delegateTypeDesc = HandleToObject(delegateType);
            MethodDesc targetMethodDesc = HandleToObject(pTargetMethod.hMethod);
            Debug.Assert(!targetMethodDesc.IsUnboxingThunk());

            var typeOrMethodContext = (pTargetMethod.tokenContext == contextFromMethodBeingCompiled()) ?
                MethodBeingCompiled : HandleToObject((void*)pTargetMethod.tokenContext);

            TypeDesc constrainedType = null;
            if (targetConstraint != 0)
            {
                MethodIL methodIL = _compilation.GetMethodIL(MethodBeingCompiled);
                constrainedType = (TypeDesc)ResolveTokenInScope(methodIL, typeOrMethodContext, targetConstraint);
            }

            MethodWithToken targetMethod = new MethodWithToken(
                targetMethodDesc,
                HandleToModuleToken(ref pTargetMethod),
                constrainedType: constrainedType,
                unboxing: false,
                context: typeOrMethodContext);

            pLookup.lookupKind.needsRuntimeLookup = false;
            pLookup.constLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.DelegateCtor(delegateTypeDesc, targetMethod));
        }

        private ISymbolNode GetHelperFtnUncached(CorInfoHelpFunc ftnNum)
        {
            ReadyToRunHelper id;

            switch (ftnNum)
            {
                case CorInfoHelpFunc.CORINFO_HELP_THROW:
                    id = ReadyToRunHelper.Throw;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_RETHROW:
                    id = ReadyToRunHelper.Rethrow;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_OVERFLOW:
                    id = ReadyToRunHelper.Overflow;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_RNGCHKFAIL:
                    id = ReadyToRunHelper.RngChkFail;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FAIL_FAST:
                    id = ReadyToRunHelper.FailFast;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROWNULLREF:
                    id = ReadyToRunHelper.ThrowNullRef;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_THROWDIVZERO:
                    id = ReadyToRunHelper.ThrowDivZero;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF:
                    id = ReadyToRunHelper.WriteBarrier;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF:
                    id = ReadyToRunHelper.CheckedWriteBarrier;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_BYREF:
                    id = ReadyToRunHelper.ByRefWriteBarrier;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ARRADDR_ST:
                    id = ReadyToRunHelper.Stelem_Ref;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LDELEMA_REF:
                    id = ReadyToRunHelper.Ldelema_Ref;
                    break;


                case CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
                    id = ReadyToRunHelper.GenericGcStaticBase;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
                    id = ReadyToRunHelper.GenericNonGcStaticBase;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE:
                    id = ReadyToRunHelper.GenericGcTlsBase;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE:
                    id = ReadyToRunHelper.GenericNonGcTlsBase;
                    break;


                case CorInfoHelpFunc.CORINFO_HELP_MEMSET:
                    id = ReadyToRunHelper.MemSet;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MEMZERO:
                    id = ReadyToRunHelper.MemZero;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MEMCPY:
                    id = ReadyToRunHelper.MemCpy;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NATIVE_MEMSET:
                    id = ReadyToRunHelper.NativeMemSet;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_METHODDESC_TO_STUBRUNTIMEMETHOD:
                    id = ReadyToRunHelper.GetRuntimeMethodHandle;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD:
                    id = ReadyToRunHelper.GetRuntimeFieldHandle;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE:
                    id = ReadyToRunHelper.GetRuntimeTypeHandle;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOF_EXCEPTION:
                    id = ReadyToRunHelper.IsInstanceOfException;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_BOX:
                    id = ReadyToRunHelper.Box;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_BOX_NULLABLE:
                    id = ReadyToRunHelper.Box_Nullable;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UNBOX:
                    id = ReadyToRunHelper.Unbox;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UNBOX_NULLABLE:
                    id = ReadyToRunHelper.Unbox_Nullable;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEW_MDARR:
                    id = ReadyToRunHelper.NewMultiDimArr;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWFAST:
                    id = ReadyToRunHelper.NewObject;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_DIRECT:
                    id = ReadyToRunHelper.NewArray;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_MAYBEFROZEN:
                    id = ReadyToRunHelper.NewMaybeFrozenArray;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_NEWFAST_MAYBEFROZEN:
                    id = ReadyToRunHelper.NewMaybeFrozenObject;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_VIRTUAL_FUNC_PTR:
                    id = ReadyToRunHelper.VirtualFuncPtr;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_READYTORUN_VIRTUAL_FUNC_PTR:
                    id = ReadyToRunHelper.VirtualFuncPtr;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LMUL:
                    id = ReadyToRunHelper.LMul;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LMUL_OVF:
                    id = ReadyToRunHelper.LMulOfv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULMUL_OVF:
                    id = ReadyToRunHelper.ULMulOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LDIV:
                    id = ReadyToRunHelper.LDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LMOD:
                    id = ReadyToRunHelper.LMod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULDIV:
                    id = ReadyToRunHelper.ULDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULMOD:
                    id = ReadyToRunHelper.ULMod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LLSH:
                    id = ReadyToRunHelper.LLsh;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LRSH:
                    id = ReadyToRunHelper.LRsh;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LRSZ:
                    id = ReadyToRunHelper.LRsz;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_LNG2DBL:
                    id = ReadyToRunHelper.Lng2Dbl;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ULNG2DBL:
                    id = ReadyToRunHelper.ULng2Dbl;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_DIV:
                    id = ReadyToRunHelper.Div;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MOD:
                    id = ReadyToRunHelper.Mod;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UDIV:
                    id = ReadyToRunHelper.UDiv;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_UMOD:
                    id = ReadyToRunHelper.UMod;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_DBL2INT:
                    id = ReadyToRunHelper.Dbl2Int;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2INT_OVF:
                    id = ReadyToRunHelper.Dbl2IntOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2LNG:
                    id = ReadyToRunHelper.Dbl2Lng;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2LNG_OVF:
                    id = ReadyToRunHelper.Dbl2LngOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2UINT:
                    id = ReadyToRunHelper.Dbl2UInt;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2UINT_OVF:
                    id = ReadyToRunHelper.Dbl2UIntOvf;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2ULNG:
                    id = ReadyToRunHelper.Dbl2ULng;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBL2ULNG_OVF:
                    id = ReadyToRunHelper.Dbl2ULngOvf;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_FLTREM:
                    id = ReadyToRunHelper.FltRem;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBLREM:
                    id = ReadyToRunHelper.DblRem;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_FLTROUND:
                    id = ReadyToRunHelper.FltRound;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_DBLROUND:
                    id = ReadyToRunHelper.DblRound;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_CHKCASTANY:
                    id = ReadyToRunHelper.CheckCastAny;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY:
                    id = ReadyToRunHelper.CheckInstanceAny;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_MON_ENTER:
                    id = ReadyToRunHelper.MonitorEnter;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_MON_EXIT:
                    id = ReadyToRunHelper.MonitorExit;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_EAX:
                    id = ReadyToRunHelper.WriteBarrier_EAX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_EBX:
                    id = ReadyToRunHelper.WriteBarrier_EBX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_ECX:
                    id = ReadyToRunHelper.WriteBarrier_ECX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_ESI:
                    id = ReadyToRunHelper.WriteBarrier_ESI;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_EDI:
                    id = ReadyToRunHelper.WriteBarrier_EDI;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_ASSIGN_REF_EBP:
                    id = ReadyToRunHelper.WriteBarrier_EBP;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_EAX:
                    id = ReadyToRunHelper.CheckedWriteBarrier_EAX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_EBX:
                    id = ReadyToRunHelper.CheckedWriteBarrier_EBX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_ECX:
                    id = ReadyToRunHelper.CheckedWriteBarrier_ECX;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_ESI:
                    id = ReadyToRunHelper.CheckedWriteBarrier_ESI;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_EDI:
                    id = ReadyToRunHelper.CheckedWriteBarrier_EDI;
                    break;
                case CorInfoHelpFunc.CORINFO_HELP_CHECKED_ASSIGN_REF_EBP:
                    id = ReadyToRunHelper.CheckedWriteBarrier_EBP;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_ENDCATCH:
                    id = ReadyToRunHelper.EndCatch;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_PINVOKE_BEGIN:
                    id = ReadyToRunHelper.PInvokeBegin;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_PINVOKE_END:
                    id = ReadyToRunHelper.PInvokeEnd;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_BBT_FCN_ENTER:
                    id = ReadyToRunHelper.LogMethodEnter;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_STACK_PROBE:
                    id = ReadyToRunHelper.StackProbe;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_POLL_GC:
                    id = ReadyToRunHelper.GCPoll;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_GETCURRENTMANAGEDTHREADID:
                    id = ReadyToRunHelper.GetCurrentManagedThreadId;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER:
                    id = ReadyToRunHelper.ReversePInvokeEnter;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT:
                    id = ReadyToRunHelper.ReversePInvokeExit;
                    break;

                case CorInfoHelpFunc.CORINFO_HELP_INITCLASS:
                case CorInfoHelpFunc.CORINFO_HELP_INITINSTCLASS:
                case CorInfoHelpFunc.CORINFO_HELP_GETSYNCFROMCLASSHANDLE:
                case CorInfoHelpFunc.CORINFO_HELP_GETCLASSFROMMETHODPARAM:
                case CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTEXCEPTION:
                case CorInfoHelpFunc.CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION:
                case CorInfoHelpFunc.CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED:
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL:
                case CorInfoHelpFunc.CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL:
                case CorInfoHelpFunc.CORINFO_HELP_GETREFANY:
                case CorInfoHelpFunc.CORINFO_HELP_NEW_MDARR_RARE:
                // For Vector256.Create and similar cases
                case CorInfoHelpFunc.CORINFO_HELP_THROW_NOT_IMPLEMENTED:
                // For x86 tailcall where helper is required we need runtime JIT.
                case CorInfoHelpFunc.CORINFO_HELP_TAILCALL:
                    throw new RequiresRuntimeJitException(ftnNum.ToString());

                default:
                    throw new NotImplementedException(ftnNum.ToString());
            }

            return _compilation.NodeFactory.GetReadyToRunHelperCell(id);
        }

        private void getFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
        {
            throw new RequiresRuntimeJitException(HandleToObject(ftn).ToString());
        }

        private bool canTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix)
        {
            if (!fIsTailPrefix)
            {
                MethodDesc caller = HandleToObject(callerHnd);

                // Do not tailcall out of the entry point as it results in a confusing debugger experience.
                if (caller is EcmaMethod em && em.Module.EntryPoint == caller)
                {
                    return false;
                }

                // Do not tailcall from methods that are marked as noinline (people often use no-inline
                // to mean "I want to always see this method in stacktrace")
                if (caller.IsNoInlining)
                {
                    return false;
                }

                // Methods with StackCrawlMark depend on finding their caller on the stack.
                // If we tail call one of these guys, they get confused.  For lack of
                // a better way of identifying them, we use DynamicSecurity attribute to identify
                // them.
                //
                MethodDesc callee = exactCalleeHnd == null ? null : HandleToObject(exactCalleeHnd);
                if (callee != null && callee.RequireSecObject)
                {
                    return false;
                }
            }

            return true;
        }

        private FieldWithToken ComputeFieldWithToken(FieldDesc field, ref CORINFO_RESOLVED_TOKEN pResolvedToken)
        {
            ModuleToken token = HandleToModuleToken(ref pResolvedToken);
            return new FieldWithToken(field, token);
        }

        private MethodWithToken ComputeMethodWithToken(MethodDesc method, ref CORINFO_RESOLVED_TOKEN pResolvedToken, TypeDesc constrainedType, bool unboxing)
        {
            ModuleToken token = HandleToModuleToken(ref pResolvedToken, method, out object context, ref constrainedType);

            TypeDesc devirtualizedMethodOwner = null;
            if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_DevirtualizedMethod)
            {
                devirtualizedMethodOwner = HandleToObject(pResolvedToken.hClass);
            }

            return new MethodWithToken(method, token, constrainedType: constrainedType, unboxing: unboxing, context: context, devirtualizedMethodOwner: devirtualizedMethodOwner);
        }

        private ModuleToken HandleToModuleToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken, MethodDesc methodDesc, out object context, ref TypeDesc constrainedType)
        {
            if (methodDesc != null && (_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(methodDesc)
                || (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_DevirtualizedMethod)
                || methodDesc.IsPInvoke))
            {
                if ((CorTokenType)(unchecked((uint)pResolvedToken.token) & 0xFF000000u) == CorTokenType.mdtMethodDef &&
                    methodDesc?.GetTypicalMethodDefinition() is EcmaMethod ecmaMethod)
                {
                    mdToken token = (mdToken)MetadataTokens.GetToken(ecmaMethod.Handle);

                    // This is used for de-virtualization of non-generic virtual methods, and should be treated
                    // as a the methodDesc parameter defining the exact OwningType, not doing resolution through the token.
                    context = null;
                    constrainedType = null;

                    return new ModuleToken(ecmaMethod.Module, token);
                }
            }

            if (pResolvedToken.tokenType == CorInfoTokenKind.CORINFO_TOKENKIND_ResolvedStaticVirtualMethod)
            {
                context = null;
            }
            else
            {
                context = entityFromContext(pResolvedToken.tokenContext);
            }

            return HandleToModuleToken(ref pResolvedToken);
        }

        private ModuleToken HandleToModuleToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
        {
            mdToken token = pResolvedToken.token;
            var methodIL = HandleToObject(pResolvedToken.tokenScope);
            IEcmaModule module;

            // If the method body is synthetized by the compiler (the definition of the MethodIL is not
            // an EcmaMethodIL), the tokens in the MethodIL are not actual tokens: they're just
            // "per-MethodIL unique cookies". For ready to run, we need to be able to get to an actual
            // token to refer to the result of token lookup in the R2R fixups; we replace the token
            // token to refer to the result of token lookup in the R2R fixups.
            //
            // We replace the token with the token of the ECMA entity. This only works for **types/members
            // within the current version bubble**, but this happens to be good enough because
            // we only do this replacement within CoreLib to replace method bodies in places
            // that we cannot express in C# right now and for p/invokes in large version bubbles).
            MethodILScope methodILDef = methodIL.GetMethodILScopeDefinition();
            bool isFauxMethodIL = !(methodILDef is IEcmaMethodIL);
            if (isFauxMethodIL)
            {
                object resultDef = methodILDef.GetObject((int)pResolvedToken.token);

                if (resultDef is MethodDesc resultMethod)
                {
                    if (resultMethod is IL.Stubs.PInvokeTargetNativeMethod rawPinvoke)
                        resultMethod = rawPinvoke.Target;

                    // It's okay to strip the instantiation away because we don't need a MethodSpec
                    // token - SignatureBuilder will generate the generic method signature
                    // using instantiation parameters from the MethodDesc entity.
                    resultMethod = resultMethod.GetTypicalMethodDefinition();

                    Debug.Assert(resultMethod is EcmaMethod);
                    Debug.Assert(_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(((EcmaMethod)resultMethod).OwningType));
                    token = (mdToken)MetadataTokens.GetToken(((EcmaMethod)resultMethod).Handle);
                    module = ((EcmaMethod)resultMethod).Module;
                }
                else if (resultDef is FieldDesc resultField)
                {
                    // It's okay to strip the instantiation away because we don't need the
                    // instantiated MemberRef token - SignatureBuilder will generate the generic
                    // field signature using instantiation parameters from the FieldDesc entity.
                    resultField = resultField.GetTypicalFieldDefinition();

                    Debug.Assert(resultField is EcmaField);
                    Debug.Assert(_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(((EcmaField)resultField).OwningType) || ((MetadataType)resultField.OwningType).IsNonVersionable());
                    token = (mdToken)MetadataTokens.GetToken(((EcmaField)resultField).Handle);
                    module = ((EcmaField)resultField).Module;
                }
                else
                {
                    if (resultDef is EcmaType ecmaType)
                    {
                        Debug.Assert(_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(ecmaType));
                        token = (mdToken)MetadataTokens.GetToken(ecmaType.Handle);
                        module = ecmaType.EcmaModule;
                    }
                    else
                    {
                        // To replace !!0, we need to find the token for a !!0 TypeSpec within the image.
                        Debug.Assert(resultDef is SignatureMethodVariable);
                        Debug.Assert(((SignatureMethodVariable)resultDef).Index == 0);
                        module = (EcmaModule)((MetadataType)methodILDef.OwningMethod.OwningType).Module;
                        token = FindGenericMethodArgTypeSpec((EcmaModule)module);
                    }
                }
            }
            else
            {
                module = ((IEcmaMethodIL)methodILDef).Module;
            }

            return new ModuleToken(module, token);
        }

        private InfoAccessType constructStringLiteral(CORINFO_MODULE_STRUCT_* module, mdToken metaTok, ref void* ppValue)
        {
            MethodILScope methodIL = HandleToObject(module);

            // If this is not a MethodIL backed by a physical method body, we need to remap the token.
            Debug.Assert(methodIL.GetMethodILScopeDefinition() is IEcmaMethodIL);

            IEcmaModule metadataModule = ((IEcmaMethodIL)methodIL.GetMethodILScopeDefinition()).Module;
            ISymbolNode stringObject = _compilation.SymbolNodeFactory.StringLiteral(
                new ModuleToken(metadataModule, metaTok));
            ppValue = (void*)ObjectToHandle(stringObject);
            return InfoAccessType.IAT_PPVALUE;
        }

        enum EHInfoFields
        {
            Flags = 0,
            TryOffset = 1,
            TryEnd = 2,
            HandlerOffset = 3,
            HandlerEnd = 4,
            ClassTokenOrOffset = 5,

            Length
        }

        private ObjectNode.ObjectData EncodeEHInfo()
        {
            int totalClauses = _ehClauses.Length;
            byte[] ehInfoData = new byte[(int)EHInfoFields.Length * sizeof(uint) * totalClauses];

            for (int i = 0; i < totalClauses; i++)
            {
                ref CORINFO_EH_CLAUSE clause = ref _ehClauses[i];
                int clauseOffset = (int)EHInfoFields.Length * sizeof(uint) * i;
                Array.Copy(BitConverter.GetBytes((uint)clause.Flags), 0, ehInfoData, clauseOffset + (int)EHInfoFields.Flags * sizeof(uint), sizeof(uint));
                Array.Copy(BitConverter.GetBytes((uint)clause.TryOffset), 0, ehInfoData, clauseOffset + (int)EHInfoFields.TryOffset * sizeof(uint), sizeof(uint));
                // JIT in fact returns the end offset in the length field
                Array.Copy(BitConverter.GetBytes((uint)(clause.TryLength)), 0, ehInfoData, clauseOffset + (int)EHInfoFields.TryEnd * sizeof(uint), sizeof(uint));
                Array.Copy(BitConverter.GetBytes((uint)clause.HandlerOffset), 0, ehInfoData, clauseOffset + (int)EHInfoFields.HandlerOffset * sizeof(uint), sizeof(uint));
                Array.Copy(BitConverter.GetBytes((uint)(clause.HandlerLength)), 0, ehInfoData, clauseOffset + (int)EHInfoFields.HandlerEnd * sizeof(uint), sizeof(uint));
                Array.Copy(BitConverter.GetBytes((uint)clause.ClassTokenOrOffset), 0, ehInfoData, clauseOffset + (int)EHInfoFields.ClassTokenOrOffset * sizeof(uint), sizeof(uint));
            }
            return new ObjectNode.ObjectData(ehInfoData, Array.Empty<Relocation>(), alignment: 1, definedSymbols: Array.Empty<ISymbolDefinitionNode>());
        }

        /// <summary>
        /// Create a NativeVarInfo array; a table from native code range to variable location
        /// on the stack / in a specific register.
        /// </summary>
        private void setVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
        {
            Debug.Assert(_debugVarInfos == null);

            if (cVars == 0)
                return;

            _debugVarInfos = new NativeVarInfo[cVars];
            for (int i = 0; i < cVars; i++)
            {
                _debugVarInfos[i] = vars[i];
            }

            // JIT gave the ownership of this to us, so need to free this.
            freeArray(vars);
        }

        /// <summary>
        /// Create a DebugLocInfo array; a table from native offset to IL offset.
        /// </summary>
        private void setBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
        {
            Debug.Assert(_debugLocInfos == null);

            _debugLocInfos = new OffsetMapping[cMap];
            for (int i = 0; i < cMap; i++)
            {
                _debugLocInfos[i] = pMap[i];
            }

            // JIT gave the ownership of this to us, so need to free this.
            freeArray(pMap);
        }

        private void PublishEmptyCode()
        {
            _methodCodeNode.SetCode(new ObjectNode.ObjectData(Array.Empty<byte>(), null, 1, Array.Empty<ISymbolDefinitionNode>()));
            _methodCodeNode.InitializeFrameInfos(Array.Empty<FrameInfo>());
            _methodCodeNode.InitializeColdFrameInfos(Array.Empty<FrameInfo>());
        }

        private CorInfoHelpFunc getCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing)
        {
            return fThrowing ? CorInfoHelpFunc.CORINFO_HELP_CHKCASTANY : CorInfoHelpFunc.CORINFO_HELP_ISINSTANCEOFANY;
        }

        private CorInfoHelpFunc getNewHelper(CORINFO_CLASS_STRUCT_* classHandle, ref bool pHasSideEffects)
        {
            TypeDesc type = HandleToObject(classHandle);
            MetadataType metadataType = type as MetadataType;
            if (metadataType != null && metadataType.IsAbstract)
            {
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramDefault);
            }

            pHasSideEffects = type.HasFinalizer;

            // If the type isn't within the version bubble, it could gain a finalizer. Always treat it as if it has a finalizer
            if (!pHasSideEffects && !_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(type))
                pHasSideEffects = true;

            return CorInfoHelpFunc.CORINFO_HELP_NEWFAST;
        }

        private CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls)
        {
            return CorInfoHelpFunc.CORINFO_HELP_NEWARR_1_DIRECT;
        }

        private bool IsClassPreInited(TypeDesc type)
        {
            if (type.IsGenericDefinition)
            {
                return true;
            }

            var uninstantiatedType = type.GetTypeDefinition();

            if (_preInitedTypes.TryGetValue(uninstantiatedType, out bool preInited))
            {
                return preInited;
            }

            preInited = ComputeIsClassPreInited(type);
            _preInitedTypes.Add(uninstantiatedType, preInited);
            return preInited;

            static bool ComputeIsClassPreInited(TypeDesc type)
            {
                if (type.IsGenericDefinition)
                {
                    return true;
                }

                if (type.HasStaticConstructor)
                {
                    return false;
                }
                if (IsDynamicStaticsOrHasBoxedRegularStatics(type))
                {
                    return false;
                }
                return true;
            }

            static bool IsDynamicStaticsOrHasBoxedRegularStatics(TypeDesc type)
            {
                bool typeHasInstantiation = type.HasInstantiation;
                foreach (FieldDesc field in type.GetFields())
                {
                    if (!field.IsStatic || field.IsLiteral)
                        continue;

                    if (typeHasInstantiation)
                        return true; // Dynamic statics

                    if (!field.HasRva &&
                        !field.IsThreadStatic &&
                        field.FieldType.IsValueType &&
                        !field.FieldType.UnderlyingType.IsPrimitive)
                    {
                        return true; // HasBoxedRegularStatics
                    }
                }

                return false;
            }
        }

        private bool IsGenericTooDeeplyNested(Instantiation instantiation, int nestingLevel)
        {
            const int MaxInstatiationNesting = 10;

            if (nestingLevel == MaxInstatiationNesting)
            {
                return true;
            }

            foreach (TypeDesc instantiationType in instantiation)
            {
                if (instantiationType.HasInstantiation && IsGenericTooDeeplyNested(instantiationType.Instantiation, nestingLevel + 1))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsGenericTooDeeplyNested(Instantiation instantiation)
        {
            return IsGenericTooDeeplyNested(instantiation, 0);
        }

        private void getFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            MemoryHelper.FillMemory((byte*)pResult, 0xcc, Marshal.SizeOf<CORINFO_FIELD_INFO>());
#endif

            Debug.Assert(((int)flags & ((int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_GET |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_SET |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_ADDRESS |
                                        (int)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_INIT_ARRAY)) != 0);

            var field = HandleToObject(pResolvedToken.hField);
            MethodDesc callerMethod = HandleToObject(callerHandle);

            if (field.Offset.IsIndeterminate)
                throw new RequiresRuntimeJitException(field);

            CORINFO_FIELD_ACCESSOR fieldAccessor;
            CORINFO_FIELD_FLAGS fieldFlags = (CORINFO_FIELD_FLAGS)0;
            uint fieldOffset = (field.IsStatic && field.HasRva ? 0xBAADF00D : (uint)field.Offset.AsInt);

            if (field.IsStatic)
            {
                fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_STATIC;

                if (field.FieldType.IsValueType && field.HasGCStaticBase && !field.HasRva)
                {
                    // statics of struct types are stored as implicitly boxed in CoreCLR i.e.
                    // we need to modify field access flags appropriately
                    fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_STATIC_IN_HEAP;
                }

                if (field.HasRva)
                {
                    fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_UNMANAGED;

                    // TODO: Handle the case when the RVA is in the TLS range
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_RELOCATABLE;
                    fieldOffset = 0;
                    pResult->fieldLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.RvaFieldAddress(ComputeFieldWithToken(field, ref pResolvedToken)));

                    // We are not going through a helper. The constructor has to be triggered explicitly.
                    if (!IsClassPreInited(field.OwningType))
                    {
                        fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_INITCLASS;
                    }
                }
                else if (field.OwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    // The JIT wants to know how to access a static field on a generic type. We need a runtime lookup.
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_GENERICS_STATIC_HELPER;
                    if (field.IsThreadStatic)
                    {
                        pResult->helper = (field.HasGCStaticBase ?
                            CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE :
                            CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE);
                    }
                    else
                    {
                        pResult->helper = (field.HasGCStaticBase ?
                            CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_GCSTATIC_BASE :
                            CorInfoHelpFunc.CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE);
                    }

                    if (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && (fieldOffset <= FieldFixupSignature.MaxCheckableOffset))
                    {
                        // ENCODE_CHECK_FIELD_OFFSET
                        AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                    }
                }
                else
                {
                    fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER;
                    pResult->helper = CorInfoHelpFunc.CORINFO_HELP_UNDEF;

                    ReadyToRunHelperId helperId = ReadyToRunHelperId.Invalid;
                    CORINFO_FIELD_ACCESSOR intrinsicAccessor;
                    if (field.IsIntrinsic &&
                        (flags & CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_GET) != 0 &&
                        (intrinsicAccessor = getFieldIntrinsic(field)) != (CORINFO_FIELD_ACCESSOR)(-1))
                    {
                        fieldAccessor = intrinsicAccessor;
                    }
                    else if (field.IsThreadStatic)
                    {
                        if (field.HasGCStaticBase)
                        {
                            pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_THREADSTATIC_BASE;
                            helperId = ReadyToRunHelperId.GetThreadStaticBase;
                        }
                        else
                        {
                            pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE;
                            helperId = ReadyToRunHelperId.GetThreadNonGcStaticBase;
                        }
                    }
                    else
                    {
                        if (field.HasGCStaticBase)
                        {
                            pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_GCSTATIC_BASE;
                            helperId = ReadyToRunHelperId.GetGCStaticBase;
                        }
                        else
                        {
                            pResult->helper = CorInfoHelpFunc.CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE;
                            helperId = ReadyToRunHelperId.GetNonGCStaticBase;
                        }
                    }

                    if (!_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(field.OwningType) &&
                        fieldAccessor == CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_STATIC_SHARED_STATIC_HELPER)
                    {
                        PreventRecursiveFieldInlinesOutsideVersionBubble(field, callerMethod);

                        // Static fields outside of the version bubble need to be accessed using the ENCODE_FIELD_ADDRESS
                        // helper in accordance with ZapInfo::getFieldInfo in CoreCLR.
                        pResult->fieldLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.FieldAddress(ComputeFieldWithToken(field, ref pResolvedToken)));

                        fieldFlags &= ~CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_STATIC_IN_HEAP; // The dynamic helper takes care of the unboxing
                        fieldOffset = 0;
                    }
                    else
                    if (helperId != ReadyToRunHelperId.Invalid)
                    {
                        if (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && (fieldOffset <= FieldFixupSignature.MaxCheckableOffset))
                        {
                            // ENCODE_CHECK_FIELD_OFFSET
                            AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                        }

                        pResult->fieldLookup = CreateConstLookupToSymbol(
                            _compilation.SymbolNodeFactory.CreateReadyToRunHelper(helperId, field.OwningType)
                            );
                    }
                }
            }
            else
            {
                fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_INSTANCE;
            }

            if (field.IsInitOnly)
                fieldFlags |= CORINFO_FIELD_FLAGS.CORINFO_FLG_FIELD_FINAL;

            pResult->fieldAccessor = fieldAccessor;
            pResult->fieldFlags = fieldFlags;
            pResult->fieldType = getFieldType(pResolvedToken.hField, &pResult->structType, pResolvedToken.hClass);
            pResult->accessAllowed = CorInfoIsAccessAllowedResult.CORINFO_ACCESS_ALLOWED;
            pResult->offset = fieldOffset;

            EncodeFieldBaseOffset(field, ref pResolvedToken, pResult, callerMethod);

            // TODO: We need to implement access checks for fields and methods.  See JitInterface.cpp in mrtjit
            //       and STS::AccessCheck::CanAccess.
        }

        private static bool IsTypeSpecForTypicalInstantiation(TypeDesc t)
        {
            Instantiation inst = t.Instantiation;
            for (int i = 0; i < inst.Length; i++)
            {
                var arg = inst[i] as SignatureTypeVariable;
                if (arg == null || arg.Index != i)
                    return false;
            }
            return true;
        }

        private void ceeInfoGetCallInfo(
            ref CORINFO_RESOLVED_TOKEN pResolvedToken,
            CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
            CORINFO_METHOD_STRUCT_* callerHandle,
            CORINFO_CALLINFO_FLAGS flags,
            CORINFO_CALL_INFO* pResult,
            out MethodDesc methodToCall,
            out MethodDesc targetMethod,
            out TypeDesc constrainedType,
            out MethodDesc originalMethod,
            out TypeDesc exactType,
            out MethodDesc callerMethod,
            out EcmaModule callerModule,
            out bool useInstantiatingStub)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            MemoryHelper.FillMemory((byte*)pResult, 0xcc, Marshal.SizeOf<CORINFO_CALL_INFO>());
#endif
            pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup = false;

            originalMethod = HandleToObject(pResolvedToken.hMethod);
            TypeDesc type = HandleToObject(pResolvedToken.hClass);

            if (type.IsGenericDefinition)
            {
                ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramSpecific, HandleToObject(callerHandle));
            }

            // This formula roughly corresponds to CoreCLR CEEInfo::resolveToken when calling GetMethodDescFromMethodSpec
            // (that always winds up by calling FindOrCreateAssociatedMethodDesc) at
            // https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/jitinterface.cpp#L1054
            // Its basic meaning is that shared generic methods always need instantiating
            // stubs as the shared generic code needs the method dictionary parameter that cannot
            // be provided by other means.
            useInstantiatingStub = originalMethod.OwningType.IsArray || originalMethod.GetCanonMethodTarget(CanonicalFormKind.Specific).RequiresInstMethodDescArg();

            callerMethod = HandleToObject(callerHandle);

            if (originalMethod.HasInstantiation && IsGenericTooDeeplyNested(originalMethod.Instantiation))
            {
                throw new RequiresRuntimeJitException(callerMethod.ToString() + " -> " + originalMethod.ToString());
            }

            if (originalMethod.OwningType.HasInstantiation && IsGenericTooDeeplyNested(originalMethod.OwningType.Instantiation))
            {
                throw new RequiresRuntimeJitException(callerMethod.ToString() + " -> " + originalMethod.ToString());
            }

            if (!_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(callerMethod) &&
                !_compilation.NodeFactory.CompilationModuleGroup.CrossModuleInlineable(callerMethod))
            {
                // We must abort inline attempts calling from outside of the version bubble being compiled
                // because we have no way to remap the token relative to the external module to the current version bubble.
                //
                // Unless this is a call made while compiling a CrossModuleInlineable method
                throw new RequiresRuntimeJitException(callerMethod.ToString() + " -> " + originalMethod.ToString());
            }

            callerModule = ((EcmaMethod)callerMethod.GetTypicalMethodDefinition()).Module;
            bool isCallVirt = (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_CALLVIRT) != 0;
            bool isLdftn = (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) != 0;
            bool isStaticVirtual = (originalMethod.Signature.IsStatic && originalMethod.IsVirtual);

            // Spec says that a callvirt lookup ignores static methods. Since static methods
            // can't have the exact same signature as instance methods, a lookup that found
            // a static method would have never found an instance method.
            if (originalMethod.Signature.IsStatic && isCallVirt)
            {
                ThrowHelper.ThrowMissingMethodException("?");
            }

            exactType = type;

            constrainedType = (pConstrainedResolvedToken != null ? HandleToObject(pConstrainedResolvedToken->hClass) : null);

            bool resolvedConstraint = false;
            bool forceUseRuntimeLookup = false;

            MethodDesc methodAfterConstraintResolution = originalMethod;
            if (constrainedType == null)
            {
                pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM;
            }
            else
            {
                // We have a "constrained." call.  Try a partial resolve of the constraint call.  Note that this
                // will not necessarily resolve the call exactly, since we might be compiling
                // shared generic code - it may just resolve it to a candidate suitable for
                // JIT compilation, and require a runtime lookup for the actual code pointer
                // to call.

                if (constrainedType.IsEnum && originalMethod.Name == "GetHashCode")
                {
                    MethodDesc methodOnUnderlyingType = constrainedType.UnderlyingType.FindVirtualFunctionTargetMethodOnObjectType(originalMethod);
                    Debug.Assert(methodOnUnderlyingType != null);

                    constrainedType = constrainedType.UnderlyingType;
                    originalMethod = methodOnUnderlyingType;
                }

                MethodDesc directMethod;
                if (isStaticVirtual)
                {
                    directMethod = constrainedType.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(originalMethod);
                    if (directMethod != null && !_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(directMethod))
                    {
                        directMethod = null;
                    }
                }
                else
                {
                    directMethod = constrainedType.TryResolveConstraintMethodApprox(exactType, originalMethod, out forceUseRuntimeLookup);
                }
                if (directMethod != null)
                {
                    // Either
                    //    1. no constraint resolution at compile time (!directMethod)
                    // OR 2. no code sharing lookup in call
                    // OR 3. we have resolved to an instantiating stub

                    // This check for introducing an instantiation stub comes from the logic in
                    // MethodTable::TryResolveConstraintMethodApprox at
                    // https://github.com/dotnet/runtime/blob/17154bd7b8f21d6d8d6fca71b89d7dcb705ec32b/src/coreclr/vm/methodtable.cpp#L8628
                    // Its meaning is that, for direct method calls on value types, instantiating
                    // stubs are always needed in the presence of generic arguments as the generic
                    // dictionary cannot be passed through "this->method table".
                    useInstantiatingStub = directMethod.OwningType.IsValueType;

                    methodAfterConstraintResolution = directMethod;
                    Debug.Assert(!methodAfterConstraintResolution.OwningType.IsInterface);
                    resolvedConstraint = true;
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM;

                    exactType = constrainedType;
                    if (isStaticVirtual)
                    {
                        pResolvedToken.tokenType = CorInfoTokenKind.CORINFO_TOKENKIND_ResolvedStaticVirtualMethod;
                        constrainedType = null;
                    }
                }
                else if (isStaticVirtual)
                {
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM;
                }
                else if (constrainedType.IsValueType)
                {
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_BOX_THIS;
                }
                else
                {
                    pResult->thisTransform = CORINFO_THIS_TRANSFORM.CORINFO_DEREF_THIS;
                }
            }

            //
            // Initialize callee context used for inlining and instantiation arguments
            //

            targetMethod = methodAfterConstraintResolution;
            bool constrainedTypeNeedsRuntimeLookup = (constrainedType != null && constrainedType.IsCanonicalSubtype(CanonicalFormKind.Any));

            if (targetMethod.HasInstantiation)
            {
                pResult->contextHandle = contextFromMethod(targetMethod);
                pResult->exactContextNeedsRuntimeLookup = constrainedTypeNeedsRuntimeLookup || targetMethod.IsSharedByGenericInstantiations;
            }
            else
            {
                pResult->contextHandle = contextFromType(exactType);
                pResult->exactContextNeedsRuntimeLookup = constrainedTypeNeedsRuntimeLookup || exactType.IsCanonicalSubtype(CanonicalFormKind.Any);

                // Use main method as the context as long as the methods are called on the same type
                if (pResult->exactContextNeedsRuntimeLookup &&
                    pResolvedToken.tokenContext == contextFromMethodBeingCompiled() &&
                    constrainedType == null &&
                    exactType == MethodBeingCompiled.OwningType)
                {
                    var methodIL = HandleToObject(pResolvedToken.tokenScope);
                    var rawMethod = (MethodDesc)methodIL.GetMethodILScopeDefinition().GetObject((int)pResolvedToken.token);
                    if (IsTypeSpecForTypicalInstantiation(rawMethod.OwningType))
                    {
                        pResult->contextHandle = contextFromMethodBeingCompiled();
                    }
                }
            }

            //
            // Determine whether to perform direct call
            //

            bool directCall = false;
            bool resolvedCallVirt = false;
            bool callVirtCrossingVersionBubble = false;

            if (isStaticVirtual && !resolvedConstraint)
            {
                // Don't use direct calls for static virtual method calls unresolved at compile time
            }
            else if (isLdftn)
            {
                PrepareForUseAsAFunctionPointer(targetMethod);
                directCall = true;
            }
            else if (targetMethod.Signature.IsStatic)
            {
                // Static methods are always direct calls
                directCall = true;
            }
            else if (!isCallVirt || resolvedConstraint)
            {
                directCall = true;
            }
            else
            {
                bool devirt;

                // Check For interfaces before the bubble check
                // since interface methods shouldnt change from non-virtual to virtual between versions
                if (targetMethod.OwningType.IsInterface)
                {
                    // Handle interface methods specially because the Sealed bit has no meaning on interfaces.
                    devirt = !targetMethod.IsVirtual;
                }
                // if we are generating version resilient code
                // AND
                //    caller/callee are in different version bubbles
                // we have to apply more restrictive rules
                // These rules are related to the "inlining rules" as far as the
                // boundaries of a version bubble are concerned.
                // This check is different between CG1 and CG2. CG1 considers two types in the same version bubble
                // if their assemblies are in the same bubble, or if the NonVersionableTypeAttribute is present on the type.
                // CG2 checks a method cache that it builds with a bunch of new code.
                else if (!_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(callerMethod) ||
                    // check the Typical TargetMethod, not the Instantiation
                    !_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(targetMethod.GetTypicalMethodDefinition()))
                {
                    // For version resiliency we won't de-virtualize all final/sealed method calls.  Because during a
                    // servicing event it is legal to unseal a method or type.
                    //
                    // Note that it is safe to devirtualize in the following cases, since a servicing event cannot later modify it
                    //  1) Callvirt on a virtual final method of a value type - since value types are sealed types as per ECMA spec
                    //  2) Delegate.Invoke() - since a Delegate is a sealed class as per ECMA spec
                    //  3) JIT intrinsics - since they have pre-defined behavior
                    devirt = targetMethod.OwningType.IsValueType ||
                        (targetMethod.OwningType.IsDelegate && targetMethod.Name == "Invoke") ||
                        (targetMethod.OwningType.IsObject && targetMethod.Name == "GetType");

                    callVirtCrossingVersionBubble = true;
                }
                else
                {
                    devirt = !targetMethod.IsVirtual || targetMethod.IsFinal || targetMethod.OwningType.IsSealed();
                }

                if (devirt)
                {
                    resolvedCallVirt = true;
                    directCall = true;
                }
            }

            methodToCall = targetMethod;
            bool isArrayConstructor = targetMethod.OwningType.IsArray && targetMethod.IsConstructor;
            MethodDesc canonMethod = (isArrayConstructor ? null : targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific));

            if (directCall)
            {
                bool isVirtualBehaviorUnresolved = (isCallVirt && !resolvedCallVirt || isStaticVirtual && !resolvedConstraint);

                // Direct calls to abstract methods are not allowed
                if (targetMethod.IsAbstract &&
                    // Compensate for always treating delegates as direct calls above
                    !(isLdftn && isVirtualBehaviorUnresolved))
                {
                    ThrowHelper.ThrowInvalidProgramException(ExceptionStringID.InvalidProgramSpecific, targetMethod);
                }

                bool allowInstParam = (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_ALLOWINSTPARAM) != 0;

                // If the target method is resolved via constrained static virtual dispatch
                // And it requires an instParam, we do not have the generic dictionary infrastructure
                // to load the correct generic context arg via EmbedGenericHandle.
                // Instead, force the call to go down the CORINFO_CALL_CODE_POINTER code path
                // which should have somewhat inferior performance. This should only actually happen in the case
                // of shared generic code calling a shared generic implementation method, which should be rare.
                //
                // An alternative design would be to add a new generic dictionary entry kind to hold the MethodDesc
                // of the constrained target instead, and use that in some circumstances; however, implementation of 
                // that design requires refactoring variuos parts of the JIT interface as well as
                // TryResolveConstraintMethodApprox. In particular we would need to be abled to embed a constrained lookup
                // via EmbedGenericHandle, as well as decide in TryResolveConstraintMethodApprox if the call can be made
                // via a single use of CORINFO_CALL_CODE_POINTER, or would be better done with a CORINFO_CALL + embedded
                // constrained generic handle, or if there is a case where we would want to use both a CORINFO_CALL and
                // embedded constrained generic handle. Given the current expected high performance use case of this feature
                // which is generic numerics which will always resolve to exact valuetypes, it is not expected that
                // the complexity involved would be worth the risk. Other scenarios are not expected to be as performance
                // sensitive.
                if (isStaticVirtual && pResult->exactContextNeedsRuntimeLookup)
                {
                    allowInstParam = false;
                }

                if (!allowInstParam && canonMethod != null && canonMethod.RequiresInstArg())
                {
                    useInstantiatingStub = true;
                }

                // We don't allow a JIT to call the code directly if a runtime lookup is
                // needed. This is the case if
                //     1. the scan of the call token indicated that it involves code sharing
                // AND 2. the method is an instantiating stub
                //
                // In these cases the correct instantiating stub is only found via a runtime lookup.
                //
                // Note that most JITs don't call instantiating stubs directly if they can help it -
                // they call the underlying shared code and provide the type context parameter
                // explicitly. However
                //    (a) some JITs may call instantiating stubs (it makes the JIT simpler) and
                //    (b) if the method is a remote stub then the EE will force the
                //        call through an instantiating stub and
                //    (c) constraint calls that require runtime context lookup are never resolved
                //        to underlying shared generic code

                const CORINFO_CALLINFO_FLAGS LdVirtFtnMask = CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN | CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_CALLVIRT;
                bool unresolvedLdVirtFtn = ((flags & LdVirtFtnMask) == LdVirtFtnMask) && !resolvedCallVirt;

                if (isArrayConstructor)
                {
                    // Constructors on arrays are special and don't actually have entrypoints.
                    // That would be fine by itself and wouldn't need special casing. But
                    // constructors on SzArray have a weird property that causes them not to have canonical forms.
                    // int[][] has a .ctor(int32,int32) to construct the jagged array in one go, but its canonical
                    // form of __Canon[] doesn't have the two-parameter constructor. The canonical form would need
                    // to have an unlimited number of constructors to cover stuff like "int[][][][][][]..."
                    pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL;
                }
                else if ((pResult->exactContextNeedsRuntimeLookup && useInstantiatingStub && (!allowInstParam || resolvedConstraint)) || forceUseRuntimeLookup)
                {
                    if (unresolvedLdVirtFtn)
                    {
                        // Compensate for always treating delegates as direct calls above.
                        // Dictionary lookup is computed in embedGenericHandle as part of the LDVIRTFTN code sequence
                        pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN;
                    }
                    else
                    {
                        pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER;

                        // For reference types, the constrained type does not affect instance virtual method resolution
                        DictionaryEntryKind entryKind = (constrainedType != null && (constrainedType.IsValueType || !isCallVirt)
                            ? DictionaryEntryKind.ConstrainedMethodEntrySlot
                            : DictionaryEntryKind.MethodEntrySlot);

                        if (isStaticVirtual && exactType.HasInstantiation)
                        {
                            useInstantiatingStub = true;
                        }

                        ComputeRuntimeLookupForSharedGenericToken(entryKind, ref pResolvedToken, pConstrainedResolvedToken, originalMethod, ref pResult->codePointerOrStubLookup);
                    }
                }
                else
                {
                    if (allowInstParam)
                    {
                        useInstantiatingStub = false;
                        methodToCall = canonMethod ?? methodToCall;
                    }

                    pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL;

                    // Compensate for always treating delegates as direct calls above
                    if (unresolvedLdVirtFtn)
                    {
                        pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN;
                    }
                }
                pResult->nullInstanceCheck = resolvedCallVirt;
            }
            else if (isStaticVirtual)
            {
                pResult->kind = CORINFO_CALL_KIND.CORINFO_CALL;
                pResult->nullInstanceCheck = false;

                // Always use an instantiating stub for unresolved constrained SVM calls as we cannot
                // always tell at compile time that a given SVM resolves to a method on a generic base
                // class and not requesting the instantiating stub makes the runtime transform the
                // owning type to its canonical equivalent that would need different codegen
                // (supplying the instantiation argument).
                if (!resolvedConstraint)
                {
                    if (pResult->exactContextNeedsRuntimeLookup)
                    {
                        throw new RequiresRuntimeJitException("EmbedGenericHandle currently doesn't support propagation of RUNTIME_LOOKUP or pConstrainedResolvedToken from ComputeRuntimeLookupForSharedGenericToken");
                        // ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind.DispatchStubAddrSlot, ref pResolvedToken, pConstrainedResolvedToken, originalMethod, ref pResult->codePointerOrStubLookup);
                        // useInstantiatingStub = false;
                    }
                    else
                    {
                        throw new RequiresRuntimeJitException("CanInline currently doesn't support propagation of constrained type so that we cannot reliably tell whether a SVM call can be inlined");
                        // Even if we decided to support SVMs unresolved at compile time, we'd still need to force the use of instantiating stub
                        // as we can't tell in advance whether the method will be runtime-resolved to a canonical representation.
                        // useInstantiatingStub = true;
                    }
                }
            }
            // All virtual calls which take method instantiations must
            // currently be implemented by an indirect call via a runtime-lookup
            // function pointer
            else if (targetMethod.HasInstantiation)
            {
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN;  // stub dispatch can't handle generic method calls yet
                pResult->nullInstanceCheck = true;
            }
            // Non-interface dispatches go through the vtable.
            else if (!targetMethod.OwningType.IsInterface)
            {
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB;
                pResult->nullInstanceCheck = true;

                // We'll special virtual calls to target methods in the corelib assembly when compiling in R2R mode, and generate fragile-NI-like callsites for improved performance. We
                // can do that because today we'll always service the corelib assembly and the runtime in one bundle. Any caller in the corelib version bubble can benefit from this
                // performance optimization.
                /* TODO-PERF, GitHub issue# 7168: uncommenting the conditional statement below enables
                ** VTABLE-based calls for Corelib (and maybe a larger framework version bubble in the
                ** future). Making it work requires construction of the method table in managed code
                ** matching the CoreCLR algorithm (MethodTableBuilder).
                if (MethodInSystemVersionBubble(callerMethod) && MethodInSystemVersionBubble(targetMethod))
                {
                    pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_VTABLE;
                }
                */
            }
            else
            {
                // At this point, we knew it is a virtual call to targetMethod,
                // If it is also a default interface method call, it should go through instantiating stub.
                useInstantiatingStub = useInstantiatingStub || (targetMethod.OwningType.IsInterface && !originalMethod.IsAbstract);
                // Insert explicit null checks for cross-version bubble non-interface calls.
                // It is required to handle null checks properly for non-virtual <-> virtual change between versions
                pResult->nullInstanceCheck = callVirtCrossingVersionBubble && !targetMethod.OwningType.IsInterface;
                pResult->kind = CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB;

                // We can't make stub calls when we need exact information
                // for interface calls from shared code.

                if (pResult->exactContextNeedsRuntimeLookup)
                {
                    ComputeRuntimeLookupForSharedGenericToken(DictionaryEntryKind.DispatchStubAddrSlot, ref pResolvedToken, null, originalMethod, ref pResult->codePointerOrStubLookup);
                }
                else
                {
                    // We use an indirect call
                    pResult->codePointerOrStubLookup.constLookup.accessType = InfoAccessType.IAT_PVALUE;
                    pResult->codePointerOrStubLookup.constLookup.addr = null;
                }
            }

            pResult->hMethod = ObjectToHandle(methodToCall);

            // TODO: access checks
            pResult->accessAllowed = CorInfoIsAccessAllowedResult.CORINFO_ACCESS_ALLOWED;

            // We're pretty much done at this point.  Let's grab the rest of the information that the jit is going to
            // need.
            pResult->classFlags = getClassAttribsInternal(type);

            pResult->methodFlags = getMethodAttribsInternal(methodToCall);

            pResult->wrapperDelegateInvoke = false;

            Get_CORINFO_SIG_INFO(methodToCall, &pResult->sig, scope: null, useInstantiatingStub);
        }

        private uint getMethodAttribs(CORINFO_METHOD_STRUCT_* ftn)
        {
            MethodDesc method = HandleToObject(ftn);
            return getMethodAttribsInternal(method);
            // OK, if the EE said we're not doing a stub dispatch then just return the kind to
        }

        private void PrepareForUseAsAFunctionPointer(MethodDesc method)
        {
            foreach (TypeDesc type in method.Signature)
            {
                if (type.IsValueType)
                {
                    classMustBeLoadedBeforeCodeIsRun(type);
                }
            }
        }

        private void classMustBeLoadedBeforeCodeIsRun(CORINFO_CLASS_STRUCT_* cls)
        {
            TypeDesc type = HandleToObject(cls);
            classMustBeLoadedBeforeCodeIsRun(type);
        }

        private void classMustBeLoadedBeforeCodeIsRun(TypeDesc type)
        {
            if (!type.IsPrimitive)
            {
                ISymbolNode node = _compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.TypeHandle, type);
                AddPrecodeFixup(node);
            }
        }

        private static bool MethodSignatureIsUnstable(MethodSignature methodSig, out string unstableMessage)
        {
            foreach (TypeDesc t in methodSig)
            {
                DefType defType = t as DefType;

                if (defType != null)
                {
                    if (!defType.LayoutAbiStable)
                    {
                        unstableMessage = $"Abi unstable type {defType}";
                        return true;
                    }
                }
            }
            unstableMessage = null;
            return false;
        }

        private void UpdateConstLookupWithRequiresRuntimeJitSymbolIfNeeded(ref CORINFO_CONST_LOOKUP constLookup, MethodDesc method)
        {
            if (MethodSignatureIsUnstable(method.Signature, out string unstableMessage))
            {
                constLookup.addr = (void*)ObjectToHandle(new RequiresRuntimeJitIfUsedSymbol(unstableMessage + " calling " + method));
                constLookup.accessType = InfoAccessType.IAT_PVALUE;
            }
        }

        private void VerifyMethodSignatureIsStable(MethodSignature methodSig)
        {
            if (MethodSignatureIsUnstable(methodSig, out var unstableMessage))
            {
                throw new RequiresRuntimeJitException(unstableMessage);
            }
        }

        private void getCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult)
        {
            MethodDesc methodToCall;
            MethodDesc targetMethod;
            TypeDesc constrainedType;
            MethodDesc originalMethod;
            TypeDesc exactType;
            MethodDesc callerMethod;
            EcmaModule callerModule;
            bool useInstantiatingStub;
            ceeInfoGetCallInfo(
                ref pResolvedToken,
                pConstrainedResolvedToken,
                callerHandle,
                flags,
                pResult,
                out methodToCall,
                out targetMethod,
                out constrainedType,
                out originalMethod,
                out exactType,
                out callerMethod,
                out callerModule,
                out useInstantiatingStub);

            if (callerMethod.HasInstantiation || callerMethod.OwningType.HasInstantiation)
            {
                _compilation.NodeFactory.DetectGenericCycles(callerMethod, methodToCall);
            }

            if (pResult->thisTransform == CORINFO_THIS_TRANSFORM.CORINFO_BOX_THIS)
            {
                // READYTORUN: FUTURE: Optionally create boxing stub at runtime
                // We couldn't resolve the constrained call into a valuetype instance method and we're asking the JIT
                // to box and do a virtual dispatch. If we were to allow the boxing to happen now, it could break future code
                // when the user adds a method to the valuetype that makes it possible to avoid boxing (if there is state
                // mutation in the method).

                // We allow this at least for primitives and enums because we control them
                // and we know there's no state mutation.
                //
                // We allow this for constrained calls to non-virtual methods, as its extremely unlikely they will become virtual (verified by fixup)
                //
                // TODO in the future we should consider allowing this for regular virtuals as well with a fixup that verifies the method wasn't implemented
                if (getTypeForPrimitiveValueClass(pConstrainedResolvedToken->hClass) != CorInfoType.CORINFO_TYPE_UNDEF)
                {
                    // allowed
                }
                else if (!originalMethod.IsVirtual && originalMethod.Context.GetWellKnownType(WellKnownType.Object) == originalMethod.OwningType)
                {
                    // alllowed, non-virtual method's on Object will never become virtual, and will also always trigger a BOX_THIS pattern
                }
                else
                {
                    throw new RequiresRuntimeJitException(pResult->thisTransform.ToString());
                }
            }

            // We validate the safety of the signature here, as it could have been adjusted
            // by virtual resolution during getCallInfo (virtual resolution could find a result using type equivalence)
            ValidateSafetyOfUsingTypeEquivalenceInSignature(targetMethod.GetTypicalMethodDefinition().Signature);

            // OK, if the EE said we're not doing a stub dispatch then just return the kind to
            // the caller.  No other kinds of virtual calls have extra information attached.
            switch (pResult->kind)
            {
                case CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB:
                    {
                        if (pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup)
                        {
                            return;
                        }

                        pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                            _compilation.SymbolNodeFactory.InterfaceDispatchCell(
                                ComputeMethodWithToken(targetMethod, ref pResolvedToken, constrainedType: null, unboxing: false),
                                MethodBeingCompiled));

                        // If the abi of the method isn't stable, this will cause a usage of the RequiresRuntimeJitSymbol, which will trigger a RequiresRuntimeJitException
                        UpdateConstLookupWithRequiresRuntimeJitSymbolIfNeeded(ref pResult->codePointerOrStubLookup.constLookup, targetMethod);
                        }
                    break;


                case CORINFO_CALL_KIND.CORINFO_CALL_CODE_POINTER:
                    Debug.Assert(pResult->codePointerOrStubLookup.lookupKind.needsRuntimeLookup);
                    // Eagerly check abi stability here as no symbol usage can be used to delay the check
                    VerifyMethodSignatureIsStable(targetMethod.Signature);

                    // There is no easy way to detect method referenced via generic lookups in generated code.
                    // Report this method reference unconditionally.
                    // TODO: m_pImage->m_pPreloader->MethodReferencedByCompiledCode(pResult->hMethod);
                    return;

                case CORINFO_CALL_KIND.CORINFO_CALL:
                    {
                        // Constrained token is not interesting with this transforms
                        if (pResult->thisTransform != CORINFO_THIS_TRANSFORM.CORINFO_NO_THIS_TRANSFORM)
                            constrainedType = null;

                        MethodDesc nonUnboxingMethod = methodToCall;
                        bool isUnboxingStub = methodToCall.IsUnboxingThunk();
                        if (isUnboxingStub)
                        {
                            nonUnboxingMethod = methodToCall.GetUnboxedMethod();
                        }
                        if (nonUnboxingMethod is IL.Stubs.PInvokeTargetNativeMethod rawPinvoke)
                        {
                            nonUnboxingMethod = rawPinvoke.Target;
                        }

                        if (methodToCall.OwningType.IsArray && methodToCall.IsConstructor)
                        {
                            pResult->codePointerOrStubLookup.constLookup = default;
                        }
                        else
                        {
                            // READYTORUN: FUTURE: Direct calls if possible
                            pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                                _compilation.NodeFactory.MethodEntrypoint(
                                    ComputeMethodWithToken(nonUnboxingMethod, ref pResolvedToken, constrainedType, unboxing: isUnboxingStub),
                                    isInstantiatingStub: useInstantiatingStub,
                                    isPrecodeImportRequired: (flags & CORINFO_CALLINFO_FLAGS.CORINFO_CALLINFO_LDFTN) != 0,
                                    isJumpableImportRequired: false));
                        }

                        // If the abi of the method isn't stable, this will cause a usage of the RequiresRuntimeJitSymbol, which will trigger a RequiresRuntimeJitException
                        UpdateConstLookupWithRequiresRuntimeJitSymbolIfNeeded(ref pResult->codePointerOrStubLookup.constLookup, targetMethod);
                    }
                    break;

                case CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_VTABLE:
                    // Only calls within the CoreLib version bubble support fragile NI codegen with vtable based calls, for better performance (because
                    // CoreLib and the runtime will always be updated together anyways - this is a special case)

                    // Eagerly check abi stability here as no symbol usage can be used to delay the check
                    VerifyMethodSignatureIsStable(targetMethod.Signature);
                    break;

                case CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_LDVIRTFTN:
                    if (!pResult->exactContextNeedsRuntimeLookup)
                    {
                        pResult->codePointerOrStubLookup.constLookup = CreateConstLookupToSymbol(
                            _compilation.NodeFactory.DynamicHelperCell(
                                ComputeMethodWithToken(targetMethod, ref pResolvedToken, constrainedType: null, unboxing: false),
                                useInstantiatingStub));

                        Debug.Assert(!pResult->sig.hasTypeArg());
                    }
                    break;

                default:
                    throw new NotImplementedException(pResult->kind.ToString());
            }

            if (pResult->sig.hasTypeArg())
            {
                if (pResult->exactContextNeedsRuntimeLookup)
                {
                    // Nothing to do... The generic handle lookup gets embedded in to the codegen
                    // during the jitting of the call.
                    // (Note: The generic lookup in R2R is performed by a call to a helper at runtime, not by
                    // codegen emitted at crossgen time)
                }
                else
                {
                    MethodDesc canonMethod = targetMethod.GetCanonMethodTarget(CanonicalFormKind.Specific);
                    if (canonMethod.RequiresInstMethodDescArg())
                    {
                        pResult->instParamLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(
                            ReadyToRunHelperId.MethodDictionary,
                            ComputeMethodWithToken(targetMethod, ref pResolvedToken, constrainedType: constrainedType, unboxing: false)));
                    }
                    else
                    {
                        pResult->instParamLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.CreateReadyToRunHelper(
                            ReadyToRunHelperId.TypeDictionary,
                            exactType));
                    }
                }
            }
        }

        private void ComputeRuntimeLookupForSharedGenericToken(
            DictionaryEntryKind entryKind,
            ref CORINFO_RESOLVED_TOKEN pResolvedToken,
            CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
            MethodDesc templateMethod,
            ref CORINFO_LOOKUP pResultLookup)
        {
            pResultLookup.lookupKind.needsRuntimeLookup = true;
            pResultLookup.lookupKind.runtimeLookupFlags = 0;

            ref CORINFO_RUNTIME_LOOKUP pResult = ref pResultLookup.runtimeLookup;
            pResult.signature = null;

            pResult.indirectFirstOffset = false;
            pResult.indirectSecondOffset = false;

            // Unless we decide otherwise, just do the lookup via a helper function
            pResult.indirections = CORINFO.USEHELPER;
            pResult.sizeOffset = CORINFO.CORINFO_NO_SIZE_CHECK;

            // Runtime lookups in inlined contexts are not supported by the runtime for now
            if (pResolvedToken.tokenContext != contextFromMethodBeingCompiled())
            {
                pResultLookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_NOT_SUPPORTED;
                return;
            }

            MethodDesc contextMethod = methodFromContext(pResolvedToken.tokenContext);
            TypeDesc contextType = typeFromContext(pResolvedToken.tokenContext);

            // There is a pathological case where invalid IL refereces __Canon type directly, but there is no dictionary availabled to store the lookup.
            if (!contextMethod.IsSharedByGenericInstantiations)
            {
                ThrowHelper.ThrowInvalidProgramException();
            }

            if (contextMethod.RequiresInstMethodDescArg())
            {
                pResultLookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_METHODPARAM;
            }
            else
            {
                if (contextMethod.RequiresInstMethodTableArg())
                    pResultLookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_CLASSPARAM;
                else
                    pResultLookup.lookupKind.runtimeLookupKind = CORINFO_RUNTIME_LOOKUP_KIND.CORINFO_LOOKUP_THISOBJ;
            }

            pResultLookup.lookupKind.runtimeLookupArgs = null;

            switch (entryKind)
            {
                case DictionaryEntryKind.DeclaringTypeHandleSlot:
                    Debug.Assert(templateMethod != null);
                    pResultLookup.lookupKind.runtimeLookupArgs = ObjectToHandle(templateMethod.OwningType);
                    pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.DeclaringTypeHandle;
                    break;

                case DictionaryEntryKind.TypeHandleSlot:
                    pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.TypeHandle;
                    break;

                case DictionaryEntryKind.MethodDescSlot:
                case DictionaryEntryKind.MethodEntrySlot:
                case DictionaryEntryKind.ConstrainedMethodEntrySlot:
                case DictionaryEntryKind.DispatchStubAddrSlot:
                    {
                        if (entryKind == DictionaryEntryKind.MethodDescSlot)
                            pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.MethodHandle;
                        else if (entryKind == DictionaryEntryKind.MethodEntrySlot || entryKind == DictionaryEntryKind.ConstrainedMethodEntrySlot)
                            pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.MethodEntry;
                        else
                            pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.VirtualDispatchCell;

                        pResultLookup.lookupKind.runtimeLookupArgs = pConstrainedResolvedToken;
                        break;
                    }

                case DictionaryEntryKind.FieldDescSlot:
                    pResultLookup.lookupKind.runtimeLookupFlags = (ushort)ReadyToRunHelperId.FieldHandle;
                    break;

                default:
                    throw new NotImplementedException(entryKind.ToString());
            }

            // For R2R compilations, we don't generate the dictionary lookup signatures (dictionary lookups are done in a
            // different way that is more version resilient... plus we can't have pointers to existing MTs/MDs in the sigs)
        }

        private void ceeInfoEmbedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, ref CORINFO_GENERICHANDLE_RESULT pResult)
        {
#if DEBUG
            // In debug, write some bogus data to the struct to ensure we have filled everything
            // properly.
            fixed (CORINFO_GENERICHANDLE_RESULT* tmp = &pResult)
                MemoryHelper.FillMemory((byte*)tmp, 0xcc, Marshal.SizeOf<CORINFO_GENERICHANDLE_RESULT>());
#endif

            bool runtimeLookup = false;
            MethodDesc templateMethod = null;

            if (!fEmbedParent && pResolvedToken.hMethod != null)
            {
                MethodDesc md = HandleToObject(pResolvedToken.hMethod);
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_METHOD;

                Debug.Assert(md.OwningType == td);

                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)ObjectToHandle(md);
                templateMethod = md;

                // Runtime lookup is only required for stubs. Regular entrypoints are always the same shared MethodDescs.
                runtimeLookup = md.IsSharedByGenericInstantiations;
            }
            else if (!fEmbedParent && pResolvedToken.hField != null)
            {
                FieldDesc fd = HandleToObject(pResolvedToken.hField);
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_FIELD;
                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)pResolvedToken.hField;

                runtimeLookup = fd.IsStatic && td.IsCanonicalSubtype(CanonicalFormKind.Specific);
            }
            else
            {
                TypeDesc td = HandleToObject(pResolvedToken.hClass);

                pResult.handleType = CorInfoGenericHandleType.CORINFO_HANDLETYPE_CLASS;
                pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)pResolvedToken.hClass;

                if (fEmbedParent && pResolvedToken.hMethod != null)
                {
                    MethodDesc declaringMethod = HandleToObject(pResolvedToken.hMethod);
                    if (declaringMethod.OwningType.GetClosestDefType() != td.GetClosestDefType())
                    {
                        //
                        // The method type may point to a sub-class of the actual class that declares the method.
                        // It is important to embed the declaring type in this case.
                        //

                        templateMethod = declaringMethod;
                        pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)ObjectToHandle(declaringMethod.OwningType);
                    }
                }

                // IsSharedByGenericInstantiations would not work here. The runtime lookup is required
                // even for standalone generic variables that show up as __Canon here.
                runtimeLookup = td.IsCanonicalSubtype(CanonicalFormKind.Specific);
            }

            Debug.Assert(pResult.compileTimeHandle != null);

            if (runtimeLookup)
            {
                DictionaryEntryKind entryKind = DictionaryEntryKind.EmptySlot;
                switch (pResult.handleType)
                {
                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_CLASS:
                        entryKind = (templateMethod != null ? DictionaryEntryKind.DeclaringTypeHandleSlot : DictionaryEntryKind.TypeHandleSlot);
                        break;
                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_METHOD:
                        entryKind = DictionaryEntryKind.MethodDescSlot;
                        break;
                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_FIELD:
                        entryKind = DictionaryEntryKind.FieldDescSlot;
                        break;
                    default:
                        throw new NotImplementedException(pResult.handleType.ToString());
                }

                ComputeRuntimeLookupForSharedGenericToken(entryKind, ref pResolvedToken, pConstrainedResolvedToken: null, templateMethod, ref pResult.lookup);
            }
            else
            {
                // If the target is not shared then we've already got our result and
                // can simply do a static look up
                pResult.lookup.lookupKind.needsRuntimeLookup = false;

                pResult.lookup.constLookup.handle = pResult.compileTimeHandle;
                pResult.lookup.constLookup.accessType = InfoAccessType.IAT_VALUE;
            }
        }

        private CORINFO_CLASS_STRUCT_* embedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection)
        {
            TypeDesc type = HandleToObject(handle);
            if (!_compilation.CompilationModuleGroup.VersionsWithType(type))
                throw new RequiresRuntimeJitException(type.ToString());

            Import typeHandleImport = (Import)_compilation.SymbolNodeFactory.CreateReadyToRunHelper(ReadyToRunHelperId.TypeHandle, type);
            Debug.Assert(typeHandleImport.RepresentsIndirectionCell);
            ppIndirection = (void*)ObjectToHandle(typeHandleImport);
            return null;
        }

        private void embedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, ref CORINFO_GENERICHANDLE_RESULT pResult)
        {
            ceeInfoEmbedGenericHandle(ref pResolvedToken, fEmbedParent, ref pResult);

            Debug.Assert(pResult.compileTimeHandle != null);

            if (pResult.lookup.lookupKind.needsRuntimeLookup)
            {
                if (pResult.handleType == CorInfoGenericHandleType.CORINFO_HANDLETYPE_METHOD)
                {
                    // There is no easy way to detect method referenced via generic lookups in generated code.
                    // Report this method reference unconditionally.
                    // TODO: m_pImage->m_pPreloader->MethodReferencedByCompiledCode((CORINFO_METHOD_HANDLE)pResult->compileTimeHandle);
                }
            }
            else
            {
                ISymbolNode symbolNode;

                switch (pResult.handleType)
                {
                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_CLASS:
                        symbolNode = _compilation.SymbolNodeFactory.CreateReadyToRunHelper(
                            ReadyToRunHelperId.TypeHandle,
                            HandleToObject(pResolvedToken.hClass));
                        break;

                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_METHOD:
                        {
                            MethodDesc md = HandleToObject(pResolvedToken.hMethod);
                            TypeDesc td = HandleToObject(pResolvedToken.hClass);

                            bool unboxingStub = false;
                            //
                            // This logic should be kept in sync with MethodTableBuilder::NeedsTightlyBoundUnboxingStub
                            // Essentially all ValueType virtual methods will require an Unboxing Stub
                            //
                            if ((td.IsValueType) && !md.Signature.IsStatic
                                && md.IsVirtual)
                            {
                                unboxingStub = true;
                            }

                            symbolNode = _compilation.SymbolNodeFactory.CreateReadyToRunHelper(
                                ReadyToRunHelperId.MethodHandle,
                                ComputeMethodWithToken(md, ref pResolvedToken, constrainedType: null, unboxing: unboxingStub));
                        }
                        break;

                    case CorInfoGenericHandleType.CORINFO_HANDLETYPE_FIELD:
                        symbolNode = _compilation.SymbolNodeFactory.CreateReadyToRunHelper(
                            ReadyToRunHelperId.FieldHandle,
                            ComputeFieldWithToken(HandleToObject(pResolvedToken.hField), ref pResolvedToken));
                        break;

                    default:
                        throw new NotImplementedException(pResult.handleType.ToString());
                }

                pResult.lookup.constLookup = CreateConstLookupToSymbol(symbolNode);
            }
        }

        private MethodDesc getUnboxingThunk(MethodDesc method)
        {
            return _unboxingThunkFactory.GetUnboxingMethod(method);
        }

        private CORINFO_METHOD_STRUCT_* embedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection)
        {
            // TODO: READYTORUN FUTURE: Handle this case correctly
            MethodDesc methodDesc = HandleToObject(handle);
            throw new RequiresRuntimeJitException("embedMethodHandle: " + methodDesc.ToString());
        }

        private bool NeedsTypeLayoutCheck(TypeDesc type)
        {
            if (!type.IsDefType)
                return false;

            if (!type.IsValueType)
                return false;

            return !_compilation.IsLayoutFixedInCurrentVersionBubble(type) || (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && !((MetadataType)type).IsNonVersionable());
        }

        /// <summary>
        /// Throws if the JIT inlines a method outside the current version bubble and that inlinee accesses
        /// fields also outside the version bubble. ReadyToRun currently cannot encode such references.
        /// </summary>
        private void PreventRecursiveFieldInlinesOutsideVersionBubble(FieldDesc field, MethodDesc callerMethod)
        {
            if (!_compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(callerMethod) && !_compilation.NodeFactory.CompilationModuleGroup.CrossModuleInlineable(callerMethod))
            {
                // Prevent recursive inline attempts where an inlined method outside of the version bubble is
                // referencing fields outside the version bubble.
                throw new RequiresRuntimeJitException(callerMethod.ToString() + " -> " + field.ToString());
            }
        }

        private void EncodeFieldBaseOffset(FieldDesc field, ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_FIELD_INFO* pResult, MethodDesc callerMethod)
        {
            TypeDesc pMT = field.OwningType;

            if (pResult->fieldAccessor != CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_INSTANCE)
            {
                // No-op except for instance fields
            }
            else if (!_compilation.IsLayoutFixedInCurrentVersionBubble(pMT))
            {
                if (pMT.IsValueType)
                {
                    // ENCODE_CHECK_FIELD_OFFSET
                    if (pResult->offset > FieldFixupSignature.MaxCheckableOffset)
                        throw new RequiresRuntimeJitException(callerMethod.ToString() + " -> " + field.ToString());

                    AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                    // No-op other than generating the check field offset fixup
                }
                else
                {
                    PreventRecursiveFieldInlinesOutsideVersionBubble(field, callerMethod);

                    // ENCODE_FIELD_OFFSET
                    pResult->offset = 0;
                    pResult->fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_INSTANCE_WITH_BASE;
                    pResult->fieldLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.FieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                }
            }
            else if (pMT.IsValueType)
            {
                if (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && !callerMethod.IsNonVersionable() && (pResult->offset <= FieldFixupSignature.MaxCheckableOffset))
                {
                    // ENCODE_CHECK_FIELD_OFFSET
                    if (_compilation.CompilationModuleGroup.VersionsWithType(field.OwningType)) // Only verify versions with types
                        AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                }
                // ENCODE_NONE
            }
            else if (_compilation.IsInheritanceChainLayoutFixedInCurrentVersionBubble(pMT.BaseType))
            {
                if (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && !callerMethod.IsNonVersionable() && (pResult->offset <= FieldFixupSignature.MaxCheckableOffset))
                {
                    // ENCODE_CHECK_FIELD_OFFSET
                    if (_compilation.CompilationModuleGroup.VersionsWithType(field.OwningType)) // Only verify versions with types
                        AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                }
                // ENCODE_NONE
            }
            else if (TypeCannotUseBasePlusOffsetEncoding(pMT.BaseType as MetadataType))
            {
                // ENCODE_CHECK_FIELD_OFFSET
                AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                // No-op other than generating the check field offset fixup
            }
            else
            {
                PreventRecursiveFieldInlinesOutsideVersionBubble(field, callerMethod);

                if (_compilation.SymbolNodeFactory.VerifyTypeAndFieldLayout && !callerMethod.IsNonVersionable() && (pResult->offset <= FieldFixupSignature.MaxCheckableOffset))
                {
                    // ENCODE_CHECK_FIELD_OFFSET
                    if (_compilation.CompilationModuleGroup.VersionsWithType(field.OwningType)) // Only verify versions with types
                        AddPrecodeFixup(_compilation.SymbolNodeFactory.CheckFieldOffset(ComputeFieldWithToken(field, ref pResolvedToken)));
                }

                // ENCODE_FIELD_BASE_OFFSET
                int fieldBaseOffset = ((MetadataType)pMT).FieldBaseOffset().AsInt;
                Debug.Assert(pResult->offset >= (uint)fieldBaseOffset);
                pResult->offset -= (uint)fieldBaseOffset;
                pResult->fieldAccessor = CORINFO_FIELD_ACCESSOR.CORINFO_FIELD_INSTANCE_WITH_BASE;
                pResult->fieldLookup = CreateConstLookupToSymbol(_compilation.SymbolNodeFactory.FieldBaseOffset(field.OwningType));
            }
        }

        private bool TypeCannotUseBasePlusOffsetEncoding(MetadataType type)
        {
            if (type == null)
                return true;

            // Types which are encoded with sequential or explicit layout do not support an aligned base offset, and so we just encode with the exact offset
            // of the field, and if that offset is incorrect, the method cannot be used.
            if (type.IsSequentialLayout || type.IsExplicitLayout)
                return true;
            else if (type.BaseType is MetadataType metadataType)
            {
                return TypeCannotUseBasePlusOffsetEncoding(metadataType);
            }
            return false;
        }

        private void getGSCookie(IntPtr* pCookieVal, IntPtr** ppCookieVal)
        {
            *pCookieVal = IntPtr.Zero;
            *ppCookieVal = (IntPtr *)ObjectToHandle(_compilation.NodeFactory.GetReadyToRunHelperCell(ReadyToRunHelper.GSCookie));
        }

        private int* getAddrOfCaptureThreadGlobal(ref void* ppIndirection)
        {
            ppIndirection = (void*)ObjectToHandle(_compilation.NodeFactory.GetReadyToRunHelperCell(ReadyToRunHelper.IndirectTrapThreads));
            return null;
        }

        private void getMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
        { throw new NotImplementedException("getMethodVTableOffset"); }
        private void expandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_GENERICHANDLE_RESULT pResult)
        { throw new NotImplementedException("expandRawHandleIntrinsic"); }

        private void* getMethodSync(CORINFO_METHOD_STRUCT_* ftn, ref void* ppIndirection)
        {
            // Used with CORINFO_HELP_MON_ENTER_STATIC/CORINFO_HELP_MON_EXIT_STATIC - we don't have this fixup in R2R.
            throw new RequiresRuntimeJitException($"{MethodBeingCompiled} -> {nameof(getMethodSync)}");
        }

        private byte[] _bbCounts;

        partial void findKnownBBCountBlock(ref BlockType blockType, void* location, ref int offset)
        {
            if (_bbCounts != null)
            {
                fixed (byte* pBBCountData = _bbCounts)
                {
                    if (pBBCountData <= (byte*)location && (byte*)location < (pBBCountData + _bbCounts.Length))
                    {
                        offset = (int)((byte*)location - pBBCountData);
                        blockType = BlockType.BBCounts;
                        return;
                    }
                }
            }
            blockType = BlockType.Unknown;
        }

        private unsafe HRESULT allocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData)
        {
            CORJIT_FLAGS flags = default(CORJIT_FLAGS);
            getJitFlags(ref flags, 0);
            *pInstrumentationData = null;

            if (flags.IsSet(CorJitFlag.CORJIT_FLAG_IL_STUB))
            {
                return HRESULT.E_NOTIMPL;
            }

            // Methods without ecma metadata are not instrumented
            EcmaMethod ecmaMethod = _methodCodeNode.Method.GetTypicalMethodDefinition() as EcmaMethod;
            if (ecmaMethod == null)
            {
                return HRESULT.E_NOTIMPL;
            }

            // Only allocation of PGO data for the current method is supported.
            if (_methodCodeNode.Method != HandleToObject(ftnHnd))
            {
                return HRESULT.E_NOTIMPL;
            }

            if (!_compilation.IsModuleInstrumented(ecmaMethod.Module))
            {
                return HRESULT.E_NOTIMPL;
            }

            // Validate that each schema item is only used for a basic block count
            for (uint iSchema = 0; iSchema < countSchemaItems; iSchema++)
            {
                if (pSchema[iSchema].InstrumentationKind != PgoInstrumentationKind.BasicBlockIntCount)
                    return HRESULT.E_NOTIMPL;
                if (pSchema[iSchema].Count != 1)
                    return HRESULT.E_NOTIMPL;
            }

            BlockCounts* blockCounts = (BlockCounts*)GetPin(_bbCounts = new byte[countSchemaItems * sizeof(BlockCounts)]);
            *pInstrumentationData = (byte*)blockCounts;

            for (uint iSchema = 0; iSchema < countSchemaItems; iSchema++)
            {
                // Update schema have correct offsets
                pSchema[iSchema].Offset = new IntPtr((byte*)&blockCounts[iSchema].ExecutionCount - (byte*)blockCounts);
                // Insert IL Offsets into block data to match schema
                blockCounts[iSchema].ILOffset = (uint)pSchema[iSchema].ILOffset;
            }

            return 0;
        }

        private void getAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
        {
            MethodDesc methodDesc = HandleToObject(method);
            if (methodDesc is IL.Stubs.PInvokeTargetNativeMethod rawPInvoke)
                methodDesc = rawPInvoke.Target;
            Debug.Assert(_compilation.CompilationModuleGroup.VersionsWithMethodBody(methodDesc));
            EcmaMethod ecmaMethod = (EcmaMethod)methodDesc;
            ModuleToken moduleToken = new ModuleToken(ecmaMethod.Module, ecmaMethod.Handle);
            MethodWithToken methodWithToken = new MethodWithToken(ecmaMethod, moduleToken, constrainedType: null, unboxing: false, context: null);

            if ((ecmaMethod.GetPInvokeMethodCallingConventions() & UnmanagedCallingConventions.IsSuppressGcTransition) != 0)
            {
                pLookup.addr = (void*)ObjectToHandle(_compilation.SymbolNodeFactory.GetPInvokeTargetNode(methodWithToken));
                pLookup.accessType = InfoAccessType.IAT_PVALUE;
            }
            else
            {
                pLookup.addr = (void*)ObjectToHandle(_compilation.SymbolNodeFactory.GetIndirectPInvokeTargetNode(methodWithToken));
                pLookup.accessType = InfoAccessType.IAT_PPVALUE;
            }
        }

        private bool pInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* handle, CORINFO_SIG_INFO* callSiteSig)
        {
            if (handle != null)
            {
                var method = HandleToObject(handle);
                if (method.IsRawPInvoke())
                {
                    return false;
                }

                // If this method is in another versioning unit, then the compilation cannot inline the pinvoke (as we aren't currently
                // able to construct a token correctly to refer to the pinvoke method.
                if (!_compilation.CompilationModuleGroup.VersionsWithMethodBody(method))
                {
                    return true;
                }

                MethodIL stubIL = null;
                try
                {
                    stubIL = _compilation.GetMethodIL(method);
                    if (stubIL == null)
                    {
                        // This is the case of a PInvoke method that requires marshallers, which we can't use in this compilation
                        Debug.Assert(!_compilation.NodeFactory.CompilationModuleGroup.GeneratesPInvoke(method));
                        return true;
                    }

                    // Marshalling behavior isn't modeled as protected by R2R rules, so disable pinvoke inlining for code outside
                    // of the version bubble
                    if (!_compilation.CompilationModuleGroup.VersionsWithMethodBody(method))
                        return true;
                }
                catch (RequiresRuntimeJitException)
                {
                    // The PInvoke IL emitter will throw for known unsupported scenario. We cannot propagate the exception here since
                    // this interface call might be used to check if a certain pinvoke can be inlined in the caller. Throwing means that the
                    // caller will not get compiled. Instead, we'll return true to let the JIT know that it cannot inline the pinvoke, and
                    // the actual pinvoke call will be handled by a stub that we create and compile in the runtime.
                    return true;
                }

                return ((PInvokeILStubMethodIL)stubIL).IsMarshallingRequired;
            }
            else
            {
                var sig = HandleToObject(callSiteSig->methodSignature);
                return Marshaller.IsMarshallingRequired(sig, Array.Empty<ParameterMetadata>(), ((MetadataType)HandleToObject(callSiteSig->scope).OwningMethod.OwningType).Module);
            }
        }

        private bool convertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool mustConvert)
        {
            throw new NotImplementedException();
        }

        private bool canGetCookieForPInvokeCalliSig(CORINFO_SIG_INFO* szMetaSig)
        {
            // If we answer "true" here, RyuJIT is going to ask for the cookie and for the CORINFO_HELP_PINVOKE_CALLI
            // helper. The helper doesn't exist in ReadyToRun, so let's just throw right here.
            throw new RequiresRuntimeJitException($"{MethodBeingCompiled} -> {nameof(canGetCookieForPInvokeCalliSig)}");
        }

        private int SizeOfPInvokeTransitionFrame => ReadyToRunRuntimeConstants.READYTORUN_PInvokeTransitionFrameSizeInPointerUnits * PointerSize;
        private int SizeOfReversePInvokeTransitionFrame
        {
            get
            {
                if (_compilation.NodeFactory.Target.Architecture == TargetArchitecture.X86)
                    return ReadyToRunRuntimeConstants.READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits_X86 * PointerSize;
                else
                    return ReadyToRunRuntimeConstants.READYTORUN_ReversePInvokeTransitionFrameSizeInPointerUnits_Universal * PointerSize;
            }
        }

        private void setEHcount(uint cEH)
        {
            _ehClauses = new CORINFO_EH_CLAUSE[cEH];
        }

        private void setEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
        {
            // Filters don't have class token in the clause.ClassTokenOrOffset
            if ((clause.Flags & CORINFO_EH_CLAUSE_FLAGS.CORINFO_EH_CLAUSE_FILTER) == 0)
            {
                if (clause.ClassTokenOrOffset != 0)
                {
                    MethodIL methodIL = _compilation.GetMethodIL(MethodBeingCompiled);
                    mdToken classToken = (mdToken)clause.ClassTokenOrOffset;
                    TypeDesc clauseType = (TypeDesc)ResolveTokenInScope(methodIL, MethodBeingCompiled, classToken);

                    CORJIT_FLAGS flags = default(CORJIT_FLAGS);
                    getJitFlags(ref flags, 0);

                    if (flags.IsSet(CorJitFlag.CORJIT_FLAG_IL_STUB))
                    {
                        // IL stub tokens are 'private' and do not resolve correctly in their parent module's metadata.

                        // Currently, the only place we are using a token here is for a COM-to-CLR exception-to-HRESULT
                        // mapping catch clause.  We want this catch clause to catch all exceptions, so we override the
                        // token to be mdTypeRefNil, which used by the EH system to mean catch(...)
                        Debug.Assert(clauseType.IsObject);
                        clause.ClassTokenOrOffset = 0;
                    }
                    else
                    {
                        // For all clause types add fixup to ensure the types are loaded before the code of the method
                        // containing the catch blocks is executed. This ensures that a failure to load the types would
                        // not happen when the exception handling is in progress and it is looking for a catch handler.
                        // At that point, we could only fail fast.
                        classMustBeLoadedBeforeCodeIsRun(clauseType);
                    }
                }
            }

            _ehClauses[EHnumber] = clause;
        }

        private readonly Stack<List<ISymbolNode>> _stashedPrecodeFixups = new Stack<List<ISymbolNode>>();
        private readonly Stack<HashSet<MethodDesc>> _stashedInlinedMethods = new Stack<HashSet<MethodDesc>>();

        private void beginInlining(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd)
        {
            _stashedPrecodeFixups.Push(_precodeFixups);
            _precodeFixups = null;
            _stashedInlinedMethods.Push(_inlinedMethods);
            _inlinedMethods = null;
        }

        private void reportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
        {
            if (inlineResult == CorInfoInline.INLINE_PASS)
            {
                // We deliberately ignore inlinerHnd because we have no interest to track intermediate links now.
                MethodDesc inlinee = HandleToObject(inlineeHnd);

                // If during inlining we found Precode fixups, then only if the inline was successful, add them to the set of
                // fixups that will be used for the entire method
                List<ISymbolNode> previouslyStashedFixups = _stashedPrecodeFixups.Pop();

                if (_precodeFixups != null)
                {
                    previouslyStashedFixups = previouslyStashedFixups ?? new List<ISymbolNode>();
                    previouslyStashedFixups.AddRange(_precodeFixups);
                }
                _precodeFixups = previouslyStashedFixups;

                // If during inlining we found new inlinees, then if the inline was successful, add them to the set of fixups
                // for the entire method.
                HashSet<MethodDesc> previouslyStashedInlinees = _stashedInlinedMethods.Pop();
                if (_inlinedMethods != null)
                {
                    previouslyStashedInlinees = previouslyStashedInlinees ?? new HashSet<MethodDesc>();
                    foreach (var inlineeInHashSet in _inlinedMethods)
                        previouslyStashedInlinees.Add(inlineeInHashSet);
                }
                _inlinedMethods = previouslyStashedInlinees;

                // Then add the fact we inlined this method.
                _inlinedMethods = _inlinedMethods ?? new HashSet<MethodDesc>();
                _inlinedMethods.Add(inlinee);

                var typicalMethod = inlinee.GetTypicalMethodDefinition();
                if (!_compilation.CompilationModuleGroup.VersionsWithMethodBody(typicalMethod) &&
                    typicalMethod is EcmaMethod ecmaMethod)
                {
                    Debug.Assert(_compilation.CompilationModuleGroup.CrossModuleInlineable(typicalMethod) ||
                                 _compilation.CompilationModuleGroup.IsNonVersionableWithILTokensThatDoNotNeedTranslation(typicalMethod));
                    bool needsTokenTranslation = !_compilation.CompilationModuleGroup.IsNonVersionableWithILTokensThatDoNotNeedTranslation(typicalMethod);

                    if (needsTokenTranslation)
                    {
                        // This can happen if the method is marked as NonVersionable if there are tokens of interest
                        // or if we are working with a full cross module inline
                        ISymbolNode ilBodyNode = _compilation.SymbolNodeFactory.CheckILBodyFixupSignature(ecmaMethod);
                        AddPrecodeFixup(ilBodyNode);
                    }

                    // For cross module inlines, we will compile in a potentially 2 step process
                    // 1. Compile the method using the set of il bodies available at the start of a multi-threaded compilation run
                    // 2. If at any time, the set of methods that are inlined includes a method which has an IL body without
                    //    tokens that are useable in compilation, record that information, and once the multi-threaded portion
                    //    of the build finishes, it will then compute the IL bodies for those methods, then run the compilation again.
                    MethodIL methodIL = _compilation.GetMethodIL(typicalMethod);
                    if (needsTokenTranslation && !(methodIL is IMethodTokensAreUseableInCompilation) && methodIL is EcmaMethodIL)
                    {
                        // We may have already acquired the right type of MethodIL here, or be working with a method that is an IL Intrinsic
                        _ilBodiesNeeded = _ilBodiesNeeded ?? new List<EcmaMethod>();
                        _ilBodiesNeeded.Add(ecmaMethod);
                    }
                }
            }
            else
            {
                // If we didn't succeed on the inline, just pop back to the state before inlining
                _precodeFixups = _stashedPrecodeFixups.Pop();
                _inlinedMethods = _stashedInlinedMethods.Pop();
            }
        }

        private void updateEntryPointForTailCall(ref CORINFO_CONST_LOOKUP entryPoint)
        {
            // In x64 we normally use a return address to find the indirection cell for delay load.
            // For tailcalls we instead expect the JIT to leave the indirection in rax.
            if (_compilation.TypeSystemContext.Target.Architecture != TargetArchitecture.X64)
                return;

            object node = HandleToObject(entryPoint.addr);
            if (node is not DelayLoadMethodImport imp)
                return;

            Debug.Assert(imp.GetType() == typeof(DelayLoadMethodImport));
            IMethodNode newEntryPoint =
                _compilation.NodeFactory.MethodEntrypoint(
                    imp.MethodWithToken,
                    ((MethodFixupSignature)imp.ImportSignature.Target).IsInstantiatingStub,
                    isPrecodeImportRequired: false,
                    isJumpableImportRequired: true);

            entryPoint = CreateConstLookupToSymbol(newEntryPoint);
        }

        private int getExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet)
        {
            // Not implemented for R2R yet
            return 0;
        }

        private bool getStaticFieldContent(CORINFO_FIELD_STRUCT_* fieldHandle, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
        {
            Debug.Assert(fieldHandle != null);
            FieldDesc field = HandleToObject(fieldHandle);

            // For crossgen2 we only support RVA fields
            if (_compilation.NodeFactory.CompilationModuleGroup.VersionsWithType(field.OwningType) && field.HasRva)
            {
                return TryReadRvaFieldData(field, buffer, bufferSize, valueOffset);
            }
            return false;
        }

        private bool getObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset)
        {
            throw new NotSupportedException();
        }

        private CORINFO_CLASS_STRUCT_* getObjectType(CORINFO_OBJECT_STRUCT_* objPtr)
        {
            throw new NotSupportedException();
        }

        private bool isObjectImmutable(CORINFO_OBJECT_STRUCT_* objPtr)
        {
            throw new NotSupportedException();
        }

        private bool getStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* value)
        {
            return false;
        }

        private CORINFO_OBJECT_STRUCT_* getRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls)
        {
            return null;
        }

        private int getArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHnd)
        {
            return -1;
        }

        private bool getIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset)
        {
            // Implemented for JIT and NativeAOT only for now.
            return false;
        }

        private bool getStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr)
        {
            // Implemented for JIT and NativeAOT only for now.
            return false;
        }

        private void ValidateSafetyOfUsingTypeEquivalenceInSignature(MethodSignature signature)
        {
            // Type equivalent valuetypes not in the current version bubble are problematic, and cannot be referred to in our current token
            // scheme except through type references. So we need to detect them, and if they aren't referred to by type reference from a module
            // in the current build, then we need to fallback to runtime jit.
            ValidateSafetyOfUsingTypeEquivalenceOfType(signature.ReturnType);
            foreach (var type in signature)
            {
                ValidateSafetyOfUsingTypeEquivalenceOfType(type);
            }
        }

        void ValidateSafetyOfUsingTypeEquivalenceOfType(TypeDesc type)
        {
            if (type.IsValueType && type.IsTypeDefEquivalent && !_compilation.CompilationModuleGroup.VersionsWithTypeReference(type))
            {
                // Technically this is a bit pickier than needed, as cross module inlineable cases will be hit by this, but type equivalence is a
                // rarely used feature, and we can fix that if we need to.
                throw new RequiresRuntimeJitException($"Type equivalent valuetype '{type}' not directly referenced from member reference");
            }
        }

        private bool notifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn)
        {
            MethodDesc method = HandleToObject(ftn);
            Debug.Assert(method != null);
            // If ftn isn't within the current version bubble we can't rely on methodInfo being
            // stable e.g. mark calls as no-return if their IL has no rets.
            return _compilation.NodeFactory.CompilationModuleGroup.VersionsWithMethodBody(method);
        }

#pragma warning disable CA1822 // Mark members as static
        private void getThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
        {
            // Implemented for NativeAOT only for now.
        }
#pragma warning restore CA1822 // Mark members as static
    }
}
