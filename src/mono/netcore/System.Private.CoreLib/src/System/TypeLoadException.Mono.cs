// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
	partial class TypeLoadException
	{
		// Called by runtime
		internal TypeLoadException (string className, string assemblyName)
			: this (null)
		{
			_className = className;
			_assemblyName = assemblyName;
		}

		void SetMessageField ()
		{
			if (_message != null)
				return;

			if (_className == null) {
				_message = SR.Arg_TypeLoadException;
				return;
			}

			_message = SR.Format ("Could not load type '{0}' from assembly '{1}'.", _className, _assemblyName ?? SR.IO_UnknownFileName);
		}
	}
}
