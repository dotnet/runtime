// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

using System.Speech.Internal.SrgsParser;

#endregion

namespace System.Speech.Internal.SrgsCompiler
{
    // Elements of the ParseStack
    //  SRGSNamespace.Grammar
    //      _startState, _endState are ignored and set to 0.
    //  SRGSNamespace.Rule
    //      startElement()  _startState = new Rule().InitialState
    //                      _endState = _startState  (Updated by the child elements)
    //      endElement()    AddEpsilonTransition(_endState -> terminating state null)
    //  SRGSNamespace.RuleRef/Token/Tag/Item(Parent!=OneOf)
    //      startElement()  _startState = Parent._startState
    //                      _endState = _startState  (Updated by the child elements)
    //      endElement()    Parent._endState = _endState
    //  SRGSNamespace.OneOf
    //      startElement()  _startState = Parent._startState
    //                      _endState = new State
    //      endElement()    Parent._endState = _endState
    //  SRGSNamespace.Item(Parent==OneOf)
    //      startElement()  _startState = Parent._startState
    //                      _endState = _startState  (Updated by the child elements)
    //      endElement()    AddEpsilonTransition(_endState -> Parent._endState)
    //  SRGSNamespace.Example/Lexicon/Meta
    //      _startState, _endState are ignored and set to 0.
    //  SRGSNamespace.Metadata / Unknown.*
    //      _startState, _endState are ignored and set to 0.
    //      ParseElements is added to the stack, but not used.
    internal abstract class ParseElement : IElement // Compiler stack element
    {
        internal ParseElement(Rule rule)
        {
            _rule = rule;
        }

        // Token - Required confidence
        internal int _confidence;

        void IElement.PostParse(IElement parent)
        {
        }

        internal Rule _rule;
    }
}
