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
    public string AdminNickname { get; set; }
    public List<string> UsersNickname { get; set; }
    public ReservationStatus ReservationStatus { get; set; }

    public Reservation(Guid Id, DateTime startTime, DateTime endTime, string adminNickname, List<string> usersNickname,
        ReservationStatus reservationStatus)
    {
        this.Id = Id;
        StartTime = startTime;
        EndTime = endTime;
        this.AdminNickname = adminNickname;
        this.UsersNickname = usersNickname;
        ReservationStatus = reservationStatus;
    }

    public Reservation()
    {
    }
}