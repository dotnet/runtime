// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// This analyzer walks each function in the graph and annotates Invoke nodes which can
    /// be compiled using the IL .tailcall instruction.  This instruction will discard the
    /// current stack frame before calling the new function.
    /// </summary>
    internal static class TailCallAnalyzer
    {
        /// <summary>
        /// Perform tail-call analysis on the functions in the specified QilExpression.
        /// </summary>
        public static void Analyze(QilExpression qil)
        {
            foreach (QilFunction ndFunc in qil.FunctionList)
            {
                // Only analyze functions which are pushed to the writer, since otherwise code
                // is generated after the call instruction in order to process cached results
                if (XmlILConstructInfo.Read(ndFunc).ConstructMethod == XmlILConstructMethod.Writer)
                    AnalyzeDefinition(ndFunc.Definition);
            }
        }

        /// <summary>
        /// Recursively analyze the definition of a function.
        /// </summary>
        private static void AnalyzeDefinition(QilNode nd)
        {
            Debug.Assert(XmlILConstructInfo.Read(nd).PushToWriterLast,
                         "Only need to analyze expressions which will be compiled in push mode.");

            switch (nd.NodeType)
            {
                case QilNodeType.Invoke:
                    // Invoke node can either be compiled as IteratorThenWriter, or Writer.
                    // Since IteratorThenWriter involves caching the results of the function call
                    // and iterating over them, .tailcall cannot be used
                    if (XmlILConstructInfo.Read(nd).ConstructMethod == XmlILConstructMethod.Writer)
                        OptimizerPatterns.Write(nd).AddPattern(OptimizerPatternName.TailCall);
                    break;

                case QilNodeType.Loop:
                    {
                        // Recursively analyze Loop return value
                        QilLoop ndLoop = (QilLoop)nd;
                        if (ndLoop.Variable.NodeType == QilNodeType.Let || !ndLoop.Variable.Binding.XmlType.MaybeMany)
                            AnalyzeDefinition(ndLoop.Body);
                        break;
                    }

                case QilNodeType.Sequence:
                    {
                        // Recursively analyze last expression in Sequence
                        QilList ndSeq = (QilList)nd;
                        if (ndSeq.Count > 0)
                            AnalyzeDefinition(ndSeq[ndSeq.Count - 1]);
                        break;
                    }

                case QilNodeType.Choice:
                    {
                        // Recursively analyze Choice branches
                        QilChoice ndChoice = (QilChoice)nd;
                        for (int i = 0; i < ndChoice.Branches.Count; i++)
                            AnalyzeDefinition(ndChoice.Branches[i]);
                        break;
                    }

                case QilNodeType.Conditional:
                    {
                        // Recursively analyze Conditional branches
                        QilTernary ndCond = (QilTernary)nd;
                        AnalyzeDefinition(ndCond.Center);
                        AnalyzeDefinition(ndCond.Right);
                        break;
                    }

                case QilNodeType.Nop:
                    AnalyzeDefinition(((QilUnary)nd).Child);
                    break;
            }
        }
    }
}

