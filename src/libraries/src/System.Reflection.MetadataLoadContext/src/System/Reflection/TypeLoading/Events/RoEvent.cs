// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all EventInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoEvent : LeveledEventInfo
    {
        private readonly RoInstantiationProviderType _declaringType;
        private readonly Type _reflectedType;

        protected RoEvent(RoInstantiationProviderType declaringType, Type reflectedType)
        {
            Debug.Assert(declaringType != null);
            Debug.Assert(reflectedType != null);

            _declaringType = declaringType;
            _reflectedType = reflectedType;
        }

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
        public abstract override string ToString();

        public sealed override Type DeclaringType => GetRoDeclaringType();
        internal RoInstantiationProviderType GetRoDeclaringType() => _declaringType;

        public sealed override Type ReflectedType => _reflectedType;

        public sealed override string Name => _lazyName ?? (_lazyName = ComputeName());
        protected abstract string ComputeName();
        private volatile string _lazyName;

        public sealed override Module Module => GetRoModule();
        internal abstract RoModule GetRoModule();

        public abstract override int MetadataToken { get; }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => this.HasSameMetadataDefinitionAsCore(other);

        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }

        public sealed override EventAttributes Attributes => (_lazyEventAttributes == EventAttributesSentinel) ? (_lazyEventAttributes = ComputeAttributes()) : _lazyEventAttributes;
        protected abstract EventAttributes ComputeAttributes();
        private const EventAttributes EventAttributesSentinel = (EventAttributes)(-1);
        private volatile EventAttributes _lazyEventAttributes = EventAttributesSentinel;

        public sealed override Type EventHandlerType => _lazyEventType ?? (_lazyEventType = ComputeEventHandlerType());
        protected abstract Type ComputeEventHandlerType();
        private volatile Type _lazyEventType;

        private MethodInfo GetRoAddMethod() => (_lazyAdder == Sentinels.RoMethod) ? (_lazyAdder = ComputeEventAddMethod()?.FilterInheritedAccessor()) : _lazyAdder;
        private MethodInfo GetRoRemoveMethod() => (_lazyRemover == Sentinels.RoMethod) ? (_lazyRemover = ComputeEventRemoveMethod()?.FilterInheritedAccessor()) : _lazyRemover;
        private MethodInfo GetRoRaiseMethod() => (_lazyRaiser == Sentinels.RoMethod) ? (_lazyRaiser = ComputeEventRaiseMethod()?.FilterInheritedAccessor()) : _lazyRaiser;

        public sealed override MethodInfo GetAddMethod(bool nonPublic) => GetRoAddMethod()?.FilterAccessor(nonPublic);
        public sealed override MethodInfo GetRemoveMethod(bool nonPublic) => GetRoRemoveMethod()?.FilterAccessor(nonPublic);
        public sealed override MethodInfo GetRaiseMethod(bool nonPublic) => GetRoRaiseMethod()?.FilterAccessor(nonPublic);

        protected abstract RoMethod ComputeEventAddMethod();
        protected abstract RoMethod ComputeEventRemoveMethod();
        protected abstract RoMethod ComputeEventRaiseMethod();

        private volatile RoMethod _lazyAdder = Sentinels.RoMethod;
        private volatile RoMethod _lazyRemover = Sentinels.RoMethod;
        private volatile RoMethod _lazyRaiser = Sentinels.RoMethod;

        public abstract override MethodInfo[] GetOtherMethods(bool nonPublic);

        public sealed override bool IsMulticast => Loader.GetCoreType(CoreType.MulticastDelegate).IsAssignableFrom(EventHandlerType);

        // Operations that are not allowed for Reflection-only.
        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override void AddEventHandler(object target, Delegate handler) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override void RemoveEventHandler(object target, Delegate handler) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        private MetadataLoadContext Loader => GetRoModule().Loader;
        internal TypeContext TypeContext => _declaringType.Instantiation.ToTypeContext();
    }
}
