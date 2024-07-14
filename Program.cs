using Discord.WebSocket;
using Discord;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace Picktor
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token = "";

        public Program()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task MainAsync()
        {
            await _client.LoginAsync(TokenType.Bot, _token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (message.Content.StartsWith("!resize"))
            {
                var parts = message.Content.Split(' ');
                if (parts.Length == 3 && message.Attachments.Count > 0)
                {
                    if (int.TryParse(parts[1], out int width) && int.TryParse(parts[2], out int height))
                    {
                        var attachment = message.Attachments.First();
                        var imageUrl = attachment.Url;

                        using (var client = new HttpClient())
                        {
                            var response = await client.GetAsync(imageUrl);
                            var imageStream = await response.Content.ReadAsStreamAsync();
                            var image = Image.Load(imageStream);
                            image.Mutate(x => x.Resize(width, height));

                            var outputStream = new MemoryStream();
                            image.SaveAsPng(outputStream);
                            outputStream.Seek(0, SeekOrigin.Begin);

                            await message.Channel.SendFileAsync(outputStream, "resized.png");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Invalid width or height.");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Usage: !resize <width> <height> and attach an image.");
                }
            }
            else if (message.Content.StartsWith("!grayscale"))
            {
                if (message.Attachments.Count > 0)
                {
                    var attachment = message.Attachments.First();
                    var imageUrl = attachment.Url;

                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(imageUrl);
                        var imageStream = await response.Content.ReadAsStreamAsync();
                        var image = Image.Load(imageStream);
                        image.Mutate(x => x.Grayscale());

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "grayscale.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
        }
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    }
}
