/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    testobj.cs

Abstract:

    Exposes test hooks.

    Only present in TESTHOOK builds.
    
History:

    06-May-2004    MattRim     Created

--*/

#if TESTHOOK

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;

namespace System.DirectoryServices.AccountManagement
{
    public class TestHook
    {
        static public void TestADStoreKeys()
        {
            // GUID-based ADStoreKeys
            Guid adGuid1 = Guid.NewGuid();
            Guid adGuid2 = Guid.NewGuid();

            ADStoreKey key1a = new ADStoreKey(adGuid1);
            ADStoreKey key1b = new ADStoreKey(adGuid1);
            ADStoreKey key2 = new ADStoreKey(adGuid2);

            Debug.Assert(key1a.Equals(key1b));
            Debug.Assert(!key1a.Equals(key2));

            Debug.Assert(key1a.GetHashCode() == key1b.GetHashCode());
            Debug.Assert(key1a.GetHashCode() != key2.GetHashCode());

            // SID-based ADStoreKeys
            string mach1 = "machine1";
            string mach1a = "machine1";
            string mach2 = "machine2";

            byte[] sid1a = new byte[]{0x1, 0x2, 0x3};
            byte[] sid1b = new byte[]{0x1, 0x2, 0x3};
            byte[] sid2  = new byte[]{0x10, 0x20, 0x30};

            ADStoreKey key3a = new ADStoreKey(mach1, sid1a);
            ADStoreKey key3b = new ADStoreKey(mach1, sid1a);
            ADStoreKey key3c = new ADStoreKey(mach1a, sid1b);
            ADStoreKey key4  = new ADStoreKey(mach2, sid2);
            ADStoreKey key5  = new ADStoreKey(mach1, sid2);

            Debug.Assert(key3a.Equals(key3b));
            Debug.Assert(key3a.Equals(key3c));
            
            Debug.Assert(!key3a.Equals(key4));
            Debug.Assert(!key3a.Equals(key5));

            Debug.Assert(!key3a.Equals(key1a));
            Debug.Assert(!key1a.Equals(key3a));

            Debug.Assert(key3a.GetHashCode() != key4.GetHashCode());

            // Shouldn't matter, since SAMStoreKey should make a copy of the byte[] sid
            sid1b[1] = 0xf;
            Debug.Assert(key3a.Equals(key3b));
            Debug.Assert(key3a.Equals(key3c));
            
        }

        static public void TestSAMStoreKeys()
        {
            string mach1 = "machine1";
            string mach1a = "machine1";
            string mach2 = "machine2";

            byte[] sid1a = new byte[]{0x1, 0x2, 0x3};
            byte[] sid1b = new byte[]{0x1, 0x2, 0x3};
            byte[] sid2  = new byte[]{0x10, 0x20, 0x30};

            SAMStoreKey key1a = new SAMStoreKey(mach1, sid1a);
            SAMStoreKey key1b = new SAMStoreKey(mach1, sid1a);
            SAMStoreKey key1c = new SAMStoreKey(mach1a, sid1b);
            SAMStoreKey key2  = new SAMStoreKey(mach2, sid2);
            SAMStoreKey key3  = new SAMStoreKey(mach1, sid2);

            Debug.Assert(key1a.Equals(key1b));
            Debug.Assert(key1a.Equals(key1c));
            
            Debug.Assert(!key1a.Equals(key2));
            Debug.Assert(!key1a.Equals(key3));
            
            Debug.Assert(key1a.GetHashCode() != key2.GetHashCode());

            // Shouldn't matter, since SAMStoreKey should make a copy of the byte[] sid
            sid1b[1] = 0xf;
            Debug.Assert(key1a.Equals(key1b));
            Debug.Assert(key1a.Equals(key1c));
            
        }

        //
        // ValueCollection
        //
        static public PrincipalValueCollection<T> ValueCollectionConstruct<T>()
        {
            return new PrincipalValueCollection<T>();
        }
        
        static public void ValueCollectionLoad<T>(PrincipalValueCollection<T> trackList, List<T> initial)
        {
            trackList.Load(initial);
        }

        static public List<T> ValueCollectionInserted<T>(PrincipalValueCollection<T> trackList)
        {
            return trackList.Inserted;
        }

        static public List<T> ValueCollectionRemoved<T>(PrincipalValueCollection<T> trackList)
        {
            return trackList.Removed;
        }

        static public List<T> ValueCollectionChangedValues<T>(PrincipalValueCollection<T> trackList, out List<T> rightOut)
        {
            // We'd like to just return the List<Pair<T,T>>, but Pair<T,T> isn't a public class
            List<T> left = new List<T>();
            List<T> right = new List<T>();
            List<Pair<T,T>> pairList = trackList.ChangedValues;

            foreach (Pair<T,T> pair in pairList)
            {
                left.Add(pair.Left);
                right.Add(pair.Right);
            }

            rightOut = right;
            return left;
        }

        static public bool ValueCollectionChanged<T>(PrincipalValueCollection<T> trackList)
        {
            return trackList.Changed;
        }

        static public void ValueCollectionResetTracking<T>(PrincipalValueCollection<T> trackList)
        {
            trackList.ResetTracking();
        }


        //
        // IdentityClaimCollection
        //
        static public IdentityClaimCollection ICCConstruct()
        {
            Group g = new Group();
            return new IdentityClaimCollection(g);
        }

        static public IdentityClaimCollection ICCConstruct2()
        {
            Group g = new Group();
            g.Context = PrincipalContext.Test;
            return new IdentityClaimCollection(g);
        }

        static public IdentityClaimCollection ICCConstructAlt()
        {
            Group g = new Group();
            g.Context = PrincipalContext.TestAltValidation;
            return new IdentityClaimCollection(g);
        }

        static public IdentityClaimCollection ICCConstructNoTimeLimited()
        {
            Group g = new Group();
            g.Context = PrincipalContext.TestNoTimeLimited;
            return new IdentityClaimCollection(g);
        }        

        static public void ICCLoad(IdentityClaimCollection trackList, List<IdentityClaim> initial)
        {
            trackList.Load(initial);
        }

        static public List<IdentityClaim> ICCInserted(IdentityClaimCollection trackList)
        {
            return trackList.Inserted;
        }

        static public List<IdentityClaim> ICCRemoved(IdentityClaimCollection trackList)
        {
            return trackList.Removed;
        }

        static public List<IdentityClaim> ICCChangedValues(IdentityClaimCollection trackList, out List<IdentityClaim> rightOut)
        {
            // We'd like to just return the List<Pair<T,T>>, but Pair<T,T> isn't a public class
            List<IdentityClaim> left = new List<IdentityClaim>();
            List<IdentityClaim> right = new List<IdentityClaim>();
            List<Pair<IdentityClaim,IdentityClaim>> pairList = trackList.ChangedValues;

            foreach (Pair<IdentityClaim,IdentityClaim> pair in pairList)
            {
                left.Add(pair.Left);
                right.Add(pair.Right);
            }

            rightOut = right;
            return left;
        }

        static public bool ICCChanged(IdentityClaimCollection trackList)
        {
            return trackList.Changed;
        }

        static public void ICCResetTracking(IdentityClaimCollection trackList)
        {
            trackList.ResetTracking();
        }


        //
        // PrincipalCollection
        //
        static public Group MakeAGroup(string s)
        {
            Group g = new Group(PrincipalContext.Test);
            g.DisplayName = s;
            g.Key = new TestStoreKey(s);
            return g;
        }

        static public PrincipalCollection MakePrincipalCollection()
        {
            BookmarkableResultSet rs = PrincipalContext.Test.QueryCtx.GetGroupMembership(null, false);
            PrincipalCollection mc = new PrincipalCollection(rs, new Group());
            return mc;
        }

        static public List<Principal> MCInserted(PrincipalCollection trackList)
        {
            return trackList.Inserted;
        }

        static public List<Principal> MCRemoved(PrincipalCollection trackList)
        {
            return trackList.Removed;
        }        

        static public bool MCChanged(PrincipalCollection trackList)
        {
            return trackList.Changed;
        }

        static public void MCResetTracking(PrincipalCollection trackList)
        {
            trackList.ResetTracking();
        }

        //
        // AuthenticablePrincipal
        //
        static public User APGetNoninsertedAP()
        {
            User u = new User(PrincipalContext.Test);
            u.unpersisted = false;
            return u;
        }

        static public void APInitNested(AuthenticablePrincipal ap)
        {
            ap.LoadValueIntoProperty(PropertyNames.PwdInfoCannotChangePassword, true);
            ap.LoadValueIntoProperty(PropertyNames.AcctInfoBadLogonCount, 3);
        }

        static public void APLoadValue(AuthenticablePrincipal ap, string name, object value)
        {
            ap.LoadValueIntoProperty(name, value);
        }

        static public bool APGetChangeStatus(AuthenticablePrincipal ap, string name)
        {
            return ap.GetChangeStatusForProperty(name);
        }

        static public object APGetValue(AuthenticablePrincipal ap, string name)
        {
            return ap.GetValueForProperty(name);
        }

        static public void APResetChanges(AuthenticablePrincipal ap)
        {
            ap.ResetAllChangeStatus();
        }


        //
        // PasswordInfo
        //
        static public PasswordInfo ExtractPI(AuthenticablePrincipal ap)
        {
            Type t = typeof(AuthenticablePrincipal);

            return (PasswordInfo) t.InvokeMember(
                                        "PasswordInfo",
                                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty, 
                                        null,
                                        ap,
                                        new object[]{},
                                        CultureInfo.InvariantCulture);
        }
        
        static public void PILoadValue(PasswordInfo pi, string name, object value)
        {
            pi.LoadValueIntoProperty(name, value);
        }

        static public bool PIGetChangeStatus(PasswordInfo pi, string name)
        {
            return pi.GetChangeStatusForProperty(name);
        }

        static public object PIGetValue(PasswordInfo pi, string name)
        {
            return pi.GetValueForProperty(name);
        }

        static public void PIResetChanges(PasswordInfo pi)
        {
            pi.ResetAllChangeStatus();
        }

        //
        // AccountInfo
        //
        static public AccountInfo ExtractAI(AuthenticablePrincipal ap)
        {
            Type t = typeof(AuthenticablePrincipal);

            return (AccountInfo) t.InvokeMember(
                                        "AccountInfo",
                                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty, 
                                        null,
                                        ap,
                                        new object[]{},
                                        CultureInfo.InvariantCulture);
        }
        
        static public void AILoadValue(AccountInfo ai, string name, object value)
        {
            ai.LoadValueIntoProperty(name, value);
        }

        static public bool AIGetChangeStatus(AccountInfo ai, string name)
        {
            return ai.GetChangeStatusForProperty(name);
        }

        static public object AIGetValue(AccountInfo ai, string name)
        {
            return ai.GetValueForProperty(name);
        }

        static public void AIResetChanges(AccountInfo ai)
        {
            ai.ResetAllChangeStatus();
        }

        //
        // User
        //
        static public void UserLoadValue(User u, string name, object value)
        {
            u.LoadValueIntoProperty(name, value);
        }

        static public bool UserGetChangeStatus(User u, string name)
        {
            return u.GetChangeStatusForProperty(name);
        }

        static public object UserGetValue(User u, string name)
        {
            return u.GetValueForProperty(name);
        }

        static public void UserResetChanges(User u)
        {
            u.ResetAllChangeStatus();
        }    

        static public Group GroupGetNonInsertedGroup()
        {
            Group g = new Group(PrincipalContext.Test);
            g.unpersisted = false;
            return g;
        }

        //
        // Computer
        //
        static public void ComputerLoadValue(Computer computer, string name, object value)
        {
            computer.LoadValueIntoProperty(name, value);
        }

        static public bool ComputerGetChangeStatus(Computer computer, string name)
        {
            return computer.GetChangeStatusForProperty(name);
        }

        static public object ComputerGetValue(Computer computer, string name)
        {
            return computer.GetValueForProperty(name);
        }

        static public void ComputerResetChanges(Computer computer)
        {
            computer.ResetAllChangeStatus();
        }

        //
        // QBE
        //
        static public void BuildQbeFilter(Principal p)
        {
            PrincipalContext ctx = PrincipalContext.Test;
            ((TestStoreCtx)ctx.QueryCtx).CallBuildQbeFilterDescription(p);
        }
    }

}

#endif // TESTHOOK
