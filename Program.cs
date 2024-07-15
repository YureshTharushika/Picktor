using Discord.WebSocket;
using Discord;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Microsoft.Extensions.Configuration;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using Color = SixLabors.ImageSharp.Color;

namespace Picktor
{
    internal class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token;

        public Program()
        {
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.MessageReceived += MessageReceivedAsync;

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _token = configuration["Discord:Token"];

            if (string.IsNullOrEmpty(_token))
            {
                throw new Exception("Bot token is not set in the configuration file.");
            }
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
            else if (message.Content.StartsWith("!rotate"))
            {
                if (message.Attachments.Count > 0)
                {
                    var parts = message.Content.Split(' ');
                    if (parts.Length == 2 && float.TryParse(parts[1], out float angle))
                    {
                        var attachment = message.Attachments.First();
                        var imageUrl = attachment.Url;

                        using (var client = new HttpClient())
                        {
                            var response = await client.GetAsync(imageUrl);
                            var imageStream = await response.Content.ReadAsStreamAsync();
                            var image = Image.Load(imageStream);
                            image.Mutate(x => x.Rotate(angle));

                            var outputStream = new MemoryStream();
                            image.SaveAsPng(outputStream);
                            outputStream.Seek(0, SeekOrigin.Begin);

                            await message.Channel.SendFileAsync(outputStream, "rotated.png");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Usage: !rotate <angle> and attach an image.");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!flip"))
            {
                var parts = message.Content.Split(' ');
                if (parts.Length == 2 && message.Attachments.Count > 0)
                {
                    var flipType = parts[1];
                    var attachment = message.Attachments.First();
                    var imageUrl = attachment.Url;

                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(imageUrl);
                        var imageStream = await response.Content.ReadAsStreamAsync();
                        var image = Image.Load(imageStream);
                        if (flipType == "horizontal")
                        {
                            image.Mutate(x => x.Flip(FlipMode.Horizontal));
                        }
                        else if (flipType == "vertical")
                        {
                            image.Mutate(x => x.Flip(FlipMode.Vertical));
                        }

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "flipped.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Usage: !flip <horizontal|vertical> and attach an image.");
                }
            }
            else if (message.Content.StartsWith("!crop"))
            {
                var parts = message.Content.Split(' ');
                if (parts.Length == 5 && message.Attachments.Count > 0)
                {
                    if (int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y) &&
                        int.TryParse(parts[3], out int width) && int.TryParse(parts[4], out int height))
                    {
                        var attachment = message.Attachments.First();
                        var imageUrl = attachment.Url;

                        using (var client = new HttpClient())
                        {
                            var response = await client.GetAsync(imageUrl);
                            var imageStream = await response.Content.ReadAsStreamAsync();
                            var image = Image.Load(imageStream);
                            image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, width, height)));

                            var outputStream = new MemoryStream();
                            image.SaveAsPng(outputStream);
                            outputStream.Seek(0, SeekOrigin.Begin);

                            await message.Channel.SendFileAsync(outputStream, "cropped.png");
                        }
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync("Invalid crop dimensions.");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Usage: !crop <x> <y> <width> <height> and attach an image.");
                }
            }
            else if (message.Content.StartsWith("!addtext"))
            {
                var parts = message.Content.Split(new[] { ' ' }, 3);
                if (parts.Length == 3 && message.Attachments.Count > 0)
                {
                    var text = parts[2];
                    var attachment = message.Attachments.First();
                    var imageUrl = attachment.Url;

                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync(imageUrl);
                        var imageStream = await response.Content.ReadAsStreamAsync();
                        var image = Image.Load(imageStream);
                        image.Mutate(x => x.DrawText(text, SystemFonts.CreateFont("Arial", 24), Color.White, new PointF(10, 10)));

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "text.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Usage: !addtext <your text> and attach an image.");
                }
            }
            else if (message.Content.StartsWith("!blur"))
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
                        image.Mutate(x => x.GaussianBlur(5));

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "blurred.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!sharpen"))
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
                        image.Mutate(x => x.GaussianSharpen(5));

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "sharpened.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!sepia"))
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
                        image.Mutate(x => x.Sepia());

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "sepia.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!invert"))
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
                        image.Mutate(x => x.Invert());

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "inverted.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!border"))
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
                        image.Mutate(x => x.Draw(Color.Red, 10, new RectangleF(0, 0, image.Width, image.Height)));

                        var outputStream = new MemoryStream();
                        image.SaveAsPng(outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        await message.Channel.SendFileAsync(outputStream, "border.png");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please attach an image.");
                }
            }
            else if (message.Content.StartsWith("!help"))
            {
                await message.Channel.SendMessageAsync(GetHelpMessage());
            }
        }

        private string GetHelpMessage()
        {
            return @"
**Picktor - Image Manipulation Bot Commands:**

1. `!resize <width> <height>` - Resize the attached image to the specified width and height.
2. `!grayscale` - Convert the attached image to grayscale.
3. `!rotate <angle>` - Rotate the attached image by the specified angle in degrees.
4. `!flip <horizontal|vertical>` - Flip the attached image horizontally or vertically.
5. `!crop <x> <y> <width> <height>` - Crop the attached image with the specified rectangle dimensions.
6. `!addtext <your text>` - Add the specified text to the attached image.
7. `!blur` - Apply a blur effect to the attached image.
8. `!sharpen` - Apply a sharpen effect to the attached image.
9. `!sepia` - Apply a sepia effect to the attached image.
10. `!invert` - Invert the colors of the attached image.
11. `!border` - Add a red border to the attached image.

To use a command, type the command followed by the required parameters (if any) and attach an image.
";
        }

        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    }
}
