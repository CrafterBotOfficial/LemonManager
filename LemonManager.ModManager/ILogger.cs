namespace LemonManager.ModManager;

public interface ILogger
{
    public void Log(object message);
    public void Warning(object message);
    public void Error(object message);
}