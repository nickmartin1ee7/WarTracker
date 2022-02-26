using System.Diagnostics;
using WarTracker;

const string CONTENT_FILE = "index.html";

const char POSITIVE = '+';
const char NEGATIVE = '-';
const char NEUTRAL = ' ';
const char DEBUG = 'D';

string? lastContent = null;

Console.Title = "War Tracker";

TimeSpan delayAmount;
do
{
    Console.Write("Enter how often to check for updates? (ex 01:00:00 is 1 hour): ");
} while (!TimeSpan.TryParse(Console.ReadLine(), out delayAmount));

Console.Clear();

Log(' ', $"Checking for updates every {delayAmount}");

_ = StartBackgroundJob(delayAmount);

while (true)
{
    Console.ReadKey(true);

    if (!string.IsNullOrWhiteSpace(lastContent))
    {
        OpenContent();
    }
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

#if DEBUG
            Log('D', $"Old: {divCount} | New: {newDivCount}");
#endif

            if (divCount == default)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);
                Log('+', $"Got initial content @ {DateTime.Now}");
            }
            else if (newDivCount != divCount)
            {
                divCount = newDivCount;
                await File.WriteAllTextAsync(CONTENT_FILE, lastContent);
                Log('+', $"New update @ {DateTime.Now}");
            }
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