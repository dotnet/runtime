// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	public class StaticAbstractInterfaceMethods
	{
		public static void Main ()
		{
			InterfaceMethodsUsedThroughConstrainedType.Test ();
			InterfaceWithMethodsUsedEachWay.Test ();
			InterfaceMethodUsedOnConcreteType.Test ();
			InterfaceMethodsKeptThroughReflection.Test ();
			InterfaceHasStaticAndInstanceMethods.Test ();
			StaticInterfaceInheritance.Test ();
			GenericStaticInterface.Test ();
			RecursiveGenericInterface.Test ();
			UnusedInterfaces.Test ();
			ClassInheritance.Test ();
			ProcessOverrideAfterMarkedBase.Test ();
		}

		[Kept]
		public class InterfaceMethodsUsedThroughConstrainedType
		{
			[Kept]
			public interface IUsedThroughConstrainedType
			{
				[Kept]
				public static abstract int UsedThroughConstrainedType ();
			}

			[Kept]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			public class UsesIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IUsedThroughConstrainedType))]
			public class UnusedIUsedThroughConstrainedTypeMethods : IUsedThroughConstrainedType
			{
				[Kept]
				[KeptOverride (typeof (IUsedThroughConstrainedType))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			public static void CallMethodOnConstrainedType<T> () where T : IUsedThroughConstrainedType
			{
				T.UsedThroughConstrainedType ();
			}

			[Kept]
			public static void Test ()
			{
				CallMethodOnConstrainedType<UsesIUsedThroughConstrainedTypeMethods> ();

				Type t = typeof (UnusedIUsedThroughConstrainedTypeMethods);

				ExplicitImplementation.Test ();
			}

			[Kept]
			public class ExplicitImplementation
			{
				[Kept]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				public class UsedIUsedThroughConstrainedTypeExplicitMethods : IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				[KeptInterface (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
				public class UnusedIUsedThroughConstrainedTypeExplicitMethods : IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					[KeptOverride (typeof (IUsedThroughConstrainedTypeExplicitImplementation))]
					static int IUsedThroughConstrainedTypeExplicitImplementation.UsedThroughConstrainedType () => 0;
				}

				[Kept]
				public interface IUsedThroughConstrainedTypeExplicitImplementation
				{
					[Kept]
					public static abstract int UsedThroughConstrainedType ();
				}

				[Kept]
				public static void CallTypeConstrainedMethod<T> () where T : IUsedThroughConstrainedTypeExplicitImplementation
				{
					T.UsedThroughConstrainedType ();
				}

				[Kept]
				public static void Test ()
				{
					CallTypeConstrainedMethod<UsedIUsedThroughConstrainedTypeExplicitMethods> ();

					Type t = typeof (UnusedIUsedThroughConstrainedTypeExplicitMethods);
				}
			}
		}

		[Kept]
		public class InterfaceMethodUsedOnConcreteType
		{
			[Kept]
			public class UsesIUsedOnConcreteTypeMethods : IUsedOnConcreteType
			{
				[Kept]
				[RemovedOverride (typeof (IUsedOnConcreteType))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			public class UnusedIUsedOnConcreteTypeMethods : IUsedOnConcreteType
			{
				public static int UsedOnConcreteType () => 0;
			}

			public interface IUsedOnConcreteType
			{
				public static abstract int UsedOnConcreteType ();
			}

			[Kept]
			public static void Test ()
			{
				UsesIUsedOnConcreteTypeMethods.UsedOnConcreteType ();

				Type t = typeof (UnusedIUsedOnConcreteTypeMethods);
			}
		}

		[Kept]
		public class InterfaceWithMethodsUsedEachWay
		{

			[Kept]
			public interface IUsedEveryWay
			{
				[Kept]
				public static abstract int UsedThroughConstrainedType ();

				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IUsedEveryWay))]
			public class UsedIUsedEveryWay : IUsedEveryWay
			{

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				[Kept]
				[RemovedOverride (typeof (IUsedEveryWay))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IUsedEveryWay))]
			public class UnusedIUsedEveryWay : IUsedEveryWay
			{
				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				static int IUsedEveryWay.UsedThroughConstrainedTypeExplicit () => 0;

				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IUsedEveryWay))]
				public static int UsedThroughConstrainedType () => 0;
			}

			[Kept]
			public static void CallTypeConstrainedMethods<T> () where T : IUsedEveryWay
			{
				T.UsedThroughConstrainedType ();
				T.UsedThroughConstrainedTypeExplicit ();
			}

			[Kept]
			public static void Test ()
			{
				UsedIUsedEveryWay.UsedOnConcreteType ();
				CallTypeConstrainedMethods<UsedIUsedEveryWay> ();

				Type t = typeof (UnusedIUsedEveryWay);
			}
		}

		[Kept]
		public class InterfaceMethodsKeptThroughReflection
		{
			[Kept]
			public interface IMethodsKeptThroughReflection
			{
				[Kept]
				public static abstract int UnusedMethod ();

				[Kept]
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnConstrainedType ();
			}

			[Kept]
			[KeptInterface (typeof (IMethodsKeptThroughReflection))]
			public class UsedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
			{
				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UnusedMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConstrainedType () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IMethodsKeptThroughReflection))]
			public class UnusedMethodsKeptThroughtReflection : IMethodsKeptThroughReflection
			{
				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UnusedMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConstrainedType () => 0;

				[Kept]
				[KeptOverride (typeof (IMethodsKeptThroughReflection))]
				public static int UsedOnConcreteType () => 0;
			}

			[Kept]
			public static void Test ()
			{
				typeof (IMethodsKeptThroughReflection).RequiresPublicMethods ();
				UsedMethodsKeptThroughtReflection.UsedOnConcreteType ();
				UseMethodThroughTypeConstraint<UsedMethodsKeptThroughtReflection> ();

				Type t = typeof (UnusedMethodsKeptThroughtReflection);

				[Kept]
				static void UseMethodThroughTypeConstraint<T> () where T : IMethodsKeptThroughReflection
				{
					T.UsedOnConstrainedType ();
				}
			}
		}

		[Kept]
		public class InterfaceHasStaticAndInstanceMethods
		{
			[Kept]
			public interface IStaticAndInstanceMethods
			{
				public static abstract int StaticMethodCalledOnConcreteType ();

				[Kept]
				public static abstract int StaticMethodExplicitImpl ();

				[Kept]
				public int InstanceMethod ();
			}

			[Kept]
			public static void CallExplicitImplMethod<T> () where T : IStaticAndInstanceMethods
			{
				T.StaticMethodExplicitImpl ();
			}

			[Kept]
			[KeptMember (".ctor()")]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			public class UsesAllMethods : IStaticAndInstanceMethods
			{
				[Kept]
				[RemovedOverride (typeof (IStaticAndInstanceMethods))]
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				// Non-static implementation methods don't explicitly override the interface method
				public int InstanceMethod () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					UsesAllMethods.StaticMethodCalledOnConcreteType ();
					var x = new UsesAllMethods ();
					((IStaticAndInstanceMethods) x).InstanceMethod ();
					CallExplicitImplMethod<UsesAllMethods> ();
				}
			}

			[Kept]
			public class UnusedMethods : IStaticAndInstanceMethods
			{
				public static int StaticMethodCalledOnConcreteType () => 0;

				//Bug
				//[Kept]
				//[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				public int InstanceMethod () => 0;

				[Kept]
				public static void Test () { }
			}

			[Kept]
			[KeptInterface (typeof (IStaticAndInstanceMethods))]
			public class UnusedMethodsRelevantToVariantCasting : IStaticAndInstanceMethods
			{
				public static int StaticMethodCalledOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IStaticAndInstanceMethods))]
				static int IStaticAndInstanceMethods.StaticMethodExplicitImpl () => 0;

				[Kept] // Kept for valid IL
				public int InstanceMethod () => 0;

				[Kept]
				public static void Test (Type t) { }
			}

			[Kept]
			public static void Test ()
			{
				UsesAllMethods.Test ();
				UnusedMethods.Test ();
				UnusedMethodsRelevantToVariantCasting.Test (typeof (UnusedMethodsRelevantToVariantCasting));
			}
		}

		[Kept]
		public class StaticInterfaceInheritance
		{
			[Kept]
			public interface IBase1
			{
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static abstract int UsedOnConstrainedTypeExplicitImpl ();
				public static abstract int UnusedImplicitImpl ();
				public static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase1))]
			public interface IInheritsFromBase : IBase1
			{
				public static new abstract int UsedOnConcreteType ();
				public static new abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static new abstract int UsedOnConstrainedTypeExplicitImpl ();
				public static new abstract int UnusedImplicitImpl ();
				public static new abstract int UnusedExplicitImpl ();
			}

			[Kept]
			public interface IBase2
			{
				public static abstract int UsedOnConcreteType ();

				[Kept]
				public static abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static abstract int UsedOnConstrainedTypeExplicitImpl ();
				public static abstract int UnusedImplicitImpl ();
				public static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IBase2))]
			public interface IInheritsFromMultipleBases : IBase1, IBase2, IUnusedInterface
			{
				public static new abstract int UsedOnConcreteType ();
				public static new abstract int UsedOnBaseOnlyConstrainedTypeImplicitImpl ();

				[Kept]
				public static new abstract int UsedOnConstrainedTypeExplicitImpl ();
				public static new abstract int UnusedImplicitImpl ();
				public static new abstract int UnusedExplicitImpl ();
			}

			public interface IUnusedInterface
			{
				public static abstract int UsedOnConcreteType ();

				public static abstract int UnusedImplicitImpl ();

				public static abstract int UnusedExplicitImpl ();
			}

			[Kept]
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IInheritsFromBase))]
			public class ImplementsIInheritsFromBase : IInheritsFromBase
			{
				[Kept]
				[RemovedOverride (typeof (IInheritsFromBase))]
				[RemovedOverride (typeof (IBase1))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IBase1))]
				[RemovedOverride (typeof (IInheritsFromBase))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IInheritsFromBase))]
				static int IInheritsFromBase.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase1))]
				static int IBase1.UsedOnConstrainedTypeExplicitImpl () => 0;

				public static int UnusedImplicitImpl () => 0;

				static int IBase1.UnusedExplicitImpl () => 0;

				static int IInheritsFromBase.UnusedExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					ImplementsIInheritsFromBase.UsedOnConcreteType ();
					CallBase1TypeConstrainedMethod<ImplementsIInheritsFromBase> ();
					CallSingleInheritTypeConstrainedMethod<ImplementsIInheritsFromBase> ();
				}
			}

			[KeptInterface (typeof (IInheritsFromMultipleBases))]
			[KeptInterface (typeof (IBase1))]
			[KeptInterface (typeof (IBase2))]
			// [RemovedInterface (typeof (IUnusedInterface))]
			public class ImplementsIInheritsFromTwoBases : IInheritsFromMultipleBases
			{
				[Kept]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				[RemovedOverride (typeof (IBase1))]
				[RemovedOverride (typeof (IBase2))]
				[RemovedOverride (typeof (IUnusedInterface))]
				public static int UsedOnConcreteType () => 0;

				[Kept]
				[KeptOverride (typeof (IBase1))]
				[KeptOverride (typeof (IBase2))]
				[RemovedOverride (typeof (IInheritsFromMultipleBases))]
				public static int UsedOnBaseOnlyConstrainedTypeImplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase1))]
				static int IBase1.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IBase2))]
				static int IBase2.UsedOnConstrainedTypeExplicitImpl () => 0;

				[Kept]
				[KeptOverride (typeof (IInheritsFromMultipleBases))]
				static int IInheritsFromMultipleBases.UsedOnConstrainedTypeExplicitImpl () => 0;

				public static int UnusedImplicitImpl () => 0;

				static int IBase1.UnusedExplicitImpl () => 0;

				static int IBase2.UnusedExplicitImpl () => 0;

				static int IInheritsFromMultipleBases.UnusedExplicitImpl () => 0;

				static int IUnusedInterface.UnusedExplicitImpl () => 0;

				[Kept]
				public static void Test ()
				{
					ImplementsIInheritsFromTwoBases.UsedOnConcreteType ();
					CallBase1TypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
					CallBase2TypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
					CallDoubleInheritTypeConstrainedMethod<ImplementsIInheritsFromTwoBases> ();
				}
			}

			[Kept]
			public static void CallBase1TypeConstrainedMethod<T> () where T : IBase1
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void CallBase2TypeConstrainedMethod<T> () where T : IBase2
			{
				T.UsedOnBaseOnlyConstrainedTypeImplicitImpl ();
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void CallSingleInheritTypeConstrainedMethod<T> () where T : IInheritsFromBase
			{
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void CallDoubleInheritTypeConstrainedMethod<T> () where T : IInheritsFromMultipleBases
			{
				T.UsedOnConstrainedTypeExplicitImpl ();
			}

			[Kept]
			public static void Test ()
			{
				ImplementsIInheritsFromBase.Test ();
				ImplementsIInheritsFromTwoBases.Test ();
			}
		}

		[Kept]
		public class GenericStaticInterface
		{
			[Kept]
			public interface IGenericInterface<T>
			{
				public static abstract T GetT ();
				[Kept]
				public static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<int>))]
			public class ImplementsGenericInterface : IGenericInterface<int>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<int>))]
				public static int GetT () => 0;

				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				static int IGenericInterface<int>.GetTExplicit () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<int>))]
			public class ImplementsGenericInterfaceUnused : IGenericInterface<int>
			{
				public static int GetT () => 0;
				[Kept]
				[KeptOverride (typeof (IGenericInterface<int>))]
				static int IGenericInterface<int>.GetTExplicit () => 0;
			}

			[Kept]
			public static void Test ()
			{
				ImplementsGenericInterface.GetT ();
				CallExplicitMethod<ImplementsGenericInterface, int> ();
				Type t = typeof (ImplementsGenericInterfaceUnused);

			}

			[Kept]
			public static void CallExplicitMethod<T, U> () where T : IGenericInterface<U>
			{
				T.GetTExplicit ();
			}
		}

		[Kept]
		public class RecursiveGenericInterface
		{
			[Kept]
			public interface IGenericInterface<T> where T : IGenericInterface<T>
			{
				public static abstract T GetT ();
				[Kept]
				public static abstract T GetTExplicit ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			public class ImplementsIGenericInterfaceOfSelf : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
			public class ImplementsIGenericInterfaceOfOther : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				[Kept]
				[RemovedOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				public static ImplementsIGenericInterfaceOfSelf GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelf>))]
				static ImplementsIGenericInterfaceOfSelf IGenericInterface<ImplementsIGenericInterfaceOfSelf>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			[KeptInterface (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
			public class ImplementsIGenericInterfaceOfSelfUnused : IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>
			{
				public static ImplementsIGenericInterfaceOfSelfUnused GetT () => throw new NotImplementedException ();

				[Kept]
				[KeptOverride (typeof (IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>))]
				static ImplementsIGenericInterfaceOfSelfUnused IGenericInterface<ImplementsIGenericInterfaceOfSelfUnused>.GetTExplicit ()
					=> throw new NotImplementedException ();
			}

			[Kept]
			public static void Test ()
			{
				ImplementsIGenericInterfaceOfSelf.GetT ();
				ImplementsIGenericInterfaceOfOther.GetT ();
				CallExplicitGetT<ImplementsIGenericInterfaceOfSelf> ();
				CallExplicitGetT<ImplementsIGenericInterfaceOfOther> ();

				Type t = typeof (ImplementsIGenericInterfaceOfSelfUnused);
			}

			[Kept]
			public static void CallExplicitGetT<T> () where T : IGenericInterface<ImplementsIGenericInterfaceOfSelf>
			{
				T.GetTExplicit ();
			}
		}

		[Kept]
		public class UnusedInterfaces
		{
			public interface IUnusedInterface
			{
				public int UnusedMethodImplicit ();
				public int UnusedMethodExplicit ();
			}

			[Kept]
			public interface IUnusedMethods
			{
				public int UnusedMethodImplicit ();
				public int UnusedMethodExplicit ();
			}

			[Kept]
			public class ImplementsUnusedInterface : IUnusedInterface
			{
				int IUnusedInterface.UnusedMethodExplicit () => 0;

				public int UnusedMethodImplicit () => 0;
			}

			[Kept]
			// In link mode, if we remove all methods from the interface, we should be able to remove the interface. We need it now since we don't remove the type constraint from UsesIUnusedMethods<T>
			[KeptInterface (typeof (IUnusedMethods))]
			public class ImplementsIUnusedMethods : IUnusedMethods
			{
				int IUnusedMethods.UnusedMethodExplicit () => 0;

				public int UnusedMethodImplicit () => 0;
			}

			[Kept]
			// In link mode, if there are no constrained calls we should be able to remove the type constraint
			public static void UsesIUnusedMethods<T> () where T : IUnusedMethods { }

			[Kept]
			public static void Test ()
			{
				UsesIUnusedMethods<ImplementsIUnusedMethods> ();
				Type t = typeof (ImplementsUnusedInterface);
			}
		}

		[Kept]
		public class ClassInheritance
		{
			[Kept]
			public interface IBase
			{
				[Kept]
				static abstract int ExplicitlyImplemented ();
				static abstract int ImplicitlyImplementedUsedOnType ();
				static abstract int ImplicitlyImplementedUsedOnInterface ();
				int GetInt ();
			}

			[Kept]
			[KeptInterface (typeof (IBase))]
			public abstract class BaseKeptOnType : IBase
			{
				[Kept]
				[KeptOverride (typeof (IBase))]
				static int IBase.ExplicitlyImplemented () => 0;

				// Don't use at all
				public static int ImplicitlyImplementedUsedOnType () => 0;

				public static int ImplicitlyImplementedUsedOnInterface () => 0;
				public int GetInt () => 0;
			}

			[Kept]
			[KeptInterface (typeof (IBase))]
			[KeptBaseType (typeof (BaseKeptOnType))]
			public class InheritsFromBase : BaseKeptOnType, IBase
			{
				// Use on this type only
				// This doesn't override IBase.ImplicitlyImplementedUsedOnType
				[Kept]
				public static int ImplictlyImplementedUsedOnType () => 0;
			}

			[Kept]
			public static void CallIBaseMethod<T> () where T : IBase
			{
				T.ExplicitlyImplemented ();
			}

			[Kept]
			public static void Test ()
			{
				InheritsFromBase.ImplictlyImplementedUsedOnType ();
				CallIBaseMethod<InheritsFromBase> ();
			}
		}

		[Kept]
		public static class ProcessOverrideAfterMarkedBase
		{
			[Kept]
			interface IFoo
			{
				[Kept]
				static abstract int Method ();
			}

			[Kept]
			[KeptInterface (typeof (IFoo))]
			class Foo : IFoo
			{
				[Kept]
				[KeptOverride (typeof (IFoo))]
				public static int Method () => 0;
			}

			[Kept]
			public static void Test ()
			{
				typeof (Foo).RequiresPublicMethods ();
				typeof (IFoo).RequiresPublicMethods ();
			}
		}
	}
}
