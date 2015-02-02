// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Dynamic;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Used to test whether a dynamic member over which += or -= is used is an event member.
    /// </summary>
    internal sealed class CSharpIsEventBinder : DynamicMetaObjectBinder
    {
        internal string Name { get { return _name; } }
        private string _name;

        internal Type CallingContext { get { return _callingContext; } }
        private Type _callingContext;

        private RuntimeBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpIsEventBinder"/> class.
        /// </summary>
        /// <param name="name">The name of the member to test.</param>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        public CSharpIsEventBinder(
            string name,
            Type callingContext)
        {
            _name = name;
            _callingContext = callingContext;
            _binder = RuntimeBinder.GetInstance();
        }

        /// <summary>
        /// The result type of the operation.
        /// </summary>
        public override sealed Type ReturnType
        {
            get { return typeof(bool); }
        }

        /// <summary>
        /// Performs the binding of the binary dynamic operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic binary operation.</param>
        /// <param name="args">The arguments to the dynamic event test.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            return BinderHelper.Bind(this, _binder, new DynamicMetaObject[] { target }, null, null);
        }
    }
}
