// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.Security.Authentication.ExtendedProtection.Tests
{
    public class ExtendedProtectionPolicyTypeConverterTests
    {
    	[Theory]
    	[InlineData(typeof(int))]
    	[InlineData(typeof(ExtendedProtectionPolicy))]
    	[InlineData(typeof(bool))]
    	[InlineData(null)]
    	[InlineData(typeof(float))]
    	[InlineData(typeof(TypeConverter))]
    	public void CanConvertTo_NegativeTests(Type destinationType)
    	{
			Assert.False(converter.CanConvertTo(null, destinationType)); 		
    	}

    	[Fact]
    	public void CanConvertTo_PositiveTests()
    	{
    		Assert.True(converter.CanConvertTo(null, typeof(string)));
    		Assert.True(converter.CanConvertTo(null, typeof(InstanceDescriptor)));
    	}

    	[Fact]
    	public void ConvertTo_NullTypeTests()
    	{
    		Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, new ExtendedProtectionPolicy(PolicyEnforcement.Never), null));
    	}

    	[Fact]
    	public void ConvertTo_PositiveTests()
    	{
    		ExtendedProtectionPolicy policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
    		
    		InstanceDescriptor instanceDescriptor = converter.ConvertTo(null, CultureInfo.InvariantCulture, policy, typeof(InstanceDescriptor)) as InstanceDescriptor;
    		ExtendedProtectionPolicy instanceResult = instanceDescriptor.Invoke() as ExtendedProtectionPolicy;
    		Assert.NotNull(instanceDescriptor);
    		Assert.NotNull(instanceResult);
    		Assert.Equal(PolicyEnforcement.Never, instanceResult.PolicyEnforcement);
    		Assert.Equal(policy.ProtectionScenario, instanceResult.ProtectionScenario);
    		Assert.Null(instanceResult.CustomServiceNames);

    		Assert.Equal(string.Empty, 
    			converter.ConvertTo(null, CultureInfo.InvariantCulture, null, typeof(string)) as string);
    		Assert.Equal(policy.ToString(), 
    			converter.ConvertTo(null, CultureInfo.InvariantCulture, policy, typeof(string)) as string);
    	}

    	[Theory]
    	[InlineData(typeof(int))]
    	[InlineData(typeof(ExtendedProtectionPolicy))]
    	[InlineData(typeof(bool))]
    	[InlineData(typeof(float))]
    	[InlineData(typeof(TypeConverter))]
    	public void ConvertTo_NegativeTests(Type destinationType)
    	{
    		ExtendedProtectionPolicy policy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);

    		Assert.Throws<NotSupportedException>(() => converter.ConvertTo(null, CultureInfo.InvariantCulture, policy, destinationType));
    	}

    	private ExtendedProtectionPolicyTypeConverter converter = new ExtendedProtectionPolicyTypeConverter();
    }
}
