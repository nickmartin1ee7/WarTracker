namespace WarTracker
{
    internal class DataSource : IDisposable
    {
        private const string START_TAG = @"<div id=""feedler"" class=""scrotabs"">";
        private const string END_TAG = @"<div id=""language"" class=""scrotabs""";
        private const string SOURCE_URL = "https://liveuamap.com";

        private readonly HttpClient _client = new();

        public async Task<string> GetAsync()
        {
                var result = await _client.GetAsync(SOURCE_URL);
                var html = await result.Content.ReadAsStringAsync();

                if (!result.IsSuccessStatusCode || string.IsNullOrWhiteSpace(html))
                {
                    throw new Exception($"No content received. Error ({result.StatusCode}) {result.ReasonPhrase}");
                }

                var startIdx = html.IndexOf(START_TAG, StringComparison.InvariantCulture);
                var endIdx = html.IndexOf(END_TAG, StringComparison.InvariantCulture);

                return html.Substring(startIdx, endIdx - startIdx);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
