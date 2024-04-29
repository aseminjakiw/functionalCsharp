using System.Diagnostics;

namespace FunctionalCsharp;

/// <summary>
/// Better version of Nullable. Can either be Some or None. Start with using Match(). Use Map() and Bind()
/// to process values. Does not allow null as a value.
/// </summary>
[DebuggerStepThrough]
public static class Option
{
    public static Option<T> Some<T>(T value) => new Some<T>(value);
    public static Option<T> None<T>() => new None<T>();
}

/// <summary>
/// Better version of Nullable. Can either be Some or None. Start with using Match(). Use Map() and Bind()
/// to process values. Does not allow null as a value.
/// </summary>
[DebuggerStepThrough]
public abstract class Option<T>
{
    /// <summary>
    /// Transform the Option into any other type. You must provide a function for Some and None case. All other
    /// option functions are based on this. Does not allow null as a value.
    /// </summary>
    /// <param name="some">Function to transform the Some state</param>
    /// <param name="none">Function to transform the None state</param>
    /// <typeparam name="TResult">Transformation result</typeparam>
    /// <returns>Either runs the Some function or the none function depending on the current value of Option.</returns>
    public abstract TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none);

    public static implicit operator Option<T>(T value) => new Some<T>(value);

    /// <summary>
    /// Transforms the content of an Option if in the Some state by applying the map function. Does nothing otherwise.
    /// </summary>
    /// <param name="map">Function to transform content of Option</param>
    /// <typeparam name="TResult">Type of transformed Option</typeparam>
    /// <returns>If in Some state returns transformed content. If in None state does nothing.</returns>
    /// <exception cref="InvalidOperationException">Raised when content of Option is null.</exception>
    public Option<TResult> Map<TResult>(Func<T, TResult> map)
    {
        return Match(
            some: OnOk,
            none: OnNone);

        [DebuggerStepThrough]
        Option<TResult> OnOk(T x)
        {
            var newValue = map(x);
            if (newValue == null)
                throw new InvalidOperationException("map evaluated to null");
            return new Some<TResult>(newValue);
        }

        [DebuggerStepThrough]
        Option<TResult> OnNone() => new None<TResult>();
    }

    /// <summary>
    /// Similar to <see cref="Map{TResult}"/>. The difference is that the bind function returns an Option itself.
    /// If Option is in Some state applies the bind function and directly returns the resulting Option. Does
    /// nothing in None state.
    /// </summary>
    /// <param name="bind">Function to transform content of Option</param>
    /// <typeparam name="TResult">Type of transformed Option</typeparam>
    /// <returns>If Some returns result of bind. If None does nothing.</returns>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind)
    {
        return Match(
            some: bind,
            none: OnNone);

        [DebuggerStepThrough]
        Option<TResult> OnNone() => new None<TResult>();
    }
}

[DebuggerStepThrough]
internal sealed class None<T> : Option<T>
{
    public override TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => none();
    public override string ToString() => "None";
}

[DebuggerStepThrough]
internal sealed class Some<T> : Option<T>
{
    private readonly T _value;

    public Some(T? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    public override TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => some(_value);
    public override string ToString() => $"Some {_value}";
}

/// <summary>
/// Functions working with <see cref="Option{T}"/>.
/// </summary>
[DebuggerStepThrough]
public static class OptionExtensions
{
    /// <summary>
    /// Similar to <see cref="Option{T}.Match{T}"/> but does not return a value.
    /// </summary>
    public static void MatchVoid<T>(this Option<T> option, Action<T> someAction, Action noneAction)
    {
        option.Match(
            OnSome,
            OnNone);
        return;

        [DebuggerStepThrough]
        Unit OnSome(T ok)
        {
            someAction(ok);
            return new Unit();
        }

        [DebuggerStepThrough]
        Unit OnNone()
        {
            noneAction();
            return new Unit();
        }
    }

    /// <summary>
    /// Packs a value into an option. Useful for returning starting with using Option.
    /// </summary>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Option<T> ToSome<T>(this T value) => Option.Some(value);

    /// <summary>
    /// Safely converts a nullable value (reference type) to Option.
    /// </summary>
    public static Option<T> NullableToOption<T>(this T? value) => value == null ? new None<T>() : new Some<T>(value);

    /// <summary>
    /// Safely converts a nullable value (value type) to Option.
    /// </summary>
    public static Option<T> NullableToOption<T>(this Nullable<T> value) where T : struct => value == null ? new None<T>() : new Some<T>(value.Value);

    /// <summary>
    /// Converts Option to Nullable.
    /// </summary>
    public static T? ToNullable<T>(this Option<T> value)
    {
        return value.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        T? OnSome(T x) => x;

        [DebuggerStepThrough]
        T? OnNone() => (T?)default;
    }

    /// <summary>
    /// If <paramref name="predicate"/> returns true leaves Option in Some state. Otherwise option gets switched into None.
    /// </summary>
    public static Option<T> Filter<T>(this Option<T> value, Func<T, bool> predicate)
    {
        return value.Bind(OnSome);

        [DebuggerStepThrough]
        Option<T> OnSome(T x) => predicate(x) ? new Some<T>(x) : new None<T>();
    }

    /// <summary>
    /// Returns content of Option if it is in Some state. Returns <param name="defaultValue"/> otherwise.
    /// </summary>
    public static T DefaultValue<T>(this Option<T> option, T defaultValue)
    {
        return option.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        T OnSome(T x) => x;

        [DebuggerStepThrough]
        T OnNone() => defaultValue;
    }

    /// <summary>
    /// Returns content of Option if it is in Some state. Otherwise, calls <param name="func"/> and returns the result.
    /// </summary>
    public static T DefaultWith<T>(this Option<T> option, Func<T> func)
    {
        return option.Match(OnSome, func);

        [DebuggerStepThrough]
        T OnSome(T x) => x;
    }

    /// <summary>
    /// Determines if Option is in Some state.
    /// </summary>
    public static bool IsSome<T>(this Option<T> option)
    {
        return option.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        bool OnSome(T _) => true;

        [DebuggerStepThrough]
        bool OnNone() => false;
    }

    /// <summary>
    /// Determines if Option is in None state.
    /// </summary>
    public static bool IsNone<T>(this Option<T> option)
    {
        return option.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        bool OnSome(T _) => false;

        [DebuggerStepThrough]
        bool OnNone() => true;
    }

    /// <summary>
    /// Compares content of Option with <param name="valueToCompare"/>.
    /// </summary>
    public static bool Contains<T>(this Option<T> option, T valueToCompare)
    {
        return option.Match(OnSome, OnNone);
        [DebuggerStepThrough]
        bool OnSome(T x) => x.Equals(valueToCompare);
        [DebuggerStepThrough]
        bool OnNone() => false;
    }

    /// <summary>
    /// This function is intended only for unit testing purpose. It extracts the value and throws an exception if necessary.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="option"></param>
    /// <returns>When in Some state returns the value</returns>
    /// <exception cref="OptionIsNoneException">Thrown when in None state</exception>
    public static T GetValueOrThrow<T>(this Option<T> option)
    {
        return option.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        T OnSome(T x) => x;

        [DebuggerStepThrough]
        T OnNone() => throw new OptionIsNoneException();
    }

    /// <summary>
    /// This function is intended only for unit testing purpose. It ensures Option is in None state. Throws
    /// an exception otherwise.
    /// </summary>
    /// <exception cref="OptionIsSomeException">Thrown when in Some state</exception>
    public static void IsNoneOrThrow<T>(this Option<T> option)
    {
        option.Match(OnSome, OnNone);
        return;

        [DebuggerStepThrough]
        Unit OnSome(T x) => throw new OptionIsSomeException(x);

        [DebuggerStepThrough]
        Unit OnNone() => new Unit();
    }

    /// <summary>
    /// Removes all None cases from the collection and unwraps the remaining Some cases. Very useful for filtering.
    /// </summary>
    public static IEnumerable<T> Choose<T>(this IEnumerable<Option<T>> source)
    {
        return source.Where(Predicate).Select(Selector);

        [DebuggerStepThrough]
        bool Predicate(Option<T> x) => x.IsSome();

        [DebuggerStepThrough]
        T Selector(Option<T> x) => x.GetValueOrThrow();
    }

    /// <summary>
    /// Converts an option into an enumerable. If option is in Some state, returns the value as en Enumerable with single item.
    /// Otherwise the enumerable is empty
    /// </summary>
    public static IEnumerable<T> ToEnumerable<T>(this Option<T> option)
    {
        return option.Match(OnSome, OnNone);

        [DebuggerStepThrough]
        IEnumerable<T> OnSome(T x)
        {
            yield return x;
        }

        [DebuggerStepThrough]
        IEnumerable<T> OnNone()
        {
            yield break;
        }
    }

    /// <summary>
    /// Similar to LINQ FirstOrDefault() but returns an Option instead.
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static Option<TSource> FirstAsOption<TSource>(this IEnumerable<TSource> source)
    {
        if (source == null)
        {
            ArgumentNullException.ThrowIfNull(source);
        }

        if (source is IList<TSource> list)
        {
            if (list.Count > 0)
            {
                return Option.Some(list[0]);
            }
        }
        else
        {
            using var e = source.GetEnumerator();
            if (e.MoveNext())
            {
                return Option.Some(e.Current);
            }
        }

        return Option.None<TSource>();
    }
}

[DebuggerStepThrough]
public class OptionIsNoneException : Exception
{
    public OptionIsNoneException() : base("Option was unpacked and thought to be in Some state but was in None state.")
    {
    }
}

[DebuggerStepThrough]
public class OptionIsSomeException : Exception
{
    public object SomeContent { get; }

    public OptionIsSomeException(object someContent) : base("Option was unpacked and thought to be in None state but was in Some state.")
    {
        SomeContent = someContent;
    }
}

[DebuggerStepThrough]
public static class OptionLinqExtensions
{
    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Option<B> Select<A, B>(this Option<A> self, Func<A, B> map) => self.Map(map);

    /// <summary>
    /// This method is only intended to be used by the compiler. It is used by LINQ query syntax.
    /// </summary>
    public static Option<C> SelectMany<A, B, C>(
        this Option<A> self,
        Func<A, Option<B>> bind,
        Func<A, B, C> project) =>
        self.Bind(a =>
            bind(a).Select(b =>
                project(a, b)));
}
