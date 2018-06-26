// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information. 

//
// System.ComponentModel.LicenseManagerTests test cases
//
// Authors:
//	Ivan Hamilton (ivan@chimerical.com.au)
//	Gonzalo Paniagua Javier (gonzalo@ximian.com)
//	Martin Willemoes Hansen (mwh@sysrq.dk)
//
// (c) 2002 Ximian, Inc. (http://www.ximian.com)
// (c) 2003 Martin Willemoes Hansen
// (c) 2004 Ivan Hamilton

using Xunit;
using System.ComponentModel.Design;

namespace System.ComponentModel.Tests
{
    public class UnlicensedObject
    {
    }

    [LicenseProvider(typeof(TestLicenseProvider))]
    public class LicensedObject
    {
    }

    [LicenseProvider(typeof(TestLicenseProvider))]
    public class InvalidLicensedObject
    {
    }

    [LicenseProvider(typeof(TestLicenseProvider))]
    public class RuntimeLicensedObject
    {
        public RuntimeLicensedObject()
        {
            LicenseManager.Validate(typeof(RuntimeLicensedObject));
        }
        public RuntimeLicensedObject(int a) : this() { }
    }

    [LicenseProvider(typeof(TestLicenseProvider))]
    public class DesigntimeLicensedObject
    {
        public DesigntimeLicensedObject()
        {
            LicenseManager.Validate(typeof(DesigntimeLicensedObject));
        }
    }

    public class TestLicenseProvider : LicenseProvider
    {

        private class TestLicense : License
        {
            public override void Dispose()
            {
            }

            public override string LicenseKey => "YourLicenseKey";
        }

        public TestLicenseProvider() : base()
        {
        }

        public override License GetLicense(LicenseContext context, Type type, object instance, bool allowExceptions)
        {
            if (type.Name.Equals("RuntimeLicensedObject"))
            {
                if (context.UsageMode != LicenseUsageMode.Runtime)
                    if (allowExceptions)
                        throw new LicenseException(type, instance, "License fails because this is a Runtime only license");
                    else
                        return null;
                return new TestLicense();
            }

            if (type.Name.Equals("DesigntimeLicensedObject"))
            {
                if (context.UsageMode != LicenseUsageMode.Designtime)
                    if (allowExceptions)
                        throw new LicenseException(type, instance, "License fails because this is a Designtime only license");
                    else
                        return null;
                return new TestLicense();
            }

            if (type.Name.Equals("LicensedObject"))
                return new TestLicense();

            if (allowExceptions)
                throw new LicenseException(type, instance, "License fails because of class name.");
            else
                return null;
        }
    }

    public class LicenseManagerTests
    {
        [Fact]
        public void Test()
        {
            object lockObject = new object();
            //**DEFAULT CONTEXT & LicenseUsageMode**
            //Get CurrentContext, check default type
            Assert.False(LicenseManager.CurrentContext is DesigntimeLicenseContext);
            //Read default LicenseUsageMode, check against CurrentContext (LicCont).UsageMode
            Assert.Equal(LicenseManager.CurrentContext.UsageMode, LicenseManager.UsageMode);

            //**CHANGING CONTEXT**
            //Change the context and check it changes
            LicenseContext oldcontext = LicenseManager.CurrentContext;
            LicenseContext newcontext = new DesigntimeLicenseContext();
            LicenseManager.CurrentContext = newcontext;
            Assert.Equal(newcontext, LicenseManager.CurrentContext);
            //Check the UsageMode changed too
            Assert.Equal(newcontext.UsageMode, LicenseManager.UsageMode);
            //Set Context back to original
            LicenseManager.CurrentContext = oldcontext;
            //Check it went back
            Assert.Equal(oldcontext, LicenseManager.CurrentContext);
            //Check the UsageMode changed too
            Assert.Equal(oldcontext.UsageMode, LicenseManager.UsageMode);

            //**CONTEXT LOCKING**
            //Lock the context
            LicenseManager.LockContext(lockObject);
            //Try and set new context again, should throw System.InvalidOperationException: The CurrentContext property of the LicenseManager is currently locked and cannot be changed.
            bool exceptionThrown = false;
            try
            {
                LicenseManager.CurrentContext = newcontext;
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(InvalidOperationException), e.GetType());
                exceptionThrown = true;
            }
            //Check the exception was thrown
            Assert.Equal(true, exceptionThrown);
            //Check the context didn't change
            Assert.Equal(oldcontext, LicenseManager.CurrentContext);
            //Unlock it
            LicenseManager.UnlockContext(lockObject);
            //Now's unlocked, change it
            LicenseManager.CurrentContext = newcontext;
            Assert.Equal(newcontext, LicenseManager.CurrentContext);
            //Change it back
            LicenseManager.CurrentContext = oldcontext;


            //Lock the context
            LicenseManager.LockContext(lockObject);
            //Unlock with different "user" should throw System.ArgumentException: The CurrentContext property of the LicenseManager can only be unlocked with the same contextUser.
            object wrongLockObject = new object();
            exceptionThrown = false;
            try
            {
                LicenseManager.UnlockContext(wrongLockObject);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
                exceptionThrown = true;
            }
            Assert.Equal(true, exceptionThrown);
            //Unlock it
            LicenseManager.UnlockContext(lockObject);

            //** bool IsValid(Type);
            Assert.Equal(true, LicenseManager.IsLicensed(typeof(UnlicensedObject)));
            Assert.Equal(true, LicenseManager.IsLicensed(typeof(LicensedObject)));
            Assert.Equal(false, LicenseManager.IsLicensed(typeof(InvalidLicensedObject)));

            Assert.Equal(true, LicenseManager.IsValid(typeof(UnlicensedObject)));
            Assert.Equal(true, LicenseManager.IsValid(typeof(LicensedObject)));
            Assert.Equal(false, LicenseManager.IsValid(typeof(InvalidLicensedObject)));

            UnlicensedObject unlicensedObject = new UnlicensedObject();
            LicensedObject licensedObject = new LicensedObject();
            InvalidLicensedObject invalidLicensedObject = new InvalidLicensedObject();

            //** bool IsValid(Type, object, License);
            License license = null;
            Assert.Equal(true, LicenseManager.IsValid(unlicensedObject.GetType(), unlicensedObject, out license));
            Assert.Equal(null, license);

            license = null;
            Assert.Equal(true, LicenseManager.IsValid(licensedObject.GetType(), licensedObject, out license));
            Assert.Equal("TestLicense", license.GetType().Name);

            license = null;
            Assert.Equal(false, LicenseManager.IsValid(invalidLicensedObject.GetType(), invalidLicensedObject, out license));
            Assert.Equal(null, license);

            //** void Validate(Type);
            //Shouldn't throw exception
            LicenseManager.Validate(typeof(UnlicensedObject));
            //Shouldn't throw exception
            LicenseManager.Validate(typeof(LicensedObject));
            //Should throw exception
            exceptionThrown = false;
            try
            {
                LicenseManager.Validate(typeof(InvalidLicensedObject));
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(LicenseException), e.GetType());
                exceptionThrown = true;
            }
            //Check the exception was thrown
            Assert.Equal(true, exceptionThrown);

            //** License Validate(Type, object);
            //Shouldn't throw exception, returns null license
            license = LicenseManager.Validate(typeof(UnlicensedObject), unlicensedObject);
            Assert.Equal(null, license);

            //Shouldn't throw exception, returns TestLicense license
            license = LicenseManager.Validate(typeof(LicensedObject), licensedObject);
            Assert.Equal("TestLicense", license.GetType().Name);

            //Should throw exception, returns null license
            exceptionThrown = false;
            try
            {
                license = null;
                license = LicenseManager.Validate(typeof(InvalidLicensedObject), invalidLicensedObject);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(LicenseException), e.GetType());
                exceptionThrown = true;
            }
            //Check the exception was thrown
            Assert.Equal(true, exceptionThrown);
            Assert.Equal(null, license);


            //** object CreateWithContext (Type, LicenseContext);
            object cwc = null;
            //Test we can create an unlicensed object with no context
            cwc = LicenseManager.CreateWithContext(typeof(UnlicensedObject), null);
            Assert.Equal("UnlicensedObject", cwc.GetType().Name);
            //Test we can create RunTime with CurrentContext (runtime)
            cwc = null;
            cwc = LicenseManager.CreateWithContext(typeof(RuntimeLicensedObject),
                LicenseManager.CurrentContext);
            Assert.Equal("RuntimeLicensedObject", cwc.GetType().Name);
            //Test we can't create DesignTime with CurrentContext (runtime)
            exceptionThrown = false;
            try
            {
                cwc = null;
                cwc = LicenseManager.CreateWithContext(typeof(DesigntimeLicensedObject), LicenseManager.CurrentContext);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(LicenseException), e.GetType());
                exceptionThrown = true;
            }
            //Check the exception was thrown
            Assert.Equal(true, exceptionThrown);
            //Test we can create DesignTime with A new DesignTimeContext 
            cwc = null;
            cwc = LicenseManager.CreateWithContext(typeof(DesigntimeLicensedObject),
                new DesigntimeLicenseContext());
            Assert.Equal("DesigntimeLicensedObject", cwc.GetType().Name);

            //** object CreateWithContext(Type, LicenseContext, object[]);
            //Test we can create RunTime with CurrentContext (runtime)
            cwc = null;
            cwc = LicenseManager.CreateWithContext(typeof(RuntimeLicensedObject),
                LicenseManager.CurrentContext, new object[] { 7 });
            Assert.Equal("RuntimeLicensedObject", cwc.GetType().Name);

        }
    }
}
