// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    /// Signifies that the attributed type has a visualizer which is pointed
    /// to by the parameter type name strings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerVisualizerAttribute : Attribute
    {
        private readonly string _visualizerObjectSourceName;
        private readonly string _visualizerName;
        private Type _target;

        public DebuggerVisualizerAttribute(string visualizerTypeName)
        {
            _visualizerName = visualizerTypeName;
        }
        public DebuggerVisualizerAttribute(string visualizerTypeName, string visualizerObjectSourceTypeName)
        {
            _visualizerName = visualizerTypeName;
            _visualizerObjectSourceName = visualizerObjectSourceTypeName;
        }
        public DebuggerVisualizerAttribute(string visualizerTypeName, Type visualizerObjectSource)
        {
            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException(nameof(visualizerObjectSource));
            }
            _visualizerName = visualizerTypeName;
            _visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
        }
        public DebuggerVisualizerAttribute(Type visualizer)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }
            _visualizerName = visualizer.AssemblyQualifiedName;
        }
        public DebuggerVisualizerAttribute(Type visualizer, Type visualizerObjectSource)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }
            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException(nameof(visualizerObjectSource));
            }
            _visualizerName = visualizer.AssemblyQualifiedName;
            _visualizerObjectSourceName = visualizerObjectSource.AssemblyQualifiedName;
        }
        public DebuggerVisualizerAttribute(Type visualizer, string visualizerObjectSourceTypeName)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }
            _visualizerName = visualizer.AssemblyQualifiedName;
            _visualizerObjectSourceName = visualizerObjectSourceTypeName;
        }

        public string VisualizerObjectSourceTypeName
        {
            get { return _visualizerObjectSourceName; }
        }
        public string VisualizerTypeName
        {
            get { return _visualizerName; }
        }
        public string Description { get; set; }
        
        public Type Target
        {
            get { return _target; }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                TargetTypeName = value.AssemblyQualifiedName;
                _target = value;
            }
        }

        public string TargetTypeName { get; set; }
    }
}