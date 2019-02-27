// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public interface IController { }
    public interface IAuthentication { }
    public interface IFormsAuthenticationService { }
    public interface IMembershipService { }

    public class FormsAuthenticationServiceImpl : IFormsAuthenticationService { }
    public class MembershipServiceImpl : IMembershipService { }
    public class SpecificMembershipServiceImpl : IMembershipService { }
    public class HttpDigestAuthentication : IAuthentication { }

    public class AmbiguousConstructors
    {
        public AmbiguousConstructors(string first, int second) { StringArg = first; IntArg = second; }
        public AmbiguousConstructors(int first, string second) { IntArg = first; StringArg = second; }

        public int IntArg { get; set; }
        public string StringArg { get; set; }
    }

    public class AmbiguousConstructorsWithAttribute
    {
        [ImportingConstructorAttribute]
        public AmbiguousConstructorsWithAttribute(string first, int second) { StringArg = first; IntArg = second; }
        public AmbiguousConstructorsWithAttribute(int first, string second) { IntArg = first; StringArg = second; }

        public int IntArg { get; set; }
        public string StringArg { get; set; }
    }

    public class LongestConstructorWithAttribute
    {
        [ImportingConstructorAttribute]
        public LongestConstructorWithAttribute(string first, int second) { StringArg = first; IntArg = second; }
        public LongestConstructorWithAttribute(int first) { IntArg = first; }

        public int IntArg { get; set; }
        public string StringArg { get; set; }
    }

    public class LongestConstructorShortestWithAttribute
    {
        public LongestConstructorShortestWithAttribute(string first, int second) { StringArg = first; IntArg = second; }
        [ImportingConstructorAttribute]
        public LongestConstructorShortestWithAttribute(int first) { IntArg = first; }

        public int IntArg { get; set; }
        public string StringArg { get; set; }
    }


    public class ConstructorArgs
    {
        public ConstructorArgs()
        {
            IntArg = 10;
            StringArg = "Hello, World";
        }

        public int IntArg { get; set; }
        public string StringArg { get; set; }
    }

    public class AccountController : IController
    {
        public IMembershipService MembershipService { get; private set; }

        public AccountController(IMembershipService membershipService)
        {
            MembershipService = membershipService;
        }

        public AccountController(IAuthentication auth)
        {
        }
    }

    public class HttpRequestValidator
    {
        public IAuthentication authenticator;

        public HttpRequestValidator(IAuthentication authenticator) { }
    }

    public class ManyConstructorsController : IController
    {
        public IFormsAuthenticationService FormsService { get; set; }
        public IMembershipService MembershipService { get; set; }
        public HttpRequestValidator Validator { get; set; }

        public ManyConstructorsController() { }
        public ManyConstructorsController(
            IFormsAuthenticationService formsService)
        {
            FormsService = formsService;
        }

        public ManyConstructorsController(
            IFormsAuthenticationService formsService,
            IMembershipService membershipService)
        {
            FormsService = formsService;
            MembershipService = MembershipService;
        }

        public ManyConstructorsController(
            IFormsAuthenticationService formsService,
            IMembershipService membershipService,
            HttpRequestValidator validator)
        {
            FormsService = formsService;
            MembershipService = membershipService;
            Validator = validator;
        }
    }

    public class PartBuilderUnitTests
    {
        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void ManyConstructorsControllerFindLongestConstructor_ShouldSucceed()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<FormsAuthenticationServiceImpl>().Export<IFormsAuthenticationService>();
            ctx.ForType<HttpDigestAuthentication>().Export<IAuthentication>();
            ctx.ForType<MembershipServiceImpl>().Export<IMembershipService>();
            ctx.ForType<HttpRequestValidator>().Export();
            ctx.ForType<ManyConstructorsController>().Export();

            var catalog = new TypeCatalog(new[] {
                typeof(FormsAuthenticationServiceImpl),
                typeof(HttpDigestAuthentication),
                typeof(MembershipServiceImpl),
                typeof(HttpRequestValidator),
                typeof(ManyConstructorsController) }, ctx);

            Assert.True(catalog.Parts.Count() == 5);

            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            ManyConstructorsController item = container.GetExportedValue<ManyConstructorsController>();

            Assert.True(item.Validator != null);
            Assert.True(item.FormsService != null);
            Assert.True(item.MembershipService != null);
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void ManyConstructorsControllerFindLongestConstructorAndImportByName_ShouldSucceed()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<FormsAuthenticationServiceImpl>().Export<IFormsAuthenticationService>();
            ctx.ForType<HttpDigestAuthentication>().Export<IAuthentication>();
            ctx.ForType<MembershipServiceImpl>().Export<IMembershipService>();
            ctx.ForType<SpecificMembershipServiceImpl>().Export<IMembershipService>((c) => c.AsContractName("membershipService"));
            ctx.ForType<HttpRequestValidator>().Export();
            ctx.ForType<ManyConstructorsController>().SelectConstructor(null, (pi, import) =>
           {
               if (typeof(IMembershipService).IsAssignableFrom(pi.ParameterType))
               {
                   import.AsContractName("membershipService");
               }
           }).Export();

            var catalog = new TypeCatalog(new[] {
                typeof(FormsAuthenticationServiceImpl),
                typeof(HttpDigestAuthentication),
                typeof(MembershipServiceImpl),
                typeof(SpecificMembershipServiceImpl),
                typeof(HttpRequestValidator),
                typeof(ManyConstructorsController) }, ctx);

            Assert.True(catalog.Parts.Count() == 6);

            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            ManyConstructorsController item = container.GetExportedValue<ManyConstructorsController>();

            Assert.True(item.Validator != null);
            Assert.True(item.FormsService != null);
            Assert.True(item.MembershipService != null);
            Assert.True(item.MembershipService.GetType() == typeof(SpecificMembershipServiceImpl));
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void LongestConstructorWithAttribute_ShouldSucceed()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<LongestConstructorWithAttribute>().Export();
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "IntArg");
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "StringArg");

            var catalog = new TypeCatalog(new[] {
                typeof(LongestConstructorWithAttribute),
                typeof(ConstructorArgs) }, ctx);
            Assert.Equal(2, catalog.Parts.Count());
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            LongestConstructorWithAttribute item = container.GetExportedValue<LongestConstructorWithAttribute>();
            Assert.Equal(10, item.IntArg);
            Assert.Equal("Hello, World", item.StringArg);
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void LongestConstructorShortestWithAttribute_ShouldSucceed()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<LongestConstructorShortestWithAttribute>().Export();
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "IntArg");
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "StringArg");

            var catalog = new TypeCatalog(new[] {
                typeof(LongestConstructorShortestWithAttribute),
                typeof(ConstructorArgs) }, ctx);
            Assert.Equal(2, catalog.Parts.Count());
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            LongestConstructorShortestWithAttribute item = container.GetExportedValue<LongestConstructorShortestWithAttribute>();
            Assert.Equal(10, item.IntArg);
            Assert.Equal(null, item.StringArg);
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void AmbiguousConstructorWithAttributeAppliedToOne_ShouldSucceed()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<AmbiguousConstructorsWithAttribute>().Export();
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "IntArg");
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "StringArg");

            var catalog = new TypeCatalog(new[] {
                typeof(AmbiguousConstructorsWithAttribute),
                typeof(ConstructorArgs) }, ctx);
            Assert.Equal(2, catalog.Parts.Count());
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            AmbiguousConstructorsWithAttribute item = container.GetExportedValue<AmbiguousConstructorsWithAttribute>();

            Assert.Equal(10, item.IntArg);
            Assert.Equal("Hello, World", item.StringArg);
        }


        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void AmbiguousConstructor_ShouldFail()
        {
            var ctx = new RegistrationBuilder();

            ctx.ForType<AmbiguousConstructors>().Export();
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "IntArg");
            ctx.ForType<ConstructorArgs>().ExportProperties((m) => m.Name == "StringArg");

            var catalog = new TypeCatalog(new[] {
                typeof(AmbiguousConstructors),
                typeof(ConstructorArgs) }, ctx);
            Assert.Equal(catalog.Parts.Count(), 2);
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);
            Assert.Throws<CompositionException>(() => container.GetExportedValue<AmbiguousConstructors>());
        }
    }
}
