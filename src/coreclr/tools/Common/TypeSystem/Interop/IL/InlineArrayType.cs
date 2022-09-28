// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;
using Internal.IL.Stubs;
using Internal.IL;
using System.Threading;

namespace Internal.TypeSystem.Interop
{
    internal sealed partial class InlineArrayType : MetadataType
    {
        public MetadataType ElementType
        {
            get;
        }

        public uint Length
        {
            get;
        }

        public override ModuleDesc Module
        {
            get;
        }

        public override string Name
        {
            get
            {
                return "_InlineArray__" + ElementType.Name + "__"+ Length;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "_InlineArray__" + ElementType.DiagnosticName + "__" + Length;
            }
        }

        public override string Namespace
        {
            get
            {
                return "Internal.CompilerGenerated";
            }
        }

        public override string DiagnosticNamespace
        {
            get
            {
                return Namespace;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        public override PInvokeStringFormat PInvokeStringFormat
        {
            get
            {
                return PInvokeStringFormat.AnsiClass;
            }
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return false;
            }
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return true;
            }
        }

        public override bool IsBeforeFieldInit
        {
            get
            {
                return false;
            }
        }

        public override DefType BaseType
        {
            get
            {
                return (DefType)Context.GetWellKnownType(WellKnownType.ValueType);
            }
        }

        public override MetadataType MetadataBaseType
        {
            get
            {
                return (MetadataType)Context.GetWellKnownType(WellKnownType.ValueType);
            }
        }

        public override bool IsSealed
        {
            get
            {
                return true;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override DefType ContainingType
        {
            get
            {
                return null;
            }
        }

        public override DefType[] ExplicitlyImplementedInterfaces
        {
            get
            {
                return Array.Empty<DefType>();
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return ElementType.Context;
            }
        }

        private InteropStateManager _interopStateManager;
        private MethodDesc[] _methods;
        private FieldDesc[] _fields;

        public InlineArrayType(ModuleDesc owningModule, MetadataType elementType, uint length, InteropStateManager interopStateManager)
        {
            Debug.Assert(elementType.IsTypeDefinition);
            Debug.Assert(elementType.IsValueType);
            Debug.Assert(!elementType.IsGenericDefinition);

            Module = owningModule;
            ElementType = elementType;
            Length = length;
            _interopStateManager = interopStateManager;
        }

        public override ClassLayoutMetadata GetClassLayout()
        {
            ClassLayoutMetadata result = default(ClassLayoutMetadata);
            result.PackingSize = 0;
            result.Size = checked((int)Length * ElementType.GetElementSize().AsInt);
            return result;
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }

        public override IEnumerable<MetadataType> GetNestedTypes()
        {
            return Array.Empty<MetadataType>();
        }

        public override MetadataType GetNestedType(string name)
        {
            return null;
        }

        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType()
        {
            return Array.Empty<MethodImplRecord>();
        }

        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name)
        {
            return Array.Empty<MethodImplRecord>();
        }

        private int _hashCode;

        private void InitializeHashCode()
        {
            var hashCodeBuilder = new Internal.NativeFormat.TypeHashingAlgorithms.HashCodeBuilder(Namespace);

            if (Namespace.Length > 0)
            {
                hashCodeBuilder.Append(".");
            }

            hashCodeBuilder.Append(Name);
            _hashCode = hashCodeBuilder.ToHashCode();
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                InitializeHashCode();
            }
            return _hashCode;
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;
            }

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= TypeFlags.ValueType;
            }

            flags |= TypeFlags.HasFinalizerComputed;
            flags |= TypeFlags.AttributeCacheComputed;

            return flags;
        }

        private void InitializeMethods()
        {
            MethodDesc[] methods = new MethodDesc[] {
                    new InlineArrayMethod(this, InlineArrayMethodKind.Getter),
                    new InlineArrayMethod(this, InlineArrayMethodKind.Setter),
                };

            Interlocked.CompareExchange(ref _methods, methods, null);
        }
        public override IEnumerable<MethodDesc> GetMethods()
        {
            if (_methods == null)
            {
                InitializeMethods();
            }
            return _methods;
        }

        public MethodDesc GetInlineArrayMethod(InlineArrayMethodKind kind)
        {
            if (_methods == null)
            {
                InitializeMethods();
            }
            return _methods[(int)kind];
        }

        private void InitializeFields()
        {
            // The inline array will inherit alignment from the dummy field
            FieldDesc[] fields = new FieldDesc[] {
                new InlineArrayField(this)
            };

            Interlocked.CompareExchange(ref _fields, fields, null);
        }
        public override IEnumerable<FieldDesc> GetFields()
        {
            if (_fields == null)
            {
                InitializeFields();
            }
            return _fields;
        }

        private sealed partial class InlineArrayMethod : ILStubMethod
        {
            private InlineArrayType _owningType;
            private InlineArrayMethodKind _kind;
            private MethodSignature _signature;

            public InlineArrayMethod(InlineArrayType owningType, InlineArrayMethodKind kind)
            {
                _owningType = owningType;
                _kind = kind;
            }

            public override TypeDesc OwningType
            {
                get
                {
                    return _owningType;
                }
            }

            public override TypeSystemContext Context
            {
                get
                {
                    return _owningType.Context;
                }
            }

            public override string Name
            {
                get
                {
                    if (_kind == InlineArrayMethodKind.Getter)
                    {
                        return "get_Item";
                    }
                    else
                    {
                        return "set_Item";
                    }
                }
            }

            public override string DiagnosticName
            {
                get
                {
                    return Name;
                }
            }

            public override MethodSignature Signature
            {
                get
                {
                    if (_signature == null)
                    {
                        if (_kind == InlineArrayMethodKind.Getter)
                        {
                            _signature = new MethodSignature(MethodSignatureFlags.None,
                                     genericParameterCount: 0,
                                    returnType: _owningType.ElementType,
                                    parameters: new TypeDesc[] { Context.GetWellKnownType(WellKnownType.Int32) });
                        }
                        else
                        {
                            _signature = new MethodSignature(MethodSignatureFlags.None,
                                     genericParameterCount: 0,
                                    returnType: Context.GetWellKnownType(WellKnownType.Void),
                                    parameters: new TypeDesc[] { Context.GetWellKnownType(WellKnownType.Int32), _owningType.ElementType });
                        }
                    }
                    return _signature;
                }
            }

            public override MethodIL EmitIL()
            {
                var emitter = new ILEmitter();
                var codeStream = emitter.NewCodeStream();
                _ = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Boolean));
                var elementType = _owningType.ElementType;

                // Getter:
                // return ((ElementType*)(&this))[index];
                //
                // Setter:
                // fixed (InlineArray* pThis = &this)
                //{
                //  ((ElementType*)pThis)[(ulong)index] = (ElementType)value;
                //}

                var vThis = emitter.NewLocal(_owningType.MakeByRefType());
                codeStream.EmitLdArg(0);
                codeStream.EmitStLoc(vThis);
                codeStream.EmitLdLoc(vThis);

                codeStream.EmitLdArg(1);
                codeStream.Emit(ILOpcode.conv_i4);

                codeStream.Emit(ILOpcode.sizeof_, emitter.NewToken(elementType));

                codeStream.Emit(ILOpcode.conv_i4);
                codeStream.Emit(ILOpcode.mul);
                codeStream.Emit(ILOpcode.conv_i);
                codeStream.Emit(ILOpcode.add);

                if (_kind == InlineArrayMethodKind.Getter)
                {
                    codeStream.EmitLdInd(elementType);
                }
                else
                {
                    codeStream.EmitLdArg(2);

                    codeStream.EmitStInd(elementType);
                    codeStream.EmitLdc(0);
                    codeStream.Emit(ILOpcode.conv_u);
                    codeStream.EmitStLoc(vThis);
                }

                codeStream.Emit(ILOpcode.ret);
                return emitter.Link(this);
            }
        }

        private sealed partial class InlineArrayField : FieldDesc
        {
            private InlineArrayType _owningType;

            public override TypeSystemContext Context
            {
                get
                {
                    return _owningType.Context;
                }
            }

            public override TypeDesc FieldType
            {
                get
                {
                    return _owningType.ElementType;
                }
            }
            public override EmbeddedSignatureData[] GetEmbeddedSignatureData() => null;

            public override bool HasRva
            {
                get
                {
                    return false;
                }
            }

            public override bool IsInitOnly
            {
                get
                {
                    return false;
                }
            }

            public override bool IsLiteral
            {
                get
                {
                    return false;
                }
            }

            public override bool IsStatic
            {
                get
                {
                    return false;
                }
            }

            public override bool IsThreadStatic
            {
                get
                {
                    return false;
                }
            }

            public override DefType OwningType
            {
                get
                {
                    return _owningType;
                }
            }

            public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
            {
                return false;
            }

            public override string Name
            {
                get
                {
                    return "InlineArrayField";
                }
            }

            public InlineArrayField(InlineArrayType owningType)
            {
                _owningType = owningType;
            }
        }
    }

    public enum InlineArrayMethodKind : byte
    {
        Getter = 0,
        Setter = 1
    }

}
