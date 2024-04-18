namespace Application.Interfaces;
/// <summary>
/// Интерфейс базового репозитория
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IBaseRepository <T>
{
    public T? GetById(Guid id);
    public List<T> GetAll();
    public void Add(T entity);
    public void Update(T entity);
    public void Delete(Guid id);
}