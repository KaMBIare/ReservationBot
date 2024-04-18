using Domain;
using Domain.Primitives;
using Infrastructure;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;


namespace Application.Module;



public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("newer_use", "Никогда не вызывай эту команду")]
    public async Task NewerUse()
    {
            await RespondAsync("https://tenor.com/view/грусть-свинья-не-ростраюйся-gif-18090829220985071294");
    }
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
        await RespondAsync(helpMessage, ephemeral:true);
    }
    
    /// <summary>
    /// команда вывода в чат всех броней
    /// </summary>
    [SlashCommand("show_all_reservations", "Показать все бронирования")]
    public async Task ShowAllReservations()
    {
        //ответ который конструируется в методе
        string respond = "Предстоящие бронирования:";
        //создание сервиса дял взаимодействия с базой данных
        ReservationService service = new ReservationService(new ApplicationContext());
        //получение всех бронеей
        List<Reservation> reservations = (List<Reservation>)service.GetAll();
        
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
        
        //выводим сообщение с информацией
        await RespondAsync(respond, ephemeral: true);
    }
    
    /// <summary>
    /// команда вывода в чат всех броней пользователя, вызывающего команду
    /// </summary>
    [SlashCommand("show_my_reservations", "Показать все мои брони")]
    public async Task ShowMyReservations()
    {
        //ответ который будет отправлен пользователю
        string respond = "";
        //получить пользователя который вызвал команду
        string admin = (Context.User as SocketGuildUser).Mention;
        
        //создание сервиса для взаимодействия с базой данных
        ReservationService service = new ReservationService(new ApplicationContext());
        
        //по его нику в базе данных вывести все брони в которых он является админом
        respond += "Все брони в которых вы являетесь админом:\n";
        //счетчик для красивого вывода списком
        int counter = 0;
        foreach (var reservation in service.GetAllReservationByAdminId(admin))
        {
            //выводим все брони кроме тех, что оменены или завершенны
            if (reservation.ReservationStatus == ReservationStatus.Planned ||
                reservation.ReservationStatus == ReservationStatus.Meeting)
            {
                counter++;
                respond +=
                    $"{counter}. {reservation.StartTime}, {reservation.ReservationStatus}. Список пользователей: ";
                foreach (var user in reservation.Users)
                {
                    respond += $"{user.Id} ";
                }

                //отделяем брони новой строкой
                respond += "\n";
            }
        }
        
        //по его нику в базе данных вывести все брони в которых он участвует
        respond += "\n \nВсе брони в которых вы являетесь участником:\n";
        //счетчик для красивого вывода списком
        counter = 0;
        foreach (var reservation in service.GetAllReservationByUserId(admin))
        {
            //выводим все брони кроме тех, что оменены или завершенны
            if (reservation.ReservationStatus == ReservationStatus.Planned ||
                reservation.ReservationStatus == ReservationStatus.Meeting)
            {
                counter++;
                respond +=
                    $"{counter}. {reservation.StartTime}, Админ - {reservation.Admin.Id}, {reservation.ReservationStatus}. Список пользователей: ";
                foreach (var user in reservation.Users)
                {
                    respond += $"{user.Id} ";
                }

                //отделяем брони новой строкой
                respond += "\n";
            }
        }
        
        //отправляем получившееся сообщения так, что бы его видел только тот кто вызвал команду
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
        
        //создание сервиса для взаимодействия с базой данных
        ReservationService service = new ReservationService(new ApplicationContext());
        
        // получение брони которой пользователь пытается отменить
        Reservation cancelationReservation = service.GetById(guid);
        
        if (cancelationReservation == null)
        {
            await RespondAsync("Бронь с таким id не найдена", ephemeral:true);
            return;
        }
        
        // проверка является ли пользователь админом брони id которой он ввел
        if (cancelationReservation.Admin.Id != (Context.User as SocketGuildUser).Mention)
        {
            await RespondAsync("Нельзя отменить бронь, не являясь ее админом", ephemeral:true);
            return;
        }
        
        //статус брони теперь canceled
        cancelationReservation.ReservationStatus = ReservationStatus.Canceled;
        //сообщаем об успешной отмене бронирования
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
        
        DateTime startTime = Convert.ToDateTime(startTimeString);
        DateTime endTime = Convert.ToDateTime(endTimeString);
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
       
        //создаем объект сервиса брони, и проверяем не занята ли переговорка на выбранное время
        var service = new ReservationService(new ApplicationContext());
        string? notValidDescription = "";
        if (!service.IsValidTime(startTime, endTime, ref notValidDescription))
        {
            await RespondAsync(notValidDescription, ephemeral:true);
            return;
        }
        
        //перобразуем лист пользователей в List<User>
        var reservationsUsers  = new List<User>();
        foreach (var i in users)
        {
            reservationsUsers.Add(new User(i.Mention, i.Mention));
        }
        
        
        //создаем объект брони, для дальнейшего сохранения
        Guid reservationId = Guid.NewGuid();
        Guid adminId = Guid.NewGuid();
        string admin = (Context.User as SocketGuildUser).Mention;
        Reservation reservation = new Reservation(reservationId, startTime, endTime, new User(admin, admin), reservationsUsers, ReservationStatus.Planned);
        //сохраняем объект брони в базу данных
        service.Add(reservation);
        
        // Отправляем сообщение с упоминанием пользователей об успешном бронировании
        if (users.Count > 0)
        {
            //ответ на команду, который увидят пользователи
            string respond = $"Переговорка успешно забронированна на {startTime}\nid брони - {reservationId}\nПользователи участвующие в переговорах:\n{admin}\n";
            respond += string.Join("\n", users.Select(user => user.Mention));
            await RespondAsync(respond, ephemeral:true);
        }
        else
        {
            // Сообщаем, если не удалось найти пользователей
            await RespondAsync("Не удалось найти упомянутых пользователей.", ephemeral:true);
        }
    }
}