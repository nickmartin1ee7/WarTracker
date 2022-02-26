using System.Diagnostics;
using WarTracker;

const string CONTENT_FILE = "index.html";
const string TITLE = "War Tracker";

const char POSITIVE = '+';
const char NEGATIVE = '-';
const char NEUTRAL = 'I';
const char VERBOSE = 'V';

string? lastContent = null;
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
Log(NEUTRAL, $"Press ENTER to open latest news in default web browser");

_ = StartBackgroundJob();

while (true)
{
    Console.ReadKey(true);

    if (!string.IsNullOrWhiteSpace(lastContent))
    {
        OpenContent();
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

void OpenContent() => Process.Start(new ProcessStartInfo
{
    CreateNoWindow = true,
    UseShellExecute = true,
    FileName = new FileInfo(CONTENT_FILE).FullName
});

#endregion

async Task StartBackgroundJob()
{
    using var ds = new DataSource();
    int divCount = default;

    while (true)
    {
        try
        {
            Log(VERBOSE, "Downloading latest content...");

            lastContent = await ds.GetAsync();
            var newDivCount = lastContent.Split("<div").Length;
            lastUpdate = DateTime.Now;

            Log(VERBOSE, $"Old Posts: {divCount} | New Posts: {newDivCount}");

            if (divCount == default)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);

                if (shouldAlert)
                    Beep();

                Log(POSITIVE, $"Got initial content @ {lastUpdate}");
            }
            else if (newDivCount != divCount)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);

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
        }

        Log(VERBOSE, $"Updating in {delayAmount} @ {DateTime.Now.Add(delayAmount)}");
        await Task.Delay(delayAmount);
    }
}
