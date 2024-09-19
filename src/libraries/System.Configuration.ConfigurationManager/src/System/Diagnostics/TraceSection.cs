// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics
{
    [RequiresUnreferencedCode(ConfigurationManager.TrimWarning)]
    internal sealed class TraceSection : ConfigurationElement
    {
        private static readonly ConfigurationPropertyCollection s_properties = new();
        private static readonly ConfigurationProperty s_propListeners = new("listeners", typeof(ListenerElementsCollection), null, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propAutoFlush = new("autoflush", typeof(bool), false, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propIndentSize = new("indentsize", typeof(int), 4, ConfigurationPropertyOptions.None);
        private static readonly ConfigurationProperty s_propUseGlobalLock = new("useGlobalLock", typeof(bool), true, ConfigurationPropertyOptions.None);

        static TraceSection()
        {
            s_properties.Add(s_propListeners);
            s_properties.Add(s_propAutoFlush);
            s_properties.Add(s_propIndentSize);
            s_properties.Add(s_propUseGlobalLock);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("autoflush", DefaultValue = false)]
        public bool AutoFlush => (bool)this[s_propAutoFlush];

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("indentsize", DefaultValue = 4)]
        public int IndentSize => (int)this[s_propIndentSize];

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("listeners")]
        public ListenerElementsCollection Listeners => (ListenerElementsCollection)this[s_propListeners];

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCodeMessage",
            Justification = "Reflection access to the ConfigurationPropertyAttribute instance is covered by RequiresUnreferencedCode on the class: https://github.com/dotnet/runtime/issues/108454")]
        [ConfigurationProperty("useGlobalLock", DefaultValue = true)]
        public bool UseGlobalLock => (bool)this[s_propUseGlobalLock];

        protected internal override ConfigurationPropertyCollection Properties => s_properties;
    }
}
