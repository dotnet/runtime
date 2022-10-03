// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;

using Internal.Reflection.Core;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // Abstract base for all ParameterInfo objects exposed by runtime MethodBase objects
    // (including the ReturnParameter.)
    //
    internal abstract class RuntimeMethodParameterInfo : RuntimeParameterInfo
    {
        protected RuntimeMethodParameterInfo(MethodBase member, int position, QSignatureTypeHandle qualifiedParameterTypeHandle, TypeContext typeContext)
            : base(member, position)
        {
            QualifiedParameterTypeHandle = qualifiedParameterTypeHandle;
            _typeContext = typeContext;
        }

        public sealed override Type[] GetOptionalCustomModifiers() => QualifiedParameterTypeHandle.GetCustomModifiers(_typeContext, optional: true);

        public sealed override Type[] GetRequiredCustomModifiers() => QualifiedParameterTypeHandle.GetCustomModifiers(_typeContext, optional: false);

        public sealed override Type ParameterType
        {
            get
            {
                return _lazyParameterType ??= QualifiedParameterTypeHandle.Resolve(_typeContext);
            }
        }

        internal sealed override string ParameterTypeString
        {
            get
            {
                return QualifiedParameterTypeHandle.FormatTypeName(_typeContext);
            }
        }

        protected readonly QSignatureTypeHandle QualifiedParameterTypeHandle;
        private readonly TypeContext _typeContext;
        private volatile Type _lazyParameterType;
    }
}
