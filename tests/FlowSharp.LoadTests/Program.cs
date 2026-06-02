using FlowSharp.LoadTests;

// Kullanim:
//   dotnet run -c Release --project tests/FlowSharp.LoadTests -- queue [total]
//   dotnet run -c Release --project tests/FlowSharp.LoadTests -- http <baseUrl> [total] [parallel]
var mode = args.Length > 0 ? args[0] : "queue";

switch (mode)
{
    case "queue":
        var total = args.Length > 1 ? int.Parse(args[1]) : 500;
        await QueueThroughput.RunAsync(total, parallelisms: [1, 4, 8, 16]);
        break;

    case "http":
        if (args.Length < 2)
        {
            Console.WriteLine("Kullanim: ... -- http http://localhost:5000 [total] [parallel]");
            return;
        }
        var url = args[1];
        var httpTotal = args.Length > 2 ? int.Parse(args[2]) : 2000;
        var httpParallel = args.Length > 3 ? int.Parse(args[3]) : 50;
        await HttpLoad.RunAsync(url, httpTotal, httpParallel);
        break;

    default:
        Console.WriteLine("Bilinmeyen mod. 'queue' veya 'http' kullanin.");
        break;
}
