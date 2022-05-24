// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;
using System.Runtime.InteropServices;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // This represents the synthetic nullary instance constructor for Types created by Type.GetTypeFromCLSID().
    //
    internal sealed partial class RuntimeCLSIDNullaryConstructorInfo : RuntimeConstructorInfo
    {
        private RuntimeCLSIDNullaryConstructorInfo(RuntimeCLSIDTypeInfo declaringType)
        {
            _declaringType = declaringType;
        }

        public sealed override MethodAttributes Attributes => MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        public sealed override CallingConventions CallingConvention => CallingConventions.Standard | CallingConventions.HasThis;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Empty<CustomAttributeData>.Enumerable;
        public sealed override Type DeclaringType => _declaringType;

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // This logic is written to match CoreCLR's behavior.
            return other is RuntimeCLSIDNullaryConstructorInfo;
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RuntimeCLSIDNullaryConstructorInfo other))
                return false;
            if (!(_declaringType.Equals(other._declaringType)))
                return false;
            return true;
        }

        public sealed override int GetHashCode() => _declaringType.GetHashCode();

        public sealed override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new PlatformNotSupportedException();
        }

        public sealed override MethodBase MetadataDefinitionMethod { get { throw new NotSupportedException(); } }

        public sealed override int MetadataToken { get { throw new InvalidOperationException(); } }

        public sealed override RuntimeMethodHandle MethodHandle { get { throw new PlatformNotSupportedException(); } }

        public sealed override MethodImplAttributes MethodImplementationFlags => MethodImplAttributes.IL;

        public sealed override string Name => ConstructorInfo.ConstructorName;

        protected sealed override RuntimeParameterInfo[] RuntimeParameters => Array.Empty<RuntimeParameterInfo>();

        public sealed override string ToString()
        {
            // A constructor's "return type" is always System.Void and we don't want to allocate a ParameterInfo object to record that revelation.
            // In deference to that, ComputeToString() lets us pass null as a synonym for "void."
            return RuntimeMethodHelpers.ComputeToString(this, Array.Empty<RuntimeTypeInfo>(), RuntimeParameters, returnParameter: null);
        }

        protected sealed override MethodInvoker UncachedMethodInvoker
        {
            get
            {
                // If we got here, someone called MethodBase.Invoke() (not to be confused with ConstructorInfo.Invoke()). When invoked a constructor, that overload
                // (which should never been exposed on MethodBase) reexecutes a constructor on an already constructed object. This is meaningless so leaving it PNSE
                // unless there's a real-world app that does this.
                throw new PlatformNotSupportedException();
            }
        }

        private readonly RuntimeCLSIDTypeInfo _declaringType;
    }
}
