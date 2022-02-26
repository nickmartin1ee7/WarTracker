using System.Diagnostics;
using WarTracker;

const string CONTENT_FILE = "posts.txt";
const string TITLE = "War Tracker";

const char POSITIVE = '+';
const char NEGATIVE = '-';
const char NEUTRAL = 'I';
const char VERBOSE = 'V';

FeedReport? lastReport = null;
DateTime? lastUpdate = null;

Console.Title = TITLE;

TimeSpan delayAmount;
do
{
    Console.Write("Enter how often to check for updates? (ex 01:00:00 is 1 hour): ");
} while (!TimeSpan.TryParse(Console.ReadLine(), out delayAmount));

Console.Write("Do you want to play a sound when there is an update? y/N");
var shouldAlert = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';
Console.WriteLine();
Console.Write("Do you want to enable verbose output? y/N");
var shouldVerbose = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';

Console.Clear();
UpdateTitle();

Log(NEUTRAL, $"Checking for updates every {delayAmount}");
Log(NEUTRAL, $"Press ENTER to display the last post");

_ = StartBackgroundJob();

while (true)
{
    Console.ReadKey(true);

    if (lastReport is not null)
    {
        PrintLastReport();
    }
    else
    {
        Log(NEGATIVE, "No data yet to view");
    }
}

#region Helpers

void Beep()
{
    for (int i = 0; i < 4; i++)
    {
        Console.Beep(800, 100);
    }
}

void UpdateTitle()
{
    var updateText = lastUpdate.HasValue ? lastUpdate.Value.ToString() : "Never";
    Console.Title = $"{TITLE} - U: {updateText}";
}

void Log(char lvl, string message)
{
    if (lvl is VERBOSE && !shouldVerbose)
        return;

    Console.ForegroundColor = lvl switch
    {
        POSITIVE => ConsoleColor.Green,
        NEGATIVE => ConsoleColor.Red,
        NEUTRAL => ConsoleColor.White,
        VERBOSE => ConsoleColor.DarkGray,
        _ => Console.ForegroundColor,
    };

    Console.WriteLine($"[{lvl}] {DateTime.Now} - {message}");
    Console.ResetColor();
}

void PrintLastReport()
{
    Console.WriteLine(lastReport?.Posts.FirstOrDefault()?.ToString());
    Console.WriteLine($"To view the entire report, open: {new FileInfo(CONTENT_FILE).FullName}");
}

#endregion

async Task StartBackgroundJob()
{
    using var ds = new DataSource();
    int lastPosts = default;

    while (true)
    {
        try
        {
            Log(VERBOSE, "Downloading latest content...");

            lastReport = await ds.GetAsync();
            lastUpdate = DateTime.Now;

            Log(VERBOSE, $"Old Posts: {lastPosts} | New Posts: {lastReport.Posts.Length}");

            if (lastPosts == default)
            {
                lastPosts = lastReport.Posts.Length;
                await File.WriteAllTextAsync(CONTENT_FILE, lastReport.ToString());

                if (shouldAlert)
                    Beep();

                Log(POSITIVE, $"Got initial content @ {lastUpdate}");
            }
            else if (lastReport.Posts.Length != lastPosts)
            {
                lastPosts = lastReport.Posts.Length;
                await File.WriteAllTextAsync(CONTENT_FILE, lastReport.ToString());

                if (shouldAlert)
                    Beep();

                Log(POSITIVE, $"New update @ {lastUpdate}");
            }
            else
            {
                Log(VERBOSE, $"No new content @ {lastUpdate}");
            }

            UpdateTitle();
        }
        catch (Exception e)
        {
            Log(NEGATIVE, e.Message);
            Log(NEUTRAL, "Retrying in 30s...");
            await Task.Delay(TimeSpan.FromSeconds(30));
            continue;
        }

        Log(VERBOSE, $"Updating in {delayAmount} @ {DateTime.Now.Add(delayAmount)}");
        await Task.Delay(delayAmount);
    }
}
