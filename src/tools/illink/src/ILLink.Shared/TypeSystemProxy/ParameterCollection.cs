// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace ILLink.Shared.TypeSystemProxy
{
	/// <summary>
	/// Enumerable struct used to enumerator over a method's parameters without allocating or going through IEnumerable
	/// </summary>
	internal struct ParameterProxyEnumerable : IEnumerable<ParameterProxy>
	{
		readonly int _start;

		readonly int _end;

		readonly MethodProxy _method;

		public ParameterProxyEnumerable (int start, int end, MethodProxy method)
		{
			_start = start;
			_end = end;
			_method = method;
		}

		public ParameterEnumerator GetEnumerator () => new ParameterEnumerator (_start, _end, _method);

		IEnumerator<ParameterProxy> IEnumerable<ParameterProxy>.GetEnumerator () => new ParameterEnumerator (_start, _end, _method);

		IEnumerator IEnumerable.GetEnumerator () => new ParameterEnumerator (_start, _end, _method);

		public struct ParameterEnumerator : IEnumerator<ParameterProxy>
		{
			readonly int _start;
			int _current;
			readonly int _end;
			readonly MethodProxy _method;
			public ParameterEnumerator (int start, int end, MethodProxy method)
			{
				_start = start;
				_current = start - 1;
				_end = end;
				_method = method;
			}

			public ParameterProxy Current => new ParameterProxy (_method, (ParameterIndex) _current);

			object IEnumerator.Current => new ParameterProxy (_method, (ParameterIndex) _current);

			public bool MoveNext () => ++_current < _end;

			public void Reset () => _current = _start;

			void IDisposable.Dispose () { }
		}
	}
}
