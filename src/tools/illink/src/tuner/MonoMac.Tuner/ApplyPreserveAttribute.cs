using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Mono.Linker;
using Mono.Linker.Steps;

using Mono.Tuner;

using Mono.Cecil;

namespace MonoMac.Tuner {

	public class ApplyPreserveAttribute : ApplyPreserveAttributeBase {

		protected override string PreserveAttribute {
			get { return "MonoMac.Foundation.PreserveAttribute"; }
		}
	}
}
