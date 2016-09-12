// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Xml.Xsl.Qil
{
    /// <summary>
    /// Adds iterator and function scoping to the QilVisitor implementation.
    /// </summary>
    internal class QilScopedVisitor : QilVisitor
    {
        //-----------------------------------------------
        // QilScopedVisitor methods
        //-----------------------------------------------

        /// <summary>
        /// Called when a variable, parameter, or function enters scope.
        /// </summary>
        protected virtual void BeginScope(QilNode node)
        {
        }

        /// <summary>
        /// Called when a variable, parameter, or function exits scope.
        /// </summary>
        protected virtual void EndScope(QilNode node)
        {
        }

        /// <summary>
        /// Called at the beginning of Visit().
        /// </summary>
        protected virtual void BeforeVisit(QilNode node)
        {
            QilExpression qil;

            switch (node.NodeType)
            {
                case QilNodeType.QilExpression:
                    // Put all global functions, variables, and parameters in scope
                    qil = (QilExpression)node;
                    foreach (QilNode param in qil.GlobalParameterList) BeginScope(param);
                    foreach (QilNode var in qil.GlobalVariableList) BeginScope(var);
                    foreach (QilNode func in qil.FunctionList) BeginScope(func);
                    break;

                case QilNodeType.Function:
                    // Put all formal arguments in scope
                    foreach (QilNode arg in ((QilFunction)node).Arguments) BeginScope(arg);
                    break;

                case QilNodeType.Loop:
                case QilNodeType.Filter:
                case QilNodeType.Sort:
                    // Put loop iterator in scope
                    BeginScope(((QilLoop)node).Variable);
                    break;
            }
        }

        /// <summary>
        /// Called at the end of Visit().
        /// </summary>
        protected virtual void AfterVisit(QilNode node)
        {
            QilExpression qil;

            switch (node.NodeType)
            {
                case QilNodeType.QilExpression:
                    // Remove all global functions, variables, and parameters from scope
                    qil = (QilExpression)node;
                    foreach (QilNode func in qil.FunctionList) EndScope(func);
                    foreach (QilNode var in qil.GlobalVariableList) EndScope(var);
                    foreach (QilNode param in qil.GlobalParameterList) EndScope(param);
                    break;

                case QilNodeType.Function:
                    // Remove all formal arguments from scope
                    foreach (QilNode arg in ((QilFunction)node).Arguments) EndScope(arg);
                    break;

                case QilNodeType.Loop:
                case QilNodeType.Filter:
                case QilNodeType.Sort:
                    // Remove loop iterator in scope
                    EndScope(((QilLoop)node).Variable);
                    break;
            }
        }


        //-----------------------------------------------
        // QilVisitor overrides
        //-----------------------------------------------

        /// <summary>
        /// Call BeforeVisit() and AfterVisit().
        /// </summary>
        protected override QilNode Visit(QilNode n)
        {
            QilNode ret;
            BeforeVisit(n);
            ret = base.Visit(n);
            AfterVisit(n);
            return ret;
        }
    }
}
