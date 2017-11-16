// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt
{
    using T = XmlQueryTypeFactory;

    /**
    InvokeGenerator is one of the trickiest peaces here.
    ARGS:
         QilFunction func      -- Functions which should be invoked. Arguments of this function (formalArgs) are Let nodes
                                  annotated with names and default values.
                                  Problem 1 is that default values can contain references to previous args of this function.
                                  Problem 2 is that default values shouldn't contain fix-up nodes.
         ArrayList actualArgs  -- Array of QilNodes annotated with names. When name of formalArg match name actualArg last one
                                  is used as invokeArg, otherwise formalArg's default value is cloned and used.
    **/

    internal class InvokeGenerator : QilCloneVisitor
    {
        private bool _debug;
        private Stack<QilIterator> _iterStack;

        private QilList _formalArgs;
        private QilList _invokeArgs;
        private int _curArg;     // this.Clone() depends on this value

        private XsltQilFactory _fac;

        public InvokeGenerator(XsltQilFactory f, bool debug) : base(f.BaseFactory)
        {
            _debug = debug;
            _fac = f;
            _iterStack = new Stack<QilIterator>();
        }

        public QilNode GenerateInvoke(QilFunction func, IList<XslNode> actualArgs)
        {
            _iterStack.Clear();
            _formalArgs = func.Arguments;
            _invokeArgs = _fac.ActualParameterList();

            // curArg is an instance variable used in Clone() method
            for (_curArg = 0; _curArg < _formalArgs.Count; _curArg++)
            {
                // Find actual value for a given formal arg
                QilParameter formalArg = (QilParameter)_formalArgs[_curArg];
                QilNode invokeArg = FindActualArg(formalArg, actualArgs);

                // If actual value was not specified, use the default value and copy its debug comment
                if (invokeArg == null)
                {
                    if (_debug)
                    {
                        if (formalArg.Name.NamespaceUri == XmlReservedNs.NsXslDebug)
                        {
                            Debug.Assert(formalArg.Name.LocalName == "namespaces", "Cur,Pos,Last don't have default values and should be always added to by caller in AddImplicitArgs()");
                            Debug.Assert(formalArg.DefaultValue != null, "PrecompileProtoTemplatesHeaders() set it");
                            invokeArg = Clone(formalArg.DefaultValue);
                        }
                        else
                        {
                            invokeArg = _fac.DefaultValueMarker();
                        }
                    }
                    else
                    {
                        Debug.Assert(formalArg.Name.NamespaceUri != XmlReservedNs.NsXslDebug, "Cur,Pos,Last don't have default values and should be always added to by caller in AddImplicitArgs(). We don't have $namespaces in !debug.");
                        invokeArg = Clone(formalArg.DefaultValue);
                    }
                }

                XmlQueryType formalType = formalArg.XmlType;
                XmlQueryType invokeType = invokeArg.XmlType;

                // Possible arg types: anyType, node-set, string, boolean, and number
                _fac.CheckXsltType(formalArg);
                _fac.CheckXsltType(invokeArg);

                if (!invokeType.IsSubtypeOf(formalType))
                {
                    // This may occur only if inferred type of invokeArg is XslFlags.None
                    Debug.Assert(invokeType == T.ItemS, "Actual argument type is not a subtype of formal argument type");
                    invokeArg = _fac.TypeAssert(invokeArg, formalType);
                }

                _invokeArgs.Add(invokeArg);
            }

            // Create Invoke node and wrap it with previous parameter declarations
            QilNode invoke = _fac.Invoke(func, _invokeArgs);
            while (_iterStack.Count != 0)
                invoke = _fac.Loop(_iterStack.Pop(), invoke);

            return invoke;
        }

        private QilNode FindActualArg(QilParameter formalArg, IList<XslNode> actualArgs)
        {
            QilName argName = formalArg.Name;
            Debug.Assert(argName != null);
            foreach (XslNode actualArg in actualArgs)
            {
                if (actualArg.Name.Equals(argName))
                {
                    return ((VarPar)actualArg).Value;
                }
            }
            return null;
        }

        // ------------------------------------ QilCloneVisitor -------------------------------------

        protected override QilNode VisitReference(QilNode n)
        {
            QilNode replacement = FindClonedReference(n);

            // If the reference is internal for the subtree being cloned, return it as is
            if (replacement != null)
            {
                return replacement;
            }

            // Replacement was not found, thus the reference is external for the subtree being cloned.
            // The case when it refers to one of previous arguments (xsl:param can refer to previous
            // xsl:param's) must be taken care of.
            for (int prevArg = 0; prevArg < _curArg; prevArg++)
            {
                Debug.Assert(_formalArgs[prevArg] != null, "formalArg must be in the list");
                Debug.Assert(_invokeArgs[prevArg] != null, "This arg should be compiled already");

                // Is this a reference to prevArg?
                if (n == _formalArgs[prevArg])
                {
                    // If prevArg is a literal, just clone it
                    if (_invokeArgs[prevArg] is QilLiteral)
                    {
                        return _invokeArgs[prevArg].ShallowClone(_fac.BaseFactory);
                    }

                    // If prevArg is not an iterator, cache it in an iterator, and return it
                    if (!(_invokeArgs[prevArg] is QilIterator))
                    {
                        QilIterator var = _fac.BaseFactory.Let(_invokeArgs[prevArg]);
                        _iterStack.Push(var);
                        _invokeArgs[prevArg] = var;
                    }
                    Debug.Assert(_invokeArgs[prevArg] is QilIterator);
                    return _invokeArgs[prevArg];
                }
            }

            // This is a truly external reference, return it as is
            return n;
        }

        protected override QilNode VisitFunction(QilFunction n)
        {
            // No need to change function references
            return n;
        }
    }
}
