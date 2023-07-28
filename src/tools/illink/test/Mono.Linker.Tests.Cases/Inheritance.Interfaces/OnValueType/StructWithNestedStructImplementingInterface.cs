using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType
{
	/// <summary>
	/// This is a specially crafted cases that was able to hit a 'Collection was modified; enumeration operation may not execute.' exception during
	/// MarkStep.ProcessMarkedTypesWithInterfaces
	/// </summary>
	public class StructWithNestedStructImplementingInterface
	{
		public static void Main ()
		{
			var type = typeof (IBuildable<>);
			GC.KeepAlive (type);
			var test = new Profile ();
			test.Foo ();
		}

		[Kept]
		public interface IRunner
		{
		}

		public interface IBuildable
		{
		}

		[Kept]
		public interface IBuilder
		{
			Result<IRunner> Build (Node node, Node root, World world);
		}

		[Kept]
		public interface IBuildable<T> : IBuildable where T : IBuilder, new()
		{
		}

		public struct Result<T> : IResult
		{
		}

		public class Node
		{
		}

		public class World
		{
		}

		public interface IResult
		{
		}

		[Kept]
		[KeptInterface (typeof (IBuilder))]
		[KeptMember (".ctor()")]
		public abstract class Builder<T> : IBuilder where T : IRunner
		{
			public abstract Result<T> Build (Node node, Node root, World world);

			Result<IRunner> IBuilder.Build (Node node, Node root, World world)
			{
				return default (Result<IRunner>);
			}
		}

		[Kept]
		[KeptInterface (typeof (IBuildable<Profile.Builder>))]
		public struct Profile : IBuildable<Profile.Builder>
		{
			[Kept]
			public void Foo ()
			{
			}

			[Kept]
			[KeptInterface (typeof (IRunner))]
			private sealed class Runner : IRunner
			{
			}

			[Kept]
			[KeptBaseType (typeof (Builder<Runner>))]
			[KeptMember (".ctor()")]
			private sealed class Builder : Builder<Runner>
			{
				public override Result<Runner> Build (Node node, Node root, World world)
				{
					return default (Result<Runner>);
				}
			}
		}
	}
}