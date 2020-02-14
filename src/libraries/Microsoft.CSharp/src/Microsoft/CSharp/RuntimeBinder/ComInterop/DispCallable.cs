// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_COM
using System.Linq.Expressions;

using System;
using System.Dynamic;
using System.Globalization;

namespace Microsoft.Scripting.ComInterop {

    /// <summary>
    /// This represents a bound dispmember on a IDispatch object.
    /// </summary>
    internal sealed class DispCallable : IPseudoComObject {

        private readonly IDispatchComObject _dispatch;
        private readonly string _memberName;
        private readonly int _dispId;

        internal DispCallable(IDispatchComObject dispatch, string memberName, int dispId) {
            _dispatch = dispatch;
            _memberName = memberName;
            _dispId = dispId;
        }

        public override string ToString() {
            return String.Format(CultureInfo.CurrentCulture, "<bound dispmethod {0}>", _memberName);
        }

        public IDispatchComObject DispatchComObject {
            get { return _dispatch; }
        }

        public IDispatch DispatchObject {
            get { return _dispatch.DispatchObject; }
        }

        public string MemberName {
            get { return _memberName; }
        }

        public int DispId {
            get { return _dispId; }
        }

        public DynamicMetaObject GetMetaObject(Expression parameter) {
            return new DispCallableMetaObject(parameter, this);
        }

        public override bool Equals(object obj) {
            return obj is DispCallable other && other._dispatch == _dispatch && other._dispId == _dispId;
        }

        public override int GetHashCode() {
            return _dispatch.GetHashCode() ^ _dispId;
        }
    }
}

#endif
