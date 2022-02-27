using System.Linq;
using WarTracker.ClassLibrary;

const string TITLE = "War Tracker";
var reportFile = new FileInfo(Path.Combine(Path.GetTempPath(), TITLE, "posts.txt"));

await CreateSaveLocation();

const char POSITIVE = '+';
const char NEGATIVE = '-';
const char NEUTRAL = 'I';
const char VERBOSE = 'V';

FeedReport? lastReport = null;
DateTime? lastUpdate = null;
bool lastCallFaulted = false;

Console.Title = TITLE;

TimeSpan delayAmount;
do
{
    Console.Write("Enter how often to check for updates? (ex 01:00:00 is 1 hour): ");
} while (!TimeSpan.TryParse(Console.ReadLine(), out delayAmount));

Console.Write("Do you want to play a sound when there is an update? y/N");
var shouldAlert = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';
Console.WriteLine();

Console.Write("Do you want to always display the latest post on a new post? y/N");
var shouldDisplayNewPosts = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';
Console.WriteLine();

Console.Write("Do you want to enable verbose output? y/N");
var shouldVerbose = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';
Console.Clear();

UpdateTitle();

Log(NEUTRAL, $"Checking for updates every {delayAmount}");
Log(NEUTRAL, "Press ENTER to display the last post");

_ = StartBackgroundJob();

while (true)
{
    Console.ReadKey(true);

    if (lastReport is not null)
    {
        DisplayLastPost();
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

void DisplayLastPost()
{
    Console.WriteLine(lastReport.Posts.FirstOrDefault()?.ToString());
    Console.WriteLine($"To view the entire report, open: {reportFile.FullName}");
}

#endregion

async Task StartBackgroundJob()
{
    using var ds = new DataSource();
    int? lastPostHash = null;

    while (true)
    {
        try
        {
            Log(VERBOSE, "Downloading latest content...");

            lastReport = await ds.GetAsync();
            lastUpdate = DateTime.Now;

            if (lastCallFaulted)
            {
                lastCallFaulted = false;
                Log(NEUTRAL, "Connection re-established");
            }

            var latestPostHash = lastReport.Posts.First().Title?.GetHashCode();
            Log(VERBOSE, $"Old Post: {lastPostHash} | New Post: {latestPostHash}");

            if (lastPostHash == default)
            {
                lastPostHash = latestPostHash;
                await File.WriteAllTextAsync(reportFile.FullName, lastReport.ToString());

                if (shouldAlert)
                    Beep();

                Log(POSITIVE, $"Got initial content @ {lastUpdate}");

                if (shouldDisplayNewPosts)
                    DisplayLastPost();
            }
            else if (latestPostHash != lastPostHash)
            {
                lastPostHash = latestPostHash;
                await File.WriteAllTextAsync(reportFile.FullName, lastReport.ToString());

                if (shouldAlert)
                    Beep();

                Log(POSITIVE, $"New update @ {lastUpdate}");

                if (shouldDisplayNewPosts)
                    DisplayLastPost();
            }
            else
            {
                Log(VERBOSE, $"No new content @ {lastUpdate}");
            }

            UpdateTitle();
        }
        catch (Exception e)
        {
            const int defaultRetryDelay = 30;

            var retryDelay = delayAmount.TotalSeconds < defaultRetryDelay
                ? delayAmount
                : TimeSpan.FromSeconds(defaultRetryDelay);

            lastCallFaulted = true;

            Log(NEGATIVE, e.Message);
            Log(NEUTRAL, $"Retrying in {retryDelay}...");

            await Task.Delay(retryDelay);
            continue;
        }

        Log(VERBOSE, $"Updating in {delayAmount} @ {DateTime.Now.Add(delayAmount)}");

        await Task.Delay(delayAmount);
    }
}

async Task CreateSaveLocation()
{
    if (!reportFile.Directory.Exists)
    {
        reportFile.Directory.Create();
    }

    if (!reportFile.Exists)
    {
        reportFile.Delete();
        await reportFile.Create().DisposeAsync();
    }
}
