// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    internal class LocalsSignatureParser : SigParser
    {
        private readonly Stack<Type> _types = new Stack<Type>();
        private readonly Stack<TypeCreator> _creators = new Stack<TypeCreator>();
        private readonly ITokenResolver _tokenResolver;
        private readonly ITypeFactory _typeFactory;

        public LocalsSignatureParser(ITokenResolver tokenResolver, ITypeFactory typeFactory)
        {
            _tokenResolver = tokenResolver;
            _typeFactory = typeFactory;
        }

        public bool Parse(byte[] sig, out Type[] types)
        {
            if (Parse(sig))
            {
                types = _types.Reverse().ToArray();
                return true;
            }

            types = null;
            return false;
        }

        private static Type ToType(byte corType)
        {
            switch (corType)
            {
                case ELEMENT_TYPE_VOID: return typeof(void);
                case ELEMENT_TYPE_BOOLEAN: return typeof(bool);
                case ELEMENT_TYPE_CHAR: return typeof(char);
                case ELEMENT_TYPE_I1: return typeof(sbyte);
                case ELEMENT_TYPE_U1: return typeof(byte);
                case ELEMENT_TYPE_I2: return typeof(short);
                case ELEMENT_TYPE_U2: return typeof(ushort);
                case ELEMENT_TYPE_I4: return typeof(int);
                case ELEMENT_TYPE_U4: return typeof(uint);
                case ELEMENT_TYPE_I8: return typeof(long);
                case ELEMENT_TYPE_U8: return typeof(ulong);
                case ELEMENT_TYPE_R4: return typeof(float);
                case ELEMENT_TYPE_R8: return typeof(double);
                case ELEMENT_TYPE_STRING: return typeof(string);
                case ELEMENT_TYPE_I: return typeof(IntPtr);
                case ELEMENT_TYPE_U: return typeof(UIntPtr);
                case ELEMENT_TYPE_OBJECT: return typeof(object);
            }

            throw new NotSupportedException();
        }

        class TypeCreator
        {
            private readonly LocalsSignatureParser _parent;
            private Inner _inner;
            private bool _byRef;

            public TypeCreator(LocalsSignatureParser parent)
            {
                _parent = parent;
            }

            public void Done()
            {
                _inner.Done(_parent._typeFactory, _parent._types);

                if (_byRef)
                {
                    new ByRefInner().Done(_parent._typeFactory, _parent._types);
                }
            }

            public void TypeSimple(byte elemType)
            {
                _inner = new SimpleInner(ToType(elemType));
            }

            public void TypeSzArray()
            {
                _inner = new SzArrayInner();
            }

            public void ArrayShape()
            {
                _inner = new ArrayInner();
            }

            public void Rank(int rank)
            {
                ((ArrayInner)_inner).Rank = rank;
            }

            public void GenericInst(int count)
            {
                _inner = new GenericInner(count);
            }

            public void ByRef()
            {
                _byRef = true;
            }

            public void TypePointer()
            {
                _inner = new PointerInner();
            }

            public void TypeValueType()
            {
                _inner = new TypeInner();
            }

            public void TypeClass()
            {
                _inner = new TypeInner();
            }

            public void TypeInternal(IntPtr ptr)
            {
                _inner = new SimpleInner(_parent._typeFactory.FromHandle(ptr));
            }

            internal void TypeDefOrRef(int token, byte indexType, int index)
            {
                Type type = _parent._tokenResolver.AsType(token);
                ((TypeInner)_inner).Resolve(type);
            }

            abstract class Inner
            {
                public abstract void Done(ITypeFactory factory, Stack<Type> stack);
            }

            class SimpleInner : Inner
            {
                private readonly Type _type;

                public SimpleInner(Type type)
                {
                    _type = type;
                }

                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(_type);
                }
            }

            class SzArrayInner : Inner
            {
                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(factory.MakeArrayType(stack.Pop()));
                }
            }

            class ArrayInner : Inner
            {
                public int Rank { get; set; }

                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(factory.MakeArrayType(stack.Pop(), Rank));
                }
            }

            class ByRefInner : Inner
            {
                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(factory.MakeByRefType(stack.Pop()));
                }
            }

            class PointerInner : Inner
            {
                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(factory.MakePointerType(stack.Pop()));
                }
            }

            class TypeInner : Inner
            {
                private Type _type;

                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    stack.Push(_type);
                }

                public void Resolve(Type type)
                {
                    _type = type;
                }
            }

            class GenericInner : Inner
            {
                private readonly int _count;

                public GenericInner(int count)
                {
                    _count = count;
                }

                public override void Done(ITypeFactory factory, Stack<Type> stack)
                {
                    var args = new Type[_count];
                    for (var i = args.Length - 1; i >= 0; i--)
                    {
                        args[i] = stack.Pop();
                    }

                    stack.Push(factory.MakeGenericType(stack.Pop(), args));
                }
            }
        }

        protected override void NotifyBeginType()
        {
            var creator = new TypeCreator(this);

            if (_pendingByRef > 0)
            {
                _pendingByRef--;
                creator.ByRef();
            }

            _creators.Push(creator);
        }

        protected override void NotifyEndType()
        {
            _creators.Pop().Done();
        }

        protected override void NotifyTypeSzArray()
        {
            _creators.Peek().TypeSzArray();
        }

        protected override void NotifyTypeSimple(byte elem_type)
        {
            _creators.Peek().TypeSimple(elem_type);
        }

        private int _pendingByRef;

        protected override void NotifyByref()
        {
            _pendingByRef++;
        }

        protected override void NotifyTypePointer()
        {
            _creators.Peek().TypePointer();
        }

        protected override void NotifyVoid()
        {
            _types.Push(typeof(void));
        }

        protected override void NotifyTypeValueType()
        {
            _creators.Peek().TypeValueType();
        }

        protected override void NotifyTypeClass()
        {
            _creators.Peek().TypeClass();
        }

        protected override void NotifyTypeInternal(IntPtr ptr)
        {
            _creators.Peek().TypeInternal(ptr);
        }

        protected override void NotifyTypeDefOrRef(int token, byte indexType, int index)
        {
            _creators.Peek().TypeDefOrRef(token, indexType, index);
        }

        protected override void NotifyBeginArrayShape()
        {
            _creators.Peek().ArrayShape();
        }

        protected override void NotifyRank(int count)
        {
            _creators.Peek().Rank(count);
        }

        protected override void NotifyTypeGenericInst(int number)
        {
            _creators.Peek().GenericInst(number);
        }

        // NB: We can't stuff these in a System.Type, so won't forward them
        protected override void NotifyNumSizes(int count) { }
        protected override void NotifySize(int count) { }
        protected override void NotifyNumLoBounds(int count) { }
        protected override void NotifyLoBound(int count) { }
        protected override void NotifyEndArrayShape() { }

        protected override void NotifyTypedByref()
        {
            throw new NotImplementedException();
        }

        // NB: These are not needed for local signatures
        protected override void NotifyBeginProperty(byte elem_type) { throw new NotImplementedException(); }
        protected override void NotifyEndProperty() { throw new NotImplementedException(); }
        protected override void NotifyBeginMethod(byte elem_type) { throw new NotImplementedException(); }
        protected override void NotifyEndMethod() { throw new NotImplementedException(); }
        protected override void NotifyBeginField(byte elem_type) { throw new NotImplementedException(); }
        protected override void NotifyEndField() { throw new NotImplementedException(); }
        protected override void NotifyBeginRetType() { throw new NotImplementedException(); }
        protected override void NotifyEndRetType() { throw new NotImplementedException(); }
        protected override void NotifyBeginParam() { throw new NotImplementedException(); }
        protected override void NotifyParamCount(int count) { throw new NotImplementedException(); }
        protected override void NotifyEndParam() { throw new NotImplementedException(); }
        protected override void NotifyGenericParamCount(int count) { throw new NotImplementedException(); }
    }
}
