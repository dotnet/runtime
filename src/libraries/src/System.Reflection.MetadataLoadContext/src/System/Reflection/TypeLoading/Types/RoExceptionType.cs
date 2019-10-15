// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// This class exists only to stash Exceptions inside GetTypeCore caches.
    /// </summary>
    internal sealed class RoExceptionType : RoDefinitionType
    {
        private readonly byte[] _ns;
        private readonly byte[] _name;
        internal Exception Exception { get; }

        internal RoExceptionType(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, Exception exception)
            : base()
        {
            _ns = ns.ToArray();
            _name = name.ToArray();
            Exception = exception;
        }

        internal sealed override bool IsTypeNameEqual(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => name.SequenceEqual(_name) && ns.SequenceEqual(_ns);

        public sealed override bool IsGenericTypeDefinition => throw null;
        public sealed override int MetadataToken => throw null;
        internal sealed override RoModule GetRoModule() => throw null;
        protected sealed override string ComputeName() => throw null;
        protected sealed override string ComputeNamespace() => throw null;
        protected sealed override TypeAttributes ComputeAttributeFlags() => throw null;
        protected sealed override RoType ComputeDeclaringType() => throw null;
        internal sealed override int GetGenericParameterCount() => throw null;
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => throw null;
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => throw null;
        internal sealed override CustomAttributeData TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => throw null;
        protected sealed override IEnumerable<CustomAttributeData> GetTrueCustomAttributes() => throw null;
        protected sealed override void GetPackSizeAndSize(out int packSize, out int size) => throw null;
        protected internal sealed override RoType ComputeEnumUnderlyingType() => throw null;
        internal sealed override RoType SpecializeBaseType(RoType[] instantiation) => throw null;
        internal sealed override IEnumerable<RoType> SpecializeInterfaces(RoType[] instantiation) => throw null;
        internal sealed override IEnumerable<RoType> GetNestedTypesCore(NameFilter filter) => throw null;
        internal sealed override RoDefinitionType GetNestedTypeCore(ReadOnlySpan<byte> utf8Name) => throw null;
        internal sealed override IEnumerable<ConstructorInfo> SpecializeConstructors(NameFilter filter, RoInstantiationProviderType declaringType) => throw null;
        internal sealed override IEnumerable<MethodInfo> SpecializeMethods(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType) => throw null;
        internal sealed override IEnumerable<EventInfo> SpecializeEvents(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType) => throw null;
        internal sealed override IEnumerable<FieldInfo> SpecializeFields(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType) => throw null;
        internal sealed override IEnumerable<PropertyInfo> SpecializeProperties(NameFilter filter, Type reflectedType, RoInstantiationProviderType declaringType) => throw null;
    }
}
