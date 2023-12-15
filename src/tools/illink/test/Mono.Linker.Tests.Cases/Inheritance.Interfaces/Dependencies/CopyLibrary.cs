// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies
{
	public interface ICopyLibraryInterface
	{
		void CopyLibraryInterfaceMethod ();
		void CopyLibraryExplicitImplementationInterfaceMethod ();
	}

	public interface ICopyLibraryStaticInterface
	{
		static abstract void CopyLibraryStaticInterfaceMethod ();
		static abstract void CopyLibraryExplicitImplementationStaticInterfaceMethod ();
	}
}
