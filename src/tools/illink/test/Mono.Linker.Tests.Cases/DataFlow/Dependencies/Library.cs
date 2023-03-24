// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.DataFlow.Dependencies
{
	public class Library
	{
		public interface IAnnotatedMethods
		{
			static abstract void GenericWithMethodsStatic<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();

			static abstract void ParamWithMethodsStatic ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t);

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			static abstract Type ReturnWithMethodsStatic ();

			void GenericWithMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();

			void ParamWithMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t);

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type ReturnWithMethods ();
		}

		public interface IUnannotatedMethods
		{
			static abstract void GenericStatic<T> ();

			static abstract void ParamStatic (Type t);

			static abstract Type ReturnStatic ();

			void Generic<T> ();

			void Param (Type t);

			Type Return ();
		}

		public abstract class AnnotatedMethods
		{
			public abstract void GenericWithMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();

			public abstract void ParamWithMethods ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t);

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public abstract Type ReturnWithMethods ();
		}

		public abstract class UnannotatedMethods
		{
			public abstract void Generic<T> ();

			public abstract void Param (Type t);

			public abstract Type Return ();
		}
	}
}
