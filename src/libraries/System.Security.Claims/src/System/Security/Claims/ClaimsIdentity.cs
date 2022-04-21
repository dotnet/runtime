// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;

namespace System.Security.Claims
{
    /// <summary>
    /// An Identity that is represented by a set of claims.
    /// </summary>
    public class ClaimsIdentity : IIdentity
    {
        private enum SerializationMask
        {
            None = 0,
            AuthenticationType = 1,
            BootstrapConext = 2,
            NameClaimType = 4,
            RoleClaimType = 8,
            HasClaims = 16,
            HasLabel = 32,
            Actor = 64,
            UserData = 128,
        }

        private byte[]? _userSerializationData;
        private ClaimsIdentity? _actor;
        private string? _authenticationType;
        private object? _bootstrapContext;
        private List<List<Claim>>? _externalClaims;
        private string? _label;
        private readonly List<Claim> _instanceClaims = new List<Claim>();
        private string _nameClaimType = DefaultNameClaimType;
        private string _roleClaimType = DefaultRoleClaimType;

        public const string DefaultIssuer = @"LOCAL AUTHORITY";
        public const string DefaultNameClaimType = ClaimTypes.Name;
        public const string DefaultRoleClaimType = ClaimTypes.Role;

        // NOTE about _externalClaims.
        // GenericPrincpal and RolePrincipal set role claims here so that .IsInRole will be consistent with a 'role' claim found by querying the identity or principal.
        // _externalClaims are external to the identity and assumed to be dynamic, they not serialized or copied through Clone().
        // Access through public method: ClaimProviders.

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        public ClaimsIdentity()
            : this((IIdentity?)null, (IEnumerable<Claim>?)null, (string?)null, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity"><see cref="IIdentity"/> supplies the <see cref="Name"/> and <see cref="AuthenticationType"/>.</param>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        public ClaimsIdentity(IIdentity? identity)
            : this(identity, (IEnumerable<Claim>?)null, (string?)null, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="claims"><see cref="IEnumerable{Claim}"/> associated with this instance.</param>
        /// <remarks>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        /// </remarks>
        public ClaimsIdentity(IEnumerable<Claim>? claims)
            : this((IIdentity?)null, claims, (string?)null, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="authenticationType">The authentication method used to establish this identity.</param>
        public ClaimsIdentity(string? authenticationType)
            : this((IIdentity?)null, (IEnumerable<Claim>?)null, authenticationType, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="claims"><see cref="IEnumerable{Claim}"/> associated with this instance.</param>
        /// <param name="authenticationType">The authentication method used to establish this identity.</param>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        public ClaimsIdentity(IEnumerable<Claim>? claims, string? authenticationType)
            : this((IIdentity?)null, claims, authenticationType, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity"><see cref="IIdentity"/> supplies the <see cref="Name"/> and <see cref="AuthenticationType"/>.</param>
        /// <param name="claims"><see cref="IEnumerable{Claim}"/> associated with this instance.</param>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        public ClaimsIdentity(IIdentity? identity, IEnumerable<Claim>? claims)
            : this(identity, claims, (string?)null, (string?)null, (string?)null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="authenticationType">The type of authentication used.</param>
        /// <param name="nameType">The <see cref="Claim.Type"/> used when obtaining the value of <see cref="ClaimsIdentity.Name"/>.</param>
        /// <param name="roleType">The <see cref="Claim.Type"/> used when performing logic for <see cref="ClaimsPrincipal.IsInRole"/>.</param>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        public ClaimsIdentity(string? authenticationType, string? nameType, string? roleType)
            : this((IIdentity?)null, (IEnumerable<Claim>?)null, authenticationType, nameType, roleType)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="claims"><see cref="IEnumerable{Claim}"/> associated with this instance.</param>
        /// <param name="authenticationType">The type of authentication used.</param>
        /// <param name="nameType">The <see cref="Claim.Type"/> used when obtaining the value of <see cref="ClaimsIdentity.Name"/>.</param>
        /// <param name="roleType">The <see cref="Claim.Type"/> used when performing logic for <see cref="ClaimsPrincipal.IsInRole"/>.</param>
        /// <remarks><seealso cref="ClaimsIdentity(IIdentity, IEnumerable{Claim}, string, string, string)"/> for details on how internal values are set.</remarks>
        public ClaimsIdentity(IEnumerable<Claim>? claims, string? authenticationType, string? nameType, string? roleType)
            : this((IIdentity?)null, claims, authenticationType, nameType, roleType)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="identity"><see cref="IIdentity"/> supplies the <see cref="Name"/> and <see cref="AuthenticationType"/>.</param>
        /// <param name="claims"><see cref="IEnumerable{Claim}"/> associated with this instance.</param>
        /// <param name="authenticationType">The type of authentication used.</param>
        /// <param name="nameType">The <see cref="Claim.Type"/> used when obtaining the value of <see cref="ClaimsIdentity.Name"/>.</param>
        /// <param name="roleType">The <see cref="Claim.Type"/> used when performing logic for <see cref="ClaimsPrincipal.IsInRole"/>.</param>
        /// <remarks>If 'identity' is a <see cref="ClaimsIdentity"/>, then there are potentially multiple sources for AuthenticationType, NameClaimType, RoleClaimType.
        /// <para>Priority is given to the parameters: authenticationType, nameClaimType, roleClaimType.</para>
        /// <para>All <see cref="Claim"/>s are copied into this instance in a <see cref="List{Claim}"/>. Each Claim is examined and if Claim.Subject != this, then Claim.Clone(this) is called before the claim is added.</para>
        /// <para>Any 'External' claims are ignored.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">if 'identity' is a <see cref="ClaimsIdentity"/> and <see cref="ClaimsIdentity.Actor"/> results in a circular reference back to 'this'.</exception>
        public ClaimsIdentity(IIdentity? identity, IEnumerable<Claim>? claims, string? authenticationType, string? nameType, string? roleType)
        {
            ClaimsIdentity? claimsIdentity = identity as ClaimsIdentity;

            _authenticationType = (identity != null && string.IsNullOrEmpty(authenticationType)) ? identity.AuthenticationType : authenticationType;
            _nameClaimType = !string.IsNullOrEmpty(nameType) ? nameType : (claimsIdentity != null ? claimsIdentity._nameClaimType : DefaultNameClaimType);
            _roleClaimType = !string.IsNullOrEmpty(roleType) ? roleType : (claimsIdentity != null ? claimsIdentity._roleClaimType : DefaultRoleClaimType);

            if (claimsIdentity != null)
            {
                _label = claimsIdentity._label;
                _bootstrapContext = claimsIdentity._bootstrapContext;

                if (claimsIdentity.Actor != null)
                {
                    //
                    // Check if the Actor is circular before copying. That check is done while setting
                    // the Actor property and so not really needed here. But checking just for sanity sake
                    //
                    if (!IsCircular(claimsIdentity.Actor))
                    {
                        _actor = claimsIdentity.Actor;
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.InvalidOperationException_ActorGraphCircular);
                    }
                }
                SafeAddClaims(claimsIdentity._instanceClaims);
            }
            else
            {
                if (identity != null && !string.IsNullOrEmpty(identity.Name))
                {
                    SafeAddClaim(new Claim(_nameClaimType, identity.Name, ClaimValueTypes.String, DefaultIssuer, DefaultIssuer, this));
                }
            }

            if (claims != null)
            {
                SafeAddClaims(claims);
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/> using a <see cref="BinaryReader"/>.
        /// Normally the <see cref="BinaryReader"/> is constructed using the bytes from <see cref="WriteTo(BinaryWriter)"/> and initialized in the same way as the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="ClaimsIdentity"/>.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        public ClaimsIdentity(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            Initialize(reader);
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other"><see cref="ClaimsIdentity"/> to copy.</param>
        /// <exception cref="ArgumentNullException">if 'other' is null.</exception>
        protected ClaimsIdentity(ClaimsIdentity other)
        {
            ArgumentNullException.ThrowIfNull(other);

            if (other._actor != null)
            {
                _actor = other._actor.Clone();
            }

            _authenticationType = other._authenticationType;
            _bootstrapContext = other._bootstrapContext;
            _label = other._label;
            _nameClaimType = other._nameClaimType;
            _roleClaimType = other._roleClaimType;
            if (other._userSerializationData != null)
            {
                _userSerializationData = other._userSerializationData.Clone() as byte[];
            }

            SafeAddClaims(other._instanceClaims);
        }

        protected ClaimsIdentity(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsIdentity"/> from a serialized stream created via
        /// <see cref="ISerializable"/>.
        /// </summary>
        /// <param name="info">
        /// The <see cref="SerializationInfo"/> to read from.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown is the <paramref name="info"/> is null.</exception>
        protected ClaimsIdentity(SerializationInfo info)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Gets the authentication type that can be used to determine how this <see cref="ClaimsIdentity"/> authenticated to an authority.
        /// </summary>
        public virtual string? AuthenticationType
        {
            get { return _authenticationType; }
        }

        /// <summary>
        /// Gets a value that indicates if the user has been authenticated.
        /// </summary>
        public virtual bool IsAuthenticated
        {
            get { return !string.IsNullOrEmpty(_authenticationType); }
        }

        /// <summary>
        /// Gets or sets a <see cref="ClaimsIdentity"/> that was granted delegation rights.
        /// </summary>
        /// <exception cref="InvalidOperationException">if 'value' results in a circular reference back to 'this'.</exception>
        public ClaimsIdentity? Actor
        {
            get { return _actor; }
            set
            {
                if (value != null)
                {
                    if (IsCircular(value))
                    {
                        throw new InvalidOperationException(SR.InvalidOperationException_ActorGraphCircular);
                    }
                }
                _actor = value;
            }
        }

        /// <summary>
        /// Gets or sets a context that was used to create this <see cref="ClaimsIdentity"/>.
        /// </summary>
        public object? BootstrapContext
        {
            get { return _bootstrapContext; }
            set { _bootstrapContext = value; }
        }

        /// <summary>
        /// Gets the claims as <see cref="IEnumerable{Claim}"/>, associated with this <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <remarks>May contain nulls.</remarks>
        public virtual IEnumerable<Claim> Claims
        {
            get
            {
                if (_externalClaims == null)
                {
                    return _instanceClaims;
                }

                return CombinedClaimsIterator();
            }
        }

        private IEnumerable<Claim> CombinedClaimsIterator()
        {
            for (int i = 0; i < _instanceClaims.Count; i++)
            {
                yield return _instanceClaims[i];
            }

            for (int j = 0; j < _externalClaims!.Count; j++)
            {
                if (_externalClaims[j] != null)
                {
                    foreach (Claim claim in _externalClaims[j])
                    {
                        yield return claim;
                    }
                }
            }
        }

        /// <summary>
        /// Contains any additional data provided by a derived type, typically set when calling <see cref="WriteTo(BinaryWriter, byte[])"/>.
        /// </summary>
        protected virtual byte[]? CustomSerializationData
        {
            get
            {
                return _userSerializationData;
            }
        }

        /// <summary>
        /// Allow the association of claims with this instance of <see cref="ClaimsIdentity"/>.
        /// The claims will not be serialized or added in Clone(). They will be included in searches, finds and returned from the call to <see cref="ClaimsIdentity.Claims"/>.
        /// </summary>
        internal List<List<Claim>> ExternalClaims
        {
            get
            {
                if (_externalClaims == null)
                {
                    _externalClaims = new List<List<Claim>>();
                }
                return _externalClaims;
            }
        }

        /// <summary>
        /// Gets or sets the label for this <see cref="ClaimsIdentity"/>
        /// </summary>
        public string? Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <summary>
        /// Gets the Name of this <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <remarks>Calls <see cref="FindFirst(string)"/> where string == NameClaimType, if found, returns <see cref="Claim.Value"/> otherwise null.</remarks>
        public virtual string? Name
        {
            // just an accessor for getting the name claim
            get
            {
                Claim? claim = FindFirst(_nameClaimType);
                if (claim != null)
                {
                    return claim.Value;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the value that identifies 'Name' claims. This is used when returning the property <see cref="ClaimsIdentity.Name"/>.
        /// </summary>
        public string NameClaimType
        {
            get { return _nameClaimType; }
        }

        /// <summary>
        /// Gets the value that identifies 'Role' claims. This is used when calling <see cref="ClaimsPrincipal.IsInRole"/>.
        /// </summary>
        public string RoleClaimType
        {
            get { return _roleClaimType; }
        }

        /// <summary>
        /// Creates a new instance of <see cref="ClaimsIdentity"/> with values copied from this object.
        /// </summary>
        public virtual ClaimsIdentity Clone()
        {
            return new ClaimsIdentity(this);
        }

        /// <summary>
        /// Adds a single <see cref="Claim"/> to an internal list.
        /// </summary>
        /// <param name="claim">the <see cref="Claim"/>add.</param>
        /// <remarks>If <see cref="Claim.Subject"/> != this, then Claim.Clone(this) is called before the claim is added.</remarks>
        /// <exception cref="ArgumentNullException">if 'claim' is null.</exception>
        public virtual void AddClaim(Claim claim)
        {
            ArgumentNullException.ThrowIfNull(claim);

            if (object.ReferenceEquals(claim.Subject, this))
            {
                _instanceClaims.Add(claim);
            }
            else
            {
                _instanceClaims.Add(claim.Clone(this));
            }
        }

        /// <summary>
        /// Adds a <see cref="IEnumerable{Claim}"/> to the internal list.
        /// </summary>
        /// <param name="claims">Enumeration of claims to add.</param>
        /// <remarks>Each claim is examined and if <see cref="Claim.Subject"/> != this, then Claim.Clone(this) is called before the claim is added.</remarks>
        /// <exception cref="ArgumentNullException">if 'claims' is null.</exception>
        public virtual void AddClaims(IEnumerable<Claim?> claims)
        {
            ArgumentNullException.ThrowIfNull(claims);

            foreach (Claim? claim in claims)
            {
                if (claim == null)
                {
                    continue;
                }

                if (object.ReferenceEquals(claim.Subject, this))
                {
                    _instanceClaims.Add(claim);
                }
                else
                {
                    _instanceClaims.Add(claim.Clone(this));
                }
            }
        }

        /// <summary>
        /// Attempts to remove a <see cref="Claim"/> the internal list.
        /// </summary>
        /// <param name="claim">the <see cref="Claim"/> to match.</param>
        /// <remarks> It is possible that a <see cref="Claim"/> returned from <see cref="Claims"/> cannot be removed. This would be the case for 'External' claims that are provided by reference.
        /// <para>object.ReferenceEquals is used to 'match'.</para>
        /// </remarks>
        public virtual bool TryRemoveClaim(Claim? claim)
        {
            if (claim == null)
            {
                return false;
            }

            bool removed = false;

            for (int i = 0; i < _instanceClaims.Count; i++)
            {
                if (object.ReferenceEquals(_instanceClaims[i], claim))
                {
                    _instanceClaims.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
            return removed;
        }

        /// <summary>
        /// Removes a <see cref="Claim"/> from the internal list.
        /// </summary>
        /// <param name="claim">the <see cref="Claim"/> to match.</param>
        /// <remarks> It is possible that a <see cref="Claim"/> returned from <see cref="Claims"/> cannot be removed. This would be the case for 'External' claims that are provided by reference.
        /// <para>object.ReferenceEquals is used to 'match'.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">if 'claim' cannot be removed.</exception>
        public virtual void RemoveClaim(Claim? claim)
        {
            if (!TryRemoveClaim(claim))
            {
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_ClaimCannotBeRemoved, claim));
            }
        }

        /// <summary>
        /// Adds claims to internal list. Calling Claim.Clone if Claim.Subject != this.
        /// </summary>
        /// <param name="claims">a <see cref="IEnumerable{Claim}"/> to add to </param>
        /// <remarks>private only call from constructor, adds to internal list.</remarks>
        private void SafeAddClaims(IEnumerable<Claim?> claims)
        {
            foreach (Claim? claim in claims)
            {
                if (claim == null)
                    continue;

                if (object.ReferenceEquals(claim.Subject, this))
                {
                    _instanceClaims.Add(claim);
                }
                else
                {
                    _instanceClaims.Add(claim.Clone(this));
                }
            }
        }

        /// <summary>
        /// Adds claim to internal list. Calling Claim.Clone if Claim.Subject != this.
        /// </summary>
        /// <remarks>private only call from constructor, adds to internal list.</remarks>
        private void SafeAddClaim(Claim? claim)
        {
            if (claim == null)
                return;

            if (object.ReferenceEquals(claim.Subject, this))
            {
                _instanceClaims.Add(claim);
            }
            else
            {
                _instanceClaims.Add(claim.Clone(this));
            }
        }

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each claim is matched by <paramref name="match"/>.
        /// </summary>
        /// <param name="match">The function that performs the matching logic.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            return Core(match);

            IEnumerable<Claim> Core(Predicate<Claim> match)
            {
                foreach (Claim claim in Claims)
                {
                    if (match(claim))
                    {
                        yield return claim;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each Claim.Type equals <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the claim to match.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>
        /// <remarks>Comparison is: StringComparison.OrdinalIgnoreCase.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(string type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return Core(type);

            IEnumerable<Claim> Core(string type)
            {
                foreach (Claim claim in Claims)
                {
                    if (claim != null)
                    {
                        if (string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return claim;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the first <see cref="Claim"/> that is matched by <paramref name="match"/>.
        /// </summary>
        /// <param name="match">The function that performs the matching logic.</param>
        /// <returns>A <see cref="Claim"/>, null if nothing matches.</returns>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual Claim? FindFirst(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            foreach (Claim claim in Claims)
            {
                if (match(claim))
                {
                    return claim;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the first <see cref="Claim"/> where Claim.Type equals <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the claim to match.</param>
        /// <returns>A <see cref="Claim"/>, null if nothing matches.</returns>
        /// <remarks>Comparison is: StringComparison.OrdinalIgnoreCase.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual Claim? FindFirst(string type)
        {
            ArgumentNullException.ThrowIfNull(type);

            foreach (Claim claim in Claims)
            {
                if (claim != null)
                {
                    if (string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase))
                    {
                        return claim;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Determines if a claim is contained within this ClaimsIdentity.
        /// </summary>
        /// <param name="match">The function that performs the matching logic.</param>
        /// <returns>true if a claim is found, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual bool HasClaim(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            foreach (Claim claim in Claims)
            {
                if (match(claim))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a claim with type AND value is contained within this ClaimsIdentity.
        /// </summary>
        /// <param name="type">the type of the claim to match.</param>
        /// <param name="value">the value of the claim to match.</param>
        /// <returns>true if a claim is matched, false otherwise.</returns>
        /// <remarks>Comparison is: StringComparison.OrdinalIgnoreCase for Claim.Type, StringComparison.Ordinal for Claim.Value.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
        public virtual bool HasClaim(string type, string value)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(value);

            foreach (Claim claim in Claims)
            {
                if (claim != null
                        && string.Equals(claim.Type, type, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(claim.Value, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Initializes from a <see cref="BinaryReader"/>. Normally the reader is initialized with the results from <see cref="WriteTo(BinaryWriter)"/>
        /// Normally the <see cref="BinaryReader"/> is initialized in the same way as the <see cref="BinaryWriter"/> passed to <see cref="WriteTo(BinaryWriter)"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="ClaimsIdentity"/>.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        private void Initialize(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            SerializationMask mask = (SerializationMask)reader.ReadInt32();
            int numPropertiesRead = 0;
            int numPropertiesToRead = reader.ReadInt32();

            if ((mask & SerializationMask.AuthenticationType) == SerializationMask.AuthenticationType)
            {
                _authenticationType = reader.ReadString();
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.BootstrapConext) == SerializationMask.BootstrapConext)
            {
                _bootstrapContext = reader.ReadString();
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
            {
                _nameClaimType = reader.ReadString();
                numPropertiesRead++;
            }
            else
            {
                _nameClaimType = ClaimsIdentity.DefaultNameClaimType;
            }

            if ((mask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
            {
                _roleClaimType = reader.ReadString();
                numPropertiesRead++;
            }
            else
            {
                _roleClaimType = ClaimsIdentity.DefaultRoleClaimType;
            }

            if ((mask & SerializationMask.HasLabel) == SerializationMask.HasLabel)
            {
                _label = reader.ReadString();
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.HasClaims) == SerializationMask.HasClaims)
            {
                int numberOfClaims = reader.ReadInt32();
                for (int index = 0; index < numberOfClaims; index++)
                {
                    _instanceClaims.Add(CreateClaim(reader));
                }
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.Actor) == SerializationMask.Actor)
            {
                _actor = new ClaimsIdentity(reader);
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.UserData) == SerializationMask.UserData)
            {
                int cb = reader.ReadInt32();
                _userSerializationData = reader.ReadBytes(cb);
                numPropertiesRead++;
            }

            for (int i = numPropertiesRead; i < numPropertiesToRead; i++)
            {
                reader.ReadString();
            }
        }

        /// <summary>
        /// Provides an extensibility point for derived types to create a custom <see cref="Claim"/>.
        /// </summary>
        /// <param name="reader">the <see cref="BinaryReader"/>that points at the claim.</param>
        /// <returns>a new <see cref="Claim"/>.</returns>
        protected virtual Claim CreateClaim(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return new Claim(reader, this);
        }

        /// <summary>
        /// Serializes using a <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="writer">the <see cref="BinaryWriter"/> to use for data storage.</param>
        /// <exception cref="ArgumentNullException">if 'writer' is null.</exception>
        public virtual void WriteTo(BinaryWriter writer)
        {
            WriteTo(writer, null);
        }

        /// <summary>
        /// Serializes using a <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="writer">the <see cref="BinaryWriter"/> to use for data storage.</param>
        /// <param name="userData">additional data provided by derived type.</param>
        /// <exception cref="ArgumentNullException">if 'writer' is null.</exception>
        protected virtual void WriteTo(BinaryWriter writer, byte[]? userData)
        {
            ArgumentNullException.ThrowIfNull(writer);

            int numberOfPropertiesWritten = 0;
            var mask = SerializationMask.None;
            if (_authenticationType != null)
            {
                mask |= SerializationMask.AuthenticationType;
                numberOfPropertiesWritten++;
            }

            if (_bootstrapContext != null)
            {
                if (_bootstrapContext is string)
                {
                    mask |= SerializationMask.BootstrapConext;
                    numberOfPropertiesWritten++;
                }
            }

            if (!string.Equals(_nameClaimType, ClaimsIdentity.DefaultNameClaimType, StringComparison.Ordinal))
            {
                mask |= SerializationMask.NameClaimType;
                numberOfPropertiesWritten++;
            }

            if (!string.Equals(_roleClaimType, ClaimsIdentity.DefaultRoleClaimType, StringComparison.Ordinal))
            {
                mask |= SerializationMask.RoleClaimType;
                numberOfPropertiesWritten++;
            }

            if (!string.IsNullOrWhiteSpace(_label))
            {
                mask |= SerializationMask.HasLabel;
                numberOfPropertiesWritten++;
            }

            if (_instanceClaims.Count > 0)
            {
                mask |= SerializationMask.HasClaims;
                numberOfPropertiesWritten++;
            }

            if (_actor != null)
            {
                mask |= SerializationMask.Actor;
                numberOfPropertiesWritten++;
            }

            if (userData != null && userData.Length > 0)
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.UserData;
            }

            writer.Write((int)mask);
            writer.Write(numberOfPropertiesWritten);
            if ((mask & SerializationMask.AuthenticationType) == SerializationMask.AuthenticationType)
            {
                writer.Write(_authenticationType!);
            }

            if ((mask & SerializationMask.BootstrapConext) == SerializationMask.BootstrapConext)
            {
                writer.Write((string)_bootstrapContext!);
            }

            if ((mask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
            {
                writer.Write(_nameClaimType);
            }

            if ((mask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
            {
                writer.Write(_roleClaimType);
            }

            if ((mask & SerializationMask.HasLabel) == SerializationMask.HasLabel)
            {
                writer.Write(_label!);
            }

            if ((mask & SerializationMask.HasClaims) == SerializationMask.HasClaims)
            {
                writer.Write(_instanceClaims.Count);
                foreach (var claim in _instanceClaims)
                {
                    claim.WriteTo(writer);
                }
            }

            if ((mask & SerializationMask.Actor) == SerializationMask.Actor)
            {
                _actor!.WriteTo(writer);
            }

            if ((mask & SerializationMask.UserData) == SerializationMask.UserData)
            {
                writer.Write(userData!.Length);
                writer.Write(userData);
            }

            writer.Flush();
        }

        /// <summary>
        /// Checks if a circular reference exists to 'this'
        /// </summary>
        /// <param name="subject"></param>
        /// <returns></returns>
        private bool IsCircular(ClaimsIdentity subject)
        {
            if (ReferenceEquals(this, subject))
            {
                return true;
            }

            ClaimsIdentity currSubject = subject;

            while (currSubject.Actor != null)
            {
                if (ReferenceEquals(this, currSubject.Actor))
                {
                    return true;
                }

                currSubject = currSubject.Actor;
            }

            return false;
        }

        /// <summary>
        /// Populates the specified <see cref="SerializationInfo"/> with the serialization data for the ClaimsIdentity
        /// </summary>
        /// <param name="info">The serialization information stream to write to. Satisfies ISerializable contract.</param>
        /// <param name="context">Context for serialization. Can be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if the info parameter is null.</exception>
        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
