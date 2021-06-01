// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    [System.Diagnostics.DebuggerDisplay("Name ( {Name} )")]
    public abstract class Principal : IDisposable
    {
        //
        // Public properties
        //

        public override string ToString()
        {
            return Name;
        }

        // Context property
        public PrincipalContext Context
        {
            get
            {
                // Make sure we're not disposed or deleted.
                CheckDisposedOrDeleted();

                // The only way we can't have a PrincipalContext is if we're unpersisted
                Debug.Assert(_ctx != null || this.unpersisted == true);

                return _ctx;
            }
        }

        // ContextType property
        public ContextType ContextType
        {
            get
            {
                // Make sure we're not disposed or deleted.
                CheckDisposedOrDeleted();

                // The only way we can't have a PrincipalContext is if we're unpersisted
                Debug.Assert(_ctx != null || this.unpersisted == true);

                if (_ctx == null)
                    throw new InvalidOperationException(SR.PrincipalMustSetContextForProperty);

                return _ctx.ContextType;
            }
        }

        // Description property
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _description = null;          // the actual property value

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _descriptionChanged = LoadState.NotSet;    // change-tracking

        public string Description
        {
            get
            {
                return HandleGet<string>(ref _description, PropertyNames.PrincipalDescription, ref _descriptionChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.PrincipalDescription))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _description, value, ref _descriptionChanged, PropertyNames.PrincipalDescription);
            }
        }

        // DisplayName property

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _displayName = null;          // the actual property value

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _displayNameChanged = LoadState.NotSet;    // change-tracking

        public string DisplayName
        {
            get
            {
                return HandleGet<string>(ref _displayName, PropertyNames.PrincipalDisplayName, ref _displayNameChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.PrincipalDisplayName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _displayName, value, ref _displayNameChanged, PropertyNames.PrincipalDisplayName);
            }
        }

        //
        // Convenience wrappers for the IdentityClaims property

        // SAM Account Name
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _samName = null;          // the actual property value

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _samNameChanged = LoadState.NotSet;    // change-tracking

        public string SamAccountName
        {
            get
            {
                return HandleGet<string>(ref _samName, PropertyNames.PrincipalSamAccountName, ref _samNameChanged);
            }

            set
            {
                if (null == value || 0 == value.Length)
                    throw new ArgumentNullException(nameof(value));

                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.PrincipalSamAccountName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _samName, value, ref _samNameChanged, PropertyNames.PrincipalSamAccountName);
            }
        }

        // UPN
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _userPrincipalName = null;          // the actual property value

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _userPrincipalNameChanged = LoadState.NotSet;    // change-tracking
        public string UserPrincipalName
        {
            get
            {
                return HandleGet<string>(ref _userPrincipalName, PropertyNames.PrincipalUserPrincipalName, ref _userPrincipalNameChanged);
            }

            set
            {
                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.PrincipalUserPrincipalName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                HandleSet<string>(ref _userPrincipalName, value, ref _userPrincipalNameChanged, PropertyNames.PrincipalUserPrincipalName);
            }
        }

        // SID
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SecurityIdentifier _sid = null;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _sidChanged = LoadState.NotSet;

        public SecurityIdentifier Sid
        {
            get
            {
                return HandleGet<SecurityIdentifier>(ref _sid, PropertyNames.PrincipalSid, ref _sidChanged);
            }
        }

        // GUID
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Nullable<Guid> _guid = null;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _guidChanged = LoadState.NotSet;
        public Nullable<Guid> Guid
        {
            get
            {
                return HandleGet<Nullable<Guid>>(ref _guid, PropertyNames.PrincipalGuid, ref _guidChanged);
            }
        }

        // DistinguishedName
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _distinguishedName = null;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _distinguishedNameChanged = LoadState.NotSet;
        public string DistinguishedName
        {
            get
            {
                return HandleGet<string>(ref _distinguishedName, PropertyNames.PrincipalDistinguishedName, ref _distinguishedNameChanged);
            }
        }

        // DistinguishedName
        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _structuralObjectClass = null;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _structuralObjectClassChanged = LoadState.NotSet;

        public string StructuralObjectClass
        {
            get
            {
                return HandleGet<string>(ref _structuralObjectClass, PropertyNames.PrincipalStructuralObjectClass, ref _structuralObjectClassChanged);
            }
        }

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _name = null;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private LoadState _nameChanged = LoadState.NotSet;

        public string Name
        {
            get
            {
                // TODO Store should be mapping both to the same property already....

                // TQ Special case to map name and SamAccountNAme to same cache variable.
                // This should be removed in the future.
                // Context type could be  null for an unpersisted user.
                // Default to the original domain behavior if a context is not set.

                ContextType ct = (_ctx == null) ? ContextType.Domain : _ctx.ContextType;

                if (ct == ContextType.Machine)
                {
                    return HandleGet<string>(ref _samName, PropertyNames.PrincipalSamAccountName, ref _samNameChanged);
                }
                else
                {
                    return HandleGet<string>(ref _name, PropertyNames.PrincipalName, ref _nameChanged);
                }
            }
            set
            {
                if (null == value || 0 == value.Length)
                    throw new ArgumentNullException(nameof(value));

                if (!GetStoreCtxToUse().IsValidProperty(this, PropertyNames.PrincipalName))
                    throw new InvalidOperationException(SR.InvalidPropertyForStore);

                ContextType ct = (_ctx == null) ? ContextType.Domain : _ctx.ContextType;

                if (ct == ContextType.Machine)
                {
                    HandleSet<string>(ref _samName, value, ref _samNameChanged, PropertyNames.PrincipalSamAccountName);
                }
                else
                {
                    HandleSet<string>(ref _name, value, ref _nameChanged, PropertyNames.PrincipalName);
                }
            }
        }

        private ExtensionHelper _extensionHelper;

        [System.Diagnostics.DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ExtensionHelper ExtensionHelper
        {
            get
            {
                if (null == _extensionHelper)
                    _extensionHelper = new ExtensionHelper(this);

                return _extensionHelper;
            }
        }

        //
        // Public methods
        //

        public static Principal FindByIdentity(PrincipalContext context, string identityValue)
        {
            return FindByIdentityWithType(context, typeof(Principal), identityValue);
        }

        public static Principal FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            return FindByIdentityWithType(context, typeof(Principal), identityType, identityValue);
        }

        public void Save()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Entering Save");

            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Make sure we're not a fake principal
            CheckFakePrincipal();

            // We must have a PrincipalContext to save into.  This should always be the case, unless we're unpersisted
            // and they never set a PrincipalContext.
            if (_ctx == null)
            {
                Debug.Assert(this.unpersisted == true);
                throw new InvalidOperationException(SR.PrincipalMustSetContextForSave);
            }

            // Call the appropriate operation depending on whether this is an insert or update
            StoreCtx storeCtxToUse = GetStoreCtxToUse();
            Debug.Assert(storeCtxToUse != null);    // since we know this.ctx isn't null

            if (this.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Save: inserting principal of type {0} using {1}", this.GetType(), storeCtxToUse.GetType());
                Debug.Assert(storeCtxToUse == _ctx.ContextForType(this.GetType()));
                storeCtxToUse.Insert(this);
                this.unpersisted = false;  // once we persist, we're no longer in the unpersisted state
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Save: updating principal of type {0} using {1}", this.GetType(), storeCtxToUse.GetType());
                Debug.Assert(storeCtxToUse == _ctx.QueryCtx);
                storeCtxToUse.Update(this);
            }
        }

        public void Save(PrincipalContext context)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Entering Save(Context)");

            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Make sure we're not a fake principal
            CheckFakePrincipal();

            // We must have a PrincipalContext to save into.  This should always be the case, unless we're unpersisted
            // and they never set a PrincipalContext.
            if (context == null)
            {
                Debug.Assert(this.unpersisted == true);
                throw new InvalidOperationException(SR.NullArguments);
            }

            if (context.ContextType == ContextType.Machine || _ctx.ContextType == ContextType.Machine)
            {
                throw new InvalidOperationException(SR.SaveToNotSupportedAgainstMachineStore);
            }

            // If the user is trying to save to the same context we are already set to then just save the changes
            if (context == _ctx)
            {
                Save();
                return;
            }

            // If we already have a context set on this object then make sure the new
            // context is of the same type.
            if (context.ContextType != _ctx.ContextType)
            {
                Debug.Assert(this.unpersisted == true);
                throw new InvalidOperationException(SR.SaveToMustHaveSamecontextType);
            }

            StoreCtx originalStoreCtx = GetStoreCtxToUse();

            _ctx = context;

            // Call the appropriate operation depending on whether this is an insert or update
            StoreCtx newStoreCtx = GetStoreCtxToUse();

            Debug.Assert(newStoreCtx != null);    // since we know this.ctx isn't null
            Debug.Assert(originalStoreCtx != null);    // since we know this.ctx isn't null

            if (this.unpersisted)
            {
                // We have an unpersisted principal so we just want to create a principal in the new store.
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Save(context): inserting new principal of type {0} using {1}", this.GetType(), newStoreCtx.GetType());
                Debug.Assert(newStoreCtx == _ctx.ContextForType(this.GetType()));
                newStoreCtx.Insert(this);
                this.unpersisted = false;  // once we persist, we're no longer in the unpersisted state
            }
            else
            {
                // We have a principal that already exists.  We need to move it to the new store.
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Save(context): Moving principal of type {0} using {1}", this.GetType(), newStoreCtx.GetType());

                // we are now saving to a new store so this principal is unpersisted.
                this.unpersisted = true;

                // If the user has modified the name save away the current name so
                // if the move succeeds and the update fails we will move the item back to the original
                // store with the original name.
                bool nameModified = _nameChanged == LoadState.Changed;
                string previousName = null;

                if (nameModified)
                {
                    string newName = _name;
                    _ctx.QueryCtx.Load(this, PropertyNames.PrincipalName);
                    previousName = _name;
                    this.Name = newName;
                }

                newStoreCtx.Move(originalStoreCtx, this);

                try
                {
                    this.unpersisted = false;  // once we persist, we're no longer in the unpersisted state

                    newStoreCtx.Update(this);
                }
                catch (System.SystemException e)
                {
                    try
                    {
                        GlobalDebug.WriteLineIf(GlobalDebug.Error, "Principal", "Save(context):,  Update Failed (attempting to move back) Exception {0} ", e.Message);

                        if (nameModified)
                            this.Name = previousName;

                        originalStoreCtx.Move(newStoreCtx, this);

                        GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Move back succeeded");
                    }
                    catch (System.SystemException deleteFail)
                    {
                        // The move back failed.  Just continue we will throw the original exception below.
                        GlobalDebug.WriteLineIf(GlobalDebug.Error, "Principal", "Save(context):,  Move back Failed {0} ", deleteFail.Message);
                    }

                    if (e is System.Runtime.InteropServices.COMException)
                        throw ExceptionHelper.GetExceptionFromCOMException((System.Runtime.InteropServices.COMException)e);
                    else
                        throw;
                }
            }

            _ctx.QueryCtx = newStoreCtx;  // so Updates go to the right StoreCtx
        }

        public void Delete()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Entering Delete");

            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Make sure we're not a fake principal
            CheckFakePrincipal();

            // If we're unpersisted, nothing to delete
            if (this.unpersisted)
                throw new InvalidOperationException(SR.PrincipalCantDeleteUnpersisted);

            // Since we're not unpersisted, we must have come back from a query, and the query logic would
            // have filled in a PrincipalContext on us.
            Debug.Assert(_ctx != null);

            _ctx.QueryCtx.Delete(this);
            _isDeleted = true;
        }

        public override bool Equals(object o)
        {
            Principal that = o as Principal;

            if (that == null)
                return false;

            if (object.ReferenceEquals(this, that))
                return true;

            if ((_key != null) && (that._key != null) && (_key.Equals(that._key)))
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public object GetUnderlyingObject()
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Make sure we're not a fake principal
            CheckFakePrincipal();

            if (this.UnderlyingObject == null)
            {
                throw new InvalidOperationException(SR.PrincipalMustPersistFirst);
            }

            return this.UnderlyingObject;
        }

        public Type GetUnderlyingObjectType()
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Make sure we're not a fake principal
            CheckFakePrincipal();

            if (this.unpersisted)
            {
                // If we're unpersisted, we can't determine the native type until our PrincipalContext has been set.
                if (_ctx != null)
                {
                    return _ctx.ContextForType(this.GetType()).NativeType(this);
                }
                else
                {
                    throw new InvalidOperationException(SR.PrincipalMustSetContextForNative);
                }
            }
            else
            {
                Debug.Assert(_ctx != null);
                return _ctx.QueryCtx.NativeType(this);
            }
        }

        public PrincipalSearchResult<Principal> GetGroups()
        {
            return new PrincipalSearchResult<Principal>(GetGroupsHelper());
        }

        public PrincipalSearchResult<Principal> GetGroups(PrincipalContext contextToQuery)
        {
            if (contextToQuery == null)
                throw new ArgumentNullException(nameof(contextToQuery));

            return new PrincipalSearchResult<Principal>(GetGroupsHelper(contextToQuery));
        }

        public bool IsMemberOf(GroupPrincipal group)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return group.Members.Contains(this);
        }

        public bool IsMemberOf(PrincipalContext context, IdentityType identityType, string identityValue)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            GroupPrincipal g = GroupPrincipal.FindByIdentity(context, identityType, identityValue);

            if (g != null)
            {
                return IsMemberOf(g);
            }
            else
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Principal", "IsMemberOf(urn/urn): no matching principal");
                throw new NoMatchingPrincipalException(SR.NoMatchingGroupExceptionText);
            }
        }

        //
        // IDisposable
        //
        public virtual void Dispose()
        {
            if (!_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Dispose: disposing");

                if ((this.UnderlyingObject != null) && (this.UnderlyingObject is IDisposable))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Dispose: disposing underlying object");
                    ((IDisposable)this.UnderlyingObject).Dispose();
                }

                if ((this.UnderlyingSearchObject != null) && (this.UnderlyingSearchObject is IDisposable))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "Dispose: disposing underlying search object");
                    ((IDisposable)this.UnderlyingSearchObject).Dispose();
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        //
        //
        //
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected Principal()
        {
        }

        //------------------------------------------------
        // Protected functions for use by derived classes.
        //------------------------------------------------

        // Stores all  values from derived classes for use at attributes or search filter.
        private readonly ExtensionCache _extensionCache = new ExtensionCache();
        private LoadState _extensionCacheChanged = LoadState.NotSet;

        protected object[] ExtensionGet(string attribute)
        {
            if (null == attribute)
                throw new ArgumentException(SR.NullArguments);

            ExtensionCacheValue val;
            if (_extensionCache.TryGetValue(attribute, out val))
            {
                if (val.Filter)
                {
                    return null;
                }

                return val.Value;
            }
            else if (this.unpersisted)
            {
                return Array.Empty<object>();
            }
            else
            {
                Debug.Assert(this.GetUnderlyingObjectType() == typeof(DirectoryEntry));
                DirectoryEntry de = (DirectoryEntry)this.GetUnderlyingObject();

                int valCount = de.Properties[attribute].Count;

                if (valCount == 0)
                    return Array.Empty<object>();
                else
                {
                    object[] objectArray = new object[valCount];
                    de.Properties[attribute].CopyTo(objectArray, 0);
                    return objectArray;
                }
            }
        }

        private void ValidateExtensionObject(object value)
        {
            if (value is object[])
            {
                if (((object[])value).Length == 0)
                    throw new ArgumentException(SR.InvalidExtensionCollectionType);

                foreach (object o in (object[])value)
                {
                    if (o is ICollection)
                        throw new ArgumentException(SR.InvalidExtensionCollectionType);
                }
            }
            if (value is byte[])
            {
                if (((byte[])value).Length == 0)
                {
                    throw new ArgumentException(SR.InvalidExtensionCollectionType);
                }
            }
            else
            {
                if (value != null && value is ICollection)
                {
                    ICollection collection = (ICollection)value;
                    if (collection.Count == 0)
                        throw new ArgumentException(SR.InvalidExtensionCollectionType);
                    foreach (object o in collection)
                    {
                        if (o is ICollection)
                            throw new ArgumentException(SR.InvalidExtensionCollectionType);
                    }
                }
            }
        }

        protected void ExtensionSet(string attribute, object value)
        {
            if (null == attribute)
                throw new ArgumentException(SR.NullArguments);

            ValidateExtensionObject(value);

            if (value is object[])
                _extensionCache.properties[attribute] = new ExtensionCacheValue((object[])value);
            else
                _extensionCache.properties[attribute] = new ExtensionCacheValue(new object[] { value });

            _extensionCacheChanged = LoadState.Changed;
        }

        internal void AdvancedFilterSet(string attribute, object value, Type objectType, MatchType mt)
        {
            if (null == attribute)
                throw new ArgumentException(SR.NullArguments);

            ValidateExtensionObject(value);

            if (value is object[])
                _extensionCache.properties[attribute] = new ExtensionCacheValue((object[])value, objectType, mt);
            else
                _extensionCache.properties[attribute] = new ExtensionCacheValue(new object[] { value }, objectType, mt);

            _extensionCacheChanged = LoadState.Changed;
        }

        //
        // Internal implementation
        //

        // True indicates this is a new Principal object that has not yet been persisted to the store
        internal bool unpersisted;

        // True means our store object has been deleted
        private bool _isDeleted;

        // True means that StoreCtx.Load() has been called on this Principal to load in the values
        // of all the delay-loaded properties.
        private bool _loaded;

        // True means this principal corresponds to one of the well-known SIDs that do not have a
        // corresponding store object (e.g., NT AUTHORITY\NETWORK SERVICE).  Such principals
        // should be treated as read-only.
        internal bool fakePrincipal;

        // Directly corresponds to the Principal.PrincipalContext public property
        private PrincipalContext _ctx;

        internal bool Loaded
        {
            get
            {
                return _loaded;
            }
            set
            {
                _loaded = value;
            }
        }

        // A low-level way for derived classes to access the ctx field.  Note this is intended for internal use only,
        // hence the LinkDemand.
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected internal PrincipalContext ContextRaw
        {
            get
            { return _ctx; }

            set
            {
                // Verify that the passed context is not disposed.
                if (value != null)
                    value.CheckDisposed();
                _ctx = value;
            }
        }

        internal static Principal MakePrincipal(PrincipalContext ctx, Type principalType)
        {
            Principal p = null;

            System.Reflection.ConstructorInfo CI = principalType.GetConstructor(new Type[] { typeof(PrincipalContext) });

            if (null == CI)
            {
                throw new NotSupportedException(SR.ExtensionInvalidClassDefinitionConstructor);
            }

            p = (Principal)CI.Invoke(new object[] { ctx });

            if (null == p)
            {
                throw new NotSupportedException(SR.ExtensionInvalidClassDefinitionConstructor);
            }

            p.unpersisted = false;
            return p;
        }

        // Depending on whether we're to-be-inserted or were retrieved from a query,
        // returns the appropriate StoreCtx from the PrincipalContext that we should use for
        // all StoreCtx-related operations.
        // Returns null if no context has been set yet.
        internal StoreCtx GetStoreCtxToUse()
        {
            if (_ctx == null)
            {
                Debug.Assert(this.unpersisted == true);
                return null;
            }

            if (this.unpersisted)
            {
                return _ctx.ContextForType(this.GetType());
            }
            else
            {
                return _ctx.QueryCtx;
            }
        }

        // The underlying object (e.g., DirectoryEntry, Item) corresponding to this Principal.
        // Set by StoreCtx.GetAsPrincipal when this Principal was instantiated by it.
        // If not set, this is a unpersisted principal and StoreCtx.PushChangesToNative()
        // has not yet been called on it
        private object _underlyingObject;
        internal object UnderlyingObject
        {
            get
            {
                if (_underlyingObject != null)
                    return _underlyingObject;

                return null;
            }

            set
            {
                _underlyingObject = value;
            }
        }

        // The underlying search object (e.g., SearcResult, Item) corresponding to this Principal.
        // Set by StoreCtx.GetAsPrincipal when this Principal was instantiated by it.
        // If not set, this object was not created from a search.  We need to store the searchresult until the object is persisted because
        // we may need to load properties from it.
        private object _underlyingSearchObject;
        internal object UnderlyingSearchObject
        {
            get
            {
                if (_underlyingSearchObject != null)
                    return _underlyingSearchObject;

                return null;
            }

            set
            {
                _underlyingSearchObject = value;
            }
        }

        // Optional.  This property exists entirely for the use of the StoreCtxs.  When UnderlyingObject
        // can correspond to more than one possible principal in the store (e.g., WinFS's "multiple principals
        // per contact" model), the StoreCtx can use this to track and discern which principal in the
        // UnderlyingObject this Principal object corresponds to.  Set by GetAsPrincipal(), if set at all.
        private object _discriminant;
        internal object Discriminant
        {
            get { return _discriminant; }
            set { _discriminant = value; }
        }

        // A store-specific key, used to determine if two CLR Principal objects represent the same store principal.
        // Set by GetAsPrincipal when Principal is created from a query, or when a unpersisted Principal is persisted.
        private StoreKey _key;
        internal StoreKey Key
        {
            get { return _key; }
            set { _key = value; }
        }

        private bool _disposed;

        // Checks if the principal has been disposed or deleted, and throws an appropriate exception if it has.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected void CheckDisposedOrDeleted()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Principal", "CheckDisposedOrDeleted: accessing disposed object");
                throw new ObjectDisposedException(this.GetType().ToString());
            }

            if (_isDeleted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Principal", "CheckDisposedOrDeleted: accessing deleted object");
                throw new InvalidOperationException(SR.PrincipalDeleted);
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected static Principal FindByIdentityWithType(PrincipalContext context, Type principalType, string identityValue)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            return FindByIdentityWithTypeHelper(context, principalType, null, identityValue, DateTime.UtcNow);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Advanced)]
        protected static Principal FindByIdentityWithType(PrincipalContext context, Type principalType, IdentityType identityType, string identityValue)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (identityValue == null)
                throw new ArgumentNullException(nameof(identityValue));

            if ((identityType < IdentityType.SamAccountName) || (identityType > IdentityType.Guid))
                throw new InvalidEnumArgumentException(nameof(identityType), (int)identityType, typeof(IdentityType));

            return FindByIdentityWithTypeHelper(context, principalType, identityType, identityValue, DateTime.UtcNow);
        }

        private static Principal FindByIdentityWithTypeHelper(PrincipalContext context, Type principalType, Nullable<IdentityType> identityType, string identityValue, DateTime refDate)
        {
            // Ask the store to find a Principal based on this IdentityReference info.
            Principal p = context.QueryCtx.FindPrincipalByIdentRef(principalType, (identityType == null) ? null : (string)IdentMap.StringMap[(int)identityType, 1], identityValue, refDate);

            // Did we find a match?
            if (p != null)
            {
                // Given the native object, ask the StoreCtx to construct a Principal object for us.
                return p;
            }
            else
            {
                // No match.
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Principal", "FindByIdentityWithTypeHelper: no match");
                return null;
            }
        }

        private ResultSet GetGroupsHelper()
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Unpersisted principals are not members of any group
            if (this.unpersisted)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "GetGroupsHelper: returning empty set");
                return new EmptySet();
            }

            StoreCtx storeCtx = GetStoreCtxToUse();
            Debug.Assert(storeCtx != null);

            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "GetGroupsHelper: querying");
            ResultSet resultSet = storeCtx.GetGroupsMemberOf(this);

            return resultSet;
        }

        private ResultSet GetGroupsHelper(PrincipalContext contextToQuery)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            if (_ctx == null)
                throw new InvalidOperationException(SR.UserMustSetContextForMethod);

            StoreCtx storeCtx = GetStoreCtxToUse();
            Debug.Assert(storeCtx != null);

            return contextToQuery.QueryCtx.GetGroupsMemberOf(this, storeCtx);
        }

        // If we're the result of a query and our properties haven't been loaded yet, do so now
        // We'd like this to be marked protected AND internal, but that's not possible, so we'll settle for
        // internal and treat it as if it were also protected.
        internal void LoadIfNeeded(string principalPropertyName)
        {
            // Fake principals have nothing to load, since they have no store object.
            // Just set the loaded flag.
            if (this.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "LoadIfNeeded: not needed, fake principal");

                Debug.Assert(this.unpersisted == false);
            }
            else if (!this.unpersisted)
            {
                // We came back from a query --> our PrincipalContext must be filled in
                Debug.Assert(_ctx != null);

                GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "LoadIfNeeded: loading");

                // Just load the requested property...
                _ctx.QueryCtx.Load(this, principalPropertyName);
            }
        }

        // Checks if this is a fake principal, and throws an appropriate exception if so.
        // We'd like this to be marked protected AND internal, but that's not possible, so we'll settle for
        // internal and treat it as if it were also protected.
        internal void CheckFakePrincipal()
        {
            if (this.fakePrincipal)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "Principal", "CheckFakePrincipal: fake principal");
                throw new InvalidOperationException(SR.PrincipalNotSupportedOnFakePrincipal);
            }
        }

        // These methods implement the logic shared by all the get/set accessors for the public properties.

        // We pass currentValue by ref, even though we don't directly modify it, because if the LoadIfNeeded()
        // call causes the data to be loaded, we need to pick up the post-load value, not the (empty) value at the point
        // HandleGet<T> was called.
        //
        // We'd like this to be marked protected AND internal, but that's not possible, so we'll settle for
        // internal and treat it as if it were also protected.
        internal T HandleGet<T>(ref T currentValue, string name, ref LoadState state)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Check that we actually support this propery in our store
            //CheckSupportedProperty(name);

            if (state == LoadState.NotSet)
            {
                // Load in the value, if not yet done so
                LoadIfNeeded(name);
                state = LoadState.Loaded;
            }

            return currentValue;
        }

        // We'd like this to be marked protected AND internal, but that's not possible, so we'll settle for
        // internal and treat it as if it were also protected.
        internal void HandleSet<T>(ref T currentValue, T newValue, ref LoadState state, string name)
        {
            // Make sure we're not disposed or deleted.
            CheckDisposedOrDeleted();

            // Check that we actually support this propery in our store
            //CheckSupportedProperty(name);

            // Need to do this now so that newly-set value doesn't get overwritten by later load
            //            LoadIfNeeded(name);

            currentValue = newValue;
            state = LoadState.Changed;
        }

        //
        // Load/Store implementation
        //

        //
        // Loading with query results
        //

        // Given a property name like "Principal.DisplayName",
        // writes the value into the internal field backing that property and
        // resets the change-tracking for that property to "unchanged".
        //
        // If the property is a scalar property, then value is simply an object of the property type
        //  (e.g., a string for a string-valued property).
        // If the property is an IdentityClaimCollection property, then value must be a List<IdentityClaim>.
        // If the property is a ValueCollection<T>, then value must be a List<T>.
        // If the property is a X509Certificate2Collection, then value must be a List<byte[]>, where
        //  each byte[] is a certificate.
        // (The property can never be a PrincipalCollection, since such properties
        //  are not loaded by StoreCtx.Load()).
        // ExtensionCache is never directly loaded by the store hence it does not exist in the switch
        internal virtual void LoadValueIntoProperty(string propertyName, object value)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "LoadValueIntoProperty: name=" + propertyName + " value=" + (value == null ? "null" : value.ToString()));

            switch (propertyName)
            {
                case PropertyNames.PrincipalDisplayName:
                    _displayName = (string)value;
                    _displayNameChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalDescription:
                    _description = (string)value;
                    _descriptionChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalSamAccountName:
                    _samName = (string)value;
                    _samNameChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalUserPrincipalName:
                    _userPrincipalName = (string)value;
                    _userPrincipalNameChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalSid:
                    SecurityIdentifier SID = (SecurityIdentifier)value;
                    _sid = SID;
                    _sidChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalGuid:
                    Guid PrincipalGuid = (Guid)value;
                    _guid = PrincipalGuid;
                    _guidChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalDistinguishedName:
                    _distinguishedName = (string)value;
                    _distinguishedNameChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalStructuralObjectClass:
                    _structuralObjectClass = (string)value;
                    _structuralObjectClassChanged = LoadState.Loaded;
                    break;

                case PropertyNames.PrincipalName:
                    _name = (string)value;
                    _nameChanged = LoadState.Loaded;
                    break;

                default:
                    // If we're here, we didn't find the property.  They probably asked for a property we don't
                    // support (e.g., we're a Group, and they asked for PropertyNames.UserEmailAddress).
                    break;
            }
        }

        //
        // Getting changes to persist (or to build a query from a QBE filter)
        //

        // Given a property name, returns true if that property has changed since it was loaded, false otherwise.
        internal virtual bool GetChangeStatusForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "GetChangeStatusForProperty: name=" + propertyName);

            LoadState currentPropState = propertyName switch
            {
                PropertyNames.PrincipalDisplayName => _displayNameChanged,
                PropertyNames.PrincipalDescription => _descriptionChanged,
                PropertyNames.PrincipalSamAccountName => _samNameChanged,
                PropertyNames.PrincipalUserPrincipalName => _userPrincipalNameChanged,
                PropertyNames.PrincipalSid => _sidChanged,
                PropertyNames.PrincipalGuid => _guidChanged,
                PropertyNames.PrincipalDistinguishedName => _distinguishedNameChanged,
                PropertyNames.PrincipalStructuralObjectClass => _structuralObjectClassChanged,
                PropertyNames.PrincipalName => _nameChanged,
                PropertyNames.PrincipalExtensionCache => _extensionCacheChanged,

                // If we're here, we didn't find the property.  They probably asked for a property we don't
                // support (e.g., we're a User, and they asked for PropertyNames.GroupMembers).  Since we don't
                // have it, it didn't change.
                _ => LoadState.NotSet,
            };
            return (currentPropState == LoadState.Changed);
        }

        // Given a property name, returns the current value for the property.
        // Generally, this method is called only if GetChangeStatusForProperty indicates there are changes on the
        // property specified.
        //
        // If the property is a scalar property, the return value is an object of the property type.
        // If the property is an IdentityClaimCollection property, the return value is the IdentityClaimCollection
        // itself.
        // If the property is a ValueCollection<T>, the return value is the ValueCollection<T> itself.
        // If the property is a X509Certificate2Collection, the return value is the X509Certificate2Collection itself.
        // If the property is a PrincipalCollection, the return value is the PrincipalCollection itself.
        internal virtual object GetValueForProperty(string propertyName)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "GetValueForProperty: name=" + propertyName);

            switch (propertyName)
            {
                case PropertyNames.PrincipalDisplayName:
                    return _displayName;

                case PropertyNames.PrincipalDescription:
                    return _description;

                case PropertyNames.PrincipalSamAccountName:
                    return _samName;

                case PropertyNames.PrincipalUserPrincipalName:
                    return _userPrincipalName;

                case PropertyNames.PrincipalSid:
                    return _sid;

                case PropertyNames.PrincipalGuid:
                    return _guid;

                case PropertyNames.PrincipalDistinguishedName:
                    return _distinguishedName;

                case PropertyNames.PrincipalStructuralObjectClass:
                    return _structuralObjectClass;

                case PropertyNames.PrincipalName:
                    return _name;

                case PropertyNames.PrincipalExtensionCache:
                    return _extensionCache;

                default:
                    Debug.Fail($"Principal.GetValueForProperty: Ran off end of list looking for {propertyName}");
                    return null;
            }
        }

        // Reset all change-tracking status for all properties on the object to "unchanged".
        // This is used by StoreCtx.Insert() and StoreCtx.Update() to reset the change-tracking after they
        // have persisted all current changes to the store.
        internal virtual void ResetAllChangeStatus()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "Principal", "ResetAllChangeStatus");

            _displayNameChanged = (_displayNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _descriptionChanged = (_descriptionChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _samNameChanged = (_samNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _userPrincipalNameChanged = (_userPrincipalNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _sidChanged = (_sidChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _guidChanged = (_guidChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _distinguishedNameChanged = (_distinguishedNameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _nameChanged = (_nameChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
            _extensionCacheChanged = (_extensionCacheChanged == LoadState.Changed) ? LoadState.Loaded : LoadState.NotSet;
        }
    }
}
