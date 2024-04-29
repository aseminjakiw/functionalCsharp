using System.Diagnostics;

namespace FunctionalCsharp;

/// <summary>
/// Type that can either hold a OK value or a Error. Usually used
/// in context of operations that can fail expectedly, e.g. user input. Exceptions are usually not the correct way to handle this,
/// despite the fact that almost every C# programmer thinks so.
///
/// Is doesn't have to be an Ok and Error case. It can be used in any place where you want to return one of two
/// distinct cases but usually this includes some type of error.
/// </summary>
/// <typeparam name="TOk">Type of the OK case</typeparam>
/// <typeparam name="TError">Type of the Error case</typeparam>
[DebuggerStepThrough]
public abstract class Result<TOk, TError>
{
    /// <summary>
    /// Construct new Result in the OK state.
    /// </summary>
    public static Result<TOk, TError> Ok(TOk ok) => new Ok<TOk, TError>(ok);

    /// <summary>
    /// Construct new Result in the Error state.
    /// </summary>
    public static Result<TOk, TError> Error(TError error) => new Error<TOk, TError>(error);

    public static implicit operator Result<TOk, TError>(TOk ok) => new Ok<TOk, TError>(ok);
    public static implicit operator Result<TOk, TError>(TError error) => new Error<TOk, TError>(error);

    /// <summary>
    /// Transforms an Result into a different type. You must provide a function for Ok and Error state. Calls
    /// <param name="ok"/> when result is in Ok state or <param name="error"/> when result is in Error state.
    /// Returns the result of the called function.
    /// </summary>
    /// <param name="ok">Is called when Result is in Ok state and returns the value</param>
    /// <param name="error">Is called when Result is in Error state and returns the value</param>
    /// <typeparam name="T">type after the transformation</typeparam>
    /// <returns>Returns either the result of <param name="ok"/> or <param name="error"/> call, depending
    /// on Result state</returns>
    public abstract T Match<T>(Func<TOk, T> ok, Func<TError, T> error);

    /// <summary>
    /// Transforms the OK content of <see cref="Result{TOk,TError}"/>. Unwraps, calls <param name="map"/>
    /// and wraps up the value. Does nothing in Error state.
    /// </summary>
    /// <param name="map">function to be called with Ok content</param>
    /// <typeparam name="TOkOut">resulting Ok type. Error type stays the same.</typeparam>
    public Result<TOkOut, TError> Map<TOkOut>(Func<TOk, TOkOut> map)
    {
        return Match(
            ok: ToOk,
            error: ToError);

        [DebuggerStepThrough]
        Result<TOkOut, TError> ToOk(TOk x) => new Ok<TOkOut, TError>(map(x));

        [DebuggerStepThrough]
        Result<TOkOut, TError> ToError(TError x) => new Error<TOkOut, TError>(x);
    }

    /// <summary>
    /// Similar to <see cref="Map{TOkOut}"/>. If Result is in Ok state, calls <param name="bind"/> and directly
    /// returns its value (which is itself a Result). Does nothing otherwise.
    /// </summary>
    /// <param name="bind">function to be called with Ok content</param>
    /// <typeparam name="TOkOut">resulting OK type. Error type stays the same.</typeparam>
    public Result<TOkOut, TError> Bind<TOkOut>(Func<TOk, Result<TOkOut, TError>> bind)
    {
        return Match(
            ok: bind,
            error: ToError);

        [DebuggerStepThrough]
        Result<TOkOut, TError> ToError(TError x) => new Error<TOkOut, TError>(x);
    }

    /// <summary>
    /// Similar to <see cref="Map{TOkOut}"/> but works on the Error content instead of Ok content. Transforms
    /// the Error content or does nothing if in Ok state.
    /// </summary>
    /// <param name="map">function to be called with Error content</param>
    /// <typeparam name="TErrorOut">resulting Error type. OK type stays the same.</typeparam>
    public Result<TOk, TErrorOut> MapError<TErrorOut>(Func<TError, TErrorOut> map)
    {
        return Match(
            ok: ToOk,
            error: ToError);

        [DebuggerStepThrough]
        Result<TOk, TErrorOut> ToOk(TOk x) => new Ok<TOk, TErrorOut>(x);

        [DebuggerStepThrough]
        Result<TOk, TErrorOut> ToError(TError x) => new Error<TOk, TErrorOut>(map(x));
    }

    /// <summary>
    /// Similar to <see cref="Bind{TOkOut}"/> but works on Error state. Calls <param name="bind"/> if in
    /// Error state and returns its value directly. Does nothing if in OK state.
    /// </summary>
    /// <param name="bind">function to be called with Error content</param>
    /// <typeparam name="TErrorOut">resulting Error type. OK type stays the same.</typeparam>
    public Result<TOk, TErrorOut> BindError<TErrorOut>(Func<TError, Result<TOk, TErrorOut>> bind)
    {
        return Match(
            ok: ToOk,
            error: bind);

        [DebuggerStepThrough]
        Result<TOk, TErrorOut> ToOk(TOk x) => new Ok<TOk, TErrorOut>(x);
    }
}

[DebuggerStepThrough]
internal sealed class Error<TOk, TError>(TError value) : Result<TOk, TError>
{
    public override T Match<T>(Func<TOk, T> ok, Func<TError, T> error) => error(value);
    public override string ToString() => $"Error {value}";
}

[DebuggerStepThrough]
internal sealed class Ok<TOk, TError>(TOk value) : Result<TOk, TError>
{
    public override T Match<T>(Func<TOk, T> ok, Func<TError, T> error) => ok(value);
    public override string ToString() => $"Ok {value}";
}

/// <summary>
/// Functions working with <see cref="Result{TOk,TError}"/> in the context of exceptions.
/// </summary>
[DebuggerStepThrough]
public static class Result
{
    /// <summary>
    /// Executes <param name="func"/> and returns its value as <see cref="Result{TOk,TError}"/> (OK state).
    /// If any exception is raised, it will be catched and wrapped into the Error state and returned.
    /// </summary>
    public static Result<TOk, Exception> CatchAll<TOk>(Func<TOk> func)
    {
        try
        {
            var ok = func();
            return Result<TOk, Exception>.Ok(ok);
        }
        catch (Exception e)
        {
            return Result<TOk, Exception>.Error(e);
        }
    }

    /// <summary>
    /// Executes <param name="func"/> and returns its value as <see cref="Result{TOk,TError}"/> (OK state).
    /// If exception of type <typeparam name="TException"/> is raised, it will be catched and wrapped into
    /// the Error state and returned. Other exceptions are not handled.
    /// </summary>
    public static Result<TOk, TException> Catch<TOk, TException>(Func<TOk> func)
        where TException : Exception
    {
        try
        {
            var ok = func();
            return Result<TOk, TException>.Ok(ok);
        }
        catch (TException e)
        {
            return Result<TOk, TException>.Error(e);
        }
    }
}

/// <summary>
/// Functions working with <see cref="Result{TOk,TError}"/>
/// </summary>
[DebuggerStepThrough]
public static class ResultExtensions
{
    /// <summary>
    /// Similar to <see cref="Result{TOk,TError}.Match{T}"/> but does not return a value.
    /// </summary>
    public static void MatchVoid<TOk, TError>(this Result<TOk, TError> result, Action<TOk> okAction, Action<TError> errorAction)
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
        Unit OnError(TError error)
        {
            errorAction(error);
            return new Unit();
        }
    }

    /// <summary>
    /// Transforms an <see cref="Option{T}"/> into a result. If Option is in Some state, content will be packed
    /// into Ok state of <see cref="Result{TOk,TError}"/>. Otherwise <param name="noneReplacement"/> will be packed into
    /// <see cref="Result{TOk,TError}"/>
    /// </summary>
    /// <param name="option">Option value to transform</param>
    /// <param name="noneReplacement">Will be used when Option is in None state</param>
    public static Result<TOk, TError> ToResult<TOk, TError>(this Option<TOk> option, TError noneReplacement)
    {
        return option.Match(Result<TOk, TError>.Ok, OnError);

        [DebuggerStepThrough]
        Result<TOk, TError> OnError() => Result<TOk, TError>.Error(noneReplacement);
    }

    /// <summary>
    /// Transforms into an <see cref="Option{T}"/>. If in Ok state, content will be packed into Option as
    /// Some. Otherwise it will be None.
    /// </summary>
    public static Option<TOk> OkToOption<TOk, TError>(this Result<TOk, TError> result)
    {
        return result.Match(Option.Some, OnError);

        [DebuggerStepThrough]
        Option<TOk> OnError(TError _) => Option.None<TOk>();
    }

    /// <summary>
    /// Transforms into an <see cref="Option{T}"/>. If in Error state, content will be packed into Option as
    /// Some. Otherwise it will be None.
    /// </summary>
    public static Option<TError> ErrorToOption<TOk, TError>(this Result<TOk, TError> result)
    {
        return result.Match(OnOk, Option.Some);

        [DebuggerStepThrough]
        Option<TError> OnOk(TOk _) => Option.None<TError>();
    }

    /// <summary>
    /// Returns true if Result is in OK state. Returns false otherwise
    /// </summary>
    public static bool IsOk<TOk, TError>(this Result<TOk, TError> result)
    {
        return result.Match(OnOk, OnError);

        [DebuggerStepThrough]
        bool OnOk(TOk _) => true;
        [DebuggerStepThrough]
        bool OnError(TError _) => false;
    }

    /// <summary>
    /// Returns true if Result is in Error state. Returns false otherwise
    /// </summary>
    public static bool IsError<TOk, TError>(this Result<TOk, TError> result)
    {
        return result.Match(OnOk, OnError);

        [DebuggerStepThrough]
        bool OnOk(TOk _) => false;
        [DebuggerStepThrough]
        bool OnError(TError _) => true;
    }

    /// <summary>
    /// Forces unpacking of Ok state. Does throw an <see cref="ResultIsInErrorStateException"/> if
    /// result is in error state. Useful for unit testing.
    /// </summary>
    /// <exception cref="ResultIsInErrorStateException">Thrown when in Error state</exception>
    public static TOk GetOkOrThrow<TOk, TError>(this Result<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        TOk OnOk(TOk x) => x;

        [DebuggerStepThrough]
        TOk OnError(TError x) => throw new ResultIsInErrorStateException(x);
    }

    /// <summary>
    /// Forces unpacking of Error state. Does throw an <see cref="ResultIsInOkStateException"/> if
    /// result is in error state. Useful for unit testing.
    /// </summary>
    /// <exception cref="ResultIsInOkStateException">Thrown when in Ok state</exception>
    public static TError GetErrorOrThrow<TOk, TError>(this Result<TOk, TError> input)
    {
        return input.Match(OnOk, OnError);

        [DebuggerStepThrough]
        TError OnOk(TOk x) => throw new ResultIsInOkStateException(x);

        [DebuggerStepThrough]
        TError OnError(TError x) => x;
    }
}

[DebuggerStepThrough]
public static class ResultLinqExtensions
{
    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Result<TOkOut, TError> Select<TOkIn, TError, TOkOut>(this Result<TOkIn, TError> self, Func<TOkIn, TOkOut> map) => self.Map(map);

    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Result<TOkOut, TError> SelectMany<TOkIn, TError, B, TOkOut>(
        this Result<TOkIn, TError> self,
        Func<TOkIn, Result<B, TError>> bind,
        Func<TOkIn, B, TOkOut> project) =>
        self.Bind(a =>
            bind(a).Select(b =>
                project(a, b)));
}

public class ResultIsInErrorStateException : Exception
{
    public object ErrorContent { get; }

    public ResultIsInErrorStateException(object errorContent) : base("Result was unpacked and thought to be in Ok state but was in error state.")
    {
        ErrorContent = errorContent;
    }
}

public class ResultIsInOkStateException : Exception
{
    public object OkContent { get; }

    public ResultIsInOkStateException(object okContent) : base("Result was unpacked and thought to be in Error state but was in Ok state.")
    {
        OkContent = okContent;
    }
}
