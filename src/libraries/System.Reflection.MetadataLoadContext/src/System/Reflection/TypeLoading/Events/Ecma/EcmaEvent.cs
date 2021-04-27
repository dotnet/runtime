// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all EventInfo objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed class EcmaEvent : RoEvent
    {
        private readonly EcmaModule _module;
        private readonly EventDefinitionHandle _handle;

        internal EcmaEvent(RoInstantiationProviderType declaringType, EventDefinitionHandle handle, Type reflectedType)
            : base(declaringType, reflectedType)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(declaringType != null);
            Debug.Assert(reflectedType != null);
            Debug.Assert(declaringType.Module is EcmaModule);

            _handle = handle;
            _module = (EcmaModule)(declaringType.Module);
            _neverAccessThisExceptThroughEventDefinitionProperty = handle.GetEventDefinition(Reader);
        }

        internal sealed override RoModule GetRoModule() => _module;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => EventDefinition.GetCustomAttributes().ToTrueCustomAttributes(_module);

        public sealed override int MetadataToken => _handle.GetToken();

        public sealed override bool Equals(object? obj)
        {
            if (!(obj is EcmaEvent other))
                return false;

            if (_handle != other._handle)
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            if (ReflectedType != other.ReflectedType)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => _handle.GetHashCode() ^ DeclaringType.GetHashCode();

        protected sealed override string ComputeName() => EventDefinition.Name.GetString(Reader);
        protected sealed override EventAttributes ComputeAttributes() => EventDefinition.Attributes;
        protected sealed override Type ComputeEventHandlerType() => EventDefinition.Type.ResolveTypeDefRefOrSpec(_module, TypeContext);

        public sealed override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            MetadataReader reader = Reader;
            ImmutableArray<MethodDefinitionHandle> others = EventDefinition.GetAccessors().Others;
            int count = others.Length;
            List<MethodInfo> results = new List<MethodInfo>(capacity: count);
            for (int i = 0; i < count; i++)
            {
                MethodDefinition def = others[i].GetMethodDefinition(reader);
                if (nonPublic || (def.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
                {
                    MethodInfo methodInfo = others[i].ToMethod(GetRoDeclaringType(), GetRoDeclaringType());
                    results.Add(methodInfo);
                }
            }
            return results.ToArray();
        }

        public sealed override string ToString()
        {
            string? disposedString = Loader.GetDisposedString();
            if (disposedString != null)
                return disposedString;

            return
                EventDefinition.Type.ToTypeString(TypeContext, Reader) +
                " " +
                Name;
        }

        protected sealed override RoMethod? ComputeEventAddMethod() => EventDefinition.GetAccessors().Adder.ToMethodOrNull(GetRoDeclaringType(), ReflectedType);
        protected sealed override RoMethod? ComputeEventRemoveMethod() => EventDefinition.GetAccessors().Remover.ToMethodOrNull(GetRoDeclaringType(), ReflectedType);
        protected sealed override RoMethod? ComputeEventRaiseMethod() => EventDefinition.GetAccessors().Raiser.ToMethodOrNull(GetRoDeclaringType(), ReflectedType);

        private MetadataReader Reader => _module.Reader;
        private MetadataLoadContext Loader => GetRoModule().Loader;

        private ref readonly EventDefinition EventDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughEventDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly EventDefinition _neverAccessThisExceptThroughEventDefinitionProperty;
    }
}
