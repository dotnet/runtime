// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Management.Tests
{
    [Collection("Mof Collection")]
    public class ManagementClassTestsMofRequired
    {
        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        public void Create_Modify_Delete_Static_Class()
        {
            using (var newClass = new ManagementClass(WmiTestHelper.Namespace))
            {
                const string NewClassName = "CoreFX_Create_Modify_Delete_Static_Class\uEE68\uD79D\u1659";
                const string PropertyName = "Key";
                const int PropertyValue = 10;

                newClass["__CLASS"] = NewClassName;
                newClass.Properties.Add(PropertyName, CimType.SInt32, false);
                newClass.Properties[PropertyName].Qualifiers.Add("key", true);
                newClass.Put();

                var targetClass = new ManagementClass(WmiTestHelper.Namespace, NewClassName, null);
                targetClass.Get();

                newClass[PropertyName] = PropertyValue;
                newClass.Put();
                targetClass.Get();
                Assert.Equal(PropertyValue, (int)targetClass[PropertyName]);

                // If any of the steps below fail it is likely that the new class was not deleted, likely it will have to
                // be deleted via a tool like wbemtest.
                newClass.Delete();
                ManagementException managementException = Assert.Throws<ManagementException>(() => targetClass.Get());
                Assert.Equal(ManagementStatus.NotFound, managementException.ErrorCode);
            }
        }

        [ConditionalFact(typeof(WmiTestHelper), nameof(WmiTestHelper.IsElevatedAndSupportsWmi))]
        public void Create_Modify_Delete_Static_And_Instance()
        {
            using (var newClass = new ManagementClass(WmiTestHelper.Namespace))
            {
                const string NewClassName = "CoreFX_Create_Static_Class_And_Instance";
                const string KeyPropertyName = "Key";
                const int KeyPropertyValue = 1;
                const string MoviePropertyName = "Movie";
                const string OldMovieValue = "Sequel I";
                const string NewMovieValue = "Sequel II";

                newClass["__CLASS"] = NewClassName;
                newClass.Properties.Add(KeyPropertyName, CimType.SInt32, false);
                newClass.Properties[KeyPropertyName].Qualifiers.Add("key", true);
                newClass.Properties.Add(MoviePropertyName, CimType.String, false);
                newClass.Put();

                ManagementObject newInstance = newClass.CreateInstance();
                newInstance[KeyPropertyName] = KeyPropertyValue;
                newInstance[MoviePropertyName] = OldMovieValue;
                newInstance.Put();

                var targetInstance = new ManagementObject(
                    WmiTestHelper.Namespace, $"{NewClassName}.{KeyPropertyName}='{KeyPropertyValue}'", null);
                targetInstance.Get();
                Assert.Equal(OldMovieValue, targetInstance[MoviePropertyName].ToString());

                newInstance[MoviePropertyName] = NewMovieValue;
                newInstance.Put();
                Assert.Equal(NewMovieValue, newInstance[MoviePropertyName].ToString());

                targetInstance.Get();
                Assert.Equal(NewMovieValue, targetInstance[MoviePropertyName].ToString());

                // If any of the steps below fail it is likely that the new class was not deleted, likely it will have to
                // be deleted via a tool like wbemtest.
                newInstance.Delete();
                ManagementException managementException = Assert.Throws<ManagementException>(() => targetInstance.Get());
                Assert.Equal(ManagementStatus.NotFound, managementException.ErrorCode);

                // If any of the steps below fail it is likely that the new class was not deleted, likely it will have to
                // be deleted via a tool like wbemtest.
                newClass.Delete();
                managementException = Assert.Throws<ManagementException>(() => newClass.Get());
                Assert.Equal(ManagementStatus.NotFound, managementException.ErrorCode);
            }
        }
    }
}
