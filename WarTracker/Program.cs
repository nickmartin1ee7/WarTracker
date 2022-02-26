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

Console.Write("Do you want to play a sound when there is an update? y/N");
var shouldAlert = char.ToUpperInvariant(Console.ReadKey(false).KeyChar) == 'Y';

Console.Clear();
UpdateTitle();

Log(' ', $"Checking for updates every {delayAmount}");
Log(' ', $"Press ENTER to open latest news in default web browser");

_ = StartBackgroundJob(delayAmount, shouldAlert);

while (true)
{
    Console.ReadKey(true);

    if (!string.IsNullOrWhiteSpace(lastContent))
    {
        OpenContent();
    }
}

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

async Task StartBackgroundJob(TimeSpan delay, bool alert)
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
                
                if (shouldAlert)
                    Beep();

                Log('+', $"Got initial content @ {lastUpdate}");
            }
            else if (newDivCount != divCount)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);

                if (shouldAlert)
                    Beep();

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