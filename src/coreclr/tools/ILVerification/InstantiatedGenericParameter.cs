// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    internal sealed partial class InstantiatedGenericParameter : GenericParameterDesc
    {
        private readonly GenericParameterDesc _genericParam;
        private Instantiation _typeInstantiation;
        private Instantiation _methodInstantiation;

        public GenericParameterDesc GenericParameter
        {
            get
            {
                return _genericParam;
            }
        }

        internal static Instantiation CreateGenericTypeInstantiaton(Instantiation instantiation)
        {
            if (instantiation.Length == 0)
                return instantiation;

            var genericInstantiation = CreateGenericInstantiation(instantiation);

            foreach (var parameter in genericInstantiation)
                ((InstantiatedGenericParameter)parameter)._typeInstantiation = genericInstantiation;

            return genericInstantiation;
        }

        internal static Instantiation CreateGenericMethodInstantiation(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            if (methodInstantiation.Length == 0)
                return methodInstantiation;

            var genericInstantiation = CreateGenericInstantiation(methodInstantiation);

            foreach (var parameter in genericInstantiation)
            {
                var par = (InstantiatedGenericParameter)parameter;
                par._typeInstantiation = typeInstantiation;
                par._methodInstantiation = genericInstantiation;
            }

            return genericInstantiation;
        }

        private static Instantiation CreateGenericInstantiation(Instantiation fromInstantiation)
        {
            var parameters = new TypeDesc[fromInstantiation.Length];
            for (int i = 0; i < fromInstantiation.Length; ++i)
                parameters[i] = new InstantiatedGenericParameter((GenericParameterDesc)fromInstantiation[i]);

            return new Instantiation(parameters);
        }

        private InstantiatedGenericParameter(GenericParameterDesc genericParam)
        {
            Debug.Assert(!(genericParam is InstantiatedGenericParameter));
            _genericParam = genericParam;
        }

        public override GenericParameterKind Kind => _genericParam.Kind;

        public override int Index => _genericParam.Index;

        public override TypeSystemContext Context => _genericParam.Context;

        public override GenericVariance Variance => _genericParam.Variance;

        public override GenericConstraints Constraints => _genericParam.Constraints;

        public override IEnumerable<TypeDesc> TypeConstraints
        {
            get
            {
                foreach (var constraint in _genericParam.TypeConstraints)
                {
                    yield return constraint.InstantiateSignature(_typeInstantiation, _methodInstantiation);
                }
            }
        }

        public override string ToString()
        {
            return _genericParam.ToString();
        }
    }
}
