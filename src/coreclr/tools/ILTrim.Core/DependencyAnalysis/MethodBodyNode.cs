// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;

using ReflectionMethodBodyScanner = ILCompiler.Dataflow.ReflectionMethodBodyScanner;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents method body bytes emitted into the executable.
    /// </summary>
    public class MethodBodyNode : DependencyNodeCore<NodeFactory>, INodeWithDeferredDependencies
    {
        private readonly EcmaModule _module;
        private readonly MethodDefinitionHandle _methodHandle;
        private bool _preserveUnmappedTokens;
        DependencyList _dependencies = null;

        public MethodBodyNode(EcmaModule module, MethodDefinitionHandle methodHandle)
        {
            _module = module;
            _methodHandle = methodHandle;
        }

        public void PreserveUnmappedTokens() => _preserveUnmappedTokens = true;

        public override bool StaticDependenciesAreComputed => _dependencies != null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => _dependencies;

        void INodeWithDeferredDependencies.ComputeDependencies(NodeFactory factory)
        {
            _dependencies = new DependencyList();
            DependencyList unresolvedMethodDependencies = new DependencyList();

            // RVA = 0 is an extern method, such as a DllImport
            int rva = _module.MetadataReader.GetMethodDefinition(_methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                return;

            MethodBodyBlock bodyBlock = _module.PEReader.GetMethodBody(rva);

            if (!bodyBlock.LocalSignature.IsNil)
                _dependencies.Add(factory.StandaloneSignature(_module, bodyBlock.LocalSignature), "Signatures of local variables");

            var exceptionRegions = bodyBlock.ExceptionRegions;
            if (!bodyBlock.ExceptionRegions.IsEmpty)
            {
                foreach (var exceptionRegion in exceptionRegions)
                {
                    if (exceptionRegion.Kind != ExceptionRegionKind.Catch)
                        continue;

                    _dependencies.Add(factory.GetNodeForTypeToken(_module, exceptionRegion.CatchType), "Catch type of exception region");
                }
            }

            bool requiresMethodBodyScanner = ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForMethodBody(
                factory.FlowAnnotations, _module.GetMethod(_methodHandle));

            ILReader ilReader = new(bodyBlock.GetILBytes());
            MethodDesc unresolvedGetTypeMethod = null;
            bool hasUnresolvedFieldValue = false;
            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();
                bool unresolvedFieldValueOnStack = hasUnresolvedFieldValue;
                hasUnresolvedFieldValue = false;
                MethodDesc getTypeMethodFromPreviousInstruction = unresolvedGetTypeMethod;
                unresolvedGetTypeMethod = null;

                switch (opcode)
                {
                    case ILOpcode.sizeof_:
                    case ILOpcode.newarr:
                    case ILOpcode.stsfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.jmp:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.stobj:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.constrained:
                        EntityHandle token = MetadataTokens.EntityHandle(ilReader.ReadILToken());

                        MethodDesc method;
                        MethodDefinitionHandle? unresolvedGenericMethod = null;
                        if (opcode == ILOpcode.newobj || opcode == ILOpcode.call || opcode == ILOpcode.callvirt ||
                            opcode == ILOpcode.ldvirtftn || opcode == ILOpcode.ldftn)
                        {
                            method = _module.TryGetMethod(token);
                            if (method is null && TryGetGenericMethodDefinition(token) is MethodDefinitionHandle genericMethodHandle)
                            {
                                unresolvedGenericMethod = genericMethodHandle;
                                if (opcode != ILOpcode.newobj)
                                {
                                    method = _module.GetMethod(genericMethodHandle);
                                }
                            }
                        }
                        else
                        {
                            method = null;
                        }

                        FieldDesc field;
                        if (opcode == ILOpcode.stfld || opcode == ILOpcode.stsfld ||
                            opcode == ILOpcode.ldfld || opcode == ILOpcode.ldflda ||
                            opcode == ILOpcode.ldsfld || opcode == ILOpcode.ldsflda)
                        {
                            field = _module.TryGetField(token);
                        }
                        else
                        {
                            field = null;
                        }

                        if ((opcode == ILOpcode.callvirt || opcode == ILOpcode.ldvirtftn) &&
                            method != null && method.IsVirtual)
                        {
                            MethodDesc slotMethod = MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(
                                method.GetTypicalMethodDefinition());
                            _dependencies.Add(factory.VirtualMethodUse((EcmaMethod)slotMethod), "Callvirt/ldvirtftn");
                        }

                        _dependencies.Add(token.Kind switch
                        {
                            HandleKind.TypeDefinition => factory.TypeDefinition(_module, (TypeDefinitionHandle)token),
                            HandleKind.TypeReference => factory.TypeReference(_module, (TypeReferenceHandle)token),
                            HandleKind.TypeSpecification => factory.TypeSpecification(_module, (TypeSpecificationHandle)token),
                            HandleKind.MethodDefinition => factory.MethodDefinition(_module, (MethodDefinitionHandle)token),
                            HandleKind.FieldDefinition => factory.FieldDefinition(_module, (FieldDefinitionHandle)token),
                            HandleKind.MemberReference => factory.MemberReference(_module, (MemberReferenceHandle)token),
                            HandleKind.MethodSpecification => factory.MethodSpecification(_module, (MethodSpecificationHandle)token),
                            HandleKind.StandaloneSignature => factory.StandaloneSignature(_module, (StandaloneSignatureHandle)token),
                            _ => throw new InvalidOperationException(token.Kind.ToString()),
                        }, "Instruction operand");

                        if (opcode == ILOpcode.ldtoken &&
                            _module.TryGetType(token) is TypeDesc type &&
                            type.GetTypeDefinition() is EcmaType typeDefinition &&
                            !typeDefinition.IsValueType &&
                            LayoutTypeNode.IsLayoutType(typeDefinition))
                        {
                            _dependencies.Add(factory.LayoutType(typeDefinition), "Reflected type with sequential or explicit layout");
                        }

                        if (unresolvedGenericMethod is MethodDefinitionHandle methodHandle)
                        {
                            AddUnresolvedGenericArgumentWarnings(factory, methodHandle);
                            MethodBodyNode unresolvedMethodBody = factory.MethodBody(_module, methodHandle);
                            unresolvedMethodBody.PreserveUnmappedTokens();
                            unresolvedMethodDependencies.Add(
                                unresolvedMethodBody,
                                "Fallback target method body of generic member reference");
                            unresolvedMethodDependencies.Add(
                                factory.MethodDefinition(_module, methodHandle).PreserveUnresolvedBody(),
                                "Fallback target method def of generic member reference");
                        }

                        if (method != null)
                        {
                            if (unresolvedFieldValueOnStack &&
                                method.GetName() == "GetType" &&
                                method.Signature.Length == 0 &&
                                method.Signature.ReturnType.IsTypeOf("System.Type"))
                            {
                                unresolvedGetTypeMethod = method;
                            }
                            else if (getTypeMethodFromPreviousInstruction is not null &&
                                method.Signature.IsStatic &&
                                method.Signature.Length > 0)
                            {
                                MethodParameterValue targetValue = factory.FlowAnnotations.GetMethodParameterValue(
                                    new ParameterProxy(method, (ParameterIndex)0));
                                if (targetValue.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                                {
                                    AddUnresolvedGetTypeWarning(factory, getTypeMethodFromPreviousInstruction, targetValue);
                                }
                            }
                            if (!requiresMethodBodyScanner)
                            {
                                requiresMethodBodyScanner |= ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForCallSite(
                                    factory.FlowAnnotations, method.GetTypicalMethodDefinition());
                            }
                        }

                        if (field != null && !requiresMethodBodyScanner)
                        {
                            try
                            {
                                _ = field.FieldType;
                                requiresMethodBodyScanner |= ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForAccess(
                                    factory.FlowAnnotations, field.GetTypicalFieldDefinition());
                            }
                            catch (TypeSystemException.FileNotFoundException)
                            {
                                // An unresolved field type cannot be inspected for dataflow, so retain the body
                                // and conservatively treat its value as unknown.
                                requiresMethodBodyScanner = true;
                                hasUnresolvedFieldValue = true;
                            }
                        }
                        else if (field is null &&
                            (opcode == ILOpcode.ldfld || opcode == ILOpcode.ldflda ||
                             opcode == ILOpcode.ldsfld || opcode == ILOpcode.ldsflda))
                        {
                            requiresMethodBodyScanner = true;
                            hasUnresolvedFieldValue = true;
                        }

                        break;

                    default:
                        ilReader.Skip(opcode);
                        break;
                }
            }

            // TODO: add DataflowAnalyzedMethod instead. We should make sure to handle the state machine/nested function case correctly
            if (requiresMethodBodyScanner)
            {
                var ecmaMethod = (EcmaMethod)_module.GetMethod(_methodHandle);
                if (!CompilerGeneratedState.IsNestedFunctionOrStateMachineMember(ecmaMethod))
                {
                    try
                    {
                        var list = ReflectionMethodBodyScanner.ScanAndProcessReturnValue(factory, factory.FlowAnnotations, factory.Logger,
                            EcmaMethodIL.Create(ecmaMethod), out _);
                        _dependencies.AddRange(list);
                    }
                    catch (TypeSystemException.FileNotFoundException)
                    {
                        // The method's local signature may reference a missing type. The IL prepass above
                        // still records the metadata dependencies that can be resolved.
                    }
                }
            }
            _dependencies.AddRange(unresolvedMethodDependencies);
        }

        internal static void AddMetadataDependencies(
            EcmaModule module,
            MethodDefinitionHandle methodHandle,
            NodeFactory factory,
            DependencyList dependencies)
        {
            int rva = module.MetadataReader.GetMethodDefinition(methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                return;

            MethodBodyBlock bodyBlock = module.PEReader.GetMethodBody(rva);
            if (!bodyBlock.LocalSignature.IsNil)
                dependencies.Add(factory.StandaloneSignature(module, bodyBlock.LocalSignature), "Signatures of local variables");

            foreach (ExceptionRegion exceptionRegion in bodyBlock.ExceptionRegions)
            {
                if (exceptionRegion.Kind == ExceptionRegionKind.Catch)
                    dependencies.Add(factory.GetNodeForTypeToken(module, exceptionRegion.CatchType), "Catch type of exception region");
            }

            ILReader ilReader = new(bodyBlock.GetILBytes());
            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();
                switch (opcode)
                {
                    case ILOpcode.sizeof_:
                    case ILOpcode.newarr:
                    case ILOpcode.stsfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.jmp:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.stobj:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.constrained:
                        EntityHandle token = MetadataTokens.EntityHandle(ilReader.ReadILToken());
                        switch (token.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                dependencies.Add(factory.TypeDefinition(module, (TypeDefinitionHandle)token), "Instruction operand");
                                break;
                            case HandleKind.TypeReference:
                                dependencies.Add(factory.TypeReference(module, (TypeReferenceHandle)token), "Instruction operand");
                                break;
                            case HandleKind.TypeSpecification:
                                dependencies.Add(factory.TypeSpecification(module, (TypeSpecificationHandle)token), "Instruction operand");
                                break;
                            case HandleKind.MethodDefinition:
                                dependencies.Add(factory.MethodDefinition(module, (MethodDefinitionHandle)token), "Instruction operand");
                                break;
                            case HandleKind.FieldDefinition:
                                dependencies.Add(factory.FieldDefinition(module, (FieldDefinitionHandle)token), "Instruction operand");
                                break;
                            case HandleKind.MemberReference:
                                dependencies.Add(
                                    factory.MemberReference(module, (MemberReferenceHandle)token).PreserveUnresolved(),
                                    "Instruction operand");
                                break;
                            case HandleKind.MethodSpecification:
                                dependencies.Add(factory.MethodSpecification(module, (MethodSpecificationHandle)token), "Instruction operand");
                                break;
                            case HandleKind.StandaloneSignature:
                                dependencies.Add(factory.StandaloneSignature(module, (StandaloneSignatureHandle)token), "Instruction operand");
                                break;
                        }
                        break;

                    default:
                        ilReader.Skip(opcode);
                        break;
                }
            }

        }

        private MethodDefinitionHandle? TryGetGenericMethodDefinition(EntityHandle token)
        {
            MetadataReader reader = _module.MetadataReader;
            if (token.Kind == HandleKind.MethodSpecification)
            {
                MethodSpecification methodSpecification = reader.GetMethodSpecification((MethodSpecificationHandle)token);
                if (methodSpecification.Method.Kind == HandleKind.MethodDefinition)
                    return (MethodDefinitionHandle)methodSpecification.Method;

                if (methodSpecification.Method.Kind == HandleKind.MemberReference)
                    token = methodSpecification.Method;
                else
                    return null;
            }

            if (token.Kind != HandleKind.MemberReference)
                return null;

            MemberReferenceHandle memberReferenceHandle = (MemberReferenceHandle)token;
            MemberReference memberReference = reader.GetMemberReference(memberReferenceHandle);
            if (memberReference.Parent.Kind != HandleKind.TypeSpecification || memberReference.GetKind() != MemberReferenceKind.Method)
                return null;

            TypeSpecification typeSpec = reader.GetTypeSpecification((TypeSpecificationHandle)memberReference.Parent);
            BlobReader signature = reader.GetBlobReader(typeSpec.Signature);
            if (signature.ReadSignatureTypeCode() != SignatureTypeCode.GenericTypeInstance)
                return null;

            signature.ReadCompressedInteger();
            EntityHandle typeDefinition = signature.ReadTypeHandle();
            if (typeDefinition.Kind != HandleKind.TypeDefinition)
                return null;

            BlobReader memberReferenceSignature = reader.GetBlobReader(memberReference.Signature);
            SignatureHeader signatureHeader = memberReferenceSignature.ReadSignatureHeader();
            if (signatureHeader.Kind != SignatureKind.Method)
                return null;

            int genericParameterCount = signatureHeader.IsGeneric ? memberReferenceSignature.ReadCompressedInteger() : 0;

            int parameterCount = memberReferenceSignature.ReadCompressedInteger();
            List<MethodDefinitionHandle> candidateMethods = new();
            List<MethodDefinitionHandle> exactSignatureMatches = new();
            foreach (MethodDefinitionHandle methodHandle in reader.GetTypeDefinition((TypeDefinitionHandle)typeDefinition).GetMethods())
            {
                MethodDefinition methodDefinition = reader.GetMethodDefinition(methodHandle);
                if (reader.GetString(methodDefinition.Name) != reader.GetString(memberReference.Name) ||
                    methodDefinition.GetGenericParameters().Count != genericParameterCount)
                {
                    continue;
                }

                BlobReader methodDefinitionSignature = reader.GetBlobReader(methodDefinition.Signature);
                SignatureHeader methodSignatureHeader = methodDefinitionSignature.ReadSignatureHeader();
                if (methodSignatureHeader.Kind != SignatureKind.Method)
                    continue;

                if (methodSignatureHeader.IsGeneric)
                    methodDefinitionSignature.ReadCompressedInteger();

                if (methodDefinitionSignature.ReadCompressedInteger() != parameterCount)
                    continue;

                candidateMethods.Add(methodHandle);
                byte[] methodDefinitionSignatureBytes = reader.GetBlobBytes(methodDefinition.Signature);
                byte[] memberReferenceSignatureBytes = reader.GetBlobBytes(memberReference.Signature);
                if (methodDefinitionSignatureBytes.Length != memberReferenceSignatureBytes.Length)
                    continue;

                bool signaturesMatch = true;
                for (int i = 0; i < methodDefinitionSignatureBytes.Length; i++)
                {
                    if (methodDefinitionSignatureBytes[i] != memberReferenceSignatureBytes[i])
                    {
                        signaturesMatch = false;
                        break;
                    }
                }

                if (!signaturesMatch)
                    continue;

                exactSignatureMatches.Add(methodHandle);
            }

            return exactSignatureMatches.Count == 1
                ? exactSignatureMatches[0]
                : candidateMethods.Count == 1
                    ? candidateMethods[0]
                    : null;
        }

        private void AddUnresolvedGenericArgumentWarnings(NodeFactory factory, MethodDefinitionHandle methodHandle)
        {
            MethodDesc method = _module.GetMethod(methodHandle);
            MethodDesc owningMethod = _module.GetMethod(_methodHandle);
            var diagnosticContext = new DiagnosticContext(
                new MessageOrigin(owningMethod),
                suppressTrimmerDiagnostics: factory.Logger.ShouldSuppressAnalysisWarningsForRequires(
                    owningMethod,
                    DiagnosticUtilities.RequiresUnreferencedCodeAttribute),
                suppressAotDiagnostics: factory.Logger.ShouldSuppressAnalysisWarningsForRequires(
                    owningMethod,
                    DiagnosticUtilities.RequiresDynamicCodeAttribute),
                suppressSingleFileDiagnostics: factory.Logger.ShouldSuppressAnalysisWarningsForRequires(
                    owningMethod,
                    DiagnosticUtilities.RequiresAssemblyFilesAttribute),
                factory.Logger);
            var reflectionMarker = new ReflectionMarker(
                factory.Logger,
                factory,
                factory.FlowAnnotations,
                typeHierarchyDataFlowOrigin: null,
                enabled: false);
            GenericArgumentDataFlow.ProcessUnresolvedGenericArgumentDataFlow(
                diagnosticContext,
                reflectionMarker,
                method);
        }

        private void AddUnresolvedGetTypeWarning(
            NodeFactory factory,
            MethodDesc getTypeMethod,
            MethodParameterValue targetValue)
        {
            MethodDesc owningMethod = _module.GetMethod(_methodHandle);
            var diagnosticContext = new DiagnosticContext(
                new MessageOrigin(owningMethod),
                diagnosticsEnabled: true,
                factory.Logger);
            var reflectionMarker = new ReflectionMarker(
                factory.Logger,
                factory,
                factory.FlowAnnotations,
                typeHierarchyDataFlowOrigin: null,
                enabled: false);
            var action = new RequireDynamicallyAccessedMembersAction(
                reflectionMarker,
                diagnosticContext,
                getTypeMethod);
            MethodReturnValue sourceValue = factory.FlowAnnotations.GetMethodReturnValue(
                new MethodProxy(getTypeMethod),
                isNewObj: false,
                dynamicallyAccessedMemberTypes: DynamicallyAccessedMemberTypes.None);
            action.Invoke(new MultiValue(sourceValue), targetValue);
        }

        public int Write(ModuleWritingContext writeContext)
        {
            int rva = _module.MetadataReader.GetMethodDefinition(_methodHandle).RelativeVirtualAddress;
            if (rva == 0)
                return -1;

            MethodBodyBlock bodyBlock = _module.PEReader.GetMethodBody(rva);
            var exceptionRegions = bodyBlock.ExceptionRegions;

            // Use small exception regions when the code size of the try block and
            // the handler code are less than 256 bytes and offsets smaller than 65536 bytes.
            bool useSmallExceptionRegions = ExceptionRegionEncoder.IsSmallRegionCount(exceptionRegions.Length);
            if (useSmallExceptionRegions)
            {
                foreach (var exceptionRegion in exceptionRegions)
                {
                    if (!ExceptionRegionEncoder.IsSmallExceptionRegion(exceptionRegion.TryOffset, exceptionRegion.TryLength) ||
                        !ExceptionRegionEncoder.IsSmallExceptionRegion(exceptionRegion.HandlerOffset, exceptionRegion.HandlerLength))
                    {
                        useSmallExceptionRegions = false;
                        break;
                    }
                }
            }

            BlobBuilder outputBodyBuilder = writeContext.GetSharedBlobBuilder();
            byte[] bodyBytes = bodyBlock.GetILBytes();
            ILReader ilReader = new ILReader(bodyBytes);
            while (ilReader.HasNext)
            {
                int offset = ilReader.Offset;
                ILOpcode opcode = ilReader.ReadILOpcode();

                switch (opcode)
                {
                    case ILOpcode.sizeof_:
                    case ILOpcode.newarr:
                    case ILOpcode.stsfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldsflda:
                    case ILOpcode.stfld:
                    case ILOpcode.ldfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.call:
                    case ILOpcode.calli:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldtoken:
                    case ILOpcode.ldftn:
                    case ILOpcode.ldvirtftn:
                    case ILOpcode.initobj:
                    case ILOpcode.stelem:
                    case ILOpcode.ldelem:
                    case ILOpcode.ldelema:
                    case ILOpcode.box:
                    case ILOpcode.unbox:
                    case ILOpcode.unbox_any:
                    case ILOpcode.jmp:
                    case ILOpcode.cpobj:
                    case ILOpcode.ldobj:
                    case ILOpcode.castclass:
                    case ILOpcode.isinst:
                    case ILOpcode.stobj:
                    case ILOpcode.refanyval:
                    case ILOpcode.mkrefany:
                    case ILOpcode.constrained:
                        if (opcode > ILOpcode.prefix1)
                        {
                            outputBodyBuilder.WriteByte((byte)ILOpcode.prefix1);
                            outputBodyBuilder.WriteByte((byte)(((int)opcode) & 0xff));
                        }
                        else
                        {
                            Debug.Assert(opcode != ILOpcode.prefix1);
                            outputBodyBuilder.WriteByte((byte)opcode);
                        }
                        EntityHandle token = MetadataTokens.EntityHandle(ilReader.ReadILToken());
                        EntityHandle mappedToken = writeContext.TokenMap.MapToken(token);
                        if (_preserveUnmappedTokens && mappedToken.IsNil)
                            mappedToken = token;
                        outputBodyBuilder.WriteInt32(MetadataTokens.GetToken(mappedToken));
                        break;

                    case ILOpcode.ldstr:
                        outputBodyBuilder.WriteByte((byte)opcode);
                        outputBodyBuilder.WriteInt32(
                            MetadataTokens.GetToken(
                                writeContext.MetadataBuilder.GetOrAddUserString(
                                    _module.MetadataReader.GetUserString(
                                        MetadataTokens.UserStringHandle(ilReader.ReadILToken())))));
                        break;

                    case ILOpcode.switch_:
                        // switch is the opcode, then the number of targets N as int32, then N jump offsets as int32
                        // The offsets should not be affected by trimming, so we can write out exactly the same bytes
                        outputBodyBuilder.WriteByte((byte)opcode);
                        uint numTargets = ilReader.ReadILUInt32();
                        outputBodyBuilder.WriteUInt32(numTargets);
                        var byteCount = (int)(numTargets * sizeof(uint));
                        outputBodyBuilder.WriteBytes(bodyBytes, ilReader.Offset, byteCount);
                        ilReader.Seek(ilReader.Offset + byteCount);
                        break;

                    default:
                        outputBodyBuilder.WriteBytes(bodyBytes, offset, ILOpcodeHelper.GetSize(opcode));
                        ilReader.Skip(opcode);
                        break;
                }
            }

            MethodBodyStreamEncoder.MethodBody bodyEncoder = writeContext.MethodBodyEncoder.AddMethodBody(
                outputBodyBuilder.Count,
                bodyBlock.MaxStack,
                exceptionRegionCount: exceptionRegions.Length,
                hasSmallExceptionRegions: useSmallExceptionRegions,
                (StandaloneSignatureHandle)writeContext.TokenMap.MapToken(bodyBlock.LocalSignature),
                bodyBlock.LocalVariablesInitialized ? MethodBodyAttributes.InitLocals : MethodBodyAttributes.None);
            BlobWriter instructionsWriter = new(bodyEncoder.Instructions);

            ExceptionRegionEncoder exceptionRegionEncoder = bodyEncoder.ExceptionRegions;
            foreach (var exceptionRegion in exceptionRegions)
            {
                switch (exceptionRegion.Kind)
                {
                    case ExceptionRegionKind.Catch:
                        exceptionRegionEncoder.AddCatch(
                            exceptionRegion.TryOffset,
                            exceptionRegion.TryLength,
                            exceptionRegion.HandlerOffset,
                            exceptionRegion.HandlerLength,
                            writeContext.TokenMap.MapToken(exceptionRegion.CatchType));
                        break;

                    case ExceptionRegionKind.Filter:
                        exceptionRegionEncoder.AddFilter(
                            exceptionRegion.TryOffset,
                            exceptionRegion.TryLength,
                            exceptionRegion.HandlerOffset,
                            exceptionRegion.HandlerLength,
                            exceptionRegion.FilterOffset);
                        break;

                    case ExceptionRegionKind.Finally:
                        exceptionRegionEncoder.AddFinally(
                            exceptionRegion.TryOffset,
                            exceptionRegion.TryLength,
                            exceptionRegion.HandlerOffset,
                            exceptionRegion.HandlerLength);
                        break;
                }
            }

            outputBodyBuilder.WriteContentTo(ref instructionsWriter);

            return bodyEncoder.Offset;
        }

        protected override string GetName(NodeFactory factory)
        {
            // TODO: would be nice to have a common formatter we can call into that also includes owning type
            MetadataReader reader = _module.MetadataReader;
            return "Method body for " + reader.GetString(reader.GetMethodDefinition(_methodHandle).Name);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
    }
}
