// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Configuration
{
    [DebuggerDisplay("SectionRecord {ConfigKey}")]
    internal sealed class SectionRecord
    {
        //
        // Runtime flags below 0x10000
        //

        // locked by parent input, either because a parent section is locked,
        // a parent section locks all children, or a location input for this
        // configPath has allowOverride=false.
        private const int FlagLocked = 0x00000001;

        // lock children of this section
        private const int FlagLockChildren = 0x00000002;

        // propagation of FactoryRecord.RequirePermission
        private const int FlagRequirePermission = 0x00000008;

        // Look at AddLocationInput for explanation of this flag's purpose
        private const int FlagLocationInputLockApplied = 0x00000010;

        // Look at AddIndirectLocationInput for explanation of this flag's purpose
        private const int FlagIndirectLocationInputLockApplied = 0x00000020;

        // The flag gives us the inherited lock mode for this section record without the file input
        // We need this to support SectionInformation.OverrideModeEffective.
        private const int FlagChildrenLockWithoutFileInput = 0x00000040;

        //
        // Designtime flags at or above 0x00010000
        //

        // the section has been added to the update list
        private const int FlagAddUpdate = 0x00010000;

        // result can be null, so we use this object to indicate whether it has been evaluated
        private static readonly object s_unevaluated = new object();

        // config key

        // The input from this file

        private SafeBitVector32 _flags;

        // This special input is used only when creating a location config record.
        // The inputs are from location tags which are found in the same config file as the
        // location config configPath, but point to the parent paths of the location config
        // configPath.  See the comment for VSWhidbey 540184 in Init() in
        // BaseConfigurationRecord.cs for more details.

        // The input from location sections
        // This list is ordered to keep oldest ancestors at the front

        // the cached result of evaluating this section

        // the cached result of evaluating this section after GetRuntimeObject is called


        internal SectionRecord(string configKey)
        {
            ConfigKey = configKey;
            Result = s_unevaluated;
            ResultRuntimeObject = s_unevaluated;
        }

        internal string ConfigKey { get; }

        internal bool Locked => _flags[FlagLocked];

        internal bool LockChildren => _flags[FlagLockChildren];

        internal bool LockChildrenWithoutFileInput
        {
            get
            {
                // Start assuming we dont have a file input
                // When we don't have file input the lock mode for children is the same for LockChildren and LockChildrenWithoutFileInput
                bool result = LockChildren;

                if (HasFileInput) result = _flags[FlagChildrenLockWithoutFileInput];

                return result;
            }
        }

        internal bool RequirePermission
        {
            get { return _flags[FlagRequirePermission]; }
            set { _flags[FlagRequirePermission] = value; }
        }

        internal bool AddUpdate
        {
            get { return _flags[FlagAddUpdate]; }
            set { _flags[FlagAddUpdate] = value; }
        }

        internal bool HasLocationInputs => (LocationInputs != null) && (LocationInputs.Count > 0);

        internal List<SectionInput> LocationInputs { get; private set; }

        internal SectionInput LastLocationInput => HasLocationInputs ? LocationInputs[LocationInputs.Count - 1] : null;

        internal bool HasFileInput => FileInput != null;

        internal SectionInput FileInput { get; private set; }

        internal bool HasIndirectLocationInputs
            => (IndirectLocationInputs != null) && (IndirectLocationInputs.Count > 0);

        internal List<SectionInput> IndirectLocationInputs { get; private set; }

        internal SectionInput LastIndirectLocationInput
            => HasIndirectLocationInputs ? IndirectLocationInputs[IndirectLocationInputs.Count - 1] : null;

        internal bool HasInput => HasLocationInputs || HasFileInput || HasIndirectLocationInputs;

        internal bool HasResult => Result != s_unevaluated;

        internal bool HasResultRuntimeObject => ResultRuntimeObject != s_unevaluated;

        internal object Result { get; set; }

        internal object ResultRuntimeObject { get; set; }

        internal bool HasErrors
        {
            get
            {
                if (HasLocationInputs) foreach (SectionInput input in LocationInputs) if (input.HasErrors) return true;

                if (HasIndirectLocationInputs)
                    foreach (SectionInput input in IndirectLocationInputs) if (input.HasErrors) return true;

                return HasFileInput && FileInput.HasErrors;
            }
        }

        internal void
            AddLocationInput(SectionInput sectionInput)
        {
            AddLocationInputImpl(sectionInput, false);
        }

        internal void ChangeLockSettings(OverrideMode forSelf, OverrideMode forChildren)
        {
            if (forSelf != OverrideMode.Inherit)
            {
                _flags[FlagLocked] = forSelf == OverrideMode.Deny;
                _flags[FlagLockChildren] = forSelf == OverrideMode.Deny;
            }

            if (forChildren != OverrideMode.Inherit)
                _flags[FlagLockChildren] = (forSelf == OverrideMode.Deny) || (forChildren == OverrideMode.Deny);
        }

        // AddFileInput
        internal void AddFileInput(SectionInput sectionInput)
        {
            Debug.Assert(sectionInput != null);

            FileInput = sectionInput;

            // If the file input has an explicit value for its children locking - use it
            // Note we dont change the current lock setting
            if (!sectionInput.HasErrors &&
                (sectionInput.SectionXmlInfo.OverrideModeSetting.OverrideMode != OverrideMode.Inherit))
            {
                // Store the current setting before applying the lock from the file input
                // So that if the user changes the current OverrideMode on this configKey to "Inherit"
                // we will know what we are going to inherit ( used in SectionInformation.OverrideModeEffective )
                // Note that we cannot use BaseConfigurationRecord.ResolveOverrideModeFromParent as it gives us only the lock
                // resolved up to our immediate parent which does not inlcude normal and indirect location imputs
                _flags[FlagChildrenLockWithoutFileInput] = LockChildren;

                ChangeLockSettings(OverrideMode.Inherit,
                    sectionInput.SectionXmlInfo.OverrideModeSetting.OverrideMode);
            }
        }

        internal void RemoveFileInput()
        {
            if (FileInput != null)
            {
                FileInput = null;

                // Reset LockChildren flag to the value provided by
                // location input or inherited sections.
                _flags[FlagLockChildren] = Locked;
            }
        }

        internal void
            AddIndirectLocationInput(SectionInput sectionInput)
        {
            AddLocationInputImpl(sectionInput, true);
        }

        private void
            AddLocationInputImpl(SectionInput sectionInput, bool isIndirectLocation)
        {
            List<SectionInput> inputs = isIndirectLocation
                ? IndirectLocationInputs
                : LocationInputs;

            int flag = isIndirectLocation
                ? FlagIndirectLocationInputLockApplied
                : FlagLocationInputLockApplied;

            if (inputs == null)
            {
                inputs = new List<SectionInput>(1);

                if (isIndirectLocation) IndirectLocationInputs = inputs;
                else LocationInputs = inputs;
            }

            // The list of locationSections is traversed from child to parent,
            // so insert at the beginning of the list.
            inputs.Insert(0, sectionInput);

            // Only the overrideMode from the parent thats closest to the SectionRecord has effect
            //
            // For location input:
            // Remember that this method will be called for location inputs coming from the immediate parent first
            // and then walking the hierarchy up to the root level
            //
            // For indirect location input:
            // This method will be first called for indirect input closest to the location config
            if (!sectionInput.HasErrors && !_flags[flag])
            {
                OverrideMode modeLocation = sectionInput.SectionXmlInfo.OverrideModeSetting.OverrideMode;

                if (modeLocation != OverrideMode.Inherit)
                {
                    ChangeLockSettings(modeLocation, modeLocation);
                    _flags[flag] = true;
                }
            }
        }

        internal void ClearRawXml()
        {
            if (HasLocationInputs)
                foreach (SectionInput locationInput in LocationInputs) locationInput.SectionXmlInfo.RawXml = null;

            if (HasIndirectLocationInputs)
            {
                foreach (SectionInput indirectLocationInput in IndirectLocationInputs)
                    indirectLocationInput.SectionXmlInfo.RawXml = null;
            }

            if (HasFileInput) FileInput.SectionXmlInfo.RawXml = null;
        }

        internal void ClearResult()
        {
            FileInput?.ClearResult();

            if (LocationInputs != null) foreach (SectionInput input in LocationInputs) input.ClearResult();

            Result = s_unevaluated;
            ResultRuntimeObject = s_unevaluated;
        }

        private List<ConfigurationException> GetAllErrors()
        {
            List<ConfigurationException> allErrors = null;

            if (HasLocationInputs)
                foreach (SectionInput input in LocationInputs) ErrorsHelper.AddErrors(ref allErrors, input.Errors);

            if (HasIndirectLocationInputs)
            {
                foreach (SectionInput input in IndirectLocationInputs)
                    ErrorsHelper.AddErrors(ref allErrors, input.Errors);
            }

            if (HasFileInput) ErrorsHelper.AddErrors(ref allErrors, FileInput.Errors);

            return allErrors;
        }

        internal void ThrowOnErrors()
        {
            if (HasErrors) throw new ConfigurationErrorsException(GetAllErrors());
        }
    }
}
