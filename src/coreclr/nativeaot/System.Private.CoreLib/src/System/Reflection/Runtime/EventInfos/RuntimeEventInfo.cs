// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.EventInfos
{
    //
    // The runtime's implementation of EventInfo's
    //
    [DebuggerDisplay("{_debugName}")]
    internal abstract partial class RuntimeEventInfo : EventInfo
    {
        protected RuntimeEventInfo(RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType)
        {
            ContextTypeInfo = contextTypeInfo;
            ReflectedTypeInfo = reflectedType;
        }

        public sealed override MethodInfo AddMethod
        {
            get
            {
                MethodInfo adder = _lazyAdder;
                if (adder == null)
                {
                    adder = GetEventMethod(EventMethodSemantics.Add);
                    if (adder != null)
                        return _lazyAdder = adder;

                    throw new BadImageFormatException(); // Added is a required method.
                }
                return adder;
            }
        }

        public sealed override Type DeclaringType
        {
            get
            {
                return ContextTypeInfo;
            }
        }

        public sealed override MethodInfo[] GetOtherMethods(bool nonPublic)
        {
            throw new PlatformNotSupportedException();
        }

        public abstract override bool HasSameMetadataDefinitionAs(MemberInfo other);

        public sealed override Module Module
        {
            get
            {
                return DefiningTypeInfo.Module;
            }
        }

        public sealed override string Name
        {
            get
            {
                return MetadataName;
            }
        }

        public sealed override Type ReflectedType
        {
            get
            {
                return ReflectedTypeInfo;
            }
        }

        public sealed override MethodInfo RaiseMethod
        {
            get
            {
                return GetEventMethod(EventMethodSemantics.Fire);
            }
        }

        public sealed override MethodInfo RemoveMethod
        {
            get
            {
                MethodInfo remover = _lazyRemover;
                if (remover == null)
                {
                    remover = GetEventMethod(EventMethodSemantics.Remove);
                    if (remover != null)
                        return _lazyRemover = remover;

                    throw new BadImageFormatException(); // Removed is a required method.
                }
                return remover;
            }
        }

        public sealed override string ToString()
        {
            MethodInfo addMethod = this.AddMethod;
            ParameterInfo[] parameters = addMethod.GetParametersNoCopy();
            if (parameters.Length == 0)
                throw new InvalidOperationException(); // Legacy: Why is a ToString() intentionally throwing an exception?
            RuntimeParameterInfo runtimeParameterInfo = (RuntimeParameterInfo)(parameters[0]);
            return runtimeParameterInfo.ParameterType.FormatTypeNameForReflection() + " " + this.Name;
        }

        protected RuntimeEventInfo WithDebugName()
        {
            bool populateDebugNames = DeveloperExperienceState.DeveloperExperienceModeEnabled;
#if DEBUG
            populateDebugNames = true;
#endif
            if (!populateDebugNames)
                return this;

            if (_debugName == null)
            {
                _debugName = "Constructing..."; // Protect against any inadvertent reentrancy.
                _debugName = MetadataName;
            }
            return this;
        }

        // Types that derive from RuntimeEventInfo must implement the following public surface area members
        public abstract override EventAttributes Attributes { get; }
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
        public abstract override Type EventHandlerType { get; }
        public abstract override int MetadataToken { get; }

        protected enum EventMethodSemantics
        {
            Add,
            Remove,
            Fire
        }

        /// <summary>
        /// Override to return the Method that corresponds to the specified semantic.
        /// Return null if no method is to be found.
        /// </summary>
        protected abstract MethodInfo GetEventMethod(EventMethodSemantics whichMethod);

        /// <summary>
        /// Override to provide the metadata based name of an event. (Different from the Name
        /// property in that it does not go into the reflection trace logic.)
        /// </summary>
        protected abstract string MetadataName { get; }

        /// <summary>
        /// Return the DefiningTypeInfo as a RuntimeTypeInfo (instead of as a format specific type info)
        /// </summary>
        protected abstract RuntimeTypeInfo DefiningTypeInfo { get; }


        protected readonly RuntimeTypeInfo ContextTypeInfo;
        protected readonly RuntimeTypeInfo ReflectedTypeInfo;

        private volatile MethodInfo _lazyAdder;
        private volatile MethodInfo _lazyRemover;

        private string _debugName;
    }
}
