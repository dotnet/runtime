// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DontUse
// XPathContext is not used any more but comments in it and Replacer visitor may be used to
// optimize code XSLT generates on last().
using System;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Xml.XPath;
using MS.Internal.Xml;

namespace System.Xml.Xsl.XPath {

    internal sealed class XPathContext {
        // Context is the most fundamental concept of XPath
        // In docs it is -- "current node-set" and "current node in this node-set"
        // on practice in this implementation we have "current node" (C), "position of current node in current node-set" (P)
        // and "size of the current node-set" (S)
        // All XPath expressions consume context, "step" expressions change it
        // In this XPath implementation we passing context as Tuple.
        // In most cases this Tuple has For iterator bind to some expression.
        // (1): Tuple1( For1(bind1), cond{For1}, For1 )
        // To take (C) from such context XPath uses its For1 iterator.
        //
        // (P) is little bit more complex. It should be PositionOf(For1) but this will be correct only if
        // cond{For1} == True
        // To enforce this (1) can be rewritten as
        // (2): Tuple1( For1(Tuple2( For2(bind1), cond{For2}, For2 ), True, For1 )
        // Note: cond{For2} is result of replacing For1 to For2 in cond{For1}
        //
        // (S) most complex case. We want to cash with Let variable node-set we are iterating:
        // (3): Tuple1( Let(Tuple2( For2(bind1), cond{For2}, For2) ), True, Tuple3( For1(Let), True, For1 ) )
        // (S) = SetLength(Let); (P) = PositionOf(For1)
        //
        // So 3: is most generic representation of XPath context while (1) & (2) is its simplified form.
        // We always create context node as (1) and convert it to (2) & (3) on demand.
        // XPath function position() force (1) -> (2) transformation if condition != true
        // XPath function last() force (1) -> (3) or (2) -> (3) transformation if node-set wasn't cashed yet.
        //
        // Consider expression "foo[bar + position() + last()]"
        // "foo"         ==> Tuple1(For1(Content(XmlContext)), Name(For1)=='foo', For1))
        // "bar"         ==> Tuple2(For2(Content(For1      )), Name(For2)=='bar', For2))
        //
        // "position()"  ==> PositionOf(For1)
        //     After "foo" was rewritten as:
        // "for"         ==> Tuple1(For1(Tuple3(For3(Content(XmlContext)), Name(For3)=='foo', For3)), True, For1)
        //     Note1: all external references to "foo" expression are still valid because we didn't change Tuple1
        //     Note2: "bar" is still valid because it refers to For1.
        //     Note3: "Name(For1)=='foo'" was rewritten as "Name(For3)=='foo'"
        // "last()"      ==> SetLength(Let)
        // "for"         ==> Tuple1(Let(Tuple3(For3(Content(XmlContext)), Name(For3)=='foo', For3)), True, Tuple4(For1(Let), True, For1))
        //     Note2: "bar" and "position" are still valid because they refer to For1.

        // Issue:
        // What restriction we should put on return value of context tuple.
        // In XSLT when we fixing up call-templates return value shouldn't be touched
        // In simple XPath a/b[2] return value is pure iterator and can be left as it is
        // In more complex XPath (a/b)[2] resturn value is Tuple and should be isolated with context.
        //       this can be done as grouping as well.
        // XSLT complies for-each and apply-templates in form (2) so converting to (3) shouldn't be a problem
        // -- It look like solution is group (a/b) with For(DocOrderDistinct(...)) and we are set.

        // Methods that deal with XPath context. Xslt.QilGenerator calls these method as well:
        public static QilNode GetCurrentNode(QilTuple context) {
            Debug.Assert(context != null);
            Debug.Assert(GetTuple(context).For.Type == QilNodeType.For);
            return GetTuple(context).For;
        }

        public static QilNode GetCurrentPosition(QilFactory f, QilTuple context) {
            Debug.Assert(context != null);
            if (context.Where.Type != QilNodeType.True) {
                Debug.Assert(context.For.Type == QilNodeType.For);
                // convert context (1) --> (2)
                QilIterator for2 = f.For(context.For.Binding);
                QilNode     cnd2 = new Replacer(f).Replace(/*inExpr:*/context.Where, /*from:*/context.For, /*to:*/for2);
                context.For.Binding = f.OldTuple(for2, cnd2, for2);
                context.Where = f.True();
            }
            return f.Convert(f.PositionOf((QilIterator)XPathContext.GetCurrentNode(context)), f.TypeFactory.Double());
        }

        public static QilNode GetLastPosition(QilFactory f, QilTuple context) {
            return f.Convert(f.Length(context.Clone(f)), f.TypeFactory.Double());
        }

        public static QilTuple GetTuple(QilTuple context) {
            Debug.Assert(context != null);
            if (context.For.Type == QilNodeType.Let) {
                Debug.Assert(context.Where.Type == QilNodeType.True);
                Debug.Assert(context.Return.Type == QilNodeType.OldTuple);
                return (QilTuple) context.Return;
            }
            return context;
        }

        private sealed class Replacer : QilActiveVisitor {
            QilIterator from, to;

            public Replacer(QilFactory f) : base(f) {}

            public QilNode Replace(QilNode inExpr, QilIterator from, QilIterator to) {
                this.from = from;
                this.to   = to  ;
                return Visit(inExpr);
            }

            protected override QilNode VisitClassReference(QilNode it) {
                if (it == from) {
                    return to;
                }
                return it;
            }
        }
    }
}
#endif
