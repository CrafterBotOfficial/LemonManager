using LemonManager.ModManager;
using System;
using System.Threading.Tasks;

namespace LemonManager;

public class PromptHandler : IPromptHandler
{
    public Task<string> PromptUser(string title, string message, PromptType type)
    {
        throw new NotImplementedException();
    }

    public Task<int> PromptUser(string title, params string[] options)
    {
        // throw new NotImplementedException();
        return  Task.FromResult(0);
    }
}
