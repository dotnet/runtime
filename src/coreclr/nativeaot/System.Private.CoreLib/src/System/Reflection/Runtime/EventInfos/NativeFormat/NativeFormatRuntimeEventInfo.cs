// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.CustomAttributes;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.MethodInfos.NativeFormat;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeInfos.NativeFormat;
using System.Runtime.CompilerServices;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core.Execution;

using NativeFormatMethodSemanticsAttributes = global::Internal.Metadata.NativeFormat.MethodSemanticsAttributes;

namespace System.Reflection.Runtime.EventInfos.NativeFormat
{
    [DebuggerDisplay("{_debugName}")]
    internal sealed partial class NativeFormatRuntimeEventInfo : RuntimeEventInfo
    {
        //
        // eventHandle    - the "tkEventDef" that identifies the event.
        // definingType   - the "tkTypeDef" that defined the field (this is where you get the metadata reader that created eventHandle.)
        // contextType    - the type that supplies the type context (i.e. substitutions for generic parameters.) Though you
        //                  get your raw information from "definingType", you report "contextType" as your DeclaringType property.
        //
        //  For example:
        //
        //       typeof(Foo<>).GetTypeInfo().DeclaredMembers
        //
        //           The definingType and contextType are both Foo<>
        //
        //       typeof(Foo<int,String>).GetTypeInfo().DeclaredMembers
        //
        //          The definingType is "Foo<,>"
        //          The contextType is "Foo<int,String>"
        //
        //  We don't report any DeclaredMembers for arrays or generic parameters so those don't apply.
        //
        private NativeFormatRuntimeEventInfo(EventHandle eventHandle, NativeFormatRuntimeNamedTypeInfo definingTypeInfo, RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType) :
            base(contextTypeInfo, reflectedType)
        {
            _eventHandle = eventHandle;
            _definingTypeInfo = definingTypeInfo;
            _reader = definingTypeInfo.Reader;
            _event = eventHandle.GetEvent(_reader);
        }

        protected sealed override MethodInfo GetEventMethod(EventMethodSemantics whichMethod)
        {
            NativeFormatMethodSemanticsAttributes localMethodSemantics;
            switch (whichMethod)
            {
                case EventMethodSemantics.Add:
                    localMethodSemantics = NativeFormatMethodSemanticsAttributes.AddOn;
                    break;

                case EventMethodSemantics.Fire:
                    localMethodSemantics = NativeFormatMethodSemanticsAttributes.Fire;
                    break;

                case EventMethodSemantics.Remove:
                    localMethodSemantics = NativeFormatMethodSemanticsAttributes.RemoveOn;
                    break;

                default:
                    return null;
            }

            foreach (MethodSemanticsHandle methodSemanticsHandle in _event.MethodSemantics)
            {
                MethodSemantics methodSemantics = methodSemanticsHandle.GetMethodSemantics(_reader);
                if (methodSemantics.Attributes == localMethodSemantics)
                {
                    return RuntimeNamedMethodInfo<NativeFormatMethodCommon>.GetRuntimeNamedMethodInfo(new NativeFormatMethodCommon(methodSemantics.Method, _definingTypeInfo, ContextTypeInfo), ReflectedTypeInfo);
                }
            }

            return null;
        }

        public sealed override EventAttributes Attributes
        {
            get
            {
                return _event.Flags;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return RuntimeCustomAttributeData.GetCustomAttributes(_reader, _event.CustomAttributes);
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (!(other is NativeFormatRuntimeEventInfo otherEvent))
                return false;
            if (!(_reader == otherEvent._reader))
                return false;
            if (!(_eventHandle.Equals(otherEvent._eventHandle)))
                return false;
            if (!(_definingTypeInfo.Equals(otherEvent._definingTypeInfo)))
                return false;
            return true;
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is NativeFormatRuntimeEventInfo other))
                return false;
            if (!(_reader == other._reader))
                return false;
            if (!(_eventHandle.Equals(other._eventHandle)))
                return false;
            if (!(ContextTypeInfo.Equals(other.ContextTypeInfo)))
                return false;
            if (!(ReflectedType.Equals(other.ReflectedType)))
                return false;
            return true;
        }

        public sealed override int GetHashCode()
        {
            return _eventHandle.GetHashCode();
        }

        public sealed override Type EventHandlerType
        {
            get
            {
                return _event.Type.Resolve(_reader, ContextTypeInfo.TypeContext).ToType();
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        protected sealed override string MetadataName
        {
            get
            {
                return _event.Name.GetString(_reader);
            }
        }

        protected sealed override RuntimeTypeInfo DefiningTypeInfo
        {
            get
            {
                return _definingTypeInfo;
            }
        }

        private readonly NativeFormatRuntimeNamedTypeInfo _definingTypeInfo;
        private readonly EventHandle _eventHandle;

        private readonly MetadataReader _reader;
        private readonly Event _event;
    }
}
