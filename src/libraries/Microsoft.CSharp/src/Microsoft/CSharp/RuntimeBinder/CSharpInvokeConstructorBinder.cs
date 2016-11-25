// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder, ICSharpInvokeOrInvokeMemberBinder
    {
        public CSharpCallFlags Flags { get; }

        public Type CallingContext { get; }

        public IList<CSharpArgumentInfo> ArgumentInfo { get { return _argumentInfo.AsReadOnly(); } }
        private readonly List<CSharpArgumentInfo> _argumentInfo;

        public bool StaticCall { get { return true; } }
        public IList<Type> TypeArguments { get { return Array.Empty<Type>(); } }
        public string Name { get { return ".ctor"; } }

        bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded { get { return false; } }

        private readonly RuntimeBinder _binder;

        public CSharpInvokeConstructorBinder(
            CSharpCallFlags flags,
            Type callingContext,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            Flags = flags;
            CallingContext = callingContext;
            _argumentInfo = BinderHelper.ToList(argumentInfo);
            _binder = RuntimeBinder.GetInstance();
        }

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, args), _argumentInfo, null);
        }
    }
}
