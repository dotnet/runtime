// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace OLEDB.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // SecurityFlags
    //
    ////////////////////////////////////////////////////////////////
    public enum SecurityFlags
    {
        None = 0,
        FullTrust = 1,
    }

    ////////////////////////////////////////////////////////////////
    // TestAttr (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class CAttrBase : Attribute
    {
        //Data
        private string _name;
        private string _desc;
        private object[] _params;
        private int _id;
        private bool _inheritance = true;
        private CAttrBase _parent = null;
        private string _filter;

        //Allows Inheritance (i.e.: object to determine if ever been set)
        private object _priority;       //Allows Inheritance
        private object _implemented;    //Allows Inheritance
        private object _skipped;        //Allows Inheritance
        private object _error;          //Allows Inheritance
        private object _security;       //Allows Inheritance
        private object _filtercriteria;//Allows Inheritance
        private object[] _languages;        //Allows Inheritance
        private object _xml;            //Allows Inheritance

        //Constructors
        public CAttrBase()
        {
        }

        public CAttrBase(string desc)
        {
            Desc = desc;
        }

        //Accessors
        public virtual string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public virtual string Desc
        {
            get { return _desc; }
            set { _desc = value; }
        }

        public virtual int id
        {
            get { return _id; }
            set { _id = value; }
        }

        public virtual object Param
        {
            get
            {
                if (_params != null)
                    return _params[0];
                return null;
            }
            set
            {
                if (_params == null)
                    _params = new object[1];
                _params[0] = value;
            }
        }

        public virtual object[] Params
        {
            get { return _params; }
            set { _params = value; }
        }

        public virtual bool Inheritance
        {
            get { return _inheritance; }
            set { _inheritance = value; }
        }

        public virtual CAttrBase Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public virtual string Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }

        public virtual int Pri
        {
            get
            {
                if (_priority == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Pri;

                    //Default
                    return 2;
                }
                return (int)_priority;
            }
            set { _priority = value; }
        }

        public virtual int Priority //Alias for Pri
        {
            get { return this.Pri; }
            set { this.Pri = value; }
        }

        public virtual bool Implemented
        {
            get
            {
                if (_implemented == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Implemented;

                    //Default
                    return true;
                }
                return (bool)_implemented;
            }
            set { _implemented = value; }
        }

        public virtual bool Skipped
        {
            get
            {
                if (_skipped == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Skipped;

                    //Default
                    return false;
                }
                return (bool)_skipped;
            }
            set { _skipped = value; }
        }

        public virtual bool Error
        {
            get
            {
                if (_error == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Error;

                    //Default
                    return false;
                }
                return (bool)_error;
            }
            set { _error = value; }
        }

        public virtual SecurityFlags Security
        {
            get
            {
                if (_security == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Security;

                    //Default
                    return SecurityFlags.None;
                }
                return (SecurityFlags)_security;
            }
            set { _security = value; }
        }

        public virtual string FilterCriteria
        {
            get
            {
                if (_filtercriteria == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.FilterCriteria;

                    //Default
                    return null;
                }
                return (string)_filtercriteria;
            }
            set { _filtercriteria = value; }
        }

        public virtual string Language
        {
            get
            {
                if (Languages != null)
                    return Languages[0];
                return null;
            }
            set
            {
                if (Languages == null)
                    Languages = new string[1];
                Languages[0] = value;
            }
        }

        public virtual string[] Languages
        {
            get
            {
                if (_languages == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Languages;

                    //Default
                    return null;
                }
                return (string[])_languages;
            }
            set { _languages = value; }
        }

        public virtual string Xml
        {
            get
            {
                if (_xml == null)
                {
                    //Inheritance
                    if (Inheritance && _parent != null)
                        return _parent.Xml;

                    //Default
                    return null;
                }
                return (string)_xml;
            }
            set { _xml = value; }
        }
    }


    ////////////////////////////////////////////////////////////////
    // TestModule (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestModule : CAttrBase
    {
        //Data
        private string[] _owners;
        private int _version;
        private string _created;
        private string _modified;

        //Constructors
        public TestModule()
            : base()
        {
        }

        public TestModule(string desc)
            : base(desc)
        {
            //NOTE: For all other params, just simply use the named attributes:
            //[TestModule(Desc="desc", Version=1)]
        }

        //Accessors (named attributes)
        public virtual string Owner
        {
            get
            {
                if (_owners != null)
                    return _owners[0];
                return null;
            }
            set
            {
                if (_owners == null)
                    _owners = new string[1];
                _owners[0] = value;
            }
        }

        public virtual string[] Owners
        {
            get { return _owners; }
            set { _owners = value; }
        }

        public virtual int Version
        {
            get { return _version; }
            set { _version = value; }
        }

        public virtual string Created
        {
            get { return _created; }
            set { _created = value; }
        }

        public virtual string Modified
        {
            get { return _modified; }
            set { _modified = value; }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestCase : CAttrBase
    {
        //Constructors
        public TestCase()
            : base()
        {
        }

        public TestCase(string desc)
            : base(desc)
        {
            //NOTE: For all other params, just simply use the named attributes:
            //[TestCase(Desc="desc", Name="name")]
        }
    }


    ////////////////////////////////////////////////////////////////
    // Variation (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class Variation : CAttrBase
    {
        //Data

        //Constructors
        public Variation()
            : base()
        {
        }

        public Variation(string desc)
            : base(desc)
        {
            //NOTE: For all other params, just simply use the named attributes:
            //[Variation(Desc="desc", id=1)]
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestInclude (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestInclude : Attribute
    {
        //Data
        private string _name;
        private string _file;
        private string _files;
        private string _filter;

        //Constructors
        public TestInclude()
        {
        }

        public virtual string Name
        {
            //Prefix for test case names
            get { return _name; }
            set { _name = value; }
        }

        public virtual string File
        {
            get { return _file; }
            set { _file = value; }
        }

        public virtual string Files
        {
            //Search Pattern (i.e.: *.*)
            get { return _files; }
            set { _files = value; }
        }

        public virtual string Filter
        {
            get { return _filter; }
            set { _filter = value; }
        }
    }
}
