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
    public sealed class TypeRefTypeSystemGenericParameter : GenericParameterDesc
    {
        private TypeRefTypeSystemType _owningType;
        private TypeRefTypeSystemMethod _owningMethod;
        private int _index;

        internal TypeRefTypeSystemGenericParameter(TypeRefTypeSystemType owningType, int index)
        {
            _owningType = owningType;
            _index = index;
        }

        internal TypeRefTypeSystemGenericParameter(TypeRefTypeSystemMethod owningMethod, int index)
        {
            _owningMethod = owningMethod;
            _index = index;
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
                return Kind == GenericParameterKind.Method ? $"!!{_index}" : $"!{_index}";
            }
        }

        public override GenericParameterKind Kind
        {
            get
            {
                if (_owningMethod != null)
                {
                    return GenericParameterKind.Method;
                }
                else
                {
                    return GenericParameterKind.Type;
                }
            }
        }

        public override int Index
        {
            get
            {
                return _index;
            }
        }

        public override GenericVariance Variance
        {
            get
            {
                return GenericVariance.None;
            }
        }

        public override GenericConstraints Constraints
        {
            get
            {
                return GenericConstraints.None;
            }
        }

        public override IEnumerable<TypeDesc> TypeConstraints
        {
            get
            {
                return Array.Empty<TypeDesc>();
            }
        }

        public override string DiagnosticName => Name;

        protected override int ClassCode => throw new NotImplementedException();

        protected override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer) => throw new NotImplementedException();
    }
}
