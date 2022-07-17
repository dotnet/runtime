// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySchemaPropertyCollection : CollectionBase
    {
        private DirectoryEntry? _classEntry;
        private readonly string _propertyName;
        private readonly ActiveDirectorySchemaClass _schemaClass;
        private readonly bool _isBound;
        private readonly DirectoryContext _context;

        internal ActiveDirectorySchemaPropertyCollection(DirectoryContext context,
                                                        ActiveDirectorySchemaClass schemaClass,
                                                        bool isBound,
                                                        string propertyName,
                                                        ICollection propertyNames,
                                                        bool onlyNames)
        {
            _schemaClass = schemaClass;
            _propertyName = propertyName;
            _isBound = isBound;
            _context = context;

            foreach (string ldapDisplayName in propertyNames)
            {
                // all properties in writeable property collection are non-defunct
                // so calling constructor for non-defunct property
                this.InnerList.Add(new ActiveDirectorySchemaProperty(context, ldapDisplayName, (DirectoryEntry?)null, null));
            }
        }

        internal ActiveDirectorySchemaPropertyCollection(DirectoryContext context,
                                                        ActiveDirectorySchemaClass schemaClass,
                                                        bool isBound,
                                                        string propertyName,
                                                        ICollection properties)
        {
            _schemaClass = schemaClass;
            _propertyName = propertyName;
            _isBound = isBound;
            _context = context;

            foreach (ActiveDirectorySchemaProperty schemaProperty in properties)
            {
                this.InnerList.Add(schemaProperty);
            }
        }

        public ActiveDirectorySchemaProperty this[int index]
        {
            get => (ActiveDirectorySchemaProperty)List[index]!;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!value.isBound)
                {
                    throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, value.Name));
                }

                if (!Contains(value))
                {
                    List[index] = value;
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, value), nameof(value));
                }
            }
        }

        public int Add(ActiveDirectorySchemaProperty schemaProperty)
        {
            ArgumentNullException.ThrowIfNull(schemaProperty);

            if (!schemaProperty.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaProperty.Name));
            }

            if (!Contains(schemaProperty))
            {
                return List.Add(schemaProperty);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, schemaProperty), nameof(schemaProperty));
            }
        }

        public void AddRange(ActiveDirectorySchemaProperty[] properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            foreach (ActiveDirectorySchemaProperty property in properties)
            {
                if (property == null)
                {
                    throw new ArgumentException(null, nameof(properties));
                }
            }

            for (int i = 0; ((i) < (properties.Length)); i = ((i) + (1)))
            {
                this.Add((ActiveDirectorySchemaProperty)properties[i]);
            }
        }

        public void AddRange(ActiveDirectorySchemaPropertyCollection properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            foreach (ActiveDirectorySchemaProperty property in properties)
            {
                if (property == null)
                {
                    throw new ArgumentException(null, nameof(properties));
                }
            }

            int currentCount = properties.Count;

            for (int i = 0; i < currentCount; i++)
            {
                this.Add(properties[i]);
            }
        }

        public void AddRange(ReadOnlyActiveDirectorySchemaPropertyCollection properties)
        {
            ArgumentNullException.ThrowIfNull(properties);

            foreach (ActiveDirectorySchemaProperty property in properties)
            {
                if (property == null)
                {
                    throw new ArgumentException(null, nameof(properties));
                }
            }

            int currentCount = properties.Count;

            for (int i = 0; i < currentCount; i++)
            {
                this.Add(properties[i]);
            }
        }

        public void Remove(ActiveDirectorySchemaProperty schemaProperty)
        {
            ArgumentNullException.ThrowIfNull(schemaProperty);

            if (!schemaProperty.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaProperty.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection, schemaProperty), nameof(schemaProperty));
        }

        public void Insert(int index, ActiveDirectorySchemaProperty schemaProperty)
        {
            ArgumentNullException.ThrowIfNull(schemaProperty);

            if (!schemaProperty.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaProperty.Name));
            }

            if (!Contains(schemaProperty))
            {
                List.Insert(index, schemaProperty);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, schemaProperty), nameof(schemaProperty));
            }
        }

        public bool Contains(ActiveDirectorySchemaProperty schemaProperty)
        {
            ArgumentNullException.ThrowIfNull(schemaProperty);

            if (!schemaProperty.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaProperty.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool Contains(string propertyName)
        {
            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i]!;

                if (Utils.Compare(tmp.Name, propertyName) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(ActiveDirectorySchemaProperty[] properties, int index)
        {
            List.CopyTo(properties, index);
        }

        public int IndexOf(ActiveDirectorySchemaProperty schemaProperty)
        {
            ArgumentNullException.ThrowIfNull(schemaProperty);

            if (!schemaProperty.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaProperty.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaProperty tmp = (ActiveDirectorySchemaProperty)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaProperty.Name) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        protected override void OnClearComplete()
        {
            if (_isBound)
            {
                _classEntry ??= _schemaClass.GetSchemaClassDirectoryEntry();

                try
                {
                    if (_classEntry.Properties.Contains(_propertyName))
                    {
                        _classEntry.Properties[_propertyName].Clear();
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overridden member
        protected override void OnInsertComplete(int index, object value)
#pragma warning restore CS8765
        {
            if (_isBound)
            {
                _classEntry ??= _schemaClass.GetSchemaClassDirectoryEntry();

                try
                {
                    _classEntry.Properties[_propertyName].Add(((ActiveDirectorySchemaProperty)value).Name);
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overridden member
        protected override void OnRemoveComplete(int index, object value)
#pragma warning restore CS8765
        {
            if (_isBound)
            {
                _classEntry ??= _schemaClass.GetSchemaClassDirectoryEntry();

                // because this collection can contain values from the superior classes,
                // these values would not exist in the classEntry
                // and therefore cannot be removed
                // we need to throw an exception here
                string valueName = ((ActiveDirectorySchemaProperty)value).Name;

                try
                {
                    if (_classEntry.Properties[_propertyName].Contains(valueName))
                    {
                        _classEntry.Properties[_propertyName].Remove(valueName);
                    }
                    else
                    {
                        throw new ActiveDirectoryOperationException(SR.ValueCannotBeModified);
                    }
                }
                catch (COMException e)
                {
                    throw ExceptionHelper.GetExceptionFromCOMException(_context, e);
                }
            }
        }

#pragma warning disable CS8765 // Nullability doesn't match overridden member
        protected override void OnSetComplete(int index, object oldValue, object newValue)
#pragma warning restore CS8765
        {
            if (_isBound)
            {
                // remove the old value
                OnRemoveComplete(index, oldValue);

                // add the new value
                OnInsertComplete(index, newValue);
            }
        }

        protected override void OnValidate(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!(value is ActiveDirectorySchemaProperty))
                throw new ArgumentException(null, nameof(value));

            if (!((ActiveDirectorySchemaProperty)value).isBound)
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, ((ActiveDirectorySchemaProperty)value).Name));
        }

        internal string[] GetMultiValuedProperty()
        {
            string[] values = new string[InnerList.Count];
            for (int i = 0; i < InnerList.Count; i++)
            {
                values[i] = ((ActiveDirectorySchemaProperty)InnerList[i]!).Name;
            }
            return values;
        }
    }
}
