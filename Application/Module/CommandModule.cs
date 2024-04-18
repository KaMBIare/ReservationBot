using Domain;
using Domain.Primitives;
using Infrastructure;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;


namespace Application.Module;


/// <summary>
/// команды бота
/// </summary>
public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private ReservationService _service = new ReservationService(new ApplicationContext());
    /// <summary>
    /// Никогда не вызывай эту команду
    /// </summary>
    [SlashCommand("moral_support", "Моральная поддержка")]
    public async Task NewerUse()
    {
            await RespondAsync("https://tenor.com/view/грусть-свинья-не-ростраюйся-gif-18090829220985071294");
    }
    
    /// <summary>
    /// команда выводящий пользователю все доступные команды, и их описание
    /// </summary>
    [SlashCommand("help", "Показывает список всех доступных команд")]
    public async Task Help()
    {
        // Список доступных команд
        var helpMessage = "Доступные команды:\n";
        // Добавьте другие команды по мере необходимости
        helpMessage += "/add_reservation - Добавить новую бронь\n";
        helpMessage += "/show_all_reservations - Показать все бронирования\n";
        helpMessage += "/show_my_reservations - Показать все мои брони\n";
        helpMessage += "/cancel_reservation_by_id - Отменить бронирование\n";
        helpMessage += "/moral_support - Моральная поддержка\n";
        
        //отправляем получившееся сообщение
        await RespondAsync(helpMessage, ephemeral:true);
    }
    
    /// <summary>
    /// команда вывода в чат всех броней
    /// </summary>
    [SlashCommand("show_all_reservations", "Показать все бронирования")]
    public async Task ShowAllReservations()
    {
        //обновляем статус всех броней
        RefreshReservationStatus();
        
        //ответ который будет отправлен пользователю
        string respond = "Предстоящие бронирования:";
        
        //получение всех бронеей
        List<Reservation> reservations = _service.GetAll();
        
        //добавление информации об бронях
        //счетчик для красивого вывода броней
        int counter = 0;
        foreach (var reservation in reservations)
        {
            if (reservation.ReservationStatus == ReservationStatus.Planned ||
                reservation.ReservationStatus == ReservationStatus.Meeting)
            {
                counter++;
                respond += $"\n{counter}. Время начала - {reservation.StartTime}, Время окончания - {reservation.EndTime}";
            }
        }
        
        //отправляем получившееся сообщение
        await RespondAsync(respond, ephemeral: true);
    }
    
    /// <summary>
    /// команда вывода в чат всех броней пользователя, вызывающего команду
    /// </summary>
    [SlashCommand("show_my_reservations", "Показать все мои брони")]
    public async Task ShowMyReservations()
    {
        //обновляем статус всех броней
        RefreshReservationStatus();
        
        //ответ который будет отправлен пользователю
        string respond = "";
        
        //получить пользователя который вызвал команду
        string admin = (Context.User as SocketGuildUser).Mention;
        
        //по его нику в базе данных вывести все брони в которых он является админом
        respond += "Все брони в которых вы являетесь админом:\n";
        int counter = 0; //счетчик для красивого вывода списком
        foreach (var reservation in _service.GetAllReservationByAdminId(admin))
        {
            //выводим все брони которые мы получили в методе GetAllReservationByAdminId
                counter++;
                respond +=
                    $"{counter}. {reservation.StartTime}, {reservation.ReservationStatus}. Список пользователей: ";
                
                //если список пользователей не пуст, то добавляем их в respond
                if (reservation.UsersNickname != null)
                {
                    foreach (var user in reservation.UsersNickname)
                    {
                        respond += $"{user} ";
                    }
                }
                
                //добавляем id в respond
                respond += $"\n\tId: {reservation.Id}";
                //отделяем брони новой строкой
                respond += "\n";
        }
        
        //по его нику в базе данных вывести все брони в которых он участвует
        respond += "\n \nВсе брони в которых вы являетесь участником:\n";
        counter = 0;//счетчик для красивого вывода списком
        foreach (var reservation in _service.GetAllReservationByUserId(admin))
        {
            //выводим все брони
            counter++;
            respond +=
                $"{counter}. {reservation.StartTime}, Админ - {reservation.AdminNickname}, {reservation.ReservationStatus}. Список пользователей: ";
            
            //если список пользователей не пуст, то добавляем их в respond
            if (reservation.UsersNickname != null)
            {
                foreach (var user in reservation.UsersNickname) 
                {
                    respond += $"{user} ";
                }
            }
            
            //отделяем брони новой строкой
            respond += "\n";
        }
        //отправляем получившееся сообщение
        await RespondAsync(respond, ephemeral: true);
    }
    
    /// <summary>
    /// команда отмены брони
    /// </summary>
    /// <param name="guid"></param>
    [SlashCommand("cancel_reservation_by_id", "Отменить бронирование")]
    public async Task CancelReservationById(string id)
    {
        //конвертация string id в Guid
        Guid guid = Guid.Parse(id);
        
        // получение брони которой пользователь пытается отменить
        Reservation cancelationReservation = _service.GetById(guid);
        
        //проверка на нахожддение брони с таким id в базе данных
        if (cancelationReservation == null)
        {
            //оповещаем пользователя об ошибке
            await RespondAsync("Бронь с таким id не найдена", ephemeral:true);
            return;
        }
        
        // проверка является ли пользователь админом брони id которой он ввел
        if (cancelationReservation.AdminNickname != (Context.User as SocketGuildUser).Mention)
        {
            //оповещаем пользователя об ошибке
            await RespondAsync("Нельзя отменить бронь, не являясь ее админом", ephemeral:true);
            return;
        }
        
        //устанавливаем статус брони на canceled
        cancelationReservation.ReservationStatus = ReservationStatus.Canceled;
        _service.context.SaveChanges();
        
        //оповещаем пользовотеля об успешной отмене бронирования
        await RespondAsync("Бронь успешно отменена", ephemeral:true);
    }
    
    /// <summary>
    /// команда добавления новой брони и сохранения ее в базу данных
    /// </summary>
    /// <param name="startTimeString"></param>
    /// <param name="endTimeString"></param>
    /// <param name="mentions"></param>
    [SlashCommand("add_reservation", "Добавить новую бронь")]
    public async Task AddReservation(
        [Summary("Время_начала", "Время начала брони")] string startTimeString,
        [Summary("Время_окончания", "Время окончания брони")] string endTimeString,
        [Summary("Пользователи", "пользователи, которые будут присутствовать в переговорке")] string mentions)
    {
        DateTime startTime = new DateTime();
        DateTime endTime = new DateTime();
        
        //конвертируем входные параметры времени из string в DataTime
        try
        {
            startTime = Convert.ToDateTime(startTimeString); 
            endTime = Convert.ToDateTime(endTimeString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await RespondAsync("Некоректно введена дата", ephemeral: true);
            throw;
        }
        

        // Парсинг строки упоминаний и получение списка пользователей
        var users = new List<SocketUser>();
        // Получаем всех пользователей, упомянутых в строке mentions
        foreach (var mention in mentions.Split(' '))
        {
            // Проверяем, что упоминание валидное
            if (MentionUtils.TryParseUser(mention, out ulong userId))
            {
                // Получаем пользователя из контекста
                var user = Context.Client.GetUser(userId);
                // Добавляем пользователя в список, если он не равен null
                if (user != null)
                {
                    users.Add(user);
                }
            }
        }

        //Строка с описание ошибки, если переменные startTime и endTime не пройдут валидацию в методе IsValidTime
        string? notValidDescription = "";
        if (!_service.IsValidTime(startTime, endTime, ref notValidDescription))
        {
            //сообщаем пользователю об ошибке
            await RespondAsync(notValidDescription, ephemeral:true);
            return;
        }
      
        //перобразуем лист пользователей в List<User>
        var reservationsUsers  = new List<string>();
        foreach (var user in users)
        {
            reservationsUsers.Add(user.Mention);
        }

        //создаем объект брони, для дальнейшего сохранения
        Guid reservationId = Guid.NewGuid();
        string admin = (Context.User as SocketGuildUser).Mention;
        Reservation reservation = new Reservation(reservationId, startTime, endTime, admin, reservationsUsers, ReservationStatus.Planned);

        //сохраняем объект брони в базу данных
        _service.Add(reservation);

        // Отправляем сообщение с упоминанием пользователей об успешном бронировании
        if (users.Count > 0)
        {
            //ответ на команду, который увидят пользователи
            string respond =
                $"Переговорка успешно забронированна на {startTime}\nid брони - {reservationId}\nПользователи участвующие в переговорах:\n{admin}\n";
            respond += string.Join("\n", users.Select(user => user.Mention));
            await RespondAsync(respond, ephemeral: true);
        }
        else
        {
            // Сообщаем, если не удалось найти пользователей
            await RespondAsync("Не удалось найти упомянутых пользователей.", ephemeral:true);
        }
    }

    /// <summary>
    /// метод обновляющий статус всех броней
    /// </summary>
    private async Task RefreshReservationStatus()
    {
        //достаем все брони их бд
        var reservations = _service.context.Reservations;
        //проходимся по всем броням и обновляем их статус если это необходимо
        foreach (var reservation in reservations)
        {
            //если статус брони "планируется", и время начал раньше чем текущее время то обновить их статус на "идет встреча"
            if (reservation.ReservationStatus == ReservationStatus.Planned && reservation.StartTime < DateTime.Now)
            {
                reservation.ReservationStatus = ReservationStatus.Meeting;
            }
            //если статус брони "идет встреча", и время окончания раньше чем текущее время то обновить их статус на "Завершенно"
            if (reservation.ReservationStatus == ReservationStatus.Meeting && reservation.EndTime < DateTime.Now)
            {
                reservation.ReservationStatus = ReservationStatus.Finished;
            }
        }
        //cохраняем изменения в бд
        _service.context.SaveChanges();
    }
}