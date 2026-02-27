namespace TUnit.Assertions.Core;

/// <summary>
/// Non-generic base class for <see cref="EvaluationContext{TValue}"/>.
/// Provides type-erased access to evaluation results, enabling covariant assertion sources
/// and cross-type transformations without knowing the concrete value type.
/// </summary>
public abstract class EvaluationContext
{
    /// <summary>
    /// Gets the evaluated value (boxed) and any exception that occurred.
    /// Evaluates once and caches the result for subsequent calls.
    /// </summary>
    public abstract Task<(object? Value, Exception? Exception)> GetAsObjectAsync();

    /// <summary>
    /// Re-evaluates the source by bypassing the cache and invoking the evaluator again.
    /// Returns the result as a boxed object.
    /// </summary>
    public abstract Task<(object? Value, Exception? Exception)> ReevaluateAsObjectAsync();

    /// <summary>
    /// Gets the timing information for this evaluation.
    /// Only meaningful after evaluation has occurred.
    /// </summary>
    public abstract (DateTimeOffset Start, DateTimeOffset End) GetTiming();

    /// <summary>
    /// Creates a derived typed context by mapping the boxed value to a new type.
    /// Used by <see cref="AssertionContext.MapFromObject{TNew}"/> for covariant type narrowing.
    /// </summary>
    public EvaluationContext<TNew> MapFromObject<TNew>(Func<object?, TNew?> mapper)
    {
        return new EvaluationContext<TNew>(async () =>
        {
            var (value, exception) = await GetAsObjectAsync();
            if (exception != null)
            {
                return (default(TNew), exception);
            }

            try
            {
                return (mapper(value), null);
            }
            catch (Exception ex)
            {
                return (default(TNew), ex);
            }
        });
    }
}
