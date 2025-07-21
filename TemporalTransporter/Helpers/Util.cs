using Vintagestory.API.Config;

namespace TemporalTransporter.Helpers;

public static class Util
{
    public static string LangStr(string key, params object[] args)
    {
        return Lang.Get($"temporaltransporter:{key}", args);
    }
}