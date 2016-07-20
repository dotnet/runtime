// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System.ComponentModel;
    using System.Xml.Serialization;

    internal enum FacetType
    {
        None,
        Length,
        MinLength,
        MaxLength,
        Pattern,
        Whitespace,
        Enumeration,
        MinExclusive,
        MinInclusive,
        MaxExclusive,
        MaxInclusive,
        TotalDigits,
        FractionDigits,
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaFacet"]/*' />
    public abstract class XmlSchemaFacet : XmlSchemaAnnotated
    {
        private string _value;
        private bool _isFixed;
        private FacetType _facetType;

        /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaFacet.Value"]/*' />
        [XmlAttribute("value")]
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaFacet.IsFixed"]/*' />
        [XmlAttribute("fixed"), DefaultValue(false)]
        public virtual bool IsFixed
        {
            get { return _isFixed; }
            set
            {
                if (!(this is XmlSchemaEnumerationFacet) && !(this is XmlSchemaPatternFacet))
                {
                    _isFixed = value;
                }
            }
        }

        internal FacetType FacetType
        {
            get
            {
                return _facetType;
            }
            set
            {
                _facetType = value;
            }
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaNumericFacet"]/*' />
    public abstract class XmlSchemaNumericFacet : XmlSchemaFacet { }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaLengthFacet"]/*' />
    public class XmlSchemaLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaLengthFacet()
        {
            FacetType = FacetType.Length;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMinLengthFacet"]/*' />
    public class XmlSchemaMinLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaMinLengthFacet()
        {
            FacetType = FacetType.MinLength;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMaxLengthFacet"]/*' />
    public class XmlSchemaMaxLengthFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaMaxLengthFacet()
        {
            FacetType = FacetType.MaxLength;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaPatternFacet"]/*' />
    public class XmlSchemaPatternFacet : XmlSchemaFacet
    {
        public XmlSchemaPatternFacet()
        {
            FacetType = FacetType.Pattern;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaEnumerationFacet"]/*' />
    public class XmlSchemaEnumerationFacet : XmlSchemaFacet
    {
        public XmlSchemaEnumerationFacet()
        {
            FacetType = FacetType.Enumeration;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMinExclusiveFacet"]/*' />
    public class XmlSchemaMinExclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMinExclusiveFacet()
        {
            FacetType = FacetType.MinExclusive;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMinInclusiveFacet"]/*' />
    public class XmlSchemaMinInclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMinInclusiveFacet()
        {
            FacetType = FacetType.MinInclusive;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMaxExclusiveFacet"]/*' />
    public class XmlSchemaMaxExclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMaxExclusiveFacet()
        {
            FacetType = FacetType.MaxExclusive;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaMaxInclusiveFacet"]/*' />
    public class XmlSchemaMaxInclusiveFacet : XmlSchemaFacet
    {
        public XmlSchemaMaxInclusiveFacet()
        {
            FacetType = FacetType.MaxInclusive;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaTotalDigitsFacet"]/*' />
    public class XmlSchemaTotalDigitsFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaTotalDigitsFacet()
        {
            FacetType = FacetType.TotalDigits;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaFractionDigitsFacet"]/*' />
    public class XmlSchemaFractionDigitsFacet : XmlSchemaNumericFacet
    {
        public XmlSchemaFractionDigitsFacet()
        {
            FacetType = FacetType.FractionDigits;
        }
    }

    /// <include file='doc\XmlSchemaFacet.uex' path='docs/doc[@for="XmlSchemaWhiteSpaceFacet"]/*' />
    public class XmlSchemaWhiteSpaceFacet : XmlSchemaFacet
    {
        public XmlSchemaWhiteSpaceFacet()
        {
            FacetType = FacetType.Whitespace;
        }
    }
}
