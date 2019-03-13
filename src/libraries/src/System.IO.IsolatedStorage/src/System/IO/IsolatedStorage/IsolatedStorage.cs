// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.IO.IsolatedStorage
{
    public abstract class IsolatedStorage : MarshalByRefObject
    {
        private ulong _quota;
        private bool _validQuota;

        private object _applicationIdentity;
        private object _assemblyIdentity;
        private object _domainIdentity;

        protected IsolatedStorage() { }

        public object ApplicationIdentity
        {
            get
            {
                if (Helper.IsApplication(Scope))
                    return _applicationIdentity;

                throw new InvalidOperationException(SR.IsolatedStorage_ApplicationUndefined);
            }
        }

        public object AssemblyIdentity
        {
            get
            {
                if (Helper.IsAssembly(Scope))
                    return _assemblyIdentity;

                throw new InvalidOperationException(SR.IsolatedStorage_AssemblyUndefined);
            }
        }

        public object DomainIdentity
        {
            get
            {
                if (Helper.IsDomain(Scope))
                    return _domainIdentity;

                throw new InvalidOperationException(SR.IsolatedStorage_AssemblyUndefined);
            }
        }

        [CLSCompliant(false)]
        [Obsolete("IsolatedStorage.CurrentSize has been deprecated because it is not CLS Compliant.  To get the current size use IsolatedStorage.UsedSize")]
        public virtual ulong CurrentSize
        {
            get
            {
                throw new InvalidOperationException(SR.Format(SR.IsolatedStorage_CurrentSizeUndefined, nameof(CurrentSize)));
            }
        }

        public virtual long UsedSize
        {
            get
            {
                throw new InvalidOperationException(SR.Format(SR.IsolatedStorage_QuotaIsUndefined, nameof(UsedSize)));
            }
        }

        public virtual long AvailableFreeSpace
        {
            get
            {
                throw new InvalidOperationException(SR.Format(SR.IsolatedStorage_QuotaIsUndefined, nameof(AvailableFreeSpace)));
            }
        }

        [CLSCompliant(false)]
        [Obsolete("IsolatedStorage.MaximumSize has been deprecated because it is not CLS Compliant.  To get the maximum size use IsolatedStorage.Quota")]
        public virtual ulong MaximumSize
        {
            get
            {
                if (_validQuota)
                    return _quota;

                throw new InvalidOperationException(SR.Format(SR.IsolatedStorage_QuotaIsUndefined, nameof(MaximumSize)));
            }
        }

        public virtual long Quota
        {
            get
            {
                if (_validQuota)
                    return (long)_quota;

                throw new InvalidOperationException(SR.Format(SR.IsolatedStorage_QuotaIsUndefined, nameof(Quota)));
            }

            internal set
            {
                _quota = (ulong)value;
                _validQuota = true;
            }
        }

        public IsolatedStorageScope Scope
        {
            get; private set;
        }

        protected virtual char SeparatorExternal
        {
            get { return Path.DirectorySeparatorChar; }
        }

        protected virtual char SeparatorInternal
        {
            get { return '.'; }
        }

        public virtual bool IncreaseQuotaTo(long newQuotaSize)
        {
            return false;
        }

        public abstract void Remove();

        internal string IdentityHash
        {
            get; private set;
        }

        protected void InitStore(IsolatedStorageScope scope, Type appEvidenceType)
        {
            InitStore(scope, null, appEvidenceType);
        }

        protected void InitStore(IsolatedStorageScope scope, Type domainEvidenceType, Type assemblyEvidenceType)
        {
            VerifyScope(scope);
            Scope = scope;

            object identity;
            string hash;
            Helper.GetDefaultIdentityAndHash(out identity, out hash, SeparatorInternal);

            if (Helper.IsApplication(scope))
            {
                _applicationIdentity = identity;
            }
            else
            {
                if (Helper.IsDomain(scope))
                {
                    _domainIdentity = identity;
                    hash = $"{hash}{SeparatorExternal}{hash}";
                }

                _assemblyIdentity = identity;
            }

            IdentityHash = hash;
        }

        private static void VerifyScope(IsolatedStorageScope scope)
        {
            // The only valid ones are the ones that have a helper constant defined above (c_*)

            switch (scope)
            {
                case IsolatedStorageScope.User | IsolatedStorageScope.Assembly:
                case IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain:
                case IsolatedStorageScope.Roaming | IsolatedStorageScope.User | IsolatedStorageScope.Assembly:
                case IsolatedStorageScope.Roaming | IsolatedStorageScope.User | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain:
                case IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly:
                case IsolatedStorageScope.Machine | IsolatedStorageScope.Assembly | IsolatedStorageScope.Domain:
                case IsolatedStorageScope.Application | IsolatedStorageScope.User:
                case IsolatedStorageScope.Application | IsolatedStorageScope.User | IsolatedStorageScope.Roaming:
                case IsolatedStorageScope.Application | IsolatedStorageScope.Machine:
                    break;
                default:
                    throw new ArgumentException(SR.IsolatedStorage_Scope_Invalid);
            }
        }
    }
}
