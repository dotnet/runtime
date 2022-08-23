// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.InteropServices;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ActiveDirectorySchemaClassCollection : CollectionBase
    {
        private DirectoryEntry? _classEntry;
        private readonly string _propertyName;
        private readonly ActiveDirectorySchemaClass _schemaClass;
        private readonly bool _isBound;
        private readonly DirectoryContext _context;

        internal ActiveDirectorySchemaClassCollection(DirectoryContext context,
                                                        ActiveDirectorySchemaClass schemaClass,
                                                        bool isBound,
                                                        string propertyName,
                                                        ICollection classNames,
                                                        bool onlyNames)
        {
            _schemaClass = schemaClass;
            _propertyName = propertyName;
            _isBound = isBound;
            _context = context;

            foreach (string ldapDisplayName in classNames)
            {
                // all properties in writeable class collection are non-defunct
                // so calling constructor for non-defunct class
                InnerList.Add(new ActiveDirectorySchemaClass(context, ldapDisplayName, (DirectoryEntry?)null, null));
            }
        }

        internal ActiveDirectorySchemaClassCollection(DirectoryContext context,
                                                        ActiveDirectorySchemaClass schemaClass,
                                                        bool isBound,
                                                        string propertyName,
                                                        ICollection classes)
        {
            _schemaClass = schemaClass;
            _propertyName = propertyName;
            _isBound = isBound;
            _context = context;

            foreach (ActiveDirectorySchemaClass schClass in classes)
            {
                InnerList.Add(schClass);
            }
        }

        public ActiveDirectorySchemaClass this[int index]
        {
            get => (ActiveDirectorySchemaClass)List[index]!;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

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

        public int Add(ActiveDirectorySchemaClass schemaClass)
        {
            ArgumentNullException.ThrowIfNull(schemaClass);

            if (!schemaClass.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaClass.Name));
            }

            if (!Contains(schemaClass))
            {
                return List.Add(schemaClass);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, schemaClass), nameof(schemaClass));
            }
        }

        public void AddRange(ActiveDirectorySchemaClass[] schemaClasses)
        {
            ArgumentNullException.ThrowIfNull(schemaClasses);

            foreach (ActiveDirectorySchemaClass schemaClass in schemaClasses)
            {
                if (schemaClass == null)
                {
                    throw new ArgumentException(null, nameof(schemaClasses));
                }
            }

            for (int i = 0; ((i) < (schemaClasses.Length)); i = ((i) + (1)))
            {
                Add(schemaClasses[i]);
            }
        }

        public void AddRange(ActiveDirectorySchemaClassCollection schemaClasses)
        {
            ArgumentNullException.ThrowIfNull(schemaClasses);

            foreach (ActiveDirectorySchemaClass schemaClass in schemaClasses)
            {
                if (schemaClass == null)
                {
                    throw new ArgumentException(null, nameof(schemaClasses));
                }
            }

            int currentCount = schemaClasses.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(schemaClasses[i]);
            }
        }

        public void AddRange(ReadOnlyActiveDirectorySchemaClassCollection schemaClasses)
        {
            ArgumentNullException.ThrowIfNull(schemaClasses);

            foreach (ActiveDirectorySchemaClass schemaClass in schemaClasses)
            {
                if (schemaClass == null)
                {
                    throw new ArgumentException(null, nameof(schemaClasses));
                }
            }

            int currentCount = schemaClasses.Count;
            for (int i = 0; i < currentCount; i++)
            {
                Add(schemaClasses[i]);
            }
        }

        public void Remove(ActiveDirectorySchemaClass schemaClass)
        {
            ArgumentNullException.ThrowIfNull(schemaClass);

            if (!schemaClass.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaClass.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaClass tmp = (ActiveDirectorySchemaClass)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaClass.Name) == 0)
                {
                    List.Remove(tmp);
                    return;
                }
            }
            throw new ArgumentException(SR.Format(SR.NotFoundInCollection, schemaClass), nameof(schemaClass));
        }

        public void Insert(int index, ActiveDirectorySchemaClass schemaClass)
        {
            ArgumentNullException.ThrowIfNull(schemaClass);

            if (!schemaClass.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaClass.Name));
            }

            if (!Contains(schemaClass))
            {
                List.Insert(index, schemaClass);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.AlreadyExistingInCollection, schemaClass), nameof(schemaClass));
            }
        }

        public bool Contains(ActiveDirectorySchemaClass schemaClass)
        {
            ArgumentNullException.ThrowIfNull(schemaClass);

            if (!schemaClass.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaClass.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaClass tmp = (ActiveDirectorySchemaClass)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaClass.Name) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(ActiveDirectorySchemaClass[] schemaClasses, int index)
        {
            List.CopyTo(schemaClasses, index);
        }

        public int IndexOf(ActiveDirectorySchemaClass schemaClass)
        {
            ArgumentNullException.ThrowIfNull(schemaClass);

            if (!schemaClass.isBound)
            {
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, schemaClass.Name));
            }

            for (int i = 0; i < InnerList.Count; i++)
            {
                ActiveDirectorySchemaClass tmp = (ActiveDirectorySchemaClass)InnerList[i]!;
                if (Utils.Compare(tmp.Name, schemaClass.Name) == 0)
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
                    _classEntry.Properties[_propertyName].Add(((ActiveDirectorySchemaClass)value).Name);
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
                string valueName = ((ActiveDirectorySchemaClass)value).Name;

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

            if (!(value is ActiveDirectorySchemaClass))
            {
                throw new ArgumentException(null, nameof(value));
            }

            if (!((ActiveDirectorySchemaClass)value).isBound)
                throw new InvalidOperationException(SR.Format(SR.SchemaObjectNotCommitted, ((ActiveDirectorySchemaClass)value).Name));
        }

        internal string[] GetMultiValuedProperty()
        {
            string[] values = new string[InnerList.Count];
            for (int i = 0; i < InnerList.Count; i++)
            {
                values[i] = ((ActiveDirectorySchemaClass)InnerList[i]!).Name;
            }
            return values;
        }
    }
}
