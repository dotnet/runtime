// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Dataflow
{
    internal abstract class Origin
    {
    }

    class MethodReturnOrigin : Origin
    {
        public MethodDesc Method { get; }

        public MethodReturnOrigin(MethodDesc method)
        {
            Method = method;
        }

        public string GetDisplayName() => Method.GetDisplayName();
    }

    class ParameterOrigin : Origin
    {
        public MethodDesc Method { get; }
        public int Index { get; }

        public ParameterOrigin(MethodDesc method, int index)
        {
            Method = method;
            Index = index;
        }

        public string GetDisplayName() => Method.GetDisplayName();
    }

    class MethodOrigin : Origin
    {
        public MethodDesc Method { get; }

        public MethodOrigin(MethodDesc method)
        {
            Method = method;
        }

        public string GetDisplayName() => Method.GetDisplayName();
    }

    class FieldOrigin : Origin
    {
        public FieldDesc Field { get; }

        public FieldOrigin(FieldDesc field)
        {
            Field = field;
        }

        public string GetDisplayName() => Field.GetDisplayName();
    }

    class TypeOrigin : Origin
    {
        public MetadataType Type { get; }

        public TypeOrigin(MetadataType type)
        {
            Type = type;
        }

        public string GetDisplayName() => Type.GetDisplayName();
    }

    class GenericParameterOrigin : Origin
    {
        public GenericParameterDesc GenericParameter { get; }

        public string Name => GenericParameter.Name;

        public GenericParameterOrigin(GenericParameterDesc genericParam)
        {
            GenericParameter = genericParam;
        }
    }
}
