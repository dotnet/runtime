// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder, ICSharpInvokeOrInvokeMemberBinder
    {
        public CSharpCallFlags Flags { get { return _flags; } }
        private CSharpCallFlags _flags;

        public Type CallingContext { get { return _callingContext; } }
        private Type _callingContext;

        public IList<CSharpArgumentInfo> ArgumentInfo { get { return _argumentInfo.AsReadOnly(); } }
        private List<CSharpArgumentInfo> _argumentInfo;

        public bool StaticCall { get { return true; } }
        public IList<Type> TypeArguments { get { return new Type[0]; } }
        public string Name { get { return ".ctor"; } }

        bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded { get { return false; } }

        private RuntimeBinder _binder;

        public CSharpInvokeConstructorBinder(
            CSharpCallFlags flags,
            Type callingContext,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            _flags = flags;
            _callingContext = callingContext;
            _argumentInfo = BinderHelper.ToList(argumentInfo);
            _binder = RuntimeBinder.GetInstance();
        }

        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, args), _argumentInfo, null);
        }
    }
}
