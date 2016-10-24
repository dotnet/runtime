// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Principal;

namespace System.Security.Claims
{
    /// <summary>
    /// Concrete IPrincipal supporting multiple claims-based identities
    /// </summary>
    [Serializable]
    public class ClaimsPrincipal : IPrincipal
    {
        private enum SerializationMask
        {
            None = 0,
            HasIdentities = 1,
            UserData = 2
        }

        [NonSerialized]
        private List<ClaimsIdentity> _identities = new List<ClaimsIdentity>();
        [NonSerialized]
        private byte[] _userSerializationData;

        private static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity> s_identitySelector = SelectPrimaryIdentity;
        private static Func<ClaimsPrincipal> s_principalSelector = ClaimsPrincipalSelector;

        protected ClaimsPrincipal(SerializationInfo info, StreamingContext context)
        {
            if (null == info)
            {
                throw new ArgumentNullException(nameof(info));
            }
        }

        /// <summary>
        /// This method iterates through the collection of ClaimsIdentities and chooses an identity as the primary.
        /// </summary>
        static ClaimsIdentity SelectPrimaryIdentity(IEnumerable<ClaimsIdentity> identities)
        {
            if (identities == null)
            {
                throw new ArgumentNullException(nameof(identities));
            }

            foreach (ClaimsIdentity identity in identities)
            {
                if (identity != null)
                {
                    return identity;
                }
            }

            return null;
        }

        public static Func<IEnumerable<ClaimsIdentity>, ClaimsIdentity> PrimaryIdentitySelector
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
            if (identities == null)
            {
                throw new ArgumentNullException(nameof(identities));
            }

            Contract.EndContractBlock();

            _identities.AddRange(identities);
        }

        /// <summary>
        /// Initializes an instance of <see cref="ClaimsPrincipal"/>
        /// </summary>
        /// <param name="identity"> <see cref="IIdentity"/> representing the subject in the principal. </param>
        /// <exception cref="ArgumentNullException">if 'identity' is null.</exception>
        public ClaimsPrincipal(IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            Contract.EndContractBlock();

            ClaimsIdentity ci = identity as ClaimsIdentity;
            if (ci != null)
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
            if (null == principal)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            Contract.EndContractBlock();

            //
            // If IPrincipal is a ClaimsPrincipal add all of the identities
            // If IPrincipal is not a ClaimsPrincipal, create a new identity from IPrincipal.Identity
            //
            ClaimsPrincipal cp = principal as ClaimsPrincipal;
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
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Initialize(reader);
        }

        /// <summary>
        /// Adds a single <see cref="ClaimsIdentity"/> to an internal list.
        /// </summary>
        /// <param name="identity">the <see cref="ClaimsIdentity"/>add.</param>
        /// <exception cref="ArgumentNullException">if 'identity' is null.</exception>
        public virtual void AddIdentity(ClaimsIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            Contract.EndContractBlock();

            _identities.Add(identity);
        }

        /// <summary>
        /// Adds a <see cref="IEnumerable{ClaimsIdentity}"/> to the internal list.
        /// </summary>
        /// <param name="identities">Enumeration of ClaimsIdentities to add.</param>
        /// <exception cref="ArgumentNullException">if 'identities' is null.</exception>
        public virtual void AddIdentities(IEnumerable<ClaimsIdentity> identities)
        {
            if (identities == null)
            {
                throw new ArgumentNullException(nameof(identities));
            }

            Contract.EndContractBlock();

            _identities.AddRange(identities);
        }

        /// <summary>
        /// Gets the claims as <see cref="IEnumerable{Claim}"/>, associated with this <see cref="ClaimsPrincipal"/> by enumerating all <see cref="ClaimsIdentities"/>.
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
        /// Contains any additional data provided by derived type, typically set when calling <see cref="WriteTo(BinaryWriter, byte[])"/>.</param>
        /// </summary>
        protected virtual byte[] CustomSerializationData
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
        /// Provides and extensibility point for derived types to create a custom <see cref="ClaimsIdentity"/>.
        /// </summary>
        /// <param name="reader">the <see cref="BinaryReader"/>that points at the claim.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        /// <returns>a new <see cref="ClaimsIdentity"/>.</returns>
        protected virtual ClaimsIdentity CreateClaimsIdentity(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            return new ClaimsIdentity(reader);
        }

        /// <summary>
        /// Returns the Current Principal by calling a delegate.  Users may specify the delegate.
        /// </summary>
        public static ClaimsPrincipal Current
        {
            // just accesses the current selected principal selector, doesn't set
            get
            {
                if (s_principalSelector != null)
                {
                    return s_principalSelector();
                }

                return null;
            }
        }

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each claim is matched by <param name="match"/>.
        /// </summary>
        /// <param name="match">The predicate that performs the matching logic.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>  
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindAll"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(Predicate<Claim> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            Contract.EndContractBlock();

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

        /// <summary>
        /// Retrieves a <see cref="IEnumerable{Claim}"/> where each Claim.Type equals <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the claim to match.</param>
        /// <returns>A <see cref="IEnumerable{Claim}"/> of matched claims.</returns>   
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindAll"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual IEnumerable<Claim> FindAll(string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Contract.EndContractBlock();
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

        /// <summary>
        /// Retrieves the first <see cref="Claim"/> that is matched by <param name="match"/>.
        /// </summary>
        /// <param name="match">The predicate that performs the matching logic.</param>
        /// <returns>A <see cref="Claim"/>, null if nothing matches.</returns>
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindFirst"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception> 
        public virtual Claim FindFirst(Predicate<Claim> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            Contract.EndContractBlock();

            Claim claim = null;

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
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.FindFirst"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        public virtual Claim FindFirst(string type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Contract.EndContractBlock();

            Claim claim = null;

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
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.HasClaim"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'match' is null.</exception>
        public virtual bool HasClaim(Predicate<Claim> match)
        {
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            Contract.EndContractBlock();

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
        /// <remarks>Each <see cref="ClaimsIdentity"/> is called. <seealso cref="ClaimsIdentity.HasClaim"/>.</remarks>
        /// <exception cref="ArgumentNullException">if 'type' is null.</exception>
        /// <exception cref="ArgumentNullException">if 'value' is null.</exception>
        public virtual bool HasClaim(string type, string value)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Contract.EndContractBlock();

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
        public virtual System.Security.Principal.IIdentity Identity
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
        /// Initializes from a <see cref="BinaryReader"/>. Normally the reader is initialized with the results from <see cref="WriteTo(BinaryWriter)"/>
        /// Normally the <see cref="BinaryReader"/> is initialized in the same way as the <see cref="BinaryWriter"/> passed to <see cref="WriteTo(BinaryWriter)"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="ClaimsPrincipal"/>.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        private void Initialize(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

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
                // TODO - brentschmaltz - maximum size ??
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
        protected virtual void WriteTo(BinaryWriter writer, byte[] userData)
        {

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

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

            writer.Write((Int32)mask);
            writer.Write((Int32)numberOfPropertiesWritten);
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
                writer.Write((Int32)userData.Length);
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
            if (null == info)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (_identities.Count > 0)
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_Serialization); // BinaryFormatter and WindowsIdentity would be needed
            }
        }
    }
}
