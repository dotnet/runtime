// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.ModuleCore
{
    ////////////////////////////////////////////////////////////////
    // SecurityFlags
    //
    ////////////////////////////////////////////////////////////////
    public enum SecurityFlags
    {
        None = 0,
        FullTrust = 1,
        ThreatModel = 2,
    }

    ////////////////////////////////////////////////////////////////
    // TestAttribute (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public abstract class TestAttribute : Attribute
    {
        //Data
        protected string pname;
        protected string pdesc;
        protected object[] pparams;
        protected int pid;
        protected string pguid;
        protected bool pinheritance = true;
        protected TestAttribute pparent = null;
        protected string pfilter;
        protected string pversion;

        //Allows Inheritance (ie: object to determine if ever been set)
        protected int? ppriority;       //Allows Inheritance
        protected string ppurpose;      //Allows Inheritance
        protected bool? pimplemented;   //Allows Inheritance
        protected string[] powners;     //Allows Inheritance
        protected string[] pareas;          //Allows Inheritance
        protected bool? pskipped;       //Allows Inheritance
        protected bool? perror;         //Allows Inheritance
        protected bool? pmanual;        //Allows Inheritance
        protected SecurityFlags? psecurity;     //Allows Inheritance
        protected string pfiltercriteria;//Allows Inheritance
        protected string[] planguages;      //Allows Inheritance
        protected string pxml;          //Allows Inheritance
        protected int? ptimeout;        //Allows Inheritance
        protected int? pthreads;        //Allows Inheritance
        protected int? prepeat;     //Allows Inheritance
        protected bool? pstress;        //Allows Inheritance

        //Constructors
        public TestAttribute()
        {
        }

        public TestAttribute(string desc)
        {
            Desc = desc;
        }

        public TestAttribute(string desc, params Object[] parameters)
        {
            Desc = desc;
            Params = parameters;
        }

        //Accessors
        public virtual string Name
        {
            get { return pname; }
            set { pname = value; }
        }

        public virtual string Desc
        {
            get { return pdesc; }
            set { pdesc = value; }
        }

        public virtual int Id
        {
            get { return pid; }
            set { pid = value; }
        }

        public virtual string Guid
        {
            get { return pguid; }
            set { pguid = value; }
        }

        public virtual int Priority
        {
            get
            {
                if (ppriority == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Priority;

                    //Default
                    return 2;
                }
                return (int)ppriority;
            }
            set { ppriority = value; }
        }

        public virtual string Owner
        {
            get
            {
                if (powners != null)
                    return powners[0];
                return null;
            }
            set
            {
                if (powners == null)
                    powners = new string[1];
                powners[0] = value;
            }
        }

        public virtual string[] Owners
        {
            get { return powners; }
            set { powners = value; }
        }

        public virtual string Area
        {
            get
            {
                if (pareas != null)
                    return pareas[0];
                return null;
            }
            set
            {
                if (pareas == null)
                    pareas = new string[1];
                pareas[0] = value;
            }
        }

        public virtual string[] Areas
        {
            get { return pareas; }
            set { pareas = value; }
        }

        public virtual string Version
        {
            get { return pversion; }
            set { pversion = value; }
        }

        public virtual object Param
        {
            get
            {
                if (pparams != null)
                    return pparams[0];
                return null;
            }
            set
            {
                if (pparams == null)
                    pparams = new object[1];
                pparams[0] = value;
            }
        }

        public virtual object[] Params
        {
            get { return pparams; }
            set { pparams = value; }
        }

        public virtual bool Inheritance
        {
            get { return pinheritance; }
            set { pinheritance = value; }
        }

        public virtual TestAttribute Parent
        {
            get { return pparent; }
            set { pparent = value; }
        }

        public virtual string Filter
        {
            get { return pfilter; }
            set { pfilter = value; }
        }

        public virtual string Purpose
        {
            get
            {
                if (ppurpose == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Purpose;

                    //Default
                    return null;
                }
                return (string)ppurpose;
            }
            set { ppurpose = value; }
        }

        public virtual bool Implemented
        {
            get
            {
                if (pimplemented == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Implemented;

                    //Default
                    return true;
                }
                return (bool)pimplemented;
            }
            set { pimplemented = value; }
        }

        public virtual bool Skipped
        {
            get
            {
                if (pskipped == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Skipped;

                    //Default
                    return false;
                }
                return (bool)pskipped;
            }
            set { pskipped = value; }
        }

        public virtual bool Error
        {
            get
            {
                if (perror == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Error;

                    //Default
                    return false;
                }
                return (bool)perror;
            }
            set { perror = value; }
        }

        public virtual bool Manual
        {
            get
            {
                if (pmanual == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Manual;

                    //Default
                    return false;
                }
                return (bool)pmanual;
            }
            set { pmanual = value; }
        }

        public virtual SecurityFlags Security
        {
            get
            {
                if (psecurity == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Security;

                    //Default
                    return SecurityFlags.None;
                }
                return (SecurityFlags)psecurity;
            }
            set { psecurity = value; }
        }

        public virtual string FilterCriteria
        {
            get
            {
                if (pfiltercriteria == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.FilterCriteria;

                    //Default
                    return null;
                }
                return (string)pfiltercriteria;
            }
            set { pfiltercriteria = value; }
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
                if (planguages == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Languages;

                    //Default
                    return null;
                }
                return (string[])planguages;
            }
            set { planguages = value; }
        }

        public virtual string Xml
        {
            get
            {
                if (pxml == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Xml;

                    //Default
                    return null;
                }
                return (string)pxml;
            }
            set { pxml = value; }
        }

        public virtual bool Stress
        {
            get
            {
                if (pstress == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Stress;

                    //Default
                    return false;
                }
                return (bool)pstress;
            }
            set { pstress = value; }
        }

        public virtual int Timeout
        {
            get
            {
                if (ptimeout == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Timeout;

                    //Default (infinite)
                    return 0;
                }
                return (int)ptimeout;
            }
            set { ptimeout = value; }
        }

        public virtual int Threads
        {
            get
            {
                if (pthreads == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Threads;

                    //Default (one thread)
                    return 1;
                }
                return (int)pthreads;
            }
            set { pthreads = value; }
        }

        public virtual int Repeat
        {
            get
            {
                if (prepeat == null)
                {
                    //Inheritance
                    if (Inheritance && pparent != null)
                        return pparent.Repeat;

                    //Default (no repeat)
                    return 0;
                }
                return (int)prepeat;
            }
            set { prepeat = value; }
        }
    }


    ////////////////////////////////////////////////////////////////
    // TestModule (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestModuleAttribute : TestAttribute
    {
        //Data
        protected string pcreated;
        protected string pmodified;

        //Constructors
        public TestModuleAttribute()
            : base()
        {
        }

        public TestModuleAttribute(string desc)
            : base(desc)
        {
        }

        public TestModuleAttribute(string desc, params Object[] parameters)
            : base(desc, parameters)
        {
        }

        [TestProperty(Visible = true)]
        public virtual string Created
        {
            get { return pcreated; }
            set { pcreated = value; }
        }

        [TestProperty(Visible = true)]
        public virtual string Modified
        {
            get { return pmodified; }
            set { pmodified = value; }
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestCaseAttribute : TestAttribute
    {
        //Constructors
        public TestCaseAttribute()
            : base()
        {
        }

        public TestCaseAttribute(string desc)
            : base(desc)
        {
        }

        public TestCaseAttribute(string desc, params Object[] parameters)
            : base(desc, parameters)
        {
        }
    }


    ////////////////////////////////////////////////////////////////
    // Variation (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class VariationAttribute : TestAttribute
    {
        //Data

        //Constructors
        public VariationAttribute()
            : base()
        {
        }

        public VariationAttribute(string desc)
            : base(desc)
        {
        }

        public VariationAttribute(string desc, params Object[] parameters)
            : base(desc, parameters)
        {
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestPropertyAttribute (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestPropertyAttribute : Attribute
    {
        //Data
        protected string pname;
        protected string pdesc;
        protected Guid pguid;
        protected int pid;
        protected object pdefaultvalue;
        protected TestPropertyFlags pflags = TestPropertyFlags.Read;

        //Constructors
        public TestPropertyAttribute()
        {
        }

        public virtual string Name
        {
            get { return pname; }
            set { pname = value; }
        }

        public virtual string Desc
        {
            get { return pdesc; }
            set { pdesc = value; }
        }

        public virtual Guid Guid
        {
            get { return pguid; }
            set { pguid = value; }
        }

        public virtual int Id
        {
            get { return pid; }
            set { pid = value; }
        }

        public virtual object DefaultValue
        {
            get { return pdefaultvalue; }
            set { pdefaultvalue = value; }
        }

        public virtual bool Settable
        {
            get { return this.IsFlag(TestPropertyFlags.Write); }
            set { this.SetFlag(TestPropertyFlags.Write, value); }
        }

        public virtual bool Required
        {
            get { return this.IsFlag(TestPropertyFlags.Required); }
            set { this.SetFlag(TestPropertyFlags.Required, value); }
        }

        public virtual bool Inherit
        {
            get { return this.IsFlag(TestPropertyFlags.Inheritance); }
            set { this.SetFlag(TestPropertyFlags.Inheritance, value); }
        }

        public virtual bool Visible
        {
            get { return this.IsFlag(TestPropertyFlags.Visible); }
            set { this.SetFlag(TestPropertyFlags.Visible, value); }
        }

        public virtual bool MultipleValues
        {
            get { return this.IsFlag(TestPropertyFlags.MultipleValues); }
            set { this.SetFlag(TestPropertyFlags.MultipleValues, value); }
        }
        public virtual TestPropertyFlags Flags
        {
            get { return pflags; }
            set { pflags = value; }
        }

        //Helpers
        protected bool IsFlag(TestPropertyFlags flags)
        {
            return (pflags & flags) == flags;
        }

        protected void SetFlag(TestPropertyFlags flags, bool value)
        {
            if (value)
                pflags |= flags;
            else
                pflags &= ~flags;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestInclude (attribute)
    //
    ////////////////////////////////////////////////////////////////
    public class TestIncludeAttribute : Attribute
    {
        //Data
        protected string pname;
        protected string pfile;
        protected string pfiles;
        protected string pfilter;

        //Constructors
        public TestIncludeAttribute()
        {
        }

        public virtual string Name
        {
            //Prefix for testcase names
            get { return pname; }
            set { pname = value; }
        }

        public virtual string File
        {
            get { return pfile; }
            set { pfile = value; }
        }

        public virtual string Files
        {
            //Search Pattern (ie: *.*)
            get { return pfiles; }
            set { pfiles = value; }
        }

        public virtual string Filter
        {
            get { return pfilter; }
            set { pfilter = value; }
        }
    }
}
