using peachpie.items.Arrays;

public class Program
{
    private static readonly String[] Names =
    [
        "array_map"
    ];
    
    public static void Main(string[] args)
    {
        if (args.Length < 1 || !Names.Contains(args[0]))
        {
            UsageMessage();
            return;
        }

        switch (args[0])
        {
            case "array_map": PhpArrayMap(); break;
            default: UsageMessage(); break;
        }
    }

    private static void UsageMessage()
    {
        Console.WriteLine("Usage: dotnet run <name>");
        foreach (var name in Names)
        {
            Console.WriteLine($"-- {name}");
        }
    }

    private static void PhpArrayMap()
    {
        new ArrayMap(100, 5);
        new ArrayMap(100000, 1000);
    }
}