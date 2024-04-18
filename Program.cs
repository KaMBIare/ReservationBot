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
    /// <summary>
    /// конктим бота к дискорду и логинимся
    /// </summary>
    public async Task ConnectToDiscordAsync() 
    { 
    //создаем конфиг для клиента
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
    
    _provider = serviceProvider;

    _interactionService = serviceProvider.GetRequiredService<InteractionService>();
    await _interactionService.AddModulesAsync(typeof(CommandModule).Assembly, serviceProvider);

    // Подписываемся на событие Ready
    _discordClient.Ready += async () =>
    {
        // Вызоваем регистрацию команд после того, как клиент будет готов
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
