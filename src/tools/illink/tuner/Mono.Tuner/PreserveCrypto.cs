using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Tuner {

	public class PreserveCrypto : IStep {

		AnnotationStore annotations;

		public void Process (LinkContext context)
		{
			annotations = context.Annotations;

			ProcessCorlib (context);
			ProcessSystemCore (context);
		}

		void ProcessCorlib (LinkContext context)
		{
			AssemblyDefinition corlib;
			if (!context.TryGetLinkedAssembly ("mscorlib", out corlib))
				return;

			AddPreserveInfo (corlib, "DES", "DESCryptoServiceProvider");
			AddPreserveInfo (corlib, "DSA", "DSACryptoServiceProvider");
			AddPreserveInfo (corlib, "RandomNumberGenerator", "RNGCryptoServiceProvider");
			AddPreserveInfo (corlib, "SHA1", "SHA1CryptoServiceProvider");
			AddPreserveInfo (corlib, "SHA1", "SHA1Managed");
			AddPreserveInfo (corlib, "MD5", "MD5CryptoServiceProvider");
			AddPreserveInfo (corlib, "RC2", "RC2CryptoServiceProvider");
			AddPreserveInfo (corlib, "TripleDES", "TripleDESCryptoServiceProvider");

			AddPreserveInfo (corlib, "Rijndael", "RijndaelManaged");
			AddPreserveInfo (corlib, "RIPEMD160", "RIPEMD160Managed");
			AddPreserveInfo (corlib, "SHA256", "SHA256Managed");
			AddPreserveInfo (corlib, "SHA384", "SHA384Managed");
			AddPreserveInfo (corlib, "SHA512", "SHA512Managed");

			AddPreserveInfo (corlib, "HMAC", "HMACMD5");
			AddPreserveInfo (corlib, "HMAC", "HMACRIPEMD160");
			AddPreserveInfo (corlib, "HMAC", "HMACSHA1");
			AddPreserveInfo (corlib, "HMAC", "HMACSHA256");
			AddPreserveInfo (corlib, "HMAC", "HMACSHA384");
			AddPreserveInfo (corlib, "HMAC", "HMACSHA512");

			AddPreserveInfo (corlib, "HMACMD5", "MD5CryptoServiceProvider");
			AddPreserveInfo (corlib, "HMACRIPEMD160", "RIPEMD160Managed");
			AddPreserveInfo (corlib, "HMACSHA1", "SHA1CryptoServiceProvider");
			AddPreserveInfo (corlib, "HMACSHA1", "SHA1Managed");
			AddPreserveInfo (corlib, "HMACSHA256", "SHA256Managed");
			AddPreserveInfo (corlib, "HMACSHA384", "SHA384Managed");
			AddPreserveInfo (corlib, "HMACSHA512", "SHA512Managed");

			TryAddPreserveInfo (corlib, "Aes", "AesManaged");
		}

		void ProcessSystemCore (LinkContext context)
		{
			AssemblyDefinition syscore;
			if (!context.TryGetLinkedAssembly ("System.Core", out syscore))
				return;

			// AddPreserveInfo (syscore, "Aes", "AesCryptoServiceProvider");
			TryAddPreserveInfo (syscore, "Aes", "AesManaged");
		}

		bool TryAddPreserveInfo (AssemblyDefinition assembly, string name, string type)
		{
			var marker = GetCryptoType (assembly, name);
			if (marker == null)
				return false;

			var implementation = GetCryptoType (assembly, type);
			if (implementation == null)
				return false;

			Preserve (marker, implementation);
			return true;
		}

		void AddPreserveInfo (AssemblyDefinition assembly, string name, string type)
		{
			var marker = GetCryptoType (assembly, name);
			if (marker == null)
				throw new ArgumentException (name);

			var implementation = GetCryptoType (assembly, type);
			if (implementation == null)
				throw new ArgumentException (type);

			Preserve (marker, implementation);
		}

		void Preserve (TypeDefinition marker, TypeDefinition implementation)
		{
			foreach (var constructor in implementation.GetConstructors ())
				annotations.AddPreservedMethod (marker, constructor);
		}

		TypeDefinition GetCryptoType (AssemblyDefinition assembly, string name)
		{
			return assembly.MainModule.GetType ("System.Security.Cryptography." + name);
		}
	}
}
