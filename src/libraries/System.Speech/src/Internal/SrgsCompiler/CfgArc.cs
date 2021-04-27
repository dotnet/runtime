// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal struct CfgArc
    {
        #region Constructors

        internal CfgArc(CfgArc arc)
        {
            _flag1 = arc._flag1;
            _flag2 = arc._flag2;
        }

        #endregion

        #region Internal Properties

        internal bool RuleRef
        {
            get
            {
                return ((_flag1 & 0x1) != 0);
            }
            set
            {
                if (value)
                {
                    _flag1 |= 0x1;
                }
                else
                {
                    _flag1 &= ~0x1U;
                }
            }
        }

        internal bool LastArc
        {
            get
            {
                return ((_flag1 & 0x2) != 0);
            }
            set
            {
                if (value)
                {
                    _flag1 |= 0x2;
                }
                else
                {
                    _flag1 &= ~0x2U;
                }
            }
        }

        internal bool HasSemanticTag
        {
            get
            {
                return ((_flag1 & 0x4) != 0);
            }
            set
            {
                if (value)
                {
                    _flag1 |= 0x4;
                }
                else
                {
                    _flag1 &= ~0x4U;
                }
            }
        }

        internal bool LowConfRequired
        {
            get
            {
                return ((_flag1 & 0x8) != 0);
            }
            set
            {
                if (value)
                {
                    _flag1 |= 0x8;
                }
                else
                {
                    _flag1 &= ~0x8U;
                }
            }
        }

        internal bool HighConfRequired
        {
            get
            {
                return ((_flag1 & 0x10) != 0);
            }
            set
            {
                if (value)
                {
                    _flag1 |= 0x10;
                }
                else
                {
                    _flag1 &= ~0x10U;
                }
            }
        }

        internal uint TransitionIndex
        {
            get
            {
                return (_flag1 >> 5) & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFFU)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag1 &= ~(0x3FFFFFU << 5);
                _flag1 |= value << 5;
            }
        }

        internal uint MatchMode
        {
            get
            {
                return (_flag1 >> 27) & 0x7;
            }
            set
            {
                _flag1 &= ~(0x38000000U);
                _flag1 |= value << 27;
            }
        }

        internal uint NextStartArcIndex
        {
            get
            {
                return (_flag2 >> 8) & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag2 &= ~(0x3FFFFFU << 8);
                _flag2 |= value << 8;
            }
        }

        #endregion

        #region private Fields

        private uint _flag1;

        private uint _flag2;

        #endregion
    }
}
