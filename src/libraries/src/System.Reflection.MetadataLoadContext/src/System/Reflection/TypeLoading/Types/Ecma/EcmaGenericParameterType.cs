// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// RoTypes that return true for IsGenericParameter and get its metadata from a PEReader.
    /// </summary>
    internal abstract class EcmaGenericParameterType : RoGenericParameterType
    {
        private readonly EcmaModule _ecmaModule;

        internal EcmaGenericParameterType(GenericParameterHandle handle, EcmaModule module)
            : base()
        {
            Debug.Assert(!handle.IsNil);

            Handle = handle;
            _ecmaModule = module;
            _neverAccessThisExceptThroughGenericParameterProperty = handle.GetGenericParameter(Reader);
        }

        internal sealed override RoModule GetRoModule() => _ecmaModule;

        protected sealed override int ComputePosition() => GenericParameter.Index;
        protected sealed override string ComputeName() => GenericParameter.Name.GetString(Reader);
        public sealed override GenericParameterAttributes GenericParameterAttributes => GenericParameter.Attributes;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => GenericParameter.GetCustomAttributes().ToTrueCustomAttributes(GetEcmaModule());
        internal sealed override bool IsCustomAttributeDefined(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => GenericParameter.GetCustomAttributes().IsCustomAttributeDefined(ns, name, GetEcmaModule());
        internal sealed override CustomAttributeData TryFindCustomAttribute(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name) => GenericParameter.GetCustomAttributes().TryFindCustomAttribute(ns, name, GetEcmaModule());

        public sealed override int MetadataToken => Handle.GetToken();

        protected sealed override RoType[] ComputeGenericParameterConstraints()
        {
            MetadataReader reader = Reader;
            GenericParameterConstraintHandleCollection handles = GenericParameter.GetConstraints();
            int count = handles.Count;
            if (count == 0)
                return Array.Empty<RoType>();

            TypeContext typeContext = TypeContext;
            RoType[] constraints = new RoType[count];
            int index = 0;
            foreach (GenericParameterConstraintHandle h in handles)
            {
                RoType constraint = h.GetGenericParameterConstraint(reader).Type.ResolveTypeDefRefOrSpec(GetEcmaModule(), typeContext);
                constraints[index++] = constraint;
            }
            return constraints;
        }

        protected abstract override RoType ComputeDeclaringType();
        public abstract override MethodBase DeclaringMethod { get; }

        internal GenericParameterHandle Handle { get; }
        internal EcmaModule GetEcmaModule() => _ecmaModule;
        internal MetadataReader Reader => GetEcmaModule().Reader;
        protected abstract TypeContext TypeContext { get; }

        protected ref readonly GenericParameter GenericParameter { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughGenericParameterProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly GenericParameter _neverAccessThisExceptThroughGenericParameterProperty;
    }
}
