/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    Group.cs

Abstract:

    Implements the Group class.

History:

    04-May-2004    MattRim     Created

--*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <SatisfiesLinkDemand Name="Principal" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
    [DirectoryRdnPrefix("CN")]    
    public class GroupPrincipal : Principal
    {
        //
        // Public constructors
        //
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public GroupPrincipal(PrincipalContext context)
        {
            if (context == null)
                throw new ArgumentException(StringResources.NullArguments);
            
            this.ContextRaw = context;
            this.unpersisted = true;
        }

        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        public GroupPrincipal(PrincipalContext context, string samAccountName) : this(context)
        {
            if (samAccountName == null)
                throw new ArgumentException(StringResources.NullArguments);
            
            if ( Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;
            
            this.Name = samAccountName;
            
        }


        //
        // Public properties
        //

        // IsSecurityGroup property
        bool isSecurityGroup = false;          // the actual property value
        LoadState isSecurityGroupChanged = LoadState.NotSet;   // change-tracking

        public Nullable<bool> IsSecurityGroup
        {
            get 
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();
            
                // Different stores have different defaults as to the Enabled setting
                // (AD: creates disabled by default; SAM: creates enabled by default).
                // So if the principal is unpersisted (and thus we may not know what store it's
                // going to end up in), we'll just return null unless they previously
                // set an explicit value.
                if (this.unpersisted && (this.isSecurityGroupChanged != LoadState.Changed))
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "Group",
                                    "Enabled: returning null, unpersisted={0}, enabledChanged={1}",
                                    this.unpersisted,
                                    this.isSecurityGroupChanged);
                    return null;
                }
            
                return HandleGet<bool>(ref this.isSecurityGroup, PropertyNames.GroupIsSecurityGroup, ref isSecurityGroupChanged);
            }
            
            set 
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();
            
                // We don't want to let them set a null value.
                if (!value.HasValue)
                    throw new ArgumentNullException("value");
            
                HandleSet<bool>(ref this.isSecurityGroup, value.Value, ref this.isSecurityGroupChanged, PropertyNames.GroupIsSecurityGroup);
            }
        }


        // GroupScope property
        GroupScope groupScope = System.DirectoryServices.AccountManagement.GroupScope.Local;      // the actual property value
        LoadState groupScopeChanged = LoadState.NotSet;              // change-tracking

        public Nullable<GroupScope> GroupScope
        {
            get 
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();
            
                // Different stores have different defaults for the GroupScope setting
                // (AD: Global; SAM: Local).
                // So if the principal is unpersisted (and thus we may not know what store it's
                // going to end up in), we'll just return null unless they previously
                // set an explicit value.
                if (this.unpersisted && (this.groupScopeChanged != LoadState.Changed))
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "Group",
                                    "GroupScope: returning null, unpersisted={0}, groupScopeChanged={1}",
                                    this.unpersisted,
                                    this.groupScopeChanged);
                
                    return null;
                }
            
                return HandleGet<GroupScope>(ref this.groupScope, PropertyNames.GroupGroupScope, ref groupScopeChanged);
            }
            
            set
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();
            
                // We don't want to let them set a null value.
                if (!value.HasValue)
                    throw new ArgumentNullException("value");
            
                HandleSet<GroupScope>(ref this.groupScope, value.Value, ref this.groupScopeChanged, PropertyNames.GroupGroupScope);
            }
        }


        // Members property
        PrincipalCollection members = null;

        public PrincipalCollection Members 
        {
            get
            {
                // We don't use HandleGet<T> here because we have to load in the PrincipalCollection
                // using a special procedure.  It's not loaded as part of the regular LoadIfNeeded call.
            
                // Make sure we're not disposed or deleted.
                CheckDisposedOrDeleted();

                // Check that we actually support this propery in our store
                //CheckSupportedProperty(PropertyNames.GroupMembers);
            
                if (this.members == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: creating fresh PrincipalCollection");
                
                    if (!this.unpersisted)
                    {
                        // Retrieve the members from the store.
                        // QueryCtx because when this group was retrieved, it was
                        // assigned a _specific_ context for its store
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: persisted, querying group membership");                        
                        
                        BookmarkableResultSet refs =ContextRaw.QueryCtx.GetGroupMembership(this, false);
                        this.members = new PrincipalCollection(refs, this);
                    }
                    else
                    {
                        // unpersisted means there's no values to retrieve from the store
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: unpersisted, creating empty PrincipalCollection");                        
                        this.members = new PrincipalCollection(new EmptySet(), this);
                    }
                }

                return this.members;
            }
        }
        

        //
        // Public methods
        //
        public static new GroupPrincipal  FindByIdentity(PrincipalContext context, string identityValue)
        {
            return (GroupPrincipal) FindByIdentityWithType(context, typeof(GroupPrincipal), identityValue);
        }
        
        public static new GroupPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (GroupPrincipal) FindByIdentityWithType(context, typeof(GroupPrincipal), identityType, identityValue);
        }
        

        public PrincipalSearchResult<Principal> GetMembers()
        {
            return GetMembers(false);
        }
        
        public PrincipalSearchResult<Principal> GetMembers(bool recursive)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            if (!this.unpersisted)
            {
                // Retrieve the members from the store.
                // QueryCtx because when this group was retrieved, it was
                // assigned a _specific_ context for its store
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "GetMembers: persisted, querying for members (recursive={0}", recursive);                        
                
                return new PrincipalSearchResult<Principal>(ContextRaw.QueryCtx.GetGroupMembership(this, recursive));
            }
            else
            {
                // unpersisted means there's no values to retrieve from the store
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "GetMembers: unpersisted, creating empty PrincipalSearchResult");                        
                
                return new PrincipalSearchResult<Principal>(null);
            }
        }

        bool disposed = false;

        public override void Dispose()
        {
            try
            {        
                if (!this.disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Dispose: disposing");
                
                    if (this.members != null)
                        this.members.Dispose();
                    
                    this.disposed = true;
                    GC.SuppressFinalize(this);
                }
            }
            finally
            {            
                 base.Dispose();
            }              
        }   

        //
        // Internal "constructor": Used for constructing Groups returned by a query
        //
        static internal GroupPrincipal MakeGroup(PrincipalContext ctx)
        {
            GroupPrincipal g = new GroupPrincipal(ctx);
            g.unpersisted = false;

            return g;
        }


        //
        // Load/Store implementation
        //

        //
        // Loading with query results
        //

        internal override void LoadValueIntoProperty(string propertyName, object value)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "LoadValueIntoProperty: name=" + propertyName + " value=" + ( value != null ? value.ToString() : "null" ));
        
            switch (propertyName)
            {
                case PropertyNames.GroupIsSecurityGroup:
                    this.isSecurityGroup = (bool) value;
                    this.isSecurityGroupChanged = LoadState.Loaded;
                    break;

                case PropertyNames.GroupGroupScope:
                    this.groupScope = (GroupScope) value;
                    this.groupScopeChanged = LoadState.Loaded;
                    break;

                case PropertyNames.GroupMembers:
                    Debug.Fail("Group.LoadValueIntoProperty: Trying to load Members, but Members is demand-loaded.");
                    break;

                default:
                    base.LoadValueIntoProperty(propertyName, value);
                    break;
            }
        }


        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        internal override bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "GetChangeStatusForProperty: name=" + propertyName);
        
            switch (propertyName)
            {
                case PropertyNames.GroupIsSecurityGroup:
                    return this.isSecurityGroupChanged == LoadState.Changed;

                case PropertyNames.GroupGroupScope:
                    return this.groupScopeChanged == LoadState.Changed;

                case PropertyNames.GroupMembers:
                    // If Members was never loaded, it couldn't possibly have changed
                    if (this.members != null)
                        return this.members.Changed;
                    else
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "GetChangeStatusForProperty: members was never loaded");
                        return false;
                    }

                default:
                    return base.GetChangeStatusForProperty(propertyName);
            }

        }

        // Given a property name, returns the current value for the property.
        internal override object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "GetValueForProperty: name=" + propertyName);
        
            switch (propertyName)
            {
                case PropertyNames.GroupIsSecurityGroup:
                    return this.isSecurityGroup;

                case PropertyNames.GroupGroupScope:
                    return this.groupScope;

                case PropertyNames.GroupMembers:
                    return this.members;

                default:
                    return base.GetValueForProperty(propertyName);
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "ResetAllChangeStatus");
            
            this.groupScopeChanged=  ( this.groupScopeChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;
            this.isSecurityGroupChanged=  ( this.isSecurityGroupChanged ==  LoadState.Changed ) ?  LoadState.Loaded : LoadState.NotSet;

            if (this.members != null)
                this.members.ResetTracking();

            base.ResetAllChangeStatus();
        }

        /// <summary>
        /// if isSmallGroup has a value, it means we already checked if the group is small
        /// </summary>
        private bool? isSmallGroup;

        /// <summary>
        /// cache the search result for the member attribute
        /// it will only be set for small groups!
        /// </summary>
        internal SearchResult SmallGroupMemberSearchResult {get; private set;}

        /// <summary>
        ///  Finds if the group is "small", meaning that it has less than MaxValRange values (usually 1500)
        ///  The property list for the searcher of a a group has "member" attribute. if there are more results than MaxValRange, there will also be a "member;range=..." attribute               
        ///  we can cache the result and don't fear from changes through Add/Remove/Save because the completed/pending lists are looked up before the actual values are
        /// </summary>
        internal bool IsSmallGroup()
        {            
            if (isSmallGroup.HasValue)
            {
                return isSmallGroup.Value;
            }

            isSmallGroup = false;

            DirectoryEntry de = (DirectoryEntry)this.UnderlyingObject;
            Debug.Assert(de != null);
            if (de != null)
            {
                using (DirectorySearcher ds = new DirectorySearcher(de, "(objectClass=*)", new string[]{"member"}, SearchScope.Base))
                {
                    SearchResult sr = ds.FindOne();
                    if (sr != null)
                    {
                        bool rangePropertyFound = false;
                        foreach (string propName in sr.Properties.PropertyNames)
                        {
                            if (propName.StartsWith("member;range=", StringComparison.OrdinalIgnoreCase))
                            {
                                rangePropertyFound = true;
                                break;
                            }
                        }

                        // we only consider the group "small" if there is a "member" property but no "member;range..." property
                        if (!rangePropertyFound)
                        {
                            isSmallGroup = true;
                            SmallGroupMemberSearchResult = sr;                            
                        }
                    }
                }
            }
            return isSmallGroup.Value;
        }
    }

}
