// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security {
    using System;
    using System.Threading;
    using System.Security.Util;
    using System.Collections;
    using System.IO;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Security.Policy;
    using BindingFlags = System.Reflection.BindingFlags;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [Serializable] 
    internal enum SpecialPermissionSetFlag
    {
        // These also appear in clr/src/vm/permset.h
        Regular = 0,
        NoSet = 1,
        EmptySet = 2,
        SkipVerification = 3
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public class PermissionSet : ISecurityEncodable, ICollection, IStackWalk
    {
#if _DEBUG
        internal static readonly bool debug;
#endif

        [System.Diagnostics.Conditional( "_DEBUG" )]
        private static void DEBUG_WRITE(String str) {
        #if _DEBUG
            if (debug) Console.WriteLine(str);
        #endif
         }

        [System.Diagnostics.Conditional( "_DEBUG" )]
        private static void DEBUG_COND_WRITE(bool exp, String str)
        {
        #if _DEBUG
            if (debug && (exp)) Console.WriteLine(str);
        #endif
        }

        [System.Diagnostics.Conditional( "_DEBUG" )]
        private static void DEBUG_PRINTSTACK(Exception e)
        {
        #if _DEBUG
            if (debug) Console.WriteLine((e).StackTrace);
        #endif
        }
    
        // These members are accessed from EE using their hardcoded offset.
        // Please update the PermissionSetObject in object.h if you make any changes 
        // to the fields here. !dumpobj will show the field layout

        // First the fields that are serialized x-appdomain (for perf reasons)
        private bool m_Unrestricted;
        [OptionalField(VersionAdded = 2)]
        private bool m_allPermissionsDecoded = false;

        [OptionalField(VersionAdded = 2)]
        internal TokenBasedSet m_permSet = null;

        // This is a workaround so that SQL can operate under default policy without actually
        // granting permissions in assemblies that they disallow.

        [OptionalField(VersionAdded = 2)]
        private bool m_ignoreTypeLoadFailures = false;

        // This field will be populated only for non X-AD scenarios where we create a XML-ised string of the PermissionSet
        [OptionalField(VersionAdded = 2)]
        private String m_serializedPermissionSet; 

        [NonSerialized] private bool m_CheckedForNonCas;
        [NonSerialized] private bool m_ContainsCas;
        [NonSerialized] private bool m_ContainsNonCas;

        // only used during non X-AD serialization to save the m_permSet value (which we dont want serialized)
        [NonSerialized] private TokenBasedSet m_permSetSaved;         

        // Following 4 fields are used only for serialization compat purposes: DO NOT USE THESE EVER!
#pragma warning disable 169
        private bool readableonly;    
        private TokenBasedSet m_unrestrictedPermSet;
        private TokenBasedSet m_normalPermSet;

        [OptionalField(VersionAdded = 2)]
        private bool m_canUnrestrictedOverride;
#pragma warning restore 169
        // END: Serialization-only fields

        internal static readonly PermissionSet s_fullTrust = new PermissionSet( true );

#if _DEBUG
        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            Debug.Assert(false, "PermissionSet does not support serialization on CoreCLR");
        }
#endif // _DEBUG

        internal PermissionSet()
        {
            Reset();
            m_Unrestricted = true;
        }
        
        internal PermissionSet(bool fUnrestricted)
            : this()
        {
            SetUnrestricted(fUnrestricted);
        }
        
        public PermissionSet(PermissionState state)
            : this()
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted( true );
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted( false );
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }
    
        public PermissionSet(PermissionSet permSet)
            : this()
        {
            if (permSet == null)
            {
                Reset();
                return;
            }

            m_Unrestricted = permSet.m_Unrestricted;
            m_CheckedForNonCas = permSet.m_CheckedForNonCas;
            m_ContainsCas = permSet.m_ContainsCas;
            m_ContainsNonCas = permSet.m_ContainsNonCas;
            m_ignoreTypeLoadFailures = permSet.m_ignoreTypeLoadFailures;

            if (permSet.m_permSet != null)
            {
                m_permSet = new TokenBasedSet(permSet.m_permSet);
                
                // now deep copy all permissions in set
                for (int i = m_permSet.GetStartingIndex(); i <= m_permSet.GetMaxUsedIndex(); i++)
                {
                    Object obj = m_permSet.GetItem(i);
                    IPermission perm = obj as IPermission;

                    if (perm != null)
                    {
                        m_permSet.SetItem(i, perm.Copy());
                    }
                }
            }
        }

        public virtual void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException( nameof(array) );
            Contract.EndContractBlock();
        
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);
            
            while (enumerator.MoveNext())
            {
                array.SetValue(enumerator.Current , index++ );
            }
        }
        
        
        // private constructor that doesn't create any token based sets
        private PermissionSet( Object trash, Object junk )
        {
            m_Unrestricted = false;
        }
           
        
        // Returns an object appropriate for synchronizing access to this 
        // Array.
        public virtual Object SyncRoot
        {  get { return this; } }   
        
        // Is this Array synchronized (i.e., thread-safe)?  If you want a synchronized
        // collection, you can use SyncRoot as an object to synchronize your 
        // collection with.  You could also call GetSynchronized() 
        // to get a synchronized wrapper around the Array.
        public virtual bool IsSynchronized
        {  get { return false; } }  
            
        // Is this Collection ReadOnly?
        public virtual bool IsReadOnly 
        {  get {return false; } }   

        // Reinitializes all state in PermissionSet - DO NOT null-out m_serializedPermissionSet
        internal void Reset()
        {
            m_Unrestricted = false;
            m_allPermissionsDecoded = true;
            m_permSet = null;
            
            m_ignoreTypeLoadFailures = false;

            m_CheckedForNonCas = false;
            m_ContainsCas = false;
            m_ContainsNonCas = false;
            m_permSetSaved = null;


        }

        internal void CheckSet()
        {
            if (this.m_permSet == null)
                this.m_permSet = new TokenBasedSet();
        }

        public bool IsEmpty()
        {
            if (m_Unrestricted)
                return false;

            if (m_permSet == null || m_permSet.FastIsEmpty())
                return true;

            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);

            while (enumerator.MoveNext())
            {
                IPermission perm = (IPermission)enumerator.Current;

                if (!perm.IsSubsetOf( null ))
                {
                    return false;
                }
            }

            return true;
        }

        internal bool FastIsEmpty()
        {
            if (m_Unrestricted)
                return false;

            if (m_permSet == null || m_permSet.FastIsEmpty())
                return true;

            return false;
        }            
    
        public virtual int Count
        {
            get
            {
                int count = 0;

                if (m_permSet != null)
                    count += m_permSet.GetCount();

                return count;
            }
        }

        internal IPermission GetPermission(int index)
        {
            if (m_permSet == null)
                return null;
            Object obj = m_permSet.GetItem( index );
            if (obj == null)
                return null;
            return obj as IPermission;
        }

        internal IPermission GetPermission(PermissionToken permToken)
        {
            if (permToken == null)
                return null;
                    
            return GetPermission( permToken.m_index );
        }

        internal IPermission GetPermission( IPermission perm )
        {
            if (perm == null)
                return null;

            return GetPermission(PermissionToken.GetToken( perm ));
        }

        public IPermission SetPermission(IPermission perm)
        {
            return SetPermissionImpl(perm);
        }

        // SetPermission overwrites a permission in a permissionset.
        protected virtual IPermission SetPermissionImpl(IPermission perm)
        {
            // can't get token if perm is null
            if (perm == null)
                return null;

            PermissionToken permToken = PermissionToken.GetToken(perm);

            if ((permToken.m_type & PermissionTokenType.IUnrestricted) != 0)
            {
                // SetPermission Makes the Permission "Restricted"
                m_Unrestricted = false;
            }

            CheckSet();

            IPermission currPerm = GetPermission( permToken.m_index );

            m_CheckedForNonCas = false;

            // Should we copy here?
            m_permSet.SetItem( permToken.m_index, perm );
            return perm;
        }

        public IPermission AddPermission(IPermission perm)
        {
            return AddPermissionImpl(perm);
        }

        protected virtual IPermission AddPermissionImpl(IPermission perm)
        {
            // can't get token if perm is null
            if (perm == null)
                return null;

            m_CheckedForNonCas = false;

            // If the permission set is unrestricted, then return an unrestricted instance
            // of perm.

            PermissionToken permToken = PermissionToken.GetToken(perm);

            if (this.IsUnrestricted() && ((permToken.m_type & PermissionTokenType.IUnrestricted) != 0))
            {
                Type perm_type = perm.GetType();
                Object[] objs = new Object[1];
                objs[0] = PermissionState.Unrestricted;
                return (IPermission) Activator.CreateInstance(perm_type, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, objs, null );
            }

            CheckSet();
            IPermission currPerm = GetPermission(permToken.m_index);

            // If a Permission exists in this slot, then union it with perm
            // Otherwise, just add perm.

            if (currPerm != null) {
                IPermission ip_union = currPerm.Union(perm);
                m_permSet.SetItem( permToken.m_index, ip_union );
                return ip_union;
            } else {
                // Should we copy here?
                m_permSet.SetItem( permToken.m_index, perm );
                return perm;
            }
                
        }

        private IPermission RemovePermission( int index )
        {
            IPermission perm = GetPermission(index);
            if (perm == null)
                return null;
            return (IPermission)m_permSet.RemoveItem( index ); // this cast is safe because the call to GetPermission will guarantee it is an IPermission
        }

        // Make this internal soon.
        internal void SetUnrestricted(bool unrestricted)
        {
            m_Unrestricted = unrestricted;
            if (unrestricted)
            {
                // if this is to be an unrestricted permset, null the m_permSet member
                m_permSet = null;
            }
        }

        public bool IsUnrestricted()
        {
            return m_Unrestricted;
        }

        internal enum IsSubsetOfType
        {
            Normal,
            CheckDemand,
            CheckPermitOnly,
            CheckAssertion,
        }

        internal bool IsSubsetOfHelper(PermissionSet target, IsSubsetOfType type, out IPermission firstPermThatFailed, bool ignoreNonCas)
        {
    #if _DEBUG
            if (debug)     
                DEBUG_WRITE("IsSubsetOf\n" +
                            "Other:\n" +
                            (target == null ? "<null>" : target.ToString()) +
                            "\nMe:\n" +
                            ToString());
    #endif
    
            firstPermThatFailed = null;
            if (target == null || target.FastIsEmpty())
            {
                if(this.IsEmpty())
                    return true;
                else
                {
                    firstPermThatFailed = GetFirstPerm();
                    return false;
                }
            }
            else if (this.IsUnrestricted() && !target.IsUnrestricted())
                return false;
            else if (this.m_permSet == null)
                return true;
            else
            {
                target.CheckSet();

                for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                {
                    IPermission thisPerm = this.GetPermission(i);
                    if (thisPerm == null || thisPerm.IsSubsetOf(null))
                        continue;

                    IPermission targetPerm = target.GetPermission(i);
#if _DEBUG                    
                    PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                    Debug.Assert(targetPerm == null || (token.m_type & PermissionTokenType.DontKnow) == 0, "Token not properly initialized");
#endif

                    if (target.m_Unrestricted)
                        continue;

                    // targetPerm can be null here, but that is fine since it thisPerm is a subset
                    // of empty/null then we can continue in the loop.
                    CodeAccessPermission cap = thisPerm as CodeAccessPermission;
                    if(cap == null)
                    {
                        if (!ignoreNonCas && !thisPerm.IsSubsetOf( targetPerm ))
                        {
                            firstPermThatFailed = thisPerm;
                            return false;
                        }
                    }
                    else
                    {
                        firstPermThatFailed = thisPerm;
                        switch(type)
                        {
                        case IsSubsetOfType.Normal:
                            if (!thisPerm.IsSubsetOf( targetPerm ))
                                return false;
                            break;
                        case IsSubsetOfType.CheckDemand:
                            if (!cap.CheckDemand( (CodeAccessPermission)targetPerm ))
                                return false;
                            break;
                        case IsSubsetOfType.CheckPermitOnly:
                            if (!cap.CheckPermitOnly( (CodeAccessPermission)targetPerm ))
                                return false;
                            break;
                        case IsSubsetOfType.CheckAssertion:
                            if (!cap.CheckAssert( (CodeAccessPermission)targetPerm ))
                                return false;
                            break;
                        }
                        firstPermThatFailed = null;
                    }
                }
            }

            return true;
        }

        public bool IsSubsetOf(PermissionSet target)
        {
            IPermission perm;
            return IsSubsetOfHelper(target, IsSubsetOfType.Normal, out perm, false);
        }

        internal bool CheckDemand(PermissionSet target, out IPermission firstPermThatFailed)
        {
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckDemand, out firstPermThatFailed, true);
        }

        internal bool CheckPermitOnly(PermissionSet target, out IPermission firstPermThatFailed)
        {
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckPermitOnly, out firstPermThatFailed, true);
        }

        internal bool CheckAssertion(PermissionSet target)
        {
            IPermission perm;
            return IsSubsetOfHelper(target, IsSubsetOfType.CheckAssertion, out perm, true);
        }
        
        internal bool CheckDeny(PermissionSet deniedSet, out IPermission firstPermThatFailed)
        {
            firstPermThatFailed = null;
            if (deniedSet == null || deniedSet.FastIsEmpty() || this.FastIsEmpty())
                return true;

            if(this.m_Unrestricted && deniedSet.m_Unrestricted)
                return false;

            CodeAccessPermission permThis, permThat;
            PermissionSetEnumeratorInternal enumThis = new PermissionSetEnumeratorInternal(this);

            while (enumThis.MoveNext())
            {
                permThis = enumThis.Current as CodeAccessPermission;
                if(permThis == null || permThis.IsSubsetOf(null))
                    continue; // ignore non-CAS permissions in the grant set.
                if (deniedSet.m_Unrestricted)
                {
                    firstPermThatFailed = permThis;
                    return false;
                }
                permThat = (CodeAccessPermission)deniedSet.GetPermission(enumThis.GetCurrentIndex());
                if (!permThis.CheckDeny(permThat))
                {
                    firstPermThatFailed = permThis;
                    return false;
                }
            }
            if(this.m_Unrestricted)
            {
                PermissionSetEnumeratorInternal enumThat = new PermissionSetEnumeratorInternal(deniedSet);
                while (enumThat.MoveNext())
                {
                    if(enumThat.Current is IPermission)
                        return false;
                }
            }
            return true;
        }

        internal void CheckDecoded( CodeAccessPermission demandedPerm, PermissionToken tokenDemandedPerm )
        {
            Debug.Assert( demandedPerm != null, "Expected non-null value" );

            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;

            if (tokenDemandedPerm == null)
                tokenDemandedPerm = PermissionToken.GetToken( demandedPerm );

            Debug.Assert( tokenDemandedPerm != null, "Unable to find token for demanded permission" );
        
            CheckDecoded( tokenDemandedPerm.m_index );
        }

        internal void CheckDecoded( int index )
        {
            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;

            GetPermission(index);
        }

        internal void CheckDecoded(PermissionSet demandedSet)
        {
            Debug.Assert(demandedSet != null, "Expected non-null value");

            if (this.m_allPermissionsDecoded || this.m_permSet == null)
                return;

            PermissionSetEnumeratorInternal enumerator = demandedSet.GetEnumeratorInternal();

            while (enumerator.MoveNext())
            {
                CheckDecoded(enumerator.GetCurrentIndex());
            }
        }

        internal void InplaceIntersect( PermissionSet other )
        {
            Exception savedException = null;
        
            m_CheckedForNonCas = false;
            
            if (this == other)
                return;
            
            if (other == null || other.FastIsEmpty())
            {
                // If the other is empty or null, make this empty.
                Reset();
                return;
            }

            if (this.FastIsEmpty())
                return;

            int maxMax = this.m_permSet == null ? -1 : this.m_permSet.GetMaxUsedIndex();
            int otherMax = other.m_permSet == null ? -1 : other.m_permSet.GetMaxUsedIndex();

            if (this.IsUnrestricted() && maxMax < otherMax)
            {
                maxMax = otherMax;
                this.CheckSet();
            }
                
            if (other.IsUnrestricted())
            {
                other.CheckSet();
            }
                
            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem( i );
                IPermission thisPerm = thisObj as IPermission;

                Object otherObj = other.m_permSet.GetItem( i );
                IPermission otherPerm = otherObj as IPermission;

                if (thisObj == null && otherObj == null)
                    continue;

                if (thisObj == null)
                {
                    // There is no object in <this>, so intersection is empty except for IUnrestrictedPermissions
                    if (this.IsUnrestricted())
                    {
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                this.m_permSet.SetItem( i, otherPerm.Copy() );
                                Debug.Assert( PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                            }
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (other.IsUnrestricted())
                    {
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                            if ((token.m_type & PermissionTokenType.IUnrestricted) == 0)
                                this.m_permSet.SetItem( i, null );
                        }
                    }
                    else
                    {
                        this.m_permSet.SetItem( i, null );
                    }
                }
                else
                {
                    try
                    {
                        IPermission intersectPerm;
                        if (thisPerm == null)
                            intersectPerm = otherPerm;
                        else if(otherPerm == null)
                            intersectPerm = thisPerm;
                        else
                            intersectPerm = thisPerm.Intersect( otherPerm );
                        this.m_permSet.SetItem( i, intersectPerm );
                    }
                    catch (Exception e)
                    {
                        if (savedException == null)
                            savedException = e;
                    }
                }
            }

            this.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;

            if (savedException != null)
                throw savedException;
        }

        public PermissionSet Intersect(PermissionSet other)
        {
            if (other == null || other.FastIsEmpty() || this.FastIsEmpty())
            {
                return null;
            }

            int thisMax = this.m_permSet == null ? -1 : this.m_permSet.GetMaxUsedIndex();
            int otherMax = other.m_permSet == null ? -1 : other.m_permSet.GetMaxUsedIndex();
            int minMax = thisMax < otherMax ? thisMax : otherMax;

            if (this.IsUnrestricted() && minMax < otherMax)
            {
                minMax = otherMax;
                this.CheckSet();
            }

            if (other.IsUnrestricted() && minMax < thisMax)
            {
                minMax = thisMax;
                other.CheckSet();
            }

            PermissionSet pset = new PermissionSet( false );

            if (minMax > -1)
            {
                pset.m_permSet = new TokenBasedSet();
            }

            for (int i = 0; i <= minMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem( i );
                IPermission thisPerm = thisObj as IPermission;
                Object otherObj = other.m_permSet.GetItem( i );
                IPermission otherPerm = otherObj as IPermission;

                if (thisObj == null && otherObj == null)
                    continue;

                if (thisObj == null)
                {
                    if (this.m_Unrestricted)
                    {
                        if (otherPerm != null)
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                pset.m_permSet.SetItem( i, otherPerm.Copy() );
                                Debug.Assert( PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                            }
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (other.m_Unrestricted)
                    {
                        if (thisPerm != null)
                        {
                            PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                            if ((token.m_type & PermissionTokenType.IUnrestricted) != 0)
                            {
                                pset.m_permSet.SetItem( i, thisPerm.Copy() );
                                Debug.Assert( PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                            }
                        }
                    }
                }
                else
                {
                    IPermission intersectPerm;
                    if (thisPerm == null)
                        intersectPerm = otherPerm;
                    else if(otherPerm == null)
                        intersectPerm = thisPerm;
                    else
                        intersectPerm = thisPerm.Intersect( otherPerm );
                    pset.m_permSet.SetItem( i, intersectPerm );
                    Debug.Assert( intersectPerm == null || PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                }
            }

            pset.m_Unrestricted = this.m_Unrestricted && other.m_Unrestricted;
            if (pset.FastIsEmpty())
                return null;
            else
                return pset;
        }

        internal void InplaceUnion( PermissionSet other )
        {
            // Unions the "other" PermissionSet into this one.  It can be optimized to do less copies than
            // need be done by the traditional union (and we don't have to create a new PermissionSet).
            
            if (this == other)
                return;
            
            // Quick out conditions, union doesn't change this PermissionSet
            if (other == null || other.FastIsEmpty())
                return;

            m_CheckedForNonCas = false;

            this.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;

            if (this.m_Unrestricted)
            {
                // if the result of Union is unrestricted permset, null the m_permSet member
                this.m_permSet = null;
                return;
            }


            // If we reach here, result of Union is not unrestricted
            // We have to union "normal" permission no matter what now.
            int maxMax = -1;            
            if (other.m_permSet != null)
            {
                maxMax = other.m_permSet.GetMaxUsedIndex();
                this.CheckSet();
            }
            // Save exceptions until the end
            Exception savedException = null;

            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem( i );
                IPermission thisPerm = thisObj as IPermission;

                Object otherObj = other.m_permSet.GetItem( i );
                IPermission otherPerm = otherObj as IPermission;

                if (thisObj == null && otherObj == null)
                    continue;

                if (thisObj == null)
                {
                    if (otherPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !this.m_Unrestricted)
                        {
                            this.m_permSet.SetItem( i, otherPerm.Copy() );
                        }
                    }
                }
                else if (otherObj == null)
                {
                    continue;
                }
                else
                {
                    try
                    {
                        IPermission unionPerm;
                        if(thisPerm == null)
                            unionPerm = otherPerm;
                        else if(otherPerm == null)
                            unionPerm = thisPerm;
                        else
                            unionPerm = thisPerm.Union( otherPerm );
                        this.m_permSet.SetItem( i, unionPerm );
                    }
                    catch (Exception e)
                    {
                        if (savedException == null)
                            savedException = e;
                    }
                }
            }
            
            if (savedException != null)
                throw savedException;
        }

        public PermissionSet Union(PermissionSet other)
        {
            // if other is null or empty, return a clone of myself
            if (other == null || other.FastIsEmpty())
            {
                return this.Copy();
            }
            
            if (this.FastIsEmpty())
            {
                return other.Copy();
            }

            int maxMax = -1;

            PermissionSet pset = new PermissionSet();
            pset.m_Unrestricted = this.m_Unrestricted || other.m_Unrestricted;
            if (pset.m_Unrestricted)
            {
                // if the result of Union is unrestricted permset, just return
                return pset;
            }
            
            // degenerate case where we look at both this.m_permSet and other.m_permSet
            this.CheckSet();
            other.CheckSet();
            maxMax = this.m_permSet.GetMaxUsedIndex() > other.m_permSet.GetMaxUsedIndex() ? this.m_permSet.GetMaxUsedIndex() : other.m_permSet.GetMaxUsedIndex();
            pset.m_permSet = new TokenBasedSet();



            for (int i = 0; i <= maxMax; ++i)
            {
                Object thisObj = this.m_permSet.GetItem( i );
                IPermission thisPerm = thisObj as IPermission;

                Object otherObj = other.m_permSet.GetItem( i );
                IPermission otherPerm = otherObj as IPermission;

                if (thisObj == null && otherObj == null)
                    continue;

                if (thisObj == null)
                {
                    if (otherPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !pset.m_Unrestricted)
                        {
                            pset.m_permSet.SetItem( i, otherPerm.Copy() );
                            Debug.Assert( PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                        }
                    }
                }
                else if (otherObj == null)
                {
                    if (thisPerm != null)
                    {
                        PermissionToken token = (PermissionToken)PermissionToken.s_tokenSet.GetItem( i );
                        if (((token.m_type & PermissionTokenType.IUnrestricted) == 0) || !pset.m_Unrestricted)
                        {
                            pset.m_permSet.SetItem( i, thisPerm.Copy() );
                            Debug.Assert( PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                        }
                    }
                }
                else
                {
                    IPermission unionPerm;
                    if(thisPerm == null)
                        unionPerm = otherPerm;
                    else if(otherPerm == null)
                        unionPerm = thisPerm;
                    else
                        unionPerm = thisPerm.Union( otherPerm );
                    pset.m_permSet.SetItem( i, unionPerm );
                    Debug.Assert( unionPerm == null || PermissionToken.s_tokenSet.GetItem( i ) != null, "PermissionToken should already be assigned" );
                }
            }

            return pset;
        }

        // Treating the current permission set as a grant set, and the input set as
        // a set of permissions to be denied, try to cancel out as many permissions
        // from both sets as possible. For a first cut, any granted permission that
        // is a safe subset of the corresponding denied permission can result in
        // that permission being removed from both sides.

        internal void MergeDeniedSet(PermissionSet denied)
        {
            if (denied == null || denied.FastIsEmpty() || this.FastIsEmpty())
                return;

            m_CheckedForNonCas = false;

            // Check for the unrestricted case: FastIsEmpty() will return false if the PSet is unrestricted, but has no items
            if (this.m_permSet == null || denied.m_permSet == null)
                return; //nothing can be removed

            int maxIndex = denied.m_permSet.GetMaxUsedIndex() > this.m_permSet.GetMaxUsedIndex() ? this.m_permSet.GetMaxUsedIndex() : denied.m_permSet.GetMaxUsedIndex();
            for (int i = 0; i <= maxIndex; ++i) {
                IPermission deniedPerm = denied.m_permSet.GetItem(i) as IPermission;
                if (deniedPerm == null)
                    continue;

                IPermission thisPerm = this.m_permSet.GetItem(i) as IPermission;

                if (thisPerm == null && !this.m_Unrestricted) {
                    denied.m_permSet.SetItem(i, null);
                    continue;
                }

                if (thisPerm != null && deniedPerm != null) {
                    if (thisPerm.IsSubsetOf(deniedPerm)) {
                        this.m_permSet.SetItem(i, null);
                        denied.m_permSet.SetItem(i, null);
                    }
                }
            }
        }

        // Returns true if perm is contained in this
        internal bool Contains(IPermission perm)
        {
            if (perm == null)
                return true;
            if (m_Unrestricted)
                return true;
            if (FastIsEmpty())
                return false;

            PermissionToken token = PermissionToken.GetToken(perm);
            Object thisObj = this.m_permSet.GetItem( token.m_index );
            if (thisObj == null)
                return perm.IsSubsetOf( null );

            IPermission thisPerm = GetPermission(token.m_index);
            if (thisPerm != null)
                return perm.IsSubsetOf( thisPerm );
            else
                return perm.IsSubsetOf( null );
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals( Object obj )
        {
            // Note: this method is designed to accept both PermissionSet and NamedPermissionSets.
            // It will compare them based on the values in the base type, thereby ignoring the
            // name and description of the named permission set.

            PermissionSet other = obj as PermissionSet;

            if (other == null)
                return false;

            if (this.m_Unrestricted != other.m_Unrestricted)
                return false;

            CheckSet();
            other.CheckSet();

            DecodeAllPermissions();
            other.DecodeAllPermissions();

            int maxIndex = Math.Max( this.m_permSet.GetMaxUsedIndex(), other.m_permSet.GetMaxUsedIndex() );

            for (int i = 0; i <= maxIndex; ++i)
            {
                IPermission thisPerm = (IPermission)this.m_permSet.GetItem( i );
                IPermission otherPerm = (IPermission)other.m_permSet.GetItem( i );

                if (thisPerm == null && otherPerm == null)
                {
                    continue;
                }
                else if (thisPerm == null)
                {
                    if (!otherPerm.IsSubsetOf( null ))
                        return false;
                }
                else if (otherPerm == null)
                {
                    if (!thisPerm.IsSubsetOf( null ))
                        return false;
                }
                else
                {
                    if (!thisPerm.Equals( otherPerm ))
                        return false;
                }
            }

            return true;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            int accumulator;

            accumulator = this.m_Unrestricted ? -1 : 0;

            if (this.m_permSet != null)
            {
                DecodeAllPermissions();

                int maxIndex = this.m_permSet.GetMaxUsedIndex();

                for (int i = m_permSet.GetStartingIndex(); i <= maxIndex; ++i)
                {
                    IPermission perm = (IPermission)this.m_permSet.GetItem( i );
                    if (perm != null)
                    {
                        accumulator = accumulator ^ perm.GetHashCode();
                    }
                }
            }

            return accumulator;
        }
    
        // Mark this method as requiring a security object on the caller's frame
        // so the caller won't be inlined (which would mess up stack crawling).
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Demand()
        {
            if (this.FastIsEmpty())
                return;  // demanding the empty set always passes.

            ContainsNonCodeAccessPermissions();

            if (m_ContainsCas)
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
                CodeAccessSecurityEngine.Check(GetCasOnlySet(), ref stackMark);
            }
            if (m_ContainsNonCas)
            {
                DemandNonCAS();
            }
        }

        internal void DemandNonCAS()
        {
            ContainsNonCodeAccessPermissions();

            if (m_ContainsNonCas)
            {
                if (this.m_permSet != null)
                {
                    CheckSet();
                    for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                    {
                        IPermission currPerm = GetPermission(i);
                        if (currPerm != null && !(currPerm is CodeAccessPermission))
                            currPerm.Demand();
                    }
                }
            }
        }

        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor

        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Assert() 
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.Assert(this, ref stackMark);
        }
    
        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor
    
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void Deny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.Deny(this, ref stackMark);
        }
    
        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor
    
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void PermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.PermitOnly(this, ref stackMark);
        }

        internal IPermission GetFirstPerm()
        {
            IEnumerator enumerator = GetEnumerator();
            if(!enumerator.MoveNext())
                return null;
            return enumerator.Current as IPermission;
        }

        // Returns a deep copy
        public virtual PermissionSet Copy()
        {
            return new PermissionSet(this);
        }

        internal PermissionSet CopyWithNoIdentityPermissions()
        {
            // Explicitly make a new PermissionSet, rather than copying, since we may have a
            // ReadOnlyPermissionSet which cannot have identity permissions removed from it in a true copy.
            return new PermissionSet(this);
        }

        public IEnumerator GetEnumerator()
        {
            return GetEnumeratorImpl();
        }

        protected virtual IEnumerator GetEnumeratorImpl()
        {
            return new PermissionSetEnumerator(this);
        }

        internal PermissionSetEnumeratorInternal GetEnumeratorInternal()
        {
            return new PermissionSetEnumeratorInternal(this);
        }

        private void NormalizePermissionSet()
        {
            // This function guarantees that all the permissions are placed at
            // the proper index within the token based sets.  This becomes necessary
            // since these indices are dynamically allocated based on usage order.
        
            PermissionSet permSetTemp = new PermissionSet(false);
            
            permSetTemp.m_Unrestricted = this.m_Unrestricted;

            // Move all the normal permissions to the new permission set

            if (this.m_permSet != null)
            {
                for (int i = m_permSet.GetStartingIndex(); i <= this.m_permSet.GetMaxUsedIndex(); ++i)
                {
                    Object obj = this.m_permSet.GetItem(i);
                    IPermission perm = obj as IPermission;
                    if (perm != null)
                        permSetTemp.SetPermission( perm );
                }
            }
    
            this.m_permSet = permSetTemp.m_permSet;
        }

        private void DecodeAllPermissions()
        {
            if (m_permSet == null)
            {
                m_allPermissionsDecoded = true;
                return;
            }

            int maxIndex = m_permSet.GetMaxUsedIndex();
            for (int i = 0; i <= maxIndex; ++i)
            {
                // GetPermission has the side-effect of decoding the permission in the slot
                GetPermission(i);
            }

            m_allPermissionsDecoded = true;
        }

        internal void FilterHostProtectionPermissions(HostProtectionResource fullTrustOnly, HostProtectionResource inaccessible)
        {
            HostProtectionPermission.protectedResources = fullTrustOnly;
            HostProtectionPermission hpp = (HostProtectionPermission)GetPermission(HostProtectionPermission.GetTokenIndex());
            if(hpp == null)
                return;

            HostProtectionPermission newHpp = (HostProtectionPermission)hpp.Intersect(new HostProtectionPermission(fullTrustOnly));
            if (newHpp == null)
            {
                RemovePermission(HostProtectionPermission.GetTokenIndex());
            }
            else if (newHpp.Resources != hpp.Resources)
            {
                SetPermission(newHpp);
            }
        }

        // Determines whether the permission set contains any non-code access
        // security permissions.
        public bool ContainsNonCodeAccessPermissions()
        {
            if (m_CheckedForNonCas)
                return m_ContainsNonCas;

            lock (this)
            {
                if (m_CheckedForNonCas)
                    return m_ContainsNonCas;

                m_ContainsCas = false;
                m_ContainsNonCas = false;

                if (IsUnrestricted())
                    m_ContainsCas = true;

                if (this.m_permSet != null)
                {
                    PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);

                    while (enumerator.MoveNext() && (!m_ContainsCas || !m_ContainsNonCas))
                    {
                        IPermission perm = enumerator.Current as IPermission;

                        if (perm != null)
                        {
                            if (perm is CodeAccessPermission)
                                m_ContainsCas = true;
                            else
                                m_ContainsNonCas = true;
                        }
                    }
                }

                m_CheckedForNonCas = true;
            }

            return m_ContainsNonCas;
        }
        
        // Returns a permission set containing only CAS-permissions. If possible
        // this is just the input set, otherwise a new set is allocated.
        private PermissionSet GetCasOnlySet()
        {
            if (!m_ContainsNonCas)
                return this;

            if (IsUnrestricted())
                return this;

            PermissionSet pset = new PermissionSet(false);

            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(this);

            while (enumerator.MoveNext())
            {
                IPermission perm = (IPermission)enumerator.Current;

                if (perm is CodeAccessPermission)
                    pset.AddPermission(perm);
            }

            pset.m_CheckedForNonCas = true;
            pset.m_ContainsCas = !pset.IsEmpty();
            pset.m_ContainsNonCas = false;

            return pset;
        }

        // Internal routine used by CreateSerialized to add a permission to the set
        private static void MergePermission(IPermission perm, bool separateCasFromNonCas, ref PermissionSet casPset, ref PermissionSet nonCasPset)
        {
            Debug.Assert(casPset == null || !casPset.IsReadOnly);
            Debug.Assert(nonCasPset == null || !nonCasPset.IsReadOnly);

            if (perm == null)
                return;

            if (!separateCasFromNonCas || perm is CodeAccessPermission)
            {
                if(casPset == null)
                    casPset = new PermissionSet(false);
                IPermission oldPerm = casPset.GetPermission(perm);
                IPermission unionPerm = casPset.AddPermission(perm);
                if (oldPerm != null && !oldPerm.IsSubsetOf( unionPerm ))
                    throw new NotSupportedException( Environment.GetResourceString( "NotSupported_DeclarativeUnion" ) );
            }
            else
            {
                if(nonCasPset == null)
                    nonCasPset = new PermissionSet(false);
                IPermission oldPerm = nonCasPset.GetPermission(perm);
                IPermission unionPerm = nonCasPset.AddPermission( perm );
                if (oldPerm != null && !oldPerm.IsSubsetOf( unionPerm ))
                    throw new NotSupportedException( Environment.GetResourceString( "NotSupported_DeclarativeUnion" ) );
            }
        }

        // Converts an array of SecurityAttributes to a PermissionSet
        private static byte[] CreateSerialized(Object[] attrs,
                                               bool serialize,
                                               ref byte[] nonCasBlob,
                                               out PermissionSet casPset,
                                               HostProtectionResource fullTrustOnlyResources,
                                               bool allowEmptyPermissionSets)
        {
            // Create two new (empty) sets.
            casPset = null;
            PermissionSet nonCasPset = null;

            // Most security attributes generate a single permission. The
            // PermissionSetAttribute class generates an entire permission set we
            // need to merge, however.
            for (int i = 0; i < attrs.Length; i++)
            {
#pragma warning disable 618
                Debug.Assert(i == 0 || ((SecurityAttribute)attrs[i]).m_action == ((SecurityAttribute)attrs[i - 1]).m_action, "Mixed SecurityActions");
#pragma warning restore 618
                if (attrs[i] is PermissionSetAttribute)
                {
                    PermissionSet pset = ((PermissionSetAttribute)attrs[i]).CreatePermissionSet();
                    if (pset == null)
                        throw new ArgumentException( Environment.GetResourceString( "Argument_UnableToGeneratePermissionSet" ) );

                    PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(pset);

                    while (enumerator.MoveNext())
                    {
                        IPermission perm = (IPermission)enumerator.Current;
                        MergePermission(perm, serialize, ref casPset, ref nonCasPset);
                    }

                    if(casPset == null)
                        casPset = new PermissionSet(false);
                    if (pset.IsUnrestricted())
                        casPset.SetUnrestricted(true);
                }
                else
                {
#pragma warning disable 618
                    IPermission perm = ((SecurityAttribute)attrs[i]).CreatePermission();
#pragma warning restore 618
                    MergePermission(perm, serialize, ref casPset, ref nonCasPset);
                }
            }
            Debug.Assert(serialize || nonCasPset == null, "We shouldn't separate nonCAS permissions unless fSerialize is true");

            //
            // Filter HostProtection permission.  In the VM, some optimizations are done based upon these
            // declarative permission sets being NULL if they do not exist.  When filtering the permission
            // set if we end up with an empty set, we can the permission set NULL rather than returning the
            // empty set in order to enable those optimizations.
            //
            
            if(casPset != null)
            {
                casPset.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
                casPset.ContainsNonCodeAccessPermissions(); // make sure all declarative PermissionSets are checked for non-CAS so we can just check the flag from native code
                if (allowEmptyPermissionSets && casPset.IsEmpty())
                    casPset = null;
            }
            if(nonCasPset != null)
            {
                nonCasPset.FilterHostProtectionPermissions(fullTrustOnlyResources, HostProtectionResource.None);
                nonCasPset.ContainsNonCodeAccessPermissions(); // make sure all declarative PermissionSets are checked for non-CAS so we can just check the flag from native code
                if (allowEmptyPermissionSets && nonCasPset.IsEmpty())
                    nonCasPset = null;
            }

            Debug.Assert(!serialize, "Cannot serialize permission sets on CoreCLR");
            return null;
        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void RevertAssert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAssert(ref stackMark);
        }

        internal static PermissionSet RemoveRefusedPermissionSet(PermissionSet assertSet, PermissionSet refusedSet, out bool bFailedToCompress)
        {
            Debug.Assert((assertSet == null || !assertSet.IsUnrestricted()), "Cannot be unrestricted here");
            PermissionSet retPs = null;
            bFailedToCompress = false;
            if (assertSet == null)
                return null;
            if (refusedSet != null)
            {
                if (refusedSet.IsUnrestricted())
                    return null; // we're refusing everything...cannot assert anything now.

                PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(refusedSet);
                while (enumerator.MoveNext())
                {
                    CodeAccessPermission refusedPerm = (CodeAccessPermission)enumerator.Current;
                    int i = enumerator.GetCurrentIndex();
                    if (refusedPerm != null)
                    {
                        CodeAccessPermission perm
                            = (CodeAccessPermission)assertSet.GetPermission(i);
                        try
                        {
                            if (refusedPerm.Intersect(perm) != null)
                            {
                                if (refusedPerm.Equals(perm))
                                {
                                    if (retPs == null)
                                        retPs = assertSet.Copy();
                
                                    retPs.RemovePermission(i);
                                }
                                else
                                {
                                    // Asserting a permission, part of which is already denied/refused
                                    // cannot compress this assert
                                    bFailedToCompress = true;
                                    return assertSet;
                                }
                            }
                        }
                        catch (ArgumentException)
                        {
                            // Any exception during removing a refused set from assert set => we play it safe and not assert that perm
                            if (retPs == null)
                                retPs = assertSet.Copy();
                            retPs.RemovePermission(i);
                        }
                    }
                }
            }
            if (retPs != null)
                return retPs;
            return assertSet;
        }  

        internal static void RemoveAssertedPermissionSet(PermissionSet demandSet, PermissionSet assertSet, out PermissionSet alteredDemandSet)
        {
            Debug.Assert(!assertSet.IsUnrestricted(), "Cannot call this function if assertSet is unrestricted");
            alteredDemandSet = null;
            
            PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(demandSet);
            while (enumerator.MoveNext())
            {
                CodeAccessPermission demandDerm = (CodeAccessPermission)enumerator.Current;
                int i = enumerator.GetCurrentIndex();
                if (demandDerm != null)
                {
                    CodeAccessPermission assertPerm
                        = (CodeAccessPermission)assertSet.GetPermission(i);
                    try
                    {
                        if (demandDerm.CheckAssert(assertPerm))
                        {
                            if (alteredDemandSet == null)
                                alteredDemandSet = demandSet.Copy();
            
                            alteredDemandSet.RemovePermission(i);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }
            return;
        }

        internal static bool IsIntersectingAssertedPermissions(PermissionSet assertSet1, PermissionSet assertSet2)
        {
            bool isIntersecting = false;
            if (assertSet1 != null && assertSet2 != null)
            {
                PermissionSetEnumeratorInternal enumerator = new PermissionSetEnumeratorInternal(assertSet2);
                while (enumerator.MoveNext())
                {
                    CodeAccessPermission perm2 = (CodeAccessPermission)enumerator.Current;
                    int i = enumerator.GetCurrentIndex();
                    if (perm2 != null)
                    {
                        CodeAccessPermission perm1
                            = (CodeAccessPermission)assertSet1.GetPermission(i);
                        try
                        {
                            if (perm1 != null && !perm1.Equals(perm2))
                            {
                                isIntersecting = true; // Same type of permission, but with different flags or something - cannot union them
                            }
                        }
                        catch (ArgumentException)
                        {
                            isIntersecting = true; //assume worst case
                        }
                    }
                }
            }
            return isIntersecting;
            
        }

        // This is a workaround so that SQL can operate under default policy without actually
        // granting permissions in assemblies that they disallow. 

        internal bool IgnoreTypeLoadFailures
        {
            set { m_ignoreTypeLoadFailures = value; }
        }
    }
}
