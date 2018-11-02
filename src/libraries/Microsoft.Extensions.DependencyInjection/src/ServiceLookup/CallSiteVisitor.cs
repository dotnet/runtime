using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal abstract class CallSiteVisitor<TArgument, TResult>
    {
        protected virtual TResult VisitCallSite(IServiceCallSite callSite, TArgument argument)
        {
            switch (callSite.Kind)
            {
                case CallSiteKind.Factory:
                    return VisitFactory((FactoryCallSite)callSite, argument);
                case  CallSiteKind.IEnumerable:
                    return VisitIEnumerable((IEnumerableCallSite)callSite, argument);
                case CallSiteKind.Constructor:
                    return VisitConstructor((ConstructorCallSite)callSite, argument);
                case CallSiteKind.Transient:
                    return VisitTransient((TransientCallSite)callSite, argument);
                case CallSiteKind.Singleton:
                    return VisitSingleton((SingletonCallSite)callSite, argument);
                case CallSiteKind.Scope:
                    return VisitScoped((ScopedCallSite)callSite, argument);
                case CallSiteKind.Constant:
                    return VisitConstant((ConstantCallSite)callSite, argument);
                case CallSiteKind.CreateInstance:
                    return VisitCreateInstance((CreateInstanceCallSite)callSite, argument);
                case CallSiteKind.ServiceProvider:
                    return VisitServiceProvider((ServiceProviderCallSite)callSite, argument);
                case CallSiteKind.ServiceScopeFactory:
                    return VisitServiceScopeFactory((ServiceScopeFactoryCallSite)callSite, argument);
                default:
                    throw new NotSupportedException($"Call site type {callSite.GetType()} is not supported");
            }
        }

        protected abstract TResult VisitTransient(TransientCallSite transientCallSite, TArgument argument);

        protected abstract TResult VisitConstructor(ConstructorCallSite constructorCallSite, TArgument argument);

        protected abstract TResult VisitSingleton(SingletonCallSite singletonCallSite, TArgument argument);

        protected abstract TResult VisitScoped(ScopedCallSite scopedCallSite, TArgument argument);

        protected abstract TResult VisitConstant(ConstantCallSite constantCallSite, TArgument argument);

        protected abstract TResult VisitCreateInstance(CreateInstanceCallSite createInstanceCallSite, TArgument argument);

        protected abstract TResult VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, TArgument argument);

        protected abstract TResult VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, TArgument argument);

        protected abstract TResult VisitIEnumerable(IEnumerableCallSite enumerableCallSite, TArgument argument);

        protected abstract TResult VisitFactory(FactoryCallSite factoryCallSite, TArgument argument);
    }
}