// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Text;
using System.Diagnostics;

namespace System.Xml.Schema
{
    internal class NamespaceList
    {
        public enum ListType
        {
            Any,
            Other,
            Set
        };

        private ListType _type = ListType.Any;
        private Hashtable? _set;
        private readonly string? _targetNamespace;

        public NamespaceList()
        {
        }

        public NamespaceList(string namespaces, string targetNamespace)
        {
            Debug.Assert(targetNamespace != null);
            _targetNamespace = targetNamespace;
            namespaces = namespaces.Trim();
            if (namespaces == "##any" || namespaces.Length == 0)
            {
                _type = ListType.Any;
            }
            else if (namespaces == "##other")
            {
                _type = ListType.Other;
            }
            else
            {
                _type = ListType.Set;
                _set = new Hashtable();
                string[] splitString = XmlConvert.SplitString(namespaces);
                for (int i = 0; i < splitString.Length; ++i)
                {
                    if (splitString[i] == "##local")
                    {
                        _set[string.Empty] = string.Empty;
                    }
                    else if (splitString[i] == "##targetNamespace")
                    {
                        _set[targetNamespace] = targetNamespace;
                    }
                    else
                    {
                        XmlConvert.ToUri(splitString[i]); // can throw
                        _set[splitString[i]] = splitString[i];
                    }
                }
            }
        }

        public NamespaceList Clone()
        {
            NamespaceList nsl = (NamespaceList)MemberwiseClone();
            if (_type == ListType.Set)
            {
                Debug.Assert(_set != null);
                nsl._set = (Hashtable)(_set.Clone());
            }
            return nsl;
        }

        public ListType Type
        {
            get { return _type; }
        }

        public string? Excluded
        {
            get { return _targetNamespace; }
        }

        public ICollection Enumerate
        {
            get
            {
                switch (_type)
                {
                    case ListType.Set:
                        return _set!.Keys;
                    case ListType.Other:
                    case ListType.Any:
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public virtual bool Allows(string ns)
        {
            switch (_type)
            {
                case ListType.Any:
                    return true;
                case ListType.Other:
                    return ns != _targetNamespace && ns.Length != 0;
                case ListType.Set:
                    return _set![ns] != null;
            }
            Debug.Fail($"Unexpected type {_type}");
            return false;
        }

        public bool Allows(XmlQualifiedName qname)
        {
            return Allows(qname.Namespace);
        }

        public override string ToString()
        {
            switch (_type)
            {
                case ListType.Any:
                    return "##any";
                case ListType.Other:
                    return "##other";
                case ListType.Set:
                    StringBuilder sb = new StringBuilder();
                    bool first = true;
                    foreach (string? s in _set!.Keys)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(' ');
                        }
                        if (s == _targetNamespace)
                        {
                            sb.Append("##targetNamespace");
                        }
                        else if (s!.Length == 0)
                        {
                            sb.Append("##local");
                        }
                        else
                        {
                            sb.Append(s);
                        }
                    }
                    return sb.ToString();
            }
            Debug.Fail($"Unexpected type {_type}");
            return string.Empty;
        }

        public static bool IsSubset(NamespaceList sub, NamespaceList super)
        {
            if (super._type == ListType.Any)
            {
                return true;
            }
            else if (sub._type == ListType.Other && super._type == ListType.Other)
            {
                return super._targetNamespace == sub._targetNamespace;
            }
            else if (sub._type == ListType.Set)
            {
                if (super._type == ListType.Other)
                {
                    return !sub._set!.Contains(super._targetNamespace!);
                }
                else
                {
                    Debug.Assert(super._type == ListType.Set);
                    foreach (string? ns in sub._set!.Keys)
                    {
                        if (!super._set!.Contains(ns!))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }


        public static NamespaceList? Union(NamespaceList o1, NamespaceList o2, bool v1Compat)
        {
            NamespaceList? nslist = null;
            Debug.Assert(o1 != o2);
            if (o1._type == ListType.Any)
            { //clause 2 - o1 is Any
                nslist = new NamespaceList();
            }
            else if (o2._type == ListType.Any)
            { //clause 2 - o2 is Any
                nslist = new NamespaceList();
            }
            else if (o1._type == ListType.Set && o2._type == ListType.Set)
            { //clause 3 , both are sets
                nslist = o1.Clone();
                foreach (string? ns in o2._set!.Keys)
                {
                    nslist._set![ns!] = ns;
                }
            }
            else if (o1._type == ListType.Other && o2._type == ListType.Other)
            { //clause 4, both are negations
                if (o1._targetNamespace == o2._targetNamespace)
                { //negation of same value
                    nslist = o1.Clone();
                }
                else
                { //Not a breaking change, going from not expressible to not(absent)
                    nslist = new NamespaceList("##other", string.Empty); //clause 4, negations of different values, result is not(absent)
                }
            }
            else if (o1._type == ListType.Set && o2._type == ListType.Other)
            {
                if (v1Compat)
                {
                    if (o1._set!.Contains(o2._targetNamespace!))
                    {
                        nslist = new NamespaceList();
                    }
                    else
                    { //This was not there originally in V1, added for consistency since its not breaking
                        nslist = o2.Clone();
                    }
                }
                else
                {
                    if (o2._targetNamespace != string.Empty)
                    { //clause 5, o1 is set S, o2 is not(tns)
                        nslist = o1.CompareSetToOther(o2);
                    }
                    else if (o1._set!.Contains(string.Empty))
                    { //clause 6.1 - set S includes absent, o2 is not(absent)
                        nslist = new NamespaceList();
                    }
                    else
                    { //clause 6.2 - set S does not include absent, result is not(absent)
                        nslist = new NamespaceList("##other", string.Empty);
                    }
                }
            }
            else if (o2._type == ListType.Set && o1._type == ListType.Other)
            {
                if (v1Compat)
                {
                    if (o2._set!.Contains(o2._targetNamespace!))
                    {
                        nslist = new NamespaceList();
                    }
                    else
                    {
                        nslist = o1.Clone();
                    }
                }
                else
                { //New rules
                    if (o1._targetNamespace != string.Empty)
                    { //clause 5, o1 is set S, o2 is not(tns)
                        nslist = o2.CompareSetToOther(o1);
                    }
                    else if (o2._set!.Contains(string.Empty))
                    { //clause 6.1 - set S includes absent, o2 is not(absent)
                        nslist = new NamespaceList();
                    }
                    else
                    { //clause 6.2 - set S does not include absent, result is not(absent)
                        nslist = new NamespaceList("##other", string.Empty);
                    }
                }
            }
            return nslist;
        }

        private NamespaceList? CompareSetToOther(NamespaceList other)
        {
            //clause 5.1
            NamespaceList? nslist;
            if (_set!.Contains(other._targetNamespace!))
            { //S contains negated ns
                if (_set.Contains(string.Empty))
                { // AND S contains absent
                    nslist = new NamespaceList(); //any is the result
                }
                else
                { //clause 5.2
                    nslist = new NamespaceList("##other", string.Empty);
                }
            }
            else if (_set.Contains(string.Empty))
            { //clause 5.3 - Not expressible
                nslist = null;
            }
            else
            { //clause 5.4 - Set S does not contain negated ns or absent
                nslist = other.Clone();
            }
            return nslist;
        }

        public static NamespaceList? Intersection(NamespaceList o1, NamespaceList o2, bool v1Compat)
        {
            NamespaceList? nslist = null;
            Debug.Assert(o1 != o2); //clause 1
            if (o1._type == ListType.Any)
            { //clause 2 - o1 is any
                nslist = o2.Clone();
            }
            else if (o2._type == ListType.Any)
            { //clause 2 - o2 is any
                nslist = o1.Clone();
            }
            else if (o1._type == ListType.Set && o2._type == ListType.Other)
            { //Clause 3 o2 is other
                nslist = o1.Clone();
                nslist.RemoveNamespace(o2._targetNamespace!);
                if (!v1Compat)
                {
                    nslist.RemoveNamespace(string.Empty); //remove ##local
                }
            }
            else if (o1._type == ListType.Other && o2._type == ListType.Set)
            { //Clause 3 o1 is other
                nslist = o2.Clone();
                nslist.RemoveNamespace(o1._targetNamespace!);
                if (!v1Compat)
                {
                    nslist.RemoveNamespace(string.Empty); //remove ##local
                }
            }
            else if (o1._type == ListType.Set && o2._type == ListType.Set)
            { //clause 4
                nslist = new NamespaceList();
                nslist._type = ListType.Set;
                nslist._set = new Hashtable();
                foreach (string? ns in o1._set!.Keys)
                {
                    if (o2._set!.Contains(ns!))
                    {
                        nslist._set.Add(ns!, ns);
                    }
                }
            }
            else if (o1._type == ListType.Other && o2._type == ListType.Other)
            {
                if (o1._targetNamespace == o2._targetNamespace)
                { //negation of same namespace name
                    nslist = o1.Clone();
                    return nslist;
                }
                if (!v1Compat)
                {
                    if (o1._targetNamespace == string.Empty)
                    { // clause 6 - o1 is negation of absent
                        nslist = o2.Clone();
                    }
                    else if (o2._targetNamespace == string.Empty)
                    { //clause 6 - o1 is negation of absent
                        nslist = o1.Clone();
                    }
                }
                //if it comes here, its not expressible //clause 5
            }
            return nslist;
        }

        private void RemoveNamespace(string tns)
        {
            if (_set![tns] != null)
            {
                _set.Remove(tns);
            }
        }
    };

    internal sealed class NamespaceListV1Compat : NamespaceList
    {
        public NamespaceListV1Compat(string namespaces, string targetNamespace) : base(namespaces, targetNamespace) { }

        public override bool Allows(string ns)
        {
            if (this.Type == ListType.Other)
            {
                return ns != Excluded;
            }
            else
            {
                return base.Allows(ns);
            }
        }
    }
}
