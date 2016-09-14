#|
//------------------------------------------------------------------------------
// <copyright file="XmlILOptimizer.p" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner>akimball</owner>
//------------------------------------------------------------------------------
|#


#|
###############################################################################################
###
### XmlILOptimizer.p
###
###   Pattern file used to generate XmlILOptimizerVisitor.cs.
###
###   Side Effect Policy
###   ==================
###   Side effects are either significant or insignificant.  Insignificant side effects are
###   ignored for purposes of optimization.  Nodes which are assumed to have significant side
###   effects are Error, Warning, XsltInvokeLateBound, and XsltInvokeEarlyBound (exc. in case
###   of built-in functions).  Other nodes have side effects, but they are considered
###   insignificant.  For example, the XsltConvert node may throw a conversion exception, but
###   the optimizer does not care about this side effect.
###
###   These are the rules that the optimizer follows when significant side effects are possible:
###     1. Subtrees containing significant side effects may not be completely removed from the
###        graph, except in cases covered by #5 and #6.
###     2. Subtress containing significant side effects may not be relocated in the graph,
###        unless relocation produces a graph that can evaluate to the same result.
###     3. The Conditional node's test must be evaluated before either of the branches, and only
###        the matching branch may be evaluated.
###     4. Loop, Filter, Sequence, and Sort operands are evaluated in order if any operands contain
###        significant side effects.
###     5. All other nodes (exc. those listed in #3 and #4) may have operands evaluated in any
###        order.
###     6. All nodes may lazily compute sets, and may choose not to evaluate some operands if
###        the correct answer can be derived from other operands.  For purposes of this rule,
###        the XmlQueryType of a node is not considered an operand.
###
###   Examples:
###     1. (Loop (Let $i:(Error)) (Sequence)) -- Cannot be optimized to (Sequence)
###        according to #1.
###     2. (Loop (Let $i:(Error)) (And (False) $i)) -- $i cannot be inlined according to #2.
###     3. (Conditional (false-expr) (LiteralInt32 1) (Error)) -- Error must be raised
###        according to #3.
###     4. (Loop $i:(Sequence) (Error)) -- Guaranteed to not throw an error according to #4.
###     5. (And (Error "1") (Error "2")) -- May throw error 1 or error 2 according to #5.
###     6. (And (False) (Error)) -- May or may not throw an error according to #6.
###
###############################################################################################
|#


(
(Options
  (PatternVisitor "XmlILOptimizerVisitor")
  (PatternEnum    "XmlILOptimization")
)
(
(.universe "..\\Qil\\Qil.p")

(:import "Macros.p")
(:import "ConstantFold.p")
(:import "Normalize.p")
(:import "Annotate.p")

))
