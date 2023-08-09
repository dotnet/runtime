// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Enumeration
{
#pragma warning disable SA1649
#pragma warning disable SA1402

    public class FirstModel
    {
        [ValidateEnumeratedItems]
        public IList<SecondModel>? P1;

        [ValidateEnumeratedItems(typeof(SecondValidator))]
        public IList<SecondModel>? P2;

        [ValidateEnumeratedItems]
        public IList<SecondModel?>? P3;

        [ValidateEnumeratedItems]
        public IList<ThirdModel>? P4;

        [ValidateEnumeratedItems]
        public IList<ThirdModel?>? P5;

        [ValidateEnumeratedItems]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Testing System>Nullable<T>")]
        public IList<Nullable<ThirdModel>>? P51;

        [ValidateEnumeratedItems]
        public SynteticEnumerable? P6;

        [ValidateEnumeratedItems]
        public SynteticEnumerable P7;

        [ValidateEnumeratedItems]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1125:Use shorthand for nullable types", Justification = "Testing System>Nullable<T>")]
        public Nullable<SynteticEnumerable> P8;
    }

    public class SecondModel
    {
        [Required]
        [MinLength(5)]
        public string P6 = string.Empty;
    }

    public struct ThirdModel
    {
        [Range(0, 10)]
        public int Value;
    }

    public struct SynteticEnumerable : IEnumerable<SecondModel>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<SecondModel> GetEnumerator() => new InternalEnumerator();

        private class InternalEnumerator : IEnumerator<SecondModel>
        {
            public SecondModel Current => throw new NotSupportedException();

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                // Nothing to dispose...
            }

            public bool MoveNext() => false;

            public void Reset() => throw new NotSupportedException();
        }
    }

    [OptionsValidator]
    public partial struct FirstValidator : IValidateOptions<FirstModel>
    {
    }

    [OptionsValidator]
    public partial struct SecondValidator : IValidateOptions<SecondModel>
    {
    }
}
