using System.Threading.Channels;
using Application;
using Application.Module;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.Net;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ReservationBot;

class Program
{
    private  IServiceProvider _provider;
    private  DiscordSocketClient _discordClient;
    private  CommandService _commandService;
    private  IConfiguration _configuration;
    private InteractionService _interactionService;
    
    static void Main()
        => new Program().MainAsync().GetAwaiter().GetResult();

    private async Task MainAsync()
    {
        await ConnectToDiscordAsync();
        Console.ReadLine();
    }
    private async Task Client_Ready()
    {
        ulong guildId = 0;
        _discordClient.GuildAvailable += async (guild) =>
        {
            guildId = guild.Id;
            // Сохраните guild.Id, если он вам нужен
        };
        // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
        var guild = _discordClient.GetGuild(guildId);

        // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
        var guildCommand = new SlashCommandBuilder();

        // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
        guildCommand.WithName("first-command");

        // Descriptions can have a max length of 100.
        guildCommand.WithDescription("This is my first guild slash command!");

        // Let's do our global command
        var globalCommand = new SlashCommandBuilder();
        globalCommand.WithName("first-global-command");
        globalCommand.WithDescription("This is my first global slash command");

        try
        {
            // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            // With global commands we don't need the guild.
            await _discordClient.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
            // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
        }
        catch(ApplicationCommandException exception)
        {
            // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

            // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
            Console.WriteLine(json);
        }
    }
    /// <summary>
    /// конктим бота к дискорду и логинимся
    /// </summary>
    public async Task ConnectToDiscordAsync() 
    { 
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
    };

    var serviceProvider = new ServiceCollection()
        .AddSingleton<DiscordSocketClient>(new DiscordSocketClient(config))
        .AddSingleton<InteractionService>()
        .AddSingleton<CommandService>()
        .BuildServiceProvider();

    _commandService = serviceProvider.GetRequiredService<CommandService>();
    _discordClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
    
    //_discordClient.Ready += Client_Ready;
    _provider = serviceProvider;

    _interactionService = serviceProvider.GetRequiredService<InteractionService>();
    await _interactionService.AddModulesAsync(typeof(CommandModule).Assembly, serviceProvider);

    // Подписываемся на событие Ready
    _discordClient.Ready += async () =>
    {
        // Вызовите регистрацию команд после того, как клиент будет готов
        await _interactionService.RegisterCommandsGloballyAsync();
    };

    // Обработчики событий
    _discordClient.MessageReceived += async (message) =>
    {
        // Проверка, является ли сообщение от пользователя
        if (message is SocketUserMessage userMessage && userMessage.Source == MessageSource.User)
        {
            var argPos = 0;

            // Проверка префикса команды
            if (userMessage.HasStringPrefix("/", ref argPos))
            {
                var context = new SocketCommandContext(_discordClient, userMessage);
                // Обработка команд
                await _commandService.ExecuteAsync(context, argPos, _provider);
            }
        }
    };

    _discordClient.InteractionCreated += async interaction =>
    {
        var ctx = new SocketInteractionContext(_discordClient, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _provider);
    };

    // Логирование
    _discordClient.Log += Log;

    // Получение токена из файла
    string token;
    using (StreamReader streamReader = new StreamReader("token.txt"))
    {
        token = await streamReader.ReadToEndAsync();
    }

    // Логин и запуск клиента
    await _discordClient.LoginAsync(TokenType.Bot, token);
    await _discordClient.StartAsync();

    // Ожидание завершения приложения
    await Task.Delay(-1);
}
    /// <summary>
    /// Записывает логи в консоль
    /// </summary>
    /// <param name="logMessage"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Task Log(LogMessage logMessage)
    {
        Console.WriteLine(logMessage.ToString());
        return Task.CompletedTask;
    }
}
