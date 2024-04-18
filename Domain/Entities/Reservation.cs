using Domain.Primitives;

namespace Domain;

/// <summary>
/// класс конкретной брони
/// </summary>
public class Reservation : BaseEntity
{
    /// <summary>
    /// явно указал id тк с какого-то хера без этого EF выбрасовал ексепшины типа у вас нету id, хотя у базового класа мы его наследуем
    /// </summary>
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime
    { get; set; }
    public User Admin { get; set; }
    public List<User> Users { get; set; }
    public ReservationStatus ReservationStatus { get; set; }

    public Reservation(Guid Id, DateTime startTime, DateTime endTime, User Admin, List<User> Users,
        ReservationStatus reservationStatus)
    {
        this.Id = Id;
        StartTime = startTime;
        EndTime = endTime;
        this.Admin = Admin;
        this.Users = Users;
        ReservationStatus = reservationStatus;
    }

    public Reservation()
    {
    }
}