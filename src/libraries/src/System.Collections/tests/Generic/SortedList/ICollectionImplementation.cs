// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TestSupport;
using TestSupport.Collections;
using Xunit;
using TestSupport.Common_TestSupport;
using TestSupport.Collections.SortedList_GenericICollectionTest;
using TestSupport.Collections.SortedList_GenericIEnumerableTest;
using SortedList_SortedListUtils;

namespace SortedListIColIpml
{
    public class Driver<K, V>
    {
        public bool Verify(GenerateItem<K> keyGenerator, GenerateItem<V> valueGenerator)
        {
            SortedList<K, V> d = new SortedList<K, V>();
            KeyValuePairGenerator<K, V> keyValuePairGenerator = new KeyValuePairGenerator<K, V>(keyGenerator, valueGenerator);
            ICollection_T_Test<KeyValuePair<K, V>> iCollectionTest = new ICollection_T_Test<KeyValuePair<K, V>>(d,
                new GenerateItem<KeyValuePair<K, V>>(keyValuePairGenerator.Generate), null, false);

            bool retValue = true;

            iCollectionTest.ItemsMustBeUnique = true;
            iCollectionTest.ItemsMustBeNonNull = default(K) == null;
            iCollectionTest.CollectionOrder = TestSupport.CollectionOrder.Unspecified;
            iCollectionTest.Converter = ConverterHelper.DictionaryEntryToKeyValuePairConverter<K, V>;

            retValue &= SortedList_SortedListUtils.Test.Eval(iCollectionTest.RunAllTests(), "Err_98382apeuie System.Collections.Generic.ICollection<KeyValuePair<K, V>> tests FAILED");

            return retValue;
        }
    }

    public class KeyValuePairGenerator<K, V>
    {
        private GenerateItem<K> _keyGenerator;
        private GenerateItem<V> _valueGenerator;

        public KeyValuePairGenerator(GenerateItem<K> keyGenerator, GenerateItem<V> valueGenerator)
        {
            _keyGenerator = keyGenerator;
            _valueGenerator = valueGenerator;
        }

        public KeyValuePair<K, V> Generate()
        {
            return new KeyValuePair<K, V>(_keyGenerator(), _valueGenerator());
        }
    }
    public class ICollectionImplementation
    {
        public class IntGenerator
        {
            private int _index;

            public IntGenerator()
            {
                _index = 0;
            }

            public int NextValue()
            {
                return _index++;
            }

            public Object NextValueObject()
            {
                return (Object)NextValue();
            }
        }

        public class StringGenerator
        {
            private int _index;

            public StringGenerator()
            {
                _index = 0;
            }

            public String NextValue()
            {
                return (_index++).ToString();
            }

            public Object NextValueObject()
            {
                return (Object)NextValue();
            }
        }

        [Fact]
        public static void IColImplMain()
        {
            IntGenerator intGenerator = new IntGenerator();
            StringGenerator stringGenerator = new StringGenerator();

            intGenerator.NextValue();
            stringGenerator.NextValue();

            Driver<int, string> intStringDriver = new Driver<int, string>();
            Driver<string, int> stringIntDriver = new Driver<string, int>();

            SortedList_SortedListUtils.Test.Eval(intStringDriver.Verify(new GenerateItem<int>(intGenerator.NextValue), new GenerateItem<string>(stringGenerator.NextValue)),
                "Err_658865eido Test Int32, String FAILED\n");

            SortedList_SortedListUtils.Test.Eval(stringIntDriver.Verify(new GenerateItem<string>(stringGenerator.NextValue), new GenerateItem<int>(intGenerator.NextValue)),
                "Err_40845ahekd Test String, Int32 FAILED\n");

            Assert.True(SortedList_SortedListUtils.Test.result);
        }
    }
}