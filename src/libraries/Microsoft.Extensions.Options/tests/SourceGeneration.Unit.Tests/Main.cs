// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Generators;
using SourceGenerators.Tests;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Unit.Test;

public class EmitterTests
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task TestEmitterWithCustomValidator()
    {
        string source = """
            using System;
            using System.ComponentModel.DataAnnotations;
            using Microsoft.Extensions.Options;

            #nullable enable

            namespace HelloWorld
            {
                public class MyOptions
                {
                    [Required]
                    public string Val1 { get; set; } = string.Empty;

                    [Range(1, 3)]
                    public int Val2 { get; set; }
                }

                [OptionsValidator]
                public partial struct MyOptionsValidator : IValidateOptions<MyOptions>
                {
                }
            }
            """;

        var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source);
        Assert.Empty(diagnostics);
        _ = Assert.Single(generatedSources);

#if NETCOREAPP
        string generatedSource = File.ReadAllText(@"Baselines/EmitterWithCustomValidator.netcore.g.cs");
#else
        string generatedSource = File.ReadAllText(@"Baselines/EmitterWithCustomValidator.netfx.g.cs");
#endif // NETCOREAPP
        Assert.Equal(generatedSource.Replace("\r\n", "\n"), generatedSources[0].SourceText.ToString().Replace("\r\n", "\n"));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task PotentiallyMissingAttributes()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public SecondModel? P1 { get; set; }

                [Required]
                public System.Collections.Generic.IList<SecondModel>? P2 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(DiagDescriptors.PotentiallyMissingTransitiveValidation.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.PotentiallyMissingEnumerableValidation.Id, diagnostics[1].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task IgnoredStaticMembers()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                // Since we ignore static members, we shouldn't check SecondModel,
                // and shouldn't emit the 'SYSLIB1212' warning about potentially missing transitive validation
                public static SecondModel? P1 { get; set; }

                public static SecondModel P2 = new();

                public static System.Collections.Generic.IList<SecondModel>? P3 { get; set; }

                public const SecondModel P4 = null;

                [Required]
                public string Name { get; set; } = nameof(FirstModel);
            }

            public class SecondModel
            {
                [Required]
                public string? P3;
            }

            [OptionsValidator]
            public partial class FirstModelValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ValidationAttributeOnStaticMember()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public static string? P1 { get; set; }

                [Required]
                public const string? P1;

                [ValidateObjectMembers]
                public static SecondModel P2 { get; set; } = new();

                [ValidateEnumeratedItems]
                public static System.Collections.Generic.IList<SecondModel>? P3 { get; set; }

                [Required]
                public string Name { get; set; } = nameof(FirstModel);
            }

            public class SecondModel
            {
                [Required]
                public string? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstModelValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Equal(3, d.Count);
        Assert.All(d, x => Assert.Equal(DiagDescriptors.CantValidateStaticOrConstMember.Id, x.Id));
        Assert.All(d, x => Assert.Equal(DiagnosticSeverity.Warning, x.DefaultSeverity));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CircularTypeReferences()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateObjectMembers]
                public FirstModel? P1 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.CircularTypeReferences.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task InvalidValidatorInterface()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P2 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotValidator()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P2 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            public partial class SecondValidator
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ValidatorAlreadyImplementValidateFunction()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1 { get; set; }

                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P2 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
                public ValidateOptionsResult Validate(string name, SecondModel options)
                {
                    throw new System.NotSupportedException();
                }
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.AlreadyImplementsValidateMethod.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NullValidator()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateObjectMembers(null!)]
                public SecondModel? P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P2 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.NullValidatorType.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoSimpleValidatorConstructor()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1 { get; set; }

                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P2 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
                public SecondValidator(int _)
                {
                }
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.ValidatorsNeedSimpleConstructor.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoStaticValidator()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string P1 { get; set; }
            }

            [OptionsValidator]
            public static partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.CantBeStaticClass.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task BogusModelType()
    {
        var (diagnostics, _) = await RunGenerator(@"
            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<Bogus>
            {
            }
        ");

        // the generator doesn't produce any errors here, since the C# compiler will take care of it
        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CantValidateOpenGenericMembers()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateObjectMembers]
                public T? P1 { get; set; }

                [ValidateObjectMembers]
                [Required]
                public T[]? P2 { get; set; }

                [ValidateObjectMembers]
                [Required]
                public System.Collections.Generics.IList<T> P3 { get; set;} = null!;
            }

            [OptionsValidator]
            public partial class FirstValidator<T> : IValidateOptions<FirstModel<T>>
            {
            }
        ");

        Assert.Equal(3, diagnostics.Count);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[1].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ClosedGenerics()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateObjectMembers]
                public T? P1 { get; set; }

                [ValidateObjectMembers]
                [Required]
                public T[]? P2 { get; set; }

                [ValidateObjectMembers]
                [Required]
                public int[]? P3 { get; set; }

                [ValidateObjectMembers]
                [Required]
                public System.Collections.Generics.IList<T>? P4 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel<string>>
            {
            }
        ");

        Assert.Equal(4, diagnostics.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[1].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[2].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[3].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoEligibleMembers()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateObjectMembers]
                public SecondModel? P1 { get; set; }
            }

            public class SecondModel
            {
                public string P2 { get; set; };
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
            }
        ");

        Assert.Equal(2, diagnostics.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMembersFromValidator.Id, diagnostics[1].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task AlreadyImplemented()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string One { get; set; } = string.Empty;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
                public void Validate(string name, FirstModel fm)
                {
                }
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.AlreadyImplementsValidateMethod.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceInfoWhenTheClassHasABaseClass()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class Parent
                {
                    [Required]
                    public string parentString { get; set; }
                }

                public class Child : Parent
                {
                    [Required]
                    public string childString { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<Child>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceInfoWhenTransitiveClassHasABaseClass()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class Parent
                {
                    [Required]
                    public string parentString { get; set; }
                }

                public class Child : Parent
                {
                    [Required]
                    public string childString { get; set; }
                }

                public class MyOptions
                {
                    [ValidateObjectMembers]
                    public Child childVal { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<MyOptions>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    [InlineData("bool")]
    [InlineData("int")]
    [InlineData("double")]
    [InlineData("string")]
    [InlineData("System.String")]
    [InlineData("System.DateTime")]
    public async Task ShouldProduceWarn_WhenTransitiveAttrMisused(string memberClass)
    {
        var (diagnostics, _) = await RunGenerator(@$"
                public class InnerModel
                {{
                    [Required]
                    public string childString {{ get; set; }}
                }}

                public class MyOptions
                {{
                    [Required]
                    public string simpleVal {{ get; set; }}

                    [ValidateObjectMembers]
                    public {memberClass} complexVal {{ get; set; }}
                }}

                [OptionsValidator]
                public partial class Validator : IValidateOptions<MyOptions>
                {{
                }}
            ");

        Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldProduceWarningWhenTheClassHasNoEligibleMembers()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class Child
                {
                    private string AccountName { get; set; }
                    public object Weight;
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<Child>
                {
                }
            ");

        Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.NoEligibleMembersFromValidator.Id, diagnostics[0].Id);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    [InlineData("private")]
    [InlineData("protected")]
    public async Task ShouldProduceWarningWhenTheClassMembersAreInaccessible(string accessModifier)
    {
        var (diagnostics, _) = await RunGenerator($@"
                public class Model
                {{
                    [Required]
                    public string? PublicVal {{ get; set; }}

                    [Required]
                    {accessModifier} string? Val {{ get; set; }}
                }}

                [OptionsValidator]
                public partial class Validator : IValidateOptions<Model>
                {{
                }}
            ");

        Assert.Single(diagnostics);
        Assert.Equal("SYSLIB1206", diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenMultipleValidationAnnotationsExist()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    [MinLength(5)]
                    [MaxLength(15)]
                    public string Val9 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenDataTypeAttributesAreUsed()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    [CreditCard]
                    public string Val3 = """";

                    [EmailAddress]
                    public string Val6 { get; set; }

                    [EnumDataType(typeof(string))]
                    public string Val7 { get; set; }

                    [FileExtensions]
                    public string Val8 { get; set; }

                    [Phone]
                    public string Val10 { get; set; }

                    [Url]
                    public string Val11 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenConstVariableIsUsedAsAttributeArgument()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    private const int q = 5;
                    [Range(q, 10)]
                    public string Val11 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    // Testing on all existing & eligible annotations extending ValidationAttribute that aren't used above
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceAnyMessagesWhenExistingValidationsArePlaced()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    [Required]
                    public string Val { get; set; }

                    [Compare(""val"")]
                    public string Val2 { get; set; }

                    [DataType(DataType.Password)]
                    public string _val5 = """";

                    [Range(5.1, 10.11)]
                    public string Val12 { get; set; }

                    [Range(typeof(MemberDeclarationSyntax), ""1/2/2004"", ""3/4/2004"")]
                    public string Val14 { get; set; }

                    [RegularExpression("""")]
                    public string Val15 { get; set; }

                    [StringLength(5)]
                    public string Val16 { get; set; }

                    [CustomValidation(typeof(MemberDeclarationSyntax), ""CustomMethod"")]
                    public string Val17 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenPropertiesAreUsedAsAttributeArgument()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    private const int q = 5;
                    [Range(q, 10, ErrorMessage = ""ErrorMessage"")]
                    public string Val11 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipWhenOptionsValidatorAttributeDoesNotExist()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    private const int q = 5;
                    [Range(q, 10, ErrorMessage = ""ErrorMessage"")]
                    public string Val11 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ", includeOptionValidatorReferences: false);

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipAtrributeWhenAttributeSymbolCannotBeFound()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    [RandomTest]
                    public string Val11 { get; set; }

                    [Range(1, 10, ErrorMessage = ""ErrorMessage"")]
                    public string Val12 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipAtrributeWhenAttributeSymbolIsNotBasedOnValidationAttribute()
    {
        var (diagnostics, _) = await RunGenerator(@"
                public class IValidateOptionsTestFile
                {
                    [FilterUIHint(""MultiForeignKey"")]
                    public string Val11 { get; set; }

                    [Range(1, 10, ErrorMessage = ""ErrorMessage"")]
                    public string Val12 { get; set; }
                }

                [OptionsValidator]
                public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                {
                }
            ");

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldAcceptAtrributeWhenAttributeIsInDifferentNamespace()
    {
        var (diagnostics, _) = await RunGenerator(@"
                namespace Test {
                    public class IValidateOptionsTestFile
                    {
                        [Test]
                        public string Val11 { get; set; }
                    }

                    [AttributeUsage(AttributeTargets.Class)]
                    public sealed class TestAttribute : ValidationAttribute
                    {
                    }

                    [OptionsValidator]
                    public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                    {
                    }
                }
            ", inNamespace: false);

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldHandleAtrributePropertiesOtherThanString()
    {
        var (diagnostics, _) = await RunGenerator(@"
                namespace Test {
                    public class IValidateOptionsTestFile
                    {
                        [Test(num = 5)]
                        public string Val11 { get; set; }

                        [Required]
                        public string Val12 { get; set; }
                    }

                    [OptionsValidator]
                    public partial class Validator : IValidateOptions<IValidateOptionsTestFile>
                    {
                    }
                }

                namespace System.ComponentModel.DataAnnotations {
                    [AttributeUsage(AttributeTargets.Class)]
                    public sealed class TestAttribute : ValidationAttribute
                    {
                        public int num { get; set; }
                        public TestAttribute() {
                        }
                    }
                }
            ", inNamespace: false);

        Assert.Empty(diagnostics);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldStoreFloatValuesCorrectly()
    {
        var backupCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("ru-RU", false);
        try
        {
            var (diagMessages, generatedResults) = await RunGenerator(@"
                    public class Model
                    {
                        [Range(-0.1, 1.3)]
                        public string Val { get; set; }
                    }

                    [OptionsValidator]
                    public partial class Validator : IValidateOptions<Model>
                    {
                    }
                ");

            Assert.Empty(diagMessages);
            Assert.Single(generatedResults);
            Assert.DoesNotContain("0,1", generatedResults[0].SourceText.ToString());
            Assert.DoesNotContain("1,3", generatedResults[0].SourceText.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = backupCulture;
        }
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task MultiModelValidatorGeneratesOnlyOnePartialTypeBlock()
    {
        var (diagnostics, sources) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string P2 { get; set; }
            }

            public class ThirdModel
            {
                [Required]
                public string P3 { get; set; }
            }

            [OptionsValidator]
            public partial class MultiValidator : IValidateOptions<FirstModel>, IValidateOptions<SecondModel>, IValidateOptions<ThirdModel>
            {
            }
        ");

        var typeDeclarations = sources[0].SyntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .ToArray();

        var multiValidatorTypeDeclarations = typeDeclarations
            .Where(x => x.Identifier.ValueText == "MultiValidator")
            .ToArray();

        Assert.Single(multiValidatorTypeDeclarations);

        var validateMethodDeclarations = multiValidatorTypeDeclarations[0]
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(x => x.Identifier.ValueText == "Validate")
            .ToArray();

        Assert.Equal(3, validateMethodDeclarations.Length);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CircularTypeReferencesInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateEnumeratedItems]
                public FirstModel[]? P1 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.CircularTypeReferences.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotValidatorInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateEnumeratedItems(typeof(SecondValidator)]
                public SecondModel[]? P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P2 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            public partial class SecondValidator
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NullValidatorInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateEnumeratedItems(null!)]
                public SecondModel[]? P1 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P2 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.NullValidatorType.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoSimpleValidatorConstructorInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1 { get; set; }

                [ValidateEnumeratedItems(typeof(SecondValidator)]
                public SecondModel[]? P2 { get; set; }
            }

            public class SecondModel
            {
                [Required]
                public string? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            [OptionsValidator]
            public partial class SecondValidator : IValidateOptions<SecondModel>
            {
                public SecondValidator(int _)
                {
                }
            }
        ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.ValidatorsNeedSimpleConstructor.Id, diagnostics[0].Id);
    }

    private static bool SupportRemoteExecutionAndNotInBrowser => RemoteExecutor.IsSupported && !PlatformDetection.IsBrowser;

    [ConditionalFact(nameof(SupportRemoteExecutionAndNotInBrowser))]
    public void ProduceDiagnosticFromOtherAssemblyTest()
    {
        string source = """
            using System.ComponentModel.DataAnnotations;

            #nullable enable

            namespace AnotherAssembly;

            public class ClassInAnotherAssembly
            {
                [Required]
                public string? Foo { get; set; }

                // line below causes the generator to emit a warning "SYSLIB1212" but the original location is outside of its compilation (SyntaxTree).
                // The generator should emit this diagnostics pointing at the closest location of the failure inside the compilation.
                public SecondClassInAnotherAssembly? TransitiveProperty { get; set; }
            }

            public class SecondClassInAnotherAssembly
            {
                [Required]
                public string? Bar { get; set; }
            }
            """;

        string assemblyName = Path.GetRandomFileName();
        string assemblyPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        CSharpCompilation compilation = CreateCompilationForOptionsSource(assemblyName, source);
        EmitResult emitResult = compilation.Emit(assemblyPath);
        Assert.True(emitResult.Success);

        RemoteExecutor.Invoke(async (assemblyFullPath) => {
            string source1 = """
                using Microsoft.Extensions.Options;

                namespace MyAssembly;

                [OptionsValidator]
                public partial class MyOptionsValidator : IValidateOptions<MyOptions>
                {
                }

                public class MyOptions
                {
                    [ValidateObjectMembers]
                    public AnotherAssembly.ClassInAnotherAssembly? TransitiveProperty { get; set; }
                }
                """;

            Assembly assembly = Assembly.LoadFrom(assemblyFullPath);

            var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source1, assembly);
            _ = Assert.Single(generatedSources);
            var diag = Assert.Single(diagnostics);
            Assert.Equal(DiagDescriptors.PotentiallyMissingTransitiveValidation.Id, diag.Id);

            // validate the location is inside the MyOptions class and not outside the compilation which is in the referenced assembly
            Assert.StartsWith("src-0.cs: (12,", diag.Location.GetLineSpan().ToString());
        }, assemblyPath, new RemoteInvokeOptions { TimeOut = 300 * 1000}).Dispose();

        File.Delete(assemblyPath); // cleanup
    }

    [ConditionalTheory(nameof(SupportRemoteExecutionAndNotInBrowser))]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public async Task InternalsVisibleToAssembliesTest(LanguageVersion languageVersion)
    {
        string assemblyName = Path.GetRandomFileName();
        string assemblyPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        string source = $$"""
            using Microsoft.Extensions.Options;
            using System.ComponentModel.DataAnnotations;

            // Make this assembly visible to the other assembly
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("{{assemblyName + "0"}}")]

            #nullable enable

            namespace ValidationTest
            {
                public class FirstOptions
                {
                    [Required]
                    public string? Prop { get; set; }
                }

                [OptionsValidator]
                internal sealed partial class FirstOptionsValidator : IValidateOptions<FirstOptions>
                {
                }
            }
        """;

        var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source, null, languageVersion);
        Assert.Empty(diagnostics);
        _ = Assert.Single(generatedSources);

        CSharpCompilation compilation = CreateCompilationForOptionsSource(assemblyName, source + Environment.NewLine + generatedSources[0].SourceText.ToString());
        EmitResult emitResult = compilation.Emit(assemblyPath);
        Assert.True(emitResult.Success);

        RemoteExecutor.Invoke(async (asmName, assemblyFullPath, langVersion) => {

            Assembly assembly = Assembly.LoadFrom(assemblyFullPath);

            string source1 = """
                using Microsoft.Extensions.Options;
                using System.ComponentModel.DataAnnotations;

                #nullable enable

                namespace ValidationTest
                {
                    public class SecondOptions
                    {
                        [Required]
                        public string? Prop { get; set; }
                    }

                    [OptionsValidator]
                    internal sealed partial class SecondOptionsValidator : IValidateOptions<SecondOptions>
                    {
                    }
                }
            """;

            var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source1, null, (LanguageVersion)Enum.Parse(typeof(LanguageVersion), langVersion));
            Assert.Empty(diagnostics);
            _ = Assert.Single(generatedSources);

            CSharpCompilation compilation1 = CreateCompilationForOptionsSource(asmName + "0", source1 + Environment.NewLine + generatedSources[0].SourceText.ToString(), assemblyFullPath);
            MemoryStream ms = new();
            EmitResult emitResult1 = compilation1.Emit(ms);
            Assert.True(emitResult1.Success);
        }, assemblyName, assemblyPath, languageVersion.ToString(), new RemoteInvokeOptions { TimeOut = 300 * 1000}).Dispose();

        File.Delete(assemblyPath); // cleanup
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    [InlineData(LanguageVersion.Preview)]
    [InlineData(LanguageVersion.CSharp11)]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp9)]
    public async Task GenerateSourceUsingVariousLanguageVersions(LanguageVersion languageVersion)
    {
        string source = $$"""
            using Microsoft.Extensions.Options;
            using System.ComponentModel.DataAnnotations;

            #nullable enable

            namespace LanguageVersionTest
            {
                public class MyOptions
                {
                    [Required] public string? Prop { get; set; }
                    [Range(1, 3)] public int Val { get; set; }
                }

                [OptionsValidator]
                internal sealed partial class MyOptionsValidator : IValidateOptions<MyOptions>
                {
                }
            }
        """;

        var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source, null, languageVersion);
        Assert.Empty(diagnostics);
        _ = Assert.Single(generatedSources);

        // Console.WriteLine(generatedSources[0].SourceText.ToString());
        string generatedSource = generatedSources[0].SourceText.ToString();

        if (languageVersion >= LanguageVersion.CSharp11)
        {
            Assert.Contains("file static class __Attributes", generatedSource);
            Assert.Contains("file static class __Validators", generatedSource);
        }
        else
        {
            const string attributesClassDefinition = "internal static class __Attributes_";
            const string validatorsClassDefinition = "internal static class __Validators_";
            int index = generatedSource.IndexOf(attributesClassDefinition, StringComparison.Ordinal);
            Assert.True(index > 0, $"{attributesClassDefinition} not found in the generated source");
            string suffix = generatedSource.Substring(index + attributesClassDefinition.Length, 8);
            index = generatedSource.IndexOf(validatorsClassDefinition, StringComparison.Ordinal);
            Assert.True(index > 0, $"{validatorsClassDefinition} not found in the generated source");
            Assert.True(index + validatorsClassDefinition.Length + 8 <= generatedSource.Length, $"{validatorsClassDefinition} suffix not found in the generated source");
            Assert.Equal(suffix, generatedSource.Substring(index + validatorsClassDefinition.Length, 8));
        }
    }

    [ConditionalFact(nameof(SupportRemoteExecutionAndNotInBrowser))]
    public async Task InaccessibleValidationAttributesTest()
    {
        string source = """
            using System;
            using System.ComponentModel.DataAnnotations;

            #nullable enable

            namespace ValidationTest;

            public class BaseOptions
            {
                [Timeout] // internal attribute not visible outside the assembly
                public int Prop1 { get; set; }

                [Required]
                public string Prop2 { get; set; }
            }

            [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
            internal sealed class TimeoutAttribute : ValidationAttribute
            {
                protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
                {
                    return ValidationResult.Success!;
                }
            }
        """;

        string assemblyName = Path.GetRandomFileName();
        string assemblyPath = Path.Combine(Path.GetTempPath(), assemblyName + ".dll");

        CSharpCompilation compilation = CreateCompilationForOptionsSource(assemblyName, source);
        EmitResult emitResult = compilation.Emit(assemblyPath);
        Assert.True(emitResult.Success);

        RemoteExecutor.Invoke(async (assemblyFullPath) => {
            string source0 = """
                using Microsoft.Extensions.Options;
                """;

            string source1 = """
                using System.ComponentModel.DataAnnotations;

                #nullable enable
                #pragma warning disable CS1591

                namespace ValidationTest
                {
                    public class ExtOptions : BaseOptions
                    {
                        [Range(0, 10)]
                        public int Prop3 { get; set; }
                    }
                }
                """;

            string source2 = """
                namespace ValidationTest
                {
                    [OptionsValidator]
                    internal sealed partial class ExtOptionsValidator : IValidateOptions<ExtOptions>
                    {
                    }
                }
                """;

            Assembly assembly = Assembly.LoadFrom(assemblyFullPath);

            var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source0 + source1 + source2, assembly);
            _ = Assert.Single(generatedSources);
            Assert.Single(diagnostics);
            Assert.Equal(DiagDescriptors.InaccessibleValidationAttribute.Id, diagnostics[0].Id);
            string generatedSource = generatedSources[0].SourceText.ToString();
            Assert.Contains("__OptionValidationGeneratedAttributes.__SourceGen__RangeAttribute", generatedSource);
            Assert.Contains("global::System.ComponentModel.DataAnnotations.RequiredAttribute", generatedSource);
            Assert.DoesNotContain("Timeout", generatedSource);

            CSharpCompilation compilation = CreateCompilationForOptionsSource(Path.GetRandomFileName()+".dll", source1 + Environment.NewLine + generatedSource, assemblyFullPath);
            MemoryStream ms = new();
            EmitResult emitResult = compilation.Emit(ms);
            Assert.True(emitResult.Success);
        }, assemblyPath, new RemoteInvokeOptions { TimeOut = 300 * 1000}).Dispose();

        File.Delete(assemblyPath); // cleanup

        // Test private validation attribute in the same assembly

        string source3 = """
            using System;
            using System.ComponentModel.DataAnnotations;
            using Microsoft.Extensions.Options;

            #nullable enable

            namespace ValidationTest;

            public class MyOptions
            {
                [Timeout] // private attribute
                public int Prop1 { get; set; }

                [Required]
                public string Prop2 { get; set; }

                [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
                private sealed class TimeoutAttribute : ValidationAttribute
                {
                    protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
                    {
                        return ValidationResult.Success!;
                    }
                }
            }

            [OptionsValidator]
            public sealed partial class MyOptionsValidator : IValidateOptions<MyOptions>
            {
            }
            """;

        var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source3);
        _ = Assert.Single(generatedSources);
        Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.InaccessibleValidationAttribute.Id, diagnostics[0].Id);
        string generatedSource = generatedSources[0].SourceText.ToString();
        Assert.DoesNotContain("Timeout", generatedSource);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CantValidateOpenGenericMembersInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateEnumeratedItems]
                public T[]? P1 { get; set; }

                [ValidateEnumeratedItems]
                [Required]
                public T[]? P2 { get; set; }

                [ValidateEnumeratedItems]
                [Required]
                public System.Collections.Generic.IList<T> P3 { get; set; } = null!;
            }

            [OptionsValidator]
            public partial class FirstValidator<T> : IValidateOptions<FirstModel<T>>
            {
            }
        ");

        Assert.Equal(3, diagnostics.Count);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[1].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, diagnostics[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ClosedGenericsInEnumeration()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [ValidateEnumeratedItems]
                [Required]
                public T[]? P1 { get; set; }

                [ValidateEnumeratedItems]
                [Required]
                public int[]? P2 { get; set; }

                [ValidateEnumeratedItems]
                [Required]
                public System.Collections.Generic.IList<T>? P3 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel<string>>
            {
            }
        ");

        Assert.Equal(3, diagnostics.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[1].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, diagnostics[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotEnumerable()
    {
        var (diagnostics, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateEnumeratedItems]
                public int P1 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Equal(1, diagnostics.Count);
        Assert.Equal(DiagDescriptors.NotEnumerableType.Id, diagnostics[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task LanguageVersionTest()
    {
        string source = """
            using System;
            using System.ComponentModel.DataAnnotations;
            using Microsoft.Extensions.Options;

            public class FirstModel
            {
                [Required]
                public string? P1 { get; set; }
            }

            [OptionsValidator]
            public partial class FirstModelValidator : IValidateOptions<FirstModel>
            {
            }
        """;

        Assembly [] refAssemblies = new []
        {
            Assembly.GetAssembly(typeof(RequiredAttribute)),
            Assembly.GetAssembly(typeof(OptionsValidatorAttribute)),
            Assembly.GetAssembly(typeof(IValidateOptions<object>)),
        };

        // Run the generator with C# 7.0 and verify that it fails.
        var (diagnostics, generatedSources) = await RoslynTestUtils.RunGenerator(
                new OptionsValidatorGenerator(), refAssemblies.ToArray(), new[] { source }, includeBaseReferences: true, LanguageVersion.CSharp7).ConfigureAwait(false);

        Assert.NotEmpty(diagnostics);
        Assert.Equal("SYSLIB1216", diagnostics[0].Id);
        Assert.Empty(generatedSources);

        // Run the generator with C# 8.0 and verify that it succeeds.
        (diagnostics, generatedSources) = await RoslynTestUtils.RunGenerator(
            new OptionsValidatorGenerator(), refAssemblies.ToArray(), new[] { source }, includeBaseReferences: true, LanguageVersion.CSharp8).ConfigureAwait(false);

        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

        // Compile the generated code with C# 7.0 and verify that it fails.
        CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp7);
        SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedSources[0].SourceText.ToString(), parseOptions);
        var diags = syntaxTree.GetDiagnostics().ToArray();
        Assert.Equal(1, diags.Length);
        // error CS8107: Feature 'nullable reference types' is not available in C# 7.0. Please use language version 8.0 or greater.
        Assert.Equal("CS8107", diags[0].Id);

        // Compile the generated code with C# 8.0 and verify that it succeeds.
        parseOptions = new CSharpParseOptions(LanguageVersion.CSharp8);
        syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedSources[0].SourceText.ToString(), parseOptions);
        diags = syntaxTree.GetDiagnostics().ToArray();
        Assert.Equal(0, diags.Length);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser), nameof(PlatformDetection.IsNetCore))]
    public async Task DataAnnotationAttributesWithParams()
    {
        var (diagnostics, generatedSources) = await RunGenerator(@"""
            public class MyOptions
            {
                [Required]
                public string P1 { get; set; }

                [Length(10, 20)]
                public string P2 { get; set; }

                [AllowedValues(10, 20, 30)]
                public int P3 { get; set; }

                [DeniedValues(""One"", ""Ten"", ""Hundred"")]
                public string P4 { get; set; }
            }

            [OptionsValidator]
            public partial class MyOptionsValidator : IValidateOptions<MyOptions>
            {
            }
        """);

        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

        string generatedSource = File.ReadAllText(@"Baselines/DataAnnotationAttributesWithParams.g.cs");
        Assert.Equal(generatedSource.Replace("\r\n", "\n"), generatedSources[0].SourceText.ToString().Replace("\r\n", "\n"));
    }

    private static CSharpCompilation CreateCompilationForOptionsSource(string assemblyName, string source, string? refAssemblyPath = null, LanguageVersion languageVersion = LanguageVersion.Default)
    {
        // Ensure the generated source compiles
        var compilation = CSharpCompilation
                .Create($"{assemblyName}.dll", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "System.Runtime").Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(RequiredAttribute).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(OptionsValidatorAttribute).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(IValidateOptions<object>).Assembly.Location))
                .AddReferences(MetadataReference.CreateFromFile(typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).Assembly.Location))
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion)));

        if (refAssemblyPath is not null)
        {
            compilation = compilation.AddReferences(MetadataReference.CreateFromFile(refAssemblyPath));
        }

        return compilation;
    }

    private static async Task<(IReadOnlyList<Diagnostic>, ImmutableArray<GeneratedSourceResult>)> RunGeneratorOnOptionsSource(
                                                                                                    string source,
                                                                                                    Assembly? refAssembly = null,
                                                                                                    LanguageVersion languageVersion = LanguageVersion.Preview)
    {
        List<Assembly> refAssemblies = new()
        {
            Assembly.GetAssembly(typeof(RequiredAttribute)),
            Assembly.GetAssembly(typeof(OptionsValidatorAttribute)),
            Assembly.GetAssembly(typeof(IValidateOptions<object>)),
        };

        if (refAssembly is not null)
        {
            refAssemblies.Add(refAssembly);
        }

        return await RoslynTestUtils.RunGenerator(new OptionsValidatorGenerator(), refAssemblies.ToArray(), new List<string> { source }, includeBaseReferences: true, languageVersion).ConfigureAwait(false);
    }

    private static async Task<(IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources)> RunGenerator(
        string code,
        bool wrap = true,
        bool inNamespace = true,
        bool includeOptionValidatorReferences = true,
        bool includeSystemReferences = true,
        bool includeOptionsReferences = true,
        bool includeTransitiveReferences = true)
    {
        var text = code;
        if (wrap)
        {
            var nspaceStart = "namespace Test {";
            var nspaceEnd = "}";
            if (!inNamespace)
            {
                nspaceStart = "";
                nspaceEnd = "";
            }

            text = $@"
                    {nspaceStart}
                    using System.ComponentModel.DataAnnotations;
                    using Microsoft.Extensions.Options.Validation;
                    using Microsoft.Shared.Data.Validation;
                    using Microsoft.Extensions.Options;
                    using Microsoft.CodeAnalysis.CSharp.Syntax;
                    {code}
                    {nspaceEnd}
                ";
        }

        var assemblies = new List<Assembly> { Assembly.GetAssembly(typeof(MemberDeclarationSyntax))! };

        if (includeOptionValidatorReferences)
        {
            assemblies.Add(Assembly.GetAssembly(typeof(OptionsValidatorAttribute))!);
        }

        if (includeSystemReferences)
        {
            assemblies.Add(Assembly.GetAssembly(typeof(RequiredAttribute))!);
        }

        if (includeOptionsReferences)
        {
            assemblies.Add(Assembly.GetAssembly(typeof(IValidateOptions<object>))!);
        }

        if (includeTransitiveReferences)
        {
            assemblies.Add(Assembly.GetAssembly(typeof(Microsoft.Extensions.Options.ValidateObjectMembersAttribute))!);
        }

        var result = await RoslynTestUtils.RunGenerator(new OptionsValidatorGenerator(), assemblies.ToArray(), new[] { text })
            .ConfigureAwait(false);

        return result;
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    public async Task GeneratedAttributesTest(LanguageVersion languageVersion)
    {

#if NETCOREAPP
        string lengthAttribute = $$"""
                    [LengthAttribute(1, 3)]
                    public string? P0 { get; set; }

                    [LengthAttribute(1, 3)]
                    public FakeCount? P1 { get; set; }

                    [LengthAttribute(1, 3)]
                    public FakeCountChild? P2 { get; set; }
        """;
#else
string lengthAttribute = "";
#endif //NETCOREAPP

        string source = $$"""
            using System;
            using System.Collections.Generic;
            using Microsoft.Extensions.Options;
            using System.ComponentModel.DataAnnotations;

            #nullable enable

            namespace ValidationTest
            {
                public class FakeCount
                {
                    public FakeCount(int count) { Count = count; }
                    public int Count { get; }
                }
                public class FakeCountChild : FakeCount
                {
                    public FakeCountChild(int count) : base(count) { }
                }

                public class OptionsUsingGeneratedAttributes
                {
                    {{lengthAttribute}}

                    [RangeAttribute(1, 3)]
                    public int P3 { get; set; }

                    [MinLengthAttribute(5)]
                    public string? P4 { get; set; }

                    [MaxLengthAttribute(5)]
                    public string? P5 { get; set; }

                    [CompareAttribute("P5")]
                    public string? P6 { get; set; }

                    [MinLengthAttribute(5)]
                    public FakeCount? P7 { get; set; }

                    [MinLengthAttribute(5)]
                    public FakeCountChild? P8 { get; set; }

                    [MaxLengthAttribute(5)]
                    public FakeCount? P9 { get; set; }

                    [MaxLengthAttribute(5)]
                    public FakeCountChild? P10 { get; set; }

                    [MinLengthAttribute(5)]
                    public List<string>? P11 { get; set; }

                    [MaxLengthAttribute(5)]
                    public List<string>? P12 { get; set; }

                    [RangeAttribute(typeof(TimeSpan), "00:00:00", "23:59:59")]
                    public string? P13 { get; set; }

                    [RangeAttribute(typeof(TimeSpan), "01:00:00", "23:59:59")]
                    public TimeSpan P14 { get; set; }
                }

                [OptionsValidator]
                public sealed partial class OptionsUsingGeneratedAttributesValidator : IValidateOptions<OptionsUsingGeneratedAttributes>
                {
                }
            }
        """;

        var (diagnostics, generatedSources) = await RunGeneratorOnOptionsSource(source, null, languageVersion);
        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

        string emittedSource = generatedSources[0].SourceText.ToString();
        SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(emittedSource, new CSharpParseOptions(languageVersion));
        var diags = syntaxTree.GetDiagnostics().ToArray();
        Assert.Empty(diags);

#if NETCOREAPP
        string generatedSource = File.ReadAllText(languageVersion == LanguageVersion.CSharp10  ? @"Baselines/GeneratedAttributesTest.netcore.lang10.g.cs" : @"Baselines/GeneratedAttributesTest.netcore.lang11.g.cs");
#else
        string generatedSource = File.ReadAllText(languageVersion == LanguageVersion.CSharp10  ? @"Baselines/GeneratedAttributesTest.netfx.lang10.g.cs" : @"Baselines/GeneratedAttributesTest.netfx.lang11.g.cs");
#endif // NET8_0_OR_GREATER
        Assert.Equal(generatedSource.Replace("\r\n", "\n"), emittedSource.Replace("\r\n", "\n"));

        CSharpCompilation compilation = CreateCompilationForOptionsSource(Path.GetRandomFileName(), source + emittedSource, refAssemblyPath: null, languageVersion);
        var emitResult = compilation.Emit(new MemoryStream());

        Assert.True(emitResult.Success);
        // Console.WriteLine(emittedSource);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task UsingInterfaceAsPropertyTypeForLengthAttributesTests()
    {
        var (diagnostics, generatedSources) = await RunGenerator(@"""
            using System.Collections.Generic;

            public class MyOptions
            {
                [Length(10, 20)]
                public IList<string> P1 { get; set; }

                [MinLength(4)]
                public IList<string> P2 { get; set; }

                [MaxLength(5)]
                public IList<string> P3 { get; set; }

                [Length(10, 20)]
                public ICollection<string> P4 { get; set; }

                [MinLength(4)]
                public ICollection<string> P5 { get; set; }

                [MaxLength(5)]
                public ICollection<string> P6 { get; set; }
            }

            [OptionsValidator]
            public partial class MyOptionsValidator : IValidateOptions<MyOptions>
            {
            }
        """);

        Assert.Empty(diagnostics);
        Assert.Single(generatedSources);

#if NETCOREAPP
        string generatedSource = File.ReadAllText(@"Baselines/UsingInterfaceAsPropertyTypeForLengthAttributesTests.netcore.g.cs");
#else
        string generatedSource = File.ReadAllText(@"Baselines/UsingInterfaceAsPropertyTypeForLengthAttributesTests.netfx.g.cs");
#endif // NETCOREAPP
        Assert.Equal(generatedSource.Replace("\r\n", "\n"), generatedSources[0].SourceText.ToString().Replace("\r\n", "\n"));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task OptionsExtendingSystemClassTest()
    {
        string source = """
            using System;
            using System.ComponentModel.DataAnnotations;
            using Microsoft.Extensions.Options;
            using System.Collections.Generic;

            #nullable enable

            [OptionsValidator]
            public sealed class RedisNamedClientOptions : Dictionary<string, RedisClientOptions>, IValidateOptions<RedisNamedClientOptions>
            {
                public const string Section = "RedisClients";

                [Required]
                [ValidateEnumeratedItems]
                public IEnumerable<RedisClientOptions> RedisClientOptionsList => this.Values;
            }

            public sealed class RedisClientOptions
            {
                [Required]
                [ValidateEnumeratedItems]
                public required IList<EndPointsOptions> EndPoints { get; init; }
            }

            public sealed class EndPointsOptions
            {
                [Required]
                public required string Host { get; init; }

                [Required]
                [Range(1_024, 65_535)]
                public required int Port { get; init; }
            }
        """;

        var (diagnostics, src) = await RunGenerator(source);
        Assert.Empty(diagnostics);
        Assert.Single(src);
#if NETCOREAPP
        string generatedSource = File.ReadAllText(@"Baselines/OptionsExtendingSystemClassTest.netcore.g.cs");
#else
        string generatedSource = File.ReadAllText(@"Baselines/OptionsExtendingSystemClassTest.netfx.g.cs");
#endif // NETCOREAPP
        Assert.Equal(generatedSource.Replace("\r\n", "\n"), src[0].SourceText.ToString().Replace("\r\n", "\n"));
    }
}
