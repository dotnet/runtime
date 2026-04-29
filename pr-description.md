## Add native service decoration support to Microsoft.Extensions.DependencyInjection

### Background

Service decoration is one of the most requested features for the built-in DI container. Libraries like [Scrutor](https://github.com/khellang/Scrutor) have filled this gap, but they work by replacing `ServiceDescriptor` entries with factory-based wrappers that resolve the original via keyed services. This approach makes the decoration intent opaque to third-party DI adapters (Autofac, DryIoc, etc.), preventing them from using their native decoration capabilities.

### What this PR does

Introduces first-class decoration as a concept in the DI framework. Decorations are stored separately from service descriptors, and containers can handle them natively or fall back to automatic materialization.

### API

```csharp
// Type-based
services.Decorate<IService, LoggingDecorator>();

// Factory-based
services.Decorate<IService>((inner, sp) => new CachingDecorator(inner));

// Open generics
services.Decorate(typeof(IRepository<>), typeof(CachingRepository<>));

// Keyed services
services.DecorateKeyed<IService, LoggingDecorator>("myKey");
```

### New types

| Type | Package | Description |
|---|---|---|
| `ServiceDecoration` | Abstractions | Describes a decoration (service type, key, decorator type or factory) |
| `IDecorationServiceCollection` | Abstractions | Extends `IServiceCollection` with a `Decorations` property |
| `ISupportServiceDecoration<T>` | Abstractions | Opt-in interface for adapters with native decoration support |
| `DecorationMaterializer` | DI | Converts decorations to standard keyed-services descriptors for non-aware adapters |

### How it works

- **Built-in container**: Decorations are handled natively via a new `DecoratorCallSite`. The decorator inherits the inner service's lifetime and caching semantics. The inner service parameter is identified by convention (any constructor parameter matching the service type).

- **Hosting with third-party adapters**: `ServiceFactoryAdapter` checks if the adapter implements `ISupportServiceDecoration<TContainerBuilder>`. If so, it calls `ApplyDecorations(builder, services)` after `CreateBuilder`. If not, it materializes decorations into standard factory-based descriptors using the keyed-services pattern before calling `CreateBuilder`.

- **Open generics**: Fully supported in the built-in container and aware adapters. Materialization throws for open generics (they require resolution-time type closing).

- **Generic constraints**: Decorators with type constraints are skipped when constraints aren't satisfied, matching existing behavior for open generic service registrations.

### Design decisions

- **No `TryDecorate`** — decorations are applied at resolution time, not registration time. Services can be registered before or after the decoration call. Unmatched decorations are silently ignored.
- **Separate storage** — decorations live in `IDecorationServiceCollection.Decorations`, not in `IList<ServiceDescriptor>`. Unaware adapters never see them.
- **Convention-based inner parameter** — the decorator constructor parameter matching the service type receives the inner instance, same as `ActivatorUtilities`.
- **Build-time validation** — `ValidateOnBuild` catches invalid decorator constructors through the existing `ValidateService` path. Open generic arity is validated eagerly in `Populate()`.

### Adapter integration guide

Third-party DI adapters can opt into native decoration support by implementing `ISupportServiceDecoration<TContainerBuilder>` on their `IServiceProviderFactory<TContainerBuilder>`. Here's what an Autofac adapter would look like:

```csharp
public class AutofacServiceProviderFactory
    : IServiceProviderFactory<ContainerBuilder>,
      ISupportServiceDecoration<ContainerBuilder>
{
    public ContainerBuilder CreateBuilder(IServiceCollection services)
    {
        var builder = new ContainerBuilder();
        builder.Populate(services);
        return builder;
    }

    public IServiceProvider CreateServiceProvider(ContainerBuilder builder)
        => new AutofacServiceProvider(builder.Build());

    public void ApplyDecorations(ContainerBuilder builder, IDecorationServiceCollection services)
    {
        foreach (var decoration in services.Decorations)
        {
            if (decoration.DecoratorType is { IsGenericTypeDefinition: true } openGenericDecorator)
            {
                // Open generic: use Autofac's RegisterGenericDecorator
                builder.RegisterGenericDecorator(openGenericDecorator, decoration.ServiceType);
            }
            else if (decoration.DecoratorType is { } decoratorType)
            {
                // Closed type: use Autofac's native RegisterDecorator
                builder.RegisterDecorator(decoratorType, decoration.ServiceType);
            }
            else if (decoration.DecoratorFactory is { } factory)
            {
                // Factory-based: wrap into Autofac's (context, parameters, inner) lambda
                builder.RegisterDecorator(decoration.ServiceType,
                    (context, parameters, inner) =>
                    {
                        var sp = context.Resolve<IServiceProvider>();
                        return factory(sp, inner);
                    });
            }
        }
    }
}
```

**What adapters need to do:**
1. Implement `ISupportServiceDecoration<TContainerBuilder>` on their factory
2. In `ApplyDecorations`, iterate `services.Decorations` and translate each to native container calls
3. Handle both `DecoratorType` (type-based) and `DecoratorFactory` (factory-based) decorations
4. Handle `ServiceKey` for keyed decoration support (if the container supports it)

**What happens if an adapter does nothing:**
- If materialization is enabled (current approach): closed-type decorations are converted to keyed-services-based factory descriptors automatically. Open generic decorations throw.
- The adapter's `CreateBuilder` sees standard `ServiceDescriptor` entries — no `ServiceDecoration` awareness needed.

### Test coverage

32 tests covering: type-based, factory-based, open generics, keyed services, constrained generics, multiple decorators (FIFO ordering), all lifetimes, `IEnumerable<T>`, instance/factory registrations, disposable decorator+inner, concrete type decoration, late registration, materialization, build-time validation, and all four resolution engines (Runtime, Expressions, ILEmit, Dynamic).

### Open questions

**1. Should we include automatic materialization for non-aware adapters?**

Currently, when a third-party adapter doesn't implement `ISupportServiceDecoration<T>`, the hosting layer automatically materializes decorations into standard keyed-services-based factory descriptors before calling `CreateBuilder`. This makes decorations "just work" for closed types with any adapter, but:

- Adds complexity (`DecorationMaterializer`, `WithServiceKey` descriptor remapping)
- Can't handle open generic decorations (throws)
- Depends on keyed services support in the target container

Alternatives:
1. **Throw** if there are decorations and the adapter doesn't support them — forces adapters to update, keeps the codebase simpler
2. **Silently ignore** — risky, users won't know decorations aren't applied
3. **Keep materialization** (current approach) — pragmatic fallback for existing adapters

**2. Should `ServiceDecoration` support an optional `Lifetime`?**

Currently the decorator always inherits the inner service's lifetime. This aligns with the consensus from @davidfowl and @seesharper ([LightInject](https://github.com/seesharper/LightInject)):

> *"I think people are wrong there, the most correct thing is to preserve the lifetime."* — @davidfowl

> *"Decorators ALWAYS follow the same lifetime/lifecycle as the decorated service. [...] If in any case we should require different lifetimes, and mind that these cases should be extremely rare, we can always do 'manual' decoration in the factory delegate."* — @seesharper

However, @julealgon [argues](https://github.com/dotnet/runtime/issues/36021#issuecomment-2229416498) that shorter-lived decorators on longer-lived services are legitimate (e.g., a scoped decorator on a singleton that depends on `IOptionsSnapshot<T>`). An optional `ServiceLifetime?` could support this while defaulting to inherit.

**3. Should `Decorate` throw if no matching service exists?**

Currently `Decorate` always succeeds — decorations are applied at resolution time, allowing services to be registered after the decoration call. This matches @seesharper's explicit recommendation that service registration should be decoupled from decoration order. An alternative is to validate at build time (`ValidateOnBuild`) that each decoration matches at least one descriptor, turning unmatched decorations into build errors rather than silent no-ops.

**4. Should we support conditional/predicate-based decoration?**

@julealgon [raised the scenario](https://github.com/dotnet/runtime/issues/36021#issuecomment-2249437397) of toggling decorators based on configuration, feature flags, or environment — e.g., enabling a caching decorator only when `config.EnableCaching` is true. This could be supported via a predicate on `ServiceDecoration` or on the `Decorate` call:

```csharp
services.Decorate<IService, CachingDecorator>(when: sp => config.EnableCaching);
```

This is not in scope for v1 but is worth considering for a follow-up.

### Community context

This PR addresses #36021, one of the longest-standing feature requests for the DI container. Key points from the discussion that informed the design:

- **Open generic support is the #1 gap** — multiple commenters cite it as the reason they can't use existing libraries like Scrutor. This implementation supports open generics natively in the built-in container.
- **Registration order independence** — @seesharper explicitly recommended that `Decorate` should work regardless of whether services are registered before or after the decoration call. Our approach (resolution-time application, no registration-time validation) supports this.
- **Adapter awareness** — @dadhi and @ENikS raised that adapters need to distinguish decorations from regular services. Our `IDecorationServiceCollection` + `ISupportServiceDecoration<T>` design addresses this directly.
- **Triage feedback** — @rosebyte: *"the idea feels reasonable, we consider implementing it in future."*
