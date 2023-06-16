// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Generators;
using SourceGenerators.Tests;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public partial class ParserTests
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task PotentiallyMissingAttributes()
    {
        var (d, _) = await RunGenerator(@"
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
                public string? P3;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        Assert.Equal(2, d.Count);
        Assert.Equal(DiagDescriptors.PotentiallyMissingTransitiveValidation.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.PotentiallyMissingEnumerableValidation.Id, d[1].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CircularTypeReferences()
    {
        var (d, _) = await RunGenerator(@"
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.CircularTypeReferences.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task InvalidValidatorInterface()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1;
            }

            public class SecondModel
            {
                [Required]
                public string? P2;
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NotValidator()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P1;
            }

            public class SecondModel
            {
                [Required]
                public string? P2;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }

            public partial class SecondValidator
            {
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.DoesntImplementIValidateOptions.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ValidatorAlreadyImplementValidateFunction()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1;

                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P2;
            }

            public class SecondModel
            {
                [Required]
                public string? P3;
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.AlreadyImplementsValidateMethod.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NullValidator()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [ValidateObjectMembers(null!)]
                public SecondModel? P1;
            }

            public class SecondModel
            {
                [Required]
                public string? P2;
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.NullValidatorType.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoSimpleValidatorConstructor()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string? P1;

                [ValidateObjectMembers(typeof(SecondValidator)]
                public SecondModel? P2;
            }

            public class SecondModel
            {
                [Required]
                public string? P3;
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ValidatorsNeedSimpleConstructor.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoStaticValidator()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                public string P1;
            }

            [OptionsValidator]
            public static partial class FirstValidator : IValidateOptions<FirstModel>
            {
            }
        ");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.CantBeStaticClass.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task BogusModelType()
    {
        var (d, _) = await RunGenerator(@"
            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<Bogus>
            {
            }
        ");

        // the generator doesn't produce any errors here, since the C# compiler will take care of it
        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task CantValidateOpenGenericMembers()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateObjectMembers]
                public T? P1;

                [ValidateObjectMembers]
                [Required]
                public T[]? P2;

                [ValidateObjectMembers]
                [Required]
                public System.Collections.Generics.IList<T> P3 = null!;
            }

            [OptionsValidator]
            public partial class FirstValidator<T> : IValidateOptions<FirstModel<T>>
            {
            }
        ");

        Assert.Equal(3, d.Count);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[1].Id);
        Assert.Equal(DiagDescriptors.CantUseWithGenericTypes.Id, d[2].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ClosedGenerics()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel<T>
            {
                [Required]
                [ValidateObjectMembers]
                public T? P1;

                [ValidateObjectMembers]
                [Required]
                public T[]? P2;

                [ValidateObjectMembers]
                [Required]
                public int[]? P3;

                [ValidateObjectMembers]
                [Required]
                public System.Collections.Generics.IList<T>? P4;
            }

            [OptionsValidator]
            public partial class FirstValidator : IValidateOptions<FirstModel<string>>
            {
            }
        ");

        Assert.Equal(4, d.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[1].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[2].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[3].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task NoEligibleMembers()
    {
        var (d, _) = await RunGenerator(@"
            public class FirstModel
            {
                [Required]
                [ValidateObjectMembers]
                public SecondModel? P1;
            }

            public class SecondModel
            {
                public string P2;
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

        Assert.Equal(2, d.Count);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[0].Id);
        Assert.Equal(DiagDescriptors.NoEligibleMembersFromValidator.Id, d[1].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task AlreadyImplemented()
    {
        var (d, _) = await RunGenerator(@"
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

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.AlreadyImplementsValidateMethod.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceInfoWhenTheClassHasABaseClass()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceInfoWhenTransitiveClassHasABaseClass()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
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
        var (d, _) = await RunGenerator(@$"
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

        Assert.Single(d);
        Assert.Equal(DiagDescriptors.NoEligibleMember.Id, d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldProduceWarningWhenTheClassHasNoEligibleMembers()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Single(d);
        Assert.Equal(DiagDescriptors.NoEligibleMembersFromValidator.Id, d[0].Id);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    [InlineData("private")]
    [InlineData("protected")]
    public async Task ShouldProduceWarningWhenTheClassMembersAreInaccessible(string accessModifier)
    {
        var (d, _) = await RunGenerator($@"
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

        Assert.Single(d);
        Assert.Equal("SYSLIB1206", d[0].Id);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenMultipleValidationAnnotationsExist()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenDataTypeAttributesAreUsed()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenConstVariableIsUsedAsAttributeArgument()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    // Testing on all existing & eligible annotations extending ValidationAttribute that aren't used above
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceAnyMessagesWhenExistingValidationsArePlaced()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldNotProduceErrorWhenPropertiesAreUsedAsAttributeArgument()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipWhenOptionsValidatorAttributeDoesNotExist()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipAtrributeWhenAttributeSymbolCannotBeFound()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldSkipAtrributeWhenAttributeSymbolIsNotBasedOnValidationAttribute()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldAcceptAtrributeWhenAttributeIsInDifferentNamespace()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public async Task ShouldHandleAtrributePropertiesOtherThanString()
    {
        var (d, _) = await RunGenerator(@"
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

        Assert.Empty(d);
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
        var (d, sources) = await RunGenerator(@"
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

        var result = await RoslynTestUtils.RunGenerator(new Generator(), assemblies.ToArray(), new[] { text })
            .ConfigureAwait(false);

        return result;
    }
}
