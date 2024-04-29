using System.Collections.Immutable;
using System.Diagnostics;

namespace FunctionalCsharp;

/// <summary>
/// Type that can either hold a OK value or an array of Error. Usually used
/// in context of validation where you want to collect all errors instead of stopping at the first error.
/// This type is very similar to <see cref="Result{TOk,TError}"/>.
/// </summary>
/// <typeparam name="TOk">Type of the OK case</typeparam>
/// <typeparam name="TError">Type of the Error case</typeparam>
[DebuggerStepThrough]
public abstract class Validation<TOk, TError>
{
    /// <summary>
    /// Construct new Validation in OK state.
    /// </summary>
    ///
    public static Validation<TOk, TError> Ok(TOk ok) => new OkValidation<TOk, TError>(ok);

    /// <summary>
    /// Construct new Validation with a single error.
    /// </summary>
    public static Validation<TOk, TError> Error(TError error) => new ErrorValidation<TOk, TError>([error]);

    /// <summary>
    /// Construct new Validation with multiple errors.
    /// </summary>
    public static Validation<TOk, TError> Errors(TError[] errors) => new ErrorValidation<TOk, TError>(errors.ToImmutableArray());

    /// <summary>
    /// Construct new Validation with multiple errors.
    /// </summary>
    public static Validation<TOk, TError> Errors(List<TError> errors) => new ErrorValidation<TOk, TError>(errors.ToImmutableArray());

    /// <summary>
    /// Construct new Validation with multiple errors.
    /// </summary>
    public static Validation<TOk, TError> Errors(ImmutableList<TError> errors) => new ErrorValidation<TOk, TError>(errors.ToImmutableArray());

    /// <summary>
    /// Construct new Validation with multiple errors.
    /// </summary>
    public static Validation<TOk, TError> Errors(ImmutableArray<TError> errors) => new ErrorValidation<TOk, TError>(errors);

    public static implicit operator Validation<TOk, TError>(TOk ok) => Ok(ok);
    public static implicit operator Validation<TOk, TError>(ImmutableArray<TError> errors) => Errors(errors);
    public static implicit operator Validation<TOk, TError>(List<TError> errors) => Errors(errors.ToArray());
    public static implicit operator Validation<TOk, TError>(TError error) => Errors((ImmutableArray<TError>) [error]);

    /// <summary>
    /// Transforms an Validation into a different type. You must provide a function for Ok and Error state. Calls
    /// <param name="ok"/> when Validation is in Ok state or <param name="error"/> when Validation is in Error state.
    /// Returns the result of the called function.
    /// </summary>
    /// <param name="ok">Is called when Validation is in Ok state and returns the value</param>
    /// <param name="error">Is called when Validation is in Error state and returns the value</param>
    /// <typeparam name="T">type after the transformation</typeparam>
    /// <returns>Returns either the result of <param name="ok"/> or <param name="error"/> call, depending
    /// on Validation state</returns>
    public abstract T Match<T>(Func<TOk, T> ok, Func<ImmutableArray<TError>, T> error);

    /// <summary>
    /// Transforms the OK content of <see cref="Validation{TOk,TError}"/>. Unwraps, calls <param name="map"/>
    /// and wraps up the value. Does nothing in Error state.
    /// </summary>
    /// <param name="map">function to be called with Ok content</param>
    /// <typeparam name="TOkOut">resulting Ok type. Error type stays the same.</typeparam>
    public Validation<TOkOut, TError> Map<TOkOut>(Func<TOk, TOkOut> map)
    {
        return Match(
            ok: OnOk,
            error: OnError);

        [DebuggerStepThrough]
        Validation<TOkOut, TError> OnOk(TOk x) => new OkValidation<TOkOut, TError>(map(x));

        [DebuggerStepThrough]
        Validation<TOkOut, TError> OnError(ImmutableArray<TError> x) => new ErrorValidation<TOkOut, TError>(x);
    }

    /// <summary>
    /// Similar to <see cref="Map{TOkOut}"/>. If Validation is in Ok state, calls <param name="bind"/> and directly
    /// returns its value (which is itself a Validation). Does nothing otherwise.
    /// </summary>
    /// <param name="bind">function to be called with Ok content</param>
    /// <typeparam name="TOkOut">resulting OK type. Error type stays the same.</typeparam>
    public Validation<TOkOut, TError> Bind<TOkOut>(Func<TOk, Validation<TOkOut, TError>> bind)
    {
        return Match(
            ok: bind,
            error: OnError);

        [DebuggerStepThrough]
        Validation<TOkOut, TError> OnError(ImmutableArray<TError> x) => new ErrorValidation<TOkOut, TError>(x);
    }

    /// <summary>
    /// Similar to <see cref="Map{TOkOut}"/> but works on the Error content instead of Ok content. Transforms
    /// the Error content or does nothing if in Ok state.
    /// </summary>
    /// <param name="map">function to be called with Error content</param>
    /// <typeparam name="TErrorOut">resulting Error type. OK type stays the same.</typeparam>
    public Validation<TOk, TErrorOut> MapError<TErrorOut>(Func<TError, TErrorOut> map)
    {
        return Match(
            ok: OnOk,
            error: OnError);

        [DebuggerStepThrough]
        Validation<TOk, TErrorOut> OnOk(TOk x) => new OkValidation<TOk, TErrorOut>(x);

        [DebuggerStepThrough]
        Validation<TOk, TErrorOut> OnError(ImmutableArray<TError> x) => new ErrorValidation<TOk, TErrorOut>(x.Select(map).ToImmutableArray());
    }

    /// <summary>
    /// Similar to <see cref="Bind{TOkOut}"/> but works on Error state. Calls <param name="bind"/> if in
    /// Error state and returns its value directly. Does nothing if in OK state.
    /// </summary>
    /// <param name="bind">function to be called with Error content</param>
    /// <typeparam name="TErrorOut">resulting Error type. OK type stays the same.</typeparam>
    public Validation<TOk, TErrorOut> BindError<TErrorOut>(Func<ImmutableArray<TError>, Validation<TOk, TErrorOut>> bind)
    {
        return Match(
            ok: OnOk,
            error: bind);

        [DebuggerStepThrough]
        Validation<TOk, TErrorOut> OnOk(TOk x) => new OkValidation<TOk, TErrorOut>(x);
    }
}

[DebuggerStepThrough]
internal sealed class OkValidation<TOk, TError>(TOk value) : Validation<TOk, TError>
{
    public override T Match<T>(Func<TOk, T> ok, Func<ImmutableArray<TError>, T> error) => ok(value);
    public override string ToString() => $"Ok {value}";
}

[DebuggerStepThrough]
internal sealed class ErrorValidation<TOk, TError>(ImmutableArray<TError> errors) : Validation<TOk, TError>
{
    public override T Match<T>(Func<TOk, T> ok, Func<ImmutableArray<TError>, T> error) => error(errors);
    public override string ToString() => $"Error {string.Join(Environment.NewLine, errors)}";
}

/// <summary>
/// Functions working with <see cref="Validation{TOk,TError}"/>
/// </summary>
[DebuggerStepThrough]
public static class ValidationExtensions
{
    /// <summary>
    /// Similar to <see cref="Validation{TOk,TError}.Match{T}"/> but does not return a value.
    /// </summary>
    public static void MatchVoid<TOk, TError>(this Validation<TOk, TError> result, Action<TOk> okAction, Action<ImmutableArray<TError>> errorAction)
    {
        result.Match(
            OnOk,
            OnError);
        return;

        [DebuggerStepThrough]
        Unit OnOk(TOk ok)
        {
            okAction(ok);
            return new Unit();
        }

        [DebuggerStepThrough]
        Unit OnError(ImmutableArray<TError> error)
        {
            errorAction(error);
            return new Unit();
        }
    }

    /// <summary>
    /// Transforms a <see cref="Option{T}"/> into a <see cref="Validation{TOk,TError}"/>. If optional is in Some state,
    /// unpack value and wrap into a Ok Validation. If optional is in None state, use error and wrap into Validation.
    /// </summary>
    /// <param name="error">Value used if option is in None state</param>
    public static Validation<TOk, TError> HasValue<TOk, TError>(this Option<TOk> optional, TError error)
    {
        return optional.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        Validation<TOk, TError> OnSome(TOk value) => new OkValidation<TOk, TError>(value);

        [DebuggerStepThrough]
        Validation<TOk, TError> OnNone() => new ErrorValidation<TOk, TError>([error]);
    }

    /// <summary>
    /// Advanced but useful function. <see cref="Traverse{A,TError}"/> swaps the order of generic types. It transforms a IEnumerable of
    /// Validation into a Validation of IEnumerable. It collects all OK and Error values of the IEnumerable. If only Ok values
    /// exists, then it returns a Validation in OK state with a IEnumerable of all the OK values. Otherwise it aggregates all
    /// errors und returns them as Validation in Error state.
    /// </summary>
    public static Validation<IEnumerable<A>, TError> Traverse<A, TError>(this IEnumerable<Validation<A, TError>> source)
    {
        var success = new List<A>();
        var failList = new List<TError>();
        foreach (var validation in source)
        {
            validation.MatchVoid(
                OnOk,
                OnError);
            continue;

            [DebuggerStepThrough]
            void OnOk(A a) => success.Add(a);

            [DebuggerStepThrough]
            void OnError(ImmutableArray<TError> error) => failList.AddRange(error);
        }

        return failList.Count == 0
            ? success
            : failList.ToImmutableArray();
    }

    /// <summary>
    /// Transforms a <see cref="Validation{TOk,TError}"/> into a <see cref="Option{T}"/> by using the OK state
    /// and dropping the error case.
    /// </summary>
    public static Option<TOk> OkToOption<TOk, TError>(this Validation<TOk, TError> validation)
    {
        return validation.Match(OnOk, OnError);

        [DebuggerStepThrough]
        Option<TOk> OnOk(TOk x) => new Some<TOk>(x);

        [DebuggerStepThrough]
        Option<TOk> OnError(ImmutableArray<TError> _) => new None<TOk>();
    }

    /// <summary>
    /// Transforms a <see cref="Validation{TOk,TError}"/> into a <see cref="Option{T}"/> by using the Error state
    /// and dropping the Ok state.
    /// </summary>
    public static Option<ImmutableArray<TError>> ErrorToOption<TOk, TError>(this Validation<TOk, TError> validation)
    {
        return validation.Match(OnOk, OnError);

        [DebuggerStepThrough]
        Option<ImmutableArray<TError>> OnOk(TOk _) => new None<ImmutableArray<TError>>();

        [DebuggerStepThrough]
        Option<ImmutableArray<TError>> OnError(ImmutableArray<TError> x) => new Some<ImmutableArray<TError>>(x);
    }

    /// <summary>
    /// Returns <see langword="true"/> if in Ok state. Returns <see langword="false"/> otherwise.
    /// </summary>
    public static bool IsOk<TOk, TError>(this Validation<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        bool OnOk(TOk _) => true;

        [DebuggerStepThrough]
        bool OnError(ImmutableArray<TError> _) => false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if in Error state. Returns <see langword="false"/> otherwise.
    /// </summary>
    public static bool IsError<TOk, TError>(this Validation<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        bool OnOk(TOk _) => false;

        [DebuggerStepThrough]
        bool OnError(ImmutableArray<TError> _) => true;
    }

    /// <summary>
    /// Forces unpacking of Ok state. Does throw an <see cref="ValidationIsInErrorStateException"/> if
    /// Validation is in error state. Useful for unit testing.
    /// </summary>
    /// <exception cref="ValidationIsInErrorStateException">Thrown when in Error state</exception>
    public static TOk GetOkOrThrow<TOk, TError>(this Validation<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        TOk OnOk(TOk x) => x;

        [DebuggerStepThrough]
        TOk OnError(ImmutableArray<TError> x) => throw new ValidationIsInErrorStateException(x.Cast<object>().ToArray());
    }

    /// <summary>
    /// Forces unpacking of Error state. Does throw an <see cref="ValidationIsInOkStateException"/> if
    /// Validation is in error state. Useful for unit testing.
    /// </summary>
    /// <exception cref="ValidationIsInOkStateException">Thrown when in Ok state</exception>
    public static ImmutableArray<TError> GetErrorOrThrow<TOk, TError>(this Validation<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        ImmutableArray<TError> OnOk(TOk x) => throw new ValidationIsInOkStateException(x);

        [DebuggerStepThrough]
        ImmutableArray<TError> OnError(ImmutableArray<TError> x) => x;
    }
}

/// <summary>
/// Adds error aggregation function to <see cref="Validation{TOk,TError}"/>.
/// </summary>
[DebuggerStepThrough]
public static class ApplicativeExtensions
{
    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, TOut, TError>(
        this (Validation<A, TError>, Validation<B, TError>) input,
        Func<A, B, TOut> projection)
    {
        return input.Item1.Match(
            okA => input.Item2.Match<Validation<TOut, TError>>(
                okB => new OkValidation<TOut, TError>(projection(okA, okB)),
                errB => new ErrorValidation<TOut, TError>(errB)),
            errA => input.Item2.Match<Validation<TOut, TError>>(
                okB => new ErrorValidation<TOut, TError>(errA),
                errB => new ErrorValidation<TOut, TError>(errA.Concat(errB).ToImmutableArray())));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, TOut, TError>(
        this (Validation<A, TError>, Validation<B, TError>, Validation<C, TError>) input,
        Func<A, B, C, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2)
            .Apply((a, b) => (a, b));

        return (tuple, input.Item3)
            .Apply((first, c) => projection(first.a, first.b, c));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, D, TOut, TError>(
        this (
            Validation<A, TError>,
            Validation<B, TError>,
            Validation<C, TError>,
            Validation<D, TError>
            ) input,
        Func<A, B, C, D, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2, input.Item3)
            .Apply((a, b, c) => (a, b, c));

        return (tuple, input.Item4)
            .Apply((first, second) => projection(first.a, first.b, first.c, second));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, D, E, TOut, TError>(
        this (
            Validation<A, TError>,
            Validation<B, TError>,
            Validation<C, TError>,
            Validation<D, TError>,
            Validation<E, TError>
            ) input,
        Func<A, B, C, D, E, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2, input.Item3, input.Item4)
            .Apply((a, b, c, d) => (a, b, c, d));

        return (tuple, input.Item5)
            .Apply((first, second) => projection(first.a, first.b, first.c, first.d, second));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, D, E, F, TOut, TError>(
        this (
            Validation<A, TError>,
            Validation<B, TError>,
            Validation<C, TError>,
            Validation<D, TError>,
            Validation<E, TError>,
            Validation<F, TError>
            ) input,
        Func<A, B, C, D, E, F, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2, input.Item3, input.Item4, input.Item5)
            .Apply((a, b, c, d, e) => (a, b, c, d, e));

        return (tuple, input.Item6)
            .Apply((first, second) => projection(first.a, first.b, first.c, first.d, first.e, second));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, D, E, F, G, TOut, TError>(
        this (
            Validation<A, TError>,
            Validation<B, TError>,
            Validation<C, TError>,
            Validation<D, TError>,
            Validation<E, TError>,
            Validation<F, TError>,
            Validation<G, TError>
            ) input,
        Func<A, B, C, D, E, F, G, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2, input.Item3, input.Item4, input.Item5, input.Item6)
            .Apply((a, b, c, d, e, f) => (a, b, c, d, e, f));

        return (tuple, input.Item7)
            .Apply((first, second) => projection(first.a, first.b, first.c, first.d, first.e, first.f, second));
    }

    /// <summary>
    /// If all <see cref="Validation{TOk,TError}"/> in input tuple are in OK state then call projection and wrap the result
    /// in a new <see cref="Validation{TOk,TError}"/>. Otherwise, aggregate all errors and wrap them in a new
    /// <see cref="Validation{TOk,TError}"/>. This is useful for input validation and error aggregation.
    /// </summary>
    public static Validation<TOut, TError> Apply<A, B, C, D, E, F, G, H, TOut, TError>(
        this (
            Validation<A, TError>,
            Validation<B, TError>,
            Validation<C, TError>,
            Validation<D, TError>,
            Validation<E, TError>,
            Validation<F, TError>,
            Validation<G, TError>,
            Validation<H, TError>
            ) input,
        Func<A, B, C, D, E, F, G, H, TOut> projection)
    {
        var tuple = (input.Item1, input.Item2, input.Item3, input.Item4, input.Item5, input.Item6, input.Item7)
            .Apply((a, b, c, d, e, f, g) => (a, b, c, d, e, f, g));

        return (tuple, input.Item8)
            .Apply((first, second) => projection(first.a, first.b, first.c, first.d, first.e, first.f, first.g, second));
    }
}

[DebuggerStepThrough]
public static class ValidationLinqExtensions
{
    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Validation<TOkOut, TError> Select<TOkIn, TError, TOkOut>(this Validation<TOkIn, TError> self, Func<TOkIn, TOkOut> map) => self.Map(map);

    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Validation<TOkOut, TError> SelectMany<TOkIn, TError, B, TOkOut>(
        this Validation<TOkIn, TError> self,
        Func<TOkIn, Validation<B, TError>> bind,
        Func<TOkIn, B, TOkOut> project) =>
        self.Bind(a =>
            bind(a).Select(b =>
                project(a, b)));
}

public class ValidationIsInErrorStateException : Exception
{
    public object ErrorContent { get; }

    public ValidationIsInErrorStateException(object[] errorContent) : base(
        $"Validation was unpacked and thought to be in Ok state but was in error state.{Environment.NewLine}{String.Join(Environment.NewLine, errorContent)}")
    {
        ErrorContent = errorContent;
    }
}

public class ValidationIsInOkStateException : Exception
{
    public object OkContent { get; }

    public ValidationIsInOkStateException(object okContent) : base("Validation was unpacked and thought to be in Error state but was in Ok state.")
    {
        OkContent = okContent;
    }
}
