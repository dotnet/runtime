// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Configuration.Internal;
using System.Diagnostics;

namespace System.Configuration
{
    [DebuggerDisplay("FactoryRecord {ConfigKey}")]
    internal sealed class FactoryRecord : IConfigErrorInfo
    {
        private const int FlagAllowLocation = 0x0001; // Does the factory allow location directives?
        private const int FlagRestartOnExternalChanges = 0x0002; // Restart on external changes?

        // Does access to the section require unrestricted ConfigurationPermission?
        private const int FlagRequirePermission = 0x0004;

        private const int FlagIsGroup = 0x0008; // factory represents a group
        private const int FlagIsFromTrustedConfigRecord = 0x0010; // Factory is defined in trusted config record
        private const int FlagIsUndeclared = 0x0040; // Factory is not declared - either implicit or unrecognized

        private List<ConfigurationException> _errors; // errors
        private SimpleBitVector32 _flags; // factory flags

        // constructor used for Clone()
        private FactoryRecord(
            string configKey,
            string group,
            string name,
            object factory,
            string factoryTypeName,
            SimpleBitVector32 flags,
            ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition,
            OverrideModeSetting overrideModeDefault,
            string filename,
            int lineNumber,
            ICollection<ConfigurationException> errors)
        {
            ConfigKey = configKey;
            Group = group;
            Name = name;
            Factory = factory;
            FactoryTypeName = factoryTypeName;
            _flags = flags;
            AllowDefinition = allowDefinition;
            AllowExeDefinition = allowExeDefinition;
            OverrideModeDefault = overrideModeDefault;
            Filename = filename;
            LineNumber = lineNumber;

            AddErrors(errors);
        }

        // constructor used for group
        internal FactoryRecord(string configKey, string group, string name, string factoryTypeName, string filename,
            int lineNumber)
        {
            ConfigKey = configKey;
            Group = group;
            Name = name;
            FactoryTypeName = factoryTypeName;
            IsGroup = true;
            Filename = filename;
            LineNumber = lineNumber;
        }

        // constructor used for a section
        internal FactoryRecord(
            string configKey,
            string group,
            string name,
            string factoryTypeName,
            bool allowLocation,
            ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition,
            OverrideModeSetting overrideModeDefault,
            bool restartOnExternalChanges,
            bool requirePermission,
            bool isFromTrustedConfigRecord,
            bool isUndeclared,
            string filename,
            int lineNumber)
        {
            ConfigKey = configKey;
            Group = group;
            Name = name;
            FactoryTypeName = factoryTypeName;
            AllowDefinition = allowDefinition;
            AllowExeDefinition = allowExeDefinition;
            OverrideModeDefault = overrideModeDefault;
            AllowLocation = allowLocation;
            RestartOnExternalChanges = restartOnExternalChanges;
            RequirePermission = requirePermission;
            IsFromTrustedConfigRecord = isFromTrustedConfigRecord;
            IsUndeclared = isUndeclared;
            Filename = filename;
            LineNumber = lineNumber;
        }

        internal string ConfigKey { get; }

        internal string Group { get; }

        internal string Name { get; }

        internal object Factory { get; set; }

        internal string FactoryTypeName { get; set; }

        internal ConfigurationAllowDefinition AllowDefinition { get; set; }

        internal ConfigurationAllowExeDefinition AllowExeDefinition { get; set; }

        internal OverrideModeSetting OverrideModeDefault { get; }

        internal bool AllowLocation
        {
            get { return _flags[FlagAllowLocation]; }
            set { _flags[FlagAllowLocation] = value; }
        }

        internal bool RestartOnExternalChanges
        {
            get { return _flags[FlagRestartOnExternalChanges]; }
            set { _flags[FlagRestartOnExternalChanges] = value; }
        }

        internal bool RequirePermission
        {
            get { return _flags[FlagRequirePermission]; }
            set { _flags[FlagRequirePermission] = value; }
        }

        internal bool IsGroup
        {
            get { return _flags[FlagIsGroup]; }
            set { _flags[FlagIsGroup] = value; }
        }

        internal bool IsFromTrustedConfigRecord
        {
            get { return _flags[FlagIsFromTrustedConfigRecord]; }
            set { _flags[FlagIsFromTrustedConfigRecord] = value; }
        }

        internal bool IsUndeclared
        {
            get { return _flags[FlagIsUndeclared]; }
            set { _flags[FlagIsUndeclared] = value; }
        }

        internal bool HasFile => LineNumber >= 0;

        internal List<ConfigurationException> Errors => _errors;

        internal bool HasErrors => ErrorsHelper.GetHasErrors(_errors);

        // This is used in HttpConfigurationRecord.EnsureSectionFactory() to give file and line source
        // when a section handler type is invalid or cannot be loaded.
        public string Filename { get; set; }

        public int LineNumber { get; set; }

        // by cloning we contain a single copy of the strings referred to in the factory and section records
        internal FactoryRecord CloneSection(string filename, int lineNumber)
        {
            return new FactoryRecord(ConfigKey,
                Group,
                Name,
                Factory,
                FactoryTypeName,
                _flags,
                AllowDefinition,
                AllowExeDefinition,
                OverrideModeDefault,
                filename,
                lineNumber,
                Errors);
        }

        // by cloning we contain a single copy of the strings referred to in the factory and section records
        internal FactoryRecord CloneSectionGroup(string factoryTypeName, string filename, int lineNumber)
        {
            if (FactoryTypeName != null) factoryTypeName = FactoryTypeName;

            return new FactoryRecord(ConfigKey,
                Group,
                Name,
                Factory,
                factoryTypeName,
                _flags,
                AllowDefinition,
                AllowExeDefinition,
                OverrideModeDefault,
                filename,
                lineNumber,
                Errors);
        }

        internal bool IsEquivalentType(IInternalConfigHost host, string typeName)
        {
            try
            {
                if (FactoryTypeName == typeName)
                    return true;

                Type t1, t2;

                if (host != null)
                {
                    t1 = TypeUtil.GetType(host, typeName, false);
                    t2 = TypeUtil.GetType(host, FactoryTypeName, false);
                }
                else
                {
                    t1 = TypeUtil.GetType(typeName, false);
                    t2 = TypeUtil.GetType(FactoryTypeName, false);
                }

                return (t1 != null) && (t1 == t2);
            }
            catch { }

            return false;
        }

        internal bool IsEquivalentSectionGroupFactory(IInternalConfigHost host, string typeName)
        {
            if ((typeName == null) || (FactoryTypeName == null))
                return true;

            return IsEquivalentType(host, typeName);
        }

        internal bool IsEquivalentSectionFactory(
            IInternalConfigHost host,
            string typeName,
            bool allowLocation,
            ConfigurationAllowDefinition allowDefinition,
            ConfigurationAllowExeDefinition allowExeDefinition,
            bool restartOnExternalChanges,
            bool requirePermission)
        {
            if ((allowLocation != AllowLocation) ||
                (allowDefinition != AllowDefinition) ||
                (allowExeDefinition != AllowExeDefinition) ||
                (restartOnExternalChanges != RestartOnExternalChanges) ||
                (requirePermission != RequirePermission))
                return false;

            return IsEquivalentType(host, typeName);
        }

        internal void AddErrors(ICollection<ConfigurationException> coll)
        {
            ErrorsHelper.AddErrors(ref _errors, coll);
        }

        internal void ThrowOnErrors()
        {
            ErrorsHelper.ThrowOnErrors(_errors);
        }

        internal bool IsIgnorable()
        {
            if (Factory != null)
                return Factory is IgnoreSectionHandler;

            return (FactoryTypeName != null) && FactoryTypeName.Contains("System.Configuration.IgnoreSection");
        }
    }
}
