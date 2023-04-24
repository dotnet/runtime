// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem.Interop
{
    public partial class NativeStructType : MetadataType
    {
        // The managed struct that this type will imitate
        public MetadataType ManagedStructType
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
                return "__NativeType__" + ManagedStructType.Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "__NativeType__" + ManagedStructType.DiagnosticName;
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
                return "Internal.CompilerGenerated";
            }
        }

        public override PInvokeStringFormat PInvokeStringFormat
        {
            get
            {
                return ManagedStructType.PInvokeStringFormat;
            }
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return ManagedStructType.IsExplicitLayout;
            }
        }

        public override int GetInlineArrayLength()
        {
            Debug.Fail("if this can be an inline array, implement GetInlineArrayLength");
            throw new InvalidOperationException();
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return ManagedStructType.IsSequentialLayout;
            }
        }

        public override bool IsBeforeFieldInit
        {
            get
            {
                return ManagedStructType.IsBeforeFieldInit;
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
                return ManagedStructType.Context;
            }
        }

        private NativeStructField[] _fields;
        private InteropStateManager _interopStateManager;
        private bool _hasInvalidLayout;

        public bool HasInvalidLayout
        {
            get
            {
                return _hasInvalidLayout;
            }
        }

        public FieldDesc[] Fields
        {
            get
            {
                return _fields;
            }
        }

        [ThreadStatic]
        private static Stack<MetadataType> s_typesBeingLookedAt;

        public NativeStructType(ModuleDesc owningModule, MetadataType managedStructType, InteropStateManager interopStateManager)
        {
            Debug.Assert(!managedStructType.IsGenericDefinition);

            Module = owningModule;
            ManagedStructType = managedStructType;
            _interopStateManager = interopStateManager;
            _hasInvalidLayout = false;

            Stack<MetadataType> typesBeingLookedAt = (s_typesBeingLookedAt ??= new Stack<MetadataType>());
            if (typesBeingLookedAt.Contains(managedStructType))
                ThrowHelper.ThrowTypeLoadException(managedStructType);

            typesBeingLookedAt.Push(managedStructType);
            try
            {
                CalculateFields();
            }
            finally
            {
                MetadataType popped = typesBeingLookedAt.Pop();
                Debug.Assert(popped == managedStructType);
            }
        }

        private void CalculateFields()
        {
            bool isAnsi = ManagedStructType.PInvokeStringFormat == PInvokeStringFormat.AnsiClass;

            int numFields = 0;
            foreach (FieldDesc field in ManagedStructType.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }
                numFields++;
            }

            _fields = new NativeStructField[numFields];

            int index = 0;
            foreach (FieldDesc field in ManagedStructType.GetFields())
            {
                if (field.IsStatic)
                {
                    continue;
                }

                var managedType = field.FieldType;

                TypeDesc nativeType;
                try
                {
                    nativeType = MarshalHelpers.GetNativeStructFieldType(managedType, field.GetMarshalAsDescriptor(), _interopStateManager, isAnsi);
                }
                catch (NotSupportedException)
                {
                    // if marshalling is not supported for this type the generated stubs will emit appropriate
                    // error message. We just set native type to be same as managedtype
                    nativeType = managedType;
                    _hasInvalidLayout = true;
                }

                _fields[index++] = new NativeStructField(nativeType, this, field);
            }
        }

        public override ClassLayoutMetadata GetClassLayout()
        {
            ClassLayoutMetadata layout = ManagedStructType.GetClassLayout();

            ClassLayoutMetadata result;
            result.PackingSize = layout.PackingSize;
            result.Size = layout.Size;

            if (IsExplicitLayout)
            {
                result.Offsets = new FieldAndOffset[layout.Offsets.Length];

                Debug.Assert(layout.Offsets.Length <= _fields.Length);

                int layoutIndex = 0;
                for (int index = 0; index < _fields.Length; index++)
                {
                    if (_fields[index].Name == layout.Offsets[layoutIndex].Field.Name)
                    {
                        result.Offsets[layoutIndex] = new FieldAndOffset(_fields[index], layout.Offsets[layoutIndex].Offset);
                        layoutIndex++;
                    }
                }

                Debug.Assert(layoutIndex == layout.Offsets.Length);
            }
            else
            {
                result.Offsets = null;
            }

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

        public override IEnumerable<FieldDesc> GetFields()
        {
            return _fields;
        }

        /// <summary>
        /// Synthetic field on <see cref="NativeStructType"/>.
        /// </summary>
        private partial class NativeStructField : FieldDesc
        {
            private TypeDesc _fieldType;
            private MetadataType _owningType;
            private FieldDesc _managedField;

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
                    return _fieldType;
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
                    return _managedField.Name;
                }
            }

            public NativeStructField(TypeDesc nativeType, MetadataType owningType, FieldDesc managedField)
            {
                _fieldType = nativeType;
                _owningType = owningType;
                _managedField = managedField;
            }
        }

    }
}
