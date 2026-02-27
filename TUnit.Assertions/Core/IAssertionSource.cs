using TUnit.Assertions.Conditions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Non-generic base interface for all assertion sources.
/// </summary>
public interface IAssertionSource
{
}

/// <summary>
/// Covariant interface for assertion sources, enabling variance-safe usage.
/// Because <typeparamref name="TDerived"/> is covariant (<c>out</c>), an
/// <c>ICovariantAssertionSource&lt;Dog&gt;</c> can be used wherever
/// <c>ICovariantAssertionSource&lt;Animal&gt;</c> is expected.
/// </summary>
/// <typeparam name="TDerived">The covariant type of value being asserted</typeparam>
public interface ICovariantAssertionSource<out TDerived> : IAssertionSource
{
    /// <summary>
    /// The non-generic assertion context, providing access to the expression builder,
    /// evaluation timing, and type-erased value access via <see cref="AssertionContext.MapFromObject{TNew}"/>.
    /// </summary>
    AssertionContext UntypedContext { get; }
}

/// <summary>
/// Common interface for all assertion sources (assertions and continuations).
/// Extension methods target this interface, eliminating duplication.
/// Extends <see cref="ICovariantAssertionSource{TDerived}"/> to enable covariant type narrowing.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public interface IAssertionSource<TValue> : ICovariantAssertionSource<TValue>
{
    /// <summary>
    /// The assertion context shared by all assertions in this chain.
    /// Contains the evaluation context (value, timing, exceptions) and expression builder (error messages).
    /// </summary>
    AssertionContext<TValue> Context { get; }

#if NET8_0_OR_GREATER
    AssertionContext ICovariantAssertionSource<TValue>.UntypedContext => Context;
#endif

    /// <summary>
    /// Asserts that the value is assignment-compatible with the specified type.
    /// </summary>
    TypeOfAssertion<TValue, TExpected> IsTypeOf<TExpected>();

    /// <summary>
    /// Asserts that the value is NOT exactly of the specified type.
    /// </summary>
    IsNotTypeOfAssertion<TValue, TExpected> IsNotTypeOf<TExpected>();

    /// <summary>
    /// Asserts that the value's type is assignable to the specified type.
    /// </summary>
    IsAssignableToAssertion<TExpected, TValue> IsAssignableTo<TExpected>();

    /// <summary>
    /// Asserts that the value's type is NOT assignable to the specified type.
    /// </summary>
    IsNotAssignableToAssertion<TExpected, TValue> IsNotAssignableTo<TExpected>();
}
