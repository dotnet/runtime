// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net
{
    // TrackingValidationObjectDictionary uses an internal collection of objects to store
    // only those objects which are not strings.  It still places a copy of the string
    // value of these objects into the base StringDictionary so that the public methods
    // of StringDictionary still function correctly.
    // NOTE:  all keys are converted to lowercase prior to adding to ensure consistency of
    // values between  keys in the StringDictionary and internalObjects because StringDictionary
    // automatically does this internally
    internal sealed class TrackingValidationObjectDictionary : StringDictionary
    {
        #region Private Fields

        // even though validators may exist, we should not initialize this initially since by default it is empty
        // and it may never be populated with values if the user does not set them
        private readonly Dictionary<string, ValidateAndParseValue> _validators;
        private Dictionary<string, object>? _internalObjects;

        #endregion

        #region Constructors

        // it is valid for validators to be null.  this means that no validation should be performed
        internal TrackingValidationObjectDictionary(Dictionary<string, ValidateAndParseValue> validators)
        {
            IsChanged = false;
            _validators = validators;
        }

        #endregion

        #region Private Methods

        // precondition:  key must not be null
        // addValue determines if we are doing a set (false) or an add (true)
        private void PersistValue(string key, string? value, bool addValue)
        {
            Debug.Assert(key != null, "key was null");

            // StringDictionary will automatically store the key as lower case so
            // we must convert it so that the validators and internalObjects will
            // be consistent
            key = key.ToLowerInvariant();

            // StringDictionary allows keys with null values however null values for parameters in
            // ContentDisposition have no meaning so they must be ignored on add. StringDictionary
            // would not throw on null so this can't either since it would be a breaking change.
            // in addition, a key with an empty value is not valid so we do not persist those either
            if (!string.IsNullOrEmpty(value))
            {
                ValidateAndParseValue? foundEntry;
                if (_validators != null && _validators.TryGetValue(key, out foundEntry))
                {
                    // run the validator for this key; it will throw if the value is invalid
                    object valueToAdd = foundEntry(value);

                    // now that the value is valid, ensure that internalObjects exists since we have to
                    // add to it
                    _internalObjects ??= new Dictionary<string, object>();

                    if (addValue)
                    {
                        // set will do an Add if the key does not exist but if the user
                        // specifically called Add then we must let it throw
                        _internalObjects.Add(key, valueToAdd);
                        base.Add(key, valueToAdd.ToString());
                    }
                    else
                    {
                        _internalObjects[key] = valueToAdd;
                        base[key] = valueToAdd.ToString();
                    }
                }
                else
                {
                    if (addValue)
                    {
                        base.Add(key, value);
                    }
                    else
                    {
                        base[key] = value;
                    }
                }
                IsChanged = true;
            }
        }

        #endregion

        #region Internal Fields

        // set to true if any values have been changed by any mutator method
        internal bool IsChanged { get; set; }

        // delegate to perform validation and conversion if necessary
        // these MUST throw on invalid values.  Additionally, each validator
        // may be passed a string OR another type of object and so should react
        // appropriately
        internal delegate object ValidateAndParseValue(object valueToValidate);

        #endregion

        #region Internal Methods

        // public interface only allows strings so this provides a means
        // to get the objects when they are not strings
        internal object? InternalGet(string key)
        {
            // internalObjects will throw if the key is not found so we must check it
            object? foundObject;
            if (_internalObjects != null && _internalObjects.TryGetValue(key, out foundObject))
            {
                return foundObject;
            }
            else
            {
                // this will return null if the key does not exist so no check needed
                return base[key];
            }
        }

        // this method bypasses validation
        // preconditions: value MUST have been validated and must not be null
        internal void InternalSet(string key, object value)
        {
            // InternalSet is only used with objects that belong in internalObjects so we must always
            // initialize it here
            _internalObjects ??= new Dictionary<string, object>();

            // always replace the existing value when we set internally
            _internalObjects[key] = value;
            base[key] = value.ToString();
            IsChanged = true;
        }

        #endregion

        #region Public Fields

        public override string? this[string key]
        {
            get
            {
                // no need to check internalObjects since the string equivalent in base will
                // already have been set correctly when the value was originally passed in
                return base[key];
            }
            set
            {
                PersistValue(key, value, false);
            }
        }

        #endregion

        #region Public Methods

        public override void Add(string key, string? value)
        {
            PersistValue(key, value, true);
        }

        public override void Clear()
        {
            _internalObjects?.Clear();
            base.Clear();
            IsChanged = true;
        }

        public override void Remove(string key)
        {
            _internalObjects?.Remove(key);
            base.Remove(key);
            IsChanged = true;
        }

        #endregion
    }
}
