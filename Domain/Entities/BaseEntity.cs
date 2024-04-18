namespace Domain;

/// <summary>
/// базовый класс для всех сущностей
/// </summary>
public abstract class BaseEntity
{
    public Guid Id;

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity entity)
            return false;
        return entity.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}