using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection {
	public class EventUsedViaReflection {
		public static void Main ()
		{
			var eventInfo = typeof (EventUsedViaReflection).GetEvent ("Event");
			eventInfo.GetAddMethod (false);
		}

		[Kept]
		[KeptBackingField]
		[KeptEventAddMethod]
		[KeptEventRemoveMethod]
		event EventHandler<EventArgs> Event;
	}
}
