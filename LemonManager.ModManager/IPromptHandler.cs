using System.Threading.Tasks;

namespace LemonManager.ModManager;

public interface IPromptHandler
{
    public Task<bool> PromptUser(string title, string message, PromptType type);
    public Task<int> PromptUser(string title, params string[] options);

    public void SetStatus(string status);
}

public enum PromptType
{
    Notification,
    Confirmation,
}