// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
	//
	// This is a private assembly name abstraction that's more suitable for use as keys in our caches.
	//
	//  - Immutable, unlike the public AssemblyName
	//  - Has a useful Equals() override, unlike the public AssemblyName.
	//
	// We use this as our internal interchange type and only convert to and from the public AssemblyName class at public boundaries.
	//
	public sealed class RuntimeAssemblyName : IEquatable<RuntimeAssemblyName>
	{
		public RuntimeAssemblyName(string name, Version version, string cultureName, AssemblyNameFlags flags, byte[] publicKeyOrToken)
		{
			Debug.Assert(name != null);
			this.Name = name;

			// Optional version.
			this.Version = version;

			// Optional culture name.
			this.CultureName = cultureName;

			// Optional flags (this is actually an OR of the classic flags and the ContentType.)
			this.Flags = flags;

			// Optional public key (if Flags.PublicKey == true) or public key token.
			this.PublicKeyOrToken = publicKeyOrToken;
		}

		// Simple name.
		public string Name { get; }

		// Optional version.
		public Version Version { get; }

		// Optional culture name.
		public string CultureName { get; }

		// Optional flags (this is actually an OR of the classic flags and the ContentType.)
		public AssemblyNameFlags Flags { get; }

		// Optional public key (if Flags.PublicKey == true) or public key token.
		public byte[] PublicKeyOrToken { get; }

		// Equality - this compares every bit of data in the RuntimeAssemblyName which is acceptable for use as keys in a cache
		// where semantic duplication is permissible. This method is *not* meant to define ref->def binding rules or
		// assembly binding unification rules.
		public bool Equals(RuntimeAssemblyName other)
		{
			if (other == null)
				return false;
			if (!this.Name.Equals(other.Name))
				return false;
			if (this.Version == null)
			{
				if (other.Version != null)
					return false;
			}
			else
			{
				if (!this.Version.Equals(other.Version))
					return false;
			}
			if (!string.Equals(this.CultureName, other.CultureName))
				return false;
			if (this.Flags != other.Flags)
				return false;

			byte[] thisPK = this.PublicKeyOrToken;
			byte[] otherPK = other.PublicKeyOrToken;
			if (thisPK == null)
			{
				if (otherPK != null)
					return false;
			}
			else if (otherPK == null)
			{
				return false;
			}
			else if (thisPK.Length != otherPK.Length)
			{
				return false;
			}
			else
			{
				for (int i = 0; i < thisPK.Length; i++)
				{
					if (thisPK[i] != otherPK[i])
						return false;
				}
			}

			return true;
		}

		public sealed override bool Equals(object obj)
		{
			RuntimeAssemblyName other = obj as RuntimeAssemblyName;
			if (other == null)
				return false;
			return Equals(other);
		}

		public sealed override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public string FullName
		{
			get
			{
				return AssemblyNameFormatter.ComputeDisplayName(this);
			}
		}
	}
}