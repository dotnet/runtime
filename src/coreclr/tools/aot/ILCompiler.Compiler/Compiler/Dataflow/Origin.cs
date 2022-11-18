// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.Dataflow
{
    internal abstract class Origin
    {
    }

    internal sealed class MethodReturnOrigin : Origin
    {
        public MethodDesc Method { get; }

        public MethodReturnOrigin(MethodDesc method)
        {
            Method = method;
        }

        public string GetDisplayName() => Method.GetDisplayName();
    }

    internal sealed class ParameterOrigin : Origin
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

    internal sealed class MethodOrigin : Origin
    {
        public MethodDesc Method { get; }

        public MethodOrigin(MethodDesc method)
        {
            Method = method;
        }

        public string GetDisplayName() => Method.GetDisplayName();
    }

    internal sealed class FieldOrigin : Origin
    {
        public FieldDesc Field { get; }

        public FieldOrigin(FieldDesc field)
        {
            Field = field;
        }

        public string GetDisplayName() => Field.GetDisplayName();
    }

    internal sealed class TypeOrigin : Origin
    {
        public MetadataType Type { get; }

        public TypeOrigin(MetadataType type)
        {
            Type = type;
        }

        public string GetDisplayName() => Type.GetDisplayName();
    }

    internal sealed class GenericParameterOrigin : Origin
    {
        public GenericParameterDesc GenericParameter { get; }

        public string Name => GenericParameter.Name;

        public GenericParameterOrigin(GenericParameterDesc genericParam)
        {
            GenericParameter = genericParam;
        }
    }
}
