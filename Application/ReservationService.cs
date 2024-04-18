using Application.Interfaces;
using Domain;
using Domain.Primitives;
using Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace Application;

public class ReservationService : IBaseRepository<Reservation>
{
    public ApplicationContext context;

    public ReservationService(ApplicationContext context)
    {
        this.context = context;
    }
    
    public Reservation? GetById(Guid id)
    {
        return context.Reservations.Find(id);
    }

    public List<Reservation> GetAll()
    {
        return context.Reservations.ToList();
    }

    public void Add(Reservation entity)
    {
        context.Reservations.Add(entity);
        
        try
        {
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public void Update(Reservation entity)
    {
        var updatingEntity = context.Reservations.FirstOrDefault(e => e.Equals(entity));
        //проверка на наличие сущности в базе данных, если нету то пробрасывает исключение
        if (updatingEntity == null)
        {
            throw new NullReferenceException();
        }

        updatingEntity = entity;
        context.SaveChanges();
    }

    public void Delete(Guid id)
    {
        context.Reservations.Remove(context.Reservations.Find(id));
    }
    /// <summary>
    /// возвращает true, если указаное время прошло ряд проверок на валидность
    /// </summary>
    /// <param name="startTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    public bool IsValidTime(DateTime startTime, DateTime endTime, ref string? notValidDescription)
    {
        //проверка на коректность ввода времени
        if (endTime < startTime)
        {
            notValidDescription = "Время начала не может быть позже времени окончания";
            return false;
        }
        //проверяем на попытку забронировать переговорку в прошлом
        if (startTime < DateTime.Now)
        {
            notValidDescription ="Время брони не может быть в прошлом";
            return false;
        }
        // проверка не занимает ли общее время брони больше 24 часов
        if ((endTime - startTime).Duration().TotalHours > 24)
        {
            // Разница больше 24 часов (1 суток)
            notValidDescription = "Время брони не может превышать 24 часа";
            return false;
        }
        try
        {
            foreach (var i in context.Reservations)
            {
                //если время планируемой записи находится в промежутке между началом и концом, уже существуюущей записи, которая либо планируется, либо прямо сейчас происходит встреча, то вернуть false
                if (((startTime < i.EndTime && startTime > i.StartTime)
                     || (endTime < i.EndTime && endTime > i.StartTime))
                    &&(i.ReservationStatus == ReservationStatus.Meeting || i.ReservationStatus== ReservationStatus.Planned))
                {
                    notValidDescription = "На это время переговорка уже забронированна";
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        return true;
    }

    /// <summary>
    /// возвращает все брони в который указанный пользователь является админом
    /// </summary>
    /// <param name="adminNickname"></param>
    /// <returns></returns>
    public List<Reservation> GetAllReservationByAdminId(string adminNickname)
    {
        var reservations = new List<Reservation>();
        foreach (var reservation in context.Reservations)
        {
            if (reservation.AdminNickname == adminNickname)
            {
                reservations.Add(reservation);
            }
        }
        return reservations;
    }

    /// <summary>
    /// возвращает все брони в которых указанный пользователь участвует
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public List<Reservation> GetAllReservationByUserId(string userNickname)
    {
        var reservations = new List<Reservation>();
        foreach (var reservation in context.Reservations)
        {
            foreach (var user in reservation.UsersNickname)
            {
                if (user == userNickname)
                {
                    reservations.Add(reservation);
                }
            }
        }

        return reservations;
    }
}