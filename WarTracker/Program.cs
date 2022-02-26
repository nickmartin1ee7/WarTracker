using System.Diagnostics;
using WarTracker;

const string CONTENT_FILE = "index.html";
const string TITLE = "War Tracker";

const char POSITIVE = '+';
const char NEGATIVE = '-';
const char NEUTRAL = ' ';
const char DEBUG = 'D';

string? lastContent = null;
DateTime? lastUpdate = null;

Console.Title = TITLE;

TimeSpan delayAmount;
do
{
    Console.Write("Enter how often to check for updates? (ex 01:00:00 is 1 hour): ");
} while (!TimeSpan.TryParse(Console.ReadLine(), out delayAmount));

Console.Clear();

UpdateTitle();

Log(' ', $"Checking for updates every {delayAmount}");
Log(' ', $"Press ENTER to open latest news in default web browser");

_ = StartBackgroundJob(delayAmount);

while (true)
{
    Console.ReadKey(true);

    if (!string.IsNullOrWhiteSpace(lastContent))
    {
        OpenContent();
    }
}

void UpdateTitle()
{
    var updateText = lastUpdate.HasValue ? lastUpdate.Value.ToString() : "Never";
    Console.Title = $"{TITLE} - U: {updateText}";
}

async Task StartBackgroundJob(TimeSpan delay)
{
    using var ds = new DataSource();
    int divCount = default;

    while (true)
    {
        try
        {
            lastContent = await ds.GetAsync();
            var newDivCount = lastContent.Split("<div").Length;
            lastUpdate = DateTime.Now;
#if DEBUG
            Log('D', $"Old Posts: {divCount} | New Posts: {newDivCount}");
#endif

            if (divCount == default)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);
                Log('+', $"Got initial content @ {lastUpdate}");
            }
            else if (newDivCount != divCount)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);
                Log('+', $"New update @ {lastUpdate}");
            }

            UpdateTitle();
        }
        catch (Exception e)
        {
            Log('-', e.Message);
        }

        await Task.Delay(delay);
    }
}

void Log(char lvl, string message)
{
    Console.ForegroundColor = lvl switch
    {
        POSITIVE => ConsoleColor.Green,
        NEGATIVE => ConsoleColor.Red,
        NEUTRAL => ConsoleColor.White,
        DEBUG => ConsoleColor.DarkGray,
        _ => Console.ForegroundColor,
    };

    Console.WriteLine($"[{lvl}] {DateTime.Now} - {message}");
    Console.ResetColor();
}

void OpenContent() => Process.Start(new ProcessStartInfo("cmd.exe", @$"/c ""{new FileInfo(CONTENT_FILE).FullName}"""));