// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal struct CfgRule
    {
        #region Constructors

        internal CfgRule(int id, int nameOffset, uint flag)
        {
            _flag = flag;
            _nameOffset = nameOffset;
            _id = id;
        }

        internal CfgRule(int id, int nameOffset, SPCFGRULEATTRIBUTES attributes)
        {
            _flag = 0;
            _nameOffset = nameOffset;
            _id = id;
            TopLevel = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_TopLevel) != 0);
            DefaultActive = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Active) != 0);
            PropRule = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Interpreter) != 0);
            Export = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Export) != 0);
            Dynamic = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Dynamic) != 0);
            Import = ((attributes & SPCFGRULEATTRIBUTES.SPRAF_Import) != 0);
        }

        #endregion

        #region Internal Properties

        internal bool TopLevel
        {
            get
            {
                return ((_flag & 0x0001) != 0);
            }
            set
            {
                if (value)
                {
                    _flag |= 0x0001;
                }
                else
                {
                    _flag &= ~(uint)0x0001;
                }
            }
        }

        internal bool DefaultActive
        {
            set
            {
                if (value)
                {
                    _flag |= 0x0002;
                }
                else
                {
                    _flag &= ~(uint)0x0002;
                }
            }
        }

        internal bool PropRule
        {
            set
            {
                if (value)
                {
                    _flag |= 0x0004;
                }
                else
                {
                    _flag &= ~(uint)0x0004;
                }
            }
        }

        internal bool Import
        {
            get
            {
                return ((_flag & 0x0008) != 0);
            }
            set
            {
                if (value)
                {
                    _flag |= 0x0008;
                }
                else
                {
                    _flag &= ~(uint)0x0008;
                }
            }
        }

        internal bool Export
        {
            get
            {
                return ((_flag & 0x0010) != 0);
            }
            set
            {
                if (value)
                {
                    _flag |= 0x0010;
                }
                else
                {
                    _flag &= ~(uint)0x0010;
                }
            }
        }

        internal bool HasResources
        {
            get
            {
                return ((_flag & 0x0020) != 0);
            }
        }

        internal bool Dynamic
        {
            get
            {
                return ((_flag & 0x0040) != 0);
            }
            set
            {
                if (value)
                {
                    _flag |= 0x0040;
                }
                else
                {
                    _flag &= ~(uint)0x0040;
                }
            }
        }

        internal bool HasDynamicRef
        {
            get
            {
                return ((_flag & 0x0080) != 0);
            }
            set
            {
                if (value)
                {
                    _flag |= 0x0080;
                }
                else
                {
                    _flag &= ~(uint)0x0080;
                }
            }
        }

        internal uint FirstArcIndex
        {
            get
            {
                return (_flag >> 8) & 0x3FFFFF;
            }
            set
            {
                if (value > 0x3FFFFF)
                {
                    XmlParser.ThrowSrgsException(SRID.TooManyArcs);
                }

                _flag &= ~((uint)0x3FFFFF << 8);
                _flag |= value << 8;
            }
        }

        internal bool DirtyRule
        {
            set
            {
                if (value)
                {
                    _flag |= 0x80000000;
                }
                else
                {
                    _flag &= ~0x80000000;
                }
            }
        }

        #endregion

        #region Internal Fields

        // should be private but the order is absolutely key for marshalling
        internal uint _flag;

        internal int _nameOffset;

        internal int _id;

        #endregion
    }

    #region Internal Enumeration

    [Flags]
    internal enum SPCFGRULEATTRIBUTES
    {
        SPRAF_TopLevel = (1 << 0),
        SPRAF_Active = (1 << 1),
        SPRAF_Export = (1 << 2),
        SPRAF_Import = (1 << 3),
        SPRAF_Interpreter = (1 << 4),
        SPRAF_Dynamic = (1 << 5),
        SPRAF_Root = (1 << 6),
        SPRAF_AutoPause = (1 << 16),
        SPRAF_UserDelimited = (1 << 17)
    }

    #endregion
}
