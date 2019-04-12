using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class EventUsedViaReflection {
		public static void Main ()
		{
			new Foo (); // Needed to avoid lazy body marking stubbing
			var eventInfo = typeof (Foo).GetEvent ("Event");
			eventInfo.GetAddMethod (false);
		}

		[KeptMember (".ctor()")]
		class Foo {
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler<EventArgs> Event;
		}
	}
}
