# Functional C#

## Functional programming in C# made easy

This library tries to bring concepts from functional programming
to C#. Respecting the Pareto principle, it will only contain the 
20% of the concepts that you will need 80% of the time. It is 
inspired by the following references. I highly recommend to read 
them:

- [F#](https://fsharp.org/)
- [C# language-ext](https://github.com/louthy/language-ext)
- [Mark Seeman: Church encoded either](https://blog.ploeh.dk/2018/06/11/church-encoded-either/)

## Content

- [Option: Maybe monad and functor](https://github.com/aseminjakiw/functionalCsharp#Option)
- [Result: Either monad and functor](https://github.com/aseminjakiw/functionalCsharp#Result)
- [Validation: Either monad, functor and applicative](https://github.com/aseminjakiw/functionalCsharp#Validation)

## Option

``Option<T>`` is used as a type safe replacement for ``null``. 
Technically it is a maybe monad and functor implemented with 
Church-encoding. It is analogous to [F# Option](https://fsharpforfunandprofit.com/posts/the-option-type/),
hence the name.

Use this type as a complete and better replacement for ``null``, 
``Nullable<T>`` or ``T?``. Here some [help for this](https://fsharpforfunandprofit.com/posts/the-option-type/). 

```csharp
// create a option
Option<DateTime> time = DateTime.Now.ToSome();
Option<DateTime> time = Option.Some(DateTime.Now);
Option<DateTime> time = TryGetDateOrNull().NullableToOption();

// 'resolve' or stop using option
string timeString = time.Match(ok => ok.ToString(), () => "no time");
    
// using option with fluent style
string result = 
    TryGetNumber() // returns Option<int>
    .Bind(number => ToGreaterThanZeroOption(number))
    .Map(number => number.ToString())
    .DefaultWith("Not set or smaller than zero");

// using optin with monadic binding style
record User(string Name);
record Settings(TimeSpan UpdateIntervall);
record UserSettings(User user, Settings settings);

Option<User> userOption = TryGetCurrentUser();
Option<Settings> settingsOption = TryGetCurrentSettings();
var userInfo =
    from user in userOption         // variable user is now of type User, not Option<User>
    from settings in settingsOption // variable settings is now of type Settings, not Option<Settings>
    select new UserSettings(user, settings);
```

The last example uses C# LINQ syntax to emulate a monadic binding. 
This is inspired by - [C# language-ext](https://github.com/louthy/language-ext/wiki/Does-C%23-Dream-Of-Electric-Monads%3F). 
This concept is very powerful but now we are definitely bending 
the language! If both ``userOption`` and ``settingsOption`` have
a value, we create a ``UserSettings``. Otherwise, we return None

## Result

``Result<TOk,TError>`` is used when you want to either return a result of type A or B.
Usually you use this when a function either completes with a OK value or 
produces an error. This is why the type is named ``Result<TOk,TError>``. 
Technically it is a either monad and functor implemented with Church-encoding.
For more infos how to use ``Result<TOk,TError>`` see 
[Railway-Oriented-Programing](https://fsharpforfunandprofit.com/rop/). 

```csharp
// create a result
Result<DateTime,string> time = DateTime.Now.ToOk();
Result<DateTime,string> time = Result.Ok<DateTime,string>(DateTime.Now);
Result<DateTime,string> time = GetDate();

// 'resolve' or stop using result
string timeString = time.Match(
    ok => ok.ToString(), 
    error => $"Could not get current time: {error}");
    
// using result with fluent style
Result<string,string> result = 
    GetNumber() // returns Result<int,string>
    .Bind(number => ToGreaterThanZeroOption(number))
    .Map(number => number.ToString());

// using optin with monadic binding style
record User(string Name);
record Settings(TimeSpan UpdateIntervall);
record UserSettings(User user, Settings settings);

Result<User,string> userResult = GetCurrentUser();
Result<Settings,string> settingsResult = GetCurrentSettings();
var userInfo =
    from user in userResult         // variable user is now of type User, not Result<User,string>
    from settings in settingsResult // variable settings is now of type Settings, not Result<Settings,string>
    select new UserSettings(user, settings);
```

The last example uses C# LINQ syntax to emulate a monadic binding.
This is inspired by - [C# language-ext](https://github.com/louthy/language-ext/wiki/Does-C%23-Dream-Of-Electric-Monads%3F).
This concept is very powerful but now we are definitely bending
the language! If both ``userResult`` and ``settingsResult`` are 
OK, then we create a ``UserSettings``. Otherwise we stop processing
at the first error and return it. If you want to aggregate all 
errors you instead of stopping at the first you need an 
applicative such as [Validation](https://github.com/aseminjakiw/functionalCsharp#Validation)

## Validation

``Validation<TOk,TErro>`` is very similar to [Result<TOk,TError>](https://github.com/aseminjakiw/functionalCsharp#Result). 
Instead of a single error it always handles a collection of errors. 
It has the same functions as [Result<TOk,TError>](https://github.com/aseminjakiw/functionalCsharp#Result), 
take a look there. Additionally it allows switching between stopping
at the first error (monadic binding) or aggregating all errors and 
return them at the end (applicative style).

If you want to bail out at the first error, just use ``Bind()``
and LINQ syntax as ``Result<TOk,TError>``. If you want to aggregate 
errors you need to 'switch' to the applicative API. Let's say, you
have to angles and both must be between 0° and 180°. Then you can 
create a ``StartStopAngle`` instance containing both angles. You 
want to get either the valid instance, or a single error or both 
errors if both angles are outside the expected range. This works 
like this:
```csharp
record Angle(float angle); // must be between 0° and 180°
record StartSTopAngle(Angle angleA, Angle angleB); // top-level return type

// top-level function for creation and validation
public Validation<StartStopAngle,string> CreateStartStopAngle(float angleA, float angleB)
{
    Validation<Angle,string> validAngleA = IsInRange(angleA); 
    Validation<Angle,string> validAngleB = IsInRange(angleB);

    return 
        (validAngleA, validAngleB) // put both variables into a tuple
        .Apply((a, b) => new StartStopAngle(a, b)); // here does the magic happen
}

private Validation<Angle, string> IsInRange(float angle) =>
    angle is >= 0 and <= 180 // check condition
        ? new Angle(angle) // OK result
        : $"{angle} is outside the range of 0° zu 180°"; // error result
```

The magic happens inside the ``Apply()`` function. It does the following:
It unwraps all fields of the input tuple. This only works if all of them 
are of the Type ``Validation<...,TError>`` where ``TError`` can be any type but
all field must have the SAME type. If all fields are in OK state, ``Apply()``
calls the provided function. Otherwise, it will combine alle the errors
and returns them.
