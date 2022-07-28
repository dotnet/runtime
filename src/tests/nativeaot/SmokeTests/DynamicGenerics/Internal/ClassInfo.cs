// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace CoreFXTestLibrary.Internal
{
    // This is spread over two classes so the non-generic can be used in TestInfo
    // and the generic version can be used in the generated main when creating the
    // delegates

    public abstract class ClassInfo
    {
        public Action ClassInitializer { get; set; }
        public Action ClassCleanup { get; set; }
        public Action TestInitializer { get; set; }
        public Action TestCleanup { get; set; }
        public abstract bool IsValueCreated { get; }
    }

    // Factory method used to instatiate the object to help ensure generated code is used instead of default(T) or new T().
    // Very simple implementation of Lazy<T> here to ensure instance methods only created when they will be used.
    // This Method is not thread safe!!!!!
    public class ClassInfo<T> : ClassInfo
    {
        private Func<T> _factory;

        private T _value;
        public T Value
        {
            get
            {
                if (_isValueCreated == false)
                {
                    _value = _factory();
                    _isValueCreated = true;
                }
                return _value;
            }
        }

        private bool _isValueCreated = false;
        // PLANNED: Add ClassInitialize and ClassCleanup, at which point this property will be necessary.
        public override bool IsValueCreated { get { return _isValueCreated; } }

        public ClassInfo(Func<T> Factory) : base()
        {
            _factory = Factory;
        }
    }
}
