// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
	/// <summary>
	/// Enumerable struct used to enumerator over a method's parameters without allocating or going through IEnumerable
	/// </summary>
	internal readonly struct ParameterProxyEnumerable : IEnumerable<ParameterProxy>
	{
		private readonly int _start;

		private readonly int _end;

		private readonly MethodProxy _method;

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
			private readonly int _start;
			private int _current;
			private readonly int _end;
			private readonly MethodProxy _method;

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
