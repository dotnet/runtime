// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        GrammarResult ICILVisitor<GrammarResult>.VisitBound(CILParser.BoundContext context) => VisitBound(context);
        public GrammarResult.Literal<(int? Lower, int? Upper)> VisitBound(CILParser.BoundContext context)
        {
            bool hasEllipsis = context.ELLIPSIS() is not null;
            var indices = context.int32();

            if (indices.Length == 0)
            {
                // Empty or standalone "..."
                return new((null, null));
            }

            int firstValue = VisitInt32(indices[0]).Value;

            return (indices.Length, hasEllipsis) switch
            {
                (1, false) => new((0, firstValue)),
                (1, true) => new((firstValue, null)),
                (2, _) => new((firstValue, VisitInt32(indices[1]).Value - firstValue + 1)),
                _ => throw new UnreachableException()
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitBounds(CILParser.BoundsContext context) => VisitBounds(context);
        public GrammarResult.Sequence<(int? Lower, int? Upper)> VisitBounds(CILParser.BoundsContext context)
        {
            return new(context.bound().Select(bound => VisitBound(bound).Value).ToImmutableArray());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCallConv(CILParser.CallConvContext context) => VisitCallConv(context);
        public GrammarResult.Literal<byte> VisitCallConv(CILParser.CallConvContext context)
        {
            if (context.callKind() is CILParser.CallKindContext callKind)
            {
                return new((byte)VisitCallKind(callKind).Value);
            }
            else if (context.int32() is CILParser.Int32Context int32)
            {
                return new((byte)VisitInt32(int32).Value);
            }
            else if (context.INSTANCE() is not null)
            {
                return new((byte)(VisitCallConv(context.callConv()).Value | (byte)SignatureAttributes.Instance));
            }
            else if (context.EXPLICIT() is not null)
            {
                return new((byte)(
                    VisitCallConv(context.callConv()).Value |
                    (byte)(SignatureAttributes.ExplicitThis | SignatureAttributes.Instance)));
            }
            return new(0);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitCallKind(CILParser.CallKindContext context) => VisitCallKind(context);
        public GrammarResult.Literal<SignatureCallingConvention> VisitCallKind(CILParser.CallKindContext context)
        {
            // callKind can be empty (/* EMPTY */) - return Default in that case
            if (context.ChildCount == 0)
            {
                return new(SignatureCallingConvention.Default);
            }
            int childType = context.GetChild<ITerminalNode>(context.ChildCount - 1).Symbol.Type;
            return new(childType switch
            {
                CILParser.DEFAULT => SignatureCallingConvention.Default,
                CILParser.VARARG => SignatureCallingConvention.VarArgs,
                CILParser.CDECL => SignatureCallingConvention.CDecl,
                CILParser.STDCALL => SignatureCallingConvention.StdCall,
                CILParser.THISCALL => SignatureCallingConvention.ThisCall,
                CILParser.FASTCALL => SignatureCallingConvention.FastCall,
                CILParser.UNMANAGED => SignatureCallingConvention.Unmanaged,
                _ => throw new UnreachableException()
            });
        }

        private BlobBuilder BuildMethodReferenceSignature(
            CILParser.CallConvContext callConvention,
            CILParser.TypeContext returnType,
            CILParser.SigArgsContext signatureArguments,
            int genericArity)
        {
            var signature = new BlobBuilder();
            byte header = VisitCallConv(callConvention).Value;
            if (genericArity > 0)
            {
                header |= (byte)SignatureAttributes.Generic;
            }

            signature.WriteByte(header);
            if (genericArity > 0)
            {
                signature.WriteCompressedInteger(genericArity);
            }

            ImmutableArray<SignatureArg> arguments = VisitSigArgs(signatureArguments).Value;
            signature.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
            VisitType(returnType).Value.WriteContentTo(signature);
            foreach (SignatureArg argument in arguments)
            {
                argument.SignatureBlob.WriteContentTo(signature);
            }

            return signature;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitElementType(CILParser.ElementTypeContext context) => VisitElementType(context);
        public GrammarResult.FormattedBlob VisitElementType(CILParser.ElementTypeContext context)
        {
            BlobBuilder blob = new(5);
            if (context.OBJECT() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Object);
            }
            else if (context.className() is CILParser.ClassNameContext className)
            {
                EntityRegistry.TypeEntity typeEntity = VisitClassName(className).Value;
                if (context.VALUE() is not null || context.VALUETYPE() is not null)
                {
                    // Check for well-known value types that should use primitive type codes
                    if (TryGetPrimitiveTypeCode(typeEntity, isValueType: true) is { } vtPrimCode)
                    {
                        blob.WriteByte((byte)vtPrimCode);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeKind.ValueType);
                        blob.WriteTypeEntity(typeEntity);
                    }
                }
                else
                {
                    // Check for well-known class types that should use primitive type codes
                    if (TryGetPrimitiveTypeCode(typeEntity, isValueType: false) is { } clsPrimCode)
                    {
                        blob.WriteByte((byte)clsPrimCode);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeKind.Class);
                        blob.WriteTypeEntity(typeEntity);
                    }
                }
            }
            else if (context.callConv() is CILParser.CallConvContext callConv)
            {
                // Emit function pointer signature.
                blob.WriteByte((byte)SignatureTypeCode.FunctionPointer);
                byte sigCallConv = VisitCallConv(callConv).Value;
                blob.WriteByte(sigCallConv);
                var signatureArgs = VisitSigArgs(context.sigArgs()).Value;
                int numArgs = signatureArgs.Count(arg => !arg.IsSentinel);
                blob.WriteCompressedInteger(numArgs);
                blob.LinkSuffix(VisitType(context.type()).Value);
                foreach (var arg in signatureArgs)
                {
                    blob.LinkSuffix(arg.SignatureBlob);
                }
            }
            else if (context.ELLIPSIS() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Sentinel);
                blob.LinkSuffix(VisitType(context.type()).Value);
            }
            else if (context.METHOD_TYPE_PARAMETER() is not null)
            {
                if (context.int32() is CILParser.Int32Context int32)
                {
                    // COMPAT: Always write a reference to a generic method parameter by index
                    // even if we aren't in a method or the index is out of range. We want to be able to write invalid IL like this.
                    blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                    blob.WriteCompressedInteger(VisitInt32(int32).Value);
                }
                else
                {
                    string dottedName = VisitDottedName(context.dottedName()).Value;
                    if (_currentMethod is null)
                    {
                        ReportError(DiagnosticIds.MethodTypeParameterOutsideMethod, string.Format(DiagnosticMessageTemplates.MethodTypeParameterOutsideMethod, dottedName), context);
                        blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                        blob.WriteCompressedInteger(0);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeCode.GenericMethodParameter);
                        bool foundParameter = false;
                        for (int i = 0; i < _currentMethod.Definition.GenericParameters.Count; i++)
                        {
                            EntityRegistry.GenericParameterEntity? genericParameter = _currentMethod.Definition.GenericParameters[i];
                            if (genericParameter.Name == dottedName)
                            {
                                foundParameter = true;
                                blob.WriteCompressedInteger(i);
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            // BREAK-COMPAT: ILASM would silently emit an invalid signature when a method uses an invalid method type parameter but doesn't have method type parameters.
                            // The signature used completely invalid undocumented codes (that were really sentinel values for how ilasm later detected errors due to how the parsing model worked with a YACC-based parser)
                            // and when a method had no type parameters, it didn't run the code to process out these values and emit errors.
                            // This seems like a scenario that doesn't need to be brought forward.
                            // Instead, we'll just emit a reference to "generic method parameter" 0 and report an error.

                            ReportError(DiagnosticIds.GenericParameterNotFound, string.Format(DiagnosticMessageTemplates.GenericParameterNotFound, dottedName), context);
                            blob.WriteCompressedInteger(0);
                        }
                    }
                }
            }
            else if (context.TYPE_PARAMETER() is not null)
            {
                if (context.int32() is CILParser.Int32Context int32)
                {
                    // COMPAT: Always write a reference to a generic type parameter by index
                    // even if we aren't in a type or the index is out of range. We want to be able to write invalid IL like this.
                    blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                    blob.WriteCompressedInteger(VisitInt32(int32).Value);
                }
                else
                {
                    string dottedName = VisitDottedName(context.dottedName()).Value;
                    if (_currentTypeDefinition.Count == 0)
                    {
                        ReportError(DiagnosticIds.TypeParameterOutsideType, string.Format(DiagnosticMessageTemplates.TypeParameterOutsideType, dottedName), context);
                        blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                        blob.WriteCompressedInteger(0);
                    }
                    else
                    {
                        blob.WriteByte((byte)SignatureTypeCode.GenericTypeParameter);
                        bool foundParameter = false;
                        for (int i = 0; i < _currentTypeDefinition.Peek().GenericParameters.Count; i++)
                        {
                            EntityRegistry.GenericParameterEntity? genericParameter = _currentTypeDefinition.Peek().GenericParameters[i];
                            if (genericParameter.Name == dottedName)
                            {
                                foundParameter = true;
                                blob.WriteCompressedInteger(i);
                                break;
                            }
                        }
                        if (!foundParameter)
                        {
                            // BREAK-COMPAT: ILASM would silently emit an invalid signature when a type uses an invalid method type parameter but doesn't have any type parameters.
                            // The signature used completely invalid undocumented codes (that were really sentinel values for how ilasm later detected errors due to how the parsing model worked with a YACC-based parser)
                            // and when a method had no type parameters, it didn't run the code to process out these values and emit errors.
                            // This seems like a scenario that doesn't need to be brought forward.
                            // Instead, we'll just emit a reference to "generic method parameter" 0 and report an error.

                            ReportError(DiagnosticIds.GenericParameterNotFound, string.Format(DiagnosticMessageTemplates.GenericParameterNotFound, dottedName), context);
                            blob.WriteCompressedInteger(0);
                        }
                    }
                }
            }
            else if (context.TYPEDREF() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.TypedReference);
            }
            else if (context.VOID() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.Void);
            }
            else if (context.nativeInt() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.IntPtr);
            }
            else if (context.nativeUint() is not null)
            {
                blob.WriteByte((byte)SignatureTypeCode.UIntPtr);
            }
            else if (context.simpleType() is CILParser.SimpleTypeContext simpleType)
            {
                blob.WriteByte((byte)VisitSimpleType(simpleType).Value);
            }
            else if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                // Typedef reference - resolve and write the type blob
                string alias = VisitDottedName(dottedName).Value;
                var resolved = TryResolveTypedefAsTypeBlob(alias);
                if (resolved is not null)
                {
                    // Copy the content to avoid modifying the stored blob
                    resolved.WriteContentTo(blob);
                }
                else
                {
                    ReportError(DiagnosticIds.TypedefNotFound, string.Format(DiagnosticMessageTemplates.TypedefNotFound, alias), context);
                }
            }
            else
            {
                throw new UnreachableException();
            }
            return new(blob);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitGenArity(CILParser.GenArityContext context) => VisitGenArity(context);
        public GrammarResult.Literal<int> VisitGenArity(CILParser.GenArityContext context)
            => context.genArityNotEmpty() is CILParser.GenArityNotEmptyContext genArity ? VisitGenArityNotEmpty(genArity) : new(0);

        GrammarResult ICILVisitor<GrammarResult>.VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => VisitGenArityNotEmpty(context);
        public GrammarResult.Literal<int> VisitGenArityNotEmpty(CILParser.GenArityNotEmptyContext context) => VisitInt32(context.int32());

        GrammarResult ICILVisitor<GrammarResult>.VisitNativeInt(CILParser.NativeIntContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitNativeUint(CILParser.NativeUintContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodRef(CILParser.MethodRefContext context) => VisitMethodRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMethodRef(CILParser.MethodRefContext context)
        {
            if (context.mdtoken() is CILParser.MdtokenContext token)
            {
                return new(VisitMdtoken(token).Value);
            }
            if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                // This is a typedef reference for a method member
                string alias = VisitDottedName(dottedName).Value;
                var resolved = TryResolveTypedefAsMember(alias);
                if (resolved is not null)
                {
                    return new(resolved);
                }
                ReportError(DiagnosticIds.TypedefNotFound, string.Format(DiagnosticMessageTemplates.TypedefNotFound, alias), context);
                return new(_entityRegistry.CreateLazilyRecordedMemberReference(_entityRegistry.ModuleType, alias, new BlobBuilder()));
            }
            BlobBuilder methodRefSignature = new();
            if (context.callConv() is not CILParser.CallConvContext callConvCtx)
            {
                // Parse error recovery - callConv is missing
                return new(_entityRegistry.CreateLazilyRecordedMemberReference(_entityRegistry.ModuleType, "<error>", methodRefSignature));
            }
            byte callConv = VisitCallConv(callConvCtx).Value;
            EntityRegistry.TypeEntity owner = _entityRegistry.ModuleType;
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                owner = VisitTypeSpec(typeSpec).Value;
            }
            string name = VisitMethodName(context.methodName()).Value;
            BlobBuilder? methodSpecSignature = null;
            int numGenericParameters = 0;
            if (context.typeArgs() is CILParser.TypeArgsContext typeArgs)
            {
                var types = typeArgs.type();
                numGenericParameters = types.Length;
                if (types.Length != 0)
                {
                    methodSpecSignature = new();
                    methodSpecSignature.WriteByte((byte)SignatureKind.MethodSpecification);
                    VisitTypeArgs(typeArgs).Value.WriteContentTo(methodSpecSignature);
                }
            }
            else if (context.genArityNotEmpty() is CILParser.GenArityNotEmptyContext genArityNotEmpty)
            {
                numGenericParameters = VisitGenArityNotEmpty(genArityNotEmpty).Value;
            }
            if (numGenericParameters != 0)
            {
                callConv |= (byte)SignatureAttributes.Generic;
            }
            if (_expectInstance && (callConv & (byte)SignatureAttributes.Instance) == 0)
            {
                ReportWarning(DiagnosticIds.MissingInstanceCallConv,
                    DiagnosticMessageTemplates.MissingInstanceCallConv,
                    context);
                callConv |= (byte)SignatureAttributes.Instance;
            }
            methodRefSignature.WriteByte(callConv);
            if (numGenericParameters != 0)
            {
                methodRefSignature.WriteCompressedInteger(numGenericParameters);
            }
            var args = VisitSigArgs(context.sigArgs()).Value;
            methodRefSignature.WriteCompressedInteger(args.Count(arg => !arg.IsSentinel));
            // Write return type
            VisitType(context.type()).Value.WriteContentTo(methodRefSignature);
            // Write arg signatures
            foreach (var arg in args)
            {
                arg.SignatureBlob.WriteContentTo(methodRefSignature);
            }

            var memberRef = _entityRegistry.CreateLazilyRecordedMemberReference(owner, name, methodRefSignature);

            if (methodSpecSignature is not null)
            {
                return new(_entityRegistry.GetOrCreateMethodSpecification(memberRef, methodSpecSignature));
            }

            return new(memberRef);
        }

        private EntityRegistry.MemberReferenceEntity CreateExplicitMethodReference(
            CILParser.CallConvContext callConv,
            CILParser.TypeContext returnType,
            CILParser.TypeSpecContext owner,
            CILParser.MethodNameContext methodName,
            CILParser.GenArityContext? genericArity,
            CILParser.SigArgsContext parameterList)
        {
            EntityRegistry.TypeEntity ownerType = VisitTypeSpec(owner).Value;
            string name = VisitMethodName(methodName).Value;
            return _entityRegistry.CreateLazilyRecordedMemberReference(
                ownerType,
                name,
                CreateExplicitMethodSignature(callConv, returnType, genericArity, parameterList));
        }

        private BlobBuilder CreateExplicitMethodSignature(
            CILParser.CallConvContext callConv,
            CILParser.TypeContext returnType,
            CILParser.GenArityContext? genericArity,
            CILParser.SigArgsContext parameterList)
        {
            BlobBuilder signature = new();
            byte signatureHeader = VisitCallConv(callConv).Value;
            int arity = genericArity is null ? 0 : VisitGenArity(genericArity).Value;
            if (arity != 0)
            {
                signatureHeader |= (byte)SignatureAttributes.Generic;
            }

            signature.WriteByte(signatureHeader);
            if (arity != 0)
            {
                signature.WriteCompressedInteger(arity);
            }

            ImmutableArray<SignatureArg> parameters = VisitSigArgs(parameterList).Value;
            signature.WriteCompressedInteger(parameters.Count(parameter => !parameter.IsSentinel));
            VisitType(returnType).Value.WriteContentTo(signature);
            foreach (SignatureArg parameter in parameters)
            {
                parameter.SignatureBlob.WriteContentTo(signature);
            }

            return signature;
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitParamAttr(CILParser.ParamAttrContext context) => VisitParamAttr(context);
        public GrammarResult.Literal<ParameterAttributes> VisitParamAttr(CILParser.ParamAttrContext context)
        {
            ParameterAttributes attributes = 0;
            foreach (var element in context.paramAttrElement())
            {
                attributes |= VisitParamAttrElement(element);
            }
            return new(attributes);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitParamAttrElement(CILParser.ParamAttrElementContext context) => VisitParamAttrElement(context);
        public GrammarResult.Flag<ParameterAttributes> VisitParamAttrElement(CILParser.ParamAttrElementContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((ParameterAttributes)(VisitInt32(int32).Value + 1), ShouldAppend: false);
            }
            return context switch
            {
                { @in: not null } => new(ParameterAttributes.In),
                { @out: not null } => new(ParameterAttributes.Out),
                { opt: not null } => new(ParameterAttributes.Optional),
                _ => throw new UnreachableException()
            };
        }

        /// <summary>
        /// Checks if a type entity is a well-known corelib type and returns its primitive type code.
        /// Native ilasm uses primitive type codes for well-known types like System.String and System.Object
        /// in signature blobs instead of class/valuetype TypeRef references.
        /// </summary>
        private static SignatureTypeCode? TryGetPrimitiveTypeCode(EntityRegistry.TypeEntity typeEntity, bool isValueType)
        {
            if (typeEntity is not EntityRegistry.TypeReferenceEntity typeRef)
            {
                return null;
            }

            string name = typeRef.Name;
            string ns = typeRef.Namespace;

            if (ns != "System")
            {
                return null;
            }

            if (isValueType)
            {
                return name switch
                {
                    "Boolean" => SignatureTypeCode.Boolean,
                    "Char" => SignatureTypeCode.Char,
                    "SByte" => SignatureTypeCode.SByte,
                    "Byte" => SignatureTypeCode.Byte,
                    "Int16" => SignatureTypeCode.Int16,
                    "UInt16" => SignatureTypeCode.UInt16,
                    "Int32" => SignatureTypeCode.Int32,
                    "UInt32" => SignatureTypeCode.UInt32,
                    "Int64" => SignatureTypeCode.Int64,
                    "UInt64" => SignatureTypeCode.UInt64,
                    "Single" => SignatureTypeCode.Single,
                    "Double" => SignatureTypeCode.Double,
                    "IntPtr" => SignatureTypeCode.IntPtr,
                    "UIntPtr" => SignatureTypeCode.UIntPtr,
                    "TypedReference" => SignatureTypeCode.TypedReference,
                    _ => null
                };
            }

            return name switch
            {
                "String" => SignatureTypeCode.String,
                "Object" => SignatureTypeCode.Object,
                _ => null
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSigArg(CILParser.SigArgContext context) => VisitSigArg(context);
        public GrammarResult.Literal<SignatureArg> VisitSigArg(CILParser.SigArgContext context)
        {
            if (context.ELLIPSIS() is not null)
            {
                return new(SignatureArg.CreateSentinelArgument());
            }
            string? name = context.id() is CILParser.IdContext id ? VisitId(id).Value : null;
            return new(new SignatureArg(
                VisitParamAttr(context.paramAttr()).Value,
                VisitType(context.type()).Value,
                VisitMarshalClause(context.marshalClause()).Value,
                name));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitSigArgs(CILParser.SigArgsContext context) => VisitSigArgs(context);
        public GrammarResult.Sequence<SignatureArg> VisitSigArgs(CILParser.SigArgsContext context) => new([.. context.sigArg().Select(arg => VisitSigArg(arg).Value)]);
        GrammarResult ICILVisitor<GrammarResult>.VisitSimpleType(CILParser.SimpleTypeContext context) => VisitSimpleType(context);
        public GrammarResult.Literal<SignatureTypeCode> VisitSimpleType(CILParser.SimpleTypeContext context)
        {
            // Handle 'unsigned intN' forms (2 children: 'unsigned' + intN keyword)
            if (context.ChildCount == 2)
            {
                return new(context.GetChild<ITerminalNode>(1).Symbol.Type switch
                {
                    CILParser.INT8 => SignatureTypeCode.Byte,
                    CILParser.INT16 => SignatureTypeCode.UInt16,
                    CILParser.INT32_ => SignatureTypeCode.UInt32,
                    CILParser.INT64_ => SignatureTypeCode.UInt64,
                    _ => throw new UnreachableException()
                });
            }

            return new(context.GetChild<ITerminalNode>(0).Symbol.Type switch
            {
                CILParser.CHAR => SignatureTypeCode.Char,
                CILParser.STRING => SignatureTypeCode.String,
                CILParser.BOOL => SignatureTypeCode.Boolean,
                CILParser.INT8 => SignatureTypeCode.SByte,
                CILParser.INT16 => SignatureTypeCode.Int16,
                CILParser.INT32_ => SignatureTypeCode.Int32,
                CILParser.INT64_ => SignatureTypeCode.Int64,
                CILParser.FLOAT32 => SignatureTypeCode.Single,
                CILParser.FLOAT64_ => SignatureTypeCode.Double,
                CILParser.UINT8 => SignatureTypeCode.Byte,
                CILParser.UINT16 => SignatureTypeCode.UInt16,
                CILParser.UINT32 => SignatureTypeCode.UInt32,
                CILParser.UINT64 => SignatureTypeCode.UInt64,
                _ => throw new UnreachableException()
            });
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitType(CILParser.TypeContext context) => VisitType(context);
        public GrammarResult.FormattedBlob VisitType(CILParser.TypeContext context)
        {
            // These blobs will likely be very small, so use a smaller default size.
            const int DefaultSignatureElementBlobSize = 10;
            BlobBuilder prefix = new(DefaultSignatureElementBlobSize);
            BlobBuilder suffix = new(DefaultSignatureElementBlobSize);
            BlobBuilder elementType = VisitElementType(context.elementType()).Value;

            // Prefix blob writes outer modifiers first.
            // Suffix blob writes inner modifiers first.
            // Since all blobs are prefix blobs and only some have suffix data,
            // We will go in reverse order to write the prefixes
            // and then go in forward order to write the suffixes.
            CILParser.TypeModifiersContext[] typeModifiers = context.typeModifiers();
            for (int i = typeModifiers.Length - 1; i >= 0; i--)
            {
                CILParser.TypeModifiersContext? modifier = typeModifiers[i];
                switch (modifier)
                {
                    case CILParser.SZArrayModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.SZArray);
                        break;
                    case CILParser.ArrayModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Array);
                        break;
                    case CILParser.ByRefModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.ByReference);
                        break;
                    case CILParser.PtrModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Pointer);
                        break;
                    case CILParser.PinnedModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.Pinned);
                        break;
                    case CILParser.RequiredModifierContext modreq:
                        prefix.WriteByte((byte)SignatureTypeCode.RequiredModifier);
                        prefix.WriteTypeEntity(VisitTypeSpec(modreq.typeSpec()).Value);
                        break;
                    case CILParser.OptionalModifierContext modopt:
                        prefix.WriteByte((byte)SignatureTypeCode.OptionalModifier);
                        prefix.WriteTypeEntity(VisitTypeSpec(modopt.typeSpec()).Value);
                        break;
                    case CILParser.GenericArgumentsModifierContext:
                        prefix.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
                        break;
                }
            }

            foreach (var modifier in typeModifiers)
            {
                switch (modifier)
                {
                    case CILParser.ArrayModifierContext arr:
                        var bounds = VisitBounds(arr.bounds()).Value;
                        suffix.WriteCompressedInteger(bounds.Length);
                        // Count contiguous sizes from the start (stop at first null)
                        int numSizes = 0;
                        for (int bIdx = 0; bIdx < bounds.Length; bIdx++)
                        {
                            if (bounds[bIdx].Upper is null)
                                break;
                            numSizes++;
                        }
                        // Count contiguous lower bounds from the start (stop at first null)
                        int numLoBounds = 0;
                        for (int bIdx = 0; bIdx < bounds.Length; bIdx++)
                        {
                            if (bounds[bIdx].Lower is null)
                                break;
                            numLoBounds++;
                        }
                        suffix.WriteCompressedInteger(numSizes);
                        for (int bIdx = 0; bIdx < numSizes; bIdx++)
                        {
                            suffix.WriteCompressedInteger(bounds[bIdx].Upper.GetValueOrDefault());
                        }
                        suffix.WriteCompressedInteger(numLoBounds);
                        for (int bIdx = 0; bIdx < numLoBounds; bIdx++)
                        {
                            suffix.WriteCompressedSignedInteger(bounds[bIdx].Lower.GetValueOrDefault());
                        }
                        break;
                    case CILParser.GenericArgumentsModifierContext genericArgs:
                        VisitTypeArgs(genericArgs.typeArgs()).Value.WriteContentTo(suffix);
                        break;
                }
            }

            // Work around https://github.com/dotnet/runtime/issues/127243
            // by writing to a separate blob.
            BlobBuilder fullBlob = new(elementType.Count + prefix.Count + suffix.Count);
            prefix.WriteContentTo(fullBlob);
            elementType.WriteContentTo(fullBlob);
            suffix.WriteContentTo(fullBlob);
            return new(fullBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeArgs(CILParser.TypeArgsContext context) => VisitTypeArgs(context);

        public GrammarResult.FormattedBlob VisitTypeArgs(CILParser.TypeArgsContext context)
        {
            BlobBuilder blob = new(4);
            var types = context.type();
            blob.WriteCompressedInteger(types.Length);
            foreach (var type in types)
            {
                blob.LinkSuffix(VisitType(type).Value);
            }
            return new(blob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeList(CILParser.TypeListContext context) => VisitTypeList(context);
        public GrammarResult.Sequence<EntityRegistry.TypeEntity> VisitTypeList(CILParser.TypeListContext context)
        {
            CILParser.TypeSpecContext[] bounds = context.typeSpec();
            ImmutableArray<EntityRegistry.TypeEntity>.Builder builder = ImmutableArray.CreateBuilder<EntityRegistry.TypeEntity>(bounds.Length);
            foreach (var typeSpec in bounds)
            {
                builder.Add(VisitTypeSpec(typeSpec).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypeSpec(CILParser.TypeSpecContext context) => VisitTypeSpec(context);
        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitTypeSpec(CILParser.TypeSpecContext context)
        {
            if (context.className() is CILParser.ClassNameContext className)
            {
                return new(VisitClassName(className).Value);
            }
            else if (context.dottedName() is CILParser.DottedNameContext dottedName)
            {
                string nameToResolve = VisitDottedName(dottedName).Value;
                if (context.MODULE() is not null)
                {
                    EntityRegistry.ModuleReferenceEntity? module = _entityRegistry.FindModuleReference(nameToResolve);
                    if (module is null)
                    {
                        // report error
                        return new(new EntityRegistry.FakeTypeEntity(MetadataTokens.ModuleReferenceHandle(0)));
                    }
                    return new(new EntityRegistry.FakeTypeEntity(module.Handle));
                }
                else
                {
                    return new(new EntityRegistry.FakeTypeEntity(
                        _entityRegistry.GetOrCreateAssemblyReference(nameToResolve, newRef =>
                        {
                            // Report warning on implicit assembly reference creation.
                        }).Handle));
                }
            }
            else
            {
                Debug.Assert(context.type() != null);
                return new(_entityRegistry.GetOrCreateTypeSpec(VisitType(context.type()).Value));
            }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitOptionalModifier(CILParser.OptionalModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitSZArrayModifier(CILParser.SZArrayModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitRequiredModifier(CILParser.RequiredModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitPtrModifier(CILParser.PtrModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitPinnedModifier(CILParser.PinnedModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitGenericArgumentsModifier(CILParser.GenericArgumentsModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitByRefModifier(CILParser.ByRefModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitArrayModifier(CILParser.ArrayModifierContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

    }
}
