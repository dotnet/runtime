// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
	public class MemberTypesAllBaseType
	{
		static MemberTypesAllBaseType () { }
		public MemberTypesAllBaseType () { }
		private MemberTypesAllBaseType (bool _) { }

		public void PublicMethod () { }
		private void PrivateMethod () { }

		public static void PublicStaticMethod () { }
		private static void PrivateStaticMethod () { }

		public int PublicField;
		private int PrivateField;
		public static int PublicStaticField;
		private static int PrivateStaticField;

		public bool PublicProperty { get; set; }
		private bool PrivateProperty { get; set; }
		public static bool PublicStaticProperty { get; set; }
		private static bool PrivateStaticProperty { get; set; }

		public event EventHandler<EventArgs> PublicEvent;
		private event EventHandler<EventArgs> PrivateEvent;
		public static event EventHandler<EventArgs> PublicStaticEvent;
		private static event EventHandler<EventArgs> PrivateStaticEvent;

		public class PublicNestedType
		{
			private void PrivateMethod () { }
		}

		private class PrivateNestedType
		{
			private void PrivateMethod () { }
		}
	}
}
