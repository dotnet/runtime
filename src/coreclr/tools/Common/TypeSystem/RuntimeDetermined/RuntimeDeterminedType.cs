// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents a runtime determined type. Runtime determined types are used to represent types
    /// within shared generic method bodies and generic dictionaries. The concrete type will only
    /// be known at runtime (when executing a shared generic method body under a specific generic context).
    /// </summary>
    /// <remarks>
    /// The use of runtime determined types is limited to the dependency analysis and to communicating
    /// with the codegen backend during shared generic code generation. They should not show up within
    /// the system otherwise.
    ///
    /// Runtime determined types behave mostly like the canonical type they are wrapping. Most of the overrides
    /// this type implements will forward the implementation to the <see cref="_rawCanonType"/>'s
    /// implementation.
    ///
    /// Runtime determined types also behave like signature variables in the sense that they allow being
    /// substituted during signature instantiation.
    /// </remarks>
    public sealed partial class RuntimeDeterminedType : DefType
    {
        private DefType _rawCanonType;
        private GenericParameterDesc _runtimeDeterminedDetailsType;

        public RuntimeDeterminedType(DefType rawCanonType, GenericParameterDesc runtimeDeterminedDetailsType)
        {
            _rawCanonType = rawCanonType;
            _runtimeDeterminedDetailsType = runtimeDeterminedDetailsType;
        }

        /// <summary>
        /// Gets the generic parameter this runtime determined type represents.
        /// </summary>
        public GenericParameterDesc RuntimeDeterminedDetailsType
        {
            get
            {
                return _runtimeDeterminedDetailsType;
            }
        }

        /// <summary>
        /// Gets the canonical type wrapped by this runtime determined type.
        /// </summary>
        public DefType CanonicalType
        {
            get
            {
                return _rawCanonType;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _rawCanonType.Context;
            }
        }

        public override bool IsRuntimeDeterminedSubtype
        {
            get
            {
                return true;
            }
        }

        public override DefType BaseType
        {
            get
            {
                return _rawCanonType.BaseType;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _rawCanonType.Instantiation;
            }
        }

        public override string Name
        {
            get
            {
                return _rawCanonType.Name;
            }
        }

        public override string Namespace
        {
            get
            {
                return string.Concat(_runtimeDeterminedDetailsType.Name, "_", _rawCanonType.Namespace);
            }
        }

        public override IEnumerable<MethodDesc> GetMethods()
        {
            foreach (var method in _rawCanonType.GetMethods())
            {
                yield return Context.GetMethodForRuntimeDeterminedType(method.GetTypicalMethodDefinition(), this);
            }
        }

        public override IEnumerable<MethodDesc> GetVirtualMethods()
        {
            foreach (var method in _rawCanonType.GetVirtualMethods())
            {
                yield return Context.GetMethodForRuntimeDeterminedType(method.GetTypicalMethodDefinition(), this);
            }
        }

        public override MethodDesc GetMethod(string name, MethodSignature signature, Instantiation substitution)
        {
            MethodDesc method = _rawCanonType.GetMethod(name, signature, substitution);
            if (method == null)
                return null;
            return Context.GetMethodForRuntimeDeterminedType(method.GetTypicalMethodDefinition(), this);
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= _rawCanonType.GetTypeFlags(mask);
            }

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= _rawCanonType.GetTypeFlags(mask);
            }

            if ((mask & TypeFlags.AttributeCacheComputed) != 0)
            {
                flags |= _rawCanonType.GetTypeFlags(mask);
            }

            // Might need to define the behavior if we ever hit this.
            Debug.Assert((flags & mask) != 0);
            return flags;
        }

        public override TypeDesc GetTypeDefinition()
        {
            // TODO: this is needed because NameMangler calls it to see if we're dealing with genericness. Revise?
            if (_rawCanonType.HasInstantiation)
            {
                return Context.GetRuntimeDeterminedType((DefType)_rawCanonType.GetTypeDefinition(), _runtimeDeterminedDetailsType);
            }

            return this;
        }

        public override int GetHashCode()
        {
            return _rawCanonType.GetHashCode();
        }

        protected override TypeDesc ConvertToCanonFormImpl(CanonicalFormKind kind)
        {
            return _rawCanonType.ConvertToCanonForm(kind);
        }

        public override bool IsCanonicalSubtype(CanonicalFormKind policy)
        {
            return false;
        }

        public override TypeDesc GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            if (_runtimeDeterminedDetailsType.Kind == GenericParameterKind.Type)
            {
                return typeInstantiation[_runtimeDeterminedDetailsType.Index];
            }
            else
            {
                Debug.Assert(_runtimeDeterminedDetailsType.Kind == GenericParameterKind.Method);
                return methodInstantiation[_runtimeDeterminedDetailsType.Index];
            }
        }
    }
}
