using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class EventHanderTypeGetInvokeMethod
	{
		public static void Main()
		{
			EventDelegate.Test ();
			NonDelegate.Test ();
		}

		class EventDelegate
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler MyEvent;

			public static void Test ()
			{
				var eventInfo = typeof(EventHanderTypeGetInvokeMethod).GetEvent(nameof(MyEvent));
				var invoke = eventInfo.EventHandlerType.GetMethod("Invoke");
			}
		}

		class NonDelegate
		{
			[Kept]
			[KeptBaseType (typeof (EventInfo))]
			[KeptMember (".ctor()")]
			class CustomEventInfo : EventInfo
			{
				[Kept]
				public override Type EventHandlerType {
					[Kept]
					get => typeof (NonDelegate);
				}

				[Kept]
				public override bool IsDefined (Type type, bool inherit) => throw null;
				[Kept]
				public override object[] GetCustomAttributes (bool inherit) => throw null;
				[Kept]
				public override object[] GetCustomAttributes (Type attributeType, bool inherit) => throw null;
				[Kept]
				public override string Name {
					[Kept]
					get => throw null;
				}
				[Kept]
				public override Type ReflectedType {
					[Kept]
					get => throw null;
				}
				[Kept]
				public override Type DeclaringType {
					[Kept]
					get => throw null;
				}
				[Kept]
				public override MethodInfo GetAddMethod (bool nonPublic) => throw null;
				[Kept]
				public override MethodInfo GetRemoveMethod (bool nonPublic) => throw null;
				[Kept]
				public override MethodInfo GetRaiseMethod (bool nonPublic) => throw null;
				[Kept]
				public override EventAttributes Attributes {
					[Kept]
					get => throw null;
				}
			}

			// Strictly speaking this should be kept, but trimmer doesn't see through the custom event info.
			// See discussion at https://github.com/dotnet/runtime/issues/114113.
			[RequiresUnreferencedCode (nameof (Invoke))]
			public void Invoke ()
			{
			}

			[Kept]
			[ExpectedWarning ("IL2075", nameof (Type.GetMethod), Tool.Analyzer,
				"ILLink/ILC intrinsic handling assumes EventHandlerType is a delegate: https://github.com/dotnet/runtime/issues/114113")]
			public static void Test ()
			{
				new CustomEventInfo ().EventHandlerType.GetMethod ("Invoke");
			}
		}
	}
}
