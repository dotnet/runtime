// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class MarkScopeStack
	{
		public readonly struct Scope
		{
			public readonly MessageOrigin Origin;

			public Scope (in MessageOrigin origin)
			{
				Origin = origin;
			}
		}

		readonly Stack<Scope> _scopeStack;

		internal readonly struct LocalScope : IDisposable
		{
			readonly MessageOrigin _origin;
			readonly MarkScopeStack _scopeStack;

			public LocalScope (in MessageOrigin origin, MarkScopeStack scopeStack)
			{
				_origin = origin;
				_scopeStack = scopeStack;

				_scopeStack.Push (new Scope (new MessageOrigin (origin)));
			}

			public LocalScope (in Scope scope, MarkScopeStack scopeStack)
			{
				_origin = scope.Origin;
				_scopeStack = scopeStack;
				_scopeStack.Push (scope);
			}

			public void Dispose ()
			{
				Scope scope = _scopeStack.Pop ();

				if (_origin.Provider != scope.Origin.Provider)
					throw new InternalErrorException ($"Scope stack imbalance - expected to pop '{_origin}' but instead popped '{scope.Origin}'.");
			}
		}

		internal readonly struct ParentScope : IDisposable
		{
			readonly Scope _parentScope;
			readonly Scope _childScope;
			readonly MarkScopeStack _scopeStack;

			public ParentScope (MarkScopeStack scopeStack)
			{
				_scopeStack = scopeStack;
				_childScope = _scopeStack.Pop ();
				_parentScope = _scopeStack.CurrentScope;
			}

			public void Dispose ()
			{
				if (_parentScope.Origin.Provider != _scopeStack.CurrentScope.Origin.Provider)
					throw new InternalErrorException ($"Scope stack imbalance - expected top of stack to be '{_parentScope.Origin}' but instead found '{_scopeStack.CurrentScope.Origin}'.");

				_scopeStack.Push (_childScope);
			}
		}

		public MarkScopeStack ()
		{
			_scopeStack = new Stack<Scope> ();
		}

		internal LocalScope PushLocalScope (in MessageOrigin origin)
		{
			return new LocalScope (origin, this);
		}

		internal LocalScope PushLocalScope (in Scope scope)
		{
			return new LocalScope (scope, this);
		}

		internal ParentScope PopToParentScope ()
		{
			return new ParentScope (this);
		}

		public IDisposable PushScope (in MessageOrigin origin)
		{
			return new LocalScope (origin, this);
		}

		public IDisposable PushScope (in Scope scope)
		{
			return new LocalScope (scope, this);
		}

		public IDisposable PopToParent ()
		{
			return new ParentScope (this);
		}

		public Scope CurrentScope {
			get {
				if (!_scopeStack.TryPeek (out var result))
					throw new InternalErrorException ($"Scope stack imbalance - expected scope but instead the stack is empty.");

				return result;
			}
		}

		public void UpdateCurrentScopeInstructionOffset (int offset)
		{
			var scope = _scopeStack.Pop ();
			if (scope.Origin.Provider is not MethodDefinition)
				throw new InternalErrorException ($"Trying to update instruction offset of scope stack which is not a method. Current stack scope is '{scope}'.");

			_scopeStack.Push (new Scope (new MessageOrigin (scope.Origin.Provider, offset)));
		}

		void Push (in Scope scope)
		{
			_scopeStack.Push (scope);
		}

		Scope Pop ()
		{
			if (!_scopeStack.TryPop (out var result))
				throw new InternalErrorException ($"Scope stack imbalance - trying to pop empty stack.");

			return result;
		}

		[Conditional ("DEBUG")]
		public void AssertIsEmpty () => Debug.Assert (_scopeStack.Count == 0);

	}
}
