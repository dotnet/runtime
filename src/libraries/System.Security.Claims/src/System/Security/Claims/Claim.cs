// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.Serialization;

namespace System.Security.Claims
{
    /// <summary>
    /// A Claim is a statement about an entity by an Issuer.
    /// A Claim consists of a Type, Value, a Subject and an Issuer.
    /// Additional properties, ValueType, Properties and OriginalIssuer help understand the claim when making decisions.
    /// </summary>
    [Serializable]
    public class Claim
    {
        private enum SerializationMask
        {
            None = 0,
            NameClaimType = 1,
            RoleClaimType = 2,
            StringType = 4,
            Issuer = 8,
            OriginalIssuerEqualsIssuer = 16,
            OriginalIssuer = 32,
            HasProperties = 64,
            UserData = 128,
        }

        [NonSerialized]
        private byte[] _userSerializationData;
        private string _issuer;
        private string _originalIssuer;
        private Dictionary<string, string> _properties = new Dictionary<string, string>();
        [NonSerialized]
        private ClaimsIdentity _subject;
        private string _type;
        private string _value;
        private string _valueType;

        /// <summary>
        /// Initializes an instance of <see cref="Claim"/> using a <see cref="BinaryReader"/>.
        /// Normally the <see cref="BinaryReader"/> is constructed using the bytes from <see cref="WriteTo(BinaryWriter)"/> and initialized in the same way as the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="Claim"/>.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        public Claim(BinaryReader reader)
            : this(reader, null)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="Claim"/> using a <see cref="BinaryReader"/>.
        /// Normally the <see cref="BinaryReader"/> is constructed using the bytes from <see cref="WriteTo(BinaryWriter)"/> and initialized in the same way as the <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="reader">a <see cref="BinaryReader"/> pointing to a <see cref="Claim"/>.</param>
        /// <param name="subject"> the value for <see cref="Claim.Subject"/>, which is the <see cref="ClaimsIdentity"/> that has these claims.</param>
        /// <exception cref="ArgumentNullException">if 'reader' is null.</exception>
        public Claim(BinaryReader reader, ClaimsIdentity subject)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            Initialize(reader, subject);
        }

        /// <summary>
        /// Creates a <see cref="Claim"/> with the specified type and value.
        /// </summary>
        /// <param name="type">The claim type.</param>
        /// <param name="value">The claim value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is null.</exception>
        /// <remarks>
        /// <see cref="Claim.Issuer"/> is set to <see cref="ClaimsIdentity.DefaultIssuer"/>,        
        /// <see cref="Claim.ValueType"/> is set to <see cref="ClaimValueTypes.String"/>, 
        /// <see cref="Claim.OriginalIssuer"/> is set to <see cref="ClaimsIdentity.DefaultIssuer"/>, and
        /// <see cref="Claim.Subject"/> is set to null.
        /// </remarks>
        /// <seealso cref="ClaimsIdentity"/>
        /// <seealso cref="ClaimTypes"/>
        /// <seealso cref="ClaimValueTypes"/>
        public Claim(string type, string value)
            : this(type, value, ClaimValueTypes.String, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, (ClaimsIdentity)null)
        {
        }

        /// <summary>
        /// Creates a <see cref="Claim"/> with the specified type, value, and value type.
        /// </summary>
        /// <param name="type">The claim type.</param>
        /// <param name="value">The claim value.</param>
        /// <param name="valueType">The claim value type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is null.</exception>
        /// <remarks>
        /// <see cref="Claim.Issuer"/> is set to <see cref="ClaimsIdentity.DefaultIssuer"/>,
        /// <see cref="Claim.OriginalIssuer"/> is set to <see cref="ClaimsIdentity.DefaultIssuer"/>,
        /// and <see cref="Claim.Subject"/> is set to null.
        /// </remarks>
        /// <seealso cref="ClaimsIdentity"/>
        /// <seealso cref="ClaimTypes"/>        
        /// <seealso cref="ClaimValueTypes"/>
        public Claim(string type, string value, string valueType)
            : this(type, value, valueType, ClaimsIdentity.DefaultIssuer, ClaimsIdentity.DefaultIssuer, (ClaimsIdentity)null)
        {
        }

        /// <summary>
        /// Creates a <see cref="Claim"/> with the specified type, value, value type, and issuer.
        /// </summary>
        /// <param name="type">The claim type.</param>
        /// <param name="value">The claim value.</param>
        /// <param name="valueType">The claim value type. If this parameter is empty or null, then <see cref="ClaimValueTypes.String"/> is used.</param>
        /// <param name="issuer">The claim issuer. If this parameter is empty or null, then <see cref="ClaimsIdentity.DefaultIssuer"/> is used.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is null.</exception>
        /// <remarks>
        /// <see cref="Claim.OriginalIssuer"/> is set to value of the <paramref name="issuer"/> parameter,
        /// <see cref="Claim.Subject"/> is set to null.
        /// </remarks>
        /// <seealso cref="ClaimsIdentity"/>
        /// <seealso cref="ClaimTypes"/>
        /// <seealso cref="ClaimValueTypes"/>
        public Claim(string type, string value, string valueType, string issuer)
            : this(type, value, valueType, issuer, issuer, (ClaimsIdentity)null)
        {
        }

        /// <summary>
        /// Creates a <see cref="Claim"/> with the specified type, value, value type, issuer and original issuer.
        /// </summary>
        /// <param name="type">The claim type.</param>
        /// <param name="value">The claim value.</param>
        /// <param name="valueType">The claim value type. If this parameter is null, then <see cref="ClaimValueTypes.String"/> is used.</param>
        /// <param name="issuer">The claim issuer. If this parameter is empty or null, then <see cref="ClaimsIdentity.DefaultIssuer"/> is used.</param>
        /// <param name="originalIssuer">The original issuer of this claim. If this parameter is empty or null, then orignalIssuer == issuer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is null.</exception>
        /// <remarks>
        /// <see cref="Claim.Subject"/> is set to null.
        /// </remarks>
        /// <seealso cref="ClaimsIdentity"/>
        /// <seealso cref="ClaimTypes"/>
        /// <seealso cref="ClaimValueTypes"/>
        public Claim(string type, string value, string valueType, string issuer, string originalIssuer)
            : this(type, value, valueType, issuer, originalIssuer, (ClaimsIdentity)null)
        {
        }

        /// <summary>
        /// Creates a <see cref="Claim"/> with the specified type, value, value type, issuer, original issuer and subject.
        /// </summary>
        /// <param name="type">The claim type.</param>
        /// <param name="value">The claim value.</param>
        /// <param name="valueType">The claim value type. If this parameter is null, then <see cref="ClaimValueTypes.String"/> is used.</param>
        /// <param name="issuer">The claim issuer. If this parameter is empty or null, then <see cref="ClaimsIdentity.DefaultIssuer"/> is used.</param>
        /// <param name="originalIssuer">The original issuer of this claim. If this parameter is empty or null, then orignalIssuer == issuer.</param>
        /// <param name="subject">The subject that this claim describes.</param>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="value"/> is null.</exception>
        /// <seealso cref="ClaimsIdentity"/>
        /// <seealso cref="ClaimTypes"/>
        /// <seealso cref="ClaimValueTypes"/>
        public Claim(string type, string value, string valueType, string issuer, string originalIssuer, ClaimsIdentity subject)
            : this(type, value, valueType, issuer, originalIssuer, subject, null, null)
        {
        }

        /// <summary>
        /// This internal constructor was added as a performance boost when adding claims that are found in the NTToken.
        /// We need to add a property value to distinguish DeviceClaims from UserClaims.
        /// </summary>
        /// <param name="propertyKey">This allows adding a property when adding a Claim.</param>
        /// <param name="propertyValue">The value associcated with the property.</param>
        internal Claim(string type, string value, string valueType, string issuer, string originalIssuer, ClaimsIdentity subject, string propertyKey, string propertyValue)
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

            _type = type;
            _value = value;
            _valueType = string.IsNullOrEmpty(valueType) ? ClaimValueTypes.String : valueType;
            _issuer = string.IsNullOrEmpty(issuer) ? ClaimsIdentity.DefaultIssuer : issuer;
            _originalIssuer = string.IsNullOrEmpty(originalIssuer) ? _issuer : originalIssuer;
            _subject = subject;

            if (propertyKey != null)
            {
                _properties[propertyKey] = propertyValue;
            }
        }

        /// <summary>
        /// Copy constructor for <see cref="Claim"/>
        /// </summary>
        /// <param name="other">the <see cref="Claim"/> to copy.</param>
        /// <remarks><see cref="Claim.Subject"/>will be set to 'null'.</remarks>
        /// <exception cref="ArgumentNullException">if 'other' is null.</exception>
        protected Claim(Claim other)
            : this(other, (other == null ? (ClaimsIdentity)null : other._subject))
        {
        }

        /// <summary>
        /// Copy constructor for <see cref="Claim"/>
        /// </summary>
        /// <param name="other">the <see cref="Claim"/> to copy.</param>
        /// <param name="subject">the <see cref="ClaimsIdentity"/> to assign to <see cref="Claim.Subject"/>.</param>
        /// <remarks><see cref="Claim.Subject"/>will be set to 'subject'.</remarks>
        /// <exception cref="ArgumentNullException">if 'other' is null.</exception>
        protected Claim(Claim other, ClaimsIdentity subject)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            _issuer = other._issuer;
            _originalIssuer = other._originalIssuer;
            _subject = subject;
            _type = other._type;
            _value = other._value;
            _valueType = other._valueType;
            foreach (var key in other._properties.Keys)
            {
                _properties.Add(key, other._properties[key]);
            }

            if (other._userSerializationData != null)
            {
                _userSerializationData = other._userSerializationData.Clone() as byte[];
            }
        }

        /// <summary>
        /// Contains any additional data provided by a derived type, typically set when calling <see cref="WriteTo(BinaryWriter, byte[])"/>.</param>
        /// </summary>
        protected virtual byte[] CustomSerializationData
        {
            get
            {
                return _userSerializationData;
            }
        }

        /// <summary>
        /// Gets the issuer of the <see cref="Claim"/>.
        /// </summary>
        public string Issuer
        {
            get { return _issuer; }
        }

        /// <summary>
        /// Gets the original issuer of the <see cref="Claim"/>.
        /// </summary>
        /// <remarks>
        /// When the <see cref="OriginalIssuer"/> differs from the <see cref="Issuer"/>, it means 
        /// that the claim was issued by the <see cref="OriginalIssuer"/> and was re-issued
        /// by the <see cref="Issuer"/>.
        /// </remarks>
        public string OriginalIssuer
        {
            get { return _originalIssuer; }
        }

        /// <summary>        
        /// Gets the collection of Properties associated with the <see cref="Claim"/>.
        /// </summary>
        public IDictionary<string, string> Properties
        {
            get
            {
                return _properties;
            }
        }

        /// <summary>
        /// Gets the subject of the <see cref="Claim"/>.
        /// </summary>
        public ClaimsIdentity Subject
        {
            get { return _subject; }
            internal set { _subject = value; }
        }

        /// <summary>
        /// Gets the claim type of the <see cref="Claim"/>.
        /// <seealso cref="ClaimTypes"/>.
        /// </summary>
        public string Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the value of the <see cref="Claim"/>.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <summary>
        /// Gets the value type of the <see cref="Claim"/>.
        /// <seealso cref="ClaimValueTypes"/>
        /// </summary>
        public string ValueType
        {
            get { return _valueType; }
        }

        /// <summary>
        /// Creates a new instance <see cref="Claim"/> with values copied from this object.
        /// </summary>
        public virtual Claim Clone()
        {
            return Clone((ClaimsIdentity)null);
        }

        /// <summary>
        /// Creates a new instance <see cref="Claim"/> with values copied from this object.
        /// </summary>
        /// <param name="identity">the value for <see cref="Claim.Subject"/>, which is the <see cref="ClaimsIdentity"/> that has these claims.
        /// <remarks><see cref="Claim.Subject"/> will be set to 'identity'.</remarks>
        public virtual Claim Clone(ClaimsIdentity identity)
        {
            return new Claim(this, identity);
        }

        private void Initialize(BinaryReader reader, ClaimsIdentity subject)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            _subject = subject;

            SerializationMask mask = (SerializationMask)reader.ReadInt32();
            int numPropertiesRead = 1;
            int numPropertiesToRead = reader.ReadInt32();
            _value = reader.ReadString();

            if ((mask & SerializationMask.NameClaimType) == SerializationMask.NameClaimType)
            {
                _type = ClaimsIdentity.DefaultNameClaimType;
            }
            else if ((mask & SerializationMask.RoleClaimType) == SerializationMask.RoleClaimType)
            {
                _type = ClaimsIdentity.DefaultRoleClaimType;
            }
            else
            {
                _type = reader.ReadString();
                numPropertiesRead++;
            }

            if ((mask & SerializationMask.StringType) == SerializationMask.StringType)
            {
                _valueType = reader.ReadString();
                numPropertiesRead++;
            }
            else
            {
                _valueType = ClaimValueTypes.String;
            }

            if ((mask & SerializationMask.Issuer) == SerializationMask.Issuer)
            {
                _issuer = reader.ReadString();
                numPropertiesRead++;
            }
            else
            {
                _issuer = ClaimsIdentity.DefaultIssuer;
            }

            if ((mask & SerializationMask.OriginalIssuerEqualsIssuer) == SerializationMask.OriginalIssuerEqualsIssuer)
            {
                _originalIssuer = _issuer;
            }
            else if ((mask & SerializationMask.OriginalIssuer) == SerializationMask.OriginalIssuer)
            {
                _originalIssuer = reader.ReadString();
                numPropertiesRead++;
            }
            else
            {
                _originalIssuer = ClaimsIdentity.DefaultIssuer;
            }

            if ((mask & SerializationMask.HasProperties) == SerializationMask.HasProperties)
            {
                // TODO - brentsch, maximum # of properties
                int numProperties = reader.ReadInt32();
                for (int i = 0; i < numProperties; i++)
                {
                    Properties.Add(reader.ReadString(), reader.ReadString());
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
        protected virtual void WriteTo(BinaryWriter writer, byte[] userData)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            // TODO - brentsch, given that there may be a bunch of NameClaims and or RoleClaims, we should account for this.
            // This would require coordination between ClaimsIdentity and Claim since the ClaimsIdentity defines these 'types'. For now just use defaults.

            int numberOfPropertiesWritten = 1;
            SerializationMask mask = SerializationMask.None;
            if (string.Equals(_type, ClaimsIdentity.DefaultNameClaimType))
            {
                mask |= SerializationMask.NameClaimType;
            }
            else if (string.Equals(_type, ClaimsIdentity.DefaultRoleClaimType))
            {
                mask |= SerializationMask.RoleClaimType;
            }
            else
            {
                numberOfPropertiesWritten++;
            }

            if (!string.Equals(_valueType, ClaimValueTypes.String, StringComparison.Ordinal))
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.StringType;
            }

            if (!string.Equals(_issuer, ClaimsIdentity.DefaultIssuer, StringComparison.Ordinal))
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.Issuer;
            }

            if (string.Equals(_originalIssuer, _issuer, StringComparison.Ordinal))
            {
                mask |= SerializationMask.OriginalIssuerEqualsIssuer;
            }
            else if (!string.Equals(_originalIssuer, ClaimsIdentity.DefaultIssuer, StringComparison.Ordinal))
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.OriginalIssuer;
            }

            if (Properties.Count > 0)
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.HasProperties;
            }

            // TODO - brentschmaltz - maximum size ??
            if (userData != null && userData.Length > 0)
            {
                numberOfPropertiesWritten++;
                mask |= SerializationMask.UserData;
            }

            writer.Write((Int32)mask);
            writer.Write((Int32)numberOfPropertiesWritten);
            writer.Write(_value);

            if (((mask & SerializationMask.NameClaimType) != SerializationMask.NameClaimType) && ((mask & SerializationMask.RoleClaimType) != SerializationMask.RoleClaimType))
            {
                writer.Write(_type);
            }

            if ((mask & SerializationMask.StringType) == SerializationMask.StringType)
            {
                writer.Write(_valueType);
            }

            if ((mask & SerializationMask.Issuer) == SerializationMask.Issuer)
            {
                writer.Write(_issuer);
            }

            if ((mask & SerializationMask.OriginalIssuer) == SerializationMask.OriginalIssuer)
            {
                writer.Write(_originalIssuer);
            }

            if ((mask & SerializationMask.HasProperties) == SerializationMask.HasProperties)
            {
                writer.Write(Properties.Count);
                foreach (var key in Properties.Keys)
                {
                    writer.Write(key);
                    writer.Write(Properties[key]);
                }
            }

            if ((mask & SerializationMask.UserData) == SerializationMask.UserData)
            {
                writer.Write((Int32)userData.Length);
                writer.Write(userData);
            }

            writer.Flush();
        }

        /// <summary>
        /// Returns a string representation of the <see cref="Claim"/> object.
        /// </summary>
        /// <remarks>
        /// The returned string contains the values of the <see cref="Type"/> and <see cref="Value"/> properties.
        /// </remarks>
        /// <returns>The string representation of the <see cref="Claim"/> object.</returns>
        public override string ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}: {1}", _type, _value);
        }
    }
}
