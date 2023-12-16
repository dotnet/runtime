// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    internal sealed class EventBuilderImpl : EventBuilder
    {
        private readonly string _name;
        private EventAttributes _attributes;
        private readonly TypeBuilderImpl _typeBuilder;
        private readonly Type _eventType;

        internal EventDefinitionHandle _handle;
        internal MethodBuilder? _addOnMethod;
        internal MethodBuilder? _raiseMethod;
        internal MethodBuilder? _removeMethod;
        internal HashSet<MethodBuilder>? _otherMethods;
        internal List<CustomAttributeWrapper>? _customAttributes;

        public EventBuilderImpl(string name, EventAttributes attributes, Type eventType, TypeBuilderImpl typeBuilder)
        {
            _name = name;
            _attributes = attributes;
            _typeBuilder = typeBuilder;
            _eventType = eventType;
        }

        internal EventAttributes Attributes => _attributes;
        internal string Name => _name;
        internal Type EventType => _eventType;

        protected override void AddOtherMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _typeBuilder.ThrowIfCreated();

            _otherMethods ??= new HashSet<MethodBuilder>();
            _otherMethods.Add(mdBuilder);
        }

        protected override void SetAddOnMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _typeBuilder.ThrowIfCreated();

            _addOnMethod = mdBuilder;
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            _typeBuilder.ThrowIfCreated();

            if (con.ReflectedType!.FullName == "System.Runtime.CompilerServices.SpecialNameAttribute")
            {
                _attributes |= EventAttributes.SpecialName;
                return;
            }

            _customAttributes ??= new List<CustomAttributeWrapper>();
            _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
        }

        protected override void SetRaiseMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _typeBuilder.ThrowIfCreated();

            _raiseMethod = mdBuilder;
        }

        protected override void SetRemoveOnMethodCore(MethodBuilder mdBuilder)
        {
            ArgumentNullException.ThrowIfNull(mdBuilder);
            _typeBuilder.ThrowIfCreated();

            _removeMethod = mdBuilder;
        }
    }
}
