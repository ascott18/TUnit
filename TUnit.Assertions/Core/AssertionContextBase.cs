using System.Text;

namespace TUnit.Assertions.Core;

/// <summary>
/// Non-generic base class for <see cref="AssertionContext{TValue}"/>.
/// Provides type-erased access to the assertion context's shared state,
/// enabling covariant assertion sources and cross-type transformations.
/// </summary>
public abstract class AssertionContext
{
    /// <summary>
    /// The non-generic evaluation context for type-erased operations.
    /// </summary>
    public abstract EvaluationContext EvaluationBase { get; }

    /// <summary>
    /// Builds the assertion chain expression for error messages.
    /// Mutated as assertions are chained together.
    /// </summary>
    public StringBuilder ExpressionBuilder { get; }

    /// <summary>
    /// Pre-work to execute before evaluating assertions in this context.
    /// Used for cross-type assertion chaining (e.g., string assertions before WhenParsedInto&lt;int&gt;).
    /// </summary>
    internal Func<Task>? PendingPreWork { get; set; }

    /// <summary>
    /// Initializes the non-generic base with the shared expression builder.
    /// </summary>
    protected AssertionContext(StringBuilder expressionBuilder)
    {
        ExpressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
    }

    /// <summary>
    /// Gets the timing information for this evaluation.
    /// Only meaningful after evaluation has occurred.
    /// </summary>
    public (DateTimeOffset Start, DateTimeOffset End) GetTiming()
    {
        return EvaluationBase.GetTiming();
    }

    /// <summary>
    /// Override in the generic subclass to consume typed pending links and return them as pre-work.
    /// Called by <see cref="MapFromObject{TNew}"/> during cross-type transformations.
    /// </summary>
    internal virtual Func<Task>? ConsumeAndGetPendingWork() => null;

    /// <summary>
    /// Creates a derived typed context by mapping the boxed value to a new type.
    /// Used by <see cref="ICovariantAssertionSource{TDerived}.AsType{TNew}"/> for covariant type narrowing.
    /// Automatically transfers pending assertion work to the new context.
    /// </summary>
    public AssertionContext<TNew> MapFromObject<TNew>(Func<object?, TNew?> mapper)
    {
        var newEvaluation = EvaluationBase.MapFromObject(mapper);
        var newContext = new AssertionContext<TNew>(newEvaluation, ExpressionBuilder);

        // Transfer pending links from source context to handle cross-type chaining
        var pendingWork = ConsumeAndGetPendingWork();
        if (pendingWork != null)
        {
            newContext.PendingPreWork = pendingWork;
        }

        return newContext;
    }

    public AssertionContext<TNew> Map<TNew>()
        => this is AssertionContext<TNew> current ? current : MapFromObject(value => (TNew)value!);
}
