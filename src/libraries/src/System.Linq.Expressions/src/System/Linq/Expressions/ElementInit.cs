// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents the initialization of a list.
    /// </summary>
    public sealed class ElementInit : IArgumentProvider
    {
        private MethodInfo _addMethod;
        private ReadOnlyCollection<Expression> _arguments;

        internal ElementInit(MethodInfo addMethod, ReadOnlyCollection<Expression> arguments)
        {
            _addMethod = addMethod;
            _arguments = arguments;
        }
        /// <summary>
        /// Gets the <see cref="MethodInfo"/> used to add elements to the object.
        /// </summary>
        public MethodInfo AddMethod
        {
            get { return _addMethod; }
        }

        /// <summary>
        /// Gets the list of elements to be added to the object.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments
        {
            get { return _arguments; }
        }

        public Expression GetArgument(int index)
        {
            return _arguments[index];
        }

        public int ArgumentCount
        {
            get
            {
                return _arguments.Count;
            }
        }

        /// <summary>
        /// Creates a <see cref="String"/> representation of the node.
        /// </summary>
        /// <returns>A <see cref="String"/> representation of the node.</returns>
        public override string ToString()
        {
            return ExpressionStringBuilder.ElementInitBindingToString(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="arguments">The <see cref="Arguments" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public ElementInit Update(IEnumerable<Expression> arguments)
        {
            if (arguments == Arguments)
            {
                return this;
            }
            return Expression.ElementInit(AddMethod, arguments);
        }
    }


    public partial class Expression
    {
        /// <summary>
        /// Creates an <see cref="T:ElementInit">ElementInit</see> expression that represents the initialization of a list.
        /// </summary>
        /// <param name="addMethod">The <see cref="MethodInfo"/> for the list's Add method.</param>
        /// <param name="arguments">An array containing the Expressions to be used to initialize the list.</param>
        /// <returns>The created <see cref="T:ElementInit">ElementInit</see> expression.</returns>
        public static ElementInit ElementInit(MethodInfo addMethod, params Expression[] arguments)
        {
            return ElementInit(addMethod, arguments as IEnumerable<Expression>);
        }

        /// <summary>
        /// Creates an <see cref="T:ElementInit">ElementInit</see> expression that represents the initialization of a list.
        /// </summary>
        /// <param name="addMethod">The <see cref="MethodInfo"/> for the list's Add method.</param>
        /// <param name="arguments">An <see cref="IEnumerable{T}"/> containing <see cref="Expression"/> elements to initialize the list.</param>
        /// <returns>The created <see cref="T:ElementInit">ElementInit</see> expression.</returns>
        public static ElementInit ElementInit(MethodInfo addMethod, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(addMethod, nameof(addMethod));
            ContractUtils.RequiresNotNull(arguments, nameof(arguments));

            var argumentsRO = arguments.ToReadOnly();

            RequiresCanRead(argumentsRO, nameof(arguments));
            ValidateElementInitAddMethodInfo(addMethod);
            ValidateArgumentTypes(addMethod, ExpressionType.Call, ref argumentsRO);
            return new ElementInit(addMethod, argumentsRO);
        }

        private static void ValidateElementInitAddMethodInfo(MethodInfo addMethod)
        {
            ValidateMethodInfo(addMethod);
            ParameterInfo[] pis = addMethod.GetParametersCached();
            if (pis.Length == 0)
            {
                throw Error.ElementInitializerMethodWithZeroArgs();
            }
            if (!addMethod.Name.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                throw Error.ElementInitializerMethodNotAdd();
            }
            if (addMethod.IsStatic)
            {
                throw Error.ElementInitializerMethodStatic();
            }
            foreach (ParameterInfo pi in pis)
            {
                if (pi.ParameterType.IsByRef)
                {
                    throw Error.ElementInitializerMethodNoRefOutParam(pi.Name, addMethod.Name);
                }
            }
        }
    }
}
