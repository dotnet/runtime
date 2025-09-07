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
    class TypeRefTypeSystemMethod : MethodDesc
    {
        TypeRefTypeSystemType _type;
        byte[] _name;
        MethodSignature _signature;
        Instantiation _instantiation;

        public TypeRefTypeSystemMethod(TypeRefTypeSystemType type, ReadOnlySpan<byte> name, MethodSignature signature)
        {
            _type = type;
            _name = name.ToArray();
            _signature = signature;
            if (signature.GenericParameterCount == 0)
            {
                _instantiation = Instantiation.Empty;
            }
            else
            {
                TypeDesc[] instantiationArgs = new TypeDesc[signature.GenericParameterCount];
                for (int i = 0; i < signature.GenericParameterCount; i++)
                {
                    instantiationArgs[i] = new TypeRefTypeSystemGenericParameter(this, i);
                }
                _instantiation = new Instantiation(instantiationArgs);
            }
        }

        public override string Name => System.Text.Encoding.UTF8.GetString(U8Name);

        public override ReadOnlySpan<byte> U8Name => _name;

        public override Instantiation Instantiation => _instantiation;

        public override TypeDesc OwningType => _type;

        public override MethodSignature Signature => _signature;

        public override string DiagnosticName => GetName();

        public override TypeSystemContext Context => OwningType.Context;

        protected override int ClassCode => throw new NotImplementedException();

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => throw new NotImplementedException();
        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) => throw new NotImplementedException();
    }
}
