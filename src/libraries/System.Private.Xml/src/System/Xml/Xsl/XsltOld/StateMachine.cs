// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld
{
    internal sealed class StateMachine
    {
        // Constants for the state table
        private const int Init = 0x000000;       // Initial state
        private const int Elem = 0x000001;       // Element was output
        private const int NsN = 0x000002;       // Namespace name was output
        private const int NsV = 0x000003;       // Namespace value was output (some more can follow)
        private const int Ns = 0x000004;       // Namespace was output
        private const int AttrN = 0x000005;       // Attribute name was output
        private const int AttrV = 0x000006;       // Attribute value was output (some more can follow)
        private const int Attr = 0x000007;       // Attribute was output
        private const int InElm = 0x000008;       // Filling in element, general state text
        private const int EndEm = 0x000009;       // After end element event - next end element doesn't generate token
        private const int InCmt = 0x00000a;       // Adding text to a comment
        private const int InPI = 0x00000b;       // Adding text to a processing instruction

        private const int StateMask = 0x00000F;       // State mask

        internal const int Error = 0x000010;       // Invalid XML state

        private const int Ignor = 0x000020;       // Ignore this transition
        private const int Assrt = 0x000030;       // Assrt

        private const int U = 0x000100;       // Depth up
        private const int D = 0x000200;       // Depth down

        internal const int DepthMask = 0x000300;       // Depth mask

        internal const int DepthUp = U;
        internal const int DepthDown = D;

        private const int C = 0x000400;       // BeginChild
        private const int H = 0x000800;       // HadChild
        private const int M = 0x001000;       // EmptyTag

        internal const int BeginChild = C;
        internal const int HadChild = H;
        internal const int EmptyTag = M;

        private const int B = 0x002000;       // Begin Record
        private const int E = 0x004000;       // Record finished

        internal const int BeginRecord = B;
        internal const int EndRecord = E;

        private const int S = 0x008000;       // Push namespace scope
        private const int P = 0x010000;       // Pop current namepsace scope

        internal const int PushScope = S;
        internal const int PopScope = P;              // Next event must pop scope

        //
        // Runtime state
        //

        private int _State;

        internal StateMachine()
        {
            _State = Init;
        }

        internal int State
        {
            get
            {
                return _State;
            }

            set
            {
                // Hope you know what you are doing ...
                _State = value;
            }
        }

        internal void Reset()
        {
            _State = Init;
        }

        internal static int StateOnly(int state)
        {
            return state & StateMask;
        }

        internal int BeginOutlook(XPathNodeType nodeType)
        {
            int newState = s_BeginTransitions[(int)nodeType][_State];
            Debug.Assert(newState != Assrt);
            return newState;
        }

        internal int Begin(XPathNodeType nodeType)
        {
            int newState = s_BeginTransitions[(int)nodeType][_State];
            Debug.Assert(newState != Assrt);

            if (newState != Error && newState != Ignor)
            {
                _State = newState & StateMask;
            }
            return newState;
        }

        internal int EndOutlook(XPathNodeType nodeType)
        {
            int newState = s_EndTransitions[(int)nodeType][_State];
            Debug.Assert(newState != Assrt);
            return newState;
        }

        internal int End(XPathNodeType nodeType)
        {
            int newState = s_EndTransitions[(int)nodeType][_State];
            Debug.Assert(newState != Assrt);

            if (newState != Error && newState != Ignor)
            {
                _State = newState & StateMask;
            }
            return newState;
        }

        private static readonly int[][] s_BeginTransitions = {
            /*                                    { Init,      Elem,          NsN,     NsV,     Ns,              AttrN,   AttrV,   Attr,            InElm,           EndEm,           InCmt,   InPI  }, */
            /* Root                  */ new int[] { Error,     Error,         Error,   Error,   Error,           Error,   Error,   Error,           Error,           Error,           Error,   Error },
            /* Element               */ new int[] { Elem |B|S, Elem |U|C|B|S, Error,   Error,   Elem |C|B|S,     Error,   Error,   Elem |C|B|S,     Elem |B|S,       Elem |B|P|S,     Error,   Error },
            /* Attribute             */ new int[] { Error,     AttrN|U,       Error,   Error,   AttrN,           Error,   Error,   AttrN,           Error,           Error,           Error,   Error },
            /* Namespace             */ new int[] { Error,     NsN|U,         Error,   Error,   NsN,             Error,   Error,   Error,           Error,           Error,           Error,   Error },
            /* Text                  */ new int[] { InElm|B,   InElm|U|C|B,   NsV|U,   NsV,     InElm|C|B,       AttrV|U, AttrV,   InElm|C|B,       InElm,           InElm|B|P,       InCmt,   InPI  },
            /* SignificantWhitespace */ new int[] { InElm|B,   InElm|U|C|B,   NsV|U,   NsV,     InElm|C|B,       AttrV|U, AttrV,   InElm|C|B,       InElm,           InElm|B|P,       InCmt,   InPI  },
            /* Whitespace            */ new int[] { InElm|B,   InElm|U|C|B,   NsV|U,   NsV,     InElm|C|B,       AttrV|U, AttrV,   InElm|C|B,       InElm,           InElm|B|P,       InCmt,   InPI  },
            /* ProcessingInstruction */ new int[] { InPI |B,   InPI |U|C|B,   Error,   Error,   InPI |C|B,       Error,   Error,   InPI |C|B,       InPI |B,         InPI |B|P,       Error,   Error },
            /* Comment               */ new int[] { InCmt|B,   InCmt|U|C|B,   Error,   Error,   InCmt|C|B,       Error,   Error,   InCmt|C|B,       InCmt|B,         InCmt|B|P,       Error,   Error },
            /* All                   */ new int[] { Error,     Error,         Error,   Error,   Error,           Error,   Error,   Error,           Error,           Error,           Error,   Error },
        };

        private static readonly int[][] s_EndTransitions = {
            /*                                    { Init,      Elem,          NsN,     NsV,     Ns,              AttrN,   AttrV,   Attr,            InElm,           EndEm,           InCmt,   InPI   }, */
            /* Root                  */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* Element               */ new int[] { Assrt,     EndEm|B|E|P|M, Assrt,   Assrt,   EndEm|D|B|E|P|M, Assrt,   Assrt,   EndEm|D|B|E|P|M, EndEm|D|H|B|E|P, EndEm|D|H|B|E|P, Assrt,   Assrt  },
            /* Attribute             */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Attr,    Attr |D, Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* Namespace             */ new int[] { Assrt,     Assrt,         Ns,      Ns |D,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* Text                  */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* SignificantWhitespace */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* Whitespace            */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
            /* ProcessingInstruction */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   EndEm|E},
            /* Comment               */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           EndEm|E, Assrt  },
            /* All                   */ new int[] { Assrt,     Assrt,         Assrt,   Assrt,   Assrt,           Assrt,   Assrt,   Assrt,           Assrt,           Assrt,           Assrt,   Assrt  },
        };
    }
}
