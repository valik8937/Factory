ТЕХНІЧНИЙ ДОВІДНИК: НОВІ ФІЧІ C# 13 (.NET 9) ТА C# 14 (.NET 10) ДЛЯ СИСТЕМНОГО ПРОМПТУ ШТУЧНОГО ІНТЕЛЕКТУ

РОЗДІЛ 1: C# 13 ТА .NET 9

1.1. Параметри-колекції (params Collections)

Дозволено використовувати модифікатор params не лише для масивів (T[]), але й
для будь-яких типів, сумісних із виразами колекцій (Collection Expressions):
Span<T>, ReadOnlySpan<T>, List<T>, IEnumerable<T>, IReadOnlyList<T> тощо.
Використання ReadOnlySpan<T> дозволяє уникнути виділення пам’яті у купі
(zero-allocation).

using System;

public class Logger
{
    public void Log(params ReadOnlySpan<string> messages)
    {
        foreach (var msg in messages)
        {
            Console.WriteLine(msg);
        }
    }
}

public class Program
{
    public static void Main()
    {
        var logger = new Logger();
        logger.Log("Error", "Unauthorized access", "UserId: 42");
    }
}

1.2. Новий тип блокування System.Threading.Lock

Додано спеціалізований клас System.Threading.Lock. Коли конструкція lock отримує
об’єкт цього типу, компілятор оптимізує її, перетворюючи на виклик методу
EnterScope(), який повертає ref struct (утилізується через Dispose та
унеможливлює витоки через виключення). Це працює на 25% швидше за
класичне блокування на базі Monitor.

using System.Threading;

public class SharedResource
{
    private readonly Lock _syncLock = new();
    private int _counter;

    public void Increment()
    {
        lock (_syncLock)
        {
            _counter++;
        }
    }

    public void SafeExecute()
    {
        using (_syncLock.EnterScope())
        {
            _counter--;
        }
    }
}

1.3. Асинхронний ітератор Task.WhenEach

Дозволяє обробляти асинхронні задачі в міру їх завершення за допомогою циклу
await foreach. Це набагато ефективніша альтернатива циклічному виклику
Task.WhenAny із видаленням завершених елементів зі списку.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DataFetcher
{
    public async Task FetchDataStream()
    {
        using var client = new HttpClient();
        
        var task1 = client.GetStringAsync("https://api.github.com");
        var task2 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
        var task3 = client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/2");

        var tasks = new List<Task<string>> { task1, task2, task3 };

        await foreach (var completedTask in Task.WhenEach(tasks))
        {
            try
            {
                string result = await completedTask;
                Console.WriteLine(result.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

1.4. Неявний доступ до індексу в ініціалізаторах об'єктів (Implicit Index Access)

Дозволено використовувати оператор індексу з кінця (^) безпосередньо в
ініціалізаторах масивів або колекцій.

public class Scoreboard
{
    public string[] Rankings { get; set; } = new string[5];
}

public class Program
{
    public static void Main()
    {
        var scoreboard = new Scoreboard()
        {
            Rankings = 
            {
                [^1] = "Gold",
                [^2] = "Silver",
                [^3] = "Bronze"
            }
        };
    }
}

1.5. Нові LINQ-методи: Index, CountBy, AggregateBy

  - Index(): повертає кортеж (int Index, T Value) для кожного елемента.
  - CountBy(): групує за ключем та підраховує кількість елементів у кожній групі
    без GroupBy.
  - AggregateBy(): виконує групування за ключем та агрегацію значень в один
    крок.

using System;
using System.Linq;
using System.Collections.Generic;

public record Student(string Name, string Grade);
public record Employee(string Name, string Department, decimal Salary);

public class LinqDemo
{
    public void Execute()
    {
        var students = new List<Student>
        {
            new("Alice", "A"),
            new("Bob", "B"),
            new("Charlie", "A")
        };

        foreach (var (index, student) in students.Index())
        {
            Console.WriteLine($"{index}: {student.Name}");
        }

        IEnumerable<KeyValuePair<string, int>> gradeCounts = students.CountBy(s => s.Grade);

        var employees = new List<Employee>
        {
            new("Dave", "IT", 5000),
            new("Eve", "IT", 6000),
            new("Frank", "HR", 4000)
        };

        var salaryBudgetByDept = employees.AggregateBy(
            keySelector: emp => emp.Department,
            seed: 0m,
            func: (currentTotal, emp) => currentTotal + emp.Salary
        );
    }
}

1.6. Робота з UUID v7 (Guid.CreateVersion7)

Створення часозалежних UUID версії 7, які підходять для використання в якості
кластеризованих первинних ключів у базах даних.

using System;

public class Order
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string OrderNumber { get; set; }
}

1.7. Гібридне кешування (HybridCache)

Нове API для кешування, яке об'єднує швидке L1 (In-Memory) кешування та
масштабоване L2 (Distributed/Redis) кешування з інтегрованим захистом
від ефекту масового запиту "cache stampede".

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;

public class ProductService
{
    private readonly HybridCache _cache;

    public ProductService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GetProductDataAsync(string productId)
    {
        return await _cache.GetOrCreateAsync(
            $"product-{productId}",
            async token => await FetchFromDatabaseAsync(productId, token)
        );
    }

    private Task<string> FetchFromDatabaseAsync(string productId, System.Threading.CancellationToken token)
    {
        return Task.FromResult($"Data for {productId}");
    }
}

РОЗДІЛ 2: C# 14 ТА .NET 10

2.1. Члени-розширення (Extension Members / "Extension Everything")

Новий синтаксис блоків розширень (extension), який дозволяє розширювати типи не
лише методами, а й властивостями (properties), статичними членами (static
members) та операторами.

using System;
using System.Collections.Generic;
using System.Linq;

public static class EnumerableExtensions
{
    extension<TSource>(IEnumerable<TSource> source)
    {
        public bool IsEmpty => !source.Any();

        public TSource GetFirstOrThrow()
        {
            return source.First();
        }
    }

    extension(DateTime dateTime)
    {
        public static DateTime Epoch => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        public bool IsUtc => dateTime.Kind == DateTimeKind.Utc;
    }
}

public class Program
{
    public static void Main()
    {
        var list = new List<int>();
        bool empty = list.IsEmpty;

        var now = DateTime.UtcNow;
        bool isUtc = now.IsUtc;
        var epoch = DateTime.Epoch;
    }
}

2.2. Контекстне ключове слово field (Напівавтоматичні властивості)

Позбавляє потреби вручну оголошувати приватні backing fields для властивостей,
якщо у блоках get або set (чи ініціалізаторах) потрібна логіка валідації.
Слово field посилається на автозгенероване компілятором поле.

using System;

public class User
{
    public string Email
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !value.Contains("@"))
            {
                throw new ArgumentException("Invalid email format");
            }
            field = value.Trim();
        }
    }

    public int Age { get; set => field = value >= 0 ? value : 0; }
}

2.3. Умовне null-присвоєння (Null-conditional assignment)

Дозволено використовувати оператори умовного доступу до членів (?. та ?[])
ліворуч від знака присвоєння. Присвоєння та виконання правої частини буде
повністю проігноровано, якщо об'єкт ліворуч дорівнює null.

public class Order
{
    public string TrackingNumber { get; set; }
}

public class Customer
{
    public Order ActiveOrder { get; set; }
}

public class Program
{
    public void ProcessCustomer(Customer customer)
    {
        customer?.ActiveOrder?.TrackingNumber = "UA123456789";
        customer?.ActiveOrder?.TrackingNumber += "-EXP";
    }
}

2.4. nameof для відкритих дженериків (Unbound generic types)

Оператор nameof тепер підтримує відкриті (unbound) дженерики, наприклад
nameof(List<>) або nameof(Dictionary<,>), що усуває потребу вказувати фіктивні
типи (nameof(List<int>)) лише для того, щоб отримати ім'я класу.

using System;
using System.Collections.Generic;

public class Logger
{
    public void LogType()
    {
        string listName = nameof(List<>);
        string dictName = nameof(Dictionary<,>);
        
        Console.WriteLine(listName);
        Console.WriteLine(dictName);
    }
}

2.5. Модифікатори параметрів у простих лямбда-виразах

У C# 14 можна вказувати модифікатори параметрів (ref, out, in, scoped, ref
readonly) без обов'язкового явного зазначення типу параметра в лямбді.

public delegate void Mutator(ref int value);
public delegate void Reader(in string data);

public class LambdaDemo
{
    public void Execute()
    {
        Mutator doubleValue = (ref x) => x *= 2;
        Reader printValue = (in x) => System.Console.WriteLine(x);

        int number = 10;
        doubleValue(ref number);
    }
}

2.6. Часткові конструктори та події (Partial constructors and partial events)

Тепер конструктори екземплярів та події можуть бути оголошені як partial,
подібно до методів. Це спрощує роботу генераторів коду (Source
Generators), дозволяючи розділити сигнатуру та реалізацію конструктора/події між
файлами.

// File1.cs
public partial class Controller
{
    public partial Controller(string route);
    public partial event EventHandler OnRequest;
}

// File2.cs
public partial class Controller
{
    private readonly string _route;

    public partial Controller(string route)
    {
        _route = route;
    }

    private EventHandler? _onRequest;
    public partial event EventHandler OnRequest
    {
        add => _onRequest += value;
        remove => _onRequest -= value;
    }
}

2.7. Власні оператори комбінованого присвоєння (User-defined compound assignment)

Дозволено явно перевантажувати оператори +=, -=, *=, /= тощо. Раніше компілятор
завжди розгортав x += y у x = x + y, що викликало створення нового об’єкта.
Пряме перевантаження дозволяє змінювати стан наявного об'єкта "на місці"
(in-place mutation) без нових алокацій.

public class Accumulator
{
    public int Total { get; private set; }

    public Accumulator(int start)
    {
        Total = start;
    }

    public static Accumulator operator +(Accumulator left, int value)
    {
        return new Accumulator(left.Total + value);
    }

    public void operator +=(int value)
    {
        Total += value;
    }
}

2.8. Скриптові файли C# на рівні SDK (File-Based Apps)

Запуск одного .cs файлу без створення файлу проекту .csproj чи рішень .sln.
Метадані та залежності пакетів NuGet імпортуються за допомогою
препроцесорних директив #:sdk, #:package, та #:project
безпосередньо вгорі коду. Запуск здійснюється за допомогою dotnet
run script.cs.

#:sdk Microsoft.NET.Sdk.Web
#:package Microsoft.EntityFrameworkCore.Sqlite@9.0.0

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder();
var app = builder.Build();

app.MapGet("/", () => "Hello from Single File Script!");
app.Run();
