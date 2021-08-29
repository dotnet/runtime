// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    /// <summary>
    /// Signifies that the attributed type has a visualizer which is pointed
    /// to by the parameter type name strings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerVisualizerAttribute : Attribute
    {
        private Type? _target;

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName)
        {
            VisualizerTypeName = visualizerTypeName;
        }

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string? visualizerObjectSourceTypeName)
        {
            VisualizerTypeName = visualizerTypeName;
            VisualizerObjectSourceTypeName = visualizerObjectSourceTypeName;
        }

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string visualizerTypeName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizerObjectSource)
        {
            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException(nameof(visualizerObjectSource));
            }

            VisualizerTypeName = visualizerTypeName;
            VisualizerObjectSourceTypeName = visualizerObjectSource.AssemblyQualifiedName;
        }

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }

            VisualizerTypeName = visualizer.AssemblyQualifiedName!;
        }

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizerObjectSource)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }
            if (visualizerObjectSource == null)
            {
                throw new ArgumentNullException(nameof(visualizerObjectSource));
            }

            VisualizerTypeName = visualizer.AssemblyQualifiedName!;
            VisualizerObjectSourceTypeName = visualizerObjectSource.AssemblyQualifiedName;
        }

        public DebuggerVisualizerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type visualizer,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string? visualizerObjectSourceTypeName)
        {
            if (visualizer == null)
            {
                throw new ArgumentNullException(nameof(visualizer));
            }

            VisualizerTypeName = visualizer.AssemblyQualifiedName!;
            VisualizerObjectSourceTypeName = visualizerObjectSourceTypeName;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public string? VisualizerObjectSourceTypeName { get; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public string VisualizerTypeName { get; }

        public string? Description { get; set; }

        public Type? Target
        {
            get => _target;
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

        public string? TargetTypeName { get; set; }
    }
}
