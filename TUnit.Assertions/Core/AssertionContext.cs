using System.Text;

namespace TUnit.Assertions.Core;

/// <summary>
/// Contains the shared state for an assertion chain.
/// Combines evaluation context (value, timing, exceptions) with expression building (error messages).
/// All assertions in a chain share the same AssertionContext instance.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public sealed class AssertionContext<TValue> : AssertionContext
{
    /// <summary>
    /// Handles lazy evaluation, caching, and exception capture for the source value.
    /// </summary>
    public EvaluationContext<TValue> Evaluation { get; }

    /// <inheritdoc />
    public override EvaluationContext EvaluationBase => Evaluation;

    /// <summary>
    /// Creates a new assertion context with the given evaluation context and expression builder.
    /// </summary>
    public AssertionContext(EvaluationContext<TValue> evaluation, StringBuilder expressionBuilder)
        : base(expressionBuilder)
    {
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
    }

    /// <summary>
    /// Creates a new assertion context for immediate values (no evaluation needed).
    /// </summary>
    public AssertionContext(TValue? value, StringBuilder expressionBuilder)
        : base(expressionBuilder)
    {
        Evaluation = new EvaluationContext<TValue>(value);
    }

    /// <summary>
    /// Creates a derived context by transforming to a different type with automatic pending link transfer.
    /// This is the standard way to create type-transforming assertions (like WhenParsedInto, Match, IsTypeOf, etc).
    /// Automatically handles:
    /// - Transferring the expression builder
    /// - Consuming pending links from the source context
    /// - Setting up pre-work to execute previous assertions before the transformation
    ///
    /// Uses lazy evaluation to prevent stack overflow in circular reference scenarios.
    /// </summary>
    /// <typeparam name="TNew">The target type after transformation</typeparam>
    /// <param name="evaluationFactory">Factory to create the new evaluation context from the current one</param>
    /// <returns>A new assertion context with pending links properly transferred</returns>
    public AssertionContext<TNew> Map<TNew>(Func<EvaluationContext<TValue>, EvaluationContext<TNew>> evaluationFactory)
    {
        var newEvaluation = evaluationFactory(Evaluation);
        var newContext = new AssertionContext<TNew>(newEvaluation, ExpressionBuilder);

        // Transfer pending links from source context to handle cross-type chaining
        // e.g., Assert.That(str).HasLength(3).And.Match(@"\d+").And.Captured<int>(1)
        var (pendingAssertion, combinerType) = ConsumePendingLink();
        if (pendingAssertion != null)
        {
            // Store the pending assertion execution as pre-work
            // It will be executed before any assertions on the transformed value
            newContext.PendingPreWork = async () => await pendingAssertion.ExecuteCoreAsync();
        }

        return newContext;
    }

    /// <summary>
    /// Convenience overload for simple value-to-value transformations.
    /// Wraps a simple mapper function in an evaluation context transformation.
    /// </summary>
    public AssertionContext<TNew> Map<TNew>(Func<TValue?, TNew?> mapper)
    {
        return Map(evalContext => evalContext.Map(mapper));
    }

    /// <summary>
    /// Convenience overload for async value-to-value transformations.
    /// Wraps an async mapper function in an evaluation context transformation.
    /// The Task is unwrapped, allowing assertions to chain on the result type directly.
    /// </summary>
    public AssertionContext<TNew> Map<TNew>(Func<TValue?, Task<TNew?>> asyncMapper)
    {
        return Map(evalContext => evalContext.Map(asyncMapper));
    }

    public AssertionContext<TException> MapException<TException>() where TException : Exception
    {
        return new AssertionContext<TException>(
            Evaluation.MapException<TException>(),
            ExpressionBuilder
        );
    }

    /// <summary>
    /// Gets the evaluated value and any exception that occurred.
    /// Evaluates once and caches the result for subsequent calls.
    /// </summary>
    public Task<(TValue? Value, Exception? Exception)> GetAsync()
    {
        return Evaluation.GetAsync();
    }

    /// <summary>
    /// Pending assertion to link with when the next assertion is constructed.
    /// Set by AndContinuation/OrContinuation, consumed by Assertion constructor.
    /// </summary>
    internal Assertion<TValue>? PendingLinkPrevious { get; private set; }

    /// <summary>
    /// The type of combiner (And/Or) for the pending link.
    /// </summary>
    internal CombinerType? PendingLinkType { get; private set; }

    /// <summary>
    /// Sets the pending link state for the next assertion to consume.
    /// Called by AndContinuation/OrContinuation constructors.
    /// </summary>
    internal void SetPendingLink(Assertion<TValue> previous, CombinerType type)
    {
        PendingLinkPrevious = previous;
        PendingLinkType = type;
    }

    /// <summary>
    /// Consumes and clears the pending link state.
    /// Called by Assertion constructor to auto-detect chaining.
    /// </summary>
    internal (Assertion<TValue>? previous, CombinerType? type) ConsumePendingLink()
    {
        var result = (PendingLinkPrevious, PendingLinkType);
        PendingLinkPrevious = null;
        PendingLinkType = null;
        return result;
    }

    /// <inheritdoc />
    internal override Func<Task>? ConsumeAndGetPendingWork()
    {
        var (pendingAssertion, _) = ConsumePendingLink();
        if (pendingAssertion != null)
        {
            return async () => await pendingAssertion.ExecuteCoreAsync();
        }

        return null;
    }

    /// <summary>
    /// Converts a non-nullable reference type context to its nullable equivalent.
    /// This is safe because reference types and their nullable counterparts have identical runtime representations.
    /// Used primarily by the assertion source generator to handle non-nullable to nullable conversions.
    /// IMPORTANT: This should only be called when TValue is a reference type (class).
    /// </summary>
    /// <returns>The same context instance viewed as nullable</returns>
    internal AssertionContext<TValue?> AsNullable()
    {
        // This cast is safe because for reference types, TValue and TValue?
        // have the same runtime representation. We are only changing the
        // compiler's static analysis view of the type.
        // The generator ensures this is only called for reference types.
        return (AssertionContext<TValue?>)(object)this;
    }
}
