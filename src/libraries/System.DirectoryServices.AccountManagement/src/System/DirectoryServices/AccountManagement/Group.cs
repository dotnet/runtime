// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryRdnPrefix("CN")]
    public class GroupPrincipal : Principal
    {
        //
        // Public constructors
        //
        public GroupPrincipal(PrincipalContext context)
        {
            if (context == null)
                throw new ArgumentException(SR.NullArguments);

            this.ContextRaw = context;
            this.unpersisted = true;
        }

        public GroupPrincipal(PrincipalContext context, string samAccountName) : this(context)
        {
            if (samAccountName == null)
                throw new ArgumentException(SR.NullArguments);

            if (Context.ContextType != ContextType.ApplicationDirectory)
                this.SamAccountName = samAccountName;

            this.Name = samAccountName;
        }

        //
        // Public properties
        //

        // IsSecurityGroup property
        private bool _isSecurityGroup = false;          // the actual property value
        private LoadState _isSecurityGroupChanged = LoadState.NotSet;   // change-tracking

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
                if (this.unpersisted && (_isSecurityGroupChanged != LoadState.Changed))
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "Group",
                                    "Enabled: returning null, unpersisted={0}, enabledChanged={1}",
                                    this.unpersisted,
                                    _isSecurityGroupChanged);
                    return null;
                }

                return HandleGet<bool>(ref _isSecurityGroup, PropertyNames.GroupIsSecurityGroup, ref _isSecurityGroupChanged);
            }

            set
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();

                // We don't want to let them set a null value.
                if (!value.HasValue)
                    throw new ArgumentNullException("value");

                HandleSet<bool>(ref _isSecurityGroup, value.Value, ref _isSecurityGroupChanged, PropertyNames.GroupIsSecurityGroup);
            }
        }

        // GroupScope property
        private GroupScope _groupScope = System.DirectoryServices.AccountManagement.GroupScope.Local;      // the actual property value
        private LoadState _groupScopeChanged = LoadState.NotSet;              // change-tracking

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
                if (this.unpersisted && (_groupScopeChanged != LoadState.Changed))
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "Group",
                                    "GroupScope: returning null, unpersisted={0}, groupScopeChanged={1}",
                                    this.unpersisted,
                                    _groupScopeChanged);

                    return null;
                }

                return HandleGet<GroupScope>(ref _groupScope, PropertyNames.GroupGroupScope, ref _groupScopeChanged);
            }

            set
            {
                // Make sure we're not disposed or deleted.  Although HandleGet/HandleSet will check this,
                // we need to check these before we do anything else.
                CheckDisposedOrDeleted();

                // We don't want to let them set a null value.
                if (!value.HasValue)
                    throw new ArgumentNullException("value");

                HandleSet<GroupScope>(ref _groupScope, value.Value, ref _groupScopeChanged, PropertyNames.GroupGroupScope);
            }
        }

        // Members property
        private PrincipalCollection _members = null;

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

                if (_members == null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: creating fresh PrincipalCollection");

                    if (!this.unpersisted)
                    {
                        // Retrieve the members from the store.
                        // QueryCtx because when this group was retrieved, it was
                        // assigned a _specific_ context for its store
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: persisted, querying group membership");

                        BookmarkableResultSet refs = ContextRaw.QueryCtx.GetGroupMembership(this, false);
                        _members = new PrincipalCollection(refs, this);
                    }
                    else
                    {
                        // unpersisted means there's no values to retrieve from the store
                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Members: unpersisted, creating empty PrincipalCollection");
                        _members = new PrincipalCollection(new EmptySet(), this);
                    }
                }

                return _members;
            }
        }

        //
        // Public methods
        //
        public static new GroupPrincipal FindByIdentity(PrincipalContext context, string identityValue)
        {
            return (GroupPrincipal)FindByIdentityWithType(context, typeof(GroupPrincipal), identityValue);
        }

        public static new GroupPrincipal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return (GroupPrincipal)FindByIdentityWithType(context, typeof(GroupPrincipal), identityType, identityValue);
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

        private bool _disposed = false;

        public override void Dispose()
        {
            try
            {
                if (!_disposed)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "Dispose: disposing");

                    if (_members != null)
                        _members.Dispose();

                    _disposed = true;
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
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "LoadValueIntoProperty: name=" + propertyName + " value=" + (value != null ? value.ToString() : "null"));

            switch (propertyName)
            {
                case PropertyNames.GroupIsSecurityGroup:
                    _isSecurityGroup = (bool)value;
                    _isSecurityGroupChanged = LoadState.Loaded;
                    break;

                case PropertyNames.GroupGroupScope:
                    _groupScope = (GroupScope)value;
                    _groupScopeChanged = LoadState.Loaded;
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
                    return _isSecurityGroupChanged == LoadState.Changed;

                case PropertyNames.GroupGroupScope:
                    return _groupScopeChanged == LoadState.Changed;

                case PropertyNames.GroupMembers:
                    // If Members was never loaded, it couldn't possibly have changed
                    if (_members != null)
                        return _members.Changed;
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
                    return _isSecurityGroup;

                case PropertyNames.GroupGroupScope:
                    return _groupScope;

                case PropertyNames.GroupMembers:
                    return _members;

                default:
                    return base.GetValueForProperty(propertyName);
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        internal override void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Group", "ResetAllChangeStatus");

            _groupScopeChanged = (_groupScopeChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _isSecurityGroupChanged = (_isSecurityGroupChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;

            if (_members != null)
                _members.ResetTracking();

            base.ResetAllChangeStatus();
        }

        /// <summary>
        /// if isSmallGroup has a value, it means we already checked if the group is small
        /// </summary>
        private bool? _isSmallGroup;

        /// <summary>
        /// cache the search result for the member attribute
        /// it will only be set for small groups!
        /// </summary>
        internal SearchResult SmallGroupMemberSearchResult { get; private set; }

        /// <summary>
        ///  Finds if the group is "small", meaning that it has less than MaxValRange values (usually 1500)
        ///  The property list for the searcher of a group has "member" attribute. if there are more results than MaxValRange, there will also be a "member;range=..." attribute               
        ///  we can cache the result and don't fear from changes through Add/Remove/Save because the completed/pending lists are looked up before the actual values are
        /// </summary>
        internal bool IsSmallGroup()
        {
            if (_isSmallGroup.HasValue)
            {
                return _isSmallGroup.Value;
            }

            _isSmallGroup = false;

            DirectoryEntry de = (DirectoryEntry)this.UnderlyingObject;
            Debug.Assert(de != null);
            if (de != null)
            {
                using (DirectorySearcher ds = new DirectorySearcher(de, "(objectClass=*)", new string[] { "member" }, SearchScope.Base))
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
                            _isSmallGroup = true;
                            SmallGroupMemberSearchResult = sr;
                        }
                    }
                }
            }
            return _isSmallGroup.Value;
        }
    }
}
