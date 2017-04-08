// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics;
    using System.Xml.XPath;
    using MS.Internal.Xml.XPath;

    internal sealed class ConstraintStruct
    {
        // for each constraint
        internal CompiledIdentityConstraint constraint;     // pointer to constraint
        internal SelectorActiveAxis axisSelector;
        internal ArrayList axisFields;                     // Add tableDim * LocatedActiveAxis in a loop
        internal Hashtable qualifiedTable;                 // Checking confliction
        internal Hashtable keyrefTable;                    // several keyref tables having connections to this one is possible
        private int _tableDim;                               // dimension of table = numbers of fields;

        internal int TableDim
        {
            get { return _tableDim; }
        }

        internal ConstraintStruct(CompiledIdentityConstraint constraint)
        {
            this.constraint = constraint;
            _tableDim = constraint.Fields.Length;
            this.axisFields = new ArrayList();              // empty fields
            this.axisSelector = new SelectorActiveAxis(constraint.Selector, this);
            if (this.constraint.Role != CompiledIdentityConstraint.ConstraintRole.Keyref)
            {
                this.qualifiedTable = new Hashtable();
            }
        }
    }

    // ActiveAxis plus the location plus the state of matching in the constraint table : only for field
    internal class LocatedActiveAxis : ActiveAxis
    {
        private int _column;                     // the column in the table (the field sequence)
        internal bool isMatched;                  // if it's matched, then fill value in the validator later
        internal KeySequence Ks;                        // associated with a keysequence it will fills in

        internal int Column
        {
            get { return _column; }
        }

        internal LocatedActiveAxis(Asttree astfield, KeySequence ks, int column) : base(astfield)
        {
            this.Ks = ks;
            _column = column;
            this.isMatched = false;
        }

        internal void Reactivate(KeySequence ks)
        {
            Reactivate();
            this.Ks = ks;
        }
    }

    // exist for optimization purpose
    // ActiveAxis plus
    // 1. overload endelement function from parent to return result
    // 2. combine locatedactiveaxis and keysequence more closely
    // 3. enable locatedactiveaxis reusing (the most important optimization point)
    // 4. enable ks adding to hashtable right after moving out selector node (to enable 3)
    // 5. will modify locatedactiveaxis class accordingly
    // 6. taking care of updating ConstraintStruct.axisFields
    // 7. remove constraintTable from ConstraintStruct
    // 8. still need centralized locatedactiveaxis for movetoattribute purpose
    internal class SelectorActiveAxis : ActiveAxis
    {
        private ConstraintStruct _cs;            // pointer of constraintstruct, to enable 6
        private ArrayList _KSs;                  // stack of KSStruct, will not become less 
        private int _KSpointer = 0;              // indicate current stack top (next available element);

        public int lastDepth
        {
            get { return (_KSpointer == 0) ? -1 : ((KSStruct)_KSs[_KSpointer - 1]).depth; }
        }

        public SelectorActiveAxis(Asttree axisTree, ConstraintStruct cs) : base(axisTree)
        {
            _KSs = new ArrayList();
            _cs = cs;
        }

        public override bool EndElement(string localname, string URN)
        {
            base.EndElement(localname, URN);
            if (_KSpointer > 0 && this.CurrentDepth == lastDepth)
            {
                return true;
                // next step PopPS, and insert into hash
            }
            return false;
        }

        // update constraintStruct.axisFields as well, if it's new LocatedActiveAxis
        public int PushKS(int errline, int errcol)
        {
            // new KeySequence each time
            KeySequence ks = new KeySequence(_cs.TableDim, errline, errcol);

            // needs to clear KSStruct before using
            KSStruct kss;
            if (_KSpointer < _KSs.Count)
            {
                // reuse, clear up KSs.KSpointer
                kss = (KSStruct)_KSs[_KSpointer];
                kss.ks = ks;
                // reactivate LocatedActiveAxis
                for (int i = 0; i < _cs.TableDim; i++)
                {
                    kss.fields[i].Reactivate(ks);               // reassociate key sequence
                }
            }
            else
            { // "==", new
                kss = new KSStruct(ks, _cs.TableDim);
                for (int i = 0; i < _cs.TableDim; i++)
                {
                    kss.fields[i] = new LocatedActiveAxis(_cs.constraint.Fields[i], ks, i);
                    _cs.axisFields.Add(kss.fields[i]);          // new, add to axisFields
                }
                _KSs.Add(kss);
            }

            kss.depth = this.CurrentDepth - 1;

            return (_KSpointer++);
        }

        public KeySequence PopKS()
        {
            return ((KSStruct)_KSs[--_KSpointer]).ks;
        }
    }

    internal class KSStruct
    {
        public int depth;                       // depth of selector when it matches
        public KeySequence ks;                  // ks of selector when it matches and assigned -- needs to new each time
        public LocatedActiveAxis[] fields;      // array of fields activeaxis when it matches and assigned

        public KSStruct(KeySequence ks, int dim)
        {
            this.ks = ks;
            this.fields = new LocatedActiveAxis[dim];
        }
    }

    internal class TypedObject
    {
        private class DecimalStruct
        {
            private bool _isDecimal = false;         // rare case it will be used...
            private decimal[] _dvalue;               // to accelerate equals operation.  array <-> list

            public bool IsDecimal
            {
                get { return _isDecimal; }
                set { _isDecimal = value; }
            }

            public decimal[] Dvalue
            {
                get { return _dvalue; }
            }

            public DecimalStruct()
            {
                _dvalue = new decimal[1];
            }
            //list
            public DecimalStruct(int dim)
            {
                _dvalue = new decimal[dim];
            }
        }

        private DecimalStruct _dstruct = null;
        private object _ovalue;
        private string _svalue;      // only for output
        private XmlSchemaDatatype _xsdtype;
        private int _dim = 1;
        private bool _isList = false;

        public int Dim
        {
            get { return _dim; }
        }

        public bool IsList
        {
            get { return _isList; }
        }

        public bool IsDecimal
        {
            get
            {
                Debug.Assert(_dstruct != null);
                return _dstruct.IsDecimal;
            }
        }
        public decimal[] Dvalue
        {
            get
            {
                Debug.Assert(_dstruct != null);
                return _dstruct.Dvalue;
            }
        }

        public object Value
        {
            get { return _ovalue; }
            set { _ovalue = value; }
        }

        public XmlSchemaDatatype Type
        {
            get { return _xsdtype; }
            set { _xsdtype = value; }
        }

        public TypedObject(object obj, string svalue, XmlSchemaDatatype xsdtype)
        {
            _ovalue = obj;
            _svalue = svalue;
            _xsdtype = xsdtype;
            if (xsdtype.Variety == XmlSchemaDatatypeVariety.List ||
                xsdtype is Datatype_base64Binary ||
                xsdtype is Datatype_hexBinary)
            {
                _isList = true;
                _dim = ((Array)obj).Length;
            }
        }

        public override string ToString()
        {
            // only for exception
            return _svalue;
        }

        public void SetDecimal()
        {
            if (_dstruct != null)
            {
                return;
            }

            // Debug.Assert(!this.IsDecimal);
            switch (_xsdtype.TypeCode)
            {
                case XmlTypeCode.Byte:
                case XmlTypeCode.UnsignedByte:
                case XmlTypeCode.Short:
                case XmlTypeCode.UnsignedShort:
                case XmlTypeCode.Int:
                case XmlTypeCode.UnsignedInt:
                case XmlTypeCode.Long:
                case XmlTypeCode.UnsignedLong:
                case XmlTypeCode.Decimal:
                case XmlTypeCode.Integer:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NonNegativeInteger:
                case XmlTypeCode.NegativeInteger:
                case XmlTypeCode.NonPositiveInteger:

                    if (_isList)
                    {
                        _dstruct = new DecimalStruct(_dim);
                        for (int i = 0; i < _dim; i++)
                        {
                            _dstruct.Dvalue[i] = Convert.ToDecimal(((Array)_ovalue).GetValue(i), NumberFormatInfo.InvariantInfo);
                        }
                    }
                    else
                    { //not list
                        _dstruct = new DecimalStruct();
                        //possibility of list of length 1.
                        _dstruct.Dvalue[0] = Convert.ToDecimal(_ovalue, NumberFormatInfo.InvariantInfo);
                    }
                    _dstruct.IsDecimal = true;
                    break;

                default:
                    if (_isList)
                    {
                        _dstruct = new DecimalStruct(_dim);
                    }
                    else
                    {
                        _dstruct = new DecimalStruct();
                    }
                    break;
            }
        }

        private bool ListDValueEquals(TypedObject other)
        {
            for (int i = 0; i < this.Dim; i++)
            {
                if (this.Dvalue[i] != other.Dvalue[i])
                {
                    return false;
                }
            }
            return true;
        }

        public bool Equals(TypedObject other)
        {
            // ? one is list with one member, another is not list -- still might be equal
            if (this.Dim != other.Dim)
            {
                return false;
            }

            if (this.Type != other.Type)
            {
                //Check if types are comparable
                if (!(this.Type.IsComparable(other.Type)))
                {
                    return false;
                }
                other.SetDecimal(); // can't use cast and other.Type.IsEqual (value1, value2)
                this.SetDecimal();
                if (this.IsDecimal && other.IsDecimal)
                { //Both are decimal / derived types 
                    return this.ListDValueEquals(other);
                }
            }

            // not-Decimal derivation or type equal
            if (this.IsList)
            {
                if (other.IsList)
                { //Both are lists and values are XmlAtomicValue[] or clrvalue[]. So use Datatype_List.Compare
                    return this.Type.Compare(this.Value, other.Value) == 0;
                }
                else
                { //this is a list and other is a single value
                    Array arr1 = this.Value as System.Array;
                    XmlAtomicValue[] atomicValues1 = arr1 as XmlAtomicValue[];
                    if (atomicValues1 != null)
                    { // this is a list of union
                        return atomicValues1.Length == 1 && atomicValues1.GetValue(0).Equals(other.Value);
                    }
                    else
                    {
                        return arr1.Length == 1 && arr1.GetValue(0).Equals(other.Value);
                    }
                }
            }
            else if (other.IsList)
            {
                Array arr2 = other.Value as System.Array;
                XmlAtomicValue[] atomicValues2 = arr2 as XmlAtomicValue[];
                if (atomicValues2 != null)
                { // other is a list of union
                    return atomicValues2.Length == 1 && atomicValues2.GetValue(0).Equals(this.Value);
                }
                else
                {
                    return arr2.Length == 1 && arr2.GetValue(0).Equals(this.Value);
                }
            }
            else
            { //Both are not lists
                return this.Value.Equals(other.Value);
            }
        }
    }

    internal class KeySequence
    {
        private TypedObject[] _ks;
        private int _dim;
        private int _hashcode = -1;
        private int _posline, _poscol;            // for error reporting

        internal KeySequence(int dim, int line, int col)
        {
            Debug.Assert(dim > 0);
            _dim = dim;
            _ks = new TypedObject[dim];
            _posline = line;
            _poscol = col;
        }

        public int PosLine
        {
            get { return _posline; }
        }

        public int PosCol
        {
            get { return _poscol; }
        }

        public object this[int index]
        {
            get
            {
                object result = _ks[index];
                return result;
            }
            set
            {
                _ks[index] = (TypedObject)value;
            }
        }

        // return true if no null field
        internal bool IsQualified()
        {
            for (int i = 0; i < _ks.Length; ++i)
            {
                if ((_ks[i] == null) || (_ks[i].Value == null)) return false;
            }
            return true;
        }

        // it's not directly suit for hashtable, because it's always calculating address
        public override int GetHashCode()
        {
            if (_hashcode != -1)
            {
                return _hashcode;
            }
            _hashcode = 0;  // indicate it's changed. even the calculated hashcode below is 0
            for (int i = 0; i < _ks.Length; i++)
            {
                // extract its primitive value to calculate hashcode
                // decimal is handled differently to enable among different CLR types
                _ks[i].SetDecimal();
                if (_ks[i].IsDecimal)
                {
                    for (int j = 0; j < _ks[i].Dim; j++)
                    {
                        _hashcode += _ks[i].Dvalue[j].GetHashCode();
                    }
                }
                // BUGBUG: will need to change below parts, using canonical presentation.
                else
                {
                    Array arr = _ks[i].Value as System.Array;
                    if (arr != null)
                    {
                        XmlAtomicValue[] atomicValues = arr as XmlAtomicValue[];
                        if (atomicValues != null)
                        {
                            for (int j = 0; j < atomicValues.Length; j++)
                            {
                                _hashcode += ((XmlAtomicValue)atomicValues.GetValue(j)).TypedValue.GetHashCode();
                            }
                        }
                        else
                        {
                            for (int j = 0; j < ((Array)_ks[i].Value).Length; j++)
                            {
                                _hashcode += ((Array)_ks[i].Value).GetValue(j).GetHashCode();
                            }
                        }
                    }
                    else
                    { //not a list
                        _hashcode += _ks[i].Value.GetHashCode();
                    }
                }
            }
            return _hashcode;
        }

        // considering about derived type
        public override bool Equals(object other)
        {
            // each key sequence member can have different type
            KeySequence keySequence = (KeySequence)other;
            for (int i = 0; i < _ks.Length; i++)
            {
                if (!_ks[i].Equals(keySequence._ks[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_ks[0].ToString());
            for (int i = 1; i < _ks.Length; i++)
            {
                sb.Append(" ");
                sb.Append(_ks[i].ToString());
            }
            return sb.ToString();
        }
    }
}
