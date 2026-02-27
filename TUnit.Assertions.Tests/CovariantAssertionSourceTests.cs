using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Tests that ICovariantAssertionSource enables calling extension methods
/// defined for a base type on assertion sources wrapping derived types.
/// </summary>
public class CovariantAssertionSourceTests
{
    [Test]
    public async Task Extension_ForBaseType_CanBeCalledOnDerivedTypeAssertion()
    {
        var dog = new Dog { Name = "Buddy", Breed = "Labrador" };

        await Assert.That(dog).IsAnimalWithName("Buddy");
    }

    [Test]
    public async Task Extension_ForBaseType_FailsWhenValueDoesNotMatch()
    {
        var dog = new Dog { Name = "Buddy", Breed = "Labrador" };

        await Assert.ThrowsAsync<Exceptions.AssertionException>(async () =>
        {
            await Assert.That(dog).IsAnimalWithName("Rex");
        });
    }

    public class Animal
    {
        public required string Name { get; init; }
    }

    public class Dog : Animal
    {
        public required string Breed { get; init; }
    }
}

/// <summary>
/// Extension methods targeting ICovariantAssertionSource&lt;Animal&gt; directly,
/// demonstrating that they can be called from IAssertionSource&lt;Dog&gt; via covariance.
/// </summary>
public static class CovariantAnimalAssertionExtensions
{
    /// <summary>
    /// Non-generic extension on ICovariantAssertionSource&lt;Animal&gt;.
    /// Callable on Assert.That(dog) where Dog : Animal.
    /// Uses UntypedContext.MapFromObject to get a typed context for the base type.
    /// </summary>
    public static AnimalNameAssertion IsAnimalWithName(
        this ICovariantAssertionSource<CovariantAssertionSourceTests.Animal> source,
        string expectedName)
    {
        var context = source.Context.Map<CovariantAssertionSourceTests.Animal>();
        context.ExpressionBuilder.Append($".IsAnimalWithName(\"{expectedName}\")");
        return new AnimalNameAssertion(context, expectedName);
    }
}

/// <summary>
/// Assertion that checks an Animal's Name property.
/// </summary>
public class AnimalNameAssertion : Assertion<CovariantAssertionSourceTests.Animal>
{
    private readonly string _expectedName;

    public AnimalNameAssertion(AssertionContext<CovariantAssertionSourceTests.Animal> context, string expectedName)
        : base(context)
    {
        _expectedName = expectedName;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<CovariantAssertionSourceTests.Animal> metadata)
    {
        if (metadata.Exception != null)
        {
            return Task.FromResult(AssertionResult.Failed(metadata.Exception.Message));
        }

        var animal = metadata.Value;
        if (animal == null)
        {
            return Task.FromResult(AssertionResult.Failed("value was null"));
        }

        if (animal.Name == _expectedName)
        {
            return Task.FromResult(AssertionResult.Passed);
        }

        return Task.FromResult(AssertionResult.Failed($"Name was \"{animal.Name}\""));
    }

    protected override string GetExpectation() => $"animal with name \"{_expectedName}\"";
}
