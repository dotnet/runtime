// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading;

namespace System.Security.Claims
{
    /// <summary>
    /// Concrete IPrincipal supporting multiple claims-based identities
    /// </summary>
    public class ClaimsPrincipal : IPrincipal
    {
        private enum SerializationMask
        {
            None = 0,
            HasIdentities = 1,
            UserData = 2
        }

        private readonly List<ClaimsIdentity> _identities = new List<ClaimsIdentity>();
        private readonly byte[]? _userSerializationData;

        private static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity?> s_identitySelector = SelectPrimaryIdentity;
        private static Func<ClaimsPrincipal> s_principalSelector = ClaimsPrincipalSelector;

        private static ClaimsPrincipal? SelectClaimsPrincipal()
        {
            // Diverging behavior from .NET Framework: In Framework, the default PrincipalPolicy is
            // UnauthenticatedPrincipal. In .NET Core, the default is NoPrincipal. .NET Framework
            // would throw an ArgumentNullException when constructing the ClaimsPrincipal with a
            // null principal from the thread if it were set to use NoPrincipal. In .NET Core, since
            // NoPrincipal is the default, we return null instead of throw.

            IPrincipal? threadPrincipal = Thread.CurrentPrincipal;

            return threadPrincipal switch {
                ClaimsPrincipal claimsPrincipal => claimsPrincipal,
                not null => new ClaimsPrincipal(threadPrincipal),
                null => null
            };
        }

        protected ClaimsPrincipal(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// This method iterates through the collection of ClaimsIdentities and chooses an identity as the primary.
        /// </summary>
        private static ClaimsIdentity? SelectPrimaryIdentity(IEnumerable<ClaimsIdentity> identities)
        {
            ArgumentNullException.ThrowIfNull(identities);

            foreach (ClaimsIdentity identity in identities)
            {
                if (identity != null)
                {
                    return identity;
                }
            }

            return null;
        }

        public static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity?> PrimaryIdentitySelector
        {
            get
            {
                return s_identitySelector;
            }
            set
            {
                s_identitySelector = value;
            }
        }

        public static Func<ClaimsPrincipal> ClaimsPrincipalSelector
        {
            get
            {
                return s_principalSelector;
            }
            set
            {
                s_principalSelector = value;
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/>.
        /// </summary>
        public ClaimsPrincipal()
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/>.
        /// </summary>
        /// <param name="identities"> <see cref="IEnumerable{ClaimsIdentity}"/> the subjects in the principal.</param>
        /// <exception cref="ArgumentNullException">if 'identities' is null.</exception>
        public ClaimsPrincipal(IEnumerable<ClaimsIdentity> identities)
        {
            ArgumentNullException.ThrowIfNull(identities);

            _identities.AddRange(identities);
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/>
        /// </summary>
        /// <param name="identity"> <see cref="IIdentity"/> representing the subject in the principal. </param>
        /// <exception cref="ArgumentNullException">if 'identity' is null.</exception>
        public ClaimsPrincipal(IIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            if (identity is ClaimsIdentity ci)
            {
                _identities.Add(ci);
            }
            else
            {
                _identities.Add(new ClaimsIdentity(identity));
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/>
        /// </summary>
        /// <param name="principal"><see cref="IPrincipal"/> used to form this instance.</param>
        /// <exception cref="ArgumentNullException">if 'principal' is null.</exception>
        public ClaimsPrincipal(IPrincipal principal)
        {
            ArgumentNullException.ThrowIfNull(principal);

            //
            // If IPrincipal is a ClaimsPrincipal add all of the identities
            // If IPrincipal is not a ClaimsPrincipal, create a new identity from IPrincipal.Identity
            //
            ClaimsPrincipal? cp = principal as ClaimsPrincipal;
            if (null == cp)
            {
                _identities.Add(new ClaimsIdentity(principal.Identity));
            }
            else
            {
                if (null != cp.Identities)
                {
                    _identities.AddRange(cp.Identities);
                }
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/> using a <see cref="BinaryReader"/>.
        /// Normally the <see cref="BinaryReader"/> is constructed using the bytes from <see cref="WriteTo(BinaryWriter)"/> and initialized in the same way as the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="ClaimsPrincipal"/>.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        public ClaimsPrincipal(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            SerializationMask mask = (SerializationMask)reader.ReadInt32();
            int numPropertiesToRead = reader.ReadInt32();
            int numPropertiesRead = 0;
            if ((mask & SerializationMask.HasIdentities) == SerializationMask.HasIdentities)
            {
                numPropertiesRead++;
                int numberOfIdentities = reader.ReadInt32();
                for (int index = 0; index < numberOfIdentities; ++index)
                {
                    // directly add to _identities as that is what we serialized from
                    _identities.Add(CreateClaimsIdentity(reader));
                }
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
        /// Adds a single <see cref="ClaimsIdentity"/> to an internal list.
        /// </summary>
        /// <param name="identity">the <see cref="ClaimsIdentity"/>add.</param>
        /// <exception cref="ArgumentNullException">if 'identity' is null.</exception>
        public virtual void AddIdentity(ClaimsIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            _identities.Add(identity);
        }

        /// <summary>
        /// Adds a <see cref="IEnumerable{ClaimsIdentity}"/> to the internal list.
        /// </summary>
        /// <param name="identities">Enumeration of ClaimsIdentities to add.</param>
        /// <exception cref="ArgumentNullException">if 'identities' is null.</exception>
        public virtual void AddIdentities(IEnumerable<ClaimsIdentity> identities)
        {
            ArgumentNullException.ThrowIfNull(identities);

            _identities.AddRange(identities);
        }

        /// <summary>
        /// Gets the claims as <see cref="IEnumerable{Claim}"/>, associated with this <see cref="ClaimsPrincipal"/> by enumerating all <see cref="Identities"/>.
        /// </summary>
        public virtual IEnumerable<Claim> Claims
        {
            get
            {
                foreach (ClaimsIdentity identity in Identities)
                {
                    foreach (Claim claim in identity.Claims)
                    {
                        yield return claim;
                    }
                }
            }
        }

        /// <summary>
        /// Contains any additional data provided by derived type, typically set when calling <see cref="WriteTo(BinaryWriter, byte[])"/>.
        /// </summary>
        protected virtual byte[]? CustomSerializationData
        {
            get
            {
                return _userSerializationData;
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="ClaimsPrincipal"/> with values copied from this object.
        /// </summary>
        public virtual ClaimsPrincipal Clone()
        {
            return new ClaimsPrincipal(this);
        }

        /// <summary>
        /// Provides an extensibility point for derived types to create a custom <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="reader">the <see cref="BinaryReader"/>that points at the claim.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        /// <returns>a new <see cref="ClaimsIdentity"/>.</returns>
        protected virtual ClaimsIdentity CreateClaimsIdentity(BinaryReader reader)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return new ClaimsIdentity(reader);
        }

        /// <summary>
        /// Returns the Current Principal by calling a delegate.  Users may specify the delegate.
        /// </summary>
        public static ClaimsPrincipal? Current
        {
            // just accesses the current selected principal selector, doesn't set
            get
            {
                return s_principalSelector is not null ? s_principalSelector() : SelectClaimsPrincipal();
            }
        }

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each claim is matched by <paramref name="match"/>.
        /// </summary>
        /// <param name="match">The predicate that performs the matching logic.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindAll(string)"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);
            return Core(match);

            IEnumerable<Claim> Core(Predicate<Claim> match)
            {
                foreach (ClaimsIdentity identity in Identities)
                {
                    if (identity != null)
                    {
                        foreach (Claim claim in identity.FindAll(match))
                        {
                            yield return claim;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each Claim.Type equals <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the claim to match.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindAll(Predicate{Claim})"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(string type)
        {
            ArgumentNullException.ThrowIfNull(type);
            return Core(type);

            IEnumerable<Claim> Core(string type)
            {
                foreach (ClaimsIdentity identity in Identities)
                {
                    if (identity != null)
                    {
                        foreach (Claim claim in identity.FindAll(type))
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
        /// <param name="match">The predicate that performs the matching logic.</param>
        /// <returns>A <see cref="Claim"/>, null if nothing matches.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindFirst(string)"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual Claim? FindFirst(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            Claim? claim = null;

            foreach (ClaimsIdentity identity in Identities)
            {
                if (identity != null)
                {
                    claim = identity.FindFirst(match);
                    if (claim != null)
                    {
                        return claim;
                    }
                }
            }

            return claim;
        }

        /// <summary>
        /// Retrieves the first <see cref="Claim"/> where the Claim.Type equals <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the claim to match.</param>
        /// <returns>A <see cref="Claim"/>, null if nothing matches.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindFirst(Predicate{Claim})"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual Claim? FindFirst(string type)
        {
            ArgumentNullException.ThrowIfNull(type);

            Claim? claim = null;

            for (int i = 0; i < _identities.Count; i++)
            {
                if (_identities[i] != null)
                {
                    claim = _identities[i].FindFirst(type);
                    if (claim != null)
                    {
                        return claim;
                    }
                }
            }

            return claim;
        }

        /// <summary>
        /// Determines if a claim is contained within all the ClaimsIdentities in this ClaimPrincipal.
        /// </summary>
        /// <param name="match">The predicate that performs the matching logic.</param>
        /// <returns>true if a claim is found, false otherwise.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.HasClaim(string, string)"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual bool HasClaim(Predicate<Claim> match)
        {
            ArgumentNullException.ThrowIfNull(match);

            for (int i = 0; i < _identities.Count; i++)
            {
                if (_identities[i] != null)
                {
                    if (_identities[i].HasClaim(match))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if a claim of claimType AND claimValue exists in any of the identities.
        /// </summary>
        /// <param name="type"> the type of the claim to match.</param>
        /// <param name="value"> the value of the claim to match.</param>
        /// <returns>true if a claim is matched, false otherwise.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.HasClaim(Predicate{Claim})"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
        public virtual bool HasClaim(string type, string value)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(value);

            for (int i = 0; i < _identities.Count; i++)
            {
                if (_identities[i] != null)
                {
                    if (_identities[i].HasClaim(type, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Collection of <see cref="ClaimsIdentity" />
        /// </summary>
        public virtual IEnumerable<ClaimsIdentity> Identities
        {
            get
            {
                return _identities;
            }
        }

        /// <summary>
        /// Gets the identity of the current principal.
        /// </summary>
        public virtual System.Security.Principal.IIdentity? Identity
        {
            get
            {
                if (s_identitySelector != null)
                {
                    return s_identitySelector(_identities);
                }
                else
                {
                    return SelectPrimaryIdentity(_identities);
                }
            }
        }

        /// <summary>
        /// IsInRole answers the question: does an identity this principal possesses
        /// contain a claim of type RoleClaimType where the value is '==' to the role.
        /// </summary>
        /// <param name="role">The role to check for.</param>
        /// <returns>'True' if a claim is found. Otherwise 'False'.</returns>
        /// <remarks>Each Identity has its own definition of the ClaimType that represents a role.</remarks>
        public virtual bool IsInRole(string role)
        {
            for (int i = 0; i < _identities.Count; i++)
            {
                if (_identities[i] != null)
                {
                    if (_identities[i].HasClaim(_identities[i].RoleClaimType, role))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Serializes using a <see cref="BinaryWriter"/>
        /// </summary>
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
            if (_identities.Count > 0)
            {
                mask |= SerializationMask.HasIdentities;
                numberOfPropertiesWritten++;
            }

            if (userData != null && userData.Length > 0)
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.UserData;
            }

            writer.Write((int)mask);
            writer.Write(numberOfPropertiesWritten);
            if ((mask & SerializationMask.HasIdentities) == SerializationMask.HasIdentities)
            {
                writer.Write(_identities.Count);
                foreach (var identity in _identities)
                {
                    identity.WriteTo(writer);
                }
            }

            if ((mask & SerializationMask.UserData) == SerializationMask.UserData)
            {
                writer.Write(userData!.Length);
                writer.Write(userData);
            }

            writer.Flush();
        }

        [OnSerializing]
        private void OnSerializingMethod(StreamingContext context)
        {
            if (this is ISerializable)
            {
                return;
            }

            if (_identities.Count > 0)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_Serialization); // BinaryFormatter and WindowsIdentity would be needed
            }
        }

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
