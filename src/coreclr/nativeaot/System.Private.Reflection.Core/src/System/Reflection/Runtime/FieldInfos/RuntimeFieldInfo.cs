// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.CustomAttributes;
using System.Reflection.Runtime.BindingFlagSupport;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

using Internal.Reflection.Tracing;

namespace System.Reflection.Runtime.FieldInfos
{
    //
    // The Runtime's implementation of fields.
    //
    [DebuggerDisplay("{_debugName}")]
    internal abstract partial class RuntimeFieldInfo : FieldInfo, ITraceableTypeMember
    {
        //
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
        protected RuntimeFieldInfo(RuntimeTypeInfo contextTypeInfo, RuntimeTypeInfo reflectedType)
        {
            _contextTypeInfo = contextTypeInfo;
            _reflectedType = reflectedType;
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
#if ENABLE_REFLECTION_TRACE
                if (ReflectionTrace.Enabled)
                    ReflectionTrace.FieldInfo_CustomAttributes(this);
#endif

                foreach (CustomAttributeData cad in TrueCustomAttributes)
                    yield return cad;

                if (DeclaringType.IsExplicitLayout)
                {
                    int offset = ExplicitLayoutFieldOffsetData;
                    CustomAttributeTypedArgument offsetArgument = new CustomAttributeTypedArgument(typeof(int), offset);
                    yield return new RuntimePseudoCustomAttributeData(typeof(FieldOffsetAttribute), new CustomAttributeTypedArgument[] { offsetArgument });
                }

                FieldAttributes attributes = Attributes;
                if (0 != (attributes & FieldAttributes.NotSerialized))
                {
                    yield return new RuntimePseudoCustomAttributeData(typeof(NonSerializedAttribute), null);
                }
            }
        }

        public sealed override Type DeclaringType
        {
            get
            {
#if ENABLE_REFLECTION_TRACE
                if (ReflectionTrace.Enabled)
                    ReflectionTrace.FieldInfo_DeclaringType(this);
#endif

                return _contextTypeInfo;
            }
        }

        public sealed override Type FieldType
        {
            get
            {
                Type fieldType = _lazyFieldType;
                if (fieldType == null)
                {
                    _lazyFieldType = fieldType = this.FieldRuntimeType;
                }

                return fieldType;
            }
        }

        public abstract override Type[] GetOptionalCustomModifiers();

        public abstract override Type[] GetRequiredCustomModifiers();

        public sealed override object GetValue(object obj)
        {
#if ENABLE_REFLECTION_TRACE
            if (ReflectionTrace.Enabled)
                ReflectionTrace.FieldInfo_GetValue(this, obj);
#endif

            FieldAccessor fieldAccessor = this.FieldAccessor;
            return fieldAccessor.GetField(obj);
        }

        public sealed override object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            FieldAccessor fieldAccessor = this.FieldAccessor;
            return fieldAccessor.GetFieldDirect(obj);
        }

        public abstract override bool HasSameMetadataDefinitionAs(MemberInfo other);

        public sealed override Module Module
        {
            get
            {
                return DefiningType.Module;
            }
        }

        public sealed override Type ReflectedType
        {
            get
            {
                return _reflectedType;
            }
        }

        public sealed override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
#if ENABLE_REFLECTION_TRACE
            if (ReflectionTrace.Enabled)
                ReflectionTrace.FieldInfo_SetValue(this, obj, value);
#endif

            FieldAccessor fieldAccessor = this.FieldAccessor;
            BinderBundle binderBundle = binder.ToBinderBundle(invokeAttr, culture);
            fieldAccessor.SetField(obj, value, binderBundle);
        }

        public sealed override void SetValueDirect(TypedReference obj, object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            FieldAccessor fieldAccessor = this.FieldAccessor;
            fieldAccessor.SetFieldDirect(obj, value);
        }

        Type ITraceableTypeMember.ContainingType
        {
            get
            {
                return _contextTypeInfo;
            }
        }

        /// <summary>
        /// Override to provide the metadata based name of a field. (Different from the Name
        /// property in that it does not go into the reflection trace logic.)
        /// </summary>
        protected abstract string MetadataName { get; }

        public sealed override string Name
        {
            get
            {
#if ENABLE_REFLECTION_TRACE
                if (ReflectionTrace.Enabled)
                    ReflectionTrace.FieldInfo_Name(this);
#endif

                return MetadataName;
            }
        }

        string ITraceableTypeMember.MemberName
        {
            get
            {
                return MetadataName;
            }
        }

        public sealed override object GetRawConstantValue()
        {
            if (!IsLiteral)
                throw new InvalidOperationException();

            object defaultValue;
            if (!GetDefaultValueIfAvailable(raw: true, defaultValue: out defaultValue))
                throw new BadImageFormatException(); // Field marked literal but has no default value.

            return defaultValue;
        }

        // Types that derive from RuntimeFieldInfo must implement the following public surface area members
        public abstract override FieldAttributes Attributes { get; }
        public abstract override int MetadataToken { get; }
        public abstract override string ToString();
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
        public abstract override RuntimeFieldHandle FieldHandle { get; }

        /// <summary>
        /// Get the default value if exists for a field by parsing metadata. Return false if there is no default value.
        /// </summary>
        protected abstract bool GetDefaultValueIfAvailable(bool raw, out object defaultValue);

        /// <summary>
        /// Return a FieldAccessor object for accessing the value of a non-literal field. May rely on metadata to create correct accessor.
        /// </summary>
        protected abstract FieldAccessor TryGetFieldAccessor();

        private FieldAccessor FieldAccessor
        {
            get
            {
                FieldAccessor fieldAccessor = _lazyFieldAccessor;
                if (fieldAccessor == null)
                {
                    if (this.IsLiteral)
                    {
                        // Legacy: ECMA335 does not require that the metadata literal match the type of the field that declares it.
                        // For desktop compat, we return the metadata literal as is and do not attempt to convert or validate against the Field type.

                        object defaultValue;
                        if (!GetDefaultValueIfAvailable(raw: false, defaultValue: out defaultValue))
                        {
                            throw new BadImageFormatException(); // Field marked literal but has no default value.
                        }

                        _lazyFieldAccessor = fieldAccessor = ReflectionCoreExecution.ExecutionEnvironment.CreateLiteralFieldAccessor(defaultValue, FieldType.TypeHandle);
                    }
                    else
                    {
                        _lazyFieldAccessor = fieldAccessor = TryGetFieldAccessor();
                        if (fieldAccessor == null)
                            throw ReflectionCoreExecution.ExecutionDomain.CreateNonInvokabilityException(this);
                    }
                }
                return fieldAccessor;
            }
        }

        /// <summary>
        /// Return the type of the field by parsing metadata.
        /// </summary>
        protected abstract RuntimeTypeInfo FieldRuntimeType { get; }

        protected RuntimeFieldInfo WithDebugName()
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
                _debugName = ((ITraceableTypeMember)this).MemberName;
            }
            return this;
        }

        /// <summary>
        /// Return the DefiningTypeInfo as a RuntimeTypeInfo (instead of as a format specific type info)
        /// </summary>
        protected abstract RuntimeTypeInfo DefiningType { get; }

        protected abstract IEnumerable<CustomAttributeData> TrueCustomAttributes { get; }
        protected abstract int ExplicitLayoutFieldOffsetData { get; }

        /// <summary>
        /// Returns the field offset (asserts and throws if not an instance field). Does not include the size of the object header.
        /// </summary>
        internal int Offset => FieldAccessor.Offset;

        protected readonly RuntimeTypeInfo _contextTypeInfo;
        protected readonly RuntimeTypeInfo _reflectedType;

        private volatile FieldAccessor _lazyFieldAccessor;

        private volatile Type _lazyFieldType;

        private string _debugName;
    }
}
