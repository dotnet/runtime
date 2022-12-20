// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;

namespace Microsoft.Diagnostics.Tools.Pgo.TypeRefTypeSystem
{
    class TypeRefTypeSystemType : MetadataType
    {
        TypeRefTypeSystemModule _module;
        List<TypeRefTypeSystemMethod> _methods = new List<TypeRefTypeSystemMethod>();
        List<TypeRefTypeSystemField> _fields = new List<TypeRefTypeSystemField>();
        bool? _isValueType;
        Instantiation? _instantiation;
        string _name;
        string _namespace;
        Dictionary<string, TypeRefTypeSystemType> _nestedType;
        TypeRefTypeSystemType _containingType;

        public TypeRefTypeSystemType(string nameSpace, string name, TypeRefTypeSystemModule module)
        {
            _namespace = nameSpace;
            _name = name;
            _module = module;
        }

        private TypeRefTypeSystemType(string nameSpace, string name, TypeRefTypeSystemType containingType, TypeRefTypeSystemModule module)
        {
            _namespace = nameSpace;
            _name = name;
            _module = module;
            _containingType = containingType;
        }

        public void SetIsValueType(bool isValueType)
        {
            if (!_isValueType.HasValue)
            {
                _isValueType = isValueType;
            }
            else
            {
                if (_isValueType.Value != isValueType)
                {
                    throw new Exception($"Same type `{ToString()}` used as both ValueType and non-ValueType");
                }
            }
        }

        public void SetGenericParameterCount(int parameterCount)
        {
            if (!_instantiation.HasValue)
            {
                if (parameterCount == 0)
                {
                    _instantiation = Instantiation.Empty;
                }
                else
                {
                    TypeDesc[] instantiationArgs = new TypeDesc[parameterCount];
                    for (int i = 0; i < parameterCount; i++)
                    {
                        instantiationArgs[i] = new TypeRefTypeSystemGenericParameter(this, i);
                    }
                    _instantiation = new Instantiation(instantiationArgs);
                }
            }
            else
            {
                if (_instantiation.Value.Length != parameterCount)
                {
                    throw new Exception($"Same type `{ToString()}` expected to have both {_instantiation.Value.Length} and {parameterCount} generic arguments");
                }
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                if (!_instantiation.HasValue)
                    SetGenericParameterCount(0);
                return _instantiation.Value;
            }
        }

        public TypeRefTypeSystemType GetOrAddNestedType(string name)
        {
            if (_nestedType == null)
            {
                _nestedType = new Dictionary<string, TypeRefTypeSystemType>();
            }

            if (!_nestedType.TryGetValue(name, out TypeRefTypeSystemType type))
            {
                type = new TypeRefTypeSystemType(null, name, this, _module);
                _nestedType.Add(name, type);
            }

            return type;
        }

        public MethodDesc GetOrAddMethod(string name, MethodSignature signature)
        {
            MethodDesc method = GetMethod(name, signature);

            if (method == null)
            {
                TypeRefTypeSystemMethod newMethod = new TypeRefTypeSystemMethod(this, name, signature);
                method = newMethod;
                _methods.Add(newMethod);
            }

            return method;
        }

        public FieldDesc GetOrAddField(string name, TypeDesc fieldType, EmbeddedSignatureData[] embeddedSigData)
        {
            FieldDesc fld = GetField(name);
            if (fld == null)
            {
                TypeRefTypeSystemField newField = new TypeRefTypeSystemField(this, name, fieldType, embeddedSigData);
                fld = newField;
                _fields.Add(newField);
            }
            else
            {
                if (fld.FieldType != fieldType)
                    throw new Exception($"Field {fld} has two different field types `{fld.FieldType}` and `{fieldType}`");
            }

            return fld;
        }

        public override PInvokeStringFormat PInvokeStringFormat => throw new NotImplementedException();

        public override string Name => _name;

        public override string Namespace => _namespace;

        public override bool IsExplicitLayout => throw new NotImplementedException();

        public override bool IsSequentialLayout => throw new NotImplementedException();

        public override bool IsBeforeFieldInit => throw new NotImplementedException();

        public override ModuleDesc Module => _module;

        public override DefType BaseType => MetadataBaseType;

        public override MetadataType MetadataBaseType
        {
            get
            {
                if (!_isValueType.HasValue)
                {
                    _isValueType = false;
                }
                return _isValueType.Value ? (MetadataType)Context.GetWellKnownType(WellKnownType.ValueType) : (MetadataType)Context.GetWellKnownType(WellKnownType.Object);
            }
        }

        public override bool IsSealed => throw new NotImplementedException();

        public override bool IsAbstract => throw new NotImplementedException();

        public override DefType ContainingType => _containingType;

        public override DefType[] ExplicitlyImplementedInterfaces => Array.Empty<DefType>();

        public override string DiagnosticName => Name;

        public override string DiagnosticNamespace => Namespace;

        public override TypeSystemContext Context => Module.Context;

        protected override int ClassCode => throw new NotImplementedException();

        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name) => throw new NotImplementedException();
        public override ClassLayoutMetadata GetClassLayout() => throw new NotImplementedException();
        public override int GetHashCode() => (Namespace != null) ? HashCode.Combine(Namespace, Name, Module) : HashCode.Combine(Name, Module);
        public override MetadataType GetNestedType(string name)
        {
            TypeRefTypeSystemType type = null;
            if (_nestedType != null)
            {
                _nestedType.TryGetValue(name, out type);
            }
            return type;
        }
        public override IEnumerable<MetadataType> GetNestedTypes()
        {
            if (_nestedType != null)
            {
                foreach (var type in _nestedType.Values)
                {
                    yield return type;
                }
            }
        }

        public override IEnumerable<MethodDesc> GetMethods() => _methods;
        public override IEnumerable<FieldDesc> GetFields() => _fields;
        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => false;
        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer) => throw new NotImplementedException();
        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                TypeDesc baseType = this.BaseType;

                if (baseType != null && baseType.IsWellKnownType(WellKnownType.ValueType))
                {
                    flags |= TypeFlags.ValueType;
                }
                else
                {
                     flags |= TypeFlags.Class;
                }

                // All other cases are handled during TypeSystemContext initialization
            }

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;
            }

            if ((mask & TypeFlags.HasFinalizerComputed) != 0)
            {
                flags |= TypeFlags.HasFinalizerComputed;

                if (GetFinalizer() != null)
                    flags |= TypeFlags.HasFinalizer;
            }

            if ((mask & TypeFlags.AttributeCacheComputed) != 0)
            {
                flags |= TypeFlags.AttributeCacheComputed;
            }

            return flags;
        }

        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType() => throw new NotImplementedException();
    }
}
