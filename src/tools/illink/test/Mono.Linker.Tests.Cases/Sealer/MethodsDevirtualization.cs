using System;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Sealer
{
	[SetupLinkerArgument ("--enable-opt", "sealer")]
	[AddedPseudoAttributeAttribute ((uint) TypeAttributes.Sealed)]
	public class MethodsDevirtualization
	{
		public static void Main ()
		{
			var s0 = new Data.BaseClassMethods ();
			s0.A ();
			s0.C ();

			var s1 = new Data.Sealable ();
			s1.A ();
			s1.B ();
			s1.B2 ();
			s1.C ();
			s1.D ();

			var s2 = new Data.SealableAbstract ();
			s2.A ();

			Data.IA s3 = new Data.Subclass ();
			s3.A ();
		}
	}
}

namespace Mono.Linker.Tests.Cases.Sealer.Data
{
	[Kept]
	public class BaseClassMethods
	{
		[Kept]
		public BaseClassMethods ()
		{
		}

		[Kept]
		[AddedPseudoAttributeAttribute ((uint) MethodAttributes.Final)]
		public virtual bool A () => true;
		[Kept]
		public virtual bool B () => false;
		[Kept]
		[AddedPseudoAttributeAttribute ((uint) MethodAttributes.Final)]
		public virtual bool C () => false;
		[Kept]
		public virtual bool D () => false;
	}

	[Kept]
	[KeptBaseType (typeof (BaseClassMethods))]
	[AddedPseudoAttributeAttribute ((uint) TypeAttributes.Sealed)]
	public class Sealable : BaseClassMethods
	{
		[Kept]
		public Sealable ()
		{
		}

		[Kept]
		[AddedPseudoAttributeAttribute ((uint) MethodAttributes.Final)]
		public override bool B () => true;

		[Kept]
		[RemovedPseudoAttributeAttribute ((uint) MethodAttributes.Virtual)]
		[RemovedPseudoAttributeAttribute ((uint) MethodAttributes.VtableLayoutMask)]
		public virtual bool B2 () => false;

		[Kept]
		public new bool C () => false;

		[Kept]
		public sealed override bool D () => false;
	}

	[Kept]
	public abstract class BaseAbstractClassMethods
	{
		[Kept]
		public BaseAbstractClassMethods ()
		{
		}

		[Kept]
		public abstract bool A ();
	}

	[Kept]
	[KeptBaseType (typeof (BaseAbstractClassMethods))]
	[AddedPseudoAttributeAttribute ((uint) TypeAttributes.Sealed)]
	public class SealableAbstract : BaseAbstractClassMethods
	{
		[Kept]
		public SealableAbstract ()
		{
		}

		[Kept]
		[AddedPseudoAttributeAttribute ((uint) MethodAttributes.Final)]
		public override bool A () => true;
	}

	[Kept]
	interface IA
	{
		[Kept]
		bool A ();
	}

	[Kept]
	public class BaseA
	{
		[Kept]
		public BaseA ()
		{
		}

		[Kept]
		public bool A () => true;
	}

	[Kept]
	[KeptBaseType (typeof (BaseA))]
	[KeptInterface (typeof (IA))]
	[AddedPseudoAttributeAttribute ((uint) TypeAttributes.Sealed)]
	public class Subclass : BaseA, IA
	{
		[Kept]
		public Subclass ()
		{
		}
	}
}