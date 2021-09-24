// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("link", "test.exe")]
	[SetupCompileBefore ("RUCOnAttributeCtor.dll", new[] { "Dependencies/RequiresUnreferencedCodeOnAttributeCtorAttribute.cs" })]
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresUnreferencedCodeOnAttributeCtor
	{
		[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
		public static void Main ()
		{
			var type = new Type ();
			type.Method ();
			type.MethodAnnotatedWithRUC ();
			type.Field = 0;
			_ = type.PropertyGetter;
			type.PropertySetter = 0;
			type.EventAdd -= (sender, e) => { };
			type.EventRemove += (sender, e) => { };
			Type.Interface annotatedInterface = new Type.NestedType ();
		}

		[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
		[RequiresUnreferencedCodeOnAttributeCtor]
		[KeptMember (".ctor()")]
		public class Type
		{
			[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
			[RequiresUnreferencedCodeOnAttributeCtor]
			public void Method ()
			{
			}

			[RequiresUnreferencedCode ("Message from attribute's ctor.")]
			[RequiresUnreferencedCodeOnAttributeCtor]
			public void MethodAnnotatedWithRUC ()
			{
			}

			[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
			[RequiresUnreferencedCodeOnAttributeCtor]
			public int Field;

			public int PropertyGetter {
				[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
				[RequiresUnreferencedCodeOnAttributeCtor]
				get { return 0; }
			}

			public int PropertySetter {
				[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
				[RequiresUnreferencedCodeOnAttributeCtor]
				set { throw new NotImplementedException (); }
			}

			public event EventHandler EventAdd {
				add { }
				[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
				[RequiresUnreferencedCodeOnAttributeCtor]
				remove { }
			}

			public event EventHandler EventRemove {
				[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
				[RequiresUnreferencedCodeOnAttributeCtor]
				add { }
				remove { }
			}

			[ExpectedWarning ("IL2026", "Message from attribute's ctor.")]
			[RequiresUnreferencedCodeOnAttributeCtor]
			public interface Interface
			{
			}

			public class NestedType : Interface
			{
			}
		}
	}
}
